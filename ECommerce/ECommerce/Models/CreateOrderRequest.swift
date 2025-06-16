import Foundation

struct CreateOrderRequest: Codable {
    let userId: String
    let amount: Decimal
    let description: String
}
