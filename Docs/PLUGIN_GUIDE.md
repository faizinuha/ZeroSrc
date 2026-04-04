# ZeroMix Plugin Development Guide

Plugin ZeroMix ditulis menggunakan **Lua** dan disimpan di folder `Plugins/` dalam direktori instalasi ZeroMix.

---

## Daftar Isi

1. [Persiapan](#1-persiapan)
2. [Buat Plugin Pertama](#2-buat-plugin-pertama)
3. [Struktur Folder](#3-struktur-folder)
4. [Struktur Script](#4-struktur-script)
5. [API Reference](#5-api-reference)
6. [Contoh Plugin Lengkap](#6-contoh-plugin-lengkap)
7. [Cara Aktifkan Plugin](#7-cara-aktifkan-plugin)
8. [Cara Bagikan Plugin](#8-cara-bagikan-plugin)
9. [Tips & Aturan](#9-tips--aturan)

---

## 1. Persiapan

Yang kamu butuhkan:
- ZeroMix sudah terinstall
- Text editor (Notepad, VS Code, dll)
- Tidak perlu install apapun lagi — Lua engine sudah built-in di ZeroMix

Lokasi folder Plugins setelah install:
```
C:\Program Files\ZeroMix\Plugins\
```

---

## 2. Buat Plugin Pertama

### Step 1 — Buka folder Plugins

Buka File Explorer → navigasi ke:
```
C:\Program Files\ZeroMix\Plugins\
```

### Step 2 — Buat folder plugin

Buat folder baru dengan format nama:
```
user.pub.NamaPlugin
```

Contoh: `user.pub.HelloWorld`

> Prefix `user.pub.` artinya plugin publik yang bisa dibagikan.
> Prefix `user.priv.` artinya plugin privat, hanya untuk kamu.

### Step 3 — Buat file script.lua

Di dalam folder tadi, buat file bernama `script.lua` (harus persis nama ini).

### Step 4 — Tulis kode

Buka `script.lua` dengan text editor, tulis:

```lua
function OnLoad()
    CreateUI("Hello World", 300, 200)
    AddLabel("Halo dari plugin pertamaku!")
    AddButton("Klik Aku", "OnKlik")
end

function OnKlik()
    Notify("Hore!", "Tombol berhasil diklik!")
end
```

### Step 5 — Aktifkan di ZeroMix

Buka ZeroMix → klik menu **Plugins** → plugin kamu otomatis muncul → klik **Enable**.

Selesai! Plugin langsung jalan tanpa perlu restart.

---

## 3. Struktur Folder

```
C:\Program Files\ZeroMix\Plugins\
  │
  ├── user.pub.HelloWorld\       ← plugin publik
  │     └── script.lua
  │
  ├── user.pub.ToDo\             ← plugin publik lain
  │     ├── script.lua
  │     └── tasks_data.json      ← data tersimpan (auto-generated)
  │
  └── user.priv.RahasiaSaya\    ← plugin privat
        └── script.lua
```

Aturan penamaan folder:

| Prefix | Visibilitas | Keterangan |
|--------|-------------|------------|
| `user.pub.` | Publik | Tampil di daftar, bisa dibagikan |
| `user.priv.` | Privat | Tampil di daftar, tidak untuk dibagikan |

---

## 4. Struktur Script

```lua
-- ZeroMix Plugin: Nama Plugin
-- Deskripsi: Penjelasan singkat

-- Dipanggil SEKALI saat plugin diaktifkan
function OnLoad()
    CreateUI("Judul Window", lebar, tinggi)
    -- tambah UI di sini
end

-- Dipanggil SETIAP DETIK selama plugin aktif (opsional)
function OnUpdate()
    -- update data real-time di sini
end

-- Fungsi custom yang dipanggil oleh tombol
function FungsiBuatanSendiri()
    -- logika di sini
end
```

---

## 5. API Reference

### UI

| Fungsi | Parameter | Keterangan |
|--------|-----------|------------|
| `CreateUI(title, width, height)` | string, int, int | Buat window plugin — **wajib dipanggil pertama** |
| `AddLabel(text)` | string | Tambah teks |
| `AddInput(id, placeholder)` | string, string | Tambah input field |
| `AddButton(text, callback)` | string, string | Tambah tombol, `callback` = nama fungsi Lua |
| `GetInput(id)` | → string | Ambil nilai dari input field |

### Notifikasi & Log

| Fungsi | Parameter | Keterangan |
|--------|-----------|------------|
| `Notify(title, message)` | string, string | Tampilkan popup dialog |
| `Log(message)` | string | Tulis ke debug output (tidak tampil ke user) |

### Simpan & Baca Data

| Fungsi | Parameter | Keterangan |
|--------|-----------|------------|
| `SaveConfig(key, value)` | string, string | Simpan string ke file `key.json` di folder plugin |
| `LoadConfig(key)` | string → string | Baca data yang tersimpan, return `""` jika belum ada |
| `JsonEncode(data)` | any → string | Encode data ke JSON string |
| `JsonDecode(json)` | string → any | Decode JSON string ke data |

### Info Sistem

| Fungsi | Return | Keterangan |
|--------|--------|------------|
| `GetCpuUsage()` | number | CPU usage persen (0–100) |
| `GetRamUsage()` | number | RAM usage persen (0–100) |
| `GetTimeHour()` | number | Jam saat ini (0–23) |
| `GetTimeMin()` | number | Menit saat ini (0–59) |
| `SetStatusText(text)` | — | Set teks di status bar ZeroMix |

---

## 6. Contoh Plugin Lengkap

### To-Do List (simpan data)

```lua
function OnLoad()
    CreateUI('To-Do List', 300, 420)
    AddLabel('APA RENCANA KAMU HARI INI?')
    AddInput('task_input', 'Tulis tugas di sini...')
    AddButton('TAMBAH TUGAS', 'AddTask')

    -- Tampilkan tugas yang sudah tersimpan
    local saved = LoadConfig('tasks_data')
    if saved ~= '' and saved ~= nil then
        AddLabel('--- TUGAS TERSIMPAN ---')
        AddLabel(saved)
    end
end

function AddTask()
    local task = GetInput('task_input')
    if task ~= '' and task ~= nil then
        AddLabel('• ' .. task)
        local current = LoadConfig('tasks_data')
        local updated = (current ~= '') and (current .. '\n• ' .. task) or ('• ' .. task)
        SaveConfig('tasks_data', updated)
        Notify('Sukses', 'Tugas berhasil disimpan!')
    else
        Notify('Peringatan', 'Isi tugasnya dulu!')
    end
end
```

### Monitor CPU Real-time

```lua
function OnLoad()
    CreateUI('System Monitor', 250, 150)
    AddLabel('Memantau CPU & RAM...')
end

function OnUpdate()
    local cpu = GetCpuUsage()
    local ram = GetRamUsage()
    SetStatusText('CPU: ' .. cpu .. '% | RAM: ' .. ram .. '%')
end
```

### Jam Digital

```lua
function OnLoad()
    CreateUI('Jam', 200, 120)
    AddLabel('Waktu sekarang:')
    AddLabel('Memuat...')
end

function OnUpdate()
    local h = GetTimeHour()
    local m = GetTimeMin()
    local label = string.format('%02d:%02d', h, m)
    SetStatusText('🕐 ' .. label)
end
```

---

## 7. Cara Aktifkan Plugin

1. Buka ZeroMix
2. Klik menu **Plugins** di sidebar kiri
3. Plugin kamu otomatis terdeteksi dan muncul di daftar
4. Klik tombol **Enable** di sebelah nama plugin
5. Window plugin langsung muncul

> ZeroMix mendeteksi plugin baru secara otomatis — tidak perlu restart aplikasi.

---

## 8. Cara Bagikan Plugin

Zip folder plugin kamu:

```
user.pub.NamaPlugin.zip
  └── user.pub.NamaPlugin\
        └── script.lua
```

Kirim file zip ke orang lain. Mereka tinggal:
1. Extract zip ke `C:\Program Files\ZeroMix\Plugins\`
2. Buka ZeroMix → Plugins → Enable

---

## 9. Tips & Aturan

- `CreateUI` **harus** dipanggil sebelum `AddLabel`, `AddInput`, `AddButton`
- `OnUpdate` dipanggil setiap detik — jangan taruh operasi berat di sini
- `SaveConfig` / `LoadConfig` menyimpan data di folder plugin itu sendiri
- Nama folder = nama plugin yang tampil di UI (tanpa prefix `user.pub.` / `user.priv.`)
- Plugin `user.priv.*` tetap tampil di daftar tapi tidak dimaksudkan untuk dibagikan
- Kalau plugin error, cek output di **Debug Console** — pesan error ditulis via `Log()`
