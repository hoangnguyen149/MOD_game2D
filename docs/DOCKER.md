# Chạy thử với Docker trên Windows 11

Docker chạy **mock server và phục vụ client web có thể chơi trong trình duyệt** tại `http://127.0.0.1:5077/`. Đây là client Canvas viết riêng, không cần Unity Hub, không phải Unity WebGL. Xem [hướng dẫn chơi web](WEB-PLAY.md). Client Unity và launcher Windows Forms vẫn có thể chạy riêng trên Windows. Chưa có client Unity đã build thành EXE; container client-probe vẫn là bài kiểm thử không giao diện.

## Chuẩn bị

1. Cài Docker Desktop cho Windows, bật backend WSL 2 và dùng **Linux containers**. Mở Docker Desktop, đợi engine hoạt động.
2. Cài Git để lấy mã mới nhất. Nếu chọn client Unity, cài Unity Hub và Unity **6.3 LTS** cho project `game-client`. Chơi client web bằng Chrome/Edge/Cốc Cốc chỉ cần Docker và trình duyệt.
3. Mở PowerShell tại thư mục repository:

```powershell
git pull origin main
docker version
docker compose version
```

Nếu chưa clone repository:

```powershell
git clone https://github.com/hoangnguyen149/MOD_game2D.git
cd MOD_game2D
```

Build nhiều giai đoạn cài SDK/công cụ .NET bên trong image; máy host không cần .NET SDK chỉ để chạy các container này. Lần đầu cần Internet để tải image Microsoft. Các project Docker hiện không có PackageReference ngoài; restore dùng framework packs trong SDK, không gọi NuGet trong RUN. Các ZIP trong `downloads` là snapshot đã phát hành trước đây, không tự nhận thêm cấu hình Docker; dùng checkout mới nhất qua Git.

## Bật mock server

Tại thư mục chứa `compose.yaml`:

```powershell
docker compose up -d --build --wait mock-server
docker compose ps
Invoke-RestMethod http://127.0.0.1:5077/health
```

Kết quả mong đợi: service có trạng thái `healthy`; health API trả `status = ok`, `mode = local-mock` và `demoFaults = false`. Khi chưa healthy, đọc log:

```powershell
docker compose logs --tail 100 mock-server
```

Compose chỉ publish `127.0.0.1:5077:5077`, nên client trên chính máy Windows dùng cùng địa chỉ `http://127.0.0.1:5077/`. Không cần đổi server URL trong client sang tên service Docker.

Bản native chạy ngoài Docker vẫn bind loopback và kiểm IP loopback. Compose bật rõ `ContainerDemo=true` để server trong container bind interface nội bộ và nhận kết nối qua Docker bridge. Cổng được giới hạn ở loopback của host; không đổi thành `5077:5077` nếu chỉ thử local. Chế độ container không phải cơ chế xác thực hoặc cấu hình production.

## Mở game web

Sau khi server healthy, mở trình duyệt:

```powershell
Start-Process 'http://127.0.0.1:5077/'
```

Đăng nhập demo / local-demo-only. Dùng WASD/mũi tên để di chuyển, Space dùng kỹ năng và Esc tạm dừng. Client web là prototype viết riêng với tài nguyên tự tạo. Nếu trước đây bạn dùng ZIP cũ, lấy gói cập nhật trong hướng dẫn web trước khi build lại.

## Kiểm tra luồng đăng nhập bằng container

```powershell
docker compose --profile check run --rm --build client-probe
```

ClientProbe dùng cùng source GameApi/DTO với client Unity để kiểm tra đăng nhập, danh sách server/nhân vật, cập nhật vị trí và đăng xuất. Kết quả mong đợi:

```text
PASS client probe: login, servers, characters, position (not Unity)
```

Probe chia sẻ network namespace với service `mock-server`, vì vậy địa chỉ `127.0.0.1:5077` vẫn hợp lệ. Không nới allowlist GameApi để gọi host ngoài. `depends_on` chờ healthcheck thành công trước khi chạy probe. Đây là kiểm tra giao thức .NET, chưa chứng minh Unity, đồ họa hoặc GUI Windows chạy được.

## Chơi bằng Unity trên Windows

1. Giữ mock server Docker đang chạy.
2. Trong Unity Hub, Add project từ thư mục `game-client` rồi mở project.
3. Chọn **Owned Game → Create Bootstrap Scene**, sau đó bấm **Play**.
4. Nếu cần, đặt **Project Settings → Player → Active Input Handling** thành **Input Manager (Old)** hoặc **Both**.
5. Chọn **Local Meadow**, nhập tài khoản giả lập `demo` hoặc `demo2`, mật khẩu công khai dành cho demo `local-demo-only`.
6. Di chuyển WASD, dùng Space để thử kỹ năng và Esc để mở cài đặt. Mong đợi HUD hiển thị tên nhân vật, HP/MP và trạng thái kết nối.

Không lưu mật khẩu/token vào Compose, command line hay cấu hình launcher. Server chỉ dùng tài khoản giả lập; không thay bằng tài khoản game thật. Chưa có tích hợp máy chủ game tham khảo.

Nếu bạn đã tự build `OwnedAdventure.exe`, chạy EXE hoặc launcher trên Windows trong khi server Docker hoạt động. Launcher không chạy trong container Linux.

## Thử lỗi mạng

Bật overlay demo:

```powershell
docker compose -f compose.yaml -f compose.demo.yaml up -d --build --wait mock-server
```

Trong client Unity chọn các mục demo login rejected, login timeout hoặc disconnect on position. Chúng chỉ hoạt động khi server bật `DemoFaults=true`. Để quay về chế độ thường:

```powershell
docker compose up -d --wait mock-server
```

Có thể thử mất kết nối thực bằng cách dừng server khi đang chơi, rồi bật lại và đăng nhập lại:

```powershell
docker compose stop mock-server
docker compose up -d --wait mock-server
```

Mọi session và vị trí mẫu nằm trong RAM, mất khi server restart.

## Dừng và xử lý lỗi

```powershell
docker compose down
```

- Cổng 5077 đã dùng: dừng server native/container trước đó do bạn mở. Không chạy đồng thời hai server trên cùng cổng.
- Docker daemon không kết nối: mở Docker Desktop, kiểm tra WSL 2 và Linux containers.
- Build không tải được image/dependency: đọc lỗi registry/proxy/TLS; giữ xác minh chứng chỉ, không dùng `--insecure`.
- Container chưa healthy: xem log; `ContainerDemo` và cấu hình bind phải đúng. Chỉ đổi `ASPNETCORE_URLS` không đủ cho server vốn bind loopback và kiểm IP nguồn.
- Probe không gọi được server: dùng lệnh Compose đã cung cấp, không tách probe sang network khác mà vẫn dùng localhost.
- Unity báo lỗi đỏ: kiểm Console; phần UI/runtime Unity cần kiểm chứng bằng Unity Editor, không được thay bằng kết quả probe.

## Kiểm tra bổ sung (tùy chọn)

Khi server Compose đang chạy, nếu máy có Python 3:

```powershell
python tests/docker-host-checks.py
# Chỉ dùng sau khi bật overlay DemoFaults:
python tests/docker-host-checks.py --faults
```

Script chỉ kiểm tra service đã chạy, không tự khởi động hay dừng container. Chế độ thường mong đợi 14 checks; chế độ faults mong đợi 8 checks.

## Trạng thái kiểm chứng

Đã build và chạy thực tế trên Docker Engine Linux 29.9.2 / Compose 2.40.3: server healthy, ClientProbe login/servers/characters/position/logout thành công; 14 kiểm tra host API/quyền/port/log và 8 kiểm tra lỗi demo (22/22) pass. Sau thay đổi đã chạy lại 62 kiểm tra native API/config/launcher/HTTP/log và đều pass. Tất cả container/network do phiên test tạo đã được dọn; image giữ lại làm cache. Windows Docker Desktop và Unity GUI chưa được chạy trong môi trường này; chúng cần kiểm chứng trên máy của bạn. Xem [báo cáo Docker](../reports/DOCKER-VALIDATION.md).

Docker Desktop Linux containers không trực tiếp chạy EXE Unity Windows hoặc WinForms. Windows containers không cung cấp desktop tương tác cho ứng dụng cần GUI. Client web mới là HTML Canvas độc lập; chưa có WebGL build từ Unity, VNC hay lớp tương thích Wine trong project này.

Tài liệu chính thức:

- https://docs.docker.com/desktop/setup/install/windows-install/
- https://docs.docker.com/compose/how-tos/networking/
- https://docs.docker.com/engine/network/port-publishing/
- https://learn.microsoft.com/en-us/virtualization/windowscontainers/quick-start/lift-shift-to-containers
- https://learn.microsoft.com/en-us/dotnet/core/docker/build-container
