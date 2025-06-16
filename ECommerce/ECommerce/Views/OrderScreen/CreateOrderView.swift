import SwiftUI

struct CreateOrderView: View {
    @Environment(\.dismiss) var dismiss
    @EnvironmentObject var ordersViewModel: OrdersViewModel
    @StateObject private var viewModel = CreateOrderViewModel()

    var body: some View {
        VStack(spacing: 24) {
            HStack {
                Text("Новый заказ")
                    .font(.title3.bold())
                Spacer()
                Button {
                    dismiss()
                } label: {
                    Image(systemName: "xmark")
                        .foregroundColor(.gray)
                        .padding(8)
                        .background(Color(.systemGray5))
                        .clipShape(Circle())
                }
            }

            Divider()

            if viewModel.allUserIds.isEmpty {
                Text("Нет доступных счетов")
                    .foregroundColor(.secondary)
                    .padding(.top)
            } else {
                VStack(alignment: .leading, spacing: 12) {
                    Text("Выберите счёт")
                        .font(.subheadline)
                        .foregroundColor(.gray)
                        .padding(.horizontal)

                    Menu {
                        ForEach(viewModel.allUserIds, id: \.self) { userId in
                            Button(userId) {
                                viewModel.selectedAccountId = userId
                            }
                        }
                    } label: {
                        HStack {
                            Text(viewModel.selectedAccountId.isEmpty ? "Не выбрано" : viewModel.selectedAccountId)
                                .foregroundColor(viewModel.selectedAccountId.isEmpty ? .gray : .primary)
                            Spacer()
                            Image(systemName: "chevron.down")
                                .foregroundColor(.gray)
                        }
                        .padding()
                        .background(Color(.systemGray6))
                        .cornerRadius(10)
                        .padding(.horizontal)
                    }
                }
            }

            TextField("Описание", text: $viewModel.description)
                .textFieldStyle(.roundedBorder)
                .padding(.horizontal)

            TextField("Сумма", text: $viewModel.amount)
                .keyboardType(.decimalPad)
                .textFieldStyle(.roundedBorder)
                .padding(.horizontal)

            Button {
                Task {
                    if await viewModel.createOrder() {
                        await ordersViewModel.loadOrders()
                        dismiss()
                    }
                }
            } label: {
                HStack {
                    Spacer()
                    Text("Создать заказ")
                        .fontWeight(.semibold)
                    Spacer()
                }
            }
            .padding()
            .background(Color.green)
            .foregroundColor(.white)
            .cornerRadius(12)
            .padding(.horizontal)

            Spacer()
        }
        .padding()
        .background(
            RoundedRectangle(cornerRadius: 20)
                .fill(Color(.systemBackground))
        )
        .presentationDetents([.medium])
        .presentationDragIndicator(.visible)
        .onAppear {
            Task {
                await viewModel.loadUserIds()
            }
        }
    }
}
