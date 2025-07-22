function showOnProgressLoading(message) {
    Swal.fire({
        title: 'Loading...',
        text: message,
        allowOutsideClick: false,
        didOpen: () => {
            Swal.showLoading();
        }
    });
}

// Apply dark mode styling if system is in dark mode
function applyDarkModeToTable() {
    if (document.documentElement.classList.contains('dark')) {
        $('.dataTables_wrapper select, .dataTables_wrapper input').addClass('bg-neutral-800 text-white border-neutral-700');
    }
}

function applyEventForDataTableDarkMode() {
    // Handle dark mode toggle (if your app has a dark mode toggle)
    window.matchMedia('(prefers-color-scheme: dark)').addEventListener('change', applyDarkModeToTable);
}

// Toast notification functions
function showToast(message, type = 'success') {
    // Default options
    toastr.options = {
        "closeButton": true,
        "progressBar": true,
        "positionClass": "toast-top-right",
        "timeOut": type === 'error' ? 5000 : 3000,
        "extendedTimeOut": 1000,
        "preventDuplicates": true,
        "newestOnTop": true,
        "showEasing": "swing",
        "hideEasing": "linear",
        "showMethod": "fadeIn",
        "hideMethod": "fadeOut"
    };

    // Show toast based on type
    switch (type) {
        case 'success':
            toastr.success(message, 'Success');
            break;
        case 'error':
            toastr.error(message, 'Error');
            break;
        case 'warning':
            toastr.warning(message, 'Warning');
            break;
        case 'info':
            toastr.info(message, 'Information');
            break;
    }
}

// Convenience functions
function showSuccessToast(message) {
    showToast(message, 'success');
}

function showErrorToast(message) {
    showToast(message, 'error');
}

function showWarningToast(message) {
    showToast(message, 'warning');
}

function showInfoToast(message) {
    showToast(message, 'info');
}

// Confirmable toast - returns a Promise
function showConfirmToast(message, confirmButtonText = 'Confirm', cancelButtonText = 'Cancel') {
    return new Promise((resolve) => {
        // First hide any existing toasts
        toastr.clear();

        // Create custom toast with buttons
        toastr.options = {
            "closeButton": false,
            "timeOut": 0,
            "extendedTimeOut": 0,
            "positionClass": "toast-top-center",
            "preventDuplicates": true,
            "newestOnTop": false,
            "showEasing": "swing",
            "hideEasing": "linear",
            "showMethod": "fadeIn",
            "hideMethod": "fadeOut",
            "tapToDismiss": false
        };

        // Create the toast
        const toast = toastr.warning(
            `<div>${message}</div>
            <div class="mt-2 text-right">
                <button type="button" class="cancel-btn btn btn-sm btn-outline-light mr-2">${cancelButtonText}</button>
                <button type="button" class="confirm-btn btn btn-sm btn-warning">${confirmButtonText}</button>
            </div>`,
            'Confirmation'
        );

        // Add button event listeners
        $(toast).find('.confirm-btn').on('click', function () {
            toastr.clear();
            resolve(true);
        });

        $(toast).find('.cancel-btn').on('click', function () {
            toastr.clear();
            resolve(false);
        });
    });
}

function refreshNavbarViewComponent() {
    $.ajax({
        url: '/Home/RenderNavbar',
        type: 'GET',
        success: function (result) {
            $('#navbarViewComponent').html(result);
        },
        error: function (xhr, status, error) {
            console.error('Error loading navbar:', error);
        }
    });
}

// Utility function to escape HTML
function escapeHtml(text) {
    if (!text) return '';
    const map = {
        '&': '&amp;',
        '<': '&lt;',
        '>': '&gt;',
        '"': '&quot;',
        "'": '&#039;'
    };
    return text.toString().replace(/[&<>"']/g, function (m) { return map[m]; });
}

function formatFileSize(bytes) {
    if (bytes === 0) return '0 Bytes';
    const k = 1024;
    const sizes = ['Bytes', 'KB', 'MB', 'GB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    return parseFloat((bytes / Math.pow(k, i)).toFixed(2)) + ' ' + sizes[i];
}