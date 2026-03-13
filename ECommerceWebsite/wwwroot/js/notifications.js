// SignalR connection for real-time notifications
let notificationConnection;

document.addEventListener('DOMContentLoaded', function() {
    if (window.signalR && window.signalR.HubConnectionBuilder) {
        initializeSignalR();
    } else {
        console.warn('SignalR client not loaded; attempting dynamic load...');
        document.addEventListener('signalr:ready', function() {
            initializeSignalR();
        }, { once: true });
    }
    setupNotificationHandlers();
});

function initializeSignalR() {
    // Create connection to the SignalR hub
    notificationConnection = new signalR.HubConnectionBuilder()
        .withUrl('/notificationHub')
        .withAutomaticReconnect({
            nextRetryDelayInMilliseconds: retryContext => {
                if (retryContext.elapsedMilliseconds < 60000) {
                    // If we've been reconnecting for less than 60 seconds, wait between 0-2 seconds
                    return Math.random() * 2000;
                } else {
                    // If we've been reconnecting for more than 60 seconds, stop reconnecting
                    return null;
                }
            }
        })
        .configureLogging(signalR.LogLevel.Information)
        .build();

    // Start the connection
    notificationConnection.start()
        .then(() => {
            console.log('Connected to notification hub');
            updateConnectionStatus(true);
        })
        .catch(err => {
            console.error('Error connecting to notification hub:', err);
            updateConnectionStatus(false);
        });

    // Handle reconnection events
    notificationConnection.onreconnecting(error => {
        console.log('Reconnecting to notification hub...');
        updateConnectionStatus(false);
    });

    notificationConnection.onreconnected(connectionId => {
        console.log('Reconnected to notification hub');
        updateConnectionStatus(true);
        // Refresh notifications after reconnection
        loadNotifications();
    });

    notificationConnection.onclose(error => {
        console.log('Disconnected from notification hub');
        updateConnectionStatus(false);
    });

    // Handle incoming notifications
    notificationConnection.on('ReceiveNotification', (notification) => {
        console.log('Received notification:', notification);
        addNotificationToUI(notification);
        updateUnreadCount(1);
        showNotificationToast(notification);
    });

    // Handle multiple notifications
    notificationConnection.on('ReceiveNotifications', (notifications) => {
        console.log('Received notifications:', notifications);
        if (Array.isArray(notifications)) {
            notifications.forEach(notification => {
                addNotificationToUI(notification);
            });
            updateUnreadCount(notifications.length);
        }
    });
}

function setupNotificationHandlers() {
    // Mark notification as read when clicked
    document.addEventListener('click', function(e) {
        const markAsReadBtn = e.target.closest('.mark-as-read');
        if (markAsReadBtn) {
            e.preventDefault();
            const notificationId = markAsReadBtn.dataset.id;
            markNotificationAsRead(notificationId, markAsReadBtn.closest('.notification-item'));
        }

        // Mark all as read
        const markAllReadBtn = e.target.closest('#markAllRead');
        if (markAllReadBtn) {
            e.preventDefault();
            markAllNotificationsAsRead();
        }
    });

    // Handle notification dropdown events
    const notificationDropdown = document.getElementById('notificationDropdown');
    if (notificationDropdown) {
        notificationDropdown.addEventListener('shown.bs.dropdown', function() {
            // Mark all visible notifications as read when dropdown is shown
            const unreadNotifications = document.querySelectorAll('.notification-item.unread');
            if (unreadNotifications.length > 0) {
                const notificationIds = Array.from(unreadNotifications).map(el => el.dataset.notificationId);
                markMultipleNotificationsAsRead(notificationIds);
            }
            
            // Load notifications if not already loaded
            const notificationList = document.getElementById('notificationList');
            if (notificationList && notificationList.children.length === 0) {
                loadNotifications();
            }
        });
    }
}

function loadNotifications() {
    fetch('/api/notification?unreadOnly=true')
        .then(response => response.json())
        .then(notifications => {
            renderNotifications(notifications);
            updateUnreadCount(notifications.filter(n => !n.isRead).length);
        })
        .catch(error => {
            console.error('Error loading notifications:', error);
            showError('Failed to load notifications. Please try again.');
        });
}

function renderNotifications(notifications) {
    const notificationList = document.getElementById('notificationList');
    if (!notificationList) return;

    if (!notifications || notifications.length === 0) {
        notificationList.innerHTML = `
            <div class="empty-notifications">
                <i class="far fa-bell-slash fa-2x mb-2"></i>
                <p class="mb-0">No new notifications</p>
            </div>`;
        return;
    }

    const notificationItems = notifications.map(notification => `
        <a href="${notification.actionUrl || '#'}" 
           class="dropdown-item notification-item ${!notification.isRead ? 'unread' : ''}" 
           data-notification-id="${notification.id}">
            <div class="d-flex align-items-start">
                <div class="notification-icon me-3">
                    <div class="rounded-circle bg-light p-2 text-${getNotificationColor(notification.type)}">
                        <i class="${notification.icon || 'fas fa-bell'}"></i>
                    </div>
                </div>
                <div class="flex-grow-1">
                    <div class="d-flex justify-content-between">
                        <h6 class="notification-title mb-1">${escapeHtml(notification.title)}</h6>
                        <small class="text-muted">${formatTimeAgo(notification.createdAt)}</small>
                    </div>
                    <p class="notification-message mb-0">${escapeHtml(notification.message)}</p>
                </div>
                ${!notification.isRead ? `
                    <button class="btn btn-sm btn-link mark-as-read" data-id="${notification.id}" title="Mark as read">
                        <i class="far fa-circle"></i>
                    </button>
                ` : ''}
            </div>
        </a>
    `).join('');

    notificationList.innerHTML = notificationItems;
}

function addNotificationToUI(notification) {
    const notificationList = document.getElementById('notificationList');
    if (!notificationList) return;

    // Remove "no notifications" message if present
    const emptyMessage = notificationList.querySelector('.empty-notifications');
    if (emptyMessage) {
        notificationList.removeChild(emptyMessage);
    }

    // Create new notification element
    const notificationElement = document.createElement('a');
    notificationElement.href = notification.actionUrl || '#';
    notificationElement.className = `dropdown-item notification-item ${!notification.isRead ? 'unread' : ''}`;
    notificationElement.dataset.notificationId = notification.id;
    
    notificationElement.innerHTML = `
        <div class="d-flex align-items-start">
            <div class="notification-icon me-3">
                <div class="rounded-circle bg-light p-2 text-${getNotificationColor(notification.type)}">
                    <i class="${notification.icon || 'fas fa-bell'}"></i>
                </div>
            </div>
            <div class="flex-grow-1">
                <div class="d-flex justify-content-between">
                    <h6 class="notification-title mb-1">${escapeHtml(notification.title)}</h6>
                    <small class="text-muted">${formatTimeAgo(notification.createdAt)}</small>
                </div>
                <p class="notification-message mb-0">${escapeHtml(notification.message)}</p>
            </div>
            ${!notification.isRead ? `
                <button class="btn btn-sm btn-link mark-as-read" data-id="${notification.id}" title="Mark as read">
                    <i class="far fa-circle"></i>
                </button>
            ` : ''}
        </div>
    `;

    // Add new notification to the top of the list
    if (notificationList.firstChild) {
        notificationList.insertBefore(notificationElement, notificationList.firstChild);
    } else {
        notificationList.appendChild(notificationElement);
    }
}

function markNotificationAsRead(notificationId, element) {
    if (!notificationConnection || notificationConnection.state !== signalR.HubConnectionState.Connected) {
        console.warn('SignalR connection not available, using fallback');
        fetch(`/api/notification/${notificationId}/mark-read`, { method: 'POST' })
            .then(response => {
                if (response.ok) {
                    if (element) {
                        element.classList.remove('unread');
                        const markAsReadBtn = element.querySelector('.mark-as-read');
                        if (markAsReadBtn) {
                            markAsReadBtn.remove();
                        }
                        updateUnreadCount(-1);
                    }
                }
            });
        return;
    }

    // Use SignalR if available
    notificationConnection.invoke('MarkAsRead', parseInt(notificationId))
        .then(() => {
            if (element) {
                element.classList.remove('unread');
                const markAsReadBtn = element.querySelector('.mark-as-read');
                if (markAsReadBtn) {
                    markAsReadBtn.remove();
                }
                updateUnreadCount(-1);
            }
        })
        .catch(err => {
            console.error('Error marking notification as read:', err);
            showError('Failed to mark notification as read');
        });
}

function markMultipleNotificationsAsRead(notificationIds) {
    if (!notificationConnection || notificationConnection.state !== signalR.HubConnectionState.Connected) {
        console.warn('SignalR connection not available, using fallback');
        fetch('/api/notification/mark-all-read', { method: 'POST' })
            .then(response => {
                if (response.ok) {
                    document.querySelectorAll('.notification-item.unread').forEach(item => {
                        item.classList.remove('unread');
                        const markAsReadBtn = item.querySelector('.mark-as-read');
                        if (markAsReadBtn) {
                            markAsReadBtn.remove();
                        }
                    });
                    updateUnreadCount(0);
                }
            });
        return;
    }

    // Use SignalR if available
    notificationConnection.invoke('MarkAllAsRead')
        .then(() => {
            document.querySelectorAll('.notification-item.unread').forEach(item => {
                item.classList.remove('unread');
                const markAsReadBtn = item.querySelector('.mark-as-read');
                if (markAsReadBtn) {
                    markAsReadBtn.remove();
                }
            });
            updateUnreadCount(0);
        })
        .catch(err => {
            console.error('Error marking all notifications as read:', err);
            showError('Failed to mark all notifications as read');
        });
}

function markAllNotificationsAsRead() {
    if (!notificationConnection || notificationConnection.state !== signalR.HubConnectionState.Connected) {
        console.warn('SignalR connection not available, using fallback');
        fetch('/api/notification/mark-all-read', { method: 'POST' })
            .then(response => {
                if (response.ok) {
                    document.querySelectorAll('.notification-item.unread').forEach(item => {
                        item.classList.remove('unread');
                        const markAsReadBtn = item.querySelector('.mark-as-read');
                        if (markAsReadBtn) {
                            markAsReadBtn.remove();
                        }
                    });
                    updateUnreadCount(0);
                    showSuccess('All notifications marked as read');
                }
            });
        return;
    }

    // Use SignalR if available
    notificationConnection.invoke('MarkAllAsRead')
        .then(() => {
            document.querySelectorAll('.notification-item.unread').forEach(item => {
                item.classList.remove('unread');
                const markAsReadBtn = item.querySelector('.mark-as-read');
                if (markAsReadBtn) {
                    markAsReadBtn.remove();
                }
            });
            updateUnreadCount(0);
            showSuccess('All notifications marked as read');
        })
        .catch(err => {
            console.error('Error marking all notifications as read:', err);
            showError('Failed to mark all notifications as read');
        });
}

function updateUnreadCount(change = 0) {
    const unreadCountElement = document.getElementById('unreadCount');
    if (!unreadCountElement) return;

    let currentCount = parseInt(unreadCountElement.textContent) || 0;
    let newCount = currentCount + change;
    
    // Ensure count doesn't go below 0
    newCount = Math.max(0, newCount);
    
    unreadCountElement.textContent = newCount;
    unreadCountElement.style.display = newCount > 0 ? 'inline-block' : 'none';
    
    // Update browser tab title
    const title = document.title.replace(/^\(\d+\)\s*/, '');
    document.title = newCount > 0 ? `(${newCount}) ${title}` : title;
}

function showNotificationToast(notification) {
    // Check if browser supports notifications
    if ('Notification' in window && Notification.permission === 'granted') {
        const notificationOptions = {
            body: notification.message,
            icon: notification.icon || '/images/notification-icon.png',
            tag: `notification-${notification.id}`,
            data: {
                url: notification.actionUrl || window.location.href
            }
        };

        const notificationPopup = new Notification(notification.title, notificationOptions);
        
        notificationPopup.onclick = function() {
            window.focus();
            if (notification.actionUrl) {
                window.location.href = notification.actionUrl;
            }
            notificationPopup.close();
        };
    }
    // Fallback to toastr if available
    else if (window.toastr) {
        const notificationType = getNotificationType(notification.type);
        toastr[notificationType](notification.message, notification.title, {
            timeOut: 5000,
            extendedTimeOut: 2000,
            closeButton: true,
            tapToDismiss: true,
            onclick: function() {
                if (notification.actionUrl) {
                    window.location.href = notification.actionUrl;
                }
            }
        });
    }
}

function updateConnectionStatus(isConnected) {
    const connectionStatus = document.getElementById('connectionStatus');
    if (connectionStatus) {
        connectionStatus.textContent = isConnected ? 'Connected' : 'Disconnected';
        connectionStatus.className = isConnected ? 'text-success' : 'text-danger';
    }
}

// Helper functions
function escapeHtml(unsafe) {
    if (!unsafe) return '';
    return unsafe
        .toString()
        .replace(/&/g, '&amp;')
        .replace(/</g, '&lt;')
        .replace(/>/g, '&gt;')
        .replace(/"/g, '&quot;')
        .replace(/'/g, '&#039;');
}

function formatTimeAgo(dateString) {
    const date = new Date(dateString);
    const now = new Date();
    const seconds = Math.floor((now - date) / 1000);
    
    let interval = Math.floor(seconds / 31536000);
    if (interval >= 1) return `${interval}y ago`;
    
    interval = Math.floor(seconds / 2592000);
    if (interval >= 1) return `${interval}mo ago`;
    
    interval = Math.floor(seconds / 86400);
    if (interval >= 1) return `${interval}d ago`;
    
    interval = Math.floor(seconds / 3600);
    if (interval >= 1) return `${interval}h ago`;
    
    interval = Math.floor(seconds / 60);
    if (interval >= 1) return `${interval}m ago`;
    
    return 'Just now';
}

function getNotificationColor(type) {
    const colors = {
        'Info': 'info',
        'Success': 'success',
        'Warning': 'warning',
        'Danger': 'danger',
        'Order': 'primary',
        'Stock': 'warning',
        'Review': 'info',
        'System': 'secondary'
    };
    return colors[type] || 'primary';
}

function getNotificationType(type) {
    const types = {
        'Success': 'success',
        'Warning': 'warning',
        'Danger': 'error',
        'Info': 'info',
        'Order': 'info',
        'Stock': 'warning',
        'Review': 'success',
        'System': 'info'
    };
    return types[type] || 'info';
}

function showError(message) {
    if (window.toastr) {
        toastr.error(message);
    } else {
        console.error(message);
    }
}

function showSuccess(message) {
    if (window.toastr) {
        toastr.success(message);
    } else {
        console.log(message);
    }
}

// Request notification permission when the page loads
if ('Notification' in window) {
    if (Notification.permission !== 'denied') {
        Notification.requestPermission();
    }
}
