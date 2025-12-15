// Student Chat & Notifications Manager
const StudentUI = {
    // Chat Management
    chat: {
        cache: {
            data: null,
            timestamp: null,
            expiry: 30000
        },

        init: function () {
            $('#chatToggleBtn').click(this.toggleChat);
            $('.close-chat-btn').click(() => $('#chatContainer').hide());

            // Close when clicking outside
            $(document).click((e) => {
                if (!$(e.target).closest('.chat-widget').length) {
                    $('#chatContainer').hide();
                }
            });
        },

        toggleChat: function (e) {
            e.stopPropagation();
            const chatContainer = $('#chatContainer');
            chatContainer.toggle();

            if (chatContainer.is(':visible')) {
                StudentUI.chat.loadChatList();
            }
        },

        loadUnreadCount: function () {
            $.ajax({
                url: '/Student/GetUnreadMessagesCount',
                type: 'GET',
                success: function (response) {
                    if (response.success) {
                        StudentUI.chat.updateBadge(response.count);
                    }
                }
            });
        },

        updateBadge: function (count) {
            const badges = $('#unreadChatBadge, #navbarChatCount');
            if (count > 0) {
                badges.text(count > 99 ? '99+' : count).show();
            } else {
                badges.hide();
            }
        }
    },

    // Notifications Management
    notifications: {
        init: function () {
            // Load notifications when dropdown opens
            $('#notificationDropdown').on('show.bs.dropdown', () => {
                this.loadDropdownNotifications();
            });

            // Mark all as read
            $('#markAllAsReadBtn').click((e) => {
                e.preventDefault();
                e.stopPropagation();
                this.markAllAsRead();
            });

            // Auto-refresh every minute
            setInterval(() => this.loadUnreadCount(), 60000);
        },

        loadDropdownNotifications: function () {
            const container = $('#notificationsDropdown');
            container.html(`
                <div class="text-center py-3">
                    <div class="spinner-border spinner-border-sm text-primary"></div>
                    <p class="text-muted small mt-2">Loading...</p>
                </div>
            `);

            $.ajax({
                url: '/Student/GetDashboardNotifications',
                type: 'GET',
                success: function (response) {
                    StudentUI.notifications.renderDropdown(response);
                },
                error: function () {
                    container.html(`
                        <div class="text-center py-4">
                            <i class="fas fa-exclamation-triangle text-warning"></i>
                            <p class="text-muted small mt-2">Error loading notifications</p>
                        </div>
                    `);
                }
            });
        },

        renderDropdown: function (data) {
            const container = $('#notificationsDropdown');

            if (!data.success || !data.notifications || data.notifications.length === 0) {
                container.html(`
                    <div class="text-center py-4">
                        <i class="fas fa-bell-slash fa-2x text-muted mb-2"></i>
                        <p class="text-muted mb-3">No notifications</p>
                        <button class="btn btn-sm btn-outline-primary" 
                                onclick="StudentUI.notifications.generateTest()">
                            Generate Test
                        </button>
                    </div>
                `);
                return;
            }

            let html = '';
            data.notifications.forEach(notification => {
                const icon = this.getIcon(notification.type);
                const color = this.getColor(notification.type);
                const isUnread = !notification.isRead;
                const link = this.getLink(notification);

                html += `
                    <a class="dropdown-item notification-item ${isUnread ? 'unread' : ''}"
                       href="${link}"
                       data-notification-id="${notification.id}">
                        <div class="d-flex">
                            <div class="me-3">
                                <i class="${icon} ${color}"></i>
                            </div>
                            <div class="flex-grow-1">
                                <div class="d-flex justify-content-between">
                                    <h6 class="mb-0">${notification.title}</h6>
                                    <small class="text-muted">${notification.timeAgo}</small>
                                </div>
                                <p class="mb-0 small">${notification.message}</p>
                                ${isUnread ? '<span class="badge bg-danger mt-1">New</span>' : ''}
                            </div>
                        </div>
                    </a>
                `;
            });

            container.html(html);

            // Add click handlers
            $('.notification-item').click((e) => {
                const notificationId = $(e.currentTarget).data('notification-id');
                this.markAsRead(notificationId, $(e.currentTarget));
            });
        },

        getIcon: function (type) {
            const icons = {
                'new_comment': 'fas fa-comment',
                'lesson_completed': 'fas fa-check-circle',
                'reaction': 'fas fa-thumbs-up',
                'new_message': 'fas fa-comments',
                'system': 'fas fa-cog'
            };
            return icons[type] || 'fas fa-bell';
        },

        getColor: function (type) {
            const colors = {
                'new_comment': 'text-primary',
                'lesson_completed': 'text-success',
                'reaction': 'text-warning',
                'new_message': 'text-info',
                'system': 'text-secondary'
            };
            return colors[type] || 'text-secondary';
        },

        getLink: function (notification) {
            if (!notification.relatedId || !notification.relatedType) return '#';

            switch (notification.relatedType) {
                case 'lesson': return `/Student/LessonDetails/${notification.relatedId}`;
                case 'chat': return `/Chat/Details/${notification.relatedId}`;
                case 'course': return `/Student/CourseDetails/${notification.relatedId}`;
                default: return '#';
            }
        },

        markAsRead: function (notificationId, element) {
            $.ajax({
                url: '/Student/MarkNotificationAsRead',
                type: 'POST',
                data: {
                    id: notificationId,
                    __RequestVerificationToken: $('input[name="__RequestVerificationToken"]').val()
                },
                success: function (response) {
                    if (response.success) {
                        $(element).removeClass('unread');
                        StudentUI.notifications.updateBadge(response.unreadCount);
                    }
                }
            });
        },

        markAllAsRead: function () {
            $.ajax({
                url: '/Student/MarkAllNotificationsAsRead',
                type: 'POST',
                headers: {
                    'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val()
                },
                success: function (response) {
                    if (response.success) {
                        StudentUI.notifications.loadDropdownNotifications();
                        StudentUI.notifications.updateBadge(0);
                        StudentUI.utils.showToast('All notifications marked as read', 'success');
                    }
                }
            });
        },

        loadUnreadCount: function () {
            $.ajax({
                url: '/Student/GetUnreadNotificationsCount',
                type: 'GET',
                success: function (response) {
                    if (response.success) {
                        StudentUI.notifications.updateBadge(response.count);
                    }
                }
            });
        },

        updateBadge: function (count) {
            const badge = $('#notificationBadge');
            if (count > 0) {
                badge.text(count).show();
            } else {
                badge.hide();
            }
        },

        generateTest: function () {
            $.ajax({
                url: '/Student/GenerateTestNotification',
                type: 'POST',
                headers: {
                    'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val()
                },
                success: function (response) {
                    if (response.success) {
                        StudentUI.notifications.loadDropdownNotifications();
                        StudentUI.utils.showToast('Test notification created', 'success');
                    }
                }
            });
        }
    },

    // Utility functions
    utils: {
        showToast: function (message, type = 'info') {
            const icon = type === 'success' ? 'check-circle' :
                type === 'danger' ? 'exclamation-circle' : 'info-circle';

            // Create toast container if it doesn't exist
            let container = $('.toast-container');
            if (container.length === 0) {
                container = $('<div class="toast-container position-fixed top-0 end-0 p-3"></div>');
                $('body').append(container);
            }

            const toast = $(`
                <div class="toast align-items-center text-white bg-${type} border-0">
                    <div class="d-flex">
                        <div class="toast-body">
                            <i class="fas fa-${icon} me-2"></i>${message}
                        </div>
                        <button class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast"></button>
                    </div>
                </div>
            `);

            container.append(toast);
            const bsToast = new bootstrap.Toast(toast[0], { autohide: true, delay: 3000 });
            bsToast.show();

            toast.on('hidden.bs.toast', function () {
                $(this).remove();
            });
        }
    },

    // Initialize all components
    init: function () {
        this.chat.init();
        this.notifications.init();

        // Load initial counts
        this.chat.loadUnreadCount();
        this.notifications.loadUnreadCount();

        // Set intervals for auto-refresh
        setInterval(() => this.chat.loadUnreadCount(), 30000);

        // Welcome message
        setTimeout(() => {
            const userName = $('#userName').data('name') || 'Student';
            this.utils.showToast(`Welcome back, ${userName}!`, 'success');
        }, 1000);
    }
};

// Initialize when document is ready
$(document).ready(function () {
    StudentUI.init();
});

// Make it globally accessible
window.StudentUI = StudentUI;