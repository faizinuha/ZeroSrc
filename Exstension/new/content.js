(() => {
  if (document.getElementById('zeromix-overlay')) {
    const overlay = document.getElementById('zeromix-overlay');
    overlay.style.display = overlay.style.display === 'none' ? 'flex' : 'none';
    return;
  }

  const overlay = document.createElement('div');
  overlay.id = 'zeromix-overlay';
  overlay.style.position = 'fixed';
  overlay.style.top = 0;
  overlay.style.left = 0;
  overlay.style.width = '100%';
  overlay.style.height = '100%';
  overlay.style.background = 'rgba(0,0,0,0.6)';
  overlay.style.display = 'flex';
  overlay.style.alignItems = 'center';
  overlay.style.justifyContent = 'center';
  overlay.style.zIndex = 999999;

  const input = document.createElement('input');
  input.type = 'text';
  input.placeholder = 'Search...';
  input.style.width = '60%';
  input.style.padding = '15px';
  input.style.fontSize = '20px';
  input.style.borderRadius = '8px';
  input.style.border = 'none';
  input.style.outline = 'none';

  overlay.appendChild(input);
  document.body.appendChild(overlay);

  // biar bisa ditutup pake ESC
  document.addEventListener('keydown', (e) => {
    if (e.key === 'Escape') overlay.style.display = 'none';
  });

  input.addEventListener('keydown', (e) => {
    if (e.key === 'Enter') {
      const query = e.target.value;
      window.open(
        'https://www.google.com/search?q=' + encodeURIComponent(query),
        '_blank'
      );
    }
  });
})();
