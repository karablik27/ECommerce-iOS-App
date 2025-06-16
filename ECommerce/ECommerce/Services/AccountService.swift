import Foundation

final class AccountService {
    // Создание аккаунта
    func createAccount(userId: String) async throws {
        let url = NetworkConstants.baseURL.appendingPathComponent("/api/Accounts/\(userId)")
        var request = URLRequest(url: url)
        request.httpMethod = "POST"

        let (_, response) = try await URLSession.shared.data(for: request)

        guard let httpResp = response as? HTTPURLResponse, httpResp.statusCode == 201 else {
            throw URLError(.badServerResponse)
        }
    }

    // Пополнение баланса
    func deposit(userId: String, amount: Decimal) async throws {
        let url = NetworkConstants.baseURL.appendingPathComponent("/api/Accounts/\(userId)/deposit/\(amount)")
        var request = URLRequest(url: url)
        request.httpMethod = "POST"

        let (_, response) = try await URLSession.shared.data(for: request)

        guard let httpResp = response as? HTTPURLResponse else {
            throw URLError(.badServerResponse)
        }

        if httpResp.statusCode == 400 {
            throw DepositError.invalidAmount
        } else if httpResp.statusCode != 200 {
            throw URLError(.badServerResponse)
        }
    }

    // Получение баланса
    func getBalance(userId: String) async throws -> BankAccount {
        let url = NetworkConstants.baseURL.appendingPathComponent("/api/Accounts/\(userId)/balance")
        let (data, response) = try await URLSession.shared.data(from: url)

        guard let httpResp = response as? HTTPURLResponse, httpResp.statusCode == 200 else {
            throw URLError(.badServerResponse)
        }

        return try JSONDecoder().decode(BankAccount.self, from: data)
    }

}

enum DepositError: Error, LocalizedError {
    case invalidAmount

    var errorDescription: String? {
        switch self {
        case .invalidAmount:
            return "Сумма должна быть больше нуля"
        }
    }
}
