import SwiftUI

struct OrdersView: View {
    @ObservedObject var viewModel: OrdersViewModel
    var refreshTrigger: UUID
    var onOrderCreated: () -> Void

    @State private var showCreateOrder = false
    @State private var copiedOrderId: String?
    @State private var showCopiedToast = false

    var body: some View {
        ZStack {
            NavigationView {
                VStack(alignment: .leading, spacing: 16) {
                    HStack {
                        Text("Операции")
                            .font(.largeTitle.bold())
                        Spacer()
                        Button {
                            showCreateOrder = true
                        } label: {
                            Image(systemName: "plus")
                                .font(.title2)
                                .foregroundColor(.green)
                        }
                    }
                    .padding(.horizontal)
                    .padding(.top, 8)

                    if viewModel.isLoading && viewModel.orders.isEmpty {
                        Spacer()
                        ProgressView("Загрузка...")
                        Spacer()
                    } else if viewModel.orders.isEmpty {
                        Spacer()
                        Text("Нет заказов")
                            .foregroundColor(.secondary)
                        Spacer()
                    } else {
                        ScrollView {
                            LazyVStack(spacing: 12) {
                                ForEach(viewModel.orders, id: \.id) { order in
                                    OrderCell(order: order) {
                                        UIPasteboard.general.string = order.id
                                        copiedOrderId = order.id
                                        withAnimation {
                                            showCopiedToast = true
                                        }
                                        DispatchQueue.main.asyncAfter(deadline: .now() + 2) {
                                            withAnimation {
                                                showCopiedToast = false
                                            }
                                        }
                                    }
                                    .padding(.horizontal)
                                }
                            }
                            .padding(.top, 8)
                        }
                        .refreshable {
                            await viewModel.loadOrders()
                        }
                    }
                }
                .navigationBarHidden(true)
                .sheet(isPresented: $showCreateOrder, onDismiss: {
                    // 👉 после закрытия CreateOrderView пересоздай OrdersView
                    onOrderCreated()
                }) {
                    CreateOrderView().environmentObject(viewModel)
                }
                .onAppear {
                    Task {
                        await viewModel.loadOrders()
                    }
                }
            }

            if showCopiedToast, let copiedId = copiedOrderId {
                VStack {
                    Spacer()
                    HStack {
                        Image(systemName: "checkmark.circle.fill")
                            .foregroundColor(.white)
                        Text("ID скопирован: \(copiedId)")
                            .foregroundColor(.white)
                    }
                    .padding()
                    .background(Color.black.opacity(0.85))
                    .cornerRadius(12)
                    .padding(.bottom, 90)
                    .transition(.move(edge: .bottom).combined(with: .opacity))
                }
                .zIndex(2)
            }
        }
        .id(refreshTrigger)
    }
}



struct OrdersScreenContainer: View {
    @StateObject private var viewModel = OrdersViewModel()
    @State private var refreshTrigger = UUID()

    var body: some View {
        OrdersView(viewModel: viewModel, refreshTrigger: refreshTrigger) {
            refreshTrigger = UUID()
        }
    }
}
