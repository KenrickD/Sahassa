(function () {
    // Variables for role management
    let userRolesData = [];
    let originalUserRoleIds = [];
    let currentUserRoleIds = [];
    let userRoleChanges = [];

    // Permission management variables
    let allPermissions = [];
    let originalPermissions = new Set();
    let currentPermissions = new Set();
    let isPermissionLoading = false;

    // Initialize the module
    function init() {
        document.addEventListener('DOMContentLoaded', function () {
            setupImagePreview();
            setupPasswordVisibility();
            setupClientFilter();
            initializeTabBehavior();
            submitFormEditProfile();
            initializeUserRoleManagement();
            initializePermissionManagement();
        });
    }

    function submitFormEditProfile() {
        $("#userProfileForm").on("submit", function (e) {
            e.preventDefault(); // Prevent the default form submission

            // Create a FormData object to handle file uploads
            var formData = new FormData(this);

            // Submit the form via AJAX
            $.ajax({
                url: $(this).attr('action'),
                type: $(this).attr('method'),
                data: formData,
                processData: false, // Required for FormData
                contentType: false, // Required for FormData
                headers: {
                    'X-Requested-With': 'XMLHttpRequest' // Mark as AJAX request
                },
                success: function (response) {
                    refreshNavbarViewComponent();
                    window.location.reload();
                },
                error: function (xhr) {
                    // Handle errors
                    if (xhr.responseJSON) {
                        // If validation errors, display them
                        var errors = xhr.responseJSON.errors;
                        if (errors) {
                            $.each(errors, function (key, value) {
                                showErrorToast(value[0]);
                            });
                        } else {
                            showErrorToast("An error occurred while updating the profile.");
                        }
                    } else {
                        showErrorToast("An error occurred while updating the profile.");
                    }
                }
            });
        });
    }
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

        $("#imageUpload").on("change", function () {
            readURL(this);
        });
    }

    function setupPasswordVisibility() {
        // Toggle password visibility
        $('.toggle-password').on('click', function () {
            const toggleElement = $(this);
            const passwordField = $(toggleElement.data('toggle'));

            if (passwordField.attr('type') === 'password') {
                passwordField.attr('type', 'text');
                toggleElement.removeClass('ri-eye-line').addClass('ri-eye-off-line');
            } else {
                passwordField.attr('type', 'password');
                toggleElement.removeClass('ri-eye-off-line').addClass('ri-eye-line');
            }
        });
    }

    function setupClientFilter() {
        // Client dropdown filtering based on warehouse selection
        $('#warehouseId').on('change', function () {
            const warehouseId = $(this).val();
            if (warehouseId) {
                // Disable client dropdown while loading
                $('#clientId').prop('disabled', true);

                $.ajax({
                    url: '/User/GetClientsByWarehouse',
                    type: 'GET',
                    headers: {
                        'X-CSRF-TOKEN': $('meta[name="csrf-token"]').attr('content')
                    },
                    data: { warehouseId: warehouseId },
                    success: function (data) {
                        // Clear current options
                        $('#clientId').empty();

                        // Add default option
                        $('#clientId').append('<option value="">None</option>');

                        // Add clients from the selected warehouse
                        $.each(data, function (index, client) {
                            $('#clientId').append('<option value="' + client.value + '">' + client.text + '</option>');
                        });

                        // Re-enable the dropdown
                        $('#clientId').prop('disabled', false);
                    },
                    error: function () {
                        $('#clientId').prop('disabled', false);
                    }
                });
            } else {
                // Clear client dropdown if no warehouse is selected
                $('#clientId').empty().append('<option value="">None</option>').prop('disabled', true);
            }
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

                    // Load permissions when permission tab is activated
                    if (targetId === '#user-permission') {
                        loadUserPermissions();
                    }
                    if (targetId === '#user-role') {
                        loadUserRoles();
                    }
                });

                // Activate first tab by default
                $(`#${tabsId} [data-tabs-target]`).first().click();
            });
        }
    }


    // Initialize role management when tab is shown
    function initializeUserRoleManagement() {
        // Load user roles when tab becomes active
        //$(document).on('shown.bs.tab', 'a[href="#user-role"]', function (e) {
        //    loadUserRoles();
        //});

        // Event handlers
        $('#saveUserRolesBtn').on('click', saveUserRoleChanges);
        $('#resetUserRolesBtn').on('click', resetUserRoleChanges);
        $('#selectAllRolesBtn').on('click', selectAllRoles);
        $('#deselectAllRolesBtn').on('click', deselectAllRoles);
        $('#selectAllRolesCheckbox').on('change', toggleAllRoles);
        $('#roleSearchBox').on('input', filterRolesBySearch);

        // Handle individual checkbox changes
        $(document).on('change', '.role-checkbox', handleRoleCheckboxChange);
    }

    // Load user roles data
    function loadUserRoles() {
        const userId = $('#currentUserId').val();
        if (!userId) {
            showErrorToast('User ID not found');
            return;
        }

        $.ajax({
            url: `/User/GetUserRoles`,
            type: 'GET',
            data: { userId: userId },
            headers: {
                'X-Requested-With': 'XMLHttpRequest', // Mark as AJAX request
                'X-CSRF-TOKEN': $('meta[name="csrf-token"]').attr('content')
            },
            success: function (response) {
                if (response.success) {
                    userRolesData = response.data.allRoles;
                    originalUserRoleIds = [...response.data.userRoleIds];
                    currentUserRoleIds = [...response.data.userRoleIds];
                    userRoleChanges = [];
                    renderUserRolesTable();
                    updateUserRolesSummary();
                } else {
                    showErrorToast('Failed to load user roles: ' + response.message);
                }
            },
            error: function (xhr, status, error) {
                console.error('Error loading user roles:', error);
                showErrorToast('Failed to load user roles');
            }
        });
    }
    function filterRolesBySearch() {
        renderUserRolesTable();
        updateUserRolesSummary();
    }
    // Render roles table
    function renderUserRolesTable() {
        const tbody = $('#userRolesTableBody');

        let filteredRoles = userRolesData;

        var roleSearchBoxValue = $('#roleSearchBox').val();

        if (roleSearchBoxValue) {
            const searchTerm = roleSearchBoxValue.toLowerCase().trim();

            // Apply search filter
            if (searchTerm) {
                filteredRoles = filteredRoles.filter(role =>
                    role.name.toLowerCase().includes(searchTerm) ||
                    (role.description && role.description.toLowerCase().includes(searchTerm))
                );
            }
        }

        if (filteredRoles.length === 0) {
            tbody.html(`
            <tr>
                <td colspan="6" class="px-6 py-8 text-center text-gray-500">
                    <iconify-icon icon="solar:folder-outline" class="text-2xl mb-2"></iconify-icon>
                    <div>No roles found</div>
                </td>
            </tr>
        `);
            return;
        }

        const rows = filteredRoles.map(role => {
            const isChecked = currentUserRoleIds.includes(role.id);
            const hasChanged = originalUserRoleIds.includes(role.id) !== isChecked;

            return `
            <tr class="hover:bg-gray-50 ${hasChanged ? 'bg-blue-50' : ''}">
                <td class="px-6 py-4 whitespace-nowrap">
                    <input type="checkbox" 
                           class="role-checkbox form-checkbox h-4 w-4 text-blue-600 cursor-pointer" 
                           data-role-id="${role.id}"
                           ${isChecked ? 'checked' : ''}>
                </td>
                <td class="px-6 py-4 whitespace-nowrap">
                    <div class="text-sm font-medium text-gray-600 dark:text-gray-400">${escapeHtml(role.name)}</div>
                </td>
                <td class="px-6 py-4">
                    <div class="text-sm text-gray-600 dark:text-gray-400">${escapeHtml(role.description || 'No description')}</div>
                </td>
            </tr>
        `;
        }).join('');

        tbody.html(rows);
        updateSelectAllCheckbox();
    }

    // Handle individual role checkbox change
    function handleRoleCheckboxChange(e) {
        const checkbox = $(e.target);
        const roleId = checkbox.data('role-id');
        const isChecked = checkbox.is(':checked');

        if (isChecked) {
            if (!currentUserRoleIds.includes(roleId)) {
                currentUserRoleIds.push(roleId);
            }
        } else {
            currentUserRoleIds = currentUserRoleIds.filter(id => id !== roleId);
        }

        calculateUserRoleChanges();
        updateUserRolesSummary();
        renderUserRolesTable(); // Re-render to show changes
    }

    // Calculate what changes need to be made
    function calculateUserRoleChanges() {
        userRoleChanges = [];

        // Find roles to add
        currentUserRoleIds.forEach(roleId => {
            if (!originalUserRoleIds.includes(roleId)) {
                userRoleChanges.push({
                    action: 'add',
                    roleId: roleId
                });
            }
        });

        // Find roles to remove
        originalUserRoleIds.forEach(roleId => {
            if (!currentUserRoleIds.includes(roleId)) {
                userRoleChanges.push({
                    action: 'remove',
                    roleId: roleId
                });
            }
        });
    }

    // Update summary counts
    function updateUserRolesSummary() {
        let filteredRoles = userRolesData;

        const totalDisplayed = filteredRoles.length;
        const selectedDisplayed = filteredRoles.filter(role => currentUserRoleIds.includes(role.id)).length;

        $('#totalUserRoles').text(totalDisplayed);
        $('#selectedUserRoles').text(selectedDisplayed);
        $('#changesUserRoles').text(userRoleChanges.length);

        // Enable/disable save button
        $('#saveUserRolesBtn').prop('disabled', userRoleChanges.length === 0);
        $('#resetUserRolesBtn').prop('disabled', userRoleChanges.length === 0);
    }

    // Update select all checkbox state
    function updateSelectAllCheckbox() {
        const checkboxes = $('.role-checkbox:visible');
        const checkedBoxes = $('.role-checkbox:visible:checked');
        const selectAllCheckbox = $('#selectAllRolesCheckbox');

        if (checkedBoxes.length === 0) {
            selectAllCheckbox.prop('checked', false).prop('indeterminate', false);
        } else if (checkedBoxes.length === checkboxes.length) {
            selectAllCheckbox.prop('checked', true).prop('indeterminate', false);
        } else {
            selectAllCheckbox.prop('checked', false).prop('indeterminate', true);
        }
    }

    // Toggle all roles
    function toggleAllRoles() {
        const isChecked = $('#selectAllRolesCheckbox').is(':checked');
        const visibleCheckboxes = $('.role-checkbox:visible');

        visibleCheckboxes.each(function () {
            const roleId = $(this).data('role-id');
            $(this).prop('checked', isChecked);

            if (isChecked) {
                if (!currentUserRoleIds.includes(roleId)) {
                    currentUserRoleIds.push(roleId);
                }
            } else {
                currentUserRoleIds = currentUserRoleIds.filter(id => id !== roleId);
            }
        });

        calculateUserRoleChanges();
        updateUserRolesSummary();
        renderUserRolesTable();
    }

    // Select all roles
    function selectAllRoles() {
        $('#selectAllRolesCheckbox').prop('checked', true);
        toggleAllRoles();
    }

    // Deselect all roles
    function deselectAllRoles() {
        $('#selectAllRolesCheckbox').prop('checked', false);
        toggleAllRoles();
    }

    // Filter roles by type
    function filterRolesByType() {
        renderUserRolesTable();
        updateUserRolesSummary();
    }

    // Save role changes
    function saveUserRoleChanges() {
        if (userRoleChanges.length === 0) {
            showInfoToast('No changes to save');
            return;
        }

        const userId = $('#currentUserId').val();
        const saveBtn = $('#saveUserRolesBtn');
        const originalText = saveBtn.find('span').text();

        // Show loading state
        saveBtn.prop('disabled', true);
        saveBtn.find('span').text('Saving...');
        saveBtn.find('iconify-icon').attr('icon', 'solar:refresh-outline');
        saveBtn.find('iconify-icon').addClass('animate-spin');

        $.ajax({
            url: '/User/SaveUserRoleChanges',
            type: 'POST',
            contentType: 'application/json',
            data: JSON.stringify({
                userId: userId,
                changes: userRoleChanges
            }),
            headers: {
                'X-CSRF-TOKEN': $('meta[name="csrf-token"]').attr('content')
            },
            success: function (response) {
                if (response.success) {
                    showSuccessToast(response.message);
                    // Reload to get fresh data
                    loadUserRoles();
                } else {
                    showErrorToast('Failed to save changes: ' + response.message);
                }
            },
            error: function (xhr, status, error) {
                console.error('Error saving role changes:', error);
                showErrorToast('Failed to save role changes');
            },
            complete: function () {
                // Reset button state
                saveBtn.prop('disabled', false);
                saveBtn.find('span').text(originalText);
                saveBtn.find('iconify-icon').attr('icon', 'solar:diskette-outline');
                saveBtn.find('iconify-icon').removeClass('animate-spin');
            }
        });
    }

    // Reset role changes
    function resetUserRoleChanges() {
        if (userRoleChanges.length === 0) {
            showInfoToast('No changes to reset');
            return;
        }

        currentUserRoleIds = [...originalUserRoleIds];
        userRoleChanges = [];
        renderUserRolesTable();
        updateUserRolesSummary();
        showInfoToast('Changes reset successfully');
    }

   
    // Permission Management Functions
    function initializePermissionManagement() {
        // Module filter change
        $('#permissionModuleFilter').on('change', function () {
            filterPermissionsByModule();
            updatePermissionHeaderCheckbox();
        });

        // Select All button  
        $('#selectAllPermissionsBtn').on('click', function () {
            const visibleCheckboxes = getVisiblePermissionCheckboxes();
            visibleCheckboxes.prop('checked', true);

            // Update currentPermissions set
            visibleCheckboxes.each(function () {
                const permissionId = $(this).data('permission-id');
                if (permissionId) {
                    currentPermissions.add(permissionId);
                }
            });

            updatePermissionUI();
        });

        // Deselect All button
        $('#deselectAllPermissionsBtn').on('click', function () {
            const visibleCheckboxes = getVisiblePermissionCheckboxes();
            visibleCheckboxes.prop('checked', false);

            // Update currentPermissions set
            visibleCheckboxes.each(function () {
                const permissionId = $(this).data('permission-id');
                if (permissionId) {
                    currentPermissions.delete(permissionId);
                }
            });

            updatePermissionUI();
        });

        // Header checkbox - select/deselect all visible
        $('#selectAllPermissionsCheckbox').on('change', function () {
            const isChecked = $(this).is(':checked');
            const visibleCheckboxes = getVisiblePermissionCheckboxes();

            visibleCheckboxes.prop('checked', isChecked);

            // Update currentPermissions set
            visibleCheckboxes.each(function () {
                const permissionId = $(this).data('permission-id');
                if (permissionId) {
                    if (isChecked) {
                        currentPermissions.add(permissionId);
                    } else {
                        currentPermissions.delete(permissionId);
                    }
                }
            });

            updatePermissionUI();
        });

        // Save changes 
        $('#saveUserPermissionsBtn').on('click', function () {
            if ($(this).prop('disabled')) return;
            savePermissionChanges();
        });

        // Reset changes
        $('#resetUserPermissionsBtn').on('click', function () {
            if ($(this).prop('disabled')) return;
            resetPermissionChanges();
        });

        // Individual permission checkbox change
        $(document).on('change', '.user-permission-checkbox', function () {
            const permissionId = $(this).data('permission-id');
            const isChecked = $(this).is(':checked');

            if (isChecked) {
                currentPermissions.add(permissionId);
            } else {
                currentPermissions.delete(permissionId);
            }

            updatePermissionUI();
        });
    }

    // Helper function to get visible permission checkboxes
    function getVisiblePermissionCheckboxes() {
        return $('#userPermissionsTableBody tr:not([data-filtered="hidden"]) .user-permission-checkbox');
    }

    // Load user permissions
    function loadUserPermissions() {
        if (isPermissionLoading || allPermissions.length > 0) return;
        isPermissionLoading = true;

        const userId = $('#currentUserId').val();
        if (!userId) {
            showErrorToast('User ID not found');
            return;
        }

        $.ajax({
            url: '/User/GetUserPermissions',
            type: 'GET',
            data: { userId: userId },
            success: function (data) {

                if (data.success === false) {
                    throw new Error(data.message);
                }

                allPermissions = data.allPermissions || [];
                originalPermissions = new Set(data.userPermissions || []);
                currentPermissions = new Set(data.userPermissions || []);

                populatePermissionModuleFilter();
                renderPermissionsTable();
                updatePermissionUI();
            },
            error: function (xhr) {
                const errorMsg = xhr.responseJSON?.message || 'Failed to load permissions';
                showErrorToast(errorMsg);
                $('#userPermissionsTableBody').html(`
                    <tr>
                        <td colspan="5" class="px-6 py-8 text-center text-red-500">
                            <iconify-icon icon="solar:danger-outline" class="text-2xl mb-2"></iconify-icon>
                            <div>Failed to load permissions</div>
                        </td>
                    </tr>
                `);
            },
            complete: function () {
                isPermissionLoading = false;
            }
        });
    }

    // Populate module filter dropdown
    function populatePermissionModuleFilter() {
        const modules = [...new Set(allPermissions.map(p => p.module))].sort();
        const select = $('#permissionModuleFilter');

        select.html('<option value="">All Modules</option>');
        modules.forEach(module => {
            if (module && module.trim() !== '') {
                select.append(`<option value="${escapeHtml(module)}">${escapeHtml(module)}</option>`);
            }
        });
    }

    // Render permissions table
    function renderPermissionsTable() {
        const tbody = $('#userPermissionsTableBody');

        if (allPermissions.length === 0) {
            tbody.html(`
                <tr>
                    <td colspan="5" class="px-6 py-8 text-center text-gray-500">
                        <iconify-icon icon="solar:shield-minus-outline" class="text-2xl mb-2"></iconify-icon>
                        <div>No permissions found</div>
                    </td>
                </tr>
            `);
            return;
        }

        let html = '';
        allPermissions.forEach(permission => {
            const isChecked = currentPermissions.has(permission.id);
            const wasOriginallyChecked = originalPermissions.has(permission.id);
            const hasChanged = isChecked !== wasOriginallyChecked;

            html += `
                <tr class="user-permission-row hover:bg-gray-50" data-module="${permission.module}">
                    <td class="px-6 py-4 whitespace-nowrap">
                        <input type="checkbox" 
                               class="user-permission-checkbox form-checkbox h-4 w-4 text-blue-600 cursor-pointer" 
                               data-permission-id="${permission.id}"
                               ${isChecked ? 'checked' : ''}>
                    </td>
                    <td class="px-6 py-4 whitespace-nowrap">
                        <div class="text-sm font-medium text-gray-600 dark:text-gray-400">${escapeHtml(permission.name)}</div>
                    </td>
                    <td class="px-6 py-4">
                        <div class="text-sm text-gray-600 dark:text-gray-400">${escapeHtml(permission.description || 'No description')}</div>
                    </td>
                    <td class="px-6 py-4 whitespace-nowrap">
                        <span class="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium bg-blue-100 text-blue-800">
                            ${escapeHtml(permission.module)}
                        </span>
                    </td>
                    <td class="px-6 py-4 whitespace-nowrap">
                        ${getPermissionStatusBadge(isChecked, hasChanged)}
                    </td>
                </tr>
            `;
        });

        tbody.html(html);
    }

    // Update status badges without re-rendering entire table
    function updateStatusBadges() {
        $('.user-permission-row').each(function () {
            const permissionId = $(this).find('.user-permission-checkbox').data('permission-id');
            const isChecked = currentPermissions.has(permissionId);
            const wasOriginallyChecked = originalPermissions.has(permissionId);
            const hasChanged = isChecked !== wasOriginallyChecked;

            // Update the status badge in the last column
            const statusCell = $(this).find('td:last-child');
            statusCell.html(getPermissionStatusBadge(isChecked, hasChanged));

            // Update checkbox state
            $(this).find('.user-permission-checkbox').prop('checked', isChecked);
        });
    }
    function getPermissionStatusBadge(isChecked, hasChanged) {
        if (hasChanged) {
            return isChecked
                ? '<span class="inline-flex items-center px-2 py-1 rounded-full text-xs font-medium bg-green-100 text-green-800 dark:bg-green-900/25 dark:text-green-400"><iconify-icon icon="solar:add-circle-outline" class="mr-1"></iconify-icon>Added</span>'
                : '<span class="inline-flex items-center px-2 py-1 rounded-full text-xs font-medium bg-red-100 text-red-800 dark:bg-red-900/25 dark:text-red-400"><iconify-icon icon="solar:minus-circle-outline" class="mr-1"></iconify-icon>Removed</span>';
        }

        return isChecked
            ? '<span class="inline-flex items-center px-2 py-1 rounded-full text-xs font-medium bg-gray-100 text-gray-800 dark:bg-gray-900/25 dark:text-gray-400"><iconify-icon icon="solar:check-circle-outline" class="mr-1"></iconify-icon>Active</span>'
            : '<span class="inline-flex items-center px-2 py-1 rounded-full text-xs font-medium bg-gray-100 text-gray-500 dark:bg-gray-800 dark:text-gray-400">Inactive</span>';
    }

    // Filter permissions by module
    function filterPermissionsByModule() {
        const selectedModule = $('#permissionModuleFilter').val();
        const rows = $('.user-permission-row');

        if (selectedModule === '' || selectedModule === null) {
            // Show all rows using direct style manipulation
            rows.each(function () {
                $(this).attr('style', '').removeAttr('data-filtered');
            });
        } else {
            rows.each(function () {
                const rowModule = $(this).data('module');

                if (rowModule === selectedModule) {
                    // Show row
                    $(this).attr('style', '').removeAttr('data-filtered');
                } else {
                    // Hide row using important style
                    $(this).attr('style', 'display: none !important;').attr('data-filtered', 'hidden');
                }
            });
        }

        updatePermissionUI();
    }

    // Update permission header checkbox state
    function updatePermissionHeaderCheckbox() {
        const visibleCheckboxes = getVisiblePermissionCheckboxes();
        const checkedVisible = visibleCheckboxes.filter(':checked').length;
        const totalVisible = visibleCheckboxes.length;

        const headerCheckbox = $('#selectAllPermissionsCheckbox');

        if (totalVisible === 0) {
            headerCheckbox.prop('indeterminate', false).prop('checked', false);
        } else if (checkedVisible === 0) {
            headerCheckbox.prop('indeterminate', false).prop('checked', false);
        } else if (checkedVisible === totalVisible) {
            headerCheckbox.prop('indeterminate', false).prop('checked', true);
        } else {
            headerCheckbox.prop('indeterminate', true).prop('checked', false);
        }
    }

    // Update permission UI elements
    function updatePermissionUI() {
        const visibleRows = $('#userPermissionsTableBody tr:not([data-filtered="hidden"]).user-permission-row');
        const totalVisible = visibleRows.length;
        const selectedVisible = visibleRows.find('.user-permission-checkbox:checked').length;
        const totalChanges = calculatePermissionChanges();

        // Update counters
        $('#totalUserPermissions').text(totalVisible);
        $('#selectedUserPermissions').text(selectedVisible);
        $('#changesUserPermissions').text(totalChanges);

        // Update header checkbox
        updatePermissionHeaderCheckbox();

        // Enable/disable save and reset buttons
        const hasChanges = totalChanges > 0;
        $('#saveUserPermissionsBtn').prop('disabled', !hasChanges);
        $('#resetUserPermissionsBtn').prop('disabled', !hasChanges);

        // DON'T re-render table here as it removes filtering
        // Only update status badges for visible rows
        updateStatusBadges();
    }

    // Calculate number of permission changes
    function calculatePermissionChanges() {
        let changes = 0;
        allPermissions.forEach(permission => {
            const isCurrentlyChecked = currentPermissions.has(permission.id);
            const wasOriginallyChecked = originalPermissions.has(permission.id);
            if (isCurrentlyChecked !== wasOriginallyChecked) {
                changes++;
            }
        });
        return changes;
    }

    // Save permission changes
    function savePermissionChanges() {
        const changes = [];

        allPermissions.forEach(permission => {
            const isCurrentlyChecked = currentPermissions.has(permission.id);
            const wasOriginallyChecked = originalPermissions.has(permission.id);

            if (isCurrentlyChecked !== wasOriginallyChecked) {
                changes.push({
                    PermissionId: permission.id,
                    Action: isCurrentlyChecked ? 'add' : 'remove'
                });
            }
        });

        if (changes.length === 0) {
            showInfoToast('No changes to save');
            return;
        }

        const userId = $('#currentUserId').val();

        // Disable save button during request
        $('#saveUserPermissionsBtn').prop('disabled', true).html('<iconify-icon icon="solar:refresh-outline" class="animate-spin mr-2"></iconify-icon>Saving...');

        // Get anti-forgery token
        const token = $('input[name="__RequestVerificationToken"]').val();

        $.ajax({
            url: '/User/SaveUserPermissions',
            type: 'POST',
            headers: {
                'X-CSRF-TOKEN': $('meta[name="csrf-token"]').attr('content')
            },
            contentType: 'application/json',
            data: JSON.stringify({
                UserId: userId,
                Changes: changes
            }),
            success: function (response) {
                if (response.success) {
                    showSuccessToast(response.message);
                    originalPermissions = new Set(currentPermissions);
                    updatePermissionUI();
                } else {
                    showErrorToast(response.message);
                }
            },
            error: function (xhr) {
                if (xhr.status === 400) {
                    try {
                        const errorResponse = JSON.parse(xhr.responseText);
                        showErrorToast(errorResponse.message || 'Validation error occurred');
                    } catch (e) {
                        showErrorToast('Bad request: Please check your input data');
                    }
                } else {
                    showErrorToast('Failed to save permissions');
                }
            },
            complete: function () {
                $('#saveUserPermissionsBtn').prop('disabled', false).html('<iconify-icon icon="solar:diskette-outline" class="mr-2"></iconify-icon>Save Changes');
            }
        });
    }
    
    // Reset permission changes
    function resetPermissionChanges() {
        currentPermissions = new Set(originalPermissions);

        // Update checkboxes
        $('.user-permission-checkbox').each(function () {
            const permissionId = $(this).data('permission-id');
            $(this).prop('checked', originalPermissions.has(permissionId));
        });

        updatePermissionUI();
        showInfoToast('Changes reset');
    }

    // Expose public methods
    window.UserProfileModule = {
        init: init,
        loadUserPermissions: loadUserPermissions
    };
})();

UserProfileModule.init();