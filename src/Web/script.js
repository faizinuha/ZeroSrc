const navbar = document.querySelector('.navbar');
const menuToggle = document.querySelector('.menu-toggle');
const navMenu = document.querySelector('.nav-menu');
const navItems = document.querySelectorAll('.nav-item a');

// --- Navbar Scroll Effect ---
window.addEventListener('scroll', () => {
  if (window.scrollY > 50) {
    navbar.classList.add('scrolled');
  } else {
    navbar.classList.remove('scrolled');
  }
});

// --- Mobile Menu Toggle ---
if (menuToggle) {
  menuToggle.addEventListener('click', (e) => {
    e.stopPropagation();
    navMenu.classList.toggle('active');
  });
}

// Close menu when clicking on a link
navItems.forEach((item) => {
  item.addEventListener('click', () => {
    navMenu.classList.remove('active');
  });
});

// Close menu when clicking outside
document.addEventListener('click', (e) => {
  if (navbar && !navbar.contains(e.target)) {
    navMenu.classList.remove('active');
  }
});

// --- Mobile Dropdown Toggle ---
const dropdownToggles = document.querySelectorAll('.dropdown-toggle');
dropdownToggles.forEach((toggle) => {
  toggle.addEventListener('click', (e) => {
    if (window.innerWidth <= 768) {
      e.preventDefault();
      const parent = toggle.parentElement;
      parent.classList.toggle('active');
    }
  });
});

// --- Smooth scroll for anchor links ---
document.querySelectorAll('a[href^="#"]').forEach((anchor) => {
  anchor.addEventListener('click', function (e) {
    const href = this.getAttribute('href');
    if (href !== '#') {
      e.preventDefault();
      const target = document.querySelector(href);
      if (target) {
        target.scrollIntoView({
          behavior: 'smooth',
          block: 'start',
        });
      }
    }
  });
});

// --- Animasi Reveal saat Scroll ---
const revealElements = document.querySelectorAll('.reveal');

function revealOnScroll() {
  for (let i = 0; i < revealElements.length; i++) {
    const windowHeight = window.innerHeight;
    const elementTop = revealElements[i].getBoundingClientRect().top;
    const elementVisible = 150;

    if (elementTop < windowHeight - elementVisible) {
      revealElements[i].classList.add('visible');
    }
  }
}

window.addEventListener('scroll', revealOnScroll);
revealOnScroll();
