// Modern Homepage JavaScript

document.addEventListener('DOMContentLoaded', function() {
    // Animate elements on scroll
    const animateOnScroll = () => {
        const elements = document.querySelectorAll('.animate-on-scroll');
        
        elements.forEach(element => {
            const elementTop = element.getBoundingClientRect().top;
            const windowHeight = window.innerHeight;
            
            if (elementTop < windowHeight - 100) {
                element.classList.add('animate-fadeInUp');
            }
        });
    };

    // Initialize animations on page load
    const initAnimations = () => {
        animateOnScroll();
        window.addEventListener('scroll', animateOnScroll);
    };

    // Product quick view functionality
    const initQuickView = () => {
        const quickViewButtons = document.querySelectorAll('.quick-view-btn');
        
        quickViewButtons.forEach(button => {
            button.addEventListener('click', function(e) {
                e.preventDefault();
                const productId = this.getAttribute('data-product-id');
                // Here you would typically fetch product details via AJAX
                console.log('Quick view for product:', productId);
                // For now, just show a simple alert
                alert('Quick view feature will be implemented here for product ID: ' + productId);
            });
        });
    };

    // Initialize newsletter form
    const initNewsletter = () => {
        const newsletterForm = document.getElementById('newsletter-form');
        if (newsletterForm) {
            newsletterForm.addEventListener('submit', function(e) {
                e.preventDefault();
                const email = this.querySelector('input[type="email"]').value;
                // Here you would typically send the email to your server
                console.log('Subscribing email:', email);
                // Show success message
                const messageDiv = document.createElement('div');
                messageDiv.className = 'alert alert-success mt-3';
                messageDiv.textContent = 'Thank you for subscribing to our newsletter!';
                this.appendChild(messageDiv);
                // Reset form
                this.reset();
                // Remove message after 5 seconds
                setTimeout(() => {
                    messageDiv.remove();
                }, 5000);
            });
        }
    };

    function getAntiForgeryToken() {
        const tokenInput = document.querySelector('input[name="__RequestVerificationToken"]');
        return tokenInput ? tokenInput.value : '';
    }

    const initAddToCart = () => {
        document.querySelectorAll('.btn-add-to-cart').forEach(button => {
            button.addEventListener('click', function() {
                const productId = this.getAttribute('data-product-id');
                const originalHtml = this.innerHTML;
                this.innerHTML = '<i class="fas fa-spinner fa-spin"></i>';
                this.disabled = true;

                fetch('/Cart/AddToCart', {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/x-www-form-urlencoded',
                        'RequestVerificationToken': getAntiForgeryToken()
                    },
                    body: `productId=${productId}&quantity=1`
                })
                .then(response => response.json())
                .then(data => {
                    if (data.success) {
                        if (window.toastr) {
                            window.toastr.success(data.message || 'Product added to cart!');
                        }
                        updateCartCount(data.cartCount);
                    } else {
                        if (window.toastr) {
                            window.toastr.error(data.message || 'Failed to add product to cart.');
                        }
                    }
                })
                .catch(error => {
                    console.error('Error adding to cart:', error);
                    if (window.toastr) {
                        window.toastr.error('An unexpected error occurred.');
                    }
                })
                .finally(() => {
                    setTimeout(() => {
                        this.innerHTML = originalHtml;
                        this.disabled = false;
                    }, 1000);
                });
            });
        });
    };

    const initAddToWishlist = () => {
        document.querySelectorAll('.Wishlist-btn').forEach(button => {
            button.addEventListener('click', function() {
                const productId = this.getAttribute('data-product-id');
                const icon = this.querySelector('i');
                const originalIconClass = icon.className;
                icon.className = 'fas fa-spinner fa-spin';
                this.disabled = true;

                fetch('/Wishlist/AddToWishlist', {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json',
                        'RequestVerificationToken': getAntiForgeryToken()
                    },
                    body: JSON.stringify({ productId: parseInt(productId) })
                })
                .then(response => response.json())
                .then(data => {
                    if (data.success) {
                        if (window.toastr) {
                            window.toastr.success(data.message || 'Product added to wishlist!');
                        }
                        updateWishlistCount();
                    } else {
                        if (window.toastr) {
                            window.toastr.warning(data.message || 'Could not add to wishlist.');
                        }
                    }
                })
                .catch(error => {
                    console.error('Error adding to wishlist:', error);
                    if (window.toastr) {
                        window.toastr.error('An unexpected error occurred.');
                    }
                })
                .finally(() => {
                    setTimeout(() => {
                        icon.className = originalIconClass;
                        this.disabled = false;
                    }, 1000);
                });
            });
        });
    };

    window.updateCartCount = (count) => {
        const cartCountEl = document.getElementById('cart-count');
        if (typeof count === 'number') {
            if (cartCountEl) {
                cartCountEl.textContent = count;
                cartCountEl.style.display = count > 0 ? 'inline-block' : 'none';
            }
        } else {
            fetch('/api/cart/count')
                .then(response => response.json())
                .then(data => {
                    if (cartCountEl) {
                        cartCountEl.textContent = data.count;
                        cartCountEl.style.display = data.count > 0 ? 'inline-block' : 'none';
                    }
                })
                .catch(error => console.error('Error fetching cart count:', error));
        }
    };

    window.updateWishlistCount = () => {
        const wishlistCountEl = document.getElementById('Wishlist-count');
        fetch('/Wishlist/GetWishlistCount')
            .then(response => response.json())
            .then(data => {
                if (wishlistCountEl) {
                    wishlistCountEl.textContent = data.count;
                    wishlistCountEl.style.display = data.count > 0 ? 'inline-block' : 'none';
                }
            })
            .catch(error => console.error('Error fetching wishlist count:', error));
    };

    // Initialize all functions
    const init = () => {
        initAnimations();
        initQuickView();
        initNewsletter();
        initAddToCart();
        initAddToWishlist();
        window.updateCartCount();
        window.updateWishlistCount();
    };

    // Run initialization
    init();
});

// Smooth scrolling for anchor links
document.querySelectorAll('a[href^="#"]').forEach(anchor => {
    anchor.addEventListener('click', function (e) {
        e.preventDefault();
        const targetId = this.getAttribute('href');
        if (targetId === '#') return;
        
        const targetElement = document.querySelector(targetId);
        if (targetElement) {
            window.scrollTo({
                top: targetElement.offsetTop - 80,
                behavior: 'smooth'
            });
        }
    });
});