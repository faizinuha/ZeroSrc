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

  // --- Handler for Gemini Prompt ---
  if (message.type === 'promptGemini') {
    const { apiKey, prompt, file } = message;
    if (!apiKey) {
      chrome.runtime.sendMessage({
        type: 'geminiResponseError',
        error: 'API Key is not set.',
      });
      return false; // No async response needed
    }

    // Updated API URL and model name
    const API_URL = `https://generativelanguage.googleapis.com/v1beta/models/gemini-1.5-flash-latest:generateContent?key=${apiKey}`;

    const contents = [{ parts: [{ text: prompt || 'Jelaskan file ini' }] }];

    // Jika ada file, tambahkan ke payload
    if (file && file.base64 && file.mimeType) {
      contents[0].parts.push({
        inline_data: {
          mime_type: file.mimeType,
          data: file.base64,
        },
      });
    }
    (async () => {
      try {
        const response = await fetch(API_URL, {
          method: 'POST',
          headers: {
            'Content-Type': 'application/json',
            // The API key is in the URL, so X-goog-api-key header is not needed here
          },
          body: JSON.stringify({
            contents: contents,
          }),
        });

        const data = await response.json();

        if (data.error) {
          throw new Error(data.error.message);
        }

        // Extract the text from the non-streaming response
        const responseText = data.candidates?.[0]?.content?.parts?.[0]?.text;

        if (responseText) {
          // Send the full response as a single chunk
          chrome.runtime.sendMessage({
            type: 'geminiResponseChunk',
            chunk: responseText,
          });
        } else {
          throw new Error('No content received from API.');
        }
      } catch (error) {
        chrome.runtime.sendMessage({
          type: 'geminiResponseError',
          error: error.message,
        });
      }
    })();
    return true; // Keep the message channel open for the async response
  }

  // --- Handler for OpenAI Prompt ---
  if (message.type === 'promptOpenAI') {
    const { apiKey, prompt, file } = message;
    if (!apiKey) {
      chrome.runtime.sendMessage({
        type: 'openaiResponseError',
        error: 'API Key is not set.',
      });
      return false; // No async response needed
    }

    const API_URL = 'https://api.openai.com/v1/chat/completions';

    // Siapkan payload dasar
    const messages = [
      {
        role: 'user',
        content: [{ type: 'text', text: prompt || 'Jelaskan gambar ini' }],
      },
    ];

    // Jika ada file (dan itu gambar), tambahkan ke payload
    // CATATAN: gpt-3.5-turbo tidak mendukung ini. Anda perlu gpt-4o atau gpt-4-vision-preview
    if (file && file.base64 && file.mimeType.startsWith('image/')) {
      messages[0].content.push({
        type: 'image_url',
        image_url: {
          url: `data:${file.mimeType};base64,${file.base64}`,
        },
      });
    }

    (async () => {
      try {
        const response = await fetch(API_URL, {
          method: 'POST',
          headers: {
            'Content-Type': 'application/json',
            Authorization: `Bearer ${apiKey}`,
          },
          body: JSON.stringify({
            model: 'gpt-3.5-turbo', // Or any other model you prefer
            messages: messages,
            stream: false,
          }),
        });

        const data = await response.json();

        if (data.error) {
          throw new Error(data.error.message);
        }

        const responseText = data.choices?.[0]?.message?.content;

        if (responseText) {
          chrome.runtime.sendMessage({
            type: 'openaiResponseChunk',
            chunk: responseText,
          });
        } else {
          throw new Error('No content received from API.');
        }
      } catch (error) {
        chrome.runtime.sendMessage({
          type: 'openaiResponseError',
          error: error.message,
        });
      }
    })();
    return true; // Keep the message channel open for the async response
  }
});
