chrome.commands.onCommand.addListener((command) => {
  if (command === "toggle-popup") {
    chrome.windows.create({
      url: "searchbar.html",
      type: "popup",
      width: 500,
      height: 200,
      top: 150,
      left: 500
    });
  }
});

chrome.runtime.onMessage.addListener((message, sender, sendResponse) => {
  if (message.type === "fetchSuggestions") {
    fetch(
      `https://suggestqueries.google.com/complete/search?client=firefox&q=${encodeURIComponent(message.query)}`
    )
      .then((res) => res.json())
      .then((data) => {
        sendResponse({ suggestions: data[1] });
      })
      .catch((err) => {
        console.error("Fetch error:", err);
        sendResponse({ suggestions: [] });
      });
    return true; // penting biar async
  }
});
