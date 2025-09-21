const input = document.getElementById('searchInput');
const suggestionsBox = document.getElementById('suggestions');
const modeIndicator = document.getElementById('mode-indicator');
const responseContainer = document.getElementById('response-container');
const apiKeyOverlay = document.getElementById('api-key-overlay');
const apiKeyInput = document.getElementById('apiKeyInput');
const saveApiKeyButton = document.getElementById('saveApiKeyButton');
const changeApiKeyButton = document.getElementById('changeApiKeyButton');
const stopGenerationButton = document.getElementById('stop-generation-button');

let currentMode = 'google'; // 'google' atau 'gemini'

const searchModes = {
  google: {
    url: 'https://www.google.com/search?q=',
    placeholder: 'Cari di Google...',
    className: 'mode-google',
  },
  gemini: {
    url: 'https://gemini.google.com/app?q=',
    placeholder: 'Tanya Gemini...',
    className: 'mode-gemini',
  },
};

async function performSearch(query, mode) {
  if (!query) return;

  if (mode === 'gemini') {
    // Cek apakah API Key sudah ada
    const result = await chrome.storage.local.get(['geminiApiKey']);
    if (!result.geminiApiKey) {
      // Jika tidak ada, tampilkan modal untuk input key
      changeApiKeyButton.style.display = 'none'; // Sembunyikan tombol ganti jika belum ada key
      apiKeyOverlay.style.display = 'flex';
      return;
    }

    // Hapus placeholder HANYA jika ada
    const placeholder = responseContainer.querySelector('.placeholder-text');
    if (placeholder) {
      responseContainer.innerHTML = '';
    }
    // Tampilkan prompt pengguna
    appendChatMessage(query, 'user');

    stopGenerationButton.style.display = 'flex'; // Show stop button
    // Kirim prompt ke background script untuk diproses oleh AI
    chrome.runtime.sendMessage({
      type: 'promptGemini',
      prompt: query,
      apiKey: result.geminiApiKey,
      // We will add abort signal later
    });
    input.value = ''; // Kosongkan input setelah mengirim
  } else {
    // Buka tab pencarian Google seperti biasa
    window.open(
      searchModes[currentMode].url + encodeURIComponent(query),
      '_blank'
    );
  }
}

saveApiKeyButton.addEventListener('click', () => {
  const apiKey = apiKeyInput.value.trim();
  if (apiKey) {
    chrome.storage.local.set({ geminiApiKey: apiKey }, () => {
      apiKeyOverlay.style.display = 'none';
      apiKeyInput.value = '';
      // Beri tahu pengguna untuk mencoba lagi, jangan kirim ulang secara otomatis
      // untuk menghindari rate-limiting (Error 429).
      input.focus();
    });
  }
});

changeApiKeyButton.addEventListener('click', () => {
  chrome.storage.local.remove('geminiApiKey', () => {
    apiKeyInput.value = '';
    changeApiKeyButton.style.display = 'none';
  });
});

apiKeyOverlay.addEventListener('click', (e) => {
  // Sembunyikan overlay jika klik di luar modal
  if (e.target === apiKeyOverlay) {
    apiKeyOverlay.style.display = 'none';
  }
});

stopGenerationButton.addEventListener('click', () => {
  // Logic to stop generation will be added here. For now, it just hides.
  stopGenerationButton.style.display = 'none';
});
let currentGeminiMessageCard = null;

// Listener untuk menerima respons streaming dari background script
chrome.runtime.onMessage.addListener((message) => {
  if (message.type === 'geminiResponseChunk') {
    if (!currentGeminiMessageCard) {
      currentGeminiMessageCard = appendChatMessage('', 'gemini');
    }
    currentGeminiMessageCard.textContent += message.chunk;
    responseContainer.scrollTop = responseContainer.scrollHeight; // Auto-scroll
  } else if (message.type === 'geminiResponseError') {
    if (!currentGeminiMessageCard) {
      // This handles the case where an error occurs before any chunk is received.
      // Create the card only when the error is received.
      currentGeminiMessageCard = appendChatMessage(
        `Error: ${message.error}\n\nPastikan API Key Anda benar. Klik Tab lalu Enter untuk mengatur ulang.`,
        'gemini'
      );
    }
    stopGenerationButton.style.display = 'none';
  } else if (message.type === 'promptGemini') {
    // Reset card saat prompt baru dikirim
    currentGeminiMessageCard = null;
  }
});

function appendChatMessage(text, role) {
  const messageWrapper = document.createElement('div');
  messageWrapper.className = `chat-message ${role}`;

  const icon = document.createElement('div');
  icon.className = 'icon';
  if (role === 'user') {
    icon.textContent = 'U'; // 'U' for User
  } else {
    icon.textContent = '✧';
  }

  const messageCard = document.createElement('div');
  messageCard.className = 'message-card';
  messageCard.textContent = text;

  messageWrapper.appendChild(icon);
  messageWrapper.appendChild(messageCard);
  responseContainer.appendChild(messageWrapper);

  // Add copy button only for Gemini messages
  if (role === 'gemini') {
    const copyButton = document.createElement('button');
    copyButton.className = 'copy-button';
    copyButton.textContent = '📋'; // Clipboard emoji
    copyButton.onclick = () => {
      navigator.clipboard.writeText(messageCard.textContent).then(() => {
        copyButton.textContent = '✅'; // Checkmark on success
        setTimeout(() => {
          copyButton.textContent = '📋'; // Revert back
        }, 1500);
      });
    };
    messageCard.appendChild(copyButton);
  }

  responseContainer.scrollTop = responseContainer.scrollHeight;
  return messageCard; // Kembalikan elemen card agar bisa diupdate
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
  if (!query || currentMode === 'gemini') {
    suggestionsBox.style.display = 'none';
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
      suggestionsBox.style.display = 'flex';
    } else {
      suggestionsBox.style.display = 'none';
    }
  } catch (error) {
    console.error('Failed to fetch suggestions:', error);
    suggestionsBox.style.display = 'none';
  }
});

suggestionsBox.addEventListener('click', (e) => {
  if (e.target && e.target.classList.contains('suggestion-item')) {
    const query = e.target.textContent;
    performSearch(query, 'google'); // Klik saran selalu mencari di Google
  }
});

input.addEventListener('keydown', (e) => {
  if (e.key === 'Enter') {
    performSearch(input.value.trim(), currentMode);
    // Reset card saat prompt baru dikirim
    if (currentMode === 'gemini') {
      currentGeminiMessageCard = null;
    }
  } else if (e.key === 'Tab') {
    e.preventDefault(); // Mencegah fokus berpindah dari input
    toggleSearchMode();
  }
});

function toggleSearchMode() {
  // Ganti mode
  currentMode = currentMode === 'google' ? 'gemini' : 'google';

  // Simpan mode yang baru dipilih ke storage
  chrome.storage.local.set({ lastMode: currentMode });

  // Perbarui UI
  const newMode = searchModes[currentMode];
  input.placeholder = newMode.placeholder;
  modeIndicator.className = newMode.className;
  modeIndicator.textContent = currentMode === 'google' ? 'G' : '✧'; // 'G' untuk Google, '✧' (bintang) untuk Gemini

  // Sembunyikan atau tampilkan suggestions berdasarkan mode
  if (currentMode === 'gemini') {
    suggestionsBox.style.display = 'none';
  }

  suggestionsBox.style.display = 'none'; // Sembunyikan saran saat ganti mode
}

// Fungsi untuk memuat mode terakhir saat popup dibuka
function loadLastMode() {
  chrome.storage.local.get(['lastMode'], (result) => {
    if (result.lastMode && result.lastMode !== currentMode) {
      toggleSearchMode(); // Gunakan toggle untuk memastikan UI konsisten
    }
  });
}

// Panggil fungsi untuk memuat mode saat skrip dijalankan
document.addEventListener('DOMContentLoaded', () => {
  loadLastMode();
  // Fokuskan ke input saat popup dibuka
  input.focus();
});
