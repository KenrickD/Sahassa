(function () {
    // Private variables
    let originalConfigData = {};
    let currentConfigData = {};
    let isConfigLoaded = false;

    // Initialize the module
    function init() {
        document.addEventListener('DOMContentLoaded', function () {
            initializeTabBehavior();
            setupWarehouseForm();
            initializeConfigurationManagement();
        });
    }

    function setupWarehouseForm() {
        $("#warehouseForm").on("submit", function (e) {
            e.preventDefault(); // Prevent the default form submission

            // Create a FormData object
            var formData = new FormData(this);
            var url = $(this).attr('action');
            const warehouseId = $('#currentWarehouseId').val();
            var isEdit = false;

            if (warehouseId && warehouseId !== '00000000-0000-0000-0000-000000000000') {
                url = "/Warehouse/Edit";
                isEdit = true;
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
                        showSuccessToast("Warehouse saved successfully!");

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
                            showErrorToast(data.message || "An error occurred while saving the warehouse.");
                        }
                    }
                },
                error: function (xhr) {
                    console.error('Warehouse save error:', xhr);

                    // Handle errors
                    if (xhr.responseJSON) {
                        // If validation errors, display them
                        var errors = xhr.responseJSON.errors;
                        if (errors) {
                            $.each(errors, function (key, value) {
                                showErrorToast(value[0]);
                            });
                        } else {
                            showErrorToast(xhr.responseJSON.message || "An error occurred while saving the warehouse.");
                        }
                    } else {
                        showErrorToast("An error occurred while saving the warehouse.");
                    }
                },
                complete: function () {
                    // Reset button state
                    submitBtn.prop('disabled', false).text(originalText);
                }
            });
        });
    }

    function initializeTabBehavior() {
        // Initialize the tabs if not already done by the framework
        if (typeof FlowbiteModule !== 'undefined' && FlowbiteModule.initTabs) {
            FlowbiteModule.initTabs();
        } else {
            // Simple tab implementation
            $('[data-tabs-toggle]').each(function () {
                const tabsId = $(this).attr('id');
                const tabsContentId = $(this).data('tabs-toggle');

                $(`#${tabsId} [data-tabs-target]`).on('click', function () {
                    const targetId = $(this).data('tabs-target');

                    // Hide all content
                    $(`${tabsContentId} > div`).addClass('hidden');

                    // Show target content
                    $(targetId).removeClass('hidden');

                    // Update tab states
                    $(`#${tabsId} [data-tabs-target]`).attr('aria-selected', 'false');
                    $(this).attr('aria-selected', 'true');

                    // Load configuration when config tab is activated
                    if (targetId === '#configuration') {
                        loadWarehouseConfiguration();
                    }
                });

                // Activate first tab by default
                $(`#${tabsId} [data-tabs-target]`).first().click();
            });
        }
    }

    // Configuration Management Functions
    function initializeConfigurationManagement() {
        // Save configuration button
        $('#saveConfigBtn').on('click', function () {
            if ($(this).prop('disabled')) return;
            saveConfigurationChanges();
        });

        // Reset configuration button
        $('#resetConfigBtn').on('click', function () {
            if ($(this).prop('disabled')) return;
            resetConfigurationChanges();
        });

        // Track configuration changes
        $('#configurationForm input, #configurationForm select').on('change', function () {
            trackConfigurationChanges();
            updateConfigurationUI();
        });
    }

    // Load warehouse configuration
    function loadWarehouseConfiguration() {
        if (isConfigLoaded) return;

        const warehouseId = $('#currentWarehouseId').val();
        if (!warehouseId || warehouseId === '00000000-0000-0000-0000-000000000000') {
            console.log('No warehouse ID found for configuration');
            return;
        }

        // Load current form values as original data
        originalConfigData = getCurrentConfigFormData();
        currentConfigData = { ...originalConfigData };
        isConfigLoaded = true;

        updateConfigurationUI();
    }

    // Get current configuration form data
    function getCurrentConfigFormData() {
        return {
            requiresLotTracking: $('#requiresLotTracking').is(':checked'),
            requiresExpirationDates: $('#requiresExpirationDates').is(':checked'),
            usesSerialNumbers: $('#usesSerialNumbers').is(':checked'),
            autoAssignLocations: $('#autoAssignLocations').is(':checked'),
            inventoryStrategy: $('select[name="InventoryStrategy"]').val(),
            defaultMeasurementUnit: $('input[name="DefaultMeasurementUnit"]').val() || '',
            defaultDaysToExpiry: parseInt($('input[name="DefaultDaysToExpiry"]').val()) || 365,
            barcodeFormat: $('input[name="BarcodeFormat"]').val() || '',
            companyLogoUrl: $('input[name="CompanyLogoUrl"]').val() || '',
            themePrimaryColor: $('input[name="ThemePrimaryColor"]').val() || '',
            themeSecondaryColor: $('input[name="ThemeSecondaryColor"]').val() || ''
        };
    }

    // Track configuration changes
    function trackConfigurationChanges() {
        currentConfigData = getCurrentConfigFormData();
    }

    // Check if configuration has changes
    function hasConfigurationChanges() {
        return JSON.stringify(originalConfigData) !== JSON.stringify(currentConfigData);
    }

    // Update configuration UI elements
    function updateConfigurationUI() {
        const hasChanges = hasConfigurationChanges();

        // Enable/disable save and reset buttons
        $('#saveConfigBtn').prop('disabled', !hasChanges);
        $('#resetConfigBtn').prop('disabled', !hasChanges);
    }

    // Save configuration changes
    function saveConfigurationChanges() {
        const warehouseId = $('#currentWarehouseId').val();
        if (!warehouseId || warehouseId === '00000000-0000-0000-0000-000000000000') {
            showErrorToast('Warehouse ID not found');
            return;
        }

        trackConfigurationChanges();
        const changes = getCurrentConfigFormData();

        if (!hasConfigurationChanges()) {
            showInfoToast('No changes to save');
            return;
        }

        // Show loading state
        const saveBtn = $('#saveConfigBtn');
        const originalText = saveBtn.text();
        saveBtn.prop('disabled', true).text('Saving...');

        // Prepare data for submission - include all warehouse data
        const formData = new FormData();

        // Get all warehouse form data
        $('#warehouseForm').find('input, select, textarea').each(function () {
            const name = $(this).attr('name');
            const value = $(this).val();
            if (name && value !== undefined) {
                formData.append(name, value);
            }
        });

        // Override with configuration changes
        Object.keys(changes).forEach(key => {
            // Convert camelCase to PascalCase for model binding
            const pascalKey = key.charAt(0).toUpperCase() + key.slice(1);
            formData.set(pascalKey, changes[key]);
        });

        $.ajax({
            url: '/Warehouse/Edit',
            type: 'POST',
            data: formData,
            processData: false,
            contentType: false,
            headers: {
                'X-Requested-With': 'XMLHttpRequest',
                'X-CSRF-TOKEN': $('meta[name="csrf-token"]').attr('content')
            },
            success: function (response) {
                if (response.success !== false) {
                    showSuccessToast('Configuration saved successfully');
                    originalConfigData = { ...currentConfigData };
                    updateConfigurationUI();
                } else {
                    showErrorToast(response.message || 'Failed to save configuration');
                }
            },
            error: function (xhr) {
                console.error('Configuration save error:', xhr);

                let errorMessage = 'Failed to save configuration';
                if (xhr.responseJSON && xhr.responseJSON.message) {
                    errorMessage = xhr.responseJSON.message;
                } else if (xhr.status === 400) {
                    errorMessage = 'Invalid configuration data';
                } else if (xhr.status === 404) {
                    errorMessage = 'Warehouse not found';
                } else if (xhr.status === 500) {
                    errorMessage = 'Server error occurred';
                }

                showErrorToast(errorMessage);
            },
            complete: function () {
                // Reset button state
                saveBtn.prop('disabled', false).text(originalText);
            }
        });
    }

    // Reset configuration changes
    function resetConfigurationChanges() {
        if (!hasConfigurationChanges()) {
            showInfoToast('No changes to reset');
            return;
        }

        // Reset form values to original data
        $('#requiresLotTracking').prop('checked', originalConfigData.requiresLotTracking);
        $('#requiresExpirationDates').prop('checked', originalConfigData.requiresExpirationDates);
        $('#usesSerialNumbers').prop('checked', originalConfigData.usesSerialNumbers);
        $('#autoAssignLocations').prop('checked', originalConfigData.autoAssignLocations);
        $('select[name="InventoryStrategy"]').val(originalConfigData.inventoryStrategy);
        $('input[name="DefaultMeasurementUnit"]').val(originalConfigData.defaultMeasurementUnit);
        $('input[name="DefaultDaysToExpiry"]').val(originalConfigData.defaultDaysToExpiry);
        $('input[name="BarcodeFormat"]').val(originalConfigData.barcodeFormat);
        $('input[name="CompanyLogoUrl"]').val(originalConfigData.companyLogoUrl);
        $('input[name="ThemePrimaryColor"]').val(originalConfigData.themePrimaryColor);
        $('input[name="ThemeSecondaryColor"]').val(originalConfigData.themeSecondaryColor);

        // Update current data
        currentConfigData = { ...originalConfigData };
        updateConfigurationUI();

        showInfoToast('Configuration reset successfully');
    }

    // Expose public methods
    window.WarehouseModule = {
        init: init,
        loadWarehouseConfiguration: loadWarehouseConfiguration
    };
})();

// Initialize the module
WarehouseModule.init();