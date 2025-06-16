import SwiftUI

struct AccountsView: View {
    @StateObject private var viewModel = AccountsViewModel()
    @State private var showCreateSheet = false
    @State private var errorMessage: String?

    var body: some View {
        NavigationView {
            VStack(alignment: .leading, spacing: 16) {
                HStack {
                    Text("Счета")
                        .font(.largeTitle.bold())
                    Spacer()
                    Button {
                        showCreateSheet = true
                    } label: {
                        Image(systemName: "plus")
                            .font(.title2)
                            .foregroundColor(.green)
                    }
                }
                .padding(.horizontal)
                .padding(.top, 8)

                if viewModel.accounts.isEmpty {
                    Text("Нет доступных счетов.")
                        .foregroundColor(.secondary)
                        .padding()
                } else {
                    ScrollView {
                        LazyVStack(spacing: 12) {
                            ForEach(viewModel.accounts, id: \.userId) { account in
                                NavigationLink(
                                    destination: AccountDetailView(account: account)
                                        .environmentObject(viewModel)
                                ) {
                                    AccountCardView(account: account)
                                        .padding(.horizontal)
                                }
                            }
                        }
                        .padding(.top, 8)
                    }
                }

                Spacer()
            }
            .navigationBarHidden(true)
            .onAppear {
                Task {
                    await viewModel.loadSavedAccounts()
                }
            }
            .sheet(isPresented: $showCreateSheet) {
                CreateAccountSheet(
                    userIdInput: $viewModel.userIdInput,
                    onCreate: {
                        await viewModel.createAccount { error in
                            self.errorMessage = error
                        }
                        showCreateSheet = false
                    }
                )
                .presentationDetents([.medium])
            }
            .alert("Ошибка", isPresented: Binding(
                get: { errorMessage != nil },
                set: { _ in errorMessage = nil })
            ) {
                Button("ОК", role: .cancel) {}
            } message: {
                Text(errorMessage ?? "")
            }
        }
    }
}
