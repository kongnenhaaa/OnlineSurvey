// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

(() => {
    const reducedMotion = window.matchMedia('(prefers-reduced-motion: reduce)').matches;
    const revealItems = [...document.querySelectorAll('[data-reveal]')];

    if (reducedMotion || !('IntersectionObserver' in window)) {
        revealItems.forEach(item => item.classList.add('is-visible'));
    } else {
        const revealObserver = new IntersectionObserver(entries => {
            entries.forEach(entry => {
                if (!entry.isIntersecting) return;
                entry.target.classList.add('is-visible');
                revealObserver.unobserve(entry.target);
            });
        }, { threshold: 0.12, rootMargin: '0px 0px -30px' });
        revealItems.forEach(item => revealObserver.observe(item));
    }

    const navbar = document.querySelector('.site-nav');
    const updateNavbar = () => navbar?.classList.toggle('is-scrolled', window.scrollY > 12);
    updateNavbar();
    window.addEventListener('scroll', updateNavbar, { passive: true });

    document.querySelectorAll('[data-counter]').forEach(counter => {
        const target = Number(counter.dataset.counter || counter.textContent);
        if (!Number.isFinite(target) || reducedMotion) return;
        counter.textContent = '0';
        const duration = 700;
        const startedAt = performance.now();
        const step = now => {
            const progress = Math.min(1, (now - startedAt) / duration);
            const eased = 1 - Math.pow(1 - progress, 3);
            counter.textContent = Math.round(target * eased).toLocaleString('vi-VN');
            if (progress < 1) requestAnimationFrame(step);
        };
        requestAnimationFrame(step);
    });

    document.querySelectorAll('[data-toast]').forEach(toast => {
        window.setTimeout(() => toast.classList.add('toast-alert-hide'), 4200);
    });
})();
