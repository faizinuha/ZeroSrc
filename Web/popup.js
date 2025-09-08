document.addEventListener('DOMContentLoaded', () => {
    // DOM Elements
    const shortcutDisplay = document.getElementById("currentShortcut");
    const recorder = document.getElementById("recorder");
    const recordedKeysEl = document.getElementById("recordedKeys");
    const changeShortcutBtn = document.getElementById("changeShortcut");
    const saveShortcutBtn = document.getElementById("saveShortcut");
    const cancelShortcutBtn = document.getElementById("cancelShortcut");
    const advancedBtn = document.getElementById("advancedBtn");

    // State Variables
    let currentShortcut = ["Control", "Backslash"]; // Default shortcut
    let recordedKeys = [];
    let isRecording = false;

    // Helper function to normalize key names for display
    const normalizeKey = (key) => {
        if (key === "\\") return "Backslash";
        if (key === " ") return "Space";
        if (key.length === 1) return key.toUpperCase();
        return key;
    };
    
    // Helper function to update the displayed shortcut
    const updateShortcutDisplay = (shortcutArray) => {
        if (shortcutArray && shortcutArray.length > 0) {
            shortcutDisplay.textContent = shortcutArray.map(normalizeKey).join(" + ");
        } else {
            shortcutDisplay.textContent = "Not Set"; // Fallback if shortcut is empty
        }
    };

    // Load saved shortcut from storage on popup open
    chrome.storage.sync.get("shortcut", (data) => {
        if (data.shortcut && Array.isArray(data.shortcut) && data.shortcut.length > 0) {
            currentShortcut = data.shortcut;
            updateShortcutDisplay(currentShortcut);
        } else {
            // Set initial default shortcut if none is found
            chrome.storage.sync.set({ shortcut: currentShortcut }, () => {
                updateShortcutDisplay(currentShortcut);
            });
        }
    });

    // Event listener for "Ganti Shortcut" button
    changeShortcutBtn.addEventListener("click", () => {
        isRecording = true;
        recordedKeys = []; // Reset the recorded keys for a new recording
        recorder.classList.remove("hidden");
        recordedKeysEl.textContent = "Press key combination...";
    });

    // Event listener for "Batal" button
    cancelShortcutBtn.addEventListener("click", () => {
        isRecording = false;
        recorder.classList.add("hidden");
        recordedKeys = []; // Clear recorded keys if canceled
    });

    // Event listener for "Simpan" button
    saveShortcutBtn.addEventListener("click", () => {
        if (recordedKeys.length > 0) {
            currentShortcut = recordedKeys;
            chrome.storage.sync.set({ shortcut: currentShortcut }, () => {
                updateShortcutDisplay(currentShortcut);
                console.log("Shortcut saved:", currentShortcut);
                // Inform background script that shortcut has been updated
                chrome.runtime.sendMessage({ action: "updateShortcut" });
            });
        }
        isRecording = false;
        recorder.classList.add("hidden");
    });
    
    // Event listener for "Advanced" button
    advancedBtn.addEventListener("click", () => {
        // This button can now be used for other advanced features if needed.
        // For now, we'll just close the popup.
        window.close();
    });

    // Global keydown listener for recording shortcuts
    document.addEventListener("keydown", (e) => {
        if (!isRecording) return;
        
        e.preventDefault(); // Prevent default browser actions for the recorded keys
        e.stopPropagation(); // Stop propagation to prevent interference with other listeners

        const key = e.key;
        const normalizedKey = normalizeKey(key);

        let tempKeys = [];
        if (e.ctrlKey) tempKeys.push("Control");
        if (e.shiftKey) tempKeys.push("Shift");
        if (e.altKey) tempKeys.push("Alt");
        
        // Add the primary key if it's not a modifier already
        if (!["Control", "Shift", "Alt"].includes(normalizedKey)) {
            tempKeys.push(normalizedKey);
        }

        // Remove duplicates and update the displayed keys
        recordedKeys = Array.from(new Set(tempKeys));
        recordedKeysEl.textContent = recordedKeys.map(normalizeKey).join(" + ");
    });
});