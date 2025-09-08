let overlay = null
let currentShortcut = []
let keydownListener = null
const chrome = window.chrome // Declare chrome variable

// Helper function to normalize key names for comparison
const normalizeKey = (key) => {
  if (key === "\\") return "Backslash"
  if (key === " ") return "Space"
  if (key === "Control") return "Control"
  if (key === "Shift") return "Shift"
  if (key === "Alt") return "Alt"
  if (key.length === 1) return key.toUpperCase()
  return key
}

// Helper function to create and show the search overlay
const createOverlay = () => {
  try {
    if (overlay && overlay.parentNode) {
      overlay.parentNode.removeChild(overlay)
    }
    overlay = null

    overlay = document.createElement("div")
    overlay.id = "zeromix-overlay"
    overlay.innerHTML = `
            <div class="zeromix-box">
                <input type="text" id="zeromix-input" placeholder="Ketik untuk mencari..." autocomplete="off" />
            </div>
        `
    document.body.appendChild(overlay)

    const input = document.getElementById("zeromix-input")
    if (input) {
      input.focus()

      const handleKeydown = (e) => {
        try {
          if (e.key === "Escape") {
            removeOverlay()
          }
          if (e.key === "Enter") {
            const query = input.value.trim()
            if (query) {
              window.open("https://www.google.com/search?q=" + encodeURIComponent(query), "_blank")
            }
            removeOverlay()
          }
          e.stopPropagation()
        } catch (error) {
          console.error("ZeroMix: Error handling overlay keydown:", error)
          removeOverlay()
        }
      }

      input.addEventListener("keydown", handleKeydown)

      overlay.addEventListener("click", (e) => {
        if (e.target === overlay) {
          removeOverlay()
        }
      })
    }
  } catch (error) {
    console.error("ZeroMix: Error creating overlay:", error)
  }
}

const removeOverlay = () => {
  try {
    if (overlay && overlay.parentNode) {
      overlay.parentNode.removeChild(overlay)
    }
    overlay = null
  } catch (error) {
    console.error("ZeroMix: Error removing overlay:", error)
  }
}

// Function to reliably match the keyboard event with the stored shortcut
const matchShortcut = (e, shortcut) => {
  try {
    // If the event is from the overlay input, don't trigger the shortcut
    if (overlay && overlay.contains(e.target)) {
      return false
    }

    const pressedKeys = []
    if (e.ctrlKey) pressedKeys.push("Control")
    if (e.shiftKey) pressedKeys.push("Shift")
    if (e.altKey) pressedKeys.push("Alt")

    const key = normalizeKey(e.key)
    // Add the primary key, but only if it's not already a modifier in pressedKeys
    if (!pressedKeys.includes(key)) {
      pressedKeys.push(key)
    }

    const sortedPressed = [...pressedKeys].sort().join("+")
    const sortedShortcut = [...shortcut].sort().join("+")

    return sortedPressed === sortedShortcut
  } catch (error) {
    console.error("ZeroMix: Error matching shortcut:", error)
    return false
  }
}

// Function to load shortcut and set up the listener
const loadAndListen = (window.loadAndListen = () => {
  // Declare loadAndListen variable
  try {
    if (keydownListener) {
      document.removeEventListener("keydown", keydownListener)
      keydownListener = null
    }

    chrome.storage.sync.get("shortcut", (data) => {
      try {
        if (chrome.runtime.lastError) {
          console.error("ZeroMix: Storage error:", chrome.runtime.lastError)
          currentShortcut = ["Control", "Backslash"]
        } else if (data.shortcut && Array.isArray(data.shortcut) && data.shortcut.length > 0) {
          currentShortcut = [...data.shortcut] // Create a copy to avoid reference issues
        } else {
          currentShortcut = ["Control", "Backslash"]
        }

        keydownListener = (e) => {
          try {
            if (matchShortcut(e, currentShortcut)) {
              e.preventDefault()
              e.stopPropagation()
              createOverlay()
            }
          } catch (error) {
            console.error("ZeroMix: Error in keydown listener:", error)
          }
        }

        document.addEventListener("keydown", keydownListener)
      } catch (error) {
        console.error("ZeroMix: Error processing storage data:", error)
      }
    })
  } catch (error) {
    console.error("ZeroMix: Error in loadAndListen:", error)
  }
})

try {
  loadAndListen()
} catch (error) {
  console.error("ZeroMix: Error during initialization:", error)
}

// Listen for messages from the background script to reload the shortcut
chrome.runtime.onMessage.addListener((request, sender, sendResponse) => {
  try {
    if (request.action === "updateShortcut") {
      console.log("ZeroMix: Content script received updateShortcut message. Reloading listener.")
      window.loadAndListen()
      sendResponse({ status: "listener reloaded" })
    }
  } catch (error) {
    console.error("ZeroMix: Error handling runtime message:", error)
    sendResponse({ status: "error", error: error.message })
  }
})
