chrome.commands.onCommand.addListener((command) => {
  if (command === 'toggle-popup') {
    chrome.windows.create({
      url: 'searchbar.html',
      type: 'popup',
      width: 540,
      height: 640, // Adjusted to fit the new UI
      top: 150,
      left: 500,
    });
  }
});

chrome.runtime.onMessage.addListener((message, sender, sendResponse) => {
  // --- Handler for Google Suggestions ---
  if (message.type === 'fetchSuggestions') {
    fetch(
      `https://suggestqueries.google.com/complete/search?client=firefox&q=${encodeURIComponent(
        message.query
      )}`
    )
      .then((res) => res.json())
      .then((data) => {
        sendResponse({ success: true, suggestions: data[1] || [] });
      })
      .catch((err) => {
        console.error('Fetch error:', err);
        sendResponse({ success: false, error: err.message });
      });
    return true; // penting biar async
  }
});
