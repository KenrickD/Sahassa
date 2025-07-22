
(function () {
    // Permission management variables
    let allPermissions = [];
    let originalPermissions = new Set();
    let currentPermissions = new Set();
    let isPermissionLoading = false;

    // Initialize the module
    function init() {
        document.addEventListener('DOMContentLoaded', function () {
            initializeTabBehavior();
            setupRoleForm();
            initializePermissionManagement();
        });
    }

    function setupRoleForm() {
        $("#roleForm").on("submit", function (e) {
            e.preventDefault(); // Prevent the default form submission

            // Create a FormData object
            var formData = new FormData(this);
            var url = $(this).attr('action');
            var isEdit = false;

            const roleId = $('#currentRoleId').val();
            if (roleId != '00000000-0000-0000-0000-000000000000') {
                url = "/Role/Edit"
                isEdit = true;
            }

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
                success: function (data) {
                    if (data.success !== false) {
                        showSuccessToast("Role saved successfully!");

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
                            showErrorToast(xhr.responseJSON.message || "An error occurred while updating the role.");
                        }
                    } else {
                        showErrorToast("An error occurred while updating the role.");
                    }
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

                    // Load permissions when permission tab is activated
                    if (targetId === '#role-permissions') {
                        loadRolePermissions();
                    }
                });

                // Activate first tab by default
                $(`#${tabsId} [data-tabs-target]`).first().click();
            });
        }
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
        $('#saveRolePermissionsBtn').on('click', function () {
            if ($(this).prop('disabled')) return;
            savePermissionChanges();
        });

        // Reset changes
        $('#resetRolePermissionsBtn').on('click', function () {
            if ($(this).prop('disabled')) return;
            resetPermissionChanges();
        });

        // Individual permission checkbox change
        $(document).on('change', '.role-permission-checkbox', function () {
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
        return $('#rolePermissionsTableBody tr:not([data-filtered="hidden"]) .role-permission-checkbox');
    }

    // Load role permissions
    function loadRolePermissions() {
        if (isPermissionLoading || allPermissions.length > 0) return;
        isPermissionLoading = true;

        const roleId = $('#currentRoleId').val();
        if (!roleId) {
            showErrorToast('Role ID not found');
            return;
        }

        $.ajax({
            url: '/Role/GetRolePermissions',
            type: 'GET',
            data: { roleId: roleId },
            success: function (data) {
                if (data.success === false) {
                    throw new Error(data.message);
                }

                allPermissions = data.allPermissions || [];
                originalPermissions = new Set(data.rolePermissions || []);
                currentPermissions = new Set(data.rolePermissions || []);

                populatePermissionModuleFilter();
                renderPermissionsTable();
                updatePermissionUI();
            },
            error: function (xhr) {
                const errorMsg = xhr.responseJSON?.message || 'Failed to load permissions';
                showErrorToast(errorMsg);
                $('#rolePermissionsTableBody').html(`
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
        const tbody = $('#rolePermissionsTableBody');

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
                <tr class="role-permission-row hover:bg-gray-50 dark:hover:bg-neutral-600" data-module="${permission.module}">
                    <td class="px-6 py-4 whitespace-nowrap">
                        <input type="checkbox" 
                               class="role-permission-checkbox form-checkbox h-4 w-4 text-blue-600 cursor-pointer" 
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
                        <span class="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium bg-blue-100 text-blue-800 dark:bg-blue-900/25 dark:text-blue-400">
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
        $('.role-permission-row').each(function () {
            const permissionId = $(this).find('.role-permission-checkbox').data('permission-id');
            const isChecked = currentPermissions.has(permissionId);
            const wasOriginallyChecked = originalPermissions.has(permissionId);
            const hasChanged = isChecked !== wasOriginallyChecked;

            // Update the status badge in the last column
            const statusCell = $(this).find('td:last-child');
            statusCell.html(getPermissionStatusBadge(isChecked, hasChanged));

            // Update checkbox state
            $(this).find('.role-permission-checkbox').prop('checked', isChecked);
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
        const rows = $('.role-permission-row');

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
        const visibleRows = $('#rolePermissionsTableBody tr:not([data-filtered="hidden"]).role-permission-row');
        const totalVisible = visibleRows.length;
        const selectedVisible = visibleRows.find('.role-permission-checkbox:checked').length;
        const totalChanges = calculatePermissionChanges();

        // Update counters
        $('#totalRolePermissions').text(totalVisible);
        $('#selectedRolePermissions').text(selectedVisible);
        $('#changesRolePermissions').text(totalChanges);

        // Update header checkbox
        updatePermissionHeaderCheckbox();

        // Enable/disable save and reset buttons
        const hasChanges = totalChanges > 0;
        $('#saveRolePermissionsBtn').prop('disabled', !hasChanges);
        $('#resetRolePermissionsBtn').prop('disabled', !hasChanges);

        // Update status badges for visible rows
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

        const roleId = $('#currentRoleId').val();

        // Disable save button during request
        $('#saveRolePermissionsBtn').prop('disabled', true).html('<iconify-icon icon="solar:refresh-outline" class="animate-spin mr-2"></iconify-icon>Saving...');

        // Get anti-forgery token
        const token = $('input[name="__RequestVerificationToken"]').val();

        $.ajax({
            url: '/Role/SaveRolePermissions',
            type: 'POST',
            headers: {
                'X-CSRF-TOKEN': $('meta[name="csrf-token"]').attr('content'),
                'RequestVerificationToken': token
            },
            contentType: 'application/json',
            data: JSON.stringify({
                RoleId: roleId,
                Changes: changes
            }),
            success: function (response) {
                if (response.success) {
                    //showSuccessToast(response.message);
                    originalPermissions = new Set(currentPermissions);
                    updatePermissionUI();

                    setTimeout(() => {
                        window.location.reload();
                    }, 500);
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
                    showErrorToast('Failed to save role permissions');
                }
            },
            complete: function () {
                $('#saveRolePermissionsBtn').prop('disabled', false).html('<iconify-icon icon="solar:diskette-outline" class="mr-2"></iconify-icon>Save Changes');
            }
        });
    }

    // Reset permission changes
    function resetPermissionChanges() {
        currentPermissions = new Set(originalPermissions);

        // Update checkboxes
        $('.role-permission-checkbox').each(function () {
            const permissionId = $(this).data('permission-id');
            $(this).prop('checked', originalPermissions.has(permissionId));
        });

        updatePermissionUI();
        showInfoToast('Changes reset');
    }

    // Expose public methods
    window.RoleModule = {
        init: init,
        loadRolePermissions: loadRolePermissions
    };
})();

RoleModule.init();