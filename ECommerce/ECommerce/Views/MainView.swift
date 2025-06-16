import SwiftUI

struct MainView: View {
    @State private var selectedTab: Tab = .accounts

    enum Tab: Int, CaseIterable {
        case accounts, orders, settings

        var title: String {
            switch self {
            case .accounts: return "Счета"
            case .orders: return "Заказы"
            case .settings: return "Настройки"
            }
        }

        var systemImage: String {
            switch self {
            case .accounts: return "banknote"
            case .orders: return "cart"
            case .settings: return "gear"
            }
        }
    }

    var body: some View {
        ZStack(alignment: .bottom) {
            Group {
                switch selectedTab {
                case .accounts:
                    AccountsView()
                case .orders:
                    OrdersScreenContainer()
                case .settings:
                    SettingsView()
                }
            }
            .frame(maxWidth: .infinity, maxHeight: .infinity)

            CustomTabBar(selectedTab: $selectedTab)
        }
        .ignoresSafeArea(.keyboard)
        .background(Color(.systemBackground))
    }
}


