const input = document.getElementById('searchInput');
const suggestionsBox = document.getElementById('suggestions');

function fetchSuggestions(query) {
  return new Promise((resolve) => {
    chrome.runtime.sendMessage(
      { type: 'fetchSuggestions', query },
      (response) => {
        resolve(response.suggestions || []);
      }
    );
  });
}

input.addEventListener('input', async (e) => {
  const query = e.target.value.trim();
  if (!query) {
    suggestionsBox.style.display = 'none';
    return;
  }

  const suggestions = await fetchSuggestions(query);
  suggestionsBox.innerHTML = '';

  if (suggestions.length) {
    suggestions.forEach((item) => {
      const div = document.createElement('div');
      div.textContent = item;
      div.className = 'suggestion-item';
      div.onclick = () => {
        window.open(
          'https://www.google.com/search?q=' + encodeURIComponent(item),
          '_blank'
        );
      };
      suggestionsBox.appendChild(div);
    });
    suggestionsBox.style.display = 'flex';
  } else {
    suggestionsBox.style.display = 'none';
  }
});

// Tekan Enter
input.addEventListener('keydown', (e) => {
  if (e.key === 'Enter') {
    const query = input.value.trim();
    if (query) {
      window.open(
        'https://www.google.com/search?q=' + encodeURIComponent(query),
        '_blank'
      );
    }
  }
});
