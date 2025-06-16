import Foundation
import Combine

final class OrdersViewModel: ObservableObject {
    private let ordersService = OrdersService()

    @Published var orders: [Order] = []
    @Published var isLoading = false

    private var timer: Timer?

    func loadOrders() async {
        await MainActor.run { self.isLoading = true }
        defer {
            Task { @MainActor in self.isLoading = false }
        }

        do {
            let fetched = try await ordersService.getOrders()
            await MainActor.run {
                self.orders = fetched
            }
        } catch {
            print("Ошибка загрузки заказов: \(error)")
        }
    }

    func startPolling() {
        stopPolling()

        timer = Timer.scheduledTimer(withTimeInterval: 3.0, repeats: true) { [weak self] _ in
            Task {
                await self?.loadOrders()
            }
        }
    }

    func stopPolling() {
        timer?.invalidate()
        timer = nil
    }
}
