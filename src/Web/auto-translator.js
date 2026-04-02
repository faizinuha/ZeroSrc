class AutoTranslator {
  constructor(translations) {
    this.translations = translations;
    this.currentLang = this.getCurrentLanguage();
    this.observer = null;
    this.translationCache = {}; // Cache untuk performa
    this.init();
  }

  init() {
    // Load bahasa yang disimpan
    this.currentLang = this.getCurrentLanguage();
    this.buildTranslationCache();
    
    // Translate halaman pertama kali
    this.translatePage();
    
    // Observer untuk elemen baru yang ditambahkan dynamically
    this.observeDOM();
    
    // Setup language buttons
    this.setupLanguageButtons();
  }

  // Build cache untuk performa lebih baik
  buildTranslationCache() {
    this.translationCache = {};
    const currentTranslations = this.translations[this.currentLang] || {};
    
    for (let key in currentTranslations) {
      const value = currentTranslations[key];
      const normalizedKey = this.normalizeText(key);
      this.translationCache[normalizedKey] = value;
    }
  }

  // Dapatkan bahasa dari localStorage atau default ke ID
  getCurrentLanguage() {
    return localStorage.getItem('selectedLanguage') || 'id';
  }

  // Simpan bahasa ke localStorage
  setLanguage(lang) {
    localStorage.setItem('selectedLanguage', lang);
    this.currentLang = lang;
    this.buildTranslationCache();
    this.translatePage();
    window.dispatchEvent(new CustomEvent('languageChanged', { detail: { lang } }));
  }

  // Translate semua teks di halaman
  async translatePage() {
    // Translate text nodes (teks biasa)
    await this.translateTextNodes(document.body);
    
    // Translate attributes (placeholder, title, alt, aria-label)
    await this.translateAttributes(document.body);
    
    // Update active button
    this.updateLanguageButton();
    
    // Set HTML lang attribute
    document.documentElement.lang = this.currentLang;
  }

  // Translate serta mengelompokkan teks (Batch Processing)
  async translateTextNodes(node) {
    const walker = document.createTreeWalker(node, NodeFilter.SHOW_TEXT, null, false);
    let currentNode;
    const nodesToTranslate = [];
    const textsToTranslate = [];

    while (currentNode = walker.nextNode()) {
      const text = currentNode.nodeValue.trim();
      if (text.length > 2 && !this.isIgnoredElement(currentNode.parentElement)) {
        const normalized = this.normalizeText(text);
        // Jika ada di dictionary manual, translate langsung (Instan)
        if (this.translationCache[normalized]) {
          const trans = this.translationCache[normalized];
          currentNode.nodeValue = currentNode.nodeValue.replace(text, trans);
        } else if (this.currentLang !== 'id') {
          // Jika tidak ada di dict, masukkan ke antrian batch Google
          nodesToTranslate.push(currentNode);
          textsToTranslate.push(text);
        }
      }
    }

    // Kirim Batch ke Google (Max 100 strings per request agar tidak error)
    if (textsToTranslate.length > 0) {
      const batchSize = 30; // Ukuran paket optimal
      for (let i = 0; i < textsToTranslate.length; i += batchSize) {
        const batchTexts = textsToTranslate.slice(i, i + batchSize);
        const batchNodes = nodesToTranslate.slice(i, i + batchSize);
        
        try {
          const results = await this.fetchBatchGoogleTranslation(batchTexts, this.currentLang);
          batchNodes.forEach((node, idx) => {
            if (results[idx]) {
                const leadingSpace = node.nodeValue.match(/^\s*/)[0];
                const trailingSpace = node.nodeValue.match(/\s*$/)[0];
                node.nodeValue = leadingSpace + results[idx] + trailingSpace;
            }
          });
        } catch (e) {
          console.error("Batch translate failed", e);
        }
      }
    }
  }

  async fetchBatchGoogleTranslation(texts, targetLang) {
    // Teknik penggabungan teks dengan separator unik untuk batching gratis
    const separator = " ||| ";
    const combinedText = texts.join(separator);
    const url = `https://translate.googleapis.com/translate_a/single?client=gtx&sl=auto&tl=${targetLang}&dt=t&q=${encodeURIComponent(combinedText)}`;
    
    const response = await fetch(url);
    const data = await response.json();
    
    if (data && data[0]) {
      const fullTranslatedBody = data[0].map(x => x[0]).join('');
      // Pecah kembali berdasarkan separator
      return fullTranslatedBody.split("|||").map(s => s.trim());
    }
    return texts;
  }

  // Translate attributes seperti placeholder, title, alt
  async translateAttributes(node) {
    const elements = node.querySelectorAll('[placeholder], [title], [aria-label]');
    
    for (const el of elements) {
      if (el.placeholder) {
        const translated = await this.findTranslation(el.placeholder);
        if (translated) el.placeholder = translated;
      }
      if (el.title) {
        const translated = await this.findTranslation(el.title);
        if (translated) el.title = translated;
      }
      if (el.getAttribute('aria-label')) {
        const ariaLabel = el.getAttribute('aria-label');
        const translated = await this.findTranslation(ariaLabel);
        if (translated) el.setAttribute('aria-label', translated);
      }
    }
  }

  // Cari terjemahan dari string - gunakan cache atau Google Translate fallback
  async findTranslation(text) {
    if (!text || text.trim().length === 0) return null;
    
    // Jangan translate angka saja
    if (/^\d+$/.test(text.trim())) return text;

    const normalizedText = this.normalizeText(text);
    
    // 1. Cek cache (Dictionary manual)
    if (this.translationCache[normalizedText]) {
      return this.translationCache[normalizedText];
    }
    
    // 2. Jika bahasa adalah ID (default), tidak perlu translate jika tidak ada di dict
    if (this.currentLang === 'id') return text;

    // 3. Fallback ke Google Translate API (Gratis/Public Client)
    try {
      return await this.fetchGoogleTranslation(text, this.currentLang);
    } catch (error) {
      console.error('Google Translate Error:', error);
      return text; // Return original on error
    }
  }

  async fetchGoogleTranslation(text, targetLang) {
    const url = `https://translate.googleapis.com/translate_a/single?client=gtx&sl=auto&tl=${targetLang}&dt=t&q=${encodeURIComponent(text)}`;
    
    const response = await fetch(url);
    const data = await response.json();
    
    if (data && data[0]) {
      // Gabungkan hasil jika teks terdiri dari beberapa baris/array
      return data[0].map(x => x[0]).join('');
    }
    return text;
  }

  // Normalize text untuk perbandingan (lowercase, trim, normalize spaces)
  normalizeText(text) {
    return text
      .toLowerCase()
      .trim()
      .replace(/\s+/g, ' ');
  }

  // Element yang tidak perlu ditranslate
  isIgnoredElement(el) {
    if (!el) return false;
    
    const ignoredTags = ['SCRIPT', 'STYLE', 'CODE', 'PRE'];
    const ignoredClasses = ['easter-egg', 'loader', 'particle', 'no-translate'];
    
    if (ignoredTags.includes(el.tagName)) return true;
    
    for (let cls of ignoredClasses) {
      if (el.classList.contains(cls)) return true;
    }
    
    if (el.hasAttribute('data-no-translate')) return true;
    
    return false;
  }

  // Observer untuk elemen yang ditambahkan dynamically
  observeDOM() {
    this.observer = new MutationObserver(async (mutations) => {
      for (const mutation of mutations) {
        if (mutation.type === 'childList') {
          // Translate elemen baru
          for (const node of mutation.addedNodes) {
            if (node.nodeType === 1) { // Element node
              await this.translateTextNodes(node);
              await this.translateAttributes(node);
            }
          }
        }
      }
    });

    this.observer.observe(document.body, {
      childList: true,
      subtree: true
    });
  }

  // Setup language buttons
  setupLanguageButtons() {
    // Cari semua language buttons dengan data-lang attribute
    const buttons = document.querySelectorAll('[data-lang]');
    
    buttons.forEach(btn => {
      btn.addEventListener('click', (e) => {
        e.preventDefault();
        e.stopPropagation();
        const lang = btn.getAttribute('data-lang');
        this.setLanguage(lang);
      });
    });
  }

  // Update active button state
  updateLanguageButton() {
    document.querySelectorAll('[data-lang]').forEach(btn => {
      btn.classList.remove('active');
      
      if (btn.getAttribute('data-lang') === this.currentLang) {
        btn.classList.add('active');
      }
    });
  }
}

// Initialize saat halaman selesai load
document.addEventListener('DOMContentLoaded', function() {
  if (typeof translations !== 'undefined') {
    window.translator = new AutoTranslator(translations);
  } else {
    console.warn('translations.js tidak ditemukan. TMS tidak akan berfungsi.');
  }
});

