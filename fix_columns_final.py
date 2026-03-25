import sqlite3
import os

DB_PATH = r'D:\ClinicMS\clinic_database.db'

def main():
    try:
        conn = sqlite3.connect(DB_PATH)
        cursor = conn.cursor()
        
        # Lấy danh sách tất cả các bảng
        cursor.execute("SELECT name FROM sqlite_master WHERE type='table'")
        tables = [row[0] for row in cursor.fetchall() if row[0] != 'sqlite_sequence']
        
        print("Bắt đầu 'phủ' cột created_at cho toàn bộ database...")
        
        for table in tables:
            try:
                # Cố gắng thêm cột created_at (viết thường)
                cursor.execute(f"ALTER TABLE {table} ADD COLUMN created_at TEXT")
                print(f"Added 'created_at' to table: {table}")
            except:
                pass # Cột đã tồn tại
            
            try:
                # Cố gắng thêm cột CreatedAt (viết hoa chữ cái đầu) - đề phòng EF Core kén chọn
                cursor.execute(f"ALTER TABLE {table} ADD COLUMN CreatedAt TEXT")
                print(f"Added 'CreatedAt' to table: {table}")
            except:
                pass

        # RIÊNG BẢNG AppSettings: Đảm bảo có cả viết hoa và viết thường cho các cột Key/Value
        for table in ['AppSettings']:
            for col in ['key', 'value', 'Key', 'Value']:
                try:
                    cursor.execute(f"ALTER TABLE {table} ADD COLUMN {col} TEXT")
                    print(f"Added '{col}' to table: {table}")
                except:
                    pass

        conn.commit()
        conn.close()
        print("\nHOÀN TẤT! Toàn bộ Database đã được đồng bộ cột created_at.")
    except Exception as e:
        print(f"Lỗi: {e}")

if __name__ == "__main__":
    main()
