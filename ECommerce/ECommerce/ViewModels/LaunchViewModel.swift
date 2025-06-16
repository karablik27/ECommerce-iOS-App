import Foundation
import SwiftUI

final class LaunchViewModel: ObservableObject {
    @Published var isReady: Bool = false
    @Published var logoOpacity: Double = 0.0

    func startLoading() {
        withAnimation {
            logoOpacity = 1.0
        }

        Task {
            try? await Task.sleep(nanoseconds: 1_000_000_000)
            _ = UserDefaults.standard.stringArray(forKey: StorageKeys.savedAccountUserIds) ?? []
            try? await Task.sleep(nanoseconds: 2_400_000_000)

            await MainActor.run {
                withAnimation {
                    isReady = true
                }
            }
        }
    }
}
