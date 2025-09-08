

chrome.runtime.onInstalled.addListener(() => {
  try {
    chrome.storage.sync.get("shortcut", (data) => {
      if (chrome.runtime.lastError) {
        console.error("ZeroMix: Error checking existing shortcut:", chrome.runtime.lastError)
        setDefaultShortcut()
        return
      }

      if (!data.shortcut || !Array.isArray(data.shortcut) || data.shortcut.length === 0) {
        setDefaultShortcut()
      }
    })
  } catch (error) {
    console.error("ZeroMix: Error in onInstalled listener:", error)
  }
})

const setDefaultShortcut = () => {
  chrome.storage.sync.set({ shortcut: ["Control", "Backslash"] }, () => {
    if (chrome.runtime.lastError) {
      console.error("ZeroMix: Error setting default shortcut:", chrome.runtime.lastError)
    } else {
      console.log("ZeroMix: Default shortcut set")
    }
  })
}

chrome.runtime.onMessage.addListener((request, sender, sendResponse) => {
  try {
    if (request.action === "updateShortcut") {
      updateContentScripts()
        .then(() => {
          sendResponse({ status: "shortcut updated and content scripts reloaded" })
        })
        .catch((error) => {
          console.error("ZeroMix: Error updating content scripts:", error)
          sendResponse({ status: "error", error: error.message })
        })
      return true // Indicates an asynchronous response
    }
  } catch (error) {
    console.error("ZeroMix: Error in message listener:", error)
    sendResponse({ status: "error", error: error.message })
  }
})

const updateContentScripts = async () => {
  try {
    const tabs = await chrome.tabs.query({})
    const updatePromises = tabs
      .filter((tab) => tab.url && (tab.url.startsWith("http://") || tab.url.startsWith("https://")))
      .map(async (tab) => {
        try {
          await chrome.scripting.executeScript({
            target: { tabId: tab.id },
            function: () => {
              if (typeof window.loadAndListen === "function") {
                window.loadAndListen()
              }
            },
          })
        } catch (error) {
          console.warn(`ZeroMix: Failed to update tab ${tab.id}:`, error.message)
        }
      })

    await Promise.allSettled(updatePromises)
    console.log("ZeroMix: Content script update completed")
  } catch (error) {
    console.error("ZeroMix: Error in updateContentScripts:", error)
    throw error
  }
}
