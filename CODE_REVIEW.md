# ClinicMS - Code Review

Ngày review: 07/03/2026

## 📋 Tóm Tắt
**ClinicMS** là một ứng dụng WPF .NET 8 quản lý phòng khám với kiến trúc MVVM, sử dụng EF Core + SQLite. Hầu hết codebase được cấu trúc tốt, nhưng có một số vấn đề cần xử lý.

---

## ✅ Điểm Mạnh

### 1. **Kiến Trúc MVVM Sạch**
- BaseViewModel triển khai INotifyPropertyChanged đúng cách
- RelayCommand implementation hoàn chỉnh với CallerMemberName
- Tách biệt rõ ràng giữa View, ViewModel, Model

### 2. **Quản Lý Database Tốt**
- EF Core DbContext được cấu hình hợp lý
- Các mối quan hệ FK định nghĩa rõ ràng trong OnModelCreating
- Hỗ trợ cả offline (local AppData) + portable (cùng .exe)
- Sử dụng composite keys phù hợp (PatientContact, InvoiceItem, PrescriptionDetail)

### 3. **Xử Lý Ngoại Lệ**
- App.xaml.cs bắt DispatcherUnhandledException toàn cục
- Thông báo lỗi user-friendly
- Try-catch trong các phương thức CRUD

### 4. **Model Design**
- Sử dụng Data Annotations + Database Generated Options đúng
- Display properties (NotMapped) thông minh (Initials, DisplayId, etc.)
- Các getter thứ cấp tiện lợi (PhoneDisplay, AddressDisplay, v.v.)

### 5. **UI/UX**
- Toast notifications dùng DispatcherTimer 3 giây
- Loading states để feedback người dùng
- Multiple view support (Patients, Doctors, Visits, Medicines, Invoices)

---

## ⚠️ Vấn Đề Cần Khắc Phục

### 1. **CRITICAL: WPF Trigger Error (error.txt)**
```
'Text' property cannot be set from a property trigger and appear in the trigger's condition at the same time.
```
**Nguyên nhân:** Một số XAML views có trigger trên TextBlock/DataGrid column binding
**Giải pháp:**
- Audit tất cả .xaml files, tìm trigger và binding conflict
- Dùng Value Converter thay vì Trigger
- Ví dụ thay vì:
  ```xml
  <Trigger Property="Text" Value="...">
    <Setter Property="Text" Value="..." />
  </Trigger>
  ```
  Dùng Converter hoặc Attached Behavior

### 2. **Memory Leaks - DatabaseService không dispose DbContext**
```csharp
// ❌ Hiện tại
private static ClinicDbContext Ctx() => new();
public async Task<List<Patient>> GetPatientsAsync()
{
    await using var db = Ctx();  // OK, nhưng...
}
```
**Vấn đề:** 
- Mỗi lệnh gọi tạo DbContext mới → overhead
- Không cache connection string
- Nên dùng singleton hoặc pooling

**Khuyến nghị:**
```csharp
// Dùng dependency injection + singleton
services.AddDbContext<ClinicDbContext>(options =>
    options.UseSqlite(...), ServiceLifetime.Singleton
);
```

### 3. **Error Handling Quá Tối Giản**
```csharp
public async Task LoadPatientsAsync()
{
    try { ... }
    catch { }  // ❌ Swallow tất cả exception
    finally { LoadingPatients = false; }
}
```
**Vấn đề:**
- Không log lỗi
- Người dùng không biết tại sao load thất bại
- Khó debug

**Khuyến nghị:**
```csharp
catch (Exception ex) 
{ 
    Debug.WriteLine($"Error loading patients: {ex.Message}");
    Toast.ShowError("Không thể tải danh sách bệnh nhân");
}
```

### 4. **DatabaseService Magic Strings**
```csharp
// Nhiều string literals
v.VisitType == "Walk-in"
v.Status == "Waiting"
Gender == "Male"
```

**Vấn đề:**
- Dính typo → lỗi runtime
- Khó refactor

**Khuyến nghị:** Dùng Enum
```csharp
public enum VisitType { WalkIn, Appointment, Emergency, FollowUp }
public enum VisitStatus { Waiting, InProgress, Completed }
```

### 5. **Soft Deletes Không Tồn Tại**
```csharp
public async Task DeletePatientAsync(int id)
{
    // Xóa cứng, mất dữ liệu vĩnh viễn
    db.Patients.Remove(p);
}
```

**Vấn đề:**
- Y tế → cần audit trail, không xóa dữ liệu
- Khi có invoices/payments → không thể xóa

**Khuyến nghị:** Soft delete
```csharp
public DateTime? DeletedAt { get; set; }
// Query
db.Patients.Where(p => p.DeletedAt == null)
```

### 6. **MainViewModel Là God Object**
```csharp
public class MainViewModel : BaseViewModel
{
    // 50+ properties
    // 100+ methods (CRUD cho Patient, Doctor, Visit, Medicine, Invoice)
    private readonly DatabaseService _db = new();
    public ToastService Toast { get; } = new();
}
```

**Vấn đề:**
- Quá lớn, khó maintain
- Mỗi model (Patient, Doctor, Visit) nên có ViewModel riêng

**Khuyến nghị:**
```csharp
// Cấu trúc mới
MainViewModel (chỉ điều hướng)
├── PatientListViewModel
├── DoctorListViewModel  
├── VisitListViewModel
└── DashboardViewModel
```

### 7. **View Code-Behind Quá Nhiều**
```csharp
// PatientListView.xaml.cs ~ 60 lines
private string _search = "";
private void ApplyFilter() { ... }
private async void RefreshButton_Click() { ... }
```

**Vấn đề:**
- Logic UI nên bind XAML
- Code-behind khó test

**Khuyến nghị:**
```csharp
// Dùng IValueConverter thay vì code-behind filter
public class PatientFilterConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        // Implement filter logic
    }
}
```

### 8. **Contact Normalization Issue**
```csharp
// Patient có Phone, Email, Address (stored as strings)
// Cũng có PatientContact → Phone, Email, Address lưu ở Contact table
// Dữ liệu duplicated, khó đồng bộ
```

**Vấn đề:**
- Mâu thuẫn source of truth
- Update clumsily

**Khuyến nghị:**
- XÓA Phone, Email, Address khỏi Patient model
- LỖI xử lý Contacts qua PatientContacts join

### 9. **Không Có Validation**
```csharp
public class Patient
{
    public string FullName { get; set; } = "";  // Có thể null?
    public string Gender { get; set; }  // Enum "Male", "Female" hay free text?
}
```

**Vấn đề:**
- GIGO (Garbage In, Garbage Out)
- DB sẽ có dữ liệu bẩn

**Khuyến nghị:**
```csharp
[Required(ErrorMessage = "Họ tên bắt buộc")]
[StringLength(255)]
public string FullName { get; set; } = "";

[RegularExpression(@"^(Male|Female|Other)$")]
public string Gender { get; set; }
```

### 10. **Threading Issue - ToastService**
```csharp
public void Show(string message, ToastType type = ToastType.Success)
{
    var toast = new ToastItem { Message = message, Type = type };
    App.Current.Dispatcher.Invoke(() => Toasts.Add(toast));
    // DispatcherTimer có thể gặp race condition
}
```

**Vấn đề:**
- Nếu xóa toast khi vẫn update → crash
- Thread-safety không được đảm bảo

**Khuyến nghị:**
```csharp
private readonly object _lock = new object();
public void Show(string message, ToastType type)
{
    lock (_lock)
    {
        var toast = new ToastItem { Message = message, Type = type };
        App.Current.Dispatcher.InvokeAsync(() => Toasts.Add(toast));
    }
}
```

### 11. **Không Có Business Logic Validation**
```csharp
// Có thể tạo Visit với PatientId = 0?
// Có thể tạo Invoice mà Patient không tồn tại?
// Không có constraint tại app level
```

**Khuyến nghị:**
- Add validation ở ViewModel trước SaveChanges
- Add NOT NULL FK constraints ở database

### 12. **DateTime String Format Không Consistent**
```csharp
VisitTime?.StartsWith(today)  // "yyyy-MM-dd"
CreatedAt: string?  // Format gì?
DateOfBirth: string?  // Format gì?
```

**Vấn đề:**
- Dễ xảy ra lỗi format
- Parse string thay vì DateTime object

**Khuyến nghị:**
```csharp
public DateTime? VisitTime { get; set; }  // Dùng DateTime, không phải string
public DateTime CreatedAt { get; set; } = DateTime.Now;
```

---

## 🔧 Khuyến Nghị Cụ Thể

### **Ưu Tiên Cao (Must Fix)**
1. ✅ **Giải quyết WPF Trigger error** → Audit .xaml files
2. ✅ **Convert DateTime strings → DateTime objects**
3. ✅ **Thêm Data Validation** (Annotations)
4. ✅ **Khắc phục Soft Delete** (thêm IsDeleted / DeletedAt)

### **Ưu Tiên Trung (Should Fix)**
5. ✅ **Refactor MainViewModel → child ViewModels**
6. ✅ **Add proper error logging** (Serilog/NLog)
7. ✅ **Dùng DI Container** (Microsoft.Extensions.DependencyInjection)
8. ✅ **Dùng Enum thay String literals**

### **Ưu Tiên Thấp (Nice to Have)**
9. ✅ **Extract View code-behind → Converters/Behaviors**
10. ✅ **Unit Tests** (xUnit + Moq)
11. ✅ **Normalization: Xóa Contact fields khỏi Patient**
12. ✅ **Thread-safe Collections** (ConcurrentObservableCollection)

---

## 📊 Code Quality Metrics

| Tiêu Chí | Mức Độ | Ghi Chú |
|---------|--------|--------|
| Architecture | ⭐⭐⭐⭐ | MVVM tốt |
| Database Design | ⭐⭐⭐⭐ | EF Core chuẩn |
| Error Handling | ⭐⭐ | Quá tối giản |
| Code Reusability | ⭐⭐⭐ | MainViewModel quá lớn |
| Testing | ⭐ | Không có unit test |
| Data Validation | ⭐ | Thiếu |
| UI/UX | ⭐⭐⭐ | Tốt, nhưng có bug WPF |
| Documentation | ⭐ | Không có comments |
| **Overall** | **⭐⭐⭐** | **Tốt, cần cải thiện** |

---

## 📝 Implementation Roadmap

```
Phase 1 (P0 - Critical)
├── Fix WPF Trigger error
├── Migrate DateTime strings → DateTime
├── Add Data Validation
└── Implement Soft Delete

Phase 2 (P1 - Important)
├── Refactor MainViewModel
├── Add DI Container
├── Add Logging (Serilog)
└── Use Enums

Phase 3 (P2 - Nice to Have)
├── Unit Tests
├── Extract Code-Behind
├── Normalization
└── Performance Optimization
```

---

## 🎯 Conclusion

**ClinicMS** được xây dựng với kiến trúc MVVM vững chắc. Codebase chủ yếu sạch và dễ hiểu, nhưng cần xử lý một số vấn đề quan trọng:
- ⚠️ **WPF trigger error** ngăn ứng dụng chạy
- 🐛 **Data validation** cần được bổ sung
- 🔄 **Architecture** có thể refactor thêm
- 📝 **Error handling** cần improve

Với những cải thiện này, dự án sẽ trở thành ứng dụng quản lý phòng khám hoàn chỉnh, robust và dễ maintain.

