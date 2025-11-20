const nav_link_list = document.querySelectorAll('[data-nav]');

function set_active_nav() {
    const hash = window.location.hash.toLowerCase();
    nav_link_list.forEach(link_item => {
        if (!link_item.dataset.nav) return;
        const target = link_item.dataset.nav.toLowerCase();
        const is_match = hash.includes(target) || window.location.pathname.toLowerCase().includes(target);
        link_item.classList.toggle('active', is_match);
    });
}

function init_reveal_animation() {
    const reveal_nodes = document.querySelectorAll('.reveal');
    const observer = new IntersectionObserver(entries => {
        entries.forEach(entry => {
            if (entry.isIntersecting) {
                entry.target.classList.add('visible');
                observer.unobserve(entry.target);
            }
        });
    }, { threshold: 0.2 });

    reveal_nodes.forEach(node => observer.observe(node));
}

function animate_counters() {
    const counters = document.querySelectorAll('[data-counter]');
    counters.forEach(counter => {
        const target = parseFloat(counter.dataset.counter);
        const is_decimal = !Number.isInteger(target);
        let current = 0;
        const increment = target / 120;
        function update() {
            current += increment;
            if (current >= target) {
                counter.textContent = is_decimal ? target.toFixed(1) : Math.round(target);
                return;
            }
            counter.textContent = is_decimal ? current.toFixed(1) : Math.round(current);
            requestAnimationFrame(update);
        }
        requestAnimationFrame(update);
    });
}

document.addEventListener('DOMContentLoaded', () => {
    set_active_nav();
    init_reveal_animation();
    animate_counters();
});

