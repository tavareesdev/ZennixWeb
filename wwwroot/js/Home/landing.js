/**
 * Script da Landing Page - ZENNIX
 * Funcionalidades para melhorar a experiência do usuário
 */

document.addEventListener('DOMContentLoaded', function() {
    // Smooth scroll para links internos
    const internalLinks = document.querySelectorAll('a[href^="#"]');
    internalLinks.forEach(link => {
        link.addEventListener('click', function(e) {
            e.preventDefault();
            const targetId = this.getAttribute('href');
            if (targetId === '#') return;
            
            const targetElement = document.querySelector(targetId);
            if (targetElement) {
                const headerHeight = document.querySelector('header').offsetHeight;
                const targetPosition = targetElement.offsetTop - headerHeight - 20;
                
                window.scrollTo({
                    top: targetPosition,
                    behavior: 'smooth'
                });

                // Fechar menu mobile após clicar em um link
                closeMobileMenu();
            }
        });
    });

    // Menu Hambúrguer
    const menuToggle = document.getElementById('menuToggle');
    const navMenu = document.getElementById('navMenu');
    
    function toggleMobileMenu() {
        if (menuToggle && navMenu) {
            menuToggle.classList.toggle('active');
            navMenu.classList.toggle('active');
            
            // Prevenir scroll do body quando menu está aberto
            if (navMenu.classList.contains('active')) {
                document.body.style.overflow = 'hidden';
            } else {
                document.body.style.overflow = '';
            }
        }
    }
    
    function closeMobileMenu() {
        if (menuToggle && navMenu) {
            menuToggle.classList.remove('active');
            navMenu.classList.remove('active');
            document.body.style.overflow = '';
        }
    }
    
    // Event listeners para o menu
    if (menuToggle && navMenu) {
        menuToggle.addEventListener('click', function(e) {
            e.stopPropagation();
            toggleMobileMenu();
        });
        
        // Fechar menu ao clicar em um link do menu
        const navLinks = navMenu.querySelectorAll('a');
        navLinks.forEach(link => {
            link.addEventListener('click', closeMobileMenu);
        });
        
        // Fechar menu ao clicar fora
        document.addEventListener('click', function(e) {
            if (!menuToggle.contains(e.target) && !navMenu.contains(e.target)) {
                closeMobileMenu();
            }
        });
        
        // Fechar menu ao redimensionar a janela (se voltar para desktop)
        window.addEventListener('resize', function() {
            if (window.innerWidth > 768) {
                closeMobileMenu();
            }
        });
    }

    // Adiciona classe active no header durante o scroll
    const header = document.querySelector('header');
    const heroSection = document.querySelector('.hero');
    
    function updateHeaderOnScroll() {
        if (header && heroSection) {
            if (window.scrollY > heroSection.offsetHeight - 100) {
                header.style.backgroundColor = 'rgba(28, 78, 146, 0.95)';
                header.style.backdropFilter = 'blur(10px)';
            } else {
                header.style.backgroundColor = '#1c4e92';
                header.style.backdropFilter = 'none';
            }
        }
    }

    if (header && heroSection) {
        window.addEventListener('scroll', updateHeaderOnScroll);
    }

    // Animação de entrada para os cards de features
    const featureCards = document.querySelectorAll('.feature-card');
    const observerOptions = {
        threshold: 0.1,
        rootMargin: '0px 0px -50px 0px'
    };

    const observer = new IntersectionObserver(function(entries) {
        entries.forEach(entry => {
            if (entry.isIntersecting) {
                entry.target.style.opacity = '1';
                entry.target.style.transform = 'translateY(0)';
            }
        });
    }, observerOptions);

    // Configura estado inicial para animação
    featureCards.forEach(card => {
        card.style.opacity = '0';
        card.style.transform = 'translateY(30px)';
        card.style.transition = 'opacity 0.6s ease, transform 0.6s ease';
        observer.observe(card);
    });

    // Contador animado para estatísticas (exemplo)
    function animateCounter(element, target, duration = 2000) {
        let start = 0;
        const increment = target / (duration / 16);
        const timer = setInterval(() => {
            start += increment;
            if (start >= target) {
                element.textContent = target + '+';
                clearInterval(timer);
            } else {
                element.textContent = Math.floor(start) + '+';
            }
        }, 16);
    }

    // Exemplo de contadores (pode adaptar para dados reais)
    const counters = document.querySelectorAll('.counter');
    counters.forEach(counter => {
        const target = parseInt(counter.getAttribute('data-target'));
        if (target) {
            animateCounter(counter, target);
        }
    });

    // Download buttons - tracking (exemplo)
    const downloadButtons = document.querySelectorAll('.download-btn');
    downloadButtons.forEach(button => {
        button.addEventListener('click', function() {
            const version = this.closest('.download-card').querySelector('h3').textContent;
            console.log(`Download iniciado: ${version}`);
            
            // Tracking personalizado
            trackDownload(version, this.getAttribute('download'));
        });
    });
});

// Função para tracking de downloads (exemplo)
function trackDownload(version, fileName) {
    // Implemente seu tracking aqui (Google Analytics, etc.)
    console.log(`Download tracked: ${version} - ${fileName}`);
    
    // Exemplo com Google Analytics
    if (typeof gtag !== 'undefined') {
        gtag('event', 'download', {
            'event_category': 'engagement',
            'event_label': `${version}_${fileName}`
        });
    }
    
    // SweetAlert de confirmação (opcional)
    if (typeof Swal !== 'undefined') {
        Swal.fire({
            icon: 'info',
            title: 'Download Iniciado',
            text: `O arquivo ${fileName} está sendo baixado...`,
            timer: 3000,
            showConfirmButton: false
        });
    }
}