# ClinicMS - Bug Report and Fix Guide

This report documents the current issues found in the codebase and provides concrete fix steps.
All references use workspace-relative paths.

---

## 1) Double Font Scaling (UI scales twice)

**Severity:** High

**Symptoms**
- Changing font size in Settings (Small/Medium/Large) makes the UI scale too large/small.
- The scale effect is applied twice.

**Root Cause**
- `MainWindow.xaml` applies `LayoutTransform` bound to `FontScale`.
- `FontScaleManager.UpdateApplicationScale(...)` also applies a `LayoutTransform` to the window content.

**Files**
- `MainWindow.xaml`
- `Services/FontScaleManager.cs`

**Fix (choose ONE approach)**

**Option A (Recommended): keep ViewModel binding, remove manager scaling)**
1. In `Services/FontScaleManager.cs`, remove or disable `ApplyScaleToWindow(...)` calls.
2. Keep the `MainWindow.xaml` binding:
   ```xaml
   <Border.LayoutTransform>
       <ScaleTransform ScaleX="{Binding FontScale}" ScaleY="{Binding FontScale}"/>
   </Border.LayoutTransform>
   ```
3. Keep only the resource update if you want new windows to read the value:
   ```csharp
   Application.Current.Resources[FontScaleResourceKey] = scale;
   ```

**Option B: keep manager scaling, remove the XAML transform**
1. Remove the `LayoutTransform` in `MainWindow.xaml`.
2. Keep `FontScaleManager.ApplyScaleToWindow(...)`.

**Why this fixes it**
- Only a single scaling mechanism should be applied.

---

## 2) Visit form auto-suggest list can be empty

**Severity:** High

**Symptoms**
- In `VisitFormDialog`, patient/doctor autocomplete list is empty if the data loads after the dialog opens.
- This can lead to “Không tìm thấy” errors even if the data exists.

**Root Cause**
- `VisitFormDialog` builds `_patientNames` and `_doctorNames` once on `Loaded`.
- It never updates when `_vm.Patients` or `_vm.Doctors` finish loading.

**Files**
- `Views/VisitFormDialog.xaml.cs`

**Fix**
1. Subscribe to `CollectionChanged` on `_vm.Patients` and `_vm.Doctors`.
2. Rebuild name lists and call `SetupSearchComboBox(...)` when the collections change.
3. Ensure you unsubscribe on `Unloaded` to avoid leaks.

**Suggested patch (conceptual)**
```csharp
_vm.Patients.CollectionChanged += (_, _) => RefreshPatientList();
_vm.Doctors.CollectionChanged += (_, _) => RefreshDoctorList();

private void RefreshPatientList()
{
    _patientNames = BuildNameList(_vm.Patients.Select(p => p.FullName));
    SetupSearchComboBox(PatientNameBox, _patientNames);
}
```

---

## 3) Visit detail "Đơn thuốc" panel is non-functional

**Severity:** Medium

**Symptoms**
- The “Đơn thuốc” section in `VisitListView` is always empty.
- “Thêm thuốc” button does nothing.

**Root Cause**
- `AddMedicine_Click` handler is empty.
- No data binding exists between visits and medicines/prescriptions in the detail panel.

**Files**
- `Views/VisitListView.xaml`
- `Views/VisitListView.xaml.cs`

**Fix (short-term)**
- Hide or disable the section to avoid user confusion until full implementation is ready.

**Fix (full implementation)**
- Decide on data model:
  - Either use `Visit.PrescriptionText` (simple text) and display it here.
  - Or implement `Prescription` + `PrescriptionDetail` tables and bind them to the UI list.
- Implement `AddMedicine_Click` to open a dialog and update prescription details.

---

## 4) Toast notifications never appear

**Severity:** Medium

**Symptoms**
- Calls to `_vm.Toast.Show(...)` do not display anything.

**Root Cause**
- There is no UI host (e.g., `ItemsControl`) bound to `ToastService.Toasts`.

**Files**
- `Services/ToastService.cs`
- `App.xaml`
- (missing host in XAML views)

**Fix**
- Add a toast host to `MainWindow.xaml` (or a global layout container):
  ```xaml
  <ItemsControl ItemsSource="{Binding Toast.Toasts}" ...>
      <!-- ItemTemplate for toast -->
  </ItemsControl>
  ```
- Use existing converters and styles already defined in resources.

---

## 5) Invoices load even though UI is "Coming Soon"

**Severity:** Low

**Symptoms**
- Invoices are loaded in `MainViewModel` even though there is no invoice UI.

**Root Cause**
- `LoadInvoicesAsync()` is called on startup, but `MainWindow.xaml` shows only placeholder text.

**Files**
- `ViewModels/MainViewModel.cs`
- `MainWindow.xaml`

**Fix**
- Either:
  - Remove or defer `LoadInvoicesAsync()` until the invoice view is built, or
  - Build the invoice view so the loaded data is used.

---

## 6) Potential orphan data when deleting doctors/patients

**Severity:** Low (future risk)

**Symptoms**
- If invoice/prescription features are enabled later, deleting a doctor/patient may leave orphan records.

**Root Cause**
- `DeleteDoctorAsync` and `DeletePatientAsync` only delete visits, not invoices/payments/prescriptions.

**Files**
- `Services/DatabaseService.cs`

**Fix**
- When invoice/prescription features are enabled, cascade delete or manually delete related rows.
- Alternatively enforce FK cascades at DB schema level.

---

## Quick Wins (Recommended First Fixes)
1. Fix double scaling (Issue #1).
2. Fix visit form autocomplete refresh (Issue #2).
3. Add toast host (Issue #4).

---

## Notes
- This report is based on static code review; no runtime debugging was performed.
- If you want, I can apply the fixes directly and update this report accordingly.
