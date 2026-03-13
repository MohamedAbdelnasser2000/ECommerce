// ===== صفحة المنتجات المتناسقة - JavaScript =====

class ConsistentProductPage {
    constructor() {
        this.products = [];
        this.filteredProducts = [];
        this.currentView = 'grid';
        this.currentSort = 'newest';
        this.filters = {
            search: '',
            category: 'all',
            minPrice: 0,
            maxPrice: 1000,
            inStock: false
        };
        
        this.init();
    }

    init() {
        this.loadProducts();
        this.setupEventListeners();
        this.setupMobileFilters();
        this.updateProductsDisplay();
    }

    // تحميل المنتجات من DOM (البيانات الحقيقية من قاعدة البيانات)
    loadProducts() {
        this.products = [];
        const productCards = document.querySelectorAll('.product-card-consistent');
        
        productCards.forEach(card => {
            const productId = parseInt(card.dataset.productId);
            const nameElement = card.querySelector('.product-title a');
            const priceElement = card.querySelector('.current-price');
            const originalPriceElement = card.querySelector('.original-price');
            const imageElement = card.querySelector('.product-image');
            const stockElement = card.querySelector('.add-to-cart-btn');
            
            if (productId && nameElement && priceElement && imageElement) {
                const product = {
                    id: productId,
                    name: nameElement.textContent.trim(),
                    price: parseFloat(priceElement.textContent.replace('$', '')),
                    originalPrice: originalPriceElement ? parseFloat(originalPriceElement.textContent.replace('$', '')) : null,
                    image: imageElement.src,
                    category: 'general', // يمكن تحسينه لاحقاً
                    rating: 4.0, // افتراضي
                    reviewCount: 0, // افتراضي
                    inStock: !stockElement.disabled,
                    isNew: card.querySelector('.badge-new') !== null,
                    discount: card.querySelector('.badge-sale') !== null ? 10 : 0
                };
                
                this.products.push(product);
            }
        });
        
        this.filteredProducts = [...this.products];
    }

    // إعداد مستمعي الأحداث
    setupEventListeners() {
        // البحث
        const searchInput = document.getElementById('productSearch');
        if (searchInput) {
            let searchTimeout;
            searchInput.addEventListener('input', (e) => {
                clearTimeout(searchTimeout);
                searchTimeout = setTimeout(() => {
                    this.filters.search = e.target.value.toLowerCase();
                    this.applyFilters();
                }, 300);
            });
        }

        // الفئات: نسمح بالتنقّل الافتراضي إلى /Product?categoryId=...
        // ProductController سيتولى التصفية على الخادم.
        // لا نضيف أي منع افتراضي هنا حتى يعمل الفلتر كما هو متوقع بالخادم.


        // الترتيب
        const sortSelect = document.getElementById('sortSelect');
        if (sortSelect) {
            sortSelect.addEventListener('change', (e) => {
                this.currentSort = e.target.value;
                this.sortProducts();
                this.updateProductsDisplay();
            });
        }

        // تبديل العرض
        document.querySelectorAll('.view-btn').forEach(btn => {
            btn.addEventListener('click', (e) => {
                document.querySelectorAll('.view-btn').forEach(b => b.classList.remove('active'));
                e.target.classList.add('active');
                
                this.currentView = e.target.dataset.view;
                this.updateProductsDisplay();
            });
        });

        // شريط تمرير الأسعار
        this.setupPriceSlider();
    }

    // إعداد شريط تمرير الأسعار
    setupPriceSlider() {
        const minInput = document.getElementById('minPrice');
        const maxInput = document.getElementById('maxPrice');
        
        if (minInput && maxInput) {
            minInput.addEventListener('input', () => {
                this.filters.minPrice = parseInt(minInput.value) || 0;
                this.applyFilters();
            });
            
            maxInput.addEventListener('input', () => {
                this.filters.maxPrice = parseInt(maxInput.value) || 1000;
                this.applyFilters();
            });
        }
    }

    // إعداد الفلاتر للهاتف المحمول
    setupMobileFilters() {
        const mobileFilterBtn = document.getElementById('mobileFilterBtn');
        const filtersSidebar = document.querySelector('.filters-sidebar-consistent');
        const mobileOverlay = document.querySelector('.mobile-overlay');
        const closeFilterBtn = document.getElementById('closeFilters');

        if (mobileFilterBtn && filtersSidebar && mobileOverlay) {
            mobileFilterBtn.addEventListener('click', () => {
                filtersSidebar.classList.add('show');
                mobileOverlay.classList.add('show');
                document.body.style.overflow = 'hidden';
            });

            const closeFilters = () => {
                filtersSidebar.classList.remove('show');
                mobileOverlay.classList.remove('show');
                document.body.style.overflow = '';
            };

            if (closeFilterBtn) {
                closeFilterBtn.addEventListener('click', closeFilters);
            }
            
            mobileOverlay.addEventListener('click', closeFilters);
        }
    }

    // تطبيق الفلاتر
    applyFilters() {
        this.filteredProducts = this.products.filter(product => {
            // فلتر البحث
            if (this.filters.search && !product.name.toLowerCase().includes(this.filters.search)) {
                return false;
            }

            // فلتر الفئة
            if (this.filters.category !== 'all' && product.category !== this.filters.category) {
                return false;
            }

            // فلتر السعر
            if (product.price < this.filters.minPrice || product.price > this.filters.maxPrice) {
                return false;
            }

            // فلتر التوفر
            if (this.filters.inStock && !product.inStock) {
                return false;
            }

            return true;
        });

        this.sortProducts();
        this.updateProductsDisplay();
        this.updateProductCount();
    }

    // ترتيب المنتجات
    sortProducts() {
        switch (this.currentSort) {
            case 'price-low':
                this.filteredProducts.sort((a, b) => a.price - b.price);
                break;
            case 'price-high':
                this.filteredProducts.sort((a, b) => b.price - a.price);
                break;
            case 'rating':
                this.filteredProducts.sort((a, b) => b.rating - a.rating);
                break;
            case 'name':
                this.filteredProducts.sort((a, b) => a.name.localeCompare(b.name));
                break;
            case 'newest':
            default:
                this.filteredProducts.sort((a, b) => b.id - a.id);
                break;
        }
    }

    // تحديث عرض المنتجات
    updateProductsDisplay() {
        const productsGrid = document.querySelector('.products-grid');
        if (!productsGrid) return;

        if (this.filteredProducts.length === 0) {
            productsGrid.innerHTML = `
                <div class="no-products" style="grid-column: 1 / -1; text-align: center; padding: 3rem;">
                    <i class="fas fa-search" style="font-size: 3rem; color: var(--gold); margin-bottom: 1rem;"></i>
                    <h3 style="color: var(--text-dark); margin-bottom: 0.5rem;">لا توجد منتجات</h3>
                    <p style="color: var(--text-muted);">لم نجد أي منتجات تطابق معايير البحث الخاصة بك</p>
                </div>
            `;
            return;
        }

        productsGrid.innerHTML = this.filteredProducts.map(product => this.createProductCard(product)).join('');
        
        // إعداد أحداث البطاقات الجديدة
        this.setupProductCardEvents();
    }

    // إنشاء بطاقة منتج
    createProductCard(product) {
        const discountBadge = product.discount > 0 ? `<span class="product-badge badge-sale">خصم ${product.discount}%</span>` : '';
        const newBadge = product.isNew ? `<span class="product-badge badge-new">جديد</span>` : '';
        const outOfStockBadge = !product.inStock ? `<span class="product-badge badge-out-of-stock">غير متوفر</span>` : '';
        
        const originalPriceHtml = product.originalPrice ? 
            `<span class="original-price">$${product.originalPrice}</span>` : '';
        
        const discountPercentHtml = product.discount > 0 ? 
            `<span class="discount-percent">-${product.discount}%</span>` : '';

        const stars = Array.from({length: 5}, (_, i) => 
            `<i class="fas fa-star star ${i < Math.floor(product.rating) ? '' : 'empty'}"></i>`
        ).join('');

        return `
            <div class="product-card-consistent" data-product-id="${product.id}">
                <div class="product-image-container">
                    <a href="/Product/Details/${product.id}" aria-label="عرض ${product.name}">
                        <img src="${product.image}" alt="${product.name}" class="product-image" 
                             onerror="this.src='/images/no-image.jpg'">
                    </a>
                    
                    <div class="product-badges">
                        ${discountBadge}
                        ${newBadge}
                        ${outOfStockBadge}
                    </div>
                    
                    <div class="product-actions">
                        <button class="action-btn wishlist-btn" data-product-id="${product.id}" 
                                title="إضافة للمفضلة" aria-label="إضافة للمفضلة">
                            <i class="fas fa-heart"></i>
                        </button>
                        <button class="action-btn quick-view-btn" data-product-id="${product.id}" 
                                title="عرض سريع" aria-label="عرض سريع">
                            <i class="fas fa-eye"></i>
                        </button>
                    </div>
                </div>
                
                <div class="product-content">
                    <h3 class="product-title"><a href="/Product/Details/${product.id}" style="color: inherit; text-decoration: none;">${product.name}</a></h3>
                    
                    <div class="product-rating">
                        <div class="stars">${stars}</div>
                        <span class="rating-count">(${product.reviewCount})</span>
                    </div>
                    
                    <div class="product-price">
                        <span class="current-price">$${product.price}</span>
                        ${originalPriceHtml}
                        ${discountPercentHtml}
                    </div>
                    
                    <button class="add-to-cart-btn" data-product-id="${product.id}" 
                            ${!product.inStock ? 'disabled' : ''}>
                        <i class="fas fa-shopping-cart"></i>
                        ${product.inStock ? 'إضافة للسلة' : 'غير متوفر'}
                    </button>
                </div>
            </div>
        `;
    }

    // إعداد أحداث بطاقات المنتجات
    setupProductCardEvents() {
        // أزرار المفضلة: نتركها للمعالج العام في الهيدر (.wishlist-btn)
        // لا نضيف مستمعين محليين لتجنب الازدواج

        // أزرار العرض السريع
        document.querySelectorAll('.quick-view-btn').forEach(btn => {
            btn.addEventListener('click', (e) => {
                e.stopPropagation();
                this.showQuickView(btn.dataset.productId);
            });
        });

        // أزرار إضافة للسلة
        document.querySelectorAll('.add-to-cart-btn').forEach(btn => {
            btn.addEventListener('click', (e) => {
                e.stopPropagation();
                if (!btn.disabled) {
                    this.addToCart(btn);
                }
            });
        });
    }

    // تبديل المفضلة
    toggleFavorite(btn) {
        const productId = btn.dataset.productId;
        const isFavorited = btn.classList.contains('favorited');
        
        if (isFavorited) {
            btn.classList.remove('favorited');
            this.showToast('تم إزالة المنتج من المفضلة', 'info');
        } else {
            btn.classList.add('favorited');
            this.showToast('تم إضافة المنتج للمفضلة', 'success');
            
            // تأثير القلب
            btn.style.animation = 'heartBeat 0.6s ease';
            setTimeout(() => {
                btn.style.animation = '';
            }, 600);
        }
    }

    // إضافة للسلة
    async addToCart(btn) {
        const productId = btn.dataset.productId;
        const originalText = btn.innerHTML;
        
        // حالة التحميل
        btn.classList.add('loading');
        btn.innerHTML = '<span class="loading-spinner"></span> جاري الإضافة...';
        btn.disabled = true;
        
        try {
            const response = await fetch('/Cart/AddToCart', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/x-www-form-urlencoded',
                    'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]')?.value || ''
                },
                body: `productId=${productId}&quantity=1`
            });
            
            const result = await response.json();
            
            if (result.success) {
                btn.classList.remove('loading');
                btn.classList.add('added');
                btn.innerHTML = '<i class="fas fa-check"></i> تمت الإضافة';
                
                this.showToast('تم إضافة المنتج إلى السلة بنجاح', 'success');
                
                // تحديث عداد السلة إذا كان موجوداً
                if (result.cartCount !== undefined) {
                    this.updateCartCount(result.cartCount);
                }
                
                // إعادة تعيين الزر بعد ثانيتين
                setTimeout(() => {
                    btn.classList.remove('added');
                    btn.innerHTML = originalText;
                    btn.disabled = false;
                }, 2000);
            } else {
                throw new Error(result.message || 'فشل في إضافة المنتج');
            }
        } catch (error) {
            btn.classList.remove('loading');
            btn.innerHTML = originalText;
            btn.disabled = false;
            
            this.showToast(error.message || 'حدث خطأ أثناء إضافة المنتج للسلة', 'error');
        }
    }
    
    // تحديث عداد السلة
    updateCartCount(count) {
        const cartCountElements = document.querySelectorAll('.cart-count, .badge-cart');
        cartCountElements.forEach(element => {
            element.textContent = count;
            element.style.display = count > 0 ? 'inline' : 'none';
        });
    }

    // عرض سريع
    showQuickView(productId) {
        // الانتقال مباشرة إلى صفحة تفاصيل المنتج
        window.location.href = `/Product/Details/${productId}`;
    }

    // تحديث عدد المنتجات
    updateProductCount() {
        const countElement = document.querySelector('.products-count');
        if (countElement) {
            const total = this.products.length;
            const filtered = this.filteredProducts.length;
            countElement.innerHTML = `عرض <strong>${filtered}</strong> من أصل <strong>${total}</strong> منتج`;
        }
    }

    // عرض إشعار Toast
    showToast(message, type = 'info', duration = 3000) {
        const toastContainer = document.querySelector('.toast-container') || this.createToastContainer();
        
        const toast = document.createElement('div');
        toast.className = `toast-notification ${type}`;
        
        const icon = this.getToastIcon(type);
        toast.innerHTML = `
            <i class="${icon}"></i>
            <span>${message}</span>
        `;
        
        toastContainer.appendChild(toast);
        
        // إزالة الإشعار بعد المدة المحددة
        setTimeout(() => {
            toast.style.animation = 'slideOutToast 0.3s ease forwards';
            setTimeout(() => {
                if (toast.parentNode) {
                    toast.parentNode.removeChild(toast);
                }
            }, 300);
        }, duration);
    }

    // إنشاء حاوية الإشعارات
    createToastContainer() {
        const container = document.createElement('div');
        container.className = 'toast-container';
        document.body.appendChild(container);
        
        // إضافة CSS للانزلاق للخارج
        const style = document.createElement('style');
        style.textContent = `
            @keyframes slideOutToast {
                to {
                    transform: translateX(100%);
                    opacity: 0;
                }
            }
        `;
        document.head.appendChild(style);
        
        return container;
    }

    // الحصول على أيقونة الإشعار
    getToastIcon(type) {
        switch (type) {
            case 'success': return 'fas fa-check-circle';
            case 'error': return 'fas fa-exclamation-circle';
            case 'warning': return 'fas fa-exclamation-triangle';
            case 'info':
            default: return 'fas fa-info-circle';
        }
    }
}

// تأثيرات CSS إضافية
const additionalStyles = `
    @keyframes heartBeat {
        0%, 100% { transform: scale(1); }
        25% { transform: scale(1.2); }
        50% { transform: scale(1.4); }
        75% { transform: scale(1.2); }
    }
`;

// إضافة الأنماط للصفحة
const styleSheet = document.createElement('style');
styleSheet.textContent = additionalStyles;
document.head.appendChild(styleSheet);

// تهيئة الصفحة عند تحميل DOM
document.addEventListener('DOMContentLoaded', () => {
    new ConsistentProductPage();
});

// تصدير للاستخدام العام
window.ConsistentProductPage = ConsistentProductPage;