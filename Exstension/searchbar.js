const input = document.getElementById('searchInput');
const suggestionsBox = document.getElementById('suggestions');
const modeIndicator = document.getElementById('mode-indicator');
const responseContainer = document.getElementById('response-container');

const historySidebar = document.getElementById('history-sidebar');
const historyContent = document.getElementById('history-content');
const historyToggleBtn = document.getElementById('history-toggle-btn');

let currentMode = 'google'; // 'google', 'gemini', atau 'gemini-image'

const searchModes = {
  google: {
    url: 'https://www.google.com/search?q=',
    placeholder: 'Cari di Google...',
    className: 'mode-google',
    icon: 'G',
  },
};

async function performSearch(query, mode) {
  if (!query) return;
  // Buka tab pencarian Google seperti biasa
  input.value = ''; // Kosongkan input setelah mengirim
  window.open(
    searchModes[currentMode].url + encodeURIComponent(query),
    '_blank'
  );

  // Sembunyikan kotak saran setelah pencarian dilakukan
  suggestionsBox.classList.remove('visible');
  suggestionsBox.style.display = 'none'; // Pastikan hilang
}

/**
 * Mengambil dan menampilkan riwayat pencarian.
 * @param {string} query - Teks untuk mencari riwayat. Jika kosong, tampilkan riwayat terbaru.
 */
function displaySearchHistory(query = '') {
  if (!historyContent) return;
  historyContent.innerHTML = ''; // Bersihkan kontainer

  // Buat judul untuk bagian riwayat
  const historyTitle = document.createElement('h4');
  historyTitle.className = 'history-title';
  historyTitle.textContent = query
    ? 'Hasil Pencarian Riwayat'
    : 'Riwayat Terbaru';
  historyContent.appendChild(historyTitle);

  // Ambil item riwayat
  chrome.history.search({ text: query, maxResults: 50 }, (historyItems) => {
    if (chrome.runtime.lastError) {
      console.error(chrome.runtime.lastError.message);
      historyContent.innerHTML =
        '<div class="placeholder-text" style="font-size:14px; padding: 10px;">Gagal memuat riwayat. Pastikan izin "history" ada di manifest.</div>';
      return;
    }

    if (historyItems.length === 0) {
      historyContent.innerHTML +=
        '<div class="placeholder-text" style="font-size:14px; padding: 10px;">Tidak ada riwayat ditemukan.</div>';
      return;
    }

    const list = document.createElement('div');
    list.className = 'history-list';

    // Filter untuk menghindari duplikat judul
    const uniqueItems = [];
    const seenTitles = new Set();
    historyItems.forEach((item) => {
      if (item.title && !seenTitles.has(item.title)) {
        seenTitles.add(item.title);
        uniqueItems.push(item);
      }
    });

    uniqueItems.slice(0, 20).forEach((item) => {
      if (!item.title || !item.url) return; // Lewati item tanpa judul atau url
      const historyItem = document.createElement('a');
      historyItem.className = 'history-item';
      historyItem.href = item.url;
      historyItem.target = '_blank';
      historyItem.dataset.url = item.url; // Simpan URL untuk penghapusan

      const deleteBtn = document.createElement('button');
      deleteBtn.className = 'history-item-delete-btn';
      deleteBtn.innerHTML = '&times;';
      deleteBtn.title = 'Hapus dari riwayat';

      // Gunakan favicon Google untuk visual
      historyItem.innerHTML = `<img src="https://www.google.com/s2/favicons?domain=${
        new URL(item.url).hostname
      }" class="history-item-favicon" alt="favicon" /><div class="history-item-text"><span class="history-item-title">${
        item.title
      }</span><span class="history-item-url">${item.url}</span></div>`;
      historyItem.appendChild(deleteBtn);
      list.appendChild(historyItem);
    });
    historyContent.appendChild(list);
  });
}

function fetchSuggestions(query) {
  return new Promise((resolve, reject) => {
    chrome.runtime.sendMessage(
      { type: 'fetchSuggestions', query },
      (response) => {
        if (chrome.runtime.lastError) {
          return reject(chrome.runtime.lastError);
        }
        response.success
          ? resolve(response.suggestions)
          : reject(new Error(response.error));
      }
    );
  });
}

input.addEventListener('input', async (e) => {
  const query = e.target.value.trim();
  if (!query || currentMode !== 'google') {
    suggestionsBox.classList.remove('visible');
    suggestionsBox.style.display = 'none'; // Pastikan hilang
    // Jika input kosong, tampilkan riwayat terbaru lagi
    displaySearchHistory();
    return;
  }

  try {
    const suggestions = await fetchSuggestions(query);
    suggestionsBox.innerHTML = ''; // Hapus saran lama

    if (suggestions.length > 0) {
      const fragment = document.createDocumentFragment();
      suggestions.forEach((item) => {
        const div = document.createElement('div');
        div.textContent = item;
        div.className = 'suggestion-item';
        fragment.appendChild(div);
      });
      suggestionsBox.appendChild(fragment);
      suggestionsBox.classList.add('visible');
    } else {
      suggestionsBox.classList.remove('visible');
    }
  } catch (error) {
    console.error('Failed to fetch suggestions:', error);
    suggestionsBox.classList.remove('visible');
  }

  // Selalu cari riwayat saat mengetik
  displaySearchHistory(query);
});

suggestionsBox.addEventListener('click', (e) => {
  if (e.target && e.target.classList.contains('suggestion-item')) {
    const query = e.target.textContent;
    performSearch(query, 'google'); // Klik saran selalu mencari di Google
  }
});

input.addEventListener('keydown', (e) => {
  if (e.key === 'Enter') {
    // Cek jika ada file yang dipilih, kirim bahkan jika input teks kosong
    const query = input.value.trim();
    if (query) {
      performSearch(query, currentMode);
    }
  }
});

// Panggil fungsi untuk memuat mode saat skrip dijalankan
document.addEventListener('DOMContentLoaded', () => {
  // Tampilkan riwayat terbaru saat pertama kali dibuka
  displaySearchHistory();
  // Fokuskan ke input saat popup dibuka
  input.focus();

  // Event listener untuk tombol buka/tutup sidebar
  historyToggleBtn.addEventListener('click', () => {
    historySidebar.classList.toggle('open');
  });

  // Event listener untuk tombol hapus (delegasi)
  historyContent.addEventListener('click', (e) => {
    if (e.target && e.target.classList.contains('history-item-delete-btn')) {
      e.preventDefault(); // Mencegah link terbuka
      const historyItem = e.target.closest('.history-item');
      const urlToDelete = historyItem.dataset.url;
      chrome.history.deleteUrl({ url: urlToDelete }, () =>
        historyItem.remove()
      );
    }
  });
});
