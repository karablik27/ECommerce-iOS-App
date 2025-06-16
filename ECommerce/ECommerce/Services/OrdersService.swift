import Foundation

final class OrdersService {
    // Получение всех заказов
    func getOrders() async throws -> [Order] {
        let url = NetworkConstants.baseURL.appendingPathComponent("/orders")
        let (data, response) = try await URLSession.shared.data(from: url)

        guard let httpResp = response as? HTTPURLResponse, httpResp.statusCode == 200 else {
            throw URLError(.badServerResponse)
        }

        return try JSONDecoder().decode([Order].self, from: data)
    }

    // Создание нового заказа
    func createOrder(request: CreateOrderRequest) async throws -> UUID {
        let url = NetworkConstants.baseURL.appendingPathComponent("/orders")
        var urlRequest = URLRequest(url: url)
        urlRequest.httpMethod = "POST"
        urlRequest.setValue("application/json", forHTTPHeaderField: "Content-Type")
        urlRequest.httpBody = try JSONEncoder().encode(request)

        let (data, response) = try await URLSession.shared.data(for: urlRequest)

        guard let httpResp = response as? HTTPURLResponse, httpResp.statusCode == 200 else {
            throw URLError(.badServerResponse)
        }

        let json = try JSONDecoder().decode([String: String].self, from: data)
        guard let idString = json["id"], let id = UUID(uuidString: idString) else {
            throw URLError(.cannotParseResponse)
        }

        return id
    }
    
    func getOrder(by id: UUID) async throws -> Order {
        let url = NetworkConstants.baseURL.appendingPathComponent("/orders/\(id.uuidString)")
        let (data, response) = try await URLSession.shared.data(from: url)

        guard let httpResp = response as? HTTPURLResponse, httpResp.statusCode == 200 else {
            throw URLError(.badServerResponse)
        }

        return try JSONDecoder().decode(Order.self, from: data)
    }
}
