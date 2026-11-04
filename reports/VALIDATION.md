# Kết quả kiểm chứng

Môi trường Linux x64; .NET SDK 10.0.403, ngày 05-11-2026 theo giờ người dùng.

| Kiểm tra | Kết quả |
|---|---|
| RAR5 test/extract bằng 7-Zip 26.05 | PASS, 42 file, 138.840.111 byte; hash từng file |
| libarchive extraction | FAIL: unsupported RAR block; đã thay bằng 7-Zip, không dùng output dở |
| PE/metadata static parse | PASS, game native x86, metadata 27 |
| Il2CppDumper lần đầu redirect stdin | Sinh output xong, exit 134 ở Console.ReadKey; không gọi là thành công toàn tiến trình |
| Il2CppDumper chạy lại trong PTY | PASS, output hoàn tất và exit 0 sau phím thoát |
| Disassembly chọn lọc Capstone | Đọc opcode và direct-call sites; không phải phục hồi CFG đầy đủ |
| Build MockServer Release Linux | PASS, 0 warnings/errors |
| Build Manager Release Linux | PASS, 0 warnings/errors |
| CoreChecks | PASS 19/19: pause, normalize, bounds, HP clamp, MP depletion, NaN, mod API, loại debug API release |
| DebugChecks | PASS 4/4: speed bounds, NaN, restore HP/MP |
| Smoke HTTP và manager | PASS 13/13: health, auth, bad credentials, malformed/null login, world, token reject, path/launch/duplicate/status/stop |
| Publish MockServer win-x64 self-contained | PASS cross-publish, chưa chạy trên Windows |
| Publish Manager win-x64 self-contained | PASS cross-publish, chưa chạy trên Windows |
| Unity Editor compile / Play Mode / build IL2CPP | CHƯA CHẠY: Unity Editor không có trong PATH; chưa xác minh license/modules Windows |
| Unity client–mock integration | CHƯA CHẠY; HTTP server kiểm riêng đã pass |
| Windows GUI/client/package tổng | CHƯA CHẠY; script và source đã cung cấp |
| Chạy EXE/DLL game tham khảo | KHÔNG THỰC HIỆN theo yêu cầu phân tích tĩnh |

Không có test bị skip để làm pass suite; 36 kiểm tra đã liệt kê là kiểm tra tự tạo cho MVP. Không thay chúng cho kiểm chứng Unity. Mọi server và fixture do test mở đã được dừng. Không có truy cập máy chủ game gốc, không dùng key/tài khoản trong archive. Không thay đổi bản game đầu vào.
