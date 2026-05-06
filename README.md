import os

# Nội dung của file README.md dựa trên đặc tả và quá trình phát triển
readme_content = """# Custom UNO Online - Multiplayer Card Game

Đồ án môn học: Phát triển ứng dụng Game/Ứng dụng Phân tán.
Phiên bản: 1.0.0
Ngày hoàn thành: 07/05/2026

## 📝 Giới thiệu dự án
Dự án là một phiên bản trực tuyến của trò chơi bài UNO cổ điển, được bổ sung các quy tắc tùy chỉnh (Custom Rules) để tăng tính kịch tính. Trò chơi được xây dựng trên nền tảng Unity, sử dụng giải pháp **Netcode for GameObjects (NGO)** để xử lý kết nối đa người chơi theo mô hình Client-Server.

## ✨ Tính năng nổi bật

### 1. Hệ thống Multiplayer
* **Lobby System:** Cho phép tạo phòng và tham gia phòng thông qua danh sách phòng chờ.
* **Đồng bộ hóa thời gian thực:** Trạng thái ván bài, lượt đi và số lượng bài trên tay được đồng bộ hóa tức thì giữa tất cả người chơi.
* **Đồng bộ danh tính:** Hiển thị đúng tên người chơi lấy từ tài khoản hoặc nhập từ Lobby.

### 2. Luật chơi chuẩn (Standard Rules)
* **Xào và chia bài:** Tự động chia 7 lá cho mỗi người chơi khi bắt đầu.
* **Logic đánh bài:** Kiểm tra tính hợp lệ của lá bài dựa trên màu sắc hoặc giá trị của lá bài trên cùng.
* **Lá bài chức năng:** Hỗ trợ đầy đủ các lá Skip (Cấm), Reverse (Đảo chiều), Draw Two (+2), Wild và Wild Draw Four (+4).
* **Stacking (Cộng dồn):** Cho phép cộng dồn các lá bài phạt (+2 và +4) nếu người chơi tiếp theo có lá bài tương ứng.

### 3. Các quy tắc đặc biệt (Custom Rules - House Rules)
Dự án thực hiện thành công 3 quy tắc nâng cao theo yêu cầu đặc tả (Mục 4):
* **Rule of 0:** Khi đánh lá bài số 0, toàn bộ người chơi sẽ chuyền bài trên tay theo hướng được chọn (Cùng chiều/Ngược chiều kim đồng hồ).
* **Rule of 7:** Khi đánh lá bài số 7, người chơi có quyền chọn một đối thủ bất kỳ để tráo đổi toàn bộ bài trên tay.
* **Rule of 8 (Reaction Event):** Khi đánh lá bài số 8, một nút phản xạ hiện lên trên màn hình tất cả người chơi. Người cuối cùng không bấm kịp sẽ bị phạt rút 2 lá bài.

### 4. Giao diện & Hiệu ứng (UI/UX)
* **Responsive UI:** Giao diện tương thích với nhiều độ phân giải màn hình.
* **VFX & Sound:** * Hiệu ứng Particle System (tia lửa/bụi) khi đánh bài thành công.
    * Âm thanh sống động cho các hành động Rút bài (Draw) và Đánh bài (Play).
    * Chỉ báo lượt đi rõ ràng cho từng người chơi.

## 🛠 Công nghệ sử dụng
* **Game Engine:** Unity 2022.3 LTS (hoặc phiên bản tương đương).
* **Programming Language:** C# (.NET).
* **Networking:** Unity Netcode for GameObjects (NGO).
* **UI Framework:** TextMeshPro (TMP).

## 🚀 Hướng dẫn cài đặt và Chạy thử

### Yêu cầu hệ thống
* Hệ điều hành: Windows/macOS.
* Unity Editor 2022.3+.

### Các bước cài đặt
1.  **Clone repository:** ```bash
    git clone [https://github.com/username/uno-online-multiplayer.git](https://github.com/username/uno-online-multiplayer.git)
    ```
2.  **Mở dự án:** Mở thư mục dự án bằng Unity Hub.
3.  **Cấu hình Network:** Đảm bảo `NetworkManager` trong Scene `MainMenu` đã được thiết lập đúng với Transport của bạn (thường là Unity Transport).
4.  **Build:**
    * Vào `File -> Build Settings`.
    * Chọn nền tảng PC, Mac & Linux Standalone.
    * Bấm `Build` ra một thư mục riêng.

### Cách chạy thử (Testing)
1.  Mở bản Build (Cửa sổ 1): Chọn **Create Lobby** -> Đóng vai trò là **Host**.
2.  Mở bản Build (Cửa sổ 2): Tìm phòng trong danh sách và chọn **Join** -> Đóng vai trò là **Client**.
3.  Host nhấn **Start Game** khi mọi người đã sẵn sàng.

## 📁 Cấu trúc thư mục chính
* `/Assets/Scripts/Core`: Chứa logic trò chơi (`GameManager`, `GameState`).
* `/Assets/Scripts/Networking`: Xử lý giao tiếp mạng (`NetworkGameManager`, `LobbyManager`).
* `/Assets/Scripts/UI`: Quản lý giao diện (`GameUI`, `FeedbackManager`).
* `/Assets/Prefabs`: Chứa các mẫu lá bài và hiệu ứng.

## ⚖️ Giấy phép
Dự án được thực hiện cho mục đích giáo dục.

---
**Người thực hiện:** [Tên của bạn]  
**Mã số sinh viên:** [MSSV của bạn]