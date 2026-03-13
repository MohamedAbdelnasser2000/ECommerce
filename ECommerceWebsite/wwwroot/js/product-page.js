document.addEventListener('DOMContentLoaded', function() {
    // Mobile filter toggle
    const mobileFilterToggle = document.querySelector('.mobile-filter-toggle');
    const filterSidebar = document.getElementById('filter-sidebar');
    const filterOverlay = document.querySelector('.filter-overlay');
    const filterClose = document.querySelector('.filter-close');
    
    if (mobileFilterToggle && filterSidebar) {
        mobileFilterToggle.addEventListener('click', function() {
            filterSidebar.classList.add('show');
            document.body.style.overflow = 'hidden';
            if (filterOverlay) filterOverlay.classList.add('show');
        });
        
        if (filterClose) {
            filterClose.addEventListener('click', closeFilterSidebar);
        }
        
        if (filterOverlay) {
            filterOverlay.addEventListener('click', closeFilterSidebar);
        }
    }
    
    function closeFilterSidebar() {
        filterSidebar.classList.remove('show');
        document.body.style.overflow = '';
        if (filterOverlay) filterOverlay.classList.remove('show');
    }
    
    // Price range slider
    const rangeInput = document.querySelectorAll("input[type='range']");
    const priceInput = document.querySelectorAll(".price-input input");
    const progress = document.querySelector(".range-slider .progress");
    
    if (rangeInput.length > 1 && progress) {
        let priceGap = 1000;
        let minVal = parseInt(rangeInput[0].value);
        let maxVal = parseInt(rangeInput[1].value);
        
        rangeInput.forEach(input => {
            input.addEventListener("input", e => {
                let minVal = parseInt(rangeInput[0].value);
                let maxVal = parseInt(rangeInput[1].value);
                
                if (maxVal - minVal < priceGap) {
                    if (e.target.className === "range-min") {
                        rangeInput[0].value = maxVal - priceGap;
                    } else {
                        rangeInput[1].value = minVal + priceGap;
                    }
                } else {
                    priceInput[0].value = minVal;
                    priceInput[1].value = maxVal;
                    progress.style.left = (minVal / rangeInput[0].max) * 100 + "%";
                    progress.style.right = 100 - (maxVal / rangeInput[1].max) * 100 + "%";
                }
            });
        });
    }
    
    // Sort by functionality
    const sortSelect = document.getElementById('sortBy');
    if (sortSelect) {
        sortSelect.addEventListener('change', function() {
            const url = new URL(window.location.href);
            url.searchParams.set('sort', this.value);
            window.location.href = url.toString();
        });
        
        // Set the selected value from URL
        const urlParams = new URLSearchParams(window.location.search);
        const sortParam = urlParams.get('sort');
        if (sortParam) {
            sortSelect.value = sortParam;
        }
    }
    
    // Add to cart functionality
    const addToCartButtons = document.querySelectorAll('.add-to-cart');
    addToCartButtons.forEach(button => {
        button.addEventListener('click', function(e) {
            e.preventDefault();
            const productId = this.dataset.productId;
            const productCard = this.closest('.product-card');
            
            // Add loading state
            const originalText = this.innerHTML;
            this.innerHTML = '<i class="fas fa-spinner fa-spin"></i> جاري الإضافة...';
            this.disabled = true;
            
            // Simulate API call
            setTimeout(() => {
                // Update button state
                this.innerHTML = '<i class="fas fa-check"></i> تمت الإضافة';
                this.classList.remove('btn-primary');
                this.classList.add('btn-success');
                
                // Update cart count in the header
                updateCartCount(1);
                
                // Reset button after 2 seconds
                setTimeout(() => {
                    this.innerHTML = originalText;
                    this.classList.remove('btn-success');
                    this.classList.add('btn-primary');
                    this.disabled = false;
                }, 2000);
                
                // Show success message
                showToast('تمت إضافة المنتج إلى السلة بنجاح', 'success');
                
            }, 800);
        });
    });
    
    // Wishlist functionality
    const wishlistButtons = document.querySelectorAll('.wishlist');
    wishlistButtons.forEach(button => {
        button.addEventListener('click', function(e) {
            e.preventDefault();
            const productId = this.dataset.productId;
            const icon = this.querySelector('i');
            const isActive = this.classList.contains('active');
            
            // Toggle active state
            if (isActive) {
                this.classList.remove('active', 'text-danger');
                icon.classList.remove('fas');
                icon.classList.add('far');
                showToast('تمت إزالة المنتج من المفضلة', 'info');
            } else {
                this.classList.add('active', 'text-danger');
                icon.classList.remove('far');
                icon.classList.add('fas');
                showToast('تمت إضافة المنتج إلى المفضلة', 'success');
            }
        });
    });
    
    // Helper function to update cart count
    function updateCartCount(quantityChange) {
        const cartCount = document.getElementById('cart-count');
        if (cartCount) {
            const currentCount = parseInt(cartCount.textContent) || 0;
            cartCount.textContent = currentCount + quantityChange;
            cartCount.classList.add('animate__animated', 'animate__bounceIn');
            
            // Remove animation classes after animation completes
            setTimeout(() => {
                cartCount.classList.remove('animate__animated', 'animate__bounceIn');
            }, 1000);
        }
    }
    
    // Helper function to show toast messages
    function showToast(message, type = 'info') {
        // Check if toast container exists, if not create one
        let toastContainer = document.getElementById('toast-container');
        if (!toastContainer) {
            toastContainer = document.createElement('div');
            toastContainer.id = 'toast-container';
            toastContainer.style.position = 'fixed';
            toastContainer.style.bottom = '20px';
            toastContainer.style.left = '20px';
            toastContainer.style.zIndex = '9999';
            document.body.appendChild(toastContainer);
        }
        
        // Create toast element
        const toast = document.createElement('div');
        toast.className = `toast show align-items-center text-white bg-${type} border-0`;
        toast.role = 'alert';
        toast.setAttribute('aria-live', 'assertive');
        toast.setAttribute('aria-atomic', 'true');
        
        const toastBody = document.createElement('div');
        toastBody.className = 'd-flex';
        
        const toastContent = document.createElement('div');
        toastContent.className = 'toast-body';
        toastContent.textContent = message;
        
        const closeButton = document.createElement('button');
        closeButton.type = 'button';
        closeButton.className = 'btn-close btn-close-white me-2 m-auto';
        closeButton.setAttribute('data-bs-dismiss', 'toast');
        closeButton.setAttribute('aria-label', 'Close');
        
        toastBody.appendChild(toastContent);
        toastBody.appendChild(closeButton);
        toast.appendChild(toastBody);
        
        // Add toast to container
        toastContainer.appendChild(toast);
        
        // Auto remove toast after 3 seconds
        setTimeout(() => {
            toast.classList.remove('show');
            setTimeout(() => {
                toast.remove();
            }, 300);
        }, 3000);
    }
    
    // Scroll to top button
    const scrollToTopBtn = document.createElement('button');
    scrollToTopBtn.className = 'scroll-to-top';
    scrollToTopBtn.innerHTML = '<i class="fas fa-arrow-up"></i>';
    scrollToTopBtn.title = 'الانتقال للأعلى';
    document.body.appendChild(scrollToTopBtn);
    
    window.addEventListener('scroll', function() {
        if (window.pageYOffset > 300) {
            scrollToTopBtn.classList.add('visible');
        } else {
            scrollToTopBtn.classList.remove('visible');
        }
    });
    
    scrollToTopBtn.addEventListener('click', function() {
        window.scrollTo({
            top: 0,
            behavior: 'smooth'
        });
    });
    
    // Lazy loading for images
    if ('loading' in HTMLImageElement.prototype) {
        const lazyImages = document.querySelectorAll('img[loading="lazy"]');
        lazyImages.forEach(img => {
            img.src = img.dataset.src;
        });
    } else {
        // Fallback for browsers that don't support lazy loading
        const script = document.createElement('script');
        script.src = 'https://cdnjs.cloudflare.com/ajax/libs/lazysizes/5.3.2/lazysizes.min.js';
        document.body.appendChild(script);
    }
});
