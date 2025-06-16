import Foundation

struct BankAccount: Codable, Identifiable {
    let userId: String
    let balance: Decimal?

    var id: String { userId }
}
