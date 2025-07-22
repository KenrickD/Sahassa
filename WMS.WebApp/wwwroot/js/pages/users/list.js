// User Table Module
(function () {
    // Private variables
    let usersTable;

    // Initialize module
    function init() {
        document.addEventListener('DOMContentLoaded', function () {
            if (document.getElementById('users-table')) {
                initializeUserDataTable();
            }
        });
    }

    // Table initialization function
    function initializeUserDataTable() {
        usersTable = $('#users-table').DataTable({
            processing: true,
            serverSide: true,
            responsive: true,
            ajax: {
                url: '/User/Getusers',
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
                                            <input class="form-check-input user-select" type="checkbox" value="${data}">
                                            <label class="ms-2 form-check-label">
                                                ${meta.row + 1}
                                            </label>
                                        </div>`;
                    },
                    orderable: false
                },
                { data: 'username' },
                {
                    data: null,
                    render: function (data, type, row) {
                        return `${row.firstName} ${row.lastName || ''}`;
                    }
                },
                { data: 'email' },
                {
                    data: 'clientName',
                    render: function (data) {
                        return data || 'None';
                    }
                },
                {
                    data: 'isActive',
                    render: function (data) {
                        return data
                            ? `<span class="bg-success-100 dark:bg-success-600/25 text-success-600 dark:text-success-400 px-6 py-1.5 rounded-full font-medium text-sm">Active</span>`
                            : `<span class="bg-danger-100 dark:bg-danger-600/25 text-danger-600 dark:text-danger-400 px-6 py-1.5 rounded-full font-medium text-sm">Inactive</span>`;
                    }
                },
                {
                    data: 'id',
                    orderable: false,
                    render: function (data, type, row) {
                        let actions = `<div class="flex">
                            <a href="User/ViewProfile/${data}" class="w-8 h-8 bg-primary-50 dark:bg-primary-600/10 text-primary-600 dark:text-primary-400 rounded-full inline-flex items-center justify-center mr-1" title="View User">
                                <iconify-icon icon="iconamoon:eye-light"></iconify-icon>
                            </a>`;
                        
                        // Show edit button only if user has Write permission
                        if (row.hasWriteAccess) {
                            actions += `<a href="User/EditProfile/${data}" class="w-8 h-8 bg-success-100 dark:bg-success-600/25 text-success-600 dark:text-success-400 rounded-full inline-flex items-center justify-center mr-1" title="Edit User">
                                <iconify-icon icon="lucide:edit"></iconify-icon>
                            </a>`;
                        }
                        
                        // Show delete button only if user has Delete permission
                        if (row.hasDeleteAccess) {
                            actions += `<a href="javascript:void(0)" class="delete-user w-8 h-8 bg-danger-100 dark:bg-danger-600/25 text-danger-600 dark:text-danger-400 rounded-full inline-flex items-center justify-center" data-id="${data}" title="Delete User">
                                <iconify-icon icon="mingcute:delete-2-line"></iconify-icon>
                            </a>`;
                        }
                        
                        actions += `</div>`;
                        return actions;
                    }
                }
            ],
            columnDefs: [
                { targets: [0, 6], sortable: false }
            ],
            order: [[2, 'asc']],
            dom: '<"flex justify-between items-center mb-4"<"flex"f><"flex-1 text-right"l>>rt<"flex justify-between items-center mt-4"<"flex-1"i><"flex"p>>',
            language: {
                search: "",
                searchPlaceholder: "Search users...",
                lengthMenu: "_MENU_ per page",
                info: "Showing _START_ to _END_ of _TOTAL_ users",
                paginate: {
                    first: '<iconify-icon icon="heroicons-outline:chevron-double-left"></iconify-icon>',
                    last: '<iconify-icon icon="heroicons-outline:chevron-double-right"></iconify-icon>',
                    next: '<iconify-icon icon="heroicons-outline:chevron-right"></iconify-icon>',
                    previous: '<iconify-icon icon="heroicons-outline:chevron-left"></iconify-icon>'
                },
                zeroRecords: "No matching users found",
                emptyTable: "No users available"
            },

            drawCallback: function () {
                // Initialize any tooltips or other JS-driven components here
                // Set up delete confirmation
                $('.delete-user').on('click', function () {
                    const userId = $(this).data('id');
                    confirmDelete(userId);
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
            $('.user-select').prop('checked', isChecked);
        });

        // Set up dark mode handling
        applyEventForDataTableDarkMode();
    }

    // Delete confirmation function
    function confirmDelete(userId) {
        if (confirm('Are you sure you want to delete this user?')) {
            $.ajax({
                url: `/User/Delete`,
                data: {userId:userId},
                type: 'POST',
                headers: {
                    'X-CSRF-TOKEN': $('meta[name="csrf-token"]').attr('content')
                },
                success: function (data) {
                    if (data.success) {
                        usersTable.ajax.reload();
                        showSuccessToast('User deleted successfully')
                    } else {
                        showErrorToast(data.message);
                    }
                },
                error: function (xhr) {
                    showErrorToast('Error deleting user', xhr)
                }
            });
        }
    }

    // Expose public methods
    window.UserTableModule = {
        init: init,
        // Add any other methods you want to expose
        reloadTable: function () {
            if (usersTable) {
                usersTable.ajax.reload();
            }
        }
    };
})();

// Initialize the module
UserTableModule.init();