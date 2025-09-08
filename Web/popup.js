document.addEventListener("DOMContentLoaded", () => {
  // DOM Elements
  const shortcutDisplay = document.getElementById("currentShortcut")
  const recorder = document.getElementById("recorder")
  const recordedKeysEl = document.getElementById("recordedKeys")
  const changeShortcutBtn = document.getElementById("changeShortcut")
  const saveShortcutBtn = document.getElementById("saveShortcut")
  const cancelShortcutBtn = document.getElementById("cancelShortcut")
  const advancedBtn = document.getElementById("advancedBtn")

  // State Variables
  let currentShortcut = ["Control", "Backslash"]
  let recordedKeys = []
  let isRecording = false

  // Helper function to normalize key names for display
  const normalizeKey = (key) => {
    if (key === "\\") return "Backslash"
    if (key === " ") return "Space"
    if (key.length === 1) return key.toUpperCase()
    return key
  }

  // Helper function to update the displayed shortcut
  const updateShortcutDisplay = (shortcutArray) => {
    try {
      if (shortcutArray && shortcutArray.length > 0) {
        shortcutDisplay.textContent = shortcutArray.map(normalizeKey).join(" + ")
      } else {
        shortcutDisplay.textContent = "Not Set"
      }
    } catch (error) {
      console.error("ZeroMix: Error updating shortcut display:", error)
      shortcutDisplay.textContent = "Error"
    }
  }

  const isValidShortcut = (keys) => {
    if (!Array.isArray(keys) || keys.length === 0) return false

    // Must have at least one modifier key
    const modifiers = ["Control", "Shift", "Alt"]
    const hasModifier = keys.some((key) => modifiers.includes(key))

    // Must have at least one non-modifier key
    const hasNonModifier = keys.some((key) => !modifiers.includes(key))

    return hasModifier && hasNonModifier
  }

  const loadShortcut = () => {
    try {
      window.chrome.storage.sync.get("shortcut", (data) => {
        if (window.chrome.runtime.lastError) {
          console.error("ZeroMix: Error loading shortcut:", window.chrome.runtime.lastError)
          setDefaultShortcut()
          return
        }

        if (data.shortcut && Array.isArray(data.shortcut) && data.shortcut.length > 0) {
          currentShortcut = [...data.shortcut] // Create copy to avoid reference issues
          updateShortcutDisplay(currentShortcut)
        } else {
          setDefaultShortcut()
        }
      })
    } catch (error) {
      console.error("ZeroMix: Error in loadShortcut:", error)
      setDefaultShortcut()
    }
  }

  const setDefaultShortcut = () => {
    currentShortcut = ["Control", "Backslash"]
    window.chrome.storage.sync.set({ shortcut: currentShortcut }, () => {
      if (window.chrome.runtime.lastError) {
        console.error("ZeroMix: Error setting default shortcut:", window.chrome.runtime.lastError)
      }
      updateShortcutDisplay(currentShortcut)
    })
  }

  // Load saved shortcut from storage on popup open
  loadShortcut()

  // Event listener for "Ganti Shortcut" button
  changeShortcutBtn.addEventListener("click", () => {
    try {
      isRecording = true
      recordedKeys = []
      recorder.classList.remove("hidden")
      recordedKeysEl.textContent = "Press key combination..."
    } catch (error) {
      console.error("ZeroMix: Error starting recording:", error)
    }
  })

  // Event listener for "Batal" button
  cancelShortcutBtn.addEventListener("click", () => {
    try {
      isRecording = false
      recorder.classList.add("hidden")
      recordedKeys = []
    } catch (error) {
      console.error("ZeroMix: Error canceling recording:", error)
    }
  })

  // Event listener for "Simpan" button
  saveShortcutBtn.addEventListener("click", () => {
    try {
      if (recordedKeys.length > 0 && isValidShortcut(recordedKeys)) {
        currentShortcut = [...recordedKeys] // Create copy
        window.chrome.storage.sync.set({ shortcut: currentShortcut }, () => {
          if (window.chrome.runtime.lastError) {
            console.error("ZeroMix: Error saving shortcut:", window.chrome.runtime.lastError)
            return
          }

          updateShortcutDisplay(currentShortcut)
          console.log("ZeroMix: Shortcut saved:", currentShortcut)

          window.chrome.runtime.sendMessage({ action: "updateShortcut" }, (response) => {
            if (window.chrome.runtime.lastError) {
              console.error("ZeroMix: Error sending update message:", window.chrome.runtime.lastError)
            }
          })
        })
      } else {
        recordedKeysEl.textContent = "Invalid shortcut! Use modifier + key"
        setTimeout(() => {
          recordedKeysEl.textContent = "Press key combination..."
        }, 2000)
        return
      }

      isRecording = false
      recorder.classList.add("hidden")
    } catch (error) {
      console.error("ZeroMix: Error saving shortcut:", error)
    }
  })

  advancedBtn.addEventListener("click", () => {
    try {
      // Open Chrome extensions page for advanced management
      window.chrome.tabs.create({ url: "chrome://extensions/" })
      window.close()
    } catch (error) {
      console.error("ZeroMix: Error opening advanced settings:", error)
      window.close()
    }
  })

  // Global keydown listener for recording shortcuts
  document.addEventListener("keydown", (e) => {
    if (!isRecording) return

    try {
      e.preventDefault()
      e.stopPropagation()

      const key = e.key
      const normalizedKey = normalizeKey(key)

      const tempKeys = []
      if (e.ctrlKey) tempKeys.push("Control")
      if (e.shiftKey) tempKeys.push("Shift")
      if (e.altKey) tempKeys.push("Alt")

      // Add the primary key if it's not a modifier already
      if (!["Control", "Shift", "Alt"].includes(normalizedKey)) {
        tempKeys.push(normalizedKey)
      }

      recordedKeys = [...new Set(tempKeys)]

      if (recordedKeys.length > 0) {
        recordedKeysEl.textContent = recordedKeys.map(normalizeKey).join(" + ")
      } else {
        recordedKeysEl.textContent = "Press key combination..."
      }
    } catch (error) {
      console.error("ZeroMix: Error recording keys:", error)
      recordedKeysEl.textContent = "Error recording keys"
    }
  })
})
