// Role Table Module
(function () {
    // Private variables
    let RolesTable;

    // Initialize module
    function init() {
        document.addEventListener('DOMContentLoaded', function () {
            if (document.getElementById('roles-table')) {
                initializeRoleDataTable();
            }
        });
    }

    // Table initialization function
    function initializeRoleDataTable() {
        RolesTable = $('#roles-table').DataTable({
            processing: true,
            serverSide: true,
            responsive: true,
            ajax: {
                url: '/Role/GetRoles',
                type: 'POST',
                headers: {
                    'X-CSRF-TOKEN': $('meta[name="csrf-token"]').attr('content')
                }
            },
            columns: [
                {
                    data: 'id',
                    width: '5%',
                    render: function (data, type, row, meta) {
                        return `<div class="form-check style-check flex items-center">
                                            <input class="form-check-input Role-select" type="checkbox" value="${data}">
                                            <label class="ms-2 form-check-label">
                                                ${meta.row + 1}
                                            </label>
                                        </div>`;
                    },
                    orderable: false
                },
                { data: 'name' },
                { data: 'description' },
                {
                    data: 'id',
                    orderable: false,
                    render: function (data, type, row) {
                        let actions = `<div class="flex">
                            <a href="Role/View/${data}" class="w-8 h-8 bg-primary-50 dark:bg-primary-600/10 text-primary-600 dark:text-primary-400 rounded-full inline-flex items-center justify-center mr-1" title="View Role">
                                <iconify-icon icon="iconamoon:eye-light"></iconify-icon>
                            </a>`;

                        // Show edit button only if user has Write permission
                        if (row.hasWriteAccess) {
                            actions += `<a href="Role/Edit/${data}" class="w-8 h-8 bg-success-100 dark:bg-success-600/25 text-success-600 dark:text-success-400 rounded-full inline-flex items-center justify-center mr-1" title="Edit Role">
                                <iconify-icon icon="lucide:edit"></iconify-icon>
                            </a>`;
                        }

                        // Show delete button only if user has Delete permission
                        if (row.hasDeleteAccess) {
                            actions += `<a href="javascript:void(0)" class="delete-Role w-8 h-8 bg-danger-100 dark:bg-danger-600/25 text-danger-600 dark:text-danger-400 rounded-full inline-flex items-center justify-center" data-id="${data}" title="Delete Role">
                                <iconify-icon icon="mingcute:delete-2-line"></iconify-icon>
                            </a>`;
                        }

                        actions += `</div>`;
                        return actions;
                    }
                }
            ],
            columnDefs: [
                { targets: [0, 3], sortable: false }
            ],
            order: [[1, 'asc']],
            dom: '<"flex justify-between items-center mb-4"<"flex"f><"flex-1 text-right"l>>rt<"flex justify-between items-center mt-4"<"flex-1"i><"flex"p>>',
            language: {
                search: "",
                searchPlaceholder: "Search Roles...",
                lengthMenu: "_MENU_ per page",
                info: "Showing _START_ to _END_ of _TOTAL_ Roles",
                paginate: {
                    first: '<iconify-icon icon="heroicons-outline:chevron-double-left"></iconify-icon>',
                    last: '<iconify-icon icon="heroicons-outline:chevron-double-right"></iconify-icon>',
                    next: '<iconify-icon icon="heroicons-outline:chevron-right"></iconify-icon>',
                    previous: '<iconify-icon icon="heroicons-outline:chevron-left"></iconify-icon>'
                },
                zeroRecords: "No matching Roles found",
                emptyTable: "No Roles available"
            },

            drawCallback: function () {
                // Initialize any tooltips or other JS-driven components here
                // Set up delete confirmation
                $('.delete-Role').on('click', function () {
                    const RoleId = $(this).data('id');
                    confirmDelete(RoleId);
                });

                // Apply dark mode styling conditionally
                applyDarkModeToTable();
            }
        });

        // Set up additional event handlers
        setupEventHandlers();
    }

    // Set up event handlers
    function setupEventHandlers() {
        // Handle select all checkbox
        $('#select-all').on('change', function () {
            const isChecked = $(this).prop('checked');
            $('.Role-select').prop('checked', isChecked);
        });

        // Set up dark mode handling
        applyEventForDataTableDarkMode();
    }

    // Delete confirmation function
    function confirmDelete(roleId) {
        if (confirm('Are you sure you want to delete this Role?')) {
            $.ajax({
                url: `/Role/Delete`,
                type: 'POST',
                data: { roleId: roleId },
                headers: {
                    'X-CSRF-TOKEN': $('meta[name="csrf-token"]').attr('content')
                },
                success: function (data) {
                    if (data.success) {
                        RolesTable.ajax.reload();
                        showSuccessToast('Role deleted successfully');
                    } else {
                        showErrorToast(data.message);
                    }
                },
                error: function (xhr) {
                    showErrorToast('Error deleting role', xhr)
                }
            });
        }
    }

    // Expose public methods
    window.RoleTableModule = {
        init: init,
        // Add any other methods you want to expose
        reloadTable: function () {
            if (RolesTable) {
                RolesTable.ajax.reload();
            }
        }
    };
})();

// Initialize the module
RoleTableModule.init();