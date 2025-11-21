// === SPLASH SCREEN ANIMATION WITH PROGRESS === 
document.addEventListener('DOMContentLoaded', function() {
  const splashScreen = document.getElementById('splashScreen');
  const splashLogo = document.querySelector('.splash-logo');
  const splashTitle = document.querySelector('.splash-title');
  const splashSubtitle = document.querySelector('.splash-subtitle');
  const loaderBar = document.querySelector('.loader-bar');

  // Buat elemen progress text
  const progressText = document.createElement('p');
  progressText.className = 'splash-progress';
  progressText.textContent = 'Loading...0%';
  document.querySelector('.splash-content').appendChild(progressText);

  // Timeline untuk animasi intro
  const introTimeline = gsap.timeline();

  // 1. Logo fade in dan scale up
  introTimeline.to(splashLogo, {
    opacity: 1,
    scale: 1,
    duration: 0.8,
    ease: 'back.out'
  }, 0);

  // 2. Title muncul dengan effect
  introTimeline.to(splashTitle, {
    opacity: 1,
    duration: 0.8,
    ease: 'power2.out'
  }, 0.3);

  // 3. Subtitle muncul
  introTimeline.to(splashSubtitle, {
    opacity: 1,
    duration: 0.8,
    ease: 'power2.out'
  }, 0.5);

  // 4. Loading bar berjalan + progress counter
  introTimeline.to(loaderBar, {
    width: '100%',
    duration: 2,
    ease: 'power1.inOut'
  }, 0.8);

  // 5. Progress counter dari 0% ke 100%
  introTimeline.to(
    { progress: 0 },
    {
      progress: 100,
      duration: 2,
      ease: 'power1.inOut',
      onUpdate: function() {
        progressText.textContent = Math.round(this.targets()[0].progress) + '%';
      }
    },
    0.8 // Mulai bersamaan dengan loading bar
  );

  // 6. Semua fade out dan hilang
  introTimeline.to(splashScreen, {
    opacity: 0,
    duration: 0.8,
    ease: 'power2.inOut',
    delay: 0.5,
    onComplete: function() {
      splashScreen.classList.add('hidden');
    }
  });

  // ...existing code...
});