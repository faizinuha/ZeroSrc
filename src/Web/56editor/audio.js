/* ═══════════════════════════════════════════════════════════
   56Editor — Audio Module
   Handles: Pixabay music search, preview, add to timeline
   API key is injected by the host app (C# WebView2 bridge)
═══════════════════════════════════════════════════════════ */

'use strict';

// Pixabay API key — injected by host via window.PIXABAY_KEY
// or falls back to a placeholder that shows a helpful error
const PIXABAY_BASE = 'https://pixabay.com/api/videos/';

let audioPreviewClip = null; // currently previewing audio item

// ── DOM refs ───────────────────────────────────────────────
const audioSearch    = document.getElementById('audio-search');
const btnAudioSearch = document.getElementById('btn-audio-search');
const audioList      = document.getElementById('audio-list');
const audioPlayerMini = document.getElementById('audio-player-mini');
const audioPreview   = document.getElementById('audio-preview');
const btnAudioPlay   = document.getElementById('btn-audio-play');
const btnAudioAdd    = document.getElementById('btn-audio-add');
const audioPreviewTitle = document.getElementById('audio-preview-title');

// ── Search ─────────────────────────────────────────────────
btnAudioSearch.addEventListener('click', doSearch);
audioSearch.addEventListener('keydown', e => { if (e.key === 'Enter') doSearch(); });

async function doSearch() {
  const q = audioSearch.value.trim();
  if (!q) return;

  const key = window.PIXABAY_KEY || '';
  if (!key) {
    audioList.innerHTML = `<div class="empty-hint" style="color:#ff4444">
      <i class="fas fa-exclamation-triangle"></i>
      <p>Pixabay API key not set.<br>Host app must inject window.PIXABAY_KEY.</p>
    </div>`;
    return;
  }

  audioList.innerHTML = `<div class="empty-hint"><i class="fas fa-spinner fa-spin"></i><p>Searching...</p></div>`;

  try {
    const url = `${PIXABAY_BASE}?key=${key}&q=${encodeURIComponent(q)}&category=music&per_page=20`;
    const res = await fetch(url);
    if (!res.ok) throw new Error(`HTTP ${res.status}`);
    const data = await res.json();

    if (!data.hits?.length) {
      audioList.innerHTML = ""; const nh=document.createElement("div"); nh.className="empty-hint"; nh.innerHTML="<i class=\"fas fa-search\"></i>"; const np=document.createElement("p"); np.textContent=`No results for "${q}"`; nh.appendChild(np); audioList.appendChild(nh);
      return;
    }

    renderAudioResults(data.hits);
  } catch (err) {
    audioList.innerHTML = "";
    const eh=document.createElement("div"); eh.className="empty-hint"; eh.style.color="#ff4444";
    eh.innerHTML='<i class="fas fa-exclamation-circle"></i>';
    const ep=document.createElement("p"); ep.textContent=`Search failed: ${err.message}`; eh.appendChild(ep); audioList.appendChild(eh);
  }
}

function renderAudioResults(hits) {
  audioList.innerHTML = '';
  hits.forEach(hit => {
    // Pixabay video API — use the smallest video as audio source
    const audioUrl = hit.videos?.tiny?.url || hit.videos?.small?.url || '';
    const dur = hit.duration || 0;
    const title = hit.tags?.split(',')[0]?.trim() || `Track ${hit.id}`;
    const user  = hit.user || 'Unknown';

    const item = document.createElement('div');
    item.className = 'audio-item';
    item.innerHTML = `
      <button class="audio-item-play" title="Preview"><i class="fas fa-play"></i></button>
      <div class="audio-item-info">
        <div class="audio-item-title">${title}</div>
        <div class="audio-item-meta">${user} · ${fmtTime(dur)}</div>
      </div>`;

    item.querySelector('.audio-item-play').addEventListener('click', () => {
      previewAudio({ url: audioUrl, title, duration: dur });
    });
    item.addEventListener('dblclick', () => {
      previewAudio({ url: audioUrl, title, duration: dur });
      addAudioToTimeline({ url: audioUrl, title, duration: dur });
    });

    audioList.appendChild(item);
  });
}

// ── Preview ────────────────────────────────────────────────
function previewAudio({ url, title, duration }) {
  audioPreviewClip = { url, title, duration };
  audioPreviewTitle.textContent = title;
  audioPreview.src = url;
  audioPreview.load();
  audioPreview.play();
  btnAudioPlay.innerHTML = '<i class="fas fa-pause"></i>';
  audioPlayerMini.style.display = '';
}

btnAudioPlay.addEventListener('click', () => {
  if (audioPreview.paused) {
    audioPreview.play();
    btnAudioPlay.innerHTML = '<i class="fas fa-pause"></i>';
  } else {
    audioPreview.pause();
    btnAudioPlay.innerHTML = '<i class="fas fa-play"></i>';
  }
});

audioPreview.addEventListener('ended', () => {
  btnAudioPlay.innerHTML = '<i class="fas fa-play"></i>';
});

// ── Add to timeline ────────────────────────────────────────
btnAudioAdd.addEventListener('click', () => {
  if (!audioPreviewClip) return;
  addAudioToTimeline(audioPreviewClip);
});

function addAudioToTimeline({ url, title, duration }) {
  // addClipToTimeline is defined in editor.js
  if (typeof addClipToTimeline === 'function') {
    addClipToTimeline({ type: 'audio', name: title, src: url, duration, volume: 0.8 });
    toast(`Audio added: ${title}`, 'success');
  }
}

// ── Helpers ────────────────────────────────────────────────
function fmtTime(sec) {
  const m = Math.floor(sec / 60);
  const s = Math.floor(sec % 60);
  return `${m}:${s.toString().padStart(2, '0')}`;
}
