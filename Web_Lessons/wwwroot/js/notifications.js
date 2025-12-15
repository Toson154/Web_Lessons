// إنشاء ملف جديد: wwwroot/js/notifications.js
class NotificationManager {
    constructor() {
        this.baseUrl = '/api/notification';
        this.unreadCount = 0;
        this.pollingInterval = 30000; // 30 seconds
        this.initialize();
    }

    initialize() {
        // Load initial count
        this.loadNotificationCount();

        // Start polling for new notifications
        this.startPolling();

        // Setup event listeners
        this.setupEventListeners();
    }

    async loadNotificationCount() {
        try {
            const response = await fetch(`${this.baseUrl}/count`);
            const data = await response.json();

            if (data.count !== undefined) {
                this.unreadCount = data.count;
                this.updateBadge();
            }
        } catch (error) {
            console.error('Error loading notification count:', error);
        }
    }

    async loadNotifications(page = 1, pageSize = 20, unreadOnly = false) {
        try {
            const url = `${this.baseUrl}?page=${page}&pageSize=${pageSize}&unreadOnly=${unreadOnly}`;
            const response = await fetch(url);

            if (!response.ok) {
                throw new Error('Failed to load notifications');
            }

            return await response.json();
        } catch (error) {
            console.error('Error loading notifications:', error);
            return null;
        }
    }

    async markAsRead(notificationId) {
        try {
            const response = await fetch(`${this.baseUrl}/${notificationId}/read`, {
                method: 'PUT'
            });

            if (response.ok) {
                const data = await response.json();
                this.unreadCount = data.unreadCount || 0;
                this.updateBadge();
                return true;
            }
        } catch (error) {
            console.error('Error marking notification as read:', error);
        }
        return false;
    }

    async markAllAsRead() {
        try {
            const response = await fetch(`${this.baseUrl}/read-all`, {
                method: 'PUT'
            });

            if (response.ok) {
                const data = await response.json();
                this.unreadCount = 0;
                this.updateBadge();
                return true;
            }
        } catch (error) {
            console.error('Error marking all as read:', error);
        }
        return false;
    }

    async deleteNotification(notificationId) {
        try {
            const response = await fetch(`${this.baseUrl}/${notificationId}`, {
                method: 'DELETE'
            });

            if (response.ok) {
                const data = await response.json();
                this.unreadCount = data.unreadCount || 0;
                this.updateBadge();
                return true;
            }
        } catch (error) {
            console.error('Error deleting notification:', error);
        }
        return false;
    }

    async generateTestNotification() {
        try {
            const response = await fetch(`${this.baseUrl}/generate-test`, {
                method: 'POST'
            });

            if (response.ok) {
                const data = await response.json();
                if (data.success) {
                    this.unreadCount++;
                    this.updateBadge();
                    this.showToast('Test notification created', 'success');
                    return true;
                }
            }
        } catch (error) {
            console.error('Error generating test notification:', error);
        }
        return false;
    }

    updateBadge() {
        const badge = document.getElementById('notificationBadge');
        if (badge) {
            if (this.unreadCount > 0) {
                badge.textContent = this.unreadCount > 99 ? '99+' : this.unreadCount;
                badge.style.display = 'inline-block';
            } else {
                badge.style.display = 'none';
            }
        }
    }

    startPolling() {
        setInterval(() => {
            if (document.visibilityState === 'visible') {
                this.loadNotificationCount();
            }
        }, this.pollingInterval);
    }

    setupEventListeners() {
        // Listen for page visibility changes
        document.addEventListener('visibilitychange', () => {
            if (document.visibilityState === 'visible') {
                this.loadNotificationCount();
            }
        });
    }

    showToast(message, type = 'info') {
        // Toast implementation
        console.log(`${type}: ${message}`);
    }

    getNotificationIcon(type) {
        const icons = {
            'new_comment': 'fas fa-comment',
            'lesson_completed': 'fas fa-check-circle',
            'reaction': 'fas fa-thumbs-up',
            'new_reply': 'fas fa-reply',
            'mention': 'fas fa-at',
            'enrollment': 'fas fa-user-plus',
            'new_message': 'fas fa-comments',
            'system': 'fas fa-cog',
            'notes_saved': 'fas fa-edit',
            'course_completed': 'fas fa-trophy'
        };
        return icons[type] || 'fas fa-bell';
    }

    getNotificationColor(type) {
        const colors = {
            'new_comment': 'text-primary',
            'lesson_completed': 'text-success',
            'reaction': 'text-warning',
            'new_reply': 'text-info',
            'mention': 'text-danger',
            'enrollment': 'text-success',
            'new_message': 'text-primary',
            'system': 'text-secondary',
            'notes_saved': 'text-warning',
            'course_completed': 'text-success'
        };
        return colors[type] || 'text-secondary';
    }
}

// Initialize when DOM is loaded
document.addEventListener('DOMContentLoaded', () => {
    window.notificationManager = new NotificationManager();
});