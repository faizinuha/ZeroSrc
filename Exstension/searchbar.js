const input = document.getElementById('searchInput');
const suggestionsBox = document.getElementById('suggestions');
const modeIndicator = document.getElementById('mode-indicator');
const responseContainer = document.getElementById('response-container');
const apiKeyOverlay = document.getElementById('api-key-overlay');

// Modal Elements (diasumsikan ada di HTML Anda)
const openaiApiModal = document.getElementById('openai-api-modal');
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
let modes = ['google', 'openai'];
let currentModeIndex = 0;

const searchModes = {
  google: {
    url: 'https://www.google.com/search?q=',
    placeholder: 'Cari di Google...',
    className: 'mode-google',
    icon: 'G',
  },
  openai: {
    placeholder: 'Tanya AI (dukung gambar)...',
    className: 'mode-openai',
    icon: 'AI',
  },
};

async function performSearch(query, mode) {
  // Mode AI tidak bisa dijalankan tanpa query atau file
  if (mode === 'openai' && !query && !fileToSend) {
    return;
  }

  if (mode === 'openai') {
    const result = await chrome.storage.local.get(['openaiApiKey']);

    if (!result.openaiApiKey) {
      // Tampilkan modal jika API key tidak ada
      if (openaiApiModal) openaiApiModal.style.display = 'block';
      if (apiKeyOverlay) apiKeyOverlay.style.display = 'flex';
      return;
    }

    // Hapus placeholder jika ada
    const placeholder = responseContainer.querySelector('.placeholder-text');
    if (placeholder) responseContainer.innerHTML = '';

    // Tampilkan prompt pengguna di UI
    appendChatMessage(query, 'user');
    if (stopGenerationButton) stopGenerationButton.style.display = 'flex';

    // Kirim prompt ke background script
    const messagePayload = {
      type: 'promptOpenAI',
      apiKey: result.openaiApiKey,
      prompt: query,
    };

    if (fileToSend) {
      messagePayload.file = fileToSend;
    }

    chrome.runtime.sendMessage(messagePayload);

    // Reset UI setelah mengirim
    input.value = '';
    fileToSend = null;
    if (filePreviewContainer) filePreviewContainer.innerHTML = '';
    if (attachFileButton) attachFileButton.style.display = 'block';
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

// --- Event Listeners untuk Modal API Key, File, dll. ---
if (saveApiKeyButton) {
  saveApiKeyButton.addEventListener('click', () => {
    const apiKey = apiKeyInput.value.trim();
    if (apiKey) {
      chrome.storage.local.set({ openaiApiKey: apiKey }, () => {
        if (apiKeyOverlay) apiKeyOverlay.style.display = 'none';
        performSearch(input.value.trim(), 'openai'); // Coba lagi setelah simpan key
        input.focus();
      });
    }
  });
}

if (changeApiKeyButton) {
  changeApiKeyButton.addEventListener('click', () => {
    chrome.storage.local.remove('openaiApiKey', () => {
      if (apiKeyInput) apiKeyInput.value = '';
      if (openaiApiModal) openaiApiModal.style.display = 'block';
      if (apiKeyOverlay) apiKeyOverlay.style.display = 'flex';
    });
  });
}

if (apiKeyOverlay) {
  apiKeyOverlay.addEventListener('click', (e) => {
    if (e.target === apiKeyOverlay) {
      apiKeyOverlay.style.display = 'none';
    }
  });
}

if (attachFileButton) {
  attachFileButton.addEventListener('click', () => {
    if (fileInput) fileInput.click();
  });
}

if (fileInput) {
  fileInput.addEventListener('change', (event) => {
    const file = event.target.files[0];
    if (!file) return;

    // Hanya izinkan gambar untuk OpenAI
    if (!file.type.startsWith('image/')) {
      alert('Hanya file gambar yang didukung untuk mode AI ini.');
      return;
    }

    const reader = new FileReader();
    reader.onload = (e) => {
      const base64String = e.target.result.split(',')[1];
      fileToSend = {
        base64: base64String,
        mimeType: file.type,
        name: file.name,
      };
      displayFilePreview(file);
    };
    reader.readAsDataURL(file);
    fileInput.value = '';
  });
}

function displayFilePreview(file) {
  if (!filePreviewContainer) return;
  filePreviewContainer.innerHTML = '';
  const previewItem = document.createElement('div');
  previewItem.className = 'file-preview-item';

  const img = document.createElement('img');
  img.src = URL.createObjectURL(file);
  previewItem.appendChild(img);

  const fileName = document.createElement('span');
  fileName.textContent = `🖼️ ${file.name}`;
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

let currentMessageCard = null;

// Listener untuk menerima respons dari background script
chrome.runtime.onMessage.addListener((message) => {
  if (message.type === 'openaiResponseChunk') {
    if (!currentMessageCard) {
      currentMessageCard = appendChatMessage('', 'ai');
    }
    currentMessageCard.textContent += message.chunk;
    if (responseContainer)
      responseContainer.scrollTop = responseContainer.scrollHeight;
  } else if (message.type === 'openaiResponseError') {
    if (!currentMessageCard) {
      currentMessageCard = appendChatMessage(
        `Error: ${message.error}\n\nPastikan API Key OpenAI Anda benar dan mendukung model yang digunakan.`,
        'ai'
      );
    }
    if (stopGenerationButton) stopGenerationButton.style.display = 'none';
  } else if (message.type === 'promptOpenAI') {
    // Reset card saat prompt baru dikirim
    currentMessageCard = null;
  }
});

function appendChatMessage(text, role) {
  if (!responseContainer) return;
  const messageWrapper = document.createElement('div');
  messageWrapper.className = `chat-message ${role}`;

  const icon = document.createElement('div');
  icon.className = 'icon';
  icon.textContent = role === 'user' ? 'U' : 'AI';

  const messageCard = document.createElement('div');
  messageCard.className = 'message-card';
  messageCard.textContent = text;

  messageWrapper.appendChild(icon);
  messageWrapper.appendChild(messageCard);
  responseContainer.appendChild(messageWrapper);

  if (role === 'ai') {
    const copyButton = document.createElement('button');
    copyButton.className = 'copy-button';
    copyButton.textContent = '📋';
    copyButton.onclick = () => {
      navigator.clipboard.writeText(messageCard.textContent).then(() => {
        copyButton.textContent = '✅';
        setTimeout(() => {
          copyButton.textContent = '📋';
        }, 1500);
      });
    };
    messageCard.appendChild(copyButton);
  }

  responseContainer.scrollTop = responseContainer.scrollHeight;
  return messageCard;
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
    if (currentMode === 'openai') {
      currentMessageCard = null;
    }
  } else if (e.key === 'Tab') {
    e.preventDefault(); // Mencegah fokus berpindah dari input
    toggleSearchMode();
  }
});

function toggleSearchMode() {
  currentModeIndex = (currentModeIndex + 1) % modes.length;
  currentMode = modes[currentModeIndex];
  chrome.storage.local.set({ lastMode: currentMode });
  updateUIAfterModeChange();
}

// Fungsi untuk memuat mode terakhir saat popup dibuka
function loadLastMode() {
  chrome.storage.local.get(['lastMode'], (result) => {
    const lastMode = result.lastMode;
    if (lastMode && modes.includes(lastMode)) {
      currentMode = lastMode;
      currentModeIndex = modes.indexOf(lastMode);
    }
    updateUIAfterModeChange();
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

  // Tampilkan/sembunyikan elemen UI berdasarkan mode
  const isAiMode = currentMode === 'openai';
  if (attachFileButton)
    attachFileButton.style.display = isAiMode ? 'flex' : 'none';
  if (responseContainer)
    responseContainer.style.display = isAiMode ? 'flex' : 'none';

  // Atur saran pencarian
  if (currentMode !== 'google') {
    suggestionsBox.style.display = 'none';
  } else if (input.value.trim()) {
    // Jika kembali ke mode google dan ada teks, panggil lagi sarannya
    input.dispatchEvent(new Event('input'));
  }
}
