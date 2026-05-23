/* ═══════════════════════════════════════════════════════════
   56Editor — Core JavaScript
   Handles: media upload, timeline, playback, cut/trim, filters
═══════════════════════════════════════════════════════════ */

'use strict';

// ── State ──────────────────────────────────────────────────
const state = {
  clips: [],          // { id, type, name, src, start, end, trackStart, duration, filter, transition, fadeIn, fadeOut, speed, volume }
  selectedId: null,
  playing: false,
  currentTime: 0,
  totalDuration: 0,
  zoom: 1,            // px per second
  pxPerSec: 80,
  activeFilter: '',
  activeTransition: 'none',
  rafId: null,
};

let clipIdCounter = 0;
const newId = () => `clip_${++clipIdCounter}`;

// ── DOM refs ───────────────────────────────────────────────
const $ = id => document.getElementById(id);
const video       = $('preview-video');
const canvas      = $('preview-canvas');
const ctx         = canvas.getContext('2d');
const previewEmpty = $('preview-empty');
const textOverlay = $('text-overlay');
const playhead    = $('playhead');
const trackVideo  = $('track-video');
const trackAudio  = $('track-audio');
const trackText   = $('track-text');
const ruler       = $('timeline-ruler');
const btnPlay     = $('btn-play');
const btnExport   = $('btn-export');
const btnCut      = $('btn-cut');
const btnDelete   = $('btn-delete');
const btnSplit    = $('btn-split');
const currentTimeEl = $('current-time');
const totalTimeEl   = $('total-time');
const zoomLevelEl   = $('zoom-level');

// ── Utilities ──────────────────────────────────────────────
function toast(msg, type = '') {
  const el = $('toast');
  el.textContent = msg;
  el.className = `toast show ${type}`;
  clearTimeout(el._t);
  el._t = setTimeout(() => el.className = 'toast', 2500);
}

function fmtTime(sec) {
  const m = Math.floor(sec / 60);
  const s = Math.floor(sec % 60);
  return `${m}:${s.toString().padStart(2, '0')}`;
}

function recalcDuration() {
  state.totalDuration = state.clips.reduce((max, c) => Math.max(max, c.trackStart + c.duration), 0);
  totalTimeEl.textContent = fmtTime(state.totalDuration);
  renderRuler();
  updateToolbarState();
}

// ── Panel switching ────────────────────────────────────────
document.querySelectorAll('.tool-btn').forEach(btn => {
  btn.addEventListener('click', () => {
    document.querySelectorAll('.tool-btn').forEach(b => b.classList.remove('active'));
    document.querySelectorAll('.side-panel').forEach(p => p.classList.remove('active'));
    btn.classList.add('active');
    $(`panel-${btn.dataset.panel}`).classList.add('active');
  });
});

// ── Upload ─────────────────────────────────────────────────
const uploadZone = $('upload-zone');
const fileInput  = $('file-input');

uploadZone.addEventListener('click', () => fileInput.click());
uploadZone.addEventListener('dragover', e => { e.preventDefault(); uploadZone.classList.add('drag-over'); });
uploadZone.addEventListener('dragleave', () => uploadZone.classList.remove('drag-over'));
uploadZone.addEventListener('drop', e => {
  e.preventDefault();
  uploadZone.classList.remove('drag-over');
  handleFiles([...e.dataTransfer.files].filter(f => f.type.startsWith('video/')));
});
fileInput.addEventListener('change', () => handleFiles([...fileInput.files]));

function handleFiles(files) {
  files.forEach(file => {
    const url = URL.createObjectURL(file);
    const tmpVid = document.createElement('video');
    tmpVid.src = url;
    tmpVid.preload = 'metadata';
    tmpVid.onloadedmetadata = () => {
      const dur = tmpVid.duration;
      addMediaItem({ name: file.name, src: url, duration: dur });
      tmpVid.remove();
    };
  });
}

function isSafeMediaSrc(src) {
  try {
    const parsed = new URL(src, window.location.href);
    return parsed.protocol === 'blob:';
  } catch (_) {
    return false;
  }
}

function addMediaItem({ name, src, duration }) {
  const list = $('media-list');
  const item = document.createElement('div');
  item.className = 'media-item';

  // Generate thumbnail
  const thumb = document.createElement('img');
  thumb.alt = name;
  const tmpV = document.createElement('video');
  if (!isSafeMediaSrc(src)) {
    toast('Invalid media source', 'error');
    return;
  }
  tmpV.src = src;
  tmpV.currentTime = 0.5;
  tmpV.onloadeddata = () => {
    const c = document.createElement('canvas');
    c.width = 80; c.height = 45;
    c.getContext('2d').drawImage(tmpV, 0, 0, 80, 45);
    thumb.src = c.toDataURL();
  };

  const info = document.createElement('div'); info.className = 'media-item-info';
  const nameEl = document.createElement('div'); nameEl.className = 'media-item-name'; nameEl.textContent = name;
  const durEl  = document.createElement('div'); durEl.className  = 'media-item-dur';  durEl.textContent = fmtTime(duration);
  info.appendChild(nameEl); info.appendChild(durEl);
  const addBtn = document.createElement('button'); addBtn.className = 'media-item-add'; addBtn.title = 'Add to timeline';
  addBtn.innerHTML = '<i class="fas fa-plus"></i>';
  item.appendChild(thumb); item.appendChild(info); item.appendChild(addBtn);

  item.querySelector('.media-item-add').addEventListener('click', () => {
    addClipToTimeline({ type: 'video', name, src, duration });
  });
  list.appendChild(item);
}

// ── Add clip to timeline ───────────────────────────────────
function addClipToTimeline({ type, name, src, duration, volume = 1 }) {
  // Place after last clip of same type
  const sameTracks = state.clips.filter(c => c.type === type);
  const trackStart = sameTracks.reduce((max, c) => Math.max(max, c.trackStart + c.duration), 0);

  const clip = {
    id: newId(), type, name, src,
    start: 0, end: duration, duration,
    trackStart, filter: state.activeFilter,
    transition: state.activeTransition,
    fadeIn: false, fadeOut: false,
    speed: 1, volume,
  };
  state.clips.push(clip);
  renderClip(clip);
  recalcDuration();

  // Show preview for first video clip
  if (type === 'video' && state.clips.filter(c => c.type === 'video').length === 1) {
    loadVideoPreview(clip);
  }
  toast(`Added: ${name}`, 'success');
}

// ── Render clip element ────────────────────────────────────
function renderClip(clip) {
  const track = clip.type === 'video' ? trackVideo
              : clip.type === 'audio' ? trackAudio
              : trackText;

  const el = document.createElement('div');
  el.className = `clip ${clip.type}-clip`;
  el.dataset.id = clip.id;
  el.style.left   = `${clip.trackStart * state.pxPerSec}px`;
  el.style.width  = `${clip.duration * state.pxPerSec}px`;
  el.title = clip.name;
  const lh = document.createElement('div'); lh.className = 'clip-handle left';
  const sp = document.createElement('span'); sp.textContent = clip.name;
  const rh = document.createElement('div'); rh.className = 'clip-handle right';
  el.appendChild(lh); el.appendChild(sp); el.appendChild(rh);

  // Select on click
  el.addEventListener('mousedown', e => {
    if (e.target.classList.contains('clip-handle')) return;
    selectClip(clip.id);
    startClipDrag(e, clip, el);
  });

  // Resize handles
  el.querySelector('.clip-handle.left').addEventListener('mousedown', e => {
    e.stopPropagation();
    startResize(e, clip, el, 'left');
  });
  el.querySelector('.clip-handle.right').addEventListener('mousedown', e => {
    e.stopPropagation();
    startResize(e, clip, el, 'right');
  });

  track.appendChild(el);
}

function getClipEl(id) {
  return document.querySelector(`.clip[data-id="${id}"]`);
}

// ── Clip drag ──────────────────────────────────────────────
function startClipDrag(e, clip, el) {
  const startX = e.clientX;
  const origStart = clip.trackStart;

  const onMove = ev => {
    const dx = ev.clientX - startX;
    const newStart = Math.max(0, origStart + dx / state.pxPerSec);
    clip.trackStart = newStart;
    el.style.left = `${newStart * state.pxPerSec}px`;
    recalcDuration();
  };
  const onUp = () => {
    document.removeEventListener('mousemove', onMove);
    document.removeEventListener('mouseup', onUp);
  };
  document.addEventListener('mousemove', onMove);
  document.addEventListener('mouseup', onUp);
}

// ── Clip resize ────────────────────────────────────────────
function startResize(e, clip, el, side) {
  e.preventDefault();
  const startX = e.clientX;
  const origDur = clip.duration;
  const origStart = clip.trackStart;

  const onMove = ev => {
    const dx = (ev.clientX - startX) / state.pxPerSec;
    if (side === 'right') {
      clip.duration = Math.max(0.1, origDur + dx);
      el.style.width = `${clip.duration * state.pxPerSec}px`;
    } else {
      const newStart = Math.max(0, origStart + dx);
      const newDur = Math.max(0.1, origDur - (newStart - origStart));
      clip.trackStart = newStart;
      clip.duration = newDur;
      el.style.left  = `${newStart * state.pxPerSec}px`;
      el.style.width = `${newDur * state.pxPerSec}px`;
    }
    recalcDuration();
  };
  const onUp = () => {
    document.removeEventListener('mousemove', onMove);
    document.removeEventListener('mouseup', onUp);
  };
  document.addEventListener('mousemove', onMove);
  document.addEventListener('mouseup', onUp);
}

// ── Select clip ────────────────────────────────────────────
function selectClip(id) {
  document.querySelectorAll('.clip').forEach(el => el.classList.remove('selected'));
  state.selectedId = id;
  if (!id) {
    $('props-empty').style.display = '';
    $('props-content').style.display = 'none';
    btnDelete.disabled = true;
    btnCut.disabled = true;
    btnSplit.disabled = true;
    return;
  }
  const clip = state.clips.find(c => c.id === id);
  if (!clip) return;
  getClipEl(id)?.classList.add('selected');

  // Fill properties panel
  $('prop-name').textContent = clip.name;
  $('prop-duration').textContent = fmtTime(clip.duration);
  $('prop-start').value = clip.start.toFixed(2);
  $('prop-end').value = clip.end.toFixed(2);
  $('prop-speed').value = clip.speed;
  $('prop-volume').value = clip.volume;
  $('prop-volume-val').textContent = `${Math.round(clip.volume * 100)}%`;
  $('prop-fade-in').checked = clip.fadeIn;
  $('prop-fade-out').checked = clip.fadeOut;

  $('props-empty').style.display = 'none';
  $('props-content').style.display = '';
  btnDelete.disabled = false;
  btnCut.disabled = false;
  btnSplit.disabled = false;
}

// ── Properties panel ───────────────────────────────────────
$('prop-volume').addEventListener('input', function() {
  $('prop-volume-val').textContent = `${Math.round(this.value * 100)}%`;
});

$('btn-apply-props').addEventListener('click', () => {
  const clip = state.clips.find(c => c.id === state.selectedId);
  if (!clip) return;
  clip.start  = parseFloat($('prop-start').value) || 0;
  clip.end    = parseFloat($('prop-end').value) || clip.duration;
  clip.speed  = parseFloat($('prop-speed').value) || 1;
  clip.volume = parseFloat($('prop-volume').value);
  clip.fadeIn  = $('prop-fade-in').checked;
  clip.fadeOut = $('prop-fade-out').checked;
  clip.duration = (clip.end - clip.start) / clip.speed;
  const el = getClipEl(clip.id);
  if (el) el.style.width = `${clip.duration * state.pxPerSec}px`;
  recalcDuration();
  toast('Properties applied', 'success');
});

$('btn-delete-clip').addEventListener('click', deleteSelected);
btnDelete.addEventListener('click', deleteSelected);

function deleteSelected() {
  if (!state.selectedId) return;
  getClipEl(state.selectedId)?.remove();
  state.clips = state.clips.filter(c => c.id !== state.selectedId);
  selectClip(null);
  recalcDuration();
  toast('Clip deleted');
}

// ── Cut at playhead ────────────────────────────────────────
btnCut.addEventListener('click', () => {
  const clip = state.clips.find(c => c.id === state.selectedId);
  if (!clip) return;
  const cutAt = state.currentTime;
  if (cutAt <= clip.trackStart || cutAt >= clip.trackStart + clip.duration) {
    toast('Playhead must be inside the selected clip', 'error');
    return;
  }
  const leftDur  = cutAt - clip.trackStart;
  const rightDur = clip.duration - leftDur;

  // Shorten original
  clip.duration = leftDur;
  clip.end = clip.start + leftDur * clip.speed;
  getClipEl(clip.id).style.width = `${leftDur * state.pxPerSec}px`;

  // New right clip
  const right = { ...clip, id: newId(), trackStart: cutAt, duration: rightDur, start: clip.end };
  state.clips.push(right);
  renderClip(right);
  recalcDuration();
  toast('Clip cut');
});

// ── Split (same as cut) ────────────────────────────────────
btnSplit.addEventListener('click', () => btnCut.click());

// ── Filters ────────────────────────────────────────────────
document.querySelectorAll('.filter-item').forEach(item => {
  item.addEventListener('click', () => {
    document.querySelectorAll('.filter-item').forEach(i => i.classList.remove('active'));
    item.classList.add('active');
    state.activeFilter = item.dataset.filter;
    video.style.filter = state.activeFilter;
    // Apply to selected clip
    const clip = state.clips.find(c => c.id === state.selectedId);
    if (clip) clip.filter = state.activeFilter;
  });
});

// ── Transitions ────────────────────────────────────────────
document.querySelectorAll('.transition-item').forEach(item => {
  item.addEventListener('click', () => {
    document.querySelectorAll('.transition-item').forEach(i => i.classList.remove('active'));
    item.classList.add('active');
    state.activeTransition = item.dataset.transition;
    const clip = state.clips.find(c => c.id === state.selectedId);
    if (clip) clip.transition = state.activeTransition;
  });
});

// ── Text overlay ───────────────────────────────────────────
$('text-size').addEventListener('input', function() {
  $('text-size-val').textContent = `${this.value}px`;
});

$('btn-add-text').addEventListener('click', () => {
  const content = $('text-content').value.trim();
  if (!content) return;
  const size     = $('text-size').value;
  const color    = $('text-color').value;
  const position = $('text-position').value;

  // Add text clip to timeline at current time
  const dur = 5;
  const clip = {
    id: newId(), type: 'text', name: content,
    src: null, start: 0, end: dur, duration: dur,
    trackStart: state.currentTime,
    filter: '', transition: 'none',
    fadeIn: false, fadeOut: false, speed: 1, volume: 0,
    textContent: content, textSize: size, textColor: color, textPosition: position,
  };
  state.clips.push(clip);
  renderClip(clip);
  recalcDuration();
  toast('Text added', 'success');
});

// Enable add text button when there's content
$('text-content').addEventListener('input', function() {
  $('btn-add-text').disabled = !this.value.trim() || !state.clips.some(c => c.type === 'video');
});

// ── Video preview ──────────────────────────────────────────
function loadVideoPreview(clip) {
  if (!isSafeMediaSrc(clip.src)) {
    toast('Invalid media source', 'error');
    return;
  }
  video.src = clip.src;
  video.style.display = '';
  previewEmpty.style.display = 'none';
  btnPlay.disabled = false;
  btnExport.disabled = false;
  $('btn-add-text').disabled = !$('text-content').value.trim();
  video.style.filter = clip.filter || '';
}

// ── Playback ───────────────────────────────────────────────
btnPlay.addEventListener('click', togglePlay);

document.addEventListener('keydown', e => {
  if (e.target.tagName === 'INPUT' || e.target.tagName === 'TEXTAREA' || e.target.isContentEditable) return;
  if (e.code === 'Space') { e.preventDefault(); togglePlay(); }
  if (e.code === 'ArrowLeft')  seekBy(-1/30);
  if (e.code === 'ArrowRight') seekBy(1/30);
  if (e.code === 'Delete') deleteSelected();
});

function togglePlay() {
  if (state.playing) pause();
  else play();
}

function play() {
  if (!video.src) return;
  state.playing = true;
  btnPlay.innerHTML = '<i class="fas fa-pause"></i>';
  video.play();
  scheduleFrame();
}

function pause() {
  state.playing = false;
  btnPlay.innerHTML = '<i class="fas fa-play"></i>';
  video.pause();
  cancelAnimationFrame(state.rafId);
}

function scheduleFrame() {
  state.rafId = requestAnimationFrame(() => {
    if (!state.playing) return;
    state.currentTime = video.currentTime;
    updatePlayhead();
    updateTimeDisplay();
    updateTextOverlay();
    scheduleFrame();
  });
}

video.addEventListener('ended', () => pause());

function seekBy(delta) {
  video.currentTime = Math.max(0, Math.min(video.duration || 0, video.currentTime + delta));
  state.currentTime = video.currentTime;
  updatePlayhead();
  updateTimeDisplay();
}

$('btn-rewind').addEventListener('click', () => { video.currentTime = 0; state.currentTime = 0; updatePlayhead(); updateTimeDisplay(); });
$('btn-forward').addEventListener('click', () => seekBy(5));
$('btn-prev-frame').addEventListener('click', () => seekBy(-1/30));
$('btn-next-frame').addEventListener('click', () => seekBy(1/30));
$('volume-slider').addEventListener('input', function() { video.volume = this.value; });

function updateTimeDisplay() {
  currentTimeEl.textContent = fmtTime(state.currentTime);
}

function updatePlayhead() {
  const offset = 70; // track label width
  playhead.style.left = `${offset + state.currentTime * state.pxPerSec}px`;
}

// Click on timeline to seek
$('timeline-scroll').addEventListener('click', e => {
  const rect = trackVideo.getBoundingClientRect();
  const x = e.clientX - rect.left;
  const t = Math.max(0, x / state.pxPerSec);
  video.currentTime = Math.min(t, video.duration || 0);
  state.currentTime = video.currentTime;
  updatePlayhead();
  updateTimeDisplay();
});

// ── Text overlay rendering ─────────────────────────────────
function updateTextOverlay() {
  const active = state.clips.filter(c =>
    c.type === 'text' &&
    state.currentTime >= c.trackStart &&
    state.currentTime < c.trackStart + c.duration
  );
  if (!active.length) { textOverlay.innerHTML = ''; return; }
  const c = active[0];
  textOverlay.className = `text-overlay pos-${c.textPosition || 'center'}`;
  textOverlay.innerHTML = `<span class="overlay-text" style="font-size:${c.textSize}px;color:${c.textColor}">${c.textContent}</span>`;
}

// ── Ruler ──────────────────────────────────────────────────
function renderRuler() {
  ruler.innerHTML = '';
  const totalW = Math.max(state.totalDuration * state.pxPerSec + 200, 800);
  ruler.style.width = `${totalW + 70}px`;
  const step = state.pxPerSec >= 80 ? 1 : state.pxPerSec >= 40 ? 2 : 5;
  for (let t = 0; t <= state.totalDuration + 10; t += step / 5) {
    const tick = document.createElement('div');
    const isMajor = t % step === 0;
    tick.className = `ruler-tick${isMajor ? ' major' : ''}`;
    tick.style.left = `${70 + t * state.pxPerSec}px`;
    if (isMajor) tick.dataset.label = fmtTime(t);
    ruler.appendChild(tick);
  }
}

// ── Zoom ───────────────────────────────────────────────────
$('btn-zoom-in').addEventListener('click', () => setZoom(state.pxPerSec * 1.5));
$('btn-zoom-out').addEventListener('click', () => setZoom(state.pxPerSec / 1.5));

function setZoom(px) {
  state.pxPerSec = Math.max(20, Math.min(300, px));
  state.zoom = state.pxPerSec / 80;
  zoomLevelEl.textContent = `${state.zoom.toFixed(1)}x`;
  // Re-render all clip positions
  state.clips.forEach(clip => {
    const el = getClipEl(clip.id);
    if (el) {
      el.style.left  = `${clip.trackStart * state.pxPerSec}px`;
      el.style.width = `${clip.duration * state.pxPerSec}px`;
    }
  });
  renderRuler();
  updatePlayhead();
}

// ── Toolbar state ──────────────────────────────────────────
function updateToolbarState() {
  const hasClips = state.clips.length > 0;
  btnExport.disabled = !hasClips;
  btnPlay.disabled   = !state.clips.some(c => c.type === 'video');
}

// ── Export modal ───────────────────────────────────────────
btnExport.addEventListener('click', () => {
  $('modal-export').style.display = 'flex';
});
$('btn-close-export').addEventListener('click', () => {
  $('modal-export').style.display = 'none';
});
$('btn-start-export').addEventListener('click', () => {
  const format   = $('export-format').value;
  const quality  = $('export-quality').value;
  const filename = $('export-filename').value || '56editor-export';

  // Build export data for host app (WebView2 → C#)
  const exportData = {
    filename, format, quality,
    clips: state.clips.map(c => ({
      type: c.type, src: c.src, name: c.name,
      start: c.start, end: c.end, duration: c.duration,
      trackStart: c.trackStart, filter: c.filter,
      transition: c.transition, fadeIn: c.fadeIn, fadeOut: c.fadeOut,
      speed: c.speed, volume: c.volume,
    })),
  };

  // Notify host app via WebView2 bridge
  if (window.chrome?.webview) {
    window.chrome.webview.postMessage(JSON.stringify({ type: 'export', data: exportData }));
  } else {
    // Fallback: download project JSON
    const blob = new Blob([JSON.stringify(exportData, null, 2)], { type: 'application/json' });
    const a = document.createElement('a');
    a.href = URL.createObjectURL(blob);
    a.download = `${filename}.json`;
    a.click();
  }

  $('modal-export').style.display = 'none';
  toast('Export started...', 'success');
});

// ── Init ───────────────────────────────────────────────────
renderRuler();
updateToolbarState();
