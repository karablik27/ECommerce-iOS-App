import SwiftUI

struct CreateAccountSheet: View {
    @Binding var userIdInput: String
    var onCreate: () async -> Void

    @Environment(\.dismiss) private var dismiss

    var body: some View {
        VStack(spacing: 24) {
            HStack {
                Text("Создание счёта")
                    .font(.title3.bold())
                Spacer()
                Button {
                    dismiss()
                } label: {
                    Image(systemName: "xmark")
                        .foregroundColor(.gray)
                        .padding(8)
                        .background(Color(.systemGray5))
                        .clipShape(Circle())
                }
            }

            Divider()

            // Поле ввода
            VStack(spacing: 12) {
                TextField("Введите User ID", text: $userIdInput)
                    .textFieldStyle(.roundedBorder)
                    .padding(.horizontal)
            }

            // Кнопка
            Button {
                Task { await onCreate() }
            } label: {
                HStack {
                    Spacer()
                    Text("Создать")
                        .fontWeight(.semibold)
                    Spacer()
                }
            }
            .padding()
            .background(Color.green)
            .foregroundColor(.white)
            .cornerRadius(12)
            .padding(.horizontal)

            Spacer()
        }
        .padding()
        .background(
            RoundedRectangle(cornerRadius: 20)
                .fill(Color(.systemBackground))
        )
        .presentationDetents([.medium])
        .presentationDragIndicator(.visible)
    }
}
