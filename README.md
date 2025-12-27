
---

# 📘 BookMartMVC – Hướng Dẫn Cài Đặt & Chạy Dự Án

## 1. Giới thiệu

**BookMartMVC** là một hệ thống website thương mại điện tử bán sách trực tuyến, được xây dựng bằng **ASP.NET Core MVC**, sử dụng **Entity Framework Core**, **ASP.NET Identity**, và tích hợp **AI Chatbot (Google Gemini API)** để hỗ trợ người dùng.

Tài liệu này hướng dẫn chi tiết **cách cài đặt và chạy project trên máy local**.

---

## 2. Yêu cầu môi trường

Trước khi chạy project, cần cài đặt:

* **.NET SDK 9.0**

  * Kiểm tra:

    ```bash
    dotnet --version
    ```
* **SQL Server LocalDB** (có sẵn khi cài Visual Studio)
* **Visual Studio 2022** hoặc **VS Code**
* **SQL Server Management Studio (SSMS)** (khuyến nghị để xem database)

---

## 3. Clone source code

```bash
git clone https://github.com/nguyenphutrieu22521534/

```

---

## 4. Tạo file cấu hình `appsettings.json`

👉 Tạo file `appsettings.json` **cùng cấp với `Program.cs`**.

### Nội dung mẫu:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\MSSQLLocalDB;Database=BookMartDb;Trusted_Connection=True;TrustServerCertificate=True;"
  },
  "Gemini": {
    "ApiKey": ""
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}
```

📌 **Lưu ý**:

* Nếu chưa có Gemini API Key, để trống vẫn chạy được (chatbot sẽ không phản hồi).

---

## 5. Restore thư viện

```bash
dotnet restore
```

⚠️ Nếu có warning liên quan đến Razorpay → có thể bỏ qua, không ảnh hưởng chạy project.

---

## 6. Cài Entity Framework CLI

```bash
dotnet tool install --global dotnet-ef --version 9.0.0
```

Kiểm tra:

```bash
dotnet ef --version
```

---

## 7. Tạo database & migration

### 7.1. Tạo migration (trường hợp project chưa có migration)

```bash
dotnet ef migrations add InitialCreate
```

### 7.2. Cập nhật database

```bash
dotnet ef database update
```

---

## 8. Chạy ứng dụng

```bash
dotnet run
```

Khi thấy log:

```text
Now listening on: http://localhost:5100
Application started.
```

👉 Mở trình duyệt và truy cập:

```
http://localhost:5100
```

---

## 9. Tài khoản đăng nhập mặc định

### Admin (đã seed sẵn)

```
Email: admin@gmail.com
Password: Admin@123
```