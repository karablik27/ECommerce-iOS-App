import Foundation
import Combine
import UIKit

final class SettingsViewModel: ObservableObject {
    @Published var inputOrderId: String = ""
    @Published var orderStatus: String?
    @Published var errorMessage: String?

    private let ordersService = OrdersService()

    func checkOrderStatus() async {
        guard let uuid = UUID(uuidString: inputOrderId.trimmingCharacters(in: .whitespacesAndNewlines)) else {
            await MainActor.run { errorMessage = "Неверный формат ID" }
            return
        }

        do {
            let order = try await ordersService.getOrder(by: uuid)
            await MainActor.run {
                orderStatus = order.status
                errorMessage = nil
            }
        } catch {
            await MainActor.run {
                orderStatus = nil
                errorMessage = "Заказ не найден"
            }
        }
    }

    func pasteFromClipboard() {
        if let clipboard = UIPasteboard.general.string {
            inputOrderId = clipboard
        }
    }
}
