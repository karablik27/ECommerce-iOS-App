import SwiftUI

struct AccountCardView: View {
    let account: BankAccount
    @Environment(\.colorScheme) private var colorScheme

    var body: some View {
        HStack(spacing: 16) {
            Image(systemName: "person.crop.circle.fill")
                .resizable()
                .frame(width: 40, height: 40)
                .foregroundColor(.white)
                .background(
                    LinearGradient(
                        colors: [.green, .mint],
                        startPoint: .topLeading,
                        endPoint: .bottomTrailing
                    )
                )
                .clipShape(Circle())

            Text("Счёт: \(account.userId)")
                .font(.headline)
                .foregroundColor(.primary)

            Spacer()

            Image(systemName: "chevron.right")
                .foregroundColor(.gray)
        }
        .padding()
        .background(
            colorScheme == .dark
                ? Color(.secondarySystemBackground)
                : Color(.systemBackground)
        )
        .cornerRadius(16)
        .shadow(
            color: colorScheme == .dark
                ? Color.black.opacity(0.3)
                : Color.black.opacity(0.15),
            radius: 8, x: 0, y: 4
        )
    }
}
