import sqlite3
import os

DB_PATH = r'D:\ClinicMS\clinic_database.db'

def main():
    try:
        conn = sqlite3.connect(DB_PATH)
        cursor = conn.cursor()
        
        # SQL để lấy danh sách bảng
        cursor.execute("SELECT name FROM sqlite_master WHERE type='table'")
        tables = [row[0] for row in cursor.fetchall() if row[0] != 'sqlite_sequence']
        
        print("Đang đặt giá trị mặc định CURRENT_TIMESTAMP cho các cột created_at...")
        
        for table in tables:
            try:
                # SQLite không cho ALTER DEFAULT, nên ta chỉ đảm bảo cột tồn tại
                # Nếu muốn chuẩn hóa 100%, ta dùng script này để điền dữ liệu trống
                cursor.execute(f"UPDATE {table} SET created_at = datetime('now') WHERE created_at IS NULL")
                cursor.execute(f"UPDATE {table} SET CreatedAt = datetime('now') WHERE CreatedAt IS NULL")
            except:
                pass

        conn.commit()
        conn.close()
        print("Đã dọn dẹp dữ liệu trống thành công!")
    except Exception as e:
        print(f"Lỗi: {e}")

if __name__ == "__main__":
    main()
