// background.js

chrome.runtime.onInstalled.addListener(() => {
  chrome.storage.sync.get("shortcut", (data) => {
    if (!data.shortcut) {
      // Set default shortcut if not already set
      chrome.storage.sync.set({ shortcut: ["Control", "Backslash"] });
    }
  });
});

// Listener for messages from popup.js or content.js
chrome.runtime.onMessage.addListener((request, sender, sendResponse) => {
  if (request.action === "updateShortcut") {
    // When the shortcut is updated from the popup,
    // we might want to inform existing content scripts to reload their listener
    chrome.tabs.query({}, (tabs) => {
      tabs.forEach(tab => {
        if (tab.url && tab.url.startsWith("http")) { // Only inject into web pages
          chrome.scripting.executeScript({
            target: { tabId: tab.id },
            function: () => {
              // Re-run the loadAndListen function in content.js
              if (typeof window.loadAndListen === 'function') {
                window.loadAndListen();
              }
            }
          }).catch(error => console.warn("Failed to inject script into tab:", tab.id, error));
        }
      });
    });
    sendResponse({ status: "shortcut updated and content scripts reloaded" });
    return true; // Indicates an asynchronous response
  }
});