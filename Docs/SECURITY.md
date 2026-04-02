## Kebijakan Keamanan ZeroMix

Kami menganggap serius keamanan ZeroMix. Jika Anda menemukan kerentanan keamanan, kami sangat menghargai jika Anda melapor kepada kami secara bertanggung jawab agar kami dapat memperbaikinya secepat mungkin.

### Verifikasi Instalasi

Semua file installer ZeroMix ditandatangani secara digital (code-signed). Untuk memverifikasi integritas file yang Anda download:

```powershell
# Verifikasi SHA256 hash
(Get-FileHash "ZeroMix-Setup-vX.X.X.exe" -Algorithm SHA256).Hash
```

Bandingkan hash tersebut dengan yang tertera di halaman [Releases](https://github.com/faizinuha/ZeroMix/releases).

### Cara Install yang Aman

```powershell
# Download script installer
Invoke-WebRequest -Uri "https://raw.githubusercontent.com/faizinuha/ZeroMix/main/install.ps1" -OutFile "$env:TEMP\zeromix-install.ps1"

# Review isi script sebelum menjalankan
Get-Content "$env:TEMP\zeromix-install.ps1" | more

# Jalankan setelah yakin isinya aman
& "$env:TEMP\zeromix-install.ps1"
```

> **JANGAN** gunakan `iwr | iex` atau URL shortener. Selalu download, review, lalu jalankan.

### Cara Melapor Kerentanan

Harap jangan membuat isu publik di GitHub untuk melaporkan kerentanan keamanan. Sebagai gantinya, kirimkan laporan melalui [Security Advisory](https://github.com/faizinuha/ZeroMix/security/advisories/new).

Dalam laporan Anda, harap sertakan:

* **Deskripsi Kerentanan:** Jelaskan kerentanan yang Anda temukan secara rinci.
* **Langkah-langkah untuk Mereplikasi:** Berikan langkah-langkah yang jelas. Jika memungkinkan, sertakan cuplikan kode atau tangkapan layar.
* **Dampak Potensial:** Jelaskan bagaimana kerentanan ini dapat dieksploitasi.

### Respons Kami

* Kami akan mengakui penerimaan laporan Anda dalam waktu 3 hari kerja.
* Kami akan memberikan perkiraan waktu untuk perbaikan.
* Kami akan memberi tahu Anda setelah masalah diperbaiki.

Kami berkomitmen untuk bekerja sama dengan Anda untuk menyelesaikan masalah dengan cepat dan tidak akan mengambil tindakan hukum terhadap peneliti yang bertindak dengan itikad baik.

### Keamanan Pipeline

| Komponen | Mekanisme Keamanan |
|----------|-------------------|
| Installer Script | Download → Review → Execute (tidak pakai `iex`) |
| Code Signing | osslsigncode + PFX certificate + DigiCert timestamp |
| CI/CD Secrets | Certificate password disimpan di GitHub Secrets |
| Hash Validation | SHA256 via `Get-FileHash` |
| Dependency Check | .NET 9 & WebView2 dicek sebelum install |
| Security Scanning | CodeQL otomatis setiap push & weekly |

Terima kasih telah membantu menjaga ZeroMix tetap aman!
