# Test Plan — ClinicMS (SQL Server Migration)

## Phase 1: Kiểm tra cơ bản (Build & Migration)

```powershell
dotnet build                                    # 0 lỗi, 0 warning
dotnet ef database update                       # Apply migration thành công
dotnet run                                      # App mở được, không crash
```

**Verify:**
- File log ở `%LocalAppData%\ClinicMS\logs\` không có lỗi
- Database `ClinicMS` trên `localhost\SQLEXPRESS` có 14 bảng
- Bảng `medicines` có 5 dòng seed data

---

## Phase 2: Unit Tests (tự động)

```powershell
dotnet test ClinicMS.Tests
```

**Test coverage (~28 tests):**

| Test Class | Số test | Mục đích |
|-----------|---------|----------|
| `PatientServiceTests` | 6 | CRUD patient + search + pagination |
| `DoctorServiceTests` | 5 | CRUD doctor + search + lookup |
| `MedicineServiceTests` | 2 | CRUD + stock tracking |
| `ServiceCatalogServiceTests` | 1 | CRUD service |
| `VisitServiceTests` | 6 | Visit + prescription + invoice + date filter |
| `InvoiceServiceTests` | 4 | Generate invoice + payment + stock deduction |
| `DashboardServiceTests` | 2 | Appointments + visit stats |
| `DatabaseServiceTests` | 2 | Legacy DB service CRUD |
| `FullFlowTests` | 1 | End-to-end: patient → payment |

**Kỳ vọng:** 100% pass.

---

## Phase 3: Manual Test — CRUD từng module

### 3a. Patients

| Action | Thao tác | Kết quả mong đợi |
|--------|----------|------------------|
| Add | Nhập tên, SĐT, email, địa chỉ | Patient hiện trong danh sách |
| Search | Gõ tên vào ô search | Filter đúng |
| Search | Gõ SĐT | Tìm theo SĐT |
| Edit | Sửa SĐT | Lưu thành công |
| Delete | Xóa patient + visits | Không lỗi FK |

### 3b. Doctors

| Action | Thao tác | Kết quả mong đợi |
|--------|----------|------------------|
| Add | Nhập tên, chuyên khoa | Hiển thị trong danh sách |
| Edit | Sửa chuyên khoa | Lưu thành công |
| Search | Tìm theo tên/chuyên khoa | Kết quả đúng |

### 3c. Visits + Prescription

| Action | Thao tác | Kết quả mong đợi |
|--------|----------|------------------|
| Create visit | Chọn patient, doctor, nhập triệu chứng | Visit tạo thành công |
| Add prescription | Chọn thuốc, nhập số lượng/dosage | Reserved quantity tăng |
| Complete visit | Chuyển status → Completed | Invoice tự động tạo |

### 3d. Invoice + Payment

| Action | Thao tác | Kết quả mong đợi |
|--------|----------|------------------|
| View invoice | Mở invoice từ visit | Hiển thị đúng tổng tiền |
| Pay | Nhập số tiền = tổng | Status → Paid, stock giảm |
| Partial pay | Nhập tiền < tổng | Status → Partial |
| Multiple payments | Thanh toán 2 lần | Stock chỉ giảm 1 lần |

### 3e. Dashboard

| Action | Thao tác | Kết quả mong đợi |
|--------|----------|------------------|
| Load | Mở màn hình chính | Thống kê đúng số lượng |
| Appointments | Thêm lịch hẹn mới | Hiển thị trong upcoming |
| Visit chart | Xem biểu đồ | Hiển thị data 7 ngày gần nhất |

---

## Phase 4: Edge Cases

| Scenario | Steps | Expected |
|----------|-------|----------|
| Empty DB | Xóa DB, chạy app | Migration + seed tự động |
| Long names | Tên > 100 ký tự | Lưu được |
| Special chars | Tên có `'`, `"`, `<>` | XSS-safe (EF Core dùng parameter) |
| Double click Save | Click Save nhanh 2 lần | Không duplicate |
| Delete patient with visits | Patient có 10 visits | Tất cả visits + invoices + payments bị xóa |
| Zero stock medicine | Prescribe thuốc hết hàng | Reserved quantity = 0 (không âm) |

---

## Phase 5: Database Schema Verification

Chạy script sau (dùng SSMS, Azure Data Studio, hoặc `sqlcmd`):

```sql
USE ClinicMS;

-- 1. Kiểm tra tất cả bảng
SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_TYPE = 'BASE TABLE' 
ORDER BY TABLE_NAME;
-- Expected: 14 tables

-- 2. Kiểm tra khóa ngoại
SELECT 
    fk.name AS FK_Name,
    OBJECT_NAME(fk.parent_object_id) AS TableName,
    COL_NAME(fkc.parent_object_id, fkc.parent_column_id) AS ColumnName,
    OBJECT_NAME(fk.referenced_object_id) AS RefTableName,
    COL_NAME(fkc.referenced_object_id, fkc.referenced_column_id) AS RefColumnName
FROM sys.foreign_keys fk
JOIN sys.foreign_key_columns fkc ON fk.object_id = fkc.constraint_object_id;

-- 3. Kiểm tra seed data
SELECT MedicineId, MedicineName, StockQuantity, Price FROM medicines;
-- Expected: 5 rows (Paracetamol 500mg, Amoxicillin 500mg, ...)

-- 4. Kiểm tra identity columns
SELECT 
    OBJECT_NAME(object_id) AS TableName,
    name AS ColumnName,
    is_identity
FROM sys.columns
WHERE is_identity = 1;
-- Expected: patient_id, doctor_id, medicine_id, visit_id, appointment_id, 
--            service_id, payment_id, service_order_id, contact_id
```

---

## Phase 6: Performance Test

```sql
-- Chèn 1000 bệnh nhân mẫu
DECLARE @i INT = 1;
WHILE @i <= 1000
BEGIN
    INSERT INTO patients (full_name, gender, created_at)
    VALUES (CONCAT('Test Patient ', @i), 
            CASE WHEN @i % 2 = 0 THEN 'Nam' ELSE 'Nữ' END,
            FORMAT(GETDATE(), 'yyyy-MM-dd HH:mm:ss'));
    SET @i = @i + 1;
END
```

**Kiểm tra:**
- Màn hình Patient list load trong < 2s
- Search không bị lag
- Pagination hoạt động

---

## Phase 7: Rollback Plan (nếu lỗi)

```powershell
# Nếu app crash ngay khi start:
# 1. Check log ở %LocalAppData%\ClinicMS\logs\
# 2. Fix connection string trong appsettings.json
# 3. Nếu cần chạy lại migration:
dotnet ef migrations remove  # Rollback migration
dotnet ef database update     # Drop database
# Sửa code → chạy lại migration
dotnet ef migrations add InitialCreate
dotnet ef database update
```

---

## Checklist

- [ ] `dotnet build` — 0 error
- [ ] `dotnet ef database update` — Done
- [ ] `dotnet run` — app mở được
- [ ] `dotnet test` — 100% pass
- [ ] CRUD Patient
- [ ] CRUD Doctor
- [ ] Create Visit + Prescription
- [ ] Invoice + Payment
- [ ] Dashboard hiển thị đúng
- [ ] Search hoạt động
- [ ] Xóa cascade không lỗi
