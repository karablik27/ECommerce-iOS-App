import SwiftUI

@main
struct ECommerceApp: App {
    @AppStorage("isDarkMode") private var isDarkMode = false

    var body: some Scene {
        WindowGroup {
            LaunchView()
                .preferredColorScheme(isDarkMode ? .dark : .light)
        }
    }
}
