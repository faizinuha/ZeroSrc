const input = document.getElementById('searchInput');
const suggestionsBox = document.getElementById('suggestions');
const modeIndicator = document.getElementById('mode-indicator');
const responseContainer = document.getElementById('response-container');
const apiKeyOverlay = document.getElementById('api-key-overlay');

// Gemini Modal Elements
const geminiApiModal = document.getElementById('gemini-api-modal');
const apiKeyInput = document.getElementById('apiKeyInput');
const saveApiKeyButton = document.getElementById('saveApiKeyButton');
const changeApiKeyButton = document.getElementById('changeApiKeyButton');

const stopGenerationButton = document.getElementById('stop-generation-button');
const attachFileButton = document.getElementById('attach-file-button');
const fileInput = document.getElementById('fileInput');
const filePreviewContainer = document.getElementById('file-preview-container');

// State untuk menyimpan file yang akan diunggah
let fileToSend = null;

let currentMode = 'google'; // 'google', 'gemini', atau 'gemini-image'
let modes = ['google', 'gemini', 'gemini-image'];
let currentModeIndex = 0;

const searchModes = {
  google: {
    url: 'https://www.google.com/search?q=',
    placeholder: 'Cari di Google...',
    className: 'mode-google',
    icon: 'G',
  },
  gemini: {
    placeholder: 'Tanya Gemini...',
    className: 'mode-gemini',
    icon: '✧',
  },
  'gemini-image': {
    placeholder: 'Buat gambar dengan Gemini...',
    className: 'mode-gemini-image',
    icon: '🎨',
  },
};

async function performSearch(query, mode) {
  if (!query) return;

  // Mode AI tidak bisa dijalankan tanpa query atau file
  if ((mode === 'gemini' || mode === 'gemini-image') && !query && !fileToSend) {
    return;
  }

  if (mode === 'gemini' || mode === 'gemini-image') {
    const result = await chrome.storage.local.get(['geminiApiKey']);

    if (!result.geminiApiKey) {
      geminiApiModal.style.display = 'block';
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

    // Kirim prompt ke background script
    const messagePayload = {
      type: mode === 'gemini' ? 'promptGemini' : 'promptGeminiImage',
      apiKey: result.geminiApiKey,
      prompt: query,
    };

    if (fileToSend) {
      messagePayload.file = fileToSend;
    }

    chrome.runtime.sendMessage(messagePayload);

    // Reset UI
    input.value = '';
    fileToSend = null;
    filePreviewContainer.innerHTML = '';
    attachFileButton.style.display = 'block';
  } else {
    // Buka tab pencarian Google seperti biasa
    input.value = ''; // Kosongkan input setelah mengirim
    window.open(
      searchModes[currentMode].url + encodeURIComponent(query),
      '_blank'
    );
  }

  // Sembunyikan kotak saran setelah pencarian dilakukan
  suggestionsBox.style.display = 'none';
}

saveApiKeyButton.addEventListener('click', () => {
  const apiKey = apiKeyInput.value.trim();
  if (apiKey) {
    chrome.storage.local.set({ geminiApiKey: apiKey }, () => {
      apiKeyOverlay.style.display = 'none';
      input.value = ''; // Clear input to avoid resubmitting old prompt
      performSearch(input.value.trim(), 'gemini'); // Retry the search
      input.focus();
    });
  }
});

changeApiKeyButton.addEventListener('click', () => {
  chrome.storage.local.remove('geminiApiKey', () => {
    apiKeyInput.value = '';
    geminiApiModal.style.display = 'block';
    apiKeyOverlay.style.display = 'flex';
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

// --- File Handling Logic ---
attachFileButton.addEventListener('click', () => {
  fileInput.click();
});

fileInput.addEventListener('change', (event) => {
  const file = event.target.files[0];
  if (!file) return;

  // Batasi ukuran file (misal: 20MB) untuk mencegah error
  if (file.size > 20 * 1024 * 1024) {
    alert('File terlalu besar. Maksimal 20MB.');
    return;
  }

  const reader = new FileReader();
  reader.onload = (e) => {
    const base64String = e.target.result.split(',')[1]; // Ambil hanya data base64
    fileToSend = {
      base64: base64String,
      mimeType: file.type,
      name: file.name,
    };
    displayFilePreview(file);
  };
  reader.readAsDataURL(file);

  // Reset input file agar bisa memilih file yang sama lagi
  fileInput.value = '';
});

function displayFilePreview(file) {
  filePreviewContainer.innerHTML = ''; // Hapus pratinjau lama
  const previewItem = document.createElement('div');
  previewItem.className = 'file-preview-item';

  let fileIcon = '📄'; // Ikon default
  if (file.type.startsWith('image/')) {
    const img = document.createElement('img');
    img.src = URL.createObjectURL(file);
    previewItem.appendChild(img);
  } else if (file.type.startsWith('audio/')) {
    fileIcon = '🎵';
  } else if (file.type.startsWith('video/')) {
    fileIcon = '🎬';
  }

  const fileName = document.createElement('span');
  fileName.textContent = `${fileIcon} ${file.name}`;
  previewItem.appendChild(fileName);

  const removeButton = document.createElement('button');
  removeButton.className = 'remove-file-button';
  removeButton.innerHTML = '&times;';
  removeButton.onclick = () => {
    fileToSend = null;
    filePreviewContainer.innerHTML = '';
  };
  previewItem.appendChild(removeButton);

  filePreviewContainer.appendChild(previewItem);
}
let currentGeminiMessageCard = null;

// Listener untuk menerima respons dari background script
chrome.runtime.onMessage.addListener((message) => {
  if (
    message.type === 'geminiResponseChunk'
  ) {
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
        `Error: ${message.error}\n\nPastikan API Key Anda benar.`,
        'gemini'
      );
    }
    stopGenerationButton.style.display = 'none';
  } else if (
    message.type === 'promptGemini' ||
    message.type === 'promptGeminiImage'
  ) {
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
    // gemini
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
  if (!query || currentMode !== 'google') {
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
    // Cek jika ada file yang dipilih, kirim bahkan jika input teks kosong
    const query = input.value.trim();
    if (query || fileToSend) {
      performSearch(query, currentMode);
    }

    if (currentMode === 'gemini' || currentMode === 'gemini-image') {
      currentGeminiMessageCard = null;
    }
  } else if (e.key === 'Tab') {
    e.preventDefault(); // Mencegah fokus berpindah dari input
    toggleSearchMode();
  }
});

function toggleSearchMode() {
  // Cycle through modes: google -> gemini -> gemini-image -> google
  currentModeIndex = (currentModeIndex + 1) % modes.length;
  currentMode = modes[currentModeIndex];

  // Simpan mode yang baru dipilih ke storage
  chrome.storage.local.set({ lastMode: currentMode });

  // Perbarui UI
  const newMode = searchModes[currentMode];
  input.placeholder = newMode.placeholder;
  modeIndicator.className = newMode.className;
  modeIndicator.textContent = newMode.icon;

  // Sembunyikan atau tampilkan suggestions berdasarkan mode
  if (currentMode !== 'google') {
    suggestionsBox.style.display = 'none';
  }

  suggestionsBox.style.display = 'none'; // Sembunyikan saran saat ganti mode
}

// Fungsi untuk memuat mode terakhir saat popup dibuka
function loadLastMode() {
  chrome.storage.local.get(['lastMode'], (result) => {
    const lastMode = result.lastMode;
    if (lastMode && modes.includes(lastMode)) {
      currentMode = lastMode;
      currentModeIndex = modes.indexOf(lastMode);
      // Update UI without toggling to avoid cycling
      updateUIAfterModeChange();
    }
  });
}

// Panggil fungsi untuk memuat mode saat skrip dijalankan
document.addEventListener('DOMContentLoaded', () => {
  loadLastMode();
  // Fokuskan ke input saat popup dibuka
  input.focus();
});

function updateUIAfterModeChange() {
  const newMode = searchModes[currentMode];
  input.placeholder = newMode.placeholder;
  modeIndicator.className = newMode.className;
  modeIndicator.textContent = newMode.icon;
  if (currentMode !== 'google') suggestionsBox.style.display = 'none';
  else input.dispatchEvent(new Event('input')); // Trigger suggestion fetch if in google mode
}
