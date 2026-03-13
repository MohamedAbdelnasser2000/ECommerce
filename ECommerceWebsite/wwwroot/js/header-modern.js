// Mobile drawer + counters hookup
(function(){
  const hamburger = document.getElementById('hamburger');
  const mobileNav = document.getElementById('mobileNav');
  const mobileOverlay = document.getElementById('mobileOverlay');

  function openNav(){
    mobileNav?.classList.add('open');
    mobileOverlay?.classList.add('show');
    if (hamburger) hamburger.setAttribute('aria-expanded', 'true');
    // lock body scroll while drawer is open
    document.body.style.overflow = 'hidden';
  }
  function closeNav(){
    mobileNav?.classList.remove('open');
    mobileOverlay?.classList.remove('show');
    if (hamburger) hamburger.setAttribute('aria-expanded', 'false');
    document.body.style.overflow = '';
  }

  // Toggle drawer on hamburger click
  hamburger?.addEventListener('click', function(e){
    e.preventDefault();
    const isOpen = mobileNav?.classList.contains('open');
    if (isOpen) { closeNav(); } else { openNav(); }
  });
  
  // Close when clicking on overlay
  mobileOverlay?.addEventListener('click', closeNav);
  
  // Close on ESC
  document.addEventListener('keydown', function(e){ if(e.key === 'Escape') closeNav(); });
  
  // Close when clicking any link inside mobile nav
  mobileNav?.addEventListener('click', function(e){
    const target = e.target;
    if (target && target.closest && target.closest('a')) {
      closeNav();
    }
  });

  // Auto-close on resize back to desktop
  window.addEventListener('resize', function(){
    if (window.innerWidth >= 992) { closeNav(); }
  });

  // Use existing jQuery-based counters if available, but avoid double scheduling
  if (window.$ && !window.__hmCountersScheduled) {
    window.__hmCountersScheduled = true;
    if (typeof updateWishlistCount === 'function') {
      try { updateWishlistCount(); } catch(_){}
      setInterval(function(){ try { updateWishlistCount(); } catch(_){} }, 60000);
    }
    if (typeof loadNotifications === 'function') {
      try { loadNotifications(); } catch(_){}
      setInterval(function(){ try { loadNotifications(); } catch(_){} }, 30000);
    }
  }
})();