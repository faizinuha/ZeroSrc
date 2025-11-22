# ZeroMix Update Checker CLI

Simple update checker untuk aplikasi ZeroMix. Cek update terbaru langsung dari GitHub dengan satu perintah!

## 📦 Instalasi

### Windows (via Setup.iss)
```
ZeroMix-Setup.exe
```
Installer akan otomatis menambahkan CLI ke PATH.

### Manual (Windows)
```powershell
# 1. Install Node.js dari https://nodejs.org (LTS recommended)

# 2. Extract zeromix-cli ke C:\Program Files\ZeroMix\bin\

# 3. Tambah PATH:
setx PATH "%PATH%;C:\Program Files\ZeroMix\bin"

# 4. Restart terminal
```

### Manual (Linux/Mac)
```bash
# 1. Install Node.js
# Ubuntu/Debian: sudo apt install nodejs npm
# Mac: brew install node

# 2. Extract zeromix-cli
cp -r zeromix-cli-folder /opt/zeromix-cli

# 3. Create symlink
sudo ln -s /opt/zeromix-cli/index.js /usr/local/bin/zeromix-cli
```

## 🚀 Penggunaan

```bash
# Cek update
zeromix-cli cek-update

# Atau singkat:
zeromix-cli

# Lihat bantuan
zeromix-cli --help

# Tampilkan versi
zeromix-cli version
```

## 📋 Fitur

✅ Cek update dari GitHub otomatis  
✅ Tampilkan changelog  
✅ Buka browser ke halaman download  
✅ User-triggered (tidak paksa)  
✅ Bahasa Indonesia  
✅ Cross-platform (Windows/Mac/Linux)  
✅ Error handling dengan fallback links  

## 🔧 Development

```bash
# Test locally
node index.js cek-update

# Test bantuan
node index.js --help
```

## 📝 Konfigurasi

Untuk mengubah repository:

**index.js:**
```javascript
const REPO = 'username/nama-repo';
```

## 🐛 Troubleshooting

### Node.js tidak ditemukan
```
Instalasi dari https://nodejs.org
```

### Perintah zeromix-cli tidak dikenal
```
1. Pastikan Node.js terinstall: node --version
2. Cek PATH: echo %PATH% (Windows) atau echo $PATH (Linux/Mac)
3. Restart terminal
```

### Timeout saat cek update
```
Cek koneksi internet, GitHub API mungkin sedang down
```

## 📄 License

MIT License - Bebas digunakan dan dimodifikasi

## 🔗 Links

- 🌐 Website: https://zeromix.pages.dev
- 🐙 GitHub: https://github.com/faizinuha/ZeroMix
- 📮 Issues: https://github.com/faizinuha/ZeroMix/issues
