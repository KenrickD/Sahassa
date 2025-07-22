(function () {
    // Private variables
    let isEdit = false;
    let hasEditAccess = false;

    // Initialize the module
    function init() {
        document.addEventListener('DOMContentLoaded', function () {
            initializeVariables();
            setupProductForm();
            //setupImageHandling();
            //setupValidation();
        });
    }

    function initializeVariables() {
        const currentProductId = document.getElementById('currentProductId')?.value;
        isEdit = currentProductId && currentProductId !== '00000000-0000-0000-0000-000000000000';

        // Check if form elements are readonly to determine edit access
        const nameInput = document.querySelector('input[name="Name"]');
        hasEditAccess = nameInput && !nameInput.readOnly;
    }

    function setupProductForm() {
        const form = document.getElementById("productForm");
        if (!form) return;

        form.addEventListener("submit", function (e) {
            e.preventDefault();

            // Validate required fields
            if (!validateForm()) {
                return;
            }

            // Create FormData object
            const formData = new FormData(this);
            let url = form.getAttribute('action');
            var isEdit = false;

            if (url.toLowerCase().includes('edit')) {
                isEdit = true;
            }

            // Show loading state
            const submitBtn = form.querySelector('button[type="submit"]');
            const originalText = submitBtn.textContent;
            submitBtn.disabled = true;
            submitBtn.textContent = 'Saving...';

            // Submit the form via AJAX
            $.ajax({
                url: url,
                method: 'POST',
                data: formData,
                processData: false, // Important for FormData
                contentType: false, // Important for FormData
                headers: {
                    'X-Requested-With': 'XMLHttpRequest',
                    'X-CSRF-TOKEN': $('meta[name="csrf-token"]').attr('content') || ''
                },
                success: function (data) {
                    if (data.success !== false) {
                        showSuccessToast("Product saved successfully!");

                        if (!isEdit) {
                            setTimeout(() => {
                                window.location.reload();
                            }, 1500);
                        }
                    } else {
                        if (data.errors) {
                            for (const key in data.errors) {
                                const errors = data.errors[key];
                                if (Array.isArray(errors) && errors.length > 0) {
                                    showErrorToast(errors[0]);
                                }
                            }
                        } else {
                            showErrorToast(data.message || "An error occurred while saving the product.");
                        }
                    }
                },
                error: function (xhr, status, error) {
                    console.error('Product save error:', error);
                    showErrorToast("An error occurred while saving the product.");
                },
                complete: function () {
                    // Reset button state
                    submitBtn.disabled = false;
                    submitBtn.textContent = originalText;
                }
            });
        });
    }

    function setupImageHandling() {
        const imageInput = document.querySelector('input[name="ProductImage"]');
        const removeImageCheckbox = document.querySelector('input[name="RemoveProductImage"]');

        if (imageInput) {
            imageInput.addEventListener('change', function () {
                previewProductImage(this);
            });
        }

        if (removeImageCheckbox) {
            removeImageCheckbox.addEventListener('change', function () {
                toggleImageRemoval(this);
            });
        }
    }

    function previewProductImage(input) {
        if (input.files && input.files[0]) {
            const reader = new FileReader();
            reader.onload = function (e) {
                const preview = document.getElementById('product-image-preview');
                const placeholder = document.getElementById('product-image-placeholder');

                if (preview && placeholder) {
                    preview.src = e.target.result;
                    preview.classList.remove('hidden');
                    placeholder.classList.add('hidden');
                }
            }
            reader.readAsDataURL(input.files[0]);
        }
    }

    function toggleImageRemoval(checkbox) {
        const preview = document.getElementById('product-image-preview');
        const imageInput = document.querySelector('input[name="ProductImage"]');

        if (checkbox.checked) {
            if (preview) preview.classList.add('opacity-50');
            if (imageInput) imageInput.disabled = true;
        } else {
            if (preview) preview.classList.remove('opacity-50');
            if (imageInput) imageInput.disabled = false;
        }
    }

    function setupValidation() {
        // Stock level validation
        const minStockInput = document.querySelector('input[name="MinStockLevel"]');
        const maxStockInput = document.querySelector('input[name="MaxStockLevel"]');
        const reorderPointInput = document.querySelector('input[name="ReorderPoint"]');

        if (minStockInput && maxStockInput) {
            [minStockInput, maxStockInput].forEach(input => {
                input.addEventListener('blur', validateStockLevels);
            });
        }

        if (reorderPointInput && maxStockInput) {
            [reorderPointInput, maxStockInput].forEach(input => {
                input.addEventListener('blur', validateReorderPoint);
            });
        }
    }

    function validateStockLevels() {
        const minStock = parseFloat(document.querySelector('input[name="MinStockLevel"]')?.value) || 0;
        const maxStock = parseFloat(document.querySelector('input[name="MaxStockLevel"]')?.value) || 0;

        if (maxStock > 0 && minStock > maxStock) {
            showWarningToast('Min stock level should not be greater than max stock level');
        }
    }

    function validateReorderPoint() {
        const reorderPoint = parseFloat(document.querySelector('input[name="ReorderPoint"]')?.value) || 0;
        const maxStock = parseFloat(document.querySelector('input[name="MaxStockLevel"]')?.value) || 0;

        if (maxStock > 0 && reorderPoint > maxStock) {
            showWarningToast('Reorder point should not be greater than max stock level');
        }
    }

    function validateForm() {
        let isValid = true;
        const requiredFields = [
            { name: 'ClientId', label: 'Client' },
            { name: 'ProductTypeId', label: 'Product Type' },
            { name: 'Name', label: 'Product Name' },
            { name: 'SKU', label: 'SKU' }
        ];

        // Clear previous validation errors
        document.querySelectorAll('.text-danger-600').forEach(el => {
            if (el.textContent.includes('is required')) {
                el.textContent = '';
            }
        });

        requiredFields.forEach(field => {
            const input = document.querySelector(`[name="${field.name}"]`);
            if (input && !input.value.trim()) {
                isValid = false;

                // Find validation span for this field
                const validationSpan = input.closest('.col-span-12, .md\\:col-span-6, .md\\:col-span-4, .md\\:col-span-3')
                    ?.querySelector('.text-danger-600');

                if (validationSpan) {
                    validationSpan.textContent = `${field.label} is required`;
                }

                // Add error styling
                input.classList.add('border-red-500');

                // Remove error styling after user starts typing
                input.addEventListener('input', function () {
                    this.classList.remove('border-red-500');
                    if (validationSpan) {
                        validationSpan.textContent = '';
                    }
                }, { once: true });
            }
        });

        if (!isValid) {
            showErrorToast('Please fill in all required fields');
        }

        return isValid;
    }

    // Expose public methods
    window.ProductDetailModule = {
        init: init,
        previewProductImage: previewProductImage,
        toggleImageRemoval: toggleImageRemoval
    };
})();

// Initialize the module
ProductDetailModule.init();

// Global functions for HTML onclick handlers
function previewProductImage(input) {
    ProductDetailModule.previewProductImage(input);
}

function toggleImageRemoval(checkbox) {
    ProductDetailModule.toggleImageRemoval(checkbox);
}