
$now = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
$sqlFile = "seed_demo_data.sql"
$dbFile = "clinic_database.db"

# Create SQL script
Set-Content -Path $sqlFile -Value "BEGIN TRANSACTION;"

# 1. Doctors (100)
"INSERT INTO doctors (full_name, specialty, phone, email, created_at) VALUES " | Out-File -FilePath $sqlFile -Append
for ($i = 1; $i -le 100; $i++) {
    $name = "Bác sĩ $($i)"
    $specialty = @("Nội khoa", "Ngoại khoa", "Nhi khoa", "Sản khoa", "Da liễu", "Mắt", "Tai mũi họng", "Răng hàm mặt")[(Get-Random -Maximum 8)]
    $phone = "09$(Get-Random -Minimum 10000000 -Maximum 99999999)"
    $email = "doctor$($i)@clinic.com"
    $comma = if ($i -lt 100) { "," } else { ";" }
    "('$name', '$specialty', '$phone', '$email', '$now')$comma" | Out-File -FilePath $sqlFile -Append
}

# 2. Medicines (500)
"INSERT INTO medicines (medicine_name, unit, price, stock_quantity, created_at) VALUES " | Out-File -FilePath $sqlFile -Append
for ($i = 1; $i -le 500; $i++) {
    $name = "Thuốc $($i)"
    $unit = @("Viên", "Chai", "Ống", "Hộp", "Gói")[(Get-Random -Maximum 5)]
    $price = (Get-Random -Minimum 10 -Maximum 500) * 1000
    $stock = Get-Random -Minimum 0 -Maximum 200
    $comma = if ($i -lt 500) { "," } else { ";" }
    "('$name', '$unit', $price, $stock, '$now')$comma" | Out-File -FilePath $sqlFile -Append
}

# 3. Patients (1000)
# We need to insert into contacts first, then patients, then patient_contacts
# To avoid ID conflicts, we'll get the current max ID
# Actually, it's easier to just use SQLite's autoincrement

# Insert Contacts
"INSERT INTO contacts (phone, email, address, created_at) VALUES " | Out-File -FilePath $sqlFile -Append
for ($i = 1; $i -le 1000; $i++) {
    $phone = "03$(Get-Random -Minimum 10000000 -Maximum 99999999)"
    $email = "patient$($i)@gmail.com"
    $address = "$($i) Đường Lê Lợi, TP. HCM"
    $comma = if ($i -lt 1000) { "," } else { ";" }
    "( '$phone', '$email', '$address', '$now')$comma" | Out-File -FilePath $sqlFile -Append
}

# Insert Patients
# Using last_insert_rowid() is hard in batch, so we'll do them one by one or in blocks
# Actually, since we just inserted 1000 contacts, their IDs will be from (last_id + 1) to (last_id + 1000)
# Let's get the max contact_id first

"INSERT INTO patients (full_name, date_of_birth, gender, blood_type, height_cm, weight_kg, created_at) VALUES " | Out-File -FilePath $sqlFile -Append
for ($i = 1; $i -le 1000; $i++) {
    $name = "Bệnh nhân $($i)"
    $dob = "19$((Get-Random -Minimum 50 -Maximum 99))-0$((Get-Random -Minimum 1 -Maximum 9))-0$((Get-Random -Minimum 1 -Maximum 9))"
    $gender = @("Nam", "Nữ")[(Get-Random -Maximum 2)]
    $blood = @("A", "B", "O", "AB")[(Get-Random -Maximum 4)]
    $height = Get-Random -Minimum 150 -Maximum 190
    $weight = Get-Random -Minimum 45 -Maximum 90
    $comma = if ($i -lt 1000) { "," } else { ";" }
    "('$name', '$dob', '$gender', '$blood', $height, $weight, '$now')$comma" | Out-File -FilePath $sqlFile -Append
}

# Insert PatientContacts
# This is tricky because we need the IDs.
# Let's use a simpler approach: 
# After inserting both, we'll link them by index if they were inserted in order.
# But it's better to do it carefully.

# Actually, I'll just write a small C# script and run it, it's MUCH safer and handles IDs correctly.
