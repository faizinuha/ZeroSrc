chrome.commands.onCommand.addListener((command) => {
  if (command === 'toggle-search') {
    chrome.tabs.query({ active: true, currentWindow: true }, (tabs) => {
      if (tabs[0]) {
        chrome.scripting.executeScript({
          target: { tabId: tabs[0].id },
          files: ['content.js'],
        });
      } else {
        console.error('No active tab found!');
      }
    });
  }
});
