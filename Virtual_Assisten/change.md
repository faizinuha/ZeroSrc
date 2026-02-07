1. Ubah Padding (Cara Paling Aman)
Di baris ini, angka - 40 artinya memberikan jarak (padding) 40 pixel dari atas dan bawah. Jika Kakak ingin karakter terlihat lebih besar (lebih rapat ke atas/bawah), kecilkan angkanya (misalnya jadi - 10).

javascript
// Baris 123
const targetHeight = app.screen.height - 40; // Ubah 40 menjadi angka lebih kecil untuk memperbesar
Dan juga di baris 151 (di dalam setTimeout):

javascript
// Baris 151
const finalRatio = (app.screen.height - 40) / model.height; // Ubah ini juga agar sama
2. Gunakan Skala Manual (Jika ingin sangat besar/zoom)
Jika Kakak ingin mengabaikan sistem otomatis dan ingin karakter terlihat sangat besar (zoom-in), Kakak bisa mengganti baris model.scale.set(autoScale) dengan angka manual, tapi Kakak harus menghapus bagian setTimeout di bawahnya agar tidak ditimpa lagi oleh sistem otomatis.

Contoh jika ingin skala manual:

javascript
model.scale.set(0.08); // Contoh angka manual yang lebih besar dari standar (0.055)
Saran saya: Cukup ubah angka 40 menjadi 0 atau angka negatif (misal -50) jika Kakak ingin karakter terlihat "zoom in" melewati batas jendela. 