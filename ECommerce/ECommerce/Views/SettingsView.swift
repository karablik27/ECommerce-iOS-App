import SwiftUI

struct SettingsView: View {
    @StateObject private var viewModel = SettingsViewModel()
    @State private var showCheckSheet = false
    @AppStorage("isDarkMode") private var isDarkMode = false

    var body: some View {
        NavigationStack {
            ScrollView {
                VStack(spacing: 32) {
                    VStack(alignment: .leading, spacing: 12) {
                        Text("Заказы")
                            .font(.title2.bold())

                        Button {
                            showCheckSheet = true
                        } label: {
                            HStack(spacing: 12) {
                                Image(systemName: "magnifyingglass.circle.fill")
                                    .font(.title2)
                                Text("Проверить статус заказа")
                                    .font(.headline)
                                Spacer()
                                Image(systemName: "chevron.right")
                                    .foregroundColor(.gray)
                            }
                            .padding()
                            .background(Color.green.opacity(0.15))
                            .foregroundColor(.green)
                            .cornerRadius(16)
                        }
                    }

                    // Темная тема
                    VStack(alignment: .leading, spacing: 12) {
                        Text("Внешний вид")
                            .font(.title2.bold())

                        HStack {
                            Text(isDarkMode ? "Тёмная тема" : "Светлая тема")
                                .font(.headline)

                            Spacer()

                            Button {
                                withAnimation {
                                    isDarkMode.toggle()
                                }
                            } label: {
                                Image(systemName: isDarkMode ? "moon.fill" : "sun.max.fill")
                                    .font(.title2)
                                    .foregroundColor(.white)
                                    .padding()
                                    .background(isDarkMode ? Color.black : Color.orange)
                                    .clipShape(Circle())
                                    .shadow(radius: 4)
                            }
                        }
                        .padding()
                        .background(Color(.systemGray6))
                        .cornerRadius(16)
                    }

                    Spacer()
                }
                .padding()
            }
            .navigationTitle("Настройки")
            .sheet(isPresented: $showCheckSheet) {
                CheckOrderSheet(viewModel: viewModel)
            }
        }
        .preferredColorScheme(isDarkMode ? .dark : .light)
    }
}
