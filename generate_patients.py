import sqlite3
import random
from datetime import datetime, timedelta
import os

DB_PATH = r'D:\ClinicMS\clinic_database.db'

# Dữ liệu mẫu tiếng Việt
HO = ["Nguyễn", "Trần", "Lê", "Phạm", "Hoàng", "Phan", "Vũ", "Đặng", "Bùi", "Đỗ", "Hồ", "Ngô", "Lý", "Dương"]
LOT_NAM = ["Văn", "Minh", "Đức", "Hoàng", "Anh", "Quang", "Hữu", "Tuấn", "Thành", "Bảo"]
LOT_NU = ["Thị", "Ngọc", "Thanh", "Hương", "Mai", "Quỳnh", "Trà", "Phương", "Diệu", "Ánh"]
TEN_NAM = ["Anh", "Bình", "Cường", "Dũng", "Em", "Giang", "Hùng", "Kiên", "Long", "Minh", "Nam", "Sơn", "Tùng", "Việt", "Hoàng", "Phúc"]
TEN_NU = ["An", "Bích", "Chi", "Diệp", "Hoa", "Linh", "Mai", "Nga", "Oanh", "Quyên", "Thảo", "Vân", "Xuân", "Yến", "Tú", "Lan"]
DIA_CHI = ["Quận 1, TP.HCM", "Quận 7, TP.HCM", "Thanh Xuân, Hà Nội", "Cầu Giấy, Hà Nội", "Hải Châu, Đà Nẵng", "Ninh Kiều, Cần Thơ", "TP. Thủ Đức", "Biên Hòa, Đồng Nai"]
GENDER = ["Nam", "Nữ"]
BLOOD_TYPES = ["A+", "A-", "B+", "B-", "O+", "O-", "AB+", "AB-"]

def generate_patient():
    gender = random.choice(GENDER)
    if gender == "Nam":
        full_name = f"{random.choice(HO)} {random.choice(LOT_NAM)} {random.choice(TEN_NAM)}"
    else:
        full_name = f"{random.choice(HO)} {random.choice(LOT_NU)} {random.choice(TEN_NU)}"
    
    start_date = datetime(1950, 1, 1)
    end_date = datetime(2020, 12, 31)
    random_days = random.randint(0, (end_date - start_date).days)
    dob = (start_date + timedelta(days=random_days)).strftime('%Y-%m-%d')
    
    phone = f"0{random.randint(32, 99)}{random.randint(1000000, 9999999)}"
    email = f"user{random.randint(1000, 99999)}@gmail.com"
    address = random.choice(DIA_CHI)
    
    return {
        "full_name": full_name,
        "dob": dob,
        "gender": gender,
        "blood_type": random.choice(BLOOD_TYPES),
        "height": random.randint(150, 185),
        "weight": random.randint(45, 95),
        "phone": phone,
        "email": email,
        "address": address,
        "created_at": datetime.now().strftime('%Y-%m-%d %H:%M:%S')
    }

def create_schema(cursor):
    # Tạo các bảng cơ bản nếu chưa có (theo schema của ClinicDbContext)
    cursor.execute("""
    CREATE TABLE IF NOT EXISTS contacts (
        contact_id INTEGER PRIMARY KEY AUTOINCREMENT,
        phone TEXT,
        email TEXT,
        address TEXT
    )""")
    
    cursor.execute("""
    CREATE TABLE IF NOT EXISTS patients (
        patient_id INTEGER PRIMARY KEY AUTOINCREMENT,
        full_name TEXT NOT NULL,
        date_of_birth TEXT,
        gender TEXT,
        blood_type TEXT,
        height_cm REAL,
        weight_kg REAL,
        note TEXT,
        created_at TEXT
    )""")
    
    cursor.execute("""
    CREATE TABLE IF NOT EXISTS patient_contacts (
        patient_id INTEGER NOT NULL,
        contact_id INTEGER NOT NULL,
        is_primary INTEGER DEFAULT 0,
        PRIMARY KEY (patient_id, contact_id)
    )""")

def main():
    try:
        conn = sqlite3.connect(DB_PATH)
        cursor = conn.cursor()
        
        print("Đang khởi tạo cấu trúc bảng...")
        create_schema(cursor)
        
        print(f"Bắt đầu thêm 1000 bệnh nhân vào {DB_PATH}...")
        
        for i in range(1000):
            p = generate_patient()
            
            # 1. Thêm vào contacts
            cursor.execute("INSERT INTO contacts (phone, email, address) VALUES (?, ?, ?)", 
                           (p['phone'], p['email'], p['address']))
            contact_id = cursor.lastrowid
            
            # 2. Thêm vào patients
            cursor.execute("""
                INSERT INTO patients (full_name, date_of_birth, gender, blood_type, height_cm, weight_kg, created_at) 
                VALUES (?, ?, ?, ?, ?, ?, ?)
            """, (p['full_name'], p['dob'], p['gender'], p['blood_type'], p['height'], p['weight'], p['created_at']))
            patient_id = cursor.lastrowid
            
            # 3. Thêm vào patient_contacts
            cursor.execute("INSERT INTO patient_contacts (patient_id, contact_id, is_primary) VALUES (?, ?, 1)", 
                           (patient_id, contact_id))
            
            if (i + 1) % 200 == 0:
                print(f"Đã xong {i + 1} bệnh nhân...")

        conn.commit()
        conn.close()
        print("Hoàn thành! Đã tạo database và thêm 1000 bệnh nhân thành công.")
    except Exception as e:
        print(f"Có lỗi xảy ra: {e}")

if __name__ == "__main__":
    main()
