namespace ZeroMix.Features.ZeroConnect;

public static class PwaResources
{
    public static string GetHtml() => """
    <!DOCTYPE html>
    <html lang="id">
    <head>
      <meta charset="UTF-8">
      <meta name="viewport" content="width=device-width, initial-scale=1.0">
      <title>ZeroConnect</title>
      <meta name="theme-color" content="#0D0D0D">
      <style>
        * { box-sizing: border-box; margin: 0; padding: 0; }
        body {
          font-family: 'Segoe UI', system-ui, sans-serif;
          background: #0D0D0D; color: #F0F0F0;
          display: flex; flex-direction: column;
          min-height: 100dvh; padding: 1rem;
        }
        h1 { font-size: 1.3rem; margin-bottom: 1.5rem; color: #00D4FF; }
        .card {
          background: #1A1A1A; border: 1px solid #2A2A2A;
          border-radius: 12px; padding: 1rem; margin-bottom: 1rem;
        }
        .card h2 { font-size: 0.85rem; color: #888; margin-bottom: 0.75rem; text-transform: uppercase; }
        textarea {
          width: 100%; background: #0D0D0D; color: #F0F0F0;
          border: 1px solid #333; border-radius: 8px;
          padding: 0.75rem; font-size: 1rem; resize: none;
          min-height: 80px;
        }
        button {
          width: 100%; padding: 0.8rem; margin-top: 0.5rem;
          background: #00D4FF; color: #000; border: none;
          border-radius: 8px; font-size: 1rem; font-weight: 600;
          cursor: pointer; transition: opacity 0.2s;
        }
        button:active { opacity: 0.7; }
        button.secondary {
          background: #2A2A2A; color: #F0F0F0;
        }
        #status {
          font-size: 0.8rem; text-align: center; padding: 0.5rem;
          border-radius: 6px; margin-bottom: 1rem;
        }
        .connected { background: #0D2E1A; color: #00FF7F; }
        .disconnected { background: #2E0D0D; color: #FF4444; }
        #file-preview {
          margin-top: 0.5rem; font-size: 0.85rem; color: #888;
        }
        /* Toast */
        .toast {
          position: fixed; bottom: 24px; left: 50%;
          transform: translateX(-50%);
          background: #00D4FF; color: #000;
          padding: 10px 20px; border-radius: 20px;
          font-weight: 600; font-size: 14px;
          z-index: 9999; animation: slideUp 0.3s ease;
        }
        @keyframes slideUp { from { transform: translateX(-50%) translateY(12px); opacity: 0;} to { transform: translateX(-50%) translateY(0); opacity: 1;} }
      </style>
    </head>
    <body>
      <h1>⚡ ZeroConnect</h1>

      <div id="status" class="disconnected">● Disconnected</div>

      <div class="card">
        <h2>📋 Send Clipboard</h2>
        <textarea id="clipboard-text" placeholder="Ketik atau paste teks/link..."></textarea>
        <button onclick="sendClipboard()">Kirim ke Laptop</button>
        <button class="secondary" onclick="pasteFromPhone()">Paste dari HP</button>
      </div>

      <div class="card">
        <h2>🖼️ Send Image / File</h2>
        <input type="file" id="file-input" accept="image/*,*/*" style="display:none"
               onchange="handleFileSelect(event)">
        <button onclick="document.getElementById('file-input').click()">
          Pilih Gambar / File
        </button>
        <div id="file-preview"></div>
        <button id="send-file-btn" style="display:none" onclick="sendFile()">
          Kirim File ke Laptop
        </button>
      </div>

      <div class="card">
        <h2>📥 Dari Laptop</h2>
        <div id="received-content" style="font-size:0.9rem;color:#888;min-height:40px">
          Belum ada data masuk...
        </div>
        <button class="secondary" onclick="copyReceived()">Copy ke Clipboard HP</button>
      </div>

      <script>
        const host = location.host;
        // Include original query (token) when opening WebSocket so server can validate.
        const ws = new WebSocket(`ws://${host}${location.search}`);
        let selectedFile = null;
        let lastReceived = '';
        let lastClipboard = '';
        const _chunkBuffers = new Map(); // fileId -> { chunks: [], total, filename, isImage }

        ws.onopen = () => {
          document.getElementById('status').textContent = '● Connected ke ZeroMix';
          document.getElementById('status').className = 'connected';
          // Send auth token (first WS message) so server can validate without relying on URL
          try {
            const params = new URLSearchParams(location.search);
            const token = params.get('token');
            if (token) send({ type: 6, payload: token });
          } catch {}
          ping();
        };

        ws.onclose = () => {
          document.getElementById('status').textContent = '● Disconnected';
          document.getElementById('status').className = 'disconnected';
        };

        ws.onmessage = (e) => {
          const packet = JSON.parse(e.data);
          try {
            switch (packet.type) {
              case 0: // ClipboardText
                lastReceived = packet.payload || '';
                document.getElementById('received-content').textContent = lastReceived;
                break;

              case 1: // ClipboardImage
              case 2: // FileTransfer
                if (packet.payload && packet.filename) {
                  // payload is base64
                  const bytes = atob(packet.payload);
                  const len = bytes.length;
                  const u8 = new Uint8Array(len);
                  for (let i = 0; i < len; i++) u8[i] = bytes.charCodeAt(i);
                  const blob = new Blob([u8]);

                  if (packet.type === 1) {
                    const url = URL.createObjectURL(blob);
                    document.getElementById('received-content').innerHTML = `<img src="${url}" style="max-width:100%;height:auto">`;
                  } else {
                    const a = document.createElement('a');
                    a.href = URL.createObjectURL(blob);
                    a.download = packet.filename;
                    a.textContent = `Unduh ${packet.filename}`;
                    const rc = document.getElementById('received-content');
                    rc.innerHTML = '';
                    rc.appendChild(a);
                  }
                }
                break;

              case 5: // ChunkTransfer
                if (packet.meta && packet.payload && packet.filename) {
                  let meta = {};
                  try { meta = JSON.parse(packet.meta); } catch {}
                  const fileId = meta.fileId || 'unknown';
                  const total = meta.totalChunks || 0;
                  const entry = _chunkBuffers.get(fileId) || { chunks: [], total: total, filename: packet.filename, isImage: !!meta.isImage };
                  entry.chunks[meta.chunkIndex] = packet.payload;
                  entry.total = total;
                  _chunkBuffers.set(fileId, entry);

                  const receivedCount = entry.chunks.filter(c => c !== undefined).length;
                  document.getElementById('file-preview').textContent = `📥 Receiving... ${Math.round((receivedCount/total)*100)}%`;

                  if (entry.chunks.length === total && entry.chunks.every(c => c !== undefined)) {
                    // assemble
                    const byteArrays = entry.chunks.map(b64 => {
                      const bytes = atob(b64);
                      const u8 = new Uint8Array(bytes.length);
                      for (let i = 0; i < bytes.length; i++) u8[i] = bytes.charCodeAt(i);
                      return u8;
                    });
                    const blob = new Blob(byteArrays);
                    if (entry.isImage) {
                      const url = URL.createObjectURL(blob);
                      document.getElementById('received-content').innerHTML = `<img src="${url}" style="max-width:100%;height:auto">`;
                    } else {
                      const a = document.createElement('a');
                      a.href = URL.createObjectURL(blob);
                      a.download = entry.filename;
                      a.textContent = `Unduh ${entry.filename}`;
                      const rc = document.getElementById('received-content');
                      rc.innerHTML = '';
                      rc.appendChild(a);
                    }
                    _chunkBuffers.delete(fileId);
                    document.getElementById('file-preview').textContent = `📥 ${entry.filename} diterima`;
                  }
                }
                break;

              default:
                // ignore unknown
                break;
            }
          } catch (err) { console.error('PWA onmessage error', err); }
        };

        function send(packet) {
          if (ws.readyState === WebSocket.OPEN)
            ws.send(JSON.stringify(packet));
        }

        function sendClipboard() {
          const text = document.getElementById('clipboard-text').value.trim();
          if (!text) return;
          send({ type: 0, payload: text }); // ClipboardText
        }

        async function pasteFromPhone() {
          try {
            const text = await navigator.clipboard.readText();
            document.getElementById('clipboard-text').value = text;
          } catch {
            alert('Izinkan akses clipboard di browser kamu');
          }
        }

        function handleFileSelect(event) {
          selectedFile = event.target.files[0];
          if (!selectedFile) return;
          document.getElementById('file-preview').textContent = `📄 ${selectedFile.name}`;
          document.getElementById('send-file-btn').style.display = 'block';
        }

        async function sendFile() {
          if (!selectedFile) return;
          await sendFileWithProgress(selectedFile);
        }

        async function sendFileWithProgress(file) {
          const CHUNK_SIZE = 512 * 1024; // 512KB per chunk
          const totalChunks = Math.ceil(file.size / CHUNK_SIZE);
          const fileId = crypto.randomUUID();

          for (let i = 0; i < totalChunks; i++) {
            const start = i * CHUNK_SIZE;
            const end = Math.min(start + CHUNK_SIZE, file.size);
            const chunk = file.slice(start, end);

            const base64 = await readChunkAsBase64(chunk);

            send({
              type: 5, // ChunkTransfer
              payload: base64,
              filename: file.name,
              meta: JSON.stringify({
                fileId,
                chunkIndex: i,
                totalChunks,
                isImage: file.type.startsWith('image/')
              })
            });

            // Update progress
            const pct = Math.round(((i + 1) / totalChunks) * 100);
            document.getElementById('file-preview').textContent = 
              `📤 Sending... ${pct}%`;

            // Small delay agar not flood WebSocket
            await new Promise(r => setTimeout(r, 10));
          }
        }

        // Drag & drop support
        const dropZone = document.createElement('div');
        dropZone.id = 'drop-zone';
        dropZone.style.position = 'fixed';
        dropZone.style.top = '0';
        dropZone.style.left = '0';
        dropZone.style.right = '0';
        dropZone.style.bottom = '0';
        dropZone.style.display = 'flex';
        dropZone.style.alignItems = 'center';
        dropZone.style.justifyContent = 'center';
        dropZone.style.pointerEvents = 'none';
        document.body.appendChild(dropZone);

        window.addEventListener('dragover', e => { e.preventDefault(); dropZone.style.pointerEvents = 'auto'; dropZone.style.background = 'rgba(255,255,255,0.02)'; });
        window.addEventListener('dragleave', e => { dropZone.style.pointerEvents = 'none'; dropZone.style.background = 'transparent'; });
        window.addEventListener('drop', async e => {
          e.preventDefault();
          dropZone.style.pointerEvents = 'none';
          dropZone.style.background = 'transparent';
          const files = [...e.dataTransfer.files];
          for (const file of files) {
            await sendFileWithProgress(file);
          }
        });

        function readChunkAsBase64(blob) {
          return new Promise((resolve) => {
            const reader = new FileReader();
            reader.onload = e => resolve(e.target.result.split(',')[1]);
            reader.readAsDataURL(blob);
          });
        }

        function copyReceived() {
          if (!lastReceived) return;
          navigator.clipboard.writeText(lastReceived)
            .then(() => alert('Tersalin!'))
            .catch(() => alert('Gagal copy'));
        }

        function ping() {
          setInterval(() => send({ type: 3 }), 15000); // Ping every 15s
        }

        // Auto-detect clipboard text on phone and send to laptop (requires permission)
        setInterval(async () => {
          try {
            const text = await navigator.clipboard.readText();
            if (text && text !== lastClipboard) {
              lastClipboard = text;
              send({ type: 0, payload: text });
              showToast('📋 Tersalin ke Laptop');
            }
          } catch (e) { /* ignore if permission denied */ }
        }, 1000);

        function showToast(msg, duration = 2000) {
          try {
            const t = document.createElement('div');
            t.className = 'toast';
            t.textContent = msg;
            document.body.appendChild(t);
            setTimeout(() => t.remove(), duration);
          } catch {}
        }
      </script>
    </body>
    </html>
    """;
}