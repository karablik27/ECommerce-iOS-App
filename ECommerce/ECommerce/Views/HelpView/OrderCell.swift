import SwiftUI

struct OrderCell: View {
    let order: Order
    let onTap: () -> Void

    private func statusColor(for status: String) -> Color {
        switch status.uppercased() {
        case "FINISHED": return .green
        case "CANCELED", "CANCELLED": return .red
        case "NEW": return .orange
        default: return .gray
        }
    }

    var body: some View {
        VStack(alignment: .leading, spacing: 10) {
            HStack(alignment: .top) {
                VStack(alignment: .leading, spacing: 4) {
                    Text(order.description)
                        .font(.headline)
                        .foregroundColor(.primary)

                    Text("Пользователь: \(order.userId)")
                        .font(.subheadline)
                        .foregroundColor(.secondary)

                    Text("Сумма: \(order.amount.description)")
                        .font(.subheadline)
                        .bold()
                        .foregroundColor(.primary)
                }

                Spacer()

                Text(order.status.uppercased())
                    .font(.caption)
                    .bold()
                    .padding(.horizontal, 10)
                    .padding(.vertical, 4)
                    .background(statusColor(for: order.status).opacity(0.1))
                    .foregroundColor(statusColor(for: order.status))
                    .cornerRadius(8)
            }
        }
        .frame(maxWidth: .infinity)
        .padding()
        .background(Color(.systemBackground))
        .cornerRadius(16)
        .shadow(color: Color.black.opacity(0.1), radius: 6, x: 0, y: 3)
        .onTapGesture {
            onTap()
        }
    }
}
