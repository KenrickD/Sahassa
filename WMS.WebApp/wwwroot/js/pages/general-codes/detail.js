// General Code Type Detail Module
(function () {
    // Initialize the module
    function init() {
        document.addEventListener('DOMContentLoaded', function () {
            setupCodeTypeForm();
        });
    }

    function setupCodeTypeForm() {
        $("#codeTypeForm").on("submit", function (e) {
            e.preventDefault(); // Prevent the default form submission

            // Create a FormData object
            var formData = new FormData(this);
            var url = $(this).attr('action');
            const codeTypeId = $('#currentCodeTypeId').val();
            var isEdit = false;

            if (codeTypeId && codeTypeId !== '00000000-0000-0000-0000-000000000000') {
                url = "/GeneralCode/EditCodeType";
                isEdit = true;
            }

            // Add warehouse validation
            const warehouseSelect = $(this).find('select[name="WarehouseId"]');
            if (warehouseSelect.length && !warehouseSelect.val()) {
                showErrorToast('Please select a warehouse.');
                return;
            }

            // Add code type name validation
            const codeTypeName = $(this).find('input[name="Name"]').val();
            if (!codeTypeName || codeTypeName.trim() === '') {
                showErrorToast('Please enter a code type name.');
                return;
            }

            // Validate code type name format (recommend uppercase with underscores)
            const namePattern = /^[A-Z_][A-Z0-9_]*$/;
            if (!namePattern.test(codeTypeName.trim())) {
                if (confirm('Code type name should follow the pattern "UPPERCASE_WITH_UNDERSCORES" (e.g., ORDER_STATUS). Do you want to continue anyway?')) {
                    // User confirmed, proceed
                } else {
                    return;
                }
            }

            // Show loading state
            const submitBtn = $(this).find('button[type="submit"]');
            const originalText = submitBtn.text();
            submitBtn.prop('disabled', true).text('Saving...');

            // Submit the form via AJAX
            $.ajax({
                url: url,
                type: $(this).attr('method'),
                data: formData,
                processData: false, // Required for FormData
                contentType: false, // Required for FormData
                headers: {
                    'X-Requested-With': 'XMLHttpRequest', // Mark as AJAX request
                    'X-CSRF-TOKEN': $('meta[name="csrf-token"]').attr('content')
                },
                success: function (response) {
                    if (response.success) {
                        showSuccessToast("Code type saved successfully!");

                        if (!isEdit) {
                            setTimeout(() => {
                                window.location.href = '/GeneralCode';
                            }, 1500);
                        } else {
                            setTimeout(() => {
                                window.location.reload();
                            }, 1500);
                        }
                    } else {
                        if (response.errors) {
                            for (const key in response.errors) {
                                const errors = response.errors[key];
                                if (Array.isArray(errors) && errors.length > 0) {
                                    showErrorToast(errors[0]);
                                }
                            }
                        } else {
                            showErrorToast(response.message || "An error occurred while saving the code type.");
                        }
                    }
                },
                error: function (xhr) {
                    console.error('Code type save error:', xhr);

                    // Handle errors
                    if (xhr.responseJSON) {
                        // If validation errors, display them
                        var errors = xhr.responseJSON.errors;
                        if (errors) {
                            $.each(errors, function (key, value) {
                                showErrorToast(value[0]);
                            });
                        } else {
                            showErrorToast(xhr.responseJSON.message || "An error occurred while saving the code type.");
                        }
                    } else {
                        showErrorToast("An error occurred while saving the code type.");
                    }
                },
                complete: function () {
                    // Reset button state
                    submitBtn.prop('disabled', false).text(originalText);
                }
            });
        });

        // Auto-format code type name to uppercase with underscores
        $('input[name="Name"]').on('input', function () {
            const nameInput = $(this);
            let value = nameInput.val();

            // Convert to uppercase and replace spaces with underscores
            value = value.toUpperCase().replace(/\s+/g, '_');

            // Remove characters that aren't letters, numbers, or underscores
            value = value.replace(/[^A-Z0-9_]/g, '');

            // Ensure it doesn't start with a number
            if (/^[0-9]/.test(value)) {
                value = 'CODE_' + value;
            }

            // Limit length to 100 characters
            if (value.length > 100) {
                value = value.substring(0, 100);
            }

            if (nameInput.val() !== value) {
                nameInput.val(value);

                // Show formatting hint
                if (value !== nameInput.data('original-value')) {
                    showInfoToast('Code type name formatted automatically');
                    nameInput.data('original-value', value);
                }
            }
        });

        // Validate on blur and show suggestions
        $('input[name="Name"]').on('blur', function () {
            const nameInput = $(this);
            const value = nameInput.val().trim();

            if (value) {
                // Check if it follows the recommended pattern
                const namePattern = /^[A-Z_][A-Z0-9_]*$/;
                if (!namePattern.test(value)) {
                    // Show warning with suggested format
                    const suggested = value.toUpperCase().replace(/[^A-Z0-9_]/g, '_').replace(/_+/g, '_');
                    showWarningToast(`Consider using format: ${suggested}`);
                }

                // Check for common code type names and suggest descriptions
                suggestDescription(value);
            }
        });

        // Add character counter for description
        const descriptionTextarea = $('textarea[name="Description"]');
        if (descriptionTextarea.length) {
            const maxLength = 255;

            const counter = $('<small class="text-sm text-gray-500 mt-1">' +
                (descriptionTextarea.val() ? descriptionTextarea.val().length : 0) +
                '/' + maxLength + ' characters</small>');
            descriptionTextarea.after(counter);

            descriptionTextarea.on('input', function () {
                const currentLength = $(this).val().length;
                counter.text(currentLength + '/' + maxLength + ' characters');

                if (currentLength > maxLength * 0.9) {
                    counter.addClass('text-warning-600').removeClass('text-gray-500');
                } else {
                    counter.addClass('text-gray-500').removeClass('text-warning-600');
                }
            });
        }

        // Show helpful examples
        showCodeTypeExamples();
    }

    // Suggest description based on common code type names
    function suggestDescription(codeTypeName) {
        const descriptionField = $('textarea[name="Description"]');

        // Only suggest if description is empty
        if (descriptionField.val().trim() === '') {
            const suggestions = {
                'ORDER_STATUS': 'Status values for order processing (Draft, Confirmed, Processing, Shipped, etc.)',
                'TASK_PRIORITY': 'Priority levels for tasks (Low, Medium, High, Urgent)',
                'INVENTORY_STATUS': 'Status values for inventory items (Available, Reserved, Damaged, etc.)',
                'SHIPPING_METHOD': 'Available shipping methods (Standard, Express, Overnight, etc.)',
                'PAYMENT_STATUS': 'Payment processing status (Pending, Paid, Failed, Refunded)',
                'USER_ROLE': 'System user roles and permissions',
                'LOCATION_TYPE': 'Types of warehouse locations (Rack, Floor, Staging, etc.)',
                'MOVEMENT_TYPE': 'Types of inventory movements (Receiving, Picking, Transfer, etc.)',
                'QUALITY_STATUS': 'Quality control status values',
                'DOCUMENT_TYPE': 'Types of documents in the system'
            };

            if (suggestions[codeTypeName]) {
                descriptionField.val(suggestions[codeTypeName]);
                descriptionField.addClass('bg-yellow-50 dark:bg-yellow-900/20');
                setTimeout(() => {
                    descriptionField.removeClass('bg-yellow-50 dark:bg-yellow-900/20');
                }, 2000);

                showInfoToast('Description suggested based on code type name');

                // Update character counter
                descriptionField.trigger('input');
            }
        }
    }

    // Show helpful examples in a tooltip or info box
    function showCodeTypeExamples() {
        const nameInput = $('input[name="Name"]');

        // Add info icon with examples
        if (nameInput.length && !nameInput.siblings('.code-type-examples').length) {
            const examplesHtml = `
                <div class="code-type-examples mt-2 p-3 bg-blue-50 dark:bg-blue-900/20 rounded-lg text-sm">
                    <div class="font-medium text-blue-800 dark:text-blue-200 mb-2">
                        <iconify-icon icon="solar:info-circle-outline" class="mr-1"></iconify-icon>
                        Common Code Type Examples:
                    </div>
                    <div class="grid grid-cols-1 md:grid-cols-2 gap-2 text-blue-700 dark:text-blue-300">
                        <div>• ORDER_STATUS</div>
                        <div>• TASK_PRIORITY</div>
                        <div>• INVENTORY_STATUS</div>
                        <div>• SHIPPING_METHOD</div>
                        <div>• PAYMENT_STATUS</div>
                        <div>• LOCATION_TYPE</div>
                    </div>
                </div>
            `;

            nameInput.parent().after(examplesHtml);
        }
    }

    // Expose public methods
    window.CodeTypeModule = {
        init: init
    };
})();

// Initialize the module
CodeTypeModule.init();