(function () {
    // Initialize the module
    function init() {
        document.addEventListener('DOMContentLoaded', function () {
            setupZoneForm();
        });
    }

    function setupZoneForm() {
        $("#zoneForm").on("submit", function (e) {
            e.preventDefault(); // Prevent the default form submission

            // Create a FormData object
            var formData = new FormData(this);
            var url = $(this).attr('action');
            const zoneId = $('#currentZoneId').val();
            var isEdit = false;

            if (zoneId && zoneId !== '00000000-0000-0000-0000-000000000000') {
                url = "/Zone/Edit";
                isEdit = true;
            }

            // Add warehouse validation
            const warehouseSelect = $(this).find('select[name="WarehouseId"]');
            if (warehouseSelect.length && !warehouseSelect.val()) {
                showErrorToast('Please select a warehouse.');
                submitBtn.prop('disabled', false).text(originalText);
                return;
            }

            // Add zone name validation
            const zoneName = $(this).find('input[name="Name"]').val();
            if (!zoneName || zoneName.trim() === '') {
                showErrorToast('Please enter a zone name.');
                return;
            }

            // Add zone code validation
            const zoneCode = $(this).find('input[name="Code"]').val();
            if (!zoneCode || zoneCode.trim() === '') {
                showErrorToast('Please enter a zone code.');
                return;
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
                        showSuccessToast("Zone saved successfully!");

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
                            showErrorToast(data.message || "An error occurred while saving the zone.");
                        }
                    }
                },
                error: function (xhr) {
                    console.error('Zone save error:', xhr);

                    // Handle errors
                    if (xhr.responseJSON) {
                        // If validation errors, display them
                        var errors = xhr.responseJSON.errors;
                        if (errors) {
                            $.each(errors, function (key, value) {
                                showErrorToast(value[0]);
                            });
                        } else {
                            showErrorToast(xhr.responseJSON.message || "An error occurred while saving the zone.");
                        }
                    } else {
                        showErrorToast("An error occurred while saving the zone.");
                    }
                },
                complete: function () {
                    // Reset button state
                    submitBtn.prop('disabled', false).text(originalText);
                }
            });
        });

        // Auto-generate zone code based on zone name if code is empty
        $('input[name="Name"]').on('blur', function () {
            const nameInput = $(this);
            const codeInput = $('input[name="Code"]');

            // Only auto-generate if code field is empty
            if (codeInput.val().trim() === '' && nameInput.val().trim() !== '') {
                const zoneName = nameInput.val().trim();
                // Generate code by taking first 3 characters and making uppercase
                const generatedCode = zoneName.substring(0, 3).toUpperCase();
                codeInput.val(generatedCode);

                // Add visual feedback
                codeInput.addClass('bg-yellow-50 dark:bg-yellow-900/20');
                setTimeout(() => {
                    codeInput.removeClass('bg-yellow-50 dark:bg-yellow-900/20');
                }, 2000);

                showInfoToast('Zone code auto-generated from name');
            }
        });

        // Validate zone code format (alphanumeric, no spaces)
        $('input[name="Code"]').on('input', function () {
            const codeInput = $(this);
            let value = codeInput.val();

            // Remove non-alphanumeric characters and convert to uppercase
            value = value.replace(/[^a-zA-Z0-9]/g, '').toUpperCase();

            // Limit to 10 characters
            if (value.length > 10) {
                value = value.substring(0, 10);
            }

            if (codeInput.val() !== value) {
                codeInput.val(value);
            }
        });

        // Add character counter for description
        const descriptionTextarea = $('textarea[name="Description"]');
        if (descriptionTextarea.length) {
            const maxLength = 250;
            
            const counter = $('<small class="text-sm text-gray-500 mt-1">0/' + maxLength + ' characters</small>');
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
            descriptionTextarea.trigger("input");
        }
    }

    // Expose public methods
    window.ZoneModule = {
        init: init
    };
})();

// Initialize the module
ZoneModule.init();