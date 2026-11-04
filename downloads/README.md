# Gói tải

Các ZIP v2 là snapshot trước cập nhật Docker. Để dùng Docker, lấy checkout `main` mới nhất và xem [hướng dẫn Docker](../docs/DOCKER.md); không cần tải lại các gói Windows.

## Cập nhật chơi web trên Docker

[Gói cập nhật web nhỏ](OwnedAdventure-web-update.zip): chép các thư mục `mock-server` và `docker` trong ZIP vào root dự án đang có `compose.yaml`, thay các file prototype tương ứng. Build lại bằng `docker compose up -d --build --wait mock-server`, sau đó mở http://127.0.0.1:5077/ bằng trình duyệt. Không cần Unity Hub cho client web độc lập. [Hướng dẫn web và kiểm thử](../docs/WEB-PLAY.md).

## Các snapshot v2

- [Source + báo cáo v2](OwnedAdventure-v2-source-reports.zip).
- [Launcher Windows Forms v2 — self-contained Windows x64](OwnedAdventure-v2-Windows-Launcher.zip).
- [Mock server v2 — self-contained Windows x64](OwnedAdventure-v2-Windows-Server.zip).

Cross-build/publish thành công; **chưa chạy trên Windows**. Không gói nào có client Unity đã build. Giải nén nguyên thư mục; không trộn runtime giữa các ZIP. Source archive v2 loại toàn bộ downloads để không lồng các ZIP vào nhau.

Launcher: chạy `Launcher/Launcher.exe`. Server: chạy `Server/MockServer.exe`. Tài khoản giả lập công khai và cách build/chạy client trong [hướng dẫn C](../docs/C-HUONG-DAN.md).

## Hai tệp v1 giữ nguyên theo yêu cầu upload

- [Mã nguồn và báo cáo v1 — 1.653.397 byte](OwnedAdventure-source-reports.zip).
- [Server/manager dòng lệnh v1 — 84.285.996 byte](OwnedAdventure-Windows-Tools-only.zip).

Hai tệp trên được sao chép nguyên byte từ các gói đã cung cấp trước đây; manager v1 không phải ứng dụng Windows Forms mới. Không dùng chúng làm bằng chứng nghiệm thu phiên bản v2. Chưa có client Unity trong gói v1.

[SHA-256 và kích thước tất cả ZIP](SHA256.json). Có thể kiểm trên Windows bằng `Get-FileHash <file.zip> -Algorithm SHA256`.
