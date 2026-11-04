# Chơi prototype bằng trình duyệt và Docker

Client web được viết riêng bằng HTML/CSS/JavaScript Canvas, dùng cùng mock server với prototype Unity. Đây là đường chơi độc lập, không yêu cầu Unity Hub, không phải bản Unity/WebGL hoặc game tham khảo được phục hồi. Hình ảnh/âm thanh của prototype được tạo bằng mã, không lấy từ RAR.

## Lấy bản cập nhật

Nếu dùng Git checkout, cập nhật `main`. Nếu dùng ZIP đã giải nén như thư mục `E:\cc\MOD_game2D-main\MOD_game2D-main`, tải [gói cập nhật web](../downloads/OwnedAdventure-web-update.zip), sao chép các thư mục `mock-server` và `docker` trong ZIP vào thư mục dự án đang có `compose.yaml`, thay các file prototype khi được hỏi. Các gói v1/v2 cũ là snapshot trước client web.

## Chạy

Giữ Docker Desktop chạy Linux containers. Mở PowerShell tại thư mục có `compose.yaml`:

```powershell
docker compose up -d --build --wait mock-server
Start-Process 'http://127.0.0.1:5077/'
```

Mở bằng Chrome hoặc Edge. Nhập tài khoản giả lập `demo` hoặc `demo2`, mật khẩu công khai dành cho demo `local-demo-only`. Token chỉ ở RAM trong tab hiện tại; không có lưu mật khẩu/token vào localStorage/sessionStorage hay URL.

Cổng 5077 phục vụ cả trang game và API cùng origin. Không cần đổi compose hoặc mở cổng mới. Client Unity và launcher hiện có vẫn là các đường chạy riêng.

## Điều khiển

Xem chỉ dẫn trong trang game: di chuyển WASD/phím mũi tên, Space dùng kỹ năng, Esc tạm dừng. HUD hiển thị tên nhân vật, HP/MP, kết nối; menu cho thay theme/FPS/âm lượng. HP/MP và hiệu ứng kỹ năng thuộc mô phỏng client local; mock server chỉ quản lý session và vị trí tối thiểu, không phải game multiplayer/server-authoritative.

## Dừng / lỗi

`docker compose stop mock-server` dừng server; `docker compose up -d --wait mock-server` bật lại. Sau khi mất kết nối, khởi động lại server và đăng nhập lại trong trang. `docker compose down` kết thúc phiên thử. Session/vị trí mất khi server restart.

Nếu thấy 404 hoặc trang cũ, kiểm tra đã copy đúng vào root có `compose.yaml`, build lại và nhấn Ctrl+F5. Nếu build lỗi hoặc container không healthy, xem `docker compose logs --tail 100 mock-server`. Không dùng tài khoản game thật.

## Kiểm chứng

Đã chạy Chromium 155 thật trên Linux, kết nối tới server Docker: 33 kiểm tra browser pass (login đúng/sai, nhân vật/HP/MP, keyboard movement và cập nhật server, cast, pause/resume, FPS/theme/volume, boundary, responsive, mất kết nối/relogin, logout/token revocation, không ghi session vào browser storage, không log secret). Thêm 14 kiểm tra Docker API/port/quyền/log pass sau cập nhật. Browser disconnect được mô phỏng bằng việc chặn request position trong browser, không phải tắt server ở bài test này. Không có lỗi JavaScript chưa xử lý. [Ảnh game đã chạy](media/web-play.png), [log browser](../reports/browser-smoke.log), [báo cáo](../reports/WEB-VALIDATION.md). Windows/Cốc Cốc/Edge chưa được chạy trực tiếp trong môi trường này.


## Chạy lại browser tests (tùy chọn cho phát triển)

Cần Python và Playwright, server đang chạy Docker:

```powershell
python -m pip install -r tests/requirements-browser.txt
python -m playwright install chromium
python tests/browser-smoke.py
```

Có thể đặt CHROMIUM_PATH tới Chrome/Chromium có sẵn. Bài test tự mở/đóng browser, không tự dừng container. Người chơi không cần cài các công cụ kiểm thử này.
