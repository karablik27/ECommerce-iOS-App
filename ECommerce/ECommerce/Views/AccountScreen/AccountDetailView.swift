import SwiftUI

struct AccountDetailView: View {
    let account: BankAccount
    @State private var isBalanceVisible = false
    @State private var depositAmount: String = ""
    @State private var showDepositField = false
    @State private var depositError: String?
    
    @EnvironmentObject var viewModel: AccountsViewModel
    @Environment(\.dismiss) var dismiss

    var body: some View {
        ZStack {
            LinearGradient(
                gradient: Gradient(colors: [
                    Color(.systemBackground),
                    Color(.secondarySystemBackground)
                ]),
                startPoint: .top,
                endPoint: .bottom
            )
            .ignoresSafeArea()

            VStack(spacing: 24) {
                // Назад
                HStack {
                    Button(action: {
                        dismiss()
                    }) {
                        HStack(spacing: 4) {
                            Image(systemName: "chevron.left")
                            Text("Назад")
                        }
                        .foregroundColor(.green)
                        .font(.headline)
                    }
                    Spacer()
                }
                .padding(.horizontal)
                .padding(.top, 8)

                // Информация о счёте
                VStack(alignment: .leading, spacing: 12) {
                    Text("Счёт: \(account.userId)")
                        .font(.headline)
                        .foregroundColor(.white)

                    HStack {
                        Text("Баланс:")
                            .font(.subheadline)
                            .foregroundColor(.white.opacity(0.8))

                        Spacer()

                        Text(isBalanceVisible ? formattedBalance : "•••••••")
                            .bold()
                            .font(.title3)
                            .foregroundColor(.white)

                        Button(action: {
                            isBalanceVisible.toggle()
                        }) {
                            Image(systemName: isBalanceVisible ? "eye.slash" : "eye")
                                .foregroundColor(.white)
                        }
                    }
                }
                .padding()
                .background(
                    LinearGradient(
                        gradient: Gradient(colors: [.green, .mint]),
                        startPoint: .topLeading,
                        endPoint: .bottomTrailing
                    )
                )
                .clipShape(RoundedRectangle(cornerRadius: 20))
                .shadow(color: .black.opacity(0.2), radius: 12, x: 0, y: 6)
                .padding(.horizontal)

                // Кнопка "Пополнить"
                Button(action: {
                    withAnimation {
                        showDepositField.toggle()
                    }
                }) {
                    Text("Пополнить баланс")
                        .font(.body.weight(.medium))
                        .foregroundColor(.green)
                        .padding(.vertical, 6)
                        .padding(.horizontal, 20)
                        .background(
                            Capsule()
                                .stroke(Color.green, lineWidth: 1.5)
                        )
                }

                // Поле ввода + кнопка "Подтвердить"
                if showDepositField {
                    VStack(spacing: 12) {
                        TextField("Сумма", text: $depositAmount)
                            .keyboardType(.decimalPad)
                            .padding(12)
                            .background(Color(.systemBackground))
                            .cornerRadius(10)
                            .shadow(radius: 4)
                            .padding(.horizontal)

                        if let error = depositError {
                            Text(error)
                                .foregroundColor(.red)
                                .font(.caption)
                        }

                        Button("Подтвердить") {
                            guard let amount = Decimal(string: depositAmount), amount > 0 else {
                                withAnimation {
                                    depositError = "Введите сумму больше 0"
                                }
                                return
                            }

                            Task {
                                await viewModel.deposit(to: account, amount: amount)
                                depositAmount = ""
                                showDepositField = false
                                depositError = nil
                                await viewModel.loadSavedAccounts()
                            }
                        }
                        .buttonStyle(.borderedProminent)
                        .tint(.green)
                        .padding(.horizontal)
                    }
                    .transition(.opacity.combined(with: .move(edge: .bottom)))
                }

                Spacer()
            }
            .padding(.top)
        }
        .navigationBarHidden(true)
    }

    private var formattedBalance: String {
        if let value = account.balance {
            return "\(value) ₽"
        } else {
            return "—"
        }
    }
}
