# ZeroMix Plugin Development Guide

## Cara Membuat Plugin

Plugin ZeroMix ditulis menggunakan **Lua** dan disimpan di folder `Plugins/` dalam direktori instalasi ZeroMix.

---

## Struktur Folder Plugin

```
Plugins/
  user.pub.NamaPlugin/     ← Plugin publik (bisa dibagikan)
    script.lua
  user.priv.NamaPlugin/    ← Plugin privat (hanya lokal)
    script.lua
```

Prefix folder menentukan visibilitas:
- `user.pub.` → publik, bisa dibagikan ke orang lain
- `user.priv.` → privat, hanya untuk kamu sendiri

---

## Struktur Script Dasar

```lua
-- ZeroMix Plugin: Nama Plugin Kamu
-- Deskripsi: Apa yang dilakukan plugin ini

function OnLoad()
    -- Dipanggil saat plugin diaktifkan
    -- Buat UI di sini
    CreateUI("Judul Window", 300, 400)
    AddLabel("Halo dari plugin!")
end

function OnUpdate()
    -- Dipanggil setiap 1 detik (opsional)
    -- Cocok untuk update data real-time
end
```

---

## API Reference

### UI

| Fungsi | Parameter | Keterangan |
|--------|-----------|------------|
| `CreateUI(title, width, height)` | string, int, int | Buat window plugin |
| `AddLabel(text)` | string | Tambah teks |
| `AddInput(id, placeholder)` | string, string | Tambah input field |
| `AddButton(text, callback)` | string, string | Tambah tombol |
| `GetInput(id)` | string | Ambil nilai input |

### Notifikasi & Log

| Fungsi | Parameter | Keterangan |
|--------|-----------|------------|
| `Notify(title, message)` | string, string | Tampilkan popup notifikasi |
| `Log(message)` | string | Tulis ke debug console |

### Data & Config

| Fungsi | Parameter | Keterangan |
|--------|-----------|------------|
| `SaveConfig(key, value)` | string, string | Simpan data ke file JSON |
| `LoadConfig(key)` | string | Baca data yang tersimpan |
| `JsonEncode(data)` | any | Encode ke JSON string |
| `JsonDecode(json)` | string | Decode dari JSON string |

### System Info

| Fungsi | Return | Keterangan |
|--------|--------|------------|
| `GetCpuUsage()` | number | CPU usage (0-100) |
| `GetRamUsage()` | number | RAM usage (0-100) |
| `GetTimeHour()` | number | Jam saat ini (0-23) |
| `GetTimeMin()` | number | Menit saat ini (0-59) |
| `SetStatusText(text)` | - | Set teks status bar ZeroMix |

---

## Contoh Plugin Lengkap

### To-Do List

```lua
function OnLoad()
    CreateUI('To-Do List', 300, 420)
    AddLabel('APA RENCANA KAMU HARI INI?')
    AddInput('task_input', 'Tulis tugas di sini...')
    AddButton('TAMBAH TUGAS', 'AddTask')

    local savedTasks = LoadConfig('tasks_data')
    if savedTasks ~= '' and savedTasks ~= nil then
        AddLabel('--- TUGAS TERSIMPAN ---')
        AddLabel(savedTasks)
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
    CreateUI('CPU Monitor', 250, 150)
    AddLabel('CPU Usage:')
    AddLabel('Memuat...')
end

function OnUpdate()
    local cpu = GetCpuUsage()
    local ram = GetRamUsage()
    SetStatusText('CPU: ' .. cpu .. '% | RAM: ' .. ram .. '%')
end
```

---

## Cara Install Plugin

1. Buat folder `user.pub.NamaPlugin` di dalam folder `Plugins/` di direktori instalasi ZeroMix
2. Buat file `script.lua` di dalam folder tersebut
3. ZeroMix otomatis mendeteksi plugin baru tanpa perlu restart
4. Aktifkan plugin dari menu **Plugins** di ZeroMix

Default lokasi folder Plugins:
```
C:\Program Files\ZeroMix\Plugins\
```

---

## Cara Membagikan Plugin

Zip folder plugin kamu dan bagikan. User lain tinggal extract ke folder `Plugins/` mereka.

```
user.pub.NamaPlugin.zip
  └── user.pub.NamaPlugin/
        └── script.lua
```

---

## Tips

- Gunakan `SaveConfig` / `LoadConfig` untuk menyimpan data antar sesi
- `OnUpdate` dipanggil setiap detik — jangan taruh operasi berat di sini
- Nama folder menentukan nama plugin yang tampil di UI ZeroMix
- Plugin `user.priv.*` tidak akan muncul di daftar plugin publik
