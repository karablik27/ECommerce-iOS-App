import Foundation

struct Order: Identifiable, Codable {
    let id: String
    let userId: String
    let amount: Decimal
    let description: String
    let status: String
}

