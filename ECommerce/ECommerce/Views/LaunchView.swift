import SwiftUI

private enum Constants {
    static let imageSize: CGFloat = 160
    static let cornerRadius: CGFloat = 32
    static let offsetY: CGFloat = -20
    static let fadeInDuration: Double = 1.2
    static let totalDelay: Double = 2.4
}

/// Основной экран загрузки
struct LaunchView: View {
    @StateObject private var viewModel = LaunchViewModel()

    var body: some View {
        ZStack {
            if viewModel.isReady {
                MainView()
                    .transition(.opacity)
            } else {
                VStack {
                    Spacer()

                    Image("loading")
                        .resizable()
                        .aspectRatio(contentMode: .fit)
                        .frame(width: Constants.imageSize, height: Constants.imageSize)
                        .clipShape(RoundedRectangle(cornerRadius: Constants.cornerRadius))
                        .opacity(viewModel.logoOpacity)
                        .offset(y: Constants.offsetY)
                        .animation(.easeInOut(duration: Constants.fadeInDuration), value: viewModel.logoOpacity)

                    Spacer()
                }
                .frame(maxWidth: .infinity, maxHeight: .infinity)
                .background(Color(.systemBackground))
                .ignoresSafeArea()
                .transition(.opacity)
            }
        }
        .onAppear {
            viewModel.startLoading()
        }
        .animation(.easeInOut(duration: 0.6), value: viewModel.isReady) // Плавный fade
    }
}
