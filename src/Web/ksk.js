// Fungsi untuk beralih ke ZeroMix (Default)
async function switchToZeroMix() {
    updateHeader("Changelog", "Riwayat Update dan Perkembangan ZeroMix");
    setActiveButton("btn-zeromix");
    
    const zeroMixApi = "https://api.github.com/repos/faizinuha/ZeroMix-Win64Bit/releases";
    await fetchAndUpdateTimeline(zeroMixApi, "ZeroMix Update");
}

// Fungsi untuk beralih ke Ksk Project
async function switchToKsk() {
    updateHeader("Ksk Project - Changelog", "Riwayat Update Kurohiko Stream Kit AIO");
    setActiveButton("btn-ksk");
    
    const kskApi = "https://api.github.com/repos/faizinuha/Kurohiko-Stream-kit-AIO-Fork/releases";
    await fetchAndUpdateTimeline(kskApi, "Ksk Project");
}

// HELPER: Update Header Text
function updateHeader(title, desc) {
    const h1 = document.querySelector(".header-content h1");
    const p = document.querySelector(".header-content p");
    if (h1) h1.textContent = title;
    if (p) p.textContent = desc;
    
    // Sembunyikan p kedua yang statis agar bersih
    const p2 = document.querySelectorAll(".header-content p")[1];
    if (p2) p2.style.display = "none";
}

// HELPER: Highlight button aktif
function setActiveButton(btnId) {
    document.querySelectorAll(".nav-btn").forEach(btn => btn.classList.remove("active"));
    const activeBtn = document.getElementById(btnId);
    if (activeBtn) activeBtn.classList.add("active");
}

// HELPER: Ambil data GitHub & Update UI
async function fetchAndUpdateTimeline(apiUrl, tagLabel) {
    console.log("Fetching from:", apiUrl);
    const timeline = document.querySelector(".changelog-timeline");
    timeline.innerHTML = '<p style="text-align: center;">Mengambil data... 🔄</p>';

    try {
        const response = await fetch(apiUrl);
        if (!response.ok) throw new Error("Gagal ambil data");
        const releases = await response.json();
        
        timeline.innerHTML = "";
        if (!window.releaseAssets) window.releaseAssets = {};

        releases.forEach(release => {
            if (!release.tag_name) return;
            window.releaseAssets[release.id] = release.assets;

            let body = (release.body || "No description.")
                .replace(/\r\n/g, "<br>")
                .replace(/### (.*)/g, "<h4>$1</h4>")
                .replace(/\*\*([^*]+)\*\*/g, "<strong>$1</strong>");

            let btn = "";
            if (release.assets && release.assets.length > 0) {
              if (release.assets.length === 1) {
                btn = `<a href="${release.assets[0].browser_download_url}" target="_blank" class="btn">Download v${release.tag_name}</a>`;
              } else {
                btn = `<button class="btn" onclick="showAssetsSidebar('${release.id}')">View ${release.assets.length} Assets</button>`;
              }
            }

            const item = document.createElement("div");
            item.classList.add("timeline-item");
            item.innerHTML = `
                <div class="timeline-marker"><span class="version-badge">${release.tag_name}</span></div>
                <div class="timeline-content card">
                    <h3>${release.name || release.tag_name}</h3>
                    <div class="changelog-tag tag-update">${tagLabel}</div>
                    <p style="font-size: 0.85em; color: #888;">${new Date(release.published_at).toLocaleDateString()}</p>
                    <div style="margin: 15px 0;"><ul>${body}</ul></div>
                    <div class="release-actions">${btn}</div>
                </div>`;
            timeline.appendChild(item);
        });
        window.scrollTo({ top: 100, behavior: 'smooth' });
    } catch (e) {
        timeline.innerHTML = '<p style="text-align: center; color: red;">Gagal memuat data.</p>';
    }
}