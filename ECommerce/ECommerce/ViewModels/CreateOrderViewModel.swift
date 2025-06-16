import Foundation
import Combine

final class CreateOrderViewModel: ObservableObject {
    @Published var allUserIds: [String] = []
    @Published var selectedAccountId: String = ""
    @Published var amount: String = ""
    @Published var description: String = ""

    private let ordersService = OrdersService()
    private let accountService = AccountService()

    func loadUserIds() async {
        let ids = UserDefaults.standard.stringArray(forKey: StorageKeys.savedAccountUserIds) ?? []

        let validIds = await withTaskGroup(of: String?.self) { group in
            for id in ids {
                group.addTask {
                    do {
                        _ = try await self.accountService.getBalance(userId: id)
                        return id
                    } catch {
                        print("Не найден аккаунт \(id), пропущен")
                        return nil
                    }
                }
            }

            var result: [String] = []
            for await value in group {
                if let id = value {
                    result.append(id)
                }
            }
            return result
        }

        await MainActor.run {
            self.allUserIds = validIds
            self.selectedAccountId = validIds.first ?? ""
        }
    }



    func createOrder() async -> Bool {
        guard
            !selectedAccountId.isEmpty,
            let decimalAmount = Decimal(string: amount),
            !description.isEmpty
        else {
            return false
        }

        let req = CreateOrderRequest(userId: selectedAccountId, amount: decimalAmount, description: description)

        do {
            _ = try await ordersService.createOrder(request: req)
            return true
        } catch {
            print("Ошибка создания заказа: \(error)")
            return false
        }
    }
}
