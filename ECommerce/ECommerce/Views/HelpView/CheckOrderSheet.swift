import SwiftUI

struct CheckOrderSheet: View {
    @ObservedObject var viewModel: SettingsViewModel
    @Environment(\.dismiss) private var dismiss

    var body: some View {
        VStack(spacing: 24) {
            HStack {
                Text("Проверка заказа")
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

            Text("Чтобы скопировать ID, нажмите на нужный заказ на экране заказов")
                .font(.footnote)
                .foregroundColor(.secondary)
                .multilineTextAlignment(.center)
                .padding(.horizontal)

            TextField("Введите ID заказа", text: $viewModel.inputOrderId)
                .textFieldStyle(.roundedBorder)
                .textInputAutocapitalization(.never)
                .autocorrectionDisabled()
                .padding(.horizontal)

            HStack(spacing: 12) {
                Button {
                    viewModel.pasteFromClipboard()
                } label: {
                    HStack {
                        Image(systemName: "doc.on.clipboard")
                        Text("Вставить")
                    }
                    .frame(maxWidth: .infinity)
                }
                .padding()
                .background(Color(.systemGray6))
                .foregroundColor(.primary)
                .cornerRadius(10)

                Button {
                    Task { await viewModel.checkOrderStatus() }
                } label: {
                    HStack {
                        Image(systemName: "checkmark.seal.fill")
                        Text("Проверить")
                    }
                    .frame(maxWidth: .infinity)
                }
                .padding()
                .background(Color.green)
                .foregroundColor(.white)
                .cornerRadius(10)
            }
            .padding(.horizontal)

            if let status = viewModel.orderStatus {
                Text("Статус: \(status)")
                    .foregroundColor(status == "FINISHED" ? .green : (status == "CANCELLED" ? .red : .gray))
                    .font(.headline)
                    .padding(.top, 4)
            }

            if let error = viewModel.errorMessage {
                Text(error)
                    .foregroundColor(.red)
                    .padding(.top, 4)
            }

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
