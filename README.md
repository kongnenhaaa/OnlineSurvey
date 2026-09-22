# OnlineSurvey

Ứng dụng ASP.NET Core MVC .NET 8 cho đề tài “Hệ thống Tạo và Quản lý Khảo sát Trực tuyến”.

## Công nghệ

- ASP.NET Core MVC / C#
- MongoDB: lưu cấu trúc khảo sát và câu trả lời
- Redis: cache khảo sát đã xuất bản và giới hạn 3 lượt gửi/phút cho mỗi client
- Bootstrap

## Chạy local

1. Khởi động MongoDB và Redis (đã cài portable trong tài khoản Windows hiện tại):

   ```powershell
   .\start-dependencies.ps1
   ```

2. Chạy ứng dụng:

   ```powershell
   cd .\OnlineSurvey
   dotnet run
   ```

3. Tài khoản quản trị được lưu trong database MongoDB `OnlineSurvey`, collection
   `admins`. Khi database chưa có tài khoản, chạy lần đầu bằng PowerShell:

   ```powershell
   $env:ONLINE_SURVEY_ADMIN_USERNAME="admin"
   $env:ONLINE_SURVEY_ADMIN_PASSWORD="Admin@12345"
   dotnet run
   ```

4. Mở URL được hiển thị trong terminal. Đăng nhập quản trị bằng:

   ```text
   Username: admin
   Password: Admin@12345
   ```

Biến môi trường chỉ được dùng để tạo tài khoản đầu tiên. Những lần chạy sau,
ứng dụng xác thực trực tiếp với database và không lưu mật khẩu chữ thường trong
`appsettings.json`.

## Chức năng

- Tạo, sửa, xóa khảo sát
- Thêm câu hỏi trả lời ngắn, trả lời dài, chọn một, chọn nhiều
- Xuất bản/đóng khảo sát
- Mở form công khai theo `/surveys/{slug}`
- Kiểm tra câu hỏi bắt buộc và đáp án hợp lệ ở server
- Lưu câu trả lời vào MongoDB
- Cache form bằng Redis
- Chống spam bằng Redis counter
- Xem thống kê và câu trả lời mẫu

## Cấu trúc dữ liệu

- `surveys`: một document chứa thông tin khảo sát, `createdByAdminId` và danh sách câu hỏi động
- `responses`: một document cho mỗi lần gửi, có `surveyVersion` để biết phiên bản form
- `admins`: tài khoản quản trị và mật khẩu đã băm

Quan hệ dữ liệu:

- Một `admins` có thể tạo nhiều `surveys` qua `surveys.createdByAdminId`.
- Một `surveys` có nhiều `responses` qua `responses.surveyId`.
- `admins`: tài khoản quản trị và mật khẩu đã băm
