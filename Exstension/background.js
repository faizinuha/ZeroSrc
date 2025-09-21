chrome.commands.onCommand.addListener((command) => {
  if (command === 'toggle-popup') {
    chrome.windows.create({
      url: 'searchbar.html',
      type: 'popup',
      width: 500,
      height: 600, // Adjusted to fit the new UI
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

  // --- Handler for Gemini Prompt ---
  if (message.type === 'promptGemini') {
    const { apiKey, prompt } = message;
    if (!apiKey) {
      chrome.runtime.sendMessage({
        type: 'geminiResponseError',
        error: 'API Key is not set.',
      });
      return false; // No async response needed
    }

    const API_URL = `https://generativelanguage.googleapis.com/v1beta/models/gemini-1.5-flash-latest:streamGenerateContent?key=${apiKey}`;

    (async () => {
      try {
        const response = await fetch(API_URL, {
          method: 'POST',
          headers: {
            'Content-Type': 'application/json',
          },
          body: JSON.stringify({
            contents: [{ parts: [{ text: prompt }] }],
          }),
        });

        if (!response.ok) {
          throw new Error(
            `API Error: ${response.status} ${response.statusText}`
          );
        }

        const reader = response.body.getReader();
        const decoder = new TextDecoder();

        while (true) {
          const { done, value } = await reader.read();
          if (done) {
            break;
          }
          // Decode the chunk and process it
          const chunk = decoder.decode(value);
          const lines = chunk.split('\n');
          for (const line of lines) {
            if (line.startsWith('data: ')) {
              try {
                const jsonStr = line.substring(6); // Remove "data: "
                const data = JSON.parse(jsonStr);
                const text = data.candidates?.[0]?.content?.parts?.[0]?.text;
                if (text) {
                  chrome.runtime.sendMessage({
                    type: 'geminiResponseChunk',
                    chunk: text,
                  });
                }
              } catch (e) {
                // Ignore parsing errors for incomplete JSON
              }
            }
          }
        }
      } catch (error) {
        chrome.runtime.sendMessage({
          type: 'geminiResponseError',
          error: error.message,
        });
      }
    })();
    return true; // Keep the message channel open for async streaming
  }
});
