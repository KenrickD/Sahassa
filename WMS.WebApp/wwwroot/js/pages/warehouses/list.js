// Warehouse Table Module
(function () {
    // Private variables
    let WarehousesTable;

    // Initialize module
    function init() {
        document.addEventListener('DOMContentLoaded', function () {
            if (document.getElementById('warehouses-table')) {
                initializeWarehouseDataTable();
            }
        });
    }

    // Table initialization function
    function initializeWarehouseDataTable() {
        WarehousesTable = $('#warehouses-table').DataTable({
            processing: true,
            serverSide: true,
            responsive: true,
            ajax: {
                url: '/Warehouse/GetWarehouses',
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
                                    <input class="form-check-input warehouse-select" type="checkbox" value="${data}">
                                    <label class="ms-2 form-check-label">
                                        ${meta.row + 1}
                                    </label>
                                </div>`;
                    },
                    orderable: false
                },
                {
                    data: 'name',
                    render: function (data, type, row) {
                        return data ? `<span class="text-sm font-medium text-gray-900 dark:text-white">${escapeHtml(data)}</span>` : '<span class="text-gray-400">-</span>';
                    }
                },
                {
                    data: 'code',
                    render: function (data) {
                        return data ? `<span class="text-sm text-gray-600 dark:text-gray-400 font-mono bg-gray-100 dark:bg-gray-800 px-2 py-1 rounded">${escapeHtml(data)}</span>` : '<span class="text-gray-400">-</span>';
                    }
                },
                {
                    data: null,
                    render: function (data, type, row) {
                        const parts = [];
                        if (row.city) parts.push(row.city);
                        if (row.state) parts.push(row.state);
                        if (row.country) parts.push(row.country);

                        if (parts.length === 0) {
                            return '<span class="text-gray-400 text-sm">No location</span>';
                        }

                        return `<span class="text-sm text-gray-700 dark:text-gray-300">${escapeHtml(parts.join(', '))}</span>`;
                    }
                },
                {
                    data: 'contactPerson',
                    render: function (data) {
                        return data ? `<span class="text-sm text-gray-600 dark:text-gray-400">${escapeHtml(data)}</span>` : '<span class="text-gray-400">-</span>';
                    }
                },
                {
                    data: 'clientCount',
                    render: function (data) {
                        return `<div class="flex items-center gap-2">
                                    <span class="inline-flex items-center px-2 py-1 text-xs font-medium bg-blue-100 text-blue-800 dark:bg-blue-900 dark:text-blue-200 rounded-full">
                                        ${data} clients
                                    </span>
                                </div>`;
                    }
                },
                {
                    data: 'zoneCount',
                    render: function (data) {
                        return `<div class="flex items-center gap-2">
                                    <span class="inline-flex items-center px-2 py-1 text-xs font-medium bg-green-100 text-green-800 dark:bg-green-900 dark:text-green-200 rounded-full">
                                        ${data} zones
                                    </span>
                                </div>`;
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
                            <a href="Warehouse/View/${data}" class="w-8 h-8 bg-primary-50 dark:bg-primary-600/10 text-primary-600 dark:text-primary-400 rounded-full inline-flex items-center justify-center mr-1" title="View Warehouse">
                                <iconify-icon icon="iconamoon:eye-light"></iconify-icon>
                            </a>`;

                        // Show edit button only if user has Write permission
                        if (row.hasWriteAccess) {
                            actions += `<a href="Warehouse/Edit/${data}" class="w-8 h-8 bg-success-100 dark:bg-success-600/25 text-success-600 dark:text-success-400 rounded-full inline-flex items-center justify-center mr-1" title="Edit Warehouse">
                                <iconify-icon icon="lucide:edit"></iconify-icon>
                            </a>`;
                        }

                        // Show delete button only if user has Delete permission
                        if (row.hasDeleteAccess) {
                            actions += `<a href="javascript:void(0)" class="delete-warehouse w-8 h-8 bg-danger-100 dark:bg-danger-600/25 text-danger-600 dark:text-danger-400 rounded-full inline-flex items-center justify-center" data-id="${data}" title="Delete Warehouse">
                                <iconify-icon icon="mingcute:delete-2-line"></iconify-icon>
                            </a>`;
                        }

                        actions += `</div>`;
                        return actions;
                    }
                }
            ],
            columnDefs: [
                { targets: [0, 8], sortable: false }
            ],
            order: [[1, 'asc']],
            dom: '<"flex justify-between items-center mb-4"<"flex"f><"flex-1 text-right"l>>rt<"flex justify-between items-center mt-4"<"flex-1"i><"flex"p>>',
            language: {
                search: "",
                searchPlaceholder: "Search Warehouses...",
                lengthMenu: "_MENU_ per page",
                info: "Showing _START_ to _END_ of _TOTAL_ warehouses",
                paginate: {
                    first: '<iconify-icon icon="heroicons-outline:chevron-double-left"></iconify-icon>',
                    last: '<iconify-icon icon="heroicons-outline:chevron-double-right"></iconify-icon>',
                    next: '<iconify-icon icon="heroicons-outline:chevron-right"></iconify-icon>',
                    previous: '<iconify-icon icon="heroicons-outline:chevron-left"></iconify-icon>'
                },
                zeroRecords: "No matching warehouses found",
                emptyTable: "No warehouses available"
            },

            drawCallback: function () {
                // Initialize any tooltips or other JS-driven components here
                // Set up delete confirmation
                $('.delete-warehouse').on('click', function () {
                    const warehouseId = $(this).data('id');
                    confirmDelete(warehouseId);
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
            $('.warehouse-select').prop('checked', isChecked);
        });

        // Set up dark mode handling
        applyEventForDataTableDarkMode();
    }

    // Delete confirmation function
    function confirmDelete(warehouseId) {
        if (confirm('Are you sure you want to delete this warehouse? This action cannot be undone and will affect all associated data.')) {
            $.ajax({
                url: `/Warehouse/Delete`,
                type: 'POST',
                data: { warehouseId: warehouseId },
                headers: {
                    'X-CSRF-TOKEN': $('meta[name="csrf-token"]').attr('content')
                },
                success: function (data) {
                    if (data.success) {
                        WarehousesTable.ajax.reload();
                        showSuccessToast('Warehouse deleted successfully');
                    } else {
                        showErrorToast(data.message || 'Failed to delete warehouse');
                    }
                },
                error: function (xhr) {
                    console.error('Delete error:', xhr);
                    let errorMessage = 'Error deleting warehouse';

                    if (xhr.responseJSON && xhr.responseJSON.message) {
                        errorMessage = xhr.responseJSON.message;
                    } else if (xhr.status === 400) {
                        errorMessage = 'Bad request - warehouse may have associated data';
                    } else if (xhr.status === 404) {
                        errorMessage = 'Warehouse not found';
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
    window.WarehouseTableModule = {
        init: init,
        // Add any other methods you want to expose
        reloadTable: function () {
            if (WarehousesTable) {
                WarehousesTable.ajax.reload();
            }
        }
    };
})();

// Initialize the module
WarehouseTableModule.init();