# Owned Adventure — Unity 2D + Windows launcher + local mock

Prototype độc lập theo phương án B: không có project gốc, không kết nối máy chủ game thật, không dùng tài nguyên hay tài khoản của `mabu6sao.rar`.

- [A — Phân tích và bằng chứng](docs/A-PHAN-TICH.md)
- [B — Giải thích Unity/IL2CPP](docs/B-UNITY-IL2CPP.md)
- [C — Cài đặt, chạy, kiểm thử và đóng gói Windows 11](docs/C-HUONG-DAN.md)
- [Kiến trúc](docs/ARCHITECTURE.md) · [Giao thức mock](shared/PROTOCOL.md)
- [Kết quả kiểm thử v2](reports/VALIDATION-V2.md)
- [Các gói tải và checksum](downloads/README.md)

**Đã chạy:** 85 kiểm tra trên Linux, có luồng launcher core → client kiểm thử dùng chung lớp mạng với Unity → mock server. WinForms và các gói Windows x64 đã cross-build/publish thành công. **Chưa chạy:** Unity Editor/player, GUI WinForms và binary trên Windows. ClientProbe là test harness .NET, không phải client Unity.

## Cấu trúc

```text
game-client/     Unity 6.3 LTS: login, map, movement, HUD, settings, registered mod API
launcher/        Windows Forms + core profiles/storage/process supervision
mock-server/     .NET 10 loopback mock: login, servers, characters, position, demo faults
shared/          linked core/network project and protocol documentation
tests/           core, debug, API/config, launcher, end-to-end probe
scripts/         build, check, publish, Windows package
docs/            reports and run guides
reports/         static evidence and actual verification logs
downloads/       requested v1 archives and separately labelled v2 artifacts
```

## Chạy từ mã nguồn

Cài .NET 10 SDK (project pin 10.0.403). Từ repository:

```powershell
dotnet run --project mock-server
# Terminal khác:
dotnet run --project launcher/WinForms
```

Mở `game-client` bằng Unity 6.3 LTS, chọn **Owned Game → Create Bootstrap Scene**, Play, đăng nhập tài khoản giả lập `demo` hoặc `demo2` với mật khẩu thử nghiệm công khai `local-demo-only`. Mật khẩu nhập trong game, không lưu trong launcher/config. Chọn EXE client do bạn build ở launcher; không chọn binary game tham khảo.

Kiểm thử Windows (cần Python 3 và .NET SDK): `./scripts/check-windows.ps1`. Cloud: `bash scripts/build-dotnet.sh`, sau đó `python tests/smoke.py`. Xem hướng dẫn C cho cài đặt sạch, demo lỗi và build Unity IL2CPP.

Unity 6.3 LTS còn hỗ trợ tới 12/2027; .NET 10 LTS tới 14/11/2028, theo tài liệu chính thức đã kiểm tra. Không tự nâng project Unity 2020 nếu được cung cấp sau này: cần đánh giá riêng.

## Docker (mock server + client kiểm thử)

Cài Docker Desktop, dùng Linux containers/WSL2. Từ root checkout mới nhất:

```powershell
docker compose up -d --build --wait mock-server
docker compose --profile check run --rm --build client-probe
```

Để chơi có giao diện, mở `game-client` bằng Unity Editor trên Windows, kết nối Local Meadow tại `127.0.0.1:5077`. Container probe không phải game Unity có đồ họa. Dừng server bằng `docker compose down`. [Hướng dẫn Docker đầy đủ](docs/DOCKER.md), [kết quả Docker thực tế](reports/DOCKER-VALIDATION.md). Các ZIP cũ trong downloads là snapshot trước khi thêm Docker; dùng Git checkout mới nhất cho workflow này.
