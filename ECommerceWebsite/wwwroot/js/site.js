// E-Commerce Website JavaScript

$(document).ready(function() {
    // Initialize cart count on page load
    if ($('#cart-count').length) {
        updateCartCount();
    }

    // Auto-hide alerts after 5 seconds
    setTimeout(function() {
        $('.alert').fadeOut('slow');
    }, 5000);

    // Smooth scrolling for anchor links
    $('a[href^="#"]').on('click', function(event) {
        var target = $(this.getAttribute('href'));
        if (target.length) {
            event.preventDefault();
            $('html, body').stop().animate({
                scrollTop: target.offset().top - 100
            }, 1000);
        }
    });

    // Mobile menu (delegated handlers to be robust across partial loads)
    $(document).on('click', '#hamburger', function (e) {
        e.preventDefault();
        var $btn = $(this);
        $btn.toggleClass('active');
        $('#mobileNav').toggleClass('active');
        $('#mobileOverlay').toggleClass('active');
        $('body').toggleClass('menu-open');
    });

    $(document).on('click', '#mobileOverlay', function () {
        $(this).removeClass('active');
        $('#mobileNav').removeClass('active');
        $('#hamburger').removeClass('active');
        $('body').removeClass('menu-open');
    });

    $(document).on('click', '#mobileNav a:not(.dropdown-toggle)', function () {
        $('#mobileOverlay').removeClass('active');
        $('#mobileNav').removeClass('active');
        $('#hamburger').removeClass('active');
        $('body').removeClass('menu-open');
    });

    // Fallback for broken product images
    $('img').on('error', function() {
        try {
            var src = (this.getAttribute('src') || '').toLowerCase();
            if (src.indexOf('/images/products/') !== -1) {
                this.onerror = null; // prevent infinite loop
                this.src = '/images/products/phone1.jpg';
            }
        } catch (e) { /* ignore */ }
    });

    // Product image zoom effect
    $('.product-card img').hover(
        function() {
            $(this).css('transform', 'scale(1.05)');
        },
        function() {
            $(this).css('transform', 'scale(1)');
        }
    );

    // Quantity input validation
    $('input[type="number"]').on('input', function() {
        var min = parseInt($(this).attr('min')) || 1;
        var max = parseInt($(this).attr('max')) || 999;
        var value = parseInt($(this).val());

        if (value < min) {
            $(this).val(min);
        } else if (value > max) {
            $(this).val(max);
        }
    });

    // Wishlist button functionality (delegated for dynamic content)
    $(document).on('click', '.Wishlist-btn, .wishlist-btn', function() {
        var productId = $(this).data('product-id');
        var button = $(this);
        var icon = button.find('i');

        button.prop('disabled', true);
        icon.removeClass('far').addClass('fas'); // Change to solid heart

        $.ajax({
            url: '/Wishlist/AddToWishlist',
            method: 'POST',
            contentType: 'application/json',
            data: JSON.stringify({ ProductId: productId }),
            headers: {
                'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val()
            }
        })
        .done(function (response) {
            if (response.success) {
                icon.removeClass('far').addClass('fas'); // Keep solid heart
                // Optional: Show temporary success state
                var originalIcon = icon.attr('class');
                setTimeout(function() {
                    icon.attr('class', originalIcon);
                    button.prop('disabled', false);
                }, 2000);
            } else {
                alert(response.message || 'Could not add to wishlist');
                icon.removeClass('fas').addClass('far'); // Revert to outline
                button.prop('disabled', false);
            }
        })
        .fail(function () {
            alert('Error adding to wishlist');
            icon.removeClass('fas').addClass('far'); // Revert to outline
            button.prop('disabled', false);
        });
    });
});

// Update cart count function
function updateCartCount() {
    $.get('/Cart/GetCartCount')
        .done(function(response) {
            if (response && response.count !== undefined) {
                $('#cart-count').text(response.count);

                // Hide badge if count is 0
                if (response.count === 0) {
                    $('#cart-count').hide();
                } else {
                    $('#cart-count').show();
                }
            }
        })
        .fail(function() {
            console.log('Failed to update cart count');
        });
}

// Email validation function
function validateEmail(email) {
    var re = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    return re.test(email);
}

// Format currency function
function formatCurrency(amount) {
    return '$' + parseFloat(amount).toFixed(2);
}

// Show loading overlay
function showLoading() {
    if ($('#loading-overlay').length === 0) {
        $('body').append('<div id="loading-overlay" class="position-fixed w-100 h-100 d-flex justify-content-center align-items-center" style="top:0;left:0;background:rgba(0,0,0,0.5);z-index:9999;"><div class="spinner-border text-light" role="status"><span class="visually-hidden">Loading...</span></div></div>');
    }
}

// Hide loading overlay
function hideLoading() {
    $('#loading-overlay').remove();
}

// -----------------------------
// Notifications (Header dropdown)
// -----------------------------

function getRequestVerificationToken() {
    const tokenInput = document.querySelector('input[name="__RequestVerificationToken"]');
    return tokenInput ? tokenInput.value : null;
}

function updateNotificationCount(count) {
    var $badge = $('#notification-count');
    if ($badge.length === 0) return;
    $badge.text(count);
    if (count > 0) { $badge.show(); } else { $badge.hide(); }
}

function renderNotificationItem(n) {
    var iconClass = n.icon && n.icon.length ? n.icon : 'fas fa-bell';
    var href = n.actionUrl && n.actionUrl.length ? n.actionUrl : '#';
    var extra = n.isRead ? '' : ' style="background:#f9f9f9"';
    return (
        '<a href="' + href + '" class="dropdown-item" data-id="' + n.id + '"' + extra + '>' +
        '  <div class="d-flex align-items-center">' +
        '    <div class="me-2"><i class="' + iconClass + '"></i></div>' +
        '    <div class="flex-grow-1">' +
        '      <div class="d-flex justify-content-between">' +
        '        <strong>' + (n.title || '') + '</strong>' +
        '        <small class="text-muted">' + (n.type || '') + '</small>' +
        '      </div>' +
        '      <div class="small text-muted">' + (n.message || '') + '</div>' +
        '    </div>' +
        '  </div>' +
        '</a>'
    );
}

function loadNotifications() {
    var $list = $('#notification-list');
    if ($list.length === 0) return;

    $.get('/api/notification')
        .done(function(data) {
            $list.empty();
            if (Array.isArray(data) && data.length) {
                var unread = 0;
                data.forEach(function(n) {
                    if (!n.isRead) unread++;
                    $list.append(renderNotificationItem(n));
                });
                updateNotificationCount(unread);
            } else {
                updateNotificationCount(0);
                $list.append('<div class="dropdown-item-text text-center text-muted">No notifications</div>');
            }
        })
        .fail(function(xhr) {
            if (xhr && xhr.status === 401) {
                // not logged in; hide badge
                updateNotificationCount(0);
            } else {
                console.error('Failed to load notifications');
            }
        });
}

function markAllNotificationsAsRead() {
    var token = getRequestVerificationToken();
    $.ajax({
        url: '/api/notification/mark-all-read',
        method: 'POST',
        headers: token ? { 'RequestVerificationToken': token } : {},
    })
    .done(function() {
        updateNotificationCount(0);
        var $list = $('#notification-list');
        if ($list.length) {
            // remove highlight
            $list.find('.dropdown-item').css('background', '');
        }
    })
    .fail(function(xhr) {
        if (xhr && xhr.status === 401) return; // ignore when not signed in
        console.error('Failed to mark all as read');
    });
}
