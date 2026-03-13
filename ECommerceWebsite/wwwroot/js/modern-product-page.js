// Modern Product Page JavaScript
document.addEventListener('DOMContentLoaded', function() {
    // Initialize all components
    initMobileFilters();
    initPriceSlider();
    initProductActions();
    initScrollToTop();
    initAnimations();
    initLazyLoading();
    initTooltips();
    
    console.log('Modern Product Page initialized');
});

// Mobile Filters
function initMobileFilters() {
    const mobileFilterBtn = document.querySelector('.mobile-filter-btn');
    const filtersSidebar = document.querySelector('.filters-sidebar-modern');
    const mobileOverlay = document.querySelector('.mobile-filter-overlay');
    const mobileCloseBtn = document.querySelector('.mobile-close-btn');
    
    if (!mobileFilterBtn || !filtersSidebar || !mobileOverlay) return;
    
    // Open filters
    mobileFilterBtn.addEventListener('click', function() {
        filtersSidebar.classList.add('show');
        mobileOverlay.classList.add('show');
        document.body.style.overflow = 'hidden';
        
        // Add animation
        filtersSidebar.style.animation = 'slideInRight 0.3s ease';
    });
    
    // Close filters
    function closeFilters() {
        filtersSidebar.classList.remove('show');
        mobileOverlay.classList.remove('show');
        document.body.style.overflow = '';
        
        // Add animation
        filtersSidebar.style.animation = 'slideOutRight 0.3s ease';
    }
    
    if (mobileCloseBtn) {
        mobileCloseBtn.addEventListener('click', closeFilters);
    }
    
    mobileOverlay.addEventListener('click', closeFilters);
    
    // Close on escape key
    document.addEventListener('keydown', function(e) {
        if (e.key === 'Escape' && filtersSidebar.classList.contains('show')) {
            closeFilters();
        }
    });
}

// Price Range Slider
function initPriceSlider() {
    const sliderMin = document.querySelector('.slider-min');
    const sliderMax = document.querySelector('.slider-max');
    const sliderRange = document.querySelector('.slider-range');
    const priceInputs = document.querySelectorAll('.price-input');
    
    if (!sliderMin || !sliderMax || !sliderRange) return;
    
    const minPrice = 0;
    const maxPrice = 10000;
    let priceGap = 500;
    
    function updateSlider() {
        const minVal = parseInt(sliderMin.value);
        const maxVal = parseInt(sliderMax.value);
        
        if (maxVal - minVal < priceGap) {
            if (event.target === sliderMin) {
                sliderMin.value = maxVal - priceGap;
            } else {
                sliderMax.value = minVal + priceGap;
            }
        }
        
        const minPercent = (sliderMin.value / maxPrice) * 100;
        const maxPercent = (sliderMax.value / maxPrice) * 100;
        
        sliderRange.style.left = minPercent + '%';
        sliderRange.style.width = (maxPercent - minPercent) + '%';
        
        // Update input fields
        if (priceInputs[0]) priceInputs[0].value = sliderMin.value;
        if (priceInputs[1]) priceInputs[1].value = sliderMax.value;
    }
    
    function updateFromInputs() {
        const minVal = parseInt(priceInputs[0].value) || minPrice;
        const maxVal = parseInt(priceInputs[1].value) || maxPrice;
        
        if (maxVal - minVal >= priceGap) {
            sliderMin.value = minVal;
            sliderMax.value = maxVal;
            updateSlider();
        }
    }
    
    sliderMin.addEventListener('input', updateSlider);
    sliderMax.addEventListener('input', updateSlider);
    
    if (priceInputs[0]) priceInputs[0].addEventListener('change', updateFromInputs);
    if (priceInputs[1]) priceInputs[1].addEventListener('change', updateFromInputs);
    
    // Initialize slider
    updateSlider();
}

// Product Actions (Add to Cart, Wishlist)
function initProductActions() {
    // Add to Cart
    const addToCartButtons = document.querySelectorAll('.add-to-cart-btn');
    addToCartButtons.forEach(button => {
        button.addEventListener('click', function(e) {
            e.preventDefault();
            handleAddToCart(this);
        });
    });
    
    // Wishlist
    const wishlistButtons = document.querySelectorAll('.wishlist-btn');
    wishlistButtons.forEach(button => {
        button.addEventListener('click', function(e) {
            e.preventDefault();
            handleWishlist(this);
        });
    });
}

function handleAddToCart(button) {
    const productId = button.dataset.productId;
    const originalContent = button.innerHTML;
    
    // Add loading state
    button.innerHTML = '<i class="fas fa-spinner fa-spin"></i> جاري الإضافة...';
    button.disabled = true;
    button.classList.add('loading');
    
    // Simulate API call
    setTimeout(() => {
        // Success state
        button.innerHTML = '<i class="fas fa-check"></i> تمت الإضافة';
        button.classList.remove('loading');
        button.style.background = 'var(--success-color)';
        
        // Show success notification
        showToast('تمت إضافة المنتج إلى السلة بنجاح', 'success');
        
        // Update cart count
        updateCartCount(1);
        
        // Reset button after 2 seconds
        setTimeout(() => {
            button.innerHTML = originalContent;
            button.disabled = false;
            button.style.background = '';
        }, 2000);
        
    }, 800);
}

function handleWishlist(button) {
    const productId = button.dataset.productId;
    const icon = button.querySelector('i');
    const isActive = button.classList.contains('active');
    
    // Add animation
    button.style.transform = 'scale(0.9)';
    setTimeout(() => {
        button.style.transform = 'scale(1)';
    }, 150);
    
    if (isActive) {
        // Remove from wishlist
        button.classList.remove('active');
        icon.classList.remove('fas');
        icon.classList.add('far');
        showToast('تمت إزالة المنتج من المفضلة', 'info');
    } else {
        // Add to wishlist
        button.classList.add('active');
        icon.classList.remove('far');
        icon.classList.add('fas');
        showToast('تمت إضافة المنتج إلى المفضلة', 'success');
        
        // Heart animation
        createHeartAnimation(button);
    }
}

function createHeartAnimation(button) {
    const heart = document.createElement('div');
    heart.innerHTML = '<i class="fas fa-heart"></i>';
    heart.style.cssText = `
        position: absolute;
        color: var(--danger-color);
        font-size: 1.5rem;
        pointer-events: none;
        z-index: 1000;
        animation: heartFloat 1s ease-out forwards;
    `;
    
    const rect = button.getBoundingClientRect();
    heart.style.left = rect.left + rect.width / 2 + 'px';
    heart.style.top = rect.top + rect.height / 2 + 'px';
    
    document.body.appendChild(heart);
    
    setTimeout(() => {
        heart.remove();
    }, 1000);
}

// Scroll to Top
function initScrollToTop() {
    const scrollBtn = document.createElement('button');
    scrollBtn.className = 'scroll-to-top';
    scrollBtn.innerHTML = '<i class="fas fa-arrow-up"></i>';
    scrollBtn.title = 'العودة للأعلى';
    document.body.appendChild(scrollBtn);
    
    window.addEventListener('scroll', function() {
        if (window.pageYOffset > 300) {
            scrollBtn.classList.add('visible');
        } else {
            scrollBtn.classList.remove('visible');
        }
    });
    
    scrollBtn.addEventListener('click', function() {
        window.scrollTo({
            top: 0,
            behavior: 'smooth'
        });
    });
}

// Animations
function initAnimations() {
    // Intersection Observer for scroll animations
    const observerOptions = {
        threshold: 0.1,
        rootMargin: '0px 0px -50px 0px'
    };
    
    const observer = new IntersectionObserver(function(entries) {
        entries.forEach(entry => {
            if (entry.isIntersecting) {
                entry.target.classList.add('animate-fade-in-up');
                observer.unobserve(entry.target);
            }
        });
    }, observerOptions);
    
    // Observe product cards
    const productCards = document.querySelectorAll('.product-card-grid, .product-card-list');
    productCards.forEach((card, index) => {
        card.style.animationDelay = (index * 0.1) + 's';
        observer.observe(card);
    });
    
    // Observe filter groups
    const filterGroups = document.querySelectorAll('.filter-group');
    filterGroups.forEach((group, index) => {
        group.style.animationDelay = (index * 0.1) + 's';
        observer.observe(group);
    });
}

// Lazy Loading
function initLazyLoading() {
    if ('loading' in HTMLImageElement.prototype) {
        // Native lazy loading supported
        const images = document.querySelectorAll('img[loading="lazy"]');
        images.forEach(img => {
            img.addEventListener('load', function() {
                this.style.opacity = '1';
            });
        });
    } else {
        // Fallback for browsers without native lazy loading
        const images = document.querySelectorAll('.product-image');
        const imageObserver = new IntersectionObserver(function(entries) {
            entries.forEach(entry => {
                if (entry.isIntersecting) {
                    const img = entry.target;
                    img.src = img.dataset.src || img.src;
                    img.style.opacity = '1';
                    imageObserver.unobserve(img);
                }
            });
        });
        
        images.forEach(img => {
            img.style.opacity = '0';
            img.style.transition = 'opacity 0.3s ease';
            imageObserver.observe(img);
        });
    }
}

// Tooltips
function initTooltips() {
    const tooltipElements = document.querySelectorAll('[title]');
    tooltipElements.forEach(element => {
        element.addEventListener('mouseenter', showTooltip);
        element.addEventListener('mouseleave', hideTooltip);
    });
}

function showTooltip(e) {
    const element = e.target;
    const title = element.getAttribute('title');
    if (!title) return;
    
    // Remove title to prevent default tooltip
    element.setAttribute('data-original-title', title);
    element.removeAttribute('title');
    
    const tooltip = document.createElement('div');
    tooltip.className = 'custom-tooltip';
    tooltip.textContent = title;
    tooltip.style.cssText = `
        position: absolute;
        background: var(--dark-color);
        color: var(--white);
        padding: 0.5rem 0.75rem;
        border-radius: var(--border-radius-sm);
        font-size: var(--font-size-sm);
        z-index: 1000;
        pointer-events: none;
        opacity: 0;
        transition: opacity 0.2s ease;
        white-space: nowrap;
    `;
    
    document.body.appendChild(tooltip);
    
    const rect = element.getBoundingClientRect();
    tooltip.style.left = rect.left + rect.width / 2 - tooltip.offsetWidth / 2 + 'px';
    tooltip.style.top = rect.top - tooltip.offsetHeight - 8 + 'px';
    
    setTimeout(() => {
        tooltip.style.opacity = '1';
    }, 10);
    
    element._tooltip = tooltip;
}

function hideTooltip(e) {
    const element = e.target;
    const tooltip = element._tooltip;
    
    if (tooltip) {
        tooltip.style.opacity = '0';
        setTimeout(() => {
            tooltip.remove();
        }, 200);
        delete element._tooltip;
    }
    
    // Restore original title
    const originalTitle = element.getAttribute('data-original-title');
    if (originalTitle) {
        element.setAttribute('title', originalTitle);
        element.removeAttribute('data-original-title');
    }
}

// Toast Notifications
function showToast(message, type = 'info', duration = 3000) {
    // Create toast container if it doesn't exist
    let container = document.querySelector('.toast-container');
    if (!container) {
        container = document.createElement('div');
        container.className = 'toast-container';
        document.body.appendChild(container);
    }
    
    // Create toast
    const toast = document.createElement('div');
    toast.className = `toast-notification ${type}`;
    
    const icon = getToastIcon(type);
    toast.innerHTML = `
        <div class="toast-icon">${icon}</div>
        <div class="toast-message">${message}</div>
        <button class="toast-close" onclick="this.parentElement.remove()">
            <i class="fas fa-times"></i>
        </button>
    `;
    
    container.appendChild(toast);
    
    // Auto remove
    setTimeout(() => {
        toast.style.animation = 'slideOutRight 0.3s ease';
        setTimeout(() => {
            toast.remove();
        }, 300);
    }, duration);
}

function getToastIcon(type) {
    const icons = {
        success: '<i class="fas fa-check-circle" style="color: var(--success-color);"></i>',
        error: '<i class="fas fa-exclamation-circle" style="color: var(--danger-color);"></i>',
        warning: '<i class="fas fa-exclamation-triangle" style="color: var(--warning-color);"></i>',
        info: '<i class="fas fa-info-circle" style="color: var(--info-color);"></i>'
    };
    return icons[type] || icons.info;
}

// Update Cart Count
function updateCartCount(change) {
    const cartCount = document.getElementById('cart-count');
    if (cartCount) {
        const currentCount = parseInt(cartCount.textContent) || 0;
        const newCount = currentCount + change;
        cartCount.textContent = newCount;
        
        // Animation
        cartCount.style.transform = 'scale(1.3)';
        setTimeout(() => {
            cartCount.style.transform = 'scale(1)';
        }, 200);
        
        // Show/hide badge
        if (newCount > 0) {
            cartCount.style.display = 'block';
        } else {
            cartCount.style.display = 'none';
        }
    }
}

// Filter Collapsing (for mobile)
function initFilterCollapse() {
    const filterHeaders = document.querySelectorAll('.filter-header');
    filterHeaders.forEach(header => {
        header.addEventListener('click', function() {
            const content = this.nextElementSibling;
            const isCollapsed = content.style.display === 'none';
            
            if (isCollapsed) {
                content.style.display = 'block';
                this.classList.remove('collapsed');
            } else {
                content.style.display = 'none';
                this.classList.add('collapsed');
            }
        });
    });
}

// Search Enhancement
function initSearchEnhancement() {
    const searchInput = document.querySelector('.search-input');
    if (!searchInput) return;
    
    let searchTimeout;
    
    searchInput.addEventListener('input', function() {
        clearTimeout(searchTimeout);
        const query = this.value.trim();
        
        if (query.length >= 2) {
            searchTimeout = setTimeout(() => {
                // Show search suggestions (if implemented)
                showSearchSuggestions(query);
            }, 300);
        } else {
            hideSearchSuggestions();
        }
    });
    
    // Clear search
    const clearBtn = document.createElement('button');
    clearBtn.innerHTML = '<i class="fas fa-times"></i>';
    clearBtn.className = 'search-clear';
    clearBtn.style.cssText = `
        position: absolute;
        left: 40px;
        top: 50%;
        transform: translateY(-50%);
        background: none;
        border: none;
        color: var(--secondary-color);
        cursor: pointer;
        display: none;
    `;
    
    searchInput.parentElement.style.position = 'relative';
    searchInput.parentElement.appendChild(clearBtn);
    
    searchInput.addEventListener('input', function() {
        clearBtn.style.display = this.value ? 'block' : 'none';
    });
    
    clearBtn.addEventListener('click', function() {
        searchInput.value = '';
        this.style.display = 'none';
        searchInput.focus();
    });
}

function showSearchSuggestions(query) {
    // Implementation for search suggestions
    console.log('Search suggestions for:', query);
}

function hideSearchSuggestions() {
    // Implementation to hide search suggestions
    console.log('Hide search suggestions');
}

// Performance Optimization
function debounce(func, wait) {
    let timeout;
    return function executedFunction(...args) {
        const later = () => {
            clearTimeout(timeout);
            func(...args);
        };
        clearTimeout(timeout);
        timeout = setTimeout(later, wait);
    };
}

// Add CSS animations
const style = document.createElement('style');
style.textContent = `
    @keyframes slideInRight {
        from {
            transform: translateX(100%);
            opacity: 0;
        }
        to {
            transform: translateX(0);
            opacity: 1;
        }
    }
    
    @keyframes slideOutRight {
        from {
            transform: translateX(0);
            opacity: 1;
        }
        to {
            transform: translateX(100%);
            opacity: 0;
        }
    }
    
    @keyframes heartFloat {
        0% {
            transform: translateY(0) scale(1);
            opacity: 1;
        }
        100% {
            transform: translateY(-50px) scale(1.5);
            opacity: 0;
        }
    }
    
    @keyframes slideOutRight {
        from {
            transform: translateX(0);
            opacity: 1;
        }
        to {
            transform: translateX(100%);
            opacity: 0;
        }
    }
`;
document.head.appendChild(style);

// Initialize additional features on load
window.addEventListener('load', function() {
    initFilterCollapse();
    initSearchEnhancement();
});

// Export functions for external use
window.ModernProductPage = {
    showToast,
    updateCartCount,
    handleAddToCart,
    handleWishlist
};