# Kết quả thực tế v2

Thực hiện trên Linux x64, .NET SDK 10.0.403. Mã tham khảo EXE/DLL không được chạy. Unity Editor và Windows GUI không được chạy trong phiên này.

| Kiểm tra | Kết quả |
|---|---|
| Build mock-server, launcher core, WinForms, probe và test projects | PASS, 0 warnings/errors |
| CoreChecks | 19/19 PASS |
| DebugChecks | 4/4 PASS |
| API/configuration với mock đang chạy | 24/24 PASS |
| Launcher/storage/process + probe | 23/23 PASS |
| HTTP/auth/validation/log | 15/15 PASS |
| Tổng | 85 kiểm tra PASS |
| Launcher → ClientProbe → mock login/servers/characters/position | PASS trên Linux; probe không phải Unity |
| Simulated timeout/drop/login-error | PASS trên shared GameApi |
| Logs | Không tìm thấy mật khẩu fixture hoặc token đã cấp trong log được test |
| Publish win-x64 self-contained và framework-dependent | PASS server + launcher WinForms |
| Chạy gói Windows, GUI WinForms | CHƯA CHẠY |
| Unity import/compile/Play/player/IL2CPP | CHƯA CHẠY |
| Unity JsonUtility + HttpClient integration | CHƯA CHẠY; .NET wire serializer đã test |
| Client Windows hoàn chỉnh/package tổng | CHƯA BUILD; chỉ có source và script |

Một lần test đầu phát hiện InvalidDataException chưa được UI/test handler bắt đúng kiểu; đã sửa và chạy lại thành công. Không sửa assertion để biến lỗi thành pass. Test giữ file config hỏng, kiểm recovery/backup, unknown password field, null profile, path có khoảng trắng, hai client/giới hạn, đóng đúng child, thoát tự nhiên và exit lỗi.

Các server và tiến trình thử nghiệm đã được dừng bởi test. Không dùng key/tài khoản game gốc. Không có secret trong config mẫu; password public fixture chỉ dùng demo/tests và hướng dẫn. Báo cáo v1 giữ riêng ở VALIDATION.md, không thay bằng chứng cho phiên bản mới.

Log: v2-build.log, v2-functional.log, v2-publish.log. Script packaging Windows và checklist thủ công trong docs/C-HUONG-DAN.md. Việc có ZIP và cross-publish không chứng minh binary đã chạy trên Windows.
