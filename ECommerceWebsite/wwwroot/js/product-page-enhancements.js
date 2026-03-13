// Product Page Performance Enhancements
(function() {
    'use strict';

    // Performance monitoring
    const performanceMonitor = {
        startTime: performance.now(),
        
        mark: function(name) {
            performance.mark(name);
        },
        
        measure: function(name, startMark, endMark) {
            performance.measure(name, startMark, endMark);
        },
        
        getMetrics: function() {
            const navigation = performance.getEntriesByType('navigation')[0];
            const paint = performance.getEntriesByType('paint');
            
            return {
                domContentLoaded: navigation.domContentLoadedEventEnd - navigation.domContentLoadedEventStart,
                loadComplete: navigation.loadEventEnd - navigation.loadEventStart,
                firstPaint: paint.find(p => p.name === 'first-paint')?.startTime,
                firstContentfulPaint: paint.find(p => p.name === 'first-contentful-paint')?.startTime
            };
        }
    };

    // Image optimization and lazy loading
    const imageOptimizer = {
        init: function() {
            this.setupLazyLoading();
            this.setupImageErrorHandling();
            this.preloadCriticalImages();
        },
        
        setupLazyLoading: function() {
            if ('IntersectionObserver' in window) {
                const imageObserver = new IntersectionObserver((entries, observer) => {
                    entries.forEach(entry => {
                        if (entry.isIntersecting) {
                            const img = entry.target;
                            this.loadImage(img);
                            observer.unobserve(img);
                        }
                    });
                }, {
                    rootMargin: '50px 0px',
                    threshold: 0.01
                });

                document.querySelectorAll('img[data-src]').forEach(img => {
                    imageObserver.observe(img);
                });
            } else {
                // Fallback for older browsers
                this.loadAllImages();
            }
        },
        
        loadImage: function(img) {
            const src = img.dataset.src;
            if (src) {
                img.src = src;
                img.classList.add('loaded');
                img.removeAttribute('data-src');
            }
        },
        
        loadAllImages: function() {
            document.querySelectorAll('img[data-src]').forEach(img => {
                this.loadImage(img);
            });
        },
        
        setupImageErrorHandling: function() {
            document.addEventListener('error', (e) => {
                if (e.target.tagName === 'IMG') {
                    e.target.src = '/images/no-image.jpg';
                    e.target.classList.add('error');
                }
            }, true);
        },
        
        preloadCriticalImages: function() {
            const criticalImages = ['/images/hero-bg.jpg', '/images/logo.png'];
            criticalImages.forEach(src => {
                const link = document.createElement('link');
                link.rel = 'preload';
                link.as = 'image';
                link.href = src;
                document.head.appendChild(link);
            });
        }
    };

    // Advanced filtering with debouncing
    const advancedFiltering = {
        debounceTimer: null,
        
        init: function() {
            this.setupSearchDebouncing();
            this.setupFilterCaching();
            this.setupURLStateManagement();
        },
        
        setupSearchDebouncing: function() {
            const searchInput = document.querySelector('.search-input');
            if (searchInput) {
                searchInput.addEventListener('input', (e) => {
                    clearTimeout(this.debounceTimer);
                    this.debounceTimer = setTimeout(() => {
                        this.performSearch(e.target.value);
                    }, 300);
                });
            }
        },
        
        performSearch: function(query) {
            if (query.length >= 2) {
                this.showSearchSuggestions(query);
            } else {
                this.hideSearchSuggestions();
            }
        },
        
        showSearchSuggestions: function(query) {
            // Implementation for search suggestions
            console.log('Showing suggestions for:', query);
        },
        
        hideSearchSuggestions: function() {
            const suggestions = document.querySelector('.search-suggestions');
            if (suggestions) {
                suggestions.style.display = 'none';
            }
        },
        
        setupFilterCaching: function() {
            this.filterCache = new Map();
        },
        
        setupURLStateManagement: function() {
            window.addEventListener('popstate', (e) => {
                if (e.state) {
                    this.restoreFiltersFromState(e.state);
                }
            });
        },
        
        restoreFiltersFromState: function(state) {
            // Restore filter state from browser history
            console.log('Restoring filters:', state);
        }
    };

    // Smooth animations and transitions
    const animationManager = {
        init: function() {
            this.setupScrollAnimations();
            this.setupHoverEffects();
            this.setupLoadingAnimations();
        },
        
        setupScrollAnimations: function() {
            const observerOptions = {
                threshold: 0.1,
                rootMargin: '0px 0px -50px 0px'
            };
            
            const observer = new IntersectionObserver((entries) => {
                entries.forEach(entry => {
                    if (entry.isIntersecting) {
                        entry.target.classList.add('animate-in');
                    }
                });
            }, observerOptions);
            
            document.querySelectorAll('.animate-on-scroll').forEach(el => {
                observer.observe(el);
            });
        },
        
        setupHoverEffects: function() {
            document.querySelectorAll('.product-card-grid').forEach(card => {
                card.addEventListener('mouseenter', this.onCardHover);
                card.addEventListener('mouseleave', this.onCardLeave);
            });
        },
        
        onCardHover: function(e) {
            const card = e.currentTarget;
            card.style.transform = 'translateY(-8px)';
            card.style.boxShadow = '0 12px 32px rgba(0, 0, 0, 0.15)';
        },
        
        onCardLeave: function(e) {
            const card = e.currentTarget;
            card.style.transform = '';
            card.style.boxShadow = '';
        },
        
        setupLoadingAnimations: function() {
            this.createLoadingSkeletons();
        },
        
        createLoadingSkeletons: function() {
            const container = document.querySelector('.products-grid');
            if (container && container.children.length === 0) {
                for (let i = 0; i < 8; i++) {
                    const skeleton = this.createSkeletonCard();
                    container.appendChild(skeleton);
                }
                
                // Remove skeletons when real content loads
                setTimeout(() => {
                    document.querySelectorAll('.skeleton-card').forEach(el => el.remove());
                }, 2000);
            }
        },
        
        createSkeletonCard: function() {
            const skeleton = document.createElement('div');
            skeleton.className = 'skeleton-card loading-skeleton';
            skeleton.innerHTML = `
                <div class="skeleton-image loading-skeleton"></div>
                <div class="skeleton-content">
                    <div class="skeleton-title loading-skeleton"></div>
                    <div class="skeleton-price loading-skeleton"></div>
                    <div class="skeleton-button loading-skeleton"></div>
                </div>
            `;
            return skeleton;
        }
    };

    // Advanced cart functionality
    const cartManager = {
        init: function() {
            this.setupCartPersistence();
            this.setupCartAnimations();
            this.setupQuickAdd();
        },
        
        setupCartPersistence: function() {
            this.loadCartFromStorage();
            window.addEventListener('beforeunload', () => {
                this.saveCartToStorage();
            });
        },
        
        loadCartFromStorage: function() {
            const cart = localStorage.getItem('shopping-cart');
            if (cart) {
                this.cart = JSON.parse(cart);
                this.updateCartUI();
            }
        },
        
        saveCartToStorage: function() {
            localStorage.setItem('shopping-cart', JSON.stringify(this.cart || []));
        },
        
        setupCartAnimations: function() {
            document.addEventListener('click', (e) => {
                if (e.target.classList.contains('add-to-cart-btn')) {
                    this.animateAddToCart(e.target);
                }
            });
        },
        
        animateAddToCart: function(button) {
            const rect = button.getBoundingClientRect();
            const cartIcon = document.querySelector('.cart-icon');
            
            if (cartIcon) {
                const cartRect = cartIcon.getBoundingClientRect();
                this.createFlyingProduct(rect, cartRect);
            }
        },
        
        createFlyingProduct: function(startRect, endRect) {
            const flyingProduct = document.createElement('div');
            flyingProduct.className = 'flying-product';
            flyingProduct.innerHTML = '<i class="fas fa-shopping-cart"></i>';
            
            flyingProduct.style.cssText = `
                position: fixed;
                left: ${startRect.left + startRect.width / 2}px;
                top: ${startRect.top + startRect.height / 2}px;
                z-index: 1000;
                color: var(--primary-color);
                font-size: 1.5rem;
                pointer-events: none;
                transition: all 0.8s cubic-bezier(0.25, 0.46, 0.45, 0.94);
            `;
            
            document.body.appendChild(flyingProduct);
            
            requestAnimationFrame(() => {
                flyingProduct.style.left = endRect.left + endRect.width / 2 + 'px';
                flyingProduct.style.top = endRect.top + endRect.height / 2 + 'px';
                flyingProduct.style.transform = 'scale(0.5)';
                flyingProduct.style.opacity = '0';
            });
            
            setTimeout(() => {
                flyingProduct.remove();
            }, 800);
        },
        
        setupQuickAdd: function() {
            document.querySelectorAll('.product-card-grid').forEach(card => {
                const quickAddBtn = document.createElement('button');
                quickAddBtn.className = 'quick-add-btn';
                quickAddBtn.innerHTML = '<i class="fas fa-plus"></i>';
                quickAddBtn.title = 'إضافة سريعة';
                
                quickAddBtn.addEventListener('click', (e) => {
                    e.stopPropagation();
                    this.quickAddToCart(card);
                });
                
                card.appendChild(quickAddBtn);
            });
        },
        
        quickAddToCart: function(card) {
            const productId = card.dataset.productId;
            // Quick add implementation
            console.log('Quick adding product:', productId);
        },
        
        updateCartUI: function() {
            const cartCount = document.querySelector('.cart-count');
            if (cartCount && this.cart) {
                cartCount.textContent = this.cart.length;
                cartCount.style.display = this.cart.length > 0 ? 'block' : 'none';
            }
        }
    };

    // Accessibility enhancements
    const accessibilityManager = {
        init: function() {
            this.setupKeyboardNavigation();
            this.setupAriaLabels();
            this.setupFocusManagement();
            this.setupScreenReaderSupport();
        },
        
        setupKeyboardNavigation: function() {
            document.addEventListener('keydown', (e) => {
                if (e.key === 'Tab') {
                    document.body.classList.add('keyboard-navigation');
                }
                
                if (e.key === 'Escape') {
                    this.closeModals();
                }
            });
            
            document.addEventListener('mousedown', () => {
                document.body.classList.remove('keyboard-navigation');
            });
        },
        
        setupAriaLabels: function() {
            document.querySelectorAll('.product-card-grid').forEach((card, index) => {
                card.setAttribute('role', 'article');
                card.setAttribute('aria-label', `منتج ${index + 1}`);
            });
            
            document.querySelectorAll('.filter-group').forEach(group => {
                const header = group.querySelector('.filter-header');
                const content = group.querySelector('.filter-content');
                
                if (header && content) {
                    const id = `filter-${Math.random().toString(36).substr(2, 9)}`;
                    header.setAttribute('aria-controls', id);
                    content.setAttribute('id', id);
                    header.setAttribute('aria-expanded', 'true');
                }
            });
        },
        
        setupFocusManagement: function() {
            const focusableElements = 'button, [href], input, select, textarea, [tabindex]:not([tabindex="-1"])';
            
            document.addEventListener('keydown', (e) => {
                if (e.key === 'Tab') {
                    const focusable = Array.from(document.querySelectorAll(focusableElements));
                    const currentIndex = focusable.indexOf(document.activeElement);
                    
                    if (e.shiftKey) {
                        if (currentIndex === 0) {
                            e.preventDefault();
                            focusable[focusable.length - 1].focus();
                        }
                    } else {
                        if (currentIndex === focusable.length - 1) {
                            e.preventDefault();
                            focusable[0].focus();
                        }
                    }
                }
            });
        },
        
        setupScreenReaderSupport: function() {
            const announcer = document.createElement('div');
            announcer.setAttribute('aria-live', 'polite');
            announcer.setAttribute('aria-atomic', 'true');
            announcer.className = 'sr-only';
            document.body.appendChild(announcer);
            
            this.announcer = announcer;
        },
        
        announce: function(message) {
            if (this.announcer) {
                this.announcer.textContent = message;
                setTimeout(() => {
                    this.announcer.textContent = '';
                }, 1000);
            }
        },
        
        closeModals: function() {
            const modals = document.querySelectorAll('.modal.show, .filters-sidebar-modern.show');
            modals.forEach(modal => {
                modal.classList.remove('show');
            });
        }
    };

    // Error handling and fallbacks
    const errorHandler = {
        init: function() {
            this.setupGlobalErrorHandling();
            this.setupNetworkErrorHandling();
            this.setupFallbacks();
        },
        
        setupGlobalErrorHandling: function() {
            window.addEventListener('error', (e) => {
                console.error('Global error:', e.error);
                this.showErrorMessage('حدث خطأ غير متوقع. يرجى إعادة تحميل الصفحة.');
            });
            
            window.addEventListener('unhandledrejection', (e) => {
                console.error('Unhandled promise rejection:', e.reason);
                this.showErrorMessage('حدث خطأ في تحميل البيانات. يرجى المحاولة مرة أخرى.');
            });
        },
        
        setupNetworkErrorHandling: function() {
            if ('navigator' in window && 'onLine' in navigator) {
                window.addEventListener('online', () => {
                    this.showSuccessMessage('تم استعادة الاتصال بالإنترنت');
                });
                
                window.addEventListener('offline', () => {
                    this.showErrorMessage('لا يوجد اتصال بالإنترنت');
                });
            }
        },
        
        setupFallbacks: function() {
            // CSS fallbacks
            if (!CSS.supports('display', 'grid')) {
                document.body.classList.add('no-grid-support');
            }
            
            // JavaScript fallbacks
            if (!window.IntersectionObserver) {
                this.loadPolyfills();
            }
        },
        
        loadPolyfills: function() {
            const script = document.createElement('script');
            script.src = 'https://polyfill.io/v3/polyfill.min.js?features=IntersectionObserver';
            document.head.appendChild(script);
        },
        
        showErrorMessage: function(message) {
            if (window.ModernProductPage && window.ModernProductPage.showToast) {
                window.ModernProductPage.showToast(message, 'error');
            } else {
                alert(message);
            }
        },
        
        showSuccessMessage: function(message) {
            if (window.ModernProductPage && window.ModernProductPage.showToast) {
                window.ModernProductPage.showToast(message, 'success');
            }
        }
    };

    // Initialize all enhancements
    function initializeEnhancements() {
        performanceMonitor.mark('enhancements-start');
        
        try {
            imageOptimizer.init();
            advancedFiltering.init();
            animationManager.init();
            cartManager.init();
            accessibilityManager.init();
            errorHandler.init();
            
            performanceMonitor.mark('enhancements-end');
            performanceMonitor.measure('enhancements-duration', 'enhancements-start', 'enhancements-end');
            
            console.log('Product page enhancements initialized successfully');
            console.log('Performance metrics:', performanceMonitor.getMetrics());
            
        } catch (error) {
            console.error('Error initializing enhancements:', error);
            errorHandler.showErrorMessage('حدث خطأ في تحميل بعض المميزات');
        }
    }

    // Initialize when DOM is ready
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', initializeEnhancements);
    } else {
        initializeEnhancements();
    }

    // Export for external use
    window.ProductPageEnhancements = {
        performanceMonitor,
        imageOptimizer,
        advancedFiltering,
        animationManager,
        cartManager,
        accessibilityManager,
        errorHandler
    };

})();