import Foundation
import Combine

final class AccountsViewModel: ObservableObject {
    private let accountService = AccountService()
    private let savedAccountsKey = StorageKeys.savedAccountUserIds

    @Published var userIdInput: String = ""
    @Published var accounts: [BankAccount] = []
    @Published var selectedAccount: BankAccount?
    @Published var isLoading = false
    
    var userIds: [String] {
        accounts.map(\.userId)
    }

    init() {
        Task {
            await loadSavedAccounts()
        }
    }

    private func saveUserId(_ userId: String) {
        var saved = UserDefaults.standard.stringArray(forKey: savedAccountsKey) ?? []
        guard !saved.contains(userId) else { return }
        saved.append(userId)
        UserDefaults.standard.set(saved, forKey: savedAccountsKey)
    }

    private func removeUserId(_ userId: String) {
        var saved = UserDefaults.standard.stringArray(forKey: savedAccountsKey) ?? []
        saved.removeAll { $0 == userId }
        UserDefaults.standard.set(saved, forKey: savedAccountsKey)
    }

    private func getSavedUserIds() -> [String] {
        UserDefaults.standard.stringArray(forKey: savedAccountsKey) ?? []
    }

    func loadSavedAccounts() async {
        await MainActor.run { self.isLoading = true }

        let userIds = getSavedUserIds()

        let loadedAccounts: [BankAccount] = await withTaskGroup(of: BankAccount?.self) { group in
            for userId in userIds {
                group.addTask {
                    do {
                        return try await self.accountService.getBalance(userId: userId)
                    } catch {
                        print("Удаление невалидного userId '\(userId)' из UserDefaults")
                        await MainActor.run {
                            self.removeUserId(userId)
                        }
                        return nil
                    }
                }
            }

            var results: [BankAccount] = []

            for await account in group {
                if let account = account {
                    results.append(account)
                }
            }

            return results
        }

        await MainActor.run {
            self.accounts = loadedAccounts
            self.isLoading = false
        }
    }


    func createAccount(onError: @escaping (String?) -> Void) async {
        guard !userIdInput.isEmpty else {
            await MainActor.run { onError("User ID не может быть пустым") }
            return
        }

        await MainActor.run { self.isLoading = true }

        do {
            try await accountService.createAccount(userId: userIdInput)
            let account = try await accountService.getBalance(userId: userIdInput)

            await MainActor.run {
                accounts.append(account)
                saveUserId(account.userId)
                userIdInput = ""
            }
        } catch {
            await MainActor.run {
                if "\(error)".contains("already exists") {
                    onError("Счёт с таким User ID уже существует")
                } else {
                    onError("Не удалось создать счёт")
                }
            }
        }

        await MainActor.run { self.isLoading = false }
    }

    func deposit(to account: BankAccount, amount: Decimal) async {
        await MainActor.run { self.isLoading = true }

        do {
            try await accountService.deposit(userId: account.userId, amount: amount)
            let updated = try await accountService.getBalance(userId: account.userId)
            await MainActor.run {
                if let index = accounts.firstIndex(where: { $0.userId == account.userId }) {
                    accounts[index] = updated
                }
            }
        } catch {
            print("Ошибка пополнения: \(error)")
        }

        await MainActor.run { self.isLoading = false }
    }

    func showBalance(for account: BankAccount) async -> BankAccount? {
        do {
            return try await accountService.getBalance(userId: account.userId)
        } catch {
            print("Ошибка получения баланса: \(error)")
            return nil
        }
    }
}
