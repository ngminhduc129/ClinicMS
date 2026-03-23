import sqlite3
import os

DB_PATH = r'D:\ClinicMS\clinic_database.db'

def main():
    try:
        conn = sqlite3.connect(DB_PATH)
        cursor = conn.cursor()
        
        # Danh sách các bảng cần tạo lại để đảm bảo đúng cột
        tables_to_drop = [
            'doctors', 'visits', 'medicines', 'services', 'appointments', 
            'PrescriptionDetails', 'invoices', 'invoice_items', 'payments', 
            'service_orders', 'prescriptions'
        ]
        
        print("Đang xóa các bảng cũ (trừ bảng bệnh nhân)...")
        for table in tables_to_drop:
            cursor.execute(f"DROP TABLE IF EXISTS {table}")
        
        print("Đang tạo lại các bảng với cấu trúc cột chính xác tuyệt đối...")

        # Bảng Doctors
        cursor.execute("""
            CREATE TABLE doctors (
                doctor_id INTEGER PRIMARY KEY AUTOINCREMENT,
                full_name TEXT NOT NULL,
                specialty TEXT,
                phone TEXT,
                email TEXT,
                note TEXT,
                created_at TEXT
            )
        """)

        # Bảng Visits (Khám bệnh)
        cursor.execute("""
            CREATE TABLE visits (
                visit_id INTEGER PRIMARY KEY AUTOINCREMENT,
                patient_id INTEGER NOT NULL,
                doctor_id INTEGER NOT NULL,
                appointment_id INTEGER,
                visit_time TEXT,
                visit_type TEXT DEFAULT 'Walk-in',
                status TEXT DEFAULT 'Waiting',
                symptoms TEXT,
                diagnosis TEXT,
                note TEXT,
                pulse TEXT,
                blood_pressure TEXT,
                temperature TEXT,
                weight TEXT,
                prescription_text TEXT
            )
        """)

        # Bảng Medicines (Thuốc)
        cursor.execute("""
            CREATE TABLE medicines (
                medicine_id INTEGER PRIMARY KEY AUTOINCREMENT,
                medicine_name TEXT NOT NULL,
                unit TEXT,
                price REAL DEFAULT 0,
                stock_quantity INTEGER DEFAULT 0,
                created_at TEXT
            )
        """)

        # Bảng Services (Dịch vụ)
        cursor.execute("""
            CREATE TABLE services (
                service_id INTEGER PRIMARY KEY AUTOINCREMENT,
                service_name TEXT NOT NULL,
                price REAL DEFAULT 0
            )
        """)

        # Bảng PrescriptionDetails
        cursor.execute("""
            CREATE TABLE PrescriptionDetails (
                VisitId INTEGER NOT NULL,
                MedicineId INTEGER NOT NULL,
                Quantity REAL DEFAULT 0,
                Dosage TEXT,
                PRIMARY KEY (VisitId, MedicineId)
            )
        """)

        # Bảng Invoices (Hóa đơn)
        cursor.execute("""
            CREATE TABLE invoices (
                visit_id INTEGER PRIMARY KEY,
                total_amount REAL DEFAULT 0,
                status TEXT DEFAULT 'Unpaid',
                created_at TEXT
            )
        """)

        # Bảng InvoiceItems (Chi tiết hóa đơn)
        cursor.execute("""
            CREATE TABLE invoice_items (
                visit_id INTEGER NOT NULL,
                item_type TEXT NOT NULL,
                item_id INTEGER NOT NULL,
                quantity INTEGER DEFAULT 0,
                unit_price REAL DEFAULT 0,
                PRIMARY KEY (visit_id, item_type, item_id)
            )
        """)

        # Bảng Payments (Thanh toán)
        cursor.execute("""
            CREATE TABLE payments (
                payment_id INTEGER PRIMARY KEY AUTOINCREMENT,
                visit_id INTEGER NOT NULL,
                amount REAL DEFAULT 0,
                payment_method TEXT,
                payment_time TEXT
            )
        """)

        # Bảng ServiceOrders
        cursor.execute("""
            CREATE TABLE service_orders (
                service_order_id INTEGER PRIMARY KEY AUTOINCREMENT,
                visit_id INTEGER NOT NULL,
                service_id INTEGER NOT NULL,
                result TEXT
            )
        """)

        # Bảng Prescriptions (Đơn thuốc)
        cursor.execute("""
            CREATE TABLE prescriptions (
                visit_id INTEGER PRIMARY KEY,
                created_at TEXT
            )
        """)

        print("Đang thêm dữ liệu mẫu...")
        cursor.execute("INSERT INTO doctors (full_name, specialty, phone) VALUES ('BS. Nguyễn Văn A', 'Nội tổng quát', '0912345678')")
        cursor.execute("INSERT INTO medicines (medicine_name, unit, price, stock_quantity) VALUES ('Paracetamol 500mg', 'Viên', 2000, 500)")
        cursor.execute("INSERT INTO medicines (medicine_name, unit, price, stock_quantity) VALUES ('Amoxicillin 250mg', 'Viên', 5000, 200)")
        cursor.execute("INSERT INTO services (service_name, price) VALUES ('Khám bệnh tổng quát', 150000)")
        cursor.execute("INSERT INTO services (service_name, price) VALUES ('Xét nghiệm máu', 350000)")

        conn.commit()
        conn.close()
        print("HOÀN THÀNH! Tất cả các bảng đã được khôi phục với đầy đủ cột.")
    except Exception as e:
        print(f"Có lỗi xảy ra: {e}")

if __name__ == "__main__":
    main()
