let overlay = null;
let currentShortcut = [];
let keydownListener = null; // To store the keydown listener so we can remove it

// Helper function to normalize key names for comparison
const normalizeKey = (key) => {
    if (key === "\\") return "Backslash";
    if (key === " ") return "Space";
    if (key === "Control") return "Control";
    if (key === "Shift") return "Shift";
    if (key === "Alt") return "Alt";
    if (key.length === 1) return key.toUpperCase();
    return key;
};

// Helper function to create and show the search overlay
const createOverlay = () => {
  if (overlay) {
    document.body.removeChild(overlay); // Remove existing overlay if any
    overlay = null;
  }
  
  overlay = document.createElement("div");
  overlay.id = "zeromix-overlay";
  overlay.innerHTML = `
    <div class="zeromix-box">
      <input type="text" id="zeromix-input" placeholder="Ketik untuk mencari..." autocomplete="off" />
    </div>
  `;
  document.body.appendChild(overlay);

  const input = document.getElementById("zeromix-input");
  input.focus();

  // Event listener to close the overlay with Escape or Enter
  input.addEventListener("keydown", (e) => {
    if (e.key === "Escape") {
      overlay.remove();
      overlay = null;
    }
    if (e.key === "Enter") {
      const query = input.value.trim();
      if (query) {
        window.open("https://www.google.com/search?q=" + encodeURIComponent(query), "_blank");
      }
      overlay.remove();
      overlay = null;
    }
    e.stopPropagation(); // Prevent overlay keydowns from propagating
  });
};

// Function to reliably match the keyboard event with the stored shortcut
const matchShortcut = (e, shortcut) => {
    // If the event is from the overlay input, don't trigger the shortcut
    if (overlay && overlay.contains(e.target)) {
        return false;
    }

    const pressedKeys = [];
    if (e.ctrlKey) pressedKeys.push("Control");
    if (e.shiftKey) pressedKeys.push("Shift");
    if (e.altKey) pressedKeys.push("Alt");
    
    const key = normalizeKey(e.key);
    // Add the primary key, but only if it's not already a modifier in pressedKeys
    if (!pressedKeys.includes(key)) {
        pressedKeys.push(key);
    }
    
    // Sort both arrays to ensure order doesn't matter for comparison
    const sortedPressed = pressedKeys.sort().join("+");
    const sortedShortcut = shortcut.sort().join("+");

    return sortedPressed === sortedShortcut;
};

// Function to load shortcut and set up the listener
window.loadAndListen = () => {
    // Remove existing listener to prevent duplicates if called multiple times
    if (keydownListener) {
        document.removeEventListener("keydown", keydownListener);
    }

    chrome.storage.sync.get("shortcut", (data) => {
        if (data.shortcut && Array.isArray(data.shortcut) && data.shortcut.length > 0) {
            currentShortcut = data.shortcut;
        } else {
            // Use default if no shortcut is found (should be set by background.js)
            currentShortcut = ["Control", "Backslash"];
        }
        
        // Define the new keydown listener
        keydownListener = (e) => {
            if (matchShortcut(e, currentShortcut)) {
                e.preventDefault();
                e.stopPropagation(); // Stop propagation to prevent other elements from reacting
                createOverlay();
            }
        };

        // Add the new listener
        document.addEventListener("keydown", keydownListener);
    });
};

// Initialize the script
loadAndListen();

// Listen for messages from the background script to reload the shortcut
chrome.runtime.onMessage.addListener((request, sender, sendResponse) => {
  if (request.action === "updateShortcut") {
    console.log("Content script received updateShortcut message. Reloading listener.");
    window.loadAndListen(); // Reload the shortcut and listener
    sendResponse({ status: "listener reloaded" });
  }
});