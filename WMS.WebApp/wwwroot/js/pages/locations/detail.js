// Location Detail Module
(function () {
    // Private variables
    let isEdit = false;
    let hasEditAccess = false;

    // Initialize the module
    function init() {
        document.addEventListener('DOMContentLoaded', function () {
            initializeVariables();
            setupLocationForm();
            setupWarehouseZoneHandling();
            initializeAutoGeneration();
        });
    }

    function initializeVariables() {
        const currentLocationId = document.getElementById('currentLocationId')?.value;
        isEdit = currentLocationId && currentLocationId !== '00000000-0000-0000-0000-000000000000';

        // Check if form elements are readonly to determine edit access
        const nameInput = document.querySelector('input[name="Name"]');
        hasEditAccess = nameInput && !nameInput.readOnly;
    }

    function setupLocationForm() {
        const form = document.getElementById("locationForm");
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
                        showSuccessToast("Location saved successfully!");

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
                            showErrorToast(data.message || "An error occurred while saving the location.");
                        }
                    }
                },
                error: function (xhr, status, error) {
                    // Handle errors
                    if (xhr.responseJSON) {
                        // If validation errors, display them
                        var errors = xhr.responseJSON.errors;
                        if (errors) {
                            $.each(errors, function (key, value) {
                                showErrorToast(value[0]);
                            });
                        } else {
                            showErrorToast(xhr.responseJSON.message || "An error occurred while saving the location.");
                        }
                    } else {
                        showErrorToast("An error occurred while saving the location.");
                    }
                },
                complete: function () {
                    // Reset button state
                    submitBtn.disabled = false;
                    submitBtn.textContent = originalText;
                }
            });
        });
    }

    function setupWarehouseZoneHandling() {
        const warehouseSelect = document.getElementById('warehouseSelect');
        const zoneSelect = document.getElementById('zoneSelect');

        if (!warehouseSelect || !zoneSelect) return;

        // Handle warehouse selection change
        warehouseSelect.addEventListener('change', function () {
            const warehouseId = this.value;
            loadZonesByWarehouse(warehouseId);
        });

        // Load zones on page load if warehouse is already selected
        if (warehouseSelect.value && !isEdit) {
            loadZonesByWarehouse(warehouseSelect.value);
        }
    }

    function loadZonesByWarehouse(warehouseId) {
        const zoneSelect = document.getElementById('zoneSelect');
        const zoneLoading = document.getElementById('zone-loading');
        const zoneEmpty = document.getElementById('zone-empty');

        if (!warehouseId) {
            // Clear zones if no warehouse selected
            zoneSelect.innerHTML = '<option value="">Select Zone</option>';
            zoneSelect.disabled = true;
            hideZoneMessages();
            return;
        }

        // Show loading
        showZoneLoading();
        zoneSelect.disabled = true;

        $.ajax({
            url: '/Location/GetZonesByWarehouse',
            method: 'GET',
            data: { warehouseId: warehouseId },
            headers: {
                'X-Requested-With': 'XMLHttpRequest',
                'X-CSRF-TOKEN': $('meta[name="csrf-token"]').attr('content') || ''
            },
            success: function (data) {
                hideZoneMessages();

                if (data.success && data.data && data.data.length > 0) {
                    // Clear existing options
                    zoneSelect.innerHTML = '<option value="">Select Zone</option>';

                    // Add zone options
                    data.data.forEach(zone => {
                        const option = document.createElement('option');
                        option.value = zone.id;
                        option.textContent = `${zone.name} (${zone.code})`;
                        zoneSelect.appendChild(option);
                    });

                    zoneSelect.disabled = false;
                } else {
                    // No zones found
                    zoneSelect.innerHTML = '<option value="">No zones available</option>';
                    showZoneEmpty();
                }
            },
            error: function (xhr, status, error) {
                console.error('Error loading zones:', error);
                hideZoneMessages();
                zoneSelect.innerHTML = '<option value="">Error loading zones</option>';
                showErrorToast('Failed to load zones');
            }
        });
    }

    function showZoneLoading() {
        const zoneLoading = document.getElementById('zone-loading');
        const zoneEmpty = document.getElementById('zone-empty');

        if (zoneLoading) zoneLoading.classList.remove('hidden');
        if (zoneEmpty) zoneEmpty.classList.add('hidden');
    }

    function showZoneEmpty() {
        const zoneLoading = document.getElementById('zone-loading');
        const zoneEmpty = document.getElementById('zone-empty');

        if (zoneLoading) zoneLoading.classList.add('hidden');
        if (zoneEmpty) zoneEmpty.classList.remove('hidden');
    }

    function hideZoneMessages() {
        const zoneLoading = document.getElementById('zone-loading');
        const zoneEmpty = document.getElementById('zone-empty');

        if (zoneLoading) zoneLoading.classList.add('hidden');
        if (zoneEmpty) zoneEmpty.classList.add('hidden');
    }

    function validateForm() {
        let isValid = true;
        const requiredFields = [
            { name: 'WarehouseId', label: 'Warehouse' },
            { name: 'ZoneId', label: 'Zone' },
            { name: 'Name', label: 'Location Name' },
            { name: 'Code', label: 'Location Code' }
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

        // Validate position uniqueness if all position fields are filled
        const row = document.querySelector('[name="Row"]')?.value?.trim();
        const bay = document.querySelector('[name="Bay"]')?.value?.trim();
        const level = document.querySelector('[name="Level"]')?.value?.trim();

        if (row && bay && level) {
            // This would typically require a server-side validation
            // For now, we'll just show a warning if the combination looks suspicious
            if (parseInt(bay) > 50 || parseInt(level) > 10) {
                showWarningToast('Please verify the Bay and Level numbers are correct');
            }
        }

        if (!isValid) {
            showErrorToast('Please fill in all required fields');
        }

        return isValid;
    }

    // Auto-generate location code based on position
    function setupLocationCodeGeneration() {
        const rowInput = document.querySelector('[name="Row"]');
        const bayInput = document.querySelector('[name="Bay"]');
        const levelInput = document.querySelector('[name="Level"]');
        const codeInput = document.querySelector('[name="Code"]');

        if (!rowInput || !bayInput || !levelInput || !codeInput) return;

        function generateLocationCode() {
            const row = rowInput.value?.trim().toUpperCase();
            const bay = bayInput.value?.trim().padStart(2, '0');
            const level = levelInput.value?.trim().padStart(2, '0');

            if (row && bay && level) {
                const generatedCode = `${row}${bay}${level}`;
                if (!codeInput.value || codeInput.value === codeInput.getAttribute('data-generated')) {
                    codeInput.value = generatedCode;
                    codeInput.setAttribute('data-generated', generatedCode);
                }
            }
        }

        // Generate code when position fields change
        [rowInput, bayInput, levelInput].forEach(input => {
            input.addEventListener('input', generateLocationCode);
        });
    }

    // Auto-generate location name based on position
    function setupLocationNameGeneration() {
        const rowInput = document.querySelector('[name="Row"]');
        const bayInput = document.querySelector('[name="Bay"]');
        const levelInput = document.querySelector('[name="Level"]');
        const nameInput = document.querySelector('[name="Name"]');

        if (!rowInput || !bayInput || !levelInput || !nameInput) return;

        function generateLocationName() {
            const row = rowInput.value?.trim().toUpperCase();
            const bay = bayInput.value?.trim().padStart(2, '0');
            const level = levelInput.value?.trim().padStart(2, '0');

            if (row && bay && level) {
                const generatedName = `${row}-${bay}-${level}`;
                if (!nameInput.value || nameInput.value === nameInput.getAttribute('data-generated')) {
                    nameInput.value = generatedName;
                    nameInput.setAttribute('data-generated', generatedName);
                }
            }
        }

        // Generate name when position fields change
        [rowInput, bayInput, levelInput].forEach(input => {
            input.addEventListener('input', generateLocationName);
        });
    }

    // Initialize auto-generation features
    function initializeAutoGeneration() {
        if (hasEditAccess && !isEdit) {
            setupLocationCodeGeneration();
            setupLocationNameGeneration();
        }
    }

    // Expose public methods
    window.LocationDetailModule = {
        init: init,
        loadZonesByWarehouse: loadZonesByWarehouse
    };
})();

// Initialize the module
LocationDetailModule.init();