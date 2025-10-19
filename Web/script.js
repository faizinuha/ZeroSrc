document.addEventListener('DOMContentLoaded', () => {
  // --- Fungsi untuk alert (jika masih diperlukan) ---
  window.Alert = function () {
    alert(`
😅 Oops... fitur ini lagi cuti dulu ya!
👉 Silakan jalan-jalan ke GitHub buat info lebih lanjut.
👇 Jangan lupa scroll ke bawah, siapa tau ada harta karun fitur baru! 🏴‍☠️✨
 
🌍 English Mode Activated!
🙈 Sorry, this feature is still on vacation...
📦 But hey, scroll down to download or discover upcoming surprises!
        `);
  };

  window.NotFound = function () {
    alert('Developer Mengubah Update langsung di kirim Ke Link Tujuan....');
    alert('Silakan Ke Link Alternatif Ya....');
    alert('Lets Go');
    window.location.href =
      'https://mega.nz/folder/uEdWTbSJ#y1bCKlrXXy93gi3e5zeBXA';
  };
  // --- Animasi saat scroll ---
  const observer = new IntersectionObserver(
    (entries) => {
      entries.forEach((entry) => {
        if (entry.isIntersecting) {
          entry.target.classList.add('visible');
        }
      });
    },
    { threshold: 0.1 }
  );

  const elementsToReveal = document.querySelectorAll('.reveal');
  elementsToReveal.forEach((el) => observer.observe(el));

  // --- Inisialisasi Particles.js ---
  if (document.getElementById('particles-js')) {
    particlesJS('particles-js', {
      particles: {
        number: {
          value: 60,
          density: {
            enable: true,
            value_area: 800,
          },
        },
        color: {
          value: '#00d9ff',
        },
        shape: {
          type: 'circle',
        },
        opacity: {
          value: 0.5,
          random: true,
          anim: {
            enable: true,
            speed: 1,
            opacity_min: 0.1,
            sync: false,
          },
        },
        size: {
          value: 3,
          random: true,
        },
        line_linked: {
          enable: true,
          distance: 150,
          color: '#00d9ff',
          opacity: 0.2,
          width: 1,
        },
        move: {
          enable: true,
          speed: 2,
          direction: 'none',
          random: false,
          straight: false,
          out_mode: 'out',
          bounce: false,
        },
      },
      interactivity: {
        detect_on: 'canvas',
        events: {
          onhover: {
            enable: true,
            mode: 'bubble',
          },
           onclick: {
            enable: true,
            mode: 'repulse',
          },
        },
        modes: {
            bubble: {
                distance: 200,
                size: 6,
                duration: 2,
                opacity: 0.8,
            },
            repulse: {
                distance: 200,
                duration: 0.4,
            }
        }
      },
      retina_detect: true,
    });
  }

  // --- Inisialisasi Swiper.js ---
  const swiper = new Swiper('.screenshots-carousel', {
    loop: true,
    grabCursor: true,
    centeredSlides: true,
    slidesPerView: 'auto',
    effect: 'coverflow',
    coverflowEffect: {
      rotate: 40,
      stretch: 0,
      depth: 150,
      modifier: 1.5,
      slideShadows: true,
    },
    pagination: {
      el: '.swiper-pagination',
      clickable: true,
    },
  });
});
