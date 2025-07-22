// User Form Module
(function () {
    // Initialize the module
    function init() {
        document.addEventListener('DOMContentLoaded', function () {
            setupImagePreview();
            setupWarehouseClientFilter();
            setupRoleDropdown();
            setupFormValidation();
        });
    }

    // Image preview functionality
    function setupImagePreview() {
        function readURL(input) {
            if (input.files && input.files[0]) {
                var reader = new FileReader();
                reader.onload = function (e) {
                    $('#imagePreview').css('background-image', 'url(' + e.target.result + ')');
                    $('#imagePreview').hide();
                    $('#imagePreview').fadeIn(650);
                }
                reader.readAsDataURL(input.files[0]);
            }
        }

        $("#imageUpload").change(function () {
            readURL(this);
        });
    }

    // Client dropdown filtering based on warehouse selection
    function setupWarehouseClientFilter() {
        $('#warehouseSelect').on('change', function () {
            const warehouseId = $(this).val();
            if (warehouseId) {
                // Disable client dropdown while loading
                $('#clientSelect').prop('disabled', true);

                $.ajax({
                    url: '/User/GetClientsByWarehouse',
                    type: 'GET',
                    headers: {
                        'X-CSRF-TOKEN': $('meta[name="csrf-token"]').attr('content')
                    },
                    data: { warehouseId: warehouseId },
                    success: function (data) {
                        // Clear current options
                        $('#clientSelect').empty();

                        // Add default option
                        $('#clientSelect').append('<option value="">None</option>');

                        // Add clients from the selected warehouse
                        $.each(data, function (index, client) {
                            $('#clientSelect').append('<option value="' + client.value + '">' + client.text + '</option>');
                        });

                        // Re-enable the dropdown
                        $('#clientSelect').prop('disabled', false);
                    },
                    error: function () {
                        console.error('Failed to load clients for the selected warehouse');
                        $('#clientSelect').prop('disabled', false);
                    }
                });
            } else {
                // Clear client dropdown if no warehouse is selected
                $('#clientSelect').empty().append('<option value="">System User (No Client)</option>').prop('disabled', true);
            }
        });
    }

    // Role dropdown functionality
    function setupRoleDropdown() {
        $('#roleDropdownBtn').on('click', function (e) {
            e.preventDefault();
            $('#roleDropdownMenu').toggleClass('hidden');
        });

        // Close dropdown when clicking outside
        $(document).on('click', function (e) {
            if (!$(e.target).closest('#roleDropdownBtn, #roleDropdownMenu').length) {
                $('#roleDropdownMenu').addClass('hidden');
            }
        });

        // Select all roles
        $('#selectAllRoles').on('change', function () {
            const isChecked = $(this).prop('checked');
            $('.role-checkbox').prop('checked', isChecked);
            updateSelectedRolesText();
        });

        // Role checkbox change handler
        $('.role-checkbox').on('change', function () {
            updateSelectedRolesText();

            // Update "Select All" checkbox state
            const totalRoles = $('.role-checkbox').length;
            const selectedRoles = $('.role-checkbox:checked').length;

            $('#selectAllRoles').prop({
                'checked': selectedRoles === totalRoles,
                'indeterminate': selectedRoles > 0 && selectedRoles < totalRoles
            });
        });

        // Initialize the selected roles text
        updateSelectedRolesText();
    }

    // Update the text showing selected roles count
    function updateSelectedRolesText() {
        const selectedRoles = $('.role-checkbox:checked').length;
        const totalRoles = $('.role-checkbox').length;

        if (selectedRoles === 0) {
            $('#selectedRolesText').text('Select roles...');
        } else if (selectedRoles === totalRoles) {
            $('#selectedRolesText').text('All roles selected');
        } else {
            $('#selectedRolesText').text(selectedRoles + ' role(s) selected');
        }
    }

    // Form validation setup
    function setupFormValidation() {
        // Form submission validation
        $('#userForm').on('submit', function (e) {
            // Check if at least one role is selected
            if ($('.role-checkbox:checked').length === 0) {
                e.preventDefault();

                // Display error message
                const errorMsg = 'At least one role is required';
                if ($('[data-valmsg-for="SelectedRoleIds"]').length > 0) {
                    $('[data-valmsg-for="SelectedRoleIds"]').text(errorMsg).addClass('text-danger-600 text-sm mt-1');
                } else {
                    $('<span class="text-danger-600 text-sm mt-1">' + errorMsg + '</span>').insertAfter('#roleDropdownBtn');
                }

                // Highlight the roles dropdown
                $('#roleDropdownBtn').addClass('border-danger-600').removeClass('border-neutral-300');
            }
        });

        // jQuery validation configuration
        setupJQueryValidation();
    }

    // jQuery validation setup
    function setupJQueryValidation() {
        if ($.validator) {
            $.validator.setDefaults({
                highlight: function (element) {
                    // For checkboxes and radio buttons
                    if (element.type === "checkbox" || element.type === "radio") {
                        $(element).closest('.form-check').addClass('has-error');
                    } else if (element.id === "roleDropdownBtn" || $(element).attr("name") === "SelectedRoleIds") {
                        // For the role dropdown
                        $('#roleDropdownBtn').addClass('border-danger-600').removeClass('border-neutral-300');
                    } else {
                        // For other input elements
                        $(element).addClass('border-danger-600').removeClass('border-neutral-300');
                    }
                },
                unhighlight: function (element) {
                    // For checkboxes and radio buttons
                    if (element.type === "checkbox" || element.type === "radio") {
                        $(element).closest('.form-check').removeClass('has-error');
                    } else if (element.id === "roleDropdownBtn" || $(element).attr("name") === "SelectedRoleIds") {
                        // For the role dropdown
                        $('#roleDropdownBtn').removeClass('border-danger-600').addClass('border-neutral-300');
                    } else {
                        // For other input elements
                        $(element).removeClass('border-danger-600').addClass('border-neutral-300');
                    }
                },
                errorPlacement: function (error, element) {
                    if (element.attr("name") === "SelectedRoleIds") {
                        error.appendTo($('[data-valmsg-for="SelectedRoleIds"]'));
                    } else {
                        error.addClass('text-danger-600 text-sm mt-1').insertAfter(element);
                    }
                }
            });

            // Parse the form for validation
            $.validator.unobtrusive.parse('#userForm');
        }
    }

    // Expose public methods
    window.UserFormModule = {
        init: init,
        updateSelectedRolesText: updateSelectedRolesText
    };
})();

// Initialize the module
UserFormModule.init();