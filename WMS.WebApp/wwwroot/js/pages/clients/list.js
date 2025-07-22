// Client Table Module
(function () {
    // Private variables
    let ClientsTable;

    // Initialize module
    function init() {
        document.addEventListener('DOMContentLoaded', function () {
            if (document.getElementById('clients-table')) {
                initializeClientDataTable();
            }
        });
    }

    // Table initialization function
    function initializeClientDataTable() {
        ClientsTable = $('#clients-table').DataTable({
            processing: true,
            serverSide: true,
            responsive: true,
            ajax: {
                url: '/Client/GetClients',
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
                                    <input class="form-check-input client-select" type="checkbox" value="${data}">
                                    <label class="ms-2 form-check-label">
                                        ${meta.row + 1}
                                    </label>
                                </div>`;
                    },
                    orderable: false
                },
                {
                    data: 'warehouseName',
                    render: function (data, type, row) {
                        return data ? `<span class="text-sm text-gray-600 dark:text-gray-400">${escapeHtml(data)}</span>` : '<span class="text-gray-400">-</span>';
                    }
                },
                {
                    data: 'name',
                    render: function (data, type, row) {
                        return data ? `<span class="text-sm text-gray-600 dark:text-gray-400">${escapeHtml(data)}</span>` : '<span class="text-gray-400">-</span>';
                    }
                },
                {
                    data: 'code',
                    render: function (data) {
                        return data ? `<span class="text-sm text-gray-600 dark:text-gray-400">${escapeHtml(data)}</span>` : '<span class="text-gray-400">-</span>';
                    }
                },
                {
                    data: 'contactPerson',
                    render: function (data) {
                        return data ? `<span class="text-sm text-gray-600 dark:text-gray-400">${escapeHtml(data)}</span>` : '<span class="text-gray-400">-</span>';
                    }
                },
                {
                    data: 'contactEmail',
                    render: function (data) {
                        return data ? `<a href="mailto:${escapeHtml(data)}" class="text-sm text-blue-600 hover:text-blue-800">${escapeHtml(data)}</a>` : '<span class="text-gray-400">-</span>';
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
                            <a href="Client/View/${data}" class="w-8 h-8 bg-primary-50 dark:bg-primary-600/10 text-primary-600 dark:text-primary-400 rounded-full inline-flex items-center justify-center mr-1" title="View Client">
                                <iconify-icon icon="iconamoon:eye-light"></iconify-icon>
                            </a>`;

                        // Show edit button only if user has Write permission
                        if (row.hasWriteAccess) {
                            actions += `<a href="Client/Edit/${data}" class="w-8 h-8 bg-success-100 dark:bg-success-600/25 text-success-600 dark:text-success-400 rounded-full inline-flex items-center justify-center mr-1" title="Edit Client">
                                <iconify-icon icon="lucide:edit"></iconify-icon>
                            </a>`;
                        }

                        // Show delete button only if user has Delete permission
                        if (row.hasDeleteAccess) {
                            actions += `<a href="javascript:void(0)" class="delete-client w-8 h-8 bg-danger-100 dark:bg-danger-600/25 text-danger-600 dark:text-danger-400 rounded-full inline-flex items-center justify-center" data-id="${data}" title="Delete Client">
                                <iconify-icon icon="mingcute:delete-2-line"></iconify-icon>
                            </a>`;
                        }

                        actions += `</div>`;
                        return actions;
                    }
                }
            ],
            columnDefs: [
                { targets: [0, 7], sortable: false }
            ],
            order: [[1, 'asc']],
            dom: '<"flex justify-between items-center mb-4"<"flex"f><"flex-1 text-right"l>>rt<"flex justify-between items-center mt-4"<"flex-1"i><"flex"p>>',
            language: {
                search: "",
                searchPlaceholder: "Search Clients...",
                lengthMenu: "_MENU_ per page",
                info: "Showing _START_ to _END_ of _TOTAL_ clients",
                paginate: {
                    first: '<iconify-icon icon="heroicons-outline:chevron-double-left"></iconify-icon>',
                    last: '<iconify-icon icon="heroicons-outline:chevron-double-right"></iconify-icon>',
                    next: '<iconify-icon icon="heroicons-outline:chevron-right"></iconify-icon>',
                    previous: '<iconify-icon icon="heroicons-outline:chevron-left"></iconify-icon>'
                },
                zeroRecords: "No matching clients found",
                emptyTable: "No clients available"
            },

            drawCallback: function () {
                // Initialize any tooltips or other JS-driven components here
                // Set up delete confirmation
                $('.delete-client').on('click', function () {
                    const clientId = $(this).data('id');
                    confirmDelete(clientId);
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
            $('.client-select').prop('checked', isChecked);
        });

        // Set up dark mode handling
        applyEventForDataTableDarkMode();
    }

    // Delete confirmation function
    function confirmDelete(clientId) {
        if (confirm('Are you sure you want to delete this client? This action cannot be undone.')) {
            $.ajax({
                url: `/Client/Delete`,
                type: 'POST',
                data: { clientId: clientId },
                headers: {
                    'X-CSRF-TOKEN': $('meta[name="csrf-token"]').attr('content')
                },
                success: function (data) {
                    if (data.success) {
                        ClientsTable.ajax.reload();
                        showSuccessToast('Client deleted successfully');
                    } else {
                        showErrorToast(data.message || 'Failed to delete client');
                    }
                },
                error: function (xhr) {
                    console.error('Delete error:', xhr);
                    let errorMessage = 'Error deleting client';

                    if (xhr.responseJSON && xhr.responseJSON.message) {
                        errorMessage = xhr.responseJSON.message;
                    } else if (xhr.status === 400) {
                        errorMessage = 'Bad request - please check the client data';
                    } else if (xhr.status === 404) {
                        errorMessage = 'Client not found';
                    } else if (xhr.status === 500) {
                        errorMessage = 'Server error occurred';
                    }

                    showErrorToast(errorMessage);
                }
            });
        }
    }

    // Utility function to escape HTML
    function escapeHtml(text) {
        if (!text) return '';
        const map = {
            '&': '&amp;',
            '<': '&lt;',
            '>': '&gt;',
            '"': '&quot;',
            "'": '&#039;'
        };
        return text.toString().replace(/[&<>"']/g, function (m) { return map[m]; });
    }

    // Expose public methods
    window.ClientTableModule = {
        init: init,
        // Add any other methods you want to expose
        reloadTable: function () {
            if (ClientsTable) {
                ClientsTable.ajax.reload();
            }
        }
    };
})();

// Initialize the module
ClientTableModule.init();