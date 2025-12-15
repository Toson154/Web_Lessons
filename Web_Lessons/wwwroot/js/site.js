// Toast notifications
function showToast(message, type = 'success') {
    const toastContainer = $('.toast-container');
    if (!toastContainer.length) {
        $('body').append('<div class="toast-container position-fixed top-0 end-0 p-3"></div>');
    }

    const toastId = 'toast-' + Date.now();
    const toastHtml = `
        <div id="${toastId}" class="toast align-items-center text-white bg-${type} border-0" role="alert">
            <div class="d-flex">
                <div class="toast-body">
                    <i class="fas fa-${type === 'success' ? 'check-circle' : 'exclamation-circle'} me-2"></i>
                    ${message}
                </div>
                <button type="button" class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast"></button>
            </div>
        </div>
    `;

    $('.toast-container').append(toastHtml);
    const toast = new bootstrap.Toast(document.getElementById(toastId));
    toast.show();

    // Remove after hide
    $(`#${toastId}`).on('hidden.bs.toast', function () {
        $(this).remove();
    });
}

// File upload with preview
function setupFileUpload(inputId, previewId) {
    const input = document.getElementById(inputId);
    const preview = document.getElementById(previewId);

    if (input && preview) {
        input.addEventListener('change', function (e) {
            const file = e.target.files[0];
            if (file) {
                const reader = new FileReader();
                reader.onload = function (e) {
                    if (preview.tagName === 'IMG') {
                        preview.src = e.target.result;
                    } else {
                        preview.innerHTML = `<img src="${e.target.result}" class="img-fluid">`;
                    }
                }
                reader.readAsDataURL(file);
            }
        });
    }
}

// Progress ring animation
function updateProgressRing(elementId, percentage) {
    const element = document.getElementById(elementId);
    if (element) {
        element.style.setProperty('--progress', percentage + '%');
        element.setAttribute('data-progress', percentage);
    }
}

// Initialize on page load
$(document).ready(function () {
    // Auto-hide alerts after 5 seconds
    setTimeout(function () {
        $('.alert:not(.alert-permanent)').fadeOut(500);
    }, 5000);

    // Enable tooltips
    $('[data-bs-toggle="tooltip"]').tooltip();

    // Enable popovers
    $('[data-bs-toggle="popover"]').popover();

    // Confirm delete actions
    $('.confirm-delete').on('click', function (e) {
        if (!confirm('Are you sure you want to delete this item?')) {
            e.preventDefault();
        }
    });

    // Initialize file upload previews
    $('input[type="file"][data-preview]').each(function () {
        setupFileUpload(this.id, $(this).data('preview'));
    });

    // Video player controls
    $('video').each(function () {
        const video = this;
        video.addEventListener('timeupdate', function () {
            const progress = (video.currentTime / video.duration) * 100;
            $(video).data('progress', progress);
        });
    });
});