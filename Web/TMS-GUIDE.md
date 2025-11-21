# 🌐 ZeroMix Automatic TMS (Translation Management System) - Documentation

## Overview

ZeroMix menggunakan sistem **automatic TMS** yang bukan manual. Artinya, tidak perlu menambahkan `data-i18n` attributes di setiap elemen HTML. Sistem ini secara otomatis mendeteksi dan menerjemahkan semua teks di halaman.

## 📁 File-file TMS

### 1. **translations.js** 
- File yang berisi dictionary semua terjemahan (Indonesian & English)
- Struktur: `translations = { id: { key: value }, en: { key: value } }`
- **Tidak perlu diubah** - semua terjemahan sudah lengkap

### 2. **auto-translator.js** 
- Class utama yang menangani translasi otomatis
- Fitur-fitur:
  - 🔄 Mendeteksi dan menerjemahkan semua text nodes di halaman
  - 💾 Menyimpan pilihan bahasa ke localStorage
  - 👁️ MutationObserver untuk konten dinamis
  - ⚡ Caching untuk performa lebih baik

### 3. **Language Buttons di HTML**
```html
<button class="lang-btn active" data-lang="id">🇮🇩 ID</button>
<button class="lang-btn" data-lang="en">🇺🇸 EN</button>
```
- Letaknya di navbar (`.language-switcher`)
- Klik untuk mengganti bahasa
- Active state automatically updated

## 🚀 Cara Kerja

### Step 1: Page Load
1. `translations.js` loaded terlebih dahulu (berisi dictionary)
2. `auto-translator.js` loaded dan instantly buat instance `window.translator`
3. DOMContentLoaded event trigger inisialisasi

### Step 2: Language Detection
- Cek localStorage untuk `selectedLanguage`
- Jika tidak ada, default ke Indonesian (`id`)
- Load language dari localStorage jika ada

### Step 3: Page Translation
```javascript
translatePage() → {
  translateTextNodes() // Translate semua text
  translateAttributes() // Translate placeholder, title, alt, aria-label
  updateLanguageButton() // Update active button
}
```

### Step 4: Language Switching
1. User klik language button
2. Trigger `setLanguage(lang)` 
3. Save ke localStorage
4. Rebuild translation cache
5. Translate ulang semua halaman
6. Dispatch `languageChanged` event

### Step 5: Dynamic Content
- MutationObserver mendengarkan DOM changes
- Otomatis translate elemen baru yang ditambahkan via JavaScript

## 🔍 Cara Menambahkan Terjemahan Baru

### Jika ingin menambahkan text baru:

1. **Edit `translations.js`**:
```javascript
const translations = {
  id: {
    'Teks Asli': 'Teks Asli',
    // ... tambahkan di sini
    'Teks Baru Indonesia': 'Teks Baru Indonesia'
  },
  en: {
    'Teks Asli': 'Original Text',
    // ... tambahkan di sini
    'Teks Baru Indonesia': 'New Text English'
  }
};
```

2. **Jangan tambahkan attribute `data-i18n`** - sistem akan otomatis mendeteksi!

3. **Reload halaman** - terjemahan akan muncul otomatis

## ⚙️ Configuration

### Language Buttons
Standar di navbar:
```html
<li class="language-switcher">
  <button class="lang-btn active" data-lang="id">🇮🇩 ID</button>
  <button class="lang-btn" data-lang="en">🇺🇸 EN</button>
</li>
```

### Ignored Elements
Element yang TIDAK akan ditranslate:
- `<script>`, `<style>`, `<code>`, `<pre>` tags
- Element dengan class: `easter-egg`, `loader`, `particle`, `no-translate`
- Element dengan attribute: `data-no-translate`

### Cache
Sistem menggunakan `translationCache` untuk performa:
- Saat language berubah, cache di-rebuild
- Pencarian translation pakai cache (lebih cepat)

## 🧪 Testing

Buka `/test-translator.html` untuk test:
- ✅ Basic text translation
- ✅ Language switching
- ✅ Dynamic content translation
- ✅ localStorage persistence

## 📝 Notes

- **Automatic = Tanpa manual attribute**
- **Responsive = Kerja di semua halaman otomatis**
- **Smart = Preserve whitespace, handle nested elements**
- **Fast = Caching & selective translation**

## 🐛 Troubleshooting

### Terjemahan tidak muncul?
1. Cek browser console (F12) - ada error?
2. Pastikan `translations.js` loaded sebelum `auto-translator.js`
3. Pastikan text di HTML sama persis dengan key di `translations.js`

### Language tidak tersimpan?
1. Cek localStorage (F12 → Application → localStorage)
2. `selectedLanguage` key harus ada

### Button tidak merespons?
1. Pastikan button punya `data-lang="id"` atau `data-lang="en"`
2. Pastikan button di dalam elemen yang di-DOM-observe

## 📌 Best Practices

1. **Keep translations.js updated** - semua text harus ada di dua bahasa
2. **Don't use dynamic text in HTML** - jika text dinamis, generate dari JavaScript
3. **Use meaningful keys** - gunakan text actual sebagai key untuk mudah maintain
4. **Test semua pages** - pastikan semua page translate dengan baik

---

**Made with ❤️ for ZeroMix**
