import SwiftUI

struct CustomTabBar: View {
    @Binding var selectedTab: MainView.Tab

    var body: some View {
        HStack(spacing: 0) {
            ForEach(MainView.Tab.allCases, id: \.self) { tab in
                Button {
                    withAnimation(.easeInOut(duration: 0.15)) {
                        selectedTab = tab
                    }
                } label: {
                    VStack(spacing: 4) {
                        Image(systemName: tab.systemImage)
                            .font(.system(size: 20, weight: .semibold))
                        Text(tab.title)
                            .font(.caption)
                    }
                    .foregroundColor(selectedTab == tab ? .green : .gray)
                    .frame(maxWidth: .infinity)
                }
            }
        }
        .padding(.horizontal)
        .padding(.top, 12)
        .padding(.bottom, 24)
        .background(
            RoundedRectangle(cornerRadius: 30)
                .fill(Color(.systemGray6))
                .shadow(color: .black.opacity(0.15), radius: 10, y: -2)
        )
        .padding(.horizontal, 16)
    }
}
