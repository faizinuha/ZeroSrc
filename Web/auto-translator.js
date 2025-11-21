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
  translatePage() {
    // Translate text nodes (teks biasa)
    this.translateTextNodes(document.body);
    
    // Translate attributes (placeholder, title, alt, aria-label)
    this.translateAttributes(document.body);
    
    // Update active button
    this.updateLanguageButton();
    
    // Set HTML lang attribute
    document.documentElement.lang = this.currentLang;
  }

  // Translate text nodes secara rekursif
  translateTextNodes(node) {
    const walker = document.createTreeWalker(
      node,
      NodeFilter.SHOW_TEXT,
      null,
      false
    );

    let currentNode;
    const nodesToTranslate = [];

    // Collect semua text nodes
    while (currentNode = walker.nextNode()) {
      const text = currentNode.nodeValue.trim();
      
      // Skip empty nodes dan ignored elements
      if (text.length > 0 && !this.isIgnoredElement(currentNode.parentElement)) {
        nodesToTranslate.push(currentNode);
      }
    }

    // Translate setiap text node
    nodesToTranslate.forEach(textNode => {
      const originalText = textNode.nodeValue.trim();
      const translatedText = this.findTranslation(originalText);
      
      if (translatedText && translatedText !== originalText) {
        // Preserve whitespace - jika ada whitespace di awal/akhir, pertahankan
        const leadingSpace = textNode.nodeValue.match(/^\s*/)[0];
        const trailingSpace = textNode.nodeValue.match(/\s*$/)[0];
        textNode.nodeValue = leadingSpace + translatedText + trailingSpace;
      }
    });
  }

  // Translate attributes seperti placeholder, title, alt
  translateAttributes(node) {
    const elements = node.querySelectorAll('[placeholder], [title], [aria-label]');
    
    elements.forEach(el => {
      if (el.placeholder) {
        const translated = this.findTranslation(el.placeholder);
        if (translated) el.placeholder = translated;
      }
      if (el.title) {
        const translated = this.findTranslation(el.title);
        if (translated) el.title = translated;
      }
      if (el.getAttribute('aria-label')) {
        const ariaLabel = el.getAttribute('aria-label');
        const translated = this.findTranslation(ariaLabel);
        if (translated) el.setAttribute('aria-label', translated);
      }
    });
  }

  // Cari terjemahan dari string - gunakan cache
  findTranslation(text) {
    if (!text) return null;
    
    const normalizedText = this.normalizeText(text);
    
    // Cek cache dulu
    if (this.translationCache[normalizedText]) {
      return this.translationCache[normalizedText];
    }
    
    // Jika tidak ditemukan di cache, return null
    // (Ini berarti teks tidak ada di translations dictionary)
    return null;
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
    this.observer = new MutationObserver((mutations) => {
      mutations.forEach((mutation) => {
        if (mutation.type === 'childList') {
          // Translate elemen baru
          mutation.addedNodes.forEach(node => {
            if (node.nodeType === 1) { // Element node
              this.translateTextNodes(node);
              this.translateAttributes(node);
            }
          });
        }
      });
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

