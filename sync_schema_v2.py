import sqlite3
import os

DB_PATH = r'D:\ClinicMS\clinic_database.db'

def main():
    try:
        conn = sqlite3.connect(DB_PATH)
        cursor = conn.cursor()
        
        # 1. Đồng bộ bảng AppSettings (Rất quan trọng cho việc lưu Settings)
        # EF Core thường tìm kiếm 'Key' và 'Value' (Viết hoa)
        cursor.execute("DROP TABLE IF EXISTS AppSettings")
        cursor.execute('CREATE TABLE AppSettings ("Key" TEXT PRIMARY KEY, "Value" TEXT)')
        
        # 2. Đồng bộ bảng PrescriptionDetails (Xóa cột rác)
        try:
            cursor.execute("CREATE TABLE pd_new (VisitId INTEGER, MedicineId INTEGER, Quantity REAL, Dosage TEXT, PRIMARY KEY(VisitId, MedicineId))")
            cursor.execute("INSERT OR IGNORE INTO pd_new (VisitId, MedicineId, Quantity, Dosage) SELECT VisitId, MedicineId, Quantity, Dosage FROM PrescriptionDetails")
            cursor.execute("DROP TABLE PrescriptionDetails")
            cursor.execute("ALTER TABLE pd_new RENAME TO PrescriptionDetails")
        except:
            pass

        # 3. Đảm bảo các bảng đều có cột created_at (viết thường) vì Model dùng [Column("created_at")]
        # Chúng ta sẽ xóa cột CreatedAt (viết hoa) để tránh xung đột
        tables = ['patients', 'doctors', 'visits', 'medicines', 'services', 'invoices', 'payments', 'prescriptions', 'appointments']
        for table in tables:
            try:
                # Xóa cột CreatedAt (viết hoa)
                cursor.execute(f"ALTER TABLE {table} DROP COLUMN CreatedAt")
            except:
                pass
            
            # Đảm bảo created_at không có NULL
            try:
                cursor.execute(f"UPDATE {table} SET created_at = datetime('now') WHERE created_at IS NULL")
            except:
                pass

        # 4. Kiểm tra bảng patient_contacts
        # Đảm bảo nó không có các cột rác như created_at hay CreatedAt gây rối cho EF
        try:
            cursor.execute("CREATE TABLE pc_new (patient_id INTEGER, contact_id INTEGER, is_primary INTEGER DEFAULT 0, PRIMARY KEY(patient_id, contact_id))")
            cursor.execute("INSERT OR IGNORE INTO pc_new (patient_id, contact_id, is_primary) SELECT patient_id, contact_id, is_primary FROM patient_contacts")
            cursor.execute("DROP TABLE patient_contacts")
            cursor.execute("ALTER TABLE pc_new RENAME TO patient_contacts")
        except:
            pass

        conn.commit()
        conn.close()
        print("ĐÃ ĐỒNG BỘ SCHEMA THÀNH CÔNG!")
    except Exception as e:
        print(f"Lỗi: {e}")

if __name__ == "__main__":
    main()
