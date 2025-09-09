document.getElementById('searchInput').addEventListener('keydown', (e) => {
  if (e.key === 'Enter') {
    const query = e.target.value;
    window.open(
      'https://www.google.com/search?q=' + encodeURIComponent(query),
      '_blank'
    );
  }
});
