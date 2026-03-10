🎵 Digital Tech Portfolio & Music Player (Full-Stack)
Một hệ thống Hồ sơ năng lực kỹ thuật số tích hợp trình phát nhạc trực tuyến phong cách Spotify và hệ thống quản trị Analytics chuẩn SaaS. Dự án tập trung vào việc xử lý Real-time, Distributed Caching và Code Quality.

🔗 Live Demo: my-portfolio-web-ex66.onrender.com

🚀 Tính năng nổi bật (Key Features)
🎸 Music Player & UI/UX
Spotify-like Player: Trình phát nhạc hỗ trợ Play/Pause, Seek-bar, Shuffle và Repeat.

Real-time Sync (SignalR): Đồng bộ hóa trạng thái phát nhạc tức thì trên đa thiết bị.

Interactive Lyrics: Hiển thị lời bài hát chạy theo thời gian thực (LRC format) và hỗ trợ tua nhạc bằng cách bấm vào lời.

Modern UI: Giao diện Neumorphism & Glassmorphism tương tác cao, hỗ trợ kéo thả (Sortable.js) danh sách phát.

📊 Admin Analytics Dashboard
Traffic Monitoring: Biểu đồ xu hướng truy cập (CV Downloads, QR Scans) trong 7 ngày qua bằng Chart.js.

IP Geolocation: Tự động nhận diện và dịch ngược vị trí địa lý của khách thăm từ địa chỉ IP.

Live CV Generator: Tự động tạo CV định dạng PDF cá nhân hóa kèm mã QR động bằng QuestPDF mà không cần lưu trữ file cứng.

🛡️ Security & Performance
Distributed Caching: Sử dụng Redis (Singapore Cloud) để tối ưu hiệu năng load trang và giảm tải Database.

Google OAuth 2.0: Hệ thống đăng nhập bảo mật qua Google Identity.

Security Hardening: Ngăn chặn các lỗ hổng Path Traversal và RCE khi xử lý tệp tin tĩnh.

🛠 Tech Stack
Backend: ASP.NET Core 8.0 (Razor Pages), Entity Framework Core.

Real-time: SignalR (WebSockets).

Cache: Redis (Distributed Caching).

Database: PostgreSQL (NeonDB).

Frontend: Vanilla JS, Bootstrap 5, Chart.js, Sortable.js.

Quality Control: SonarCloud (Static Analysis), xUnit (Unit Testing).

Infrastructure: Docker, Render Cloud, GitHub Actions.

🏗 Kiến trúc hệ thống (Architecture)
Dự án tuân thủ kiến trúc Three-Tier Architecture đảm bảo tính mở rộng:

Presentation Layer: Razor Pages & JavaScript xử lý tương tác UI.

Business Logic Layer: Xử lý nghiệp vụ, Caching logic (Cache-aside pattern).

Data Access Layer: PostgreSQL & EF Core quản lý dữ liệu bền vững.

🧪 Chất lượng mã nguồn (Code Quality)
Dự án được kiểm soát chặt chẽ bởi SonarCloud:

Clean Code: Tuân thủ quy tắc đặt tên và cấu trúc file của Microsoft.

Cognitive Complexity: Tối ưu hóa các hàm xử lý logic để đảm bảo dễ đọc, dễ bảo trì.

Coverage: Đang triển khai Unit Test bằng xUnit cho các module core.

⚙️ Cài đặt (Installation)
Clone dự án:

Bash
git clone https://github.com/voy32103-code/MyPortfolio.git
Cấu hình appsettings.json hoặc Environment Variables:

ConnectionStrings:DefaultConnection: Link PostgreSQL.

ConnectionStrings:RedisConnection: Link Redis Cloud.

Authentication:Google: ClientID và ClientSecret.

Chạy dự án:

Bash
dotnet watch run
📧 Liên hệ (Contact)
Author: Võ Hưng Yên

Email: voy32103@gmail.com

LinkedIn: [Your LinkedIn Link]

Cảm ơn bạn đã ghé thăm repository này!
