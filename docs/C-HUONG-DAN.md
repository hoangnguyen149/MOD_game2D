# C — Cài đặt, chạy và đóng gói Windows 11 (v2)

## Phương án và trạng thái

Chỉ có game đã biên dịch nên dùng **phương án B**: prototype độc lập, procedural assets/audio tự tạo, mock server local. Có source thực tế cho client Unity, launcher Windows Forms, server và test harness. Chưa có Unity Editor/Windows trong phiên này để xác nhận toàn bộ MVP bằng giao diện; không gọi client Unity là “đã chạy”.

| Mốc | File chính | Bằng chứng hiện có |
|---|---|---|
| 1. Đầu vào | báo cáo A/B, reports/inventory.json | Archive/hash/PE/metadata đã kiểm tra; không chạy binary tham khảo |
| 2. Kiến trúc | docs/ARCHITECTURE.md, shared/PROTOCOL.md | Tách UI, core, network, config, process supervisor |
| 3. Mock | mock-server/Program.cs | Login, server/character list, positions, validation và lỗi giả lập đã chạy |
| 4. Client | game-client/Assets/Scripts/ | Source login/map/HUD/input/preferences; network service chạy qua ClientProbe, Unity chưa chạy |
| 5. Launcher | launcher/Core, launcher/WinForms | Core đã chạy test; WinForms compile/publish pass, GUI chưa chạy |
| 6. Mod | Core/World.cs, LocalGame.cs | API mod core test pass; menu FPS/theme/keybindings có source, cần kiểm tra Unity |
| 7. Test/package | scripts/, tests/, reports/ | 85 kiểm tra pass; gói Windows có thật, chưa chạy trên Windows |
| 8. Hướng dẫn | tài liệu này | Quy trình Windows sạch và checklist còn lại |

Mã hoàn chỉnh nằm trong các file, không có placeholder implementation. `launcher/cli` là manager dòng lệnh v1 được giữ để tham khảo, không phải launcher v2. `reports/VALIDATION.md` và hai archive không gắn v2 là kết quả v1, không được dùng làm bằng chứng cho bản v2.

## Phiên bản chính thức đã đối chiếu

- **Unity 6.3 LTS**: hỗ trợ tới 12/2027 theo https://unity.com/releases/unity-6/support. Baseline project `6000.3.0f1`, có trang release https://unity.com/releases/editor/whats-new/6000.3.0f1. Dùng patch 6.3 LTS mới nhất phù hợp rồi kiểm thử lại trước phát hành; không tự nâng project 2020 của người dùng.
- **.NET 10 LTS**: hỗ trợ tới 14/11/2028 theo https://dotnet.microsoft.com/en-us/platform/support/policy. Đã cài/build bằng SDK **10.0.403**, pin trong global.json; SDK đã đối chiếu SHA-512 với manifest Microsoft.
- Unity dùng runtime C# riêng; shared source được compile vào Unity, không dùng assembly net10 trong game.

## Máy Windows 11 sạch

Không cần Administrator để chạy game/launcher đã cài dependency. Việc cài SDK/Unity/Visual Studio có thể cần quyền theo chính sách máy.

1. Cài Git nếu muốn clone: `git clone https://github.com/hoangnguyen149/MOD_game2D.git`. Có thể tải source ZIP thay thế.
2. Nếu build từ source: cài .NET 10 SDK từ https://dotnet.microsoft.com/en-us/download/dotnet/10.0; kiểm `dotnet --version` phù hợp global.json. Cài Python 3 để chạy script smoke; kiểm `python --version`.
3. Nếu chỉ chạy **gói v2 self-contained** Server/Launcher, không cần cài .NET riêng. Hai ZIP tương ứng đều phải giải nén giữ nguyên toàn bộ file, không chỉ lấy EXE. Đây là cross-published artifacts, chưa test trên Windows.
4. Nếu build Unity: cài Unity Hub/Editor 6.3 LTS với quyền sử dụng Unity hợp lệ, Windows Build Support (IL2CPP), Visual Studio 2022 và Desktop development with C++/Windows SDK theo https://docs.unity3d.com/6000.3/Documentation/Manual/windows-requirements-and-compatibility.html. Không giả định Linux có thể build Windows IL2CPP.

Không chạy `Mod_Mabu6sao_Tv.exe`/`Auto Manager.exe` tham khảo để thử prototype.

## Khởi động server

Tại root repository, PowerShell:

```powershell
dotnet run --project mock-server
```

Hoặc chạy `Server/MockServer.exe` trong ZIP v2 self-contained. Server nghe **127.0.0.1:5077**, không bind Internet. Từ terminal khác:

```powershell
Invoke-RestMethod http://127.0.0.1:5077/health
```

Mong đợi `status=ok`, `mode=local-mock`, `demoFaults=false`. Không tự mở firewall. Nếu cổng đang dùng, dừng dịch vụ do bạn mở trên cổng đó; test không chiếm/dừng dịch vụ có sẵn. Ctrl-C dừng server, mọi session/position bị mất.

Tài khoản giả lập: `demo`, `demo2`; mật khẩu **public fixture** `local-demo-only`. Server giữ verifier PBKDF2-SHA256, không plaintext password; launcher và cấu hình không lưu password. Đây không phải thiết kế tài khoản production. Không thay bằng tài khoản thật.

## Khởi động và kiểm tra client Unity

1. Mở project `game-client`. Editor sẽ tạo Library/.meta và settings còn thiếu; project chưa được Unity import trong cloud.
2. Giữ **Active Input Handling = Input Manager (Old)** hoặc **Both**; MVP dùng UnityEngine.Input. Chọn **Owned Game → Create Bootstrap Scene**. Lệnh tạo scene nếu chưa có và đưa scene đó vào build settings; không ghi đè scene Bootstrap đã tồn tại.
3. Play. Chọn **Local Meadow** từ cấu hình, nhập demo/password fixture và Login. Mong đợi hiện `Meadow Hero`, HP 100, MP 50 và trạng thái đã kết nối. `demo2` có `Meadow Scout`.
4. Di chuyển WASD; Space dùng MP và phát âm thanh sine tự tạo. Esc mở menu; đổi âm lượng, độ phân giải, FPS, theme HUD và năm phím. Phím phải khác nhau, là A–Z, Space hoặc các tên UpArrow/DownArrow/LeftArrow/RightArrow. Save and apply ghi settings, Resume tiếp tục.
5. Tắt server khi đang chơi: request vị trí/heartbeat báo lỗi trong khoảng timeout, dừng movement và về login. Bật server lại rồi đăng nhập; không cần restart client. Hành vi UI này có source nhưng vẫn cần chạy Unity thực tế để xác nhận.

Shared GameApi dùng HttpClient async, timeout 3 giây, chỉ cho phép URL loopback chính xác, tắt redirect. Đây là đường local, không tắt TLS của tải công cụ hay gọi server ngoài. Unity build đặt HTTP allowed vì mock loopback; ứng dụng vẫn từ chối mọi host ngoài. Nguồn Unity: https://docs.unity3d.com/6000.3/Documentation/ScriptReference/PlayerSettings-insecureHttpOption.html. Thực thi HttpClient/IL2CPP phải được kiểm chứng trong bản build Unity.

Không có âm thanh/ảnh từ game cũ. Map/nhân vật là hình vuông, tone sine tự tạo. Volume được áp dụng qua AudioListener; chưa test nghe/nhìn trong Unity.

## Cấu hình mẫu và lưu dữ liệu

- Server list mặc định: `game-client/Assets/StreamingAssets/servers.json`.
- Bản đóng gói: `Data/servers.json` cạnh EXE ưu tiên hơn StreamingAssets. URL phải `http://127.0.0.1:5077/`; lỗi cấu hình hiển thị ở màn login, cho phép tải lại sau khi sửa. Server ID hiện là meadow.
- Settings: `Application.persistentDataPath/settings.json`, mẫu tại `game-client/settings.example.json`. Windows build dự kiến `%USERPROFILE%/AppData/LocalLow/OwnedLocal/OwnedAdventure`.
- Launcher: `%LOCALAPPDATA%/OwnedAdventure/profiles.json` và launcher.log. Mẫu `launcher/profiles.example.json`, không chứa secret. GUI không tự sao chép mẫu: thêm hồ sơ hoặc tự đặt mẫu vào vị trí tương ứng.
- Config validate kích thước, volume, keys, IDs; file quá lớn bị từ chối. File hỏng không tự ghi đè lúc load. Trong Unity, chỉ thao tác Save rõ ràng mới thay settings hỏng; launcher cần chọn Khôi phục cấu hình, giữ bản `.broken-*`.
- Không ghi password/token vào settings/log; không gửi qua args. Client giữ token trong RAM. Hint tên demo có thể được launcher truyền qua biến môi trường; không có session IPC vì launcher không đăng nhập thay client.

## Launcher Windows Forms

```powershell
dotnet run --project launcher/WinForms
```

Hoặc chạy `Launcher/Launcher.exe` trong gói v2.

1. Chọn EXE **OwnedAdventure.exe do bạn build**. Đường dẫn có dấu cách được truyền bằng ArgumentList, không ghép shell. Windows kiểm extension/PE signature; không xác nhận chữ ký số hay tự biết EXE đáng tin hay không.
2. Nhập tên hồ sơ, chọn demo hoặc demo2, width/height rồi **Thêm**. Sửa/xóa hồ sơ đã dừng; hồ sơ đang chạy bị khóa sửa/xóa.
3. Đặt giới hạn 1–8, lưu rồi mở client. Mỗi hồ sơ chỉ có một tiến trình đang chạy. Giảm giới hạn dưới số đang chạy bị từ chối.
4. Xem trạng thái đang chạy/đã dừng/lỗi và PID. Lỗi thoát khác 0 hiện là lỗi. Đóng client gửi yêu cầu đóng cửa sổ, chờ tối đa 2 giây rồi kill cây tiến trình con nếu cần. Không enumerate/kill theo tên game khác.
5. Đóng launcher sẽ đóng client do nó mở. Log chỉ ghi timestamp + event allowlisted, không ghi nội dung hồ sơ/đường dẫn/credentials. GUI chưa được chạy trên Windows; stop/request-close cần kiểm tra với Unity thực tế.

`OWNED_TEST_USER` chỉ là hint tài khoản công khai demo/demo2; mật khẩu người dùng nhập trong client. `--profile-id`, `--server-id`, `-screen-width`, `-screen-height`, `-screen-fullscreen 0` không chứa secret. Unity settings khi người dùng Apply có thể thay kích thước khởi tạo của launcher.

## Mô phỏng lỗi

Khởi động server bằng:

```powershell
dotnet run --project mock-server -- --DemoFaults=true
```

Hoặc `MockServer.exe --DemoFaults=true`. Chọn các server entry demo trong client:

- login rejected: 401;
- login timeout: server chờ 6 giây, client timeout sau 3 giây;
- disconnect on position: login thành công rồi server abort kết nối khi client gửi position.

Không bật flag thì fault headers không có tác dụng. Test đã chạy trên shared GameApi; UI Unity xử lý các exception tương ứng nhưng chưa kiểm thực tế.

## Mod và debug

`IMod`/`IModHost` có API theme/FPS/HUD title/phím cast. `SampleMod` đăng ký tường minh trong Awake; extension C# mới được thêm source rồi rebuild IL2CPP AOT. Không hỗ trợ tải C# DLL tùy ý lúc chạy. Tùy chỉnh runtime qua menu/settings JSON. Không inject, không chỉnh memory, không vượt auth/anti-cheat.

Editor/Development Build có speed slider, heal, damage và spawn tối đa 50 đối tượng. World debug methods và panel bao bởi `UNITY_EDITOR || DEVELOPMENT_BUILD`; `LOCAL_TESTS` chỉ dùng DebugChecks, không thêm vào Player. Release .NET core đã được reflection test xác nhận không có DebugHeal; binary Unity release chưa build để xác nhận stripping/debug separation.

## Kiểm thử và lỗi thường gặp

Windows (cần Python/.NET SDK):

```powershell
./scripts/check-windows.ps1
```

Nếu chính sách máy không cho chạy PowerShell script, dùng các lệnh dotnet build/run và `python tests/smoke.py` trong script theo chính sách cho phép; không cần tắt bảo vệ hệ thống. Script Windows **chưa chạy trong phiên này**.

Cloud/Linux đã chạy:

```bash
bash scripts/build-dotnet.sh
python tests/smoke.py
```

Kết quả thực tế: **19 core + 4 debug + 24 API/config + 23 launcher + 15 HTTP/log = 85**. Log ở reports/v2-build.log và reports/v2-functional.log. Probe dùng đúng GameApi/DTO source với Unity, nhưng dùng serializer .NET; không coi đó là xác nhận JsonUtility/Unity networking.

Lỗi config hỏng → giữ file, sửa/khôi phục có chủ đích; client path sai → chọn lại EXE; cổng bận → dừng dịch vụ bạn biết; timeout → kiểm health; đăng nhập sai → kiểm tài khoản fixture; session hết hạn → login lại; Unity thiếu Input legacy/module/SDK → Console/Hub; thiếu runtime Windows → dùng self-contained hoặc cài runtime đúng loại.

## Build và đóng gói Windows

```powershell
./scripts/package-windows.ps1 -UnityEditor 'C:/Program Files/Unity/Hub/Editor/6000.3.0f1/Editor/Unity.exe'
# Development riêng, có debug:
./scripts/package-windows.ps1 -UnityEditor 'C:/Program Files/Unity/Hub/Editor/6000.3.0f1/Editor/Unity.exe' -Development
```

Script publish server/launcher self-contained, gọi Unity batchmode, kiểm BuildResult/exit và đủ EXE/UnityPlayer/GameAssembly/_Data rồi nén toàn bộ thư mục. Release và development client dùng output riêng `artifacts/Client`, `artifacts/Client-Development`. Không chỉ copy EXE. Script package tổng chưa chạy vì thiếu Windows/Unity.

`publish-tools.sh` đã cross-publish thành công **Server/MockServer.exe** và **Launcher/Launcher.exe** win-x64, cả self-contained và framework-dependent. Các ZIP v2 self-contained cung cấp riêng, không có client Unity. File framework-dependent chỉ chạy khi máy có .NET 10 ASP.NET Core Runtime và Windows Desktop Runtime thích hợp; không lẫn với ZIP self-contained.

Các archive v1 giữ nguyên theo yêu cầu upload và ghi rõ legacy. Không trộn DLL/runtime giữa v1/v2. Không có installer hay Authenticode signing trong MVP.

## Giới hạn và nghiệm thu còn lại

Đã chứng minh server, shared client network service, process ownership/path-spaces/limit, config validation và log filtering trên Linux. Cần chạy máy Windows/Unity để xác minh: import/compile; login UI; map/input/audio/HUD/menu; invalid settings; connection loss/recovery; WinForms CRUD/UI/closing; CLI args với Unity; development/release build; không có debug ở release; gói tổng có đủ library và chạy trên máy sạch. Không đánh dấu các mục này đã đạt chỉ vì source hoặc cross-build tồn tại.

Chưa có protocol game thật, tài nguyên được phép hoặc source/SDK game gốc; không có multiplayer, đồng bộ HP/MP combat, lưu world hoặc auto-login bằng tài khoản thật. Nếu có Unity 2020 source sau này, cần kiểm tra riêng tương thích trước khi giữ/nâng engine.
