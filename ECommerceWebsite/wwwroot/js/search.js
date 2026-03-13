// search.js - محرك البحث المحدث للـ SearchController

class SearchEngine {
    constructor() {
        this.searchTimeout = null;
        this.initializeSearch();
    }

    // تهيئة محرك البحث
    initializeSearch() {
        // إعداد البحث التلقائي
        this.setupSearchSuggestions();

        // مراقبة تغييرات الفلاتر
        this.watchFilterChanges();
    }

    // إعداد اقتراحات البحث
    setupSearchSuggestions() {
        const searchInput = document.querySelector('input[name="query"]');
        if (!searchInput) return;

        searchInput.addEventListener('input', (e) => {
            const query = e.target.value.trim();

            // مسح الـ timeout السابق
            if (this.searchTimeout) {
                clearTimeout(this.searchTimeout);
            }

            // إعداد timeout جديد للاقتراحات
            if (query.length >= 2) {
                this.searchTimeout = setTimeout(() => {
                    this.showSearchSuggestions(query);
                }, 300);
            } else {
                this.hideSearchSuggestions();
            }
        });

        // إخفاء الاقتراحات عند النقر خارج مربع البحث
        document.addEventListener('click', (e) => {
            if (!e.target.closest('.search-input-container')) {
                this.hideSearchSuggestions();
            }
        });
    }

    // عرض اقتراحات البحث
    async showSearchSuggestions(query) {
        try {
            const response = await fetch(`/Search/Suggestions?query=${encodeURIComponent(query)}`);
            const suggestions = await response.json();

            if (response.ok && suggestions.length > 0) {
                this.displaySearchSuggestions(suggestions);
            } else {
                this.hideSearchSuggestions();
            }
        } catch (error) {
            console.error('خطأ في تحميل اقتراحات البحث:', error);
            this.hideSearchSuggestions();
        }
    }

    // عرض الاقتراحات في القائمة المنسدلة
    displaySearchSuggestions(suggestions) {
        const searchInput = document.querySelector('input[name="query"]');
        let suggestionsContainer = document.querySelector('.search-suggestions');

        if (!suggestionsContainer) {
            suggestionsContainer = document.createElement('div');
            suggestionsContainer.className = 'search-suggestions';
            searchInput.parentNode.appendChild(suggestionsContainer);
        }

        suggestionsContainer.innerHTML = '';

        suggestions.forEach(suggestion => {
            const suggestionItem = document.createElement('div');
            suggestionItem.className = 'search-suggestion';
            suggestionItem.textContent = suggestion;
            suggestionItem.addEventListener('click', () => {
                searchInput.value = suggestion;
                this.hideSearchSuggestions();
                searchInput.form.submit();
            });
            suggestionsContainer.appendChild(suggestionItem);
        });

        suggestionsContainer.style.display = 'block';
    }

    // إخفاء اقتراحات البحث
    hideSearchSuggestions() {
        const suggestionsContainer = document.querySelector('.search-suggestions');
        if (suggestionsContainer) {
            suggestionsContainer.style.display = 'none';
        }
    }

    // مراقبة تغييرات الفلاتر
    watchFilterChanges() {
        // مراقبة تغيير طريقة العرض (grid/list)
        const viewButtons = document.querySelectorAll('[data-view]');
        viewButtons.forEach(button => {
            button.addEventListener('click', (e) => {
                e.preventDefault();
                const viewMode = button.getAttribute('data-view');
                this.changeViewMode(viewMode);
            });
        });
    }

    // تغيير طريقة العرض
    changeViewMode(viewMode) {
        const currentUrl = new URL(window.location);
        currentUrl.searchParams.set('view', viewMode);
        currentUrl.searchParams.set('page', '1'); // إعادة تعيين الصفحة إلى الأولى
        window.location.href = currentUrl.toString();
    }
}

// تهيئة محرك البحث عند تحميل الصفحة
document.addEventListener('DOMContentLoaded', function() {
    new SearchEngine();
});

// دوال مساعدة للـ View
function changeSort() {
    const sortSelect = document.querySelector('select[name="sort"]');
    if (sortSelect) {
        const currentUrl = new URL(window.location);
        currentUrl.searchParams.set('sort', sortSelect.value);
        currentUrl.searchParams.set('page', '1'); // إعادة تعيين الصفحة إلى الأولى
        window.location.href = currentUrl.toString();
    }
}

function changeViewMode(viewMode) {
    const currentUrl = new URL(window.location);
    currentUrl.searchParams.set('view', viewMode);
    currentUrl.searchParams.set('page', '1');
    window.location.href = currentUrl.toString();
}
