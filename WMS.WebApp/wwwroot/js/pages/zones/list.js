// Zone Table Module
(function () {
    // Private variables
    let ZonesTable;

    // Initialize module
    function init() {
        document.addEventListener('DOMContentLoaded', function () {
            if (document.getElementById('zones-table')) {
                initializeZoneDataTable();
            }
        });
    }

    // Table initialization function
    function initializeZoneDataTable() {
        ZonesTable = $('#zones-table').DataTable({
            processing: true,
            serverSide: true,
            responsive: true,
            ajax: {
                url: '/Zone/GetZones',
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
                                    <input class="form-check-input zone-select" type="checkbox" value="${data}">
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
                    data: 'description',
                    render: function (data) {
                        if (!data) return '<span class="text-gray-400">-</span>';
                        const truncated = data.length > 50 ? data.substring(0, 50) + '...' : data;
                        return `<span class="text-sm text-gray-600 dark:text-gray-400" title="${escapeHtml(data)}">${escapeHtml(truncated)}</span>`;
                    }
                },
                {
                    data: 'locationCount',
                    render: function (data) {
                        return `<div class="flex items-center gap-2">
                                    <span class="inline-flex items-center px-2 py-1 text-xs font-medium bg-blue-100 text-blue-800 dark:bg-blue-900 dark:text-blue-200 rounded-full">
                                        ${data} locations
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
                            <a href="Zone/View/${data}" class="w-8 h-8 bg-primary-50 dark:bg-primary-600/10 text-primary-600 dark:text-primary-400 rounded-full inline-flex items-center justify-center mr-1" title="View Zone">
                                <iconify-icon icon="iconamoon:eye-light"></iconify-icon>
                            </a>`;

                        // Show edit button only if user has Write permission
                        if (row.hasWriteAccess) {
                            actions += `<a href="Zone/Edit/${data}" class="w-8 h-8 bg-success-100 dark:bg-success-600/25 text-success-600 dark:text-success-400 rounded-full inline-flex items-center justify-center mr-1" title="Edit Zone">
                                <iconify-icon icon="lucide:edit"></iconify-icon>
                            </a>`;
                        }

                        // Show delete button only if user has Delete permission
                        if (row.hasDeleteAccess) {
                            actions += `<a href="javascript:void(0)" class="delete-zone w-8 h-8 bg-danger-100 dark:bg-danger-600/25 text-danger-600 dark:text-danger-400 rounded-full inline-flex items-center justify-center" data-id="${data}" title="Delete Zone">
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
                searchPlaceholder: "Search Zones...",
                lengthMenu: "_MENU_ per page",
                info: "Showing _START_ to _END_ of _TOTAL_ zones",
                paginate: {
                    first: '<iconify-icon icon="heroicons-outline:chevron-double-left"></iconify-icon>',
                    last: '<iconify-icon icon="heroicons-outline:chevron-double-right"></iconify-icon>',
                    next: '<iconify-icon icon="heroicons-outline:chevron-right"></iconify-icon>',
                    previous: '<iconify-icon icon="heroicons-outline:chevron-left"></iconify-icon>'
                },
                zeroRecords: "No matching zones found",
                emptyTable: "No zones available"
            },

            drawCallback: function () {
                // Initialize any tooltips or other JS-driven components here
                // Set up delete confirmation
                $('.delete-zone').on('click', function () {
                    const zoneId = $(this).data('id');
                    confirmDelete(zoneId);
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
            $('.zone-select').prop('checked', isChecked);
        });

        // Set up dark mode handling
        applyEventForDataTableDarkMode();
    }

    // Delete confirmation function
    function confirmDelete(zoneId) {
        if (confirm('Are you sure you want to delete this zone? This action cannot be undone.')) {
            $.ajax({
                url: `/Zone/Delete`,
                type: 'POST',
                data: { zoneId: zoneId },
                headers: {
                    'X-CSRF-TOKEN': $('meta[name="csrf-token"]').attr('content')
                },
                success: function (data) {
                    if (data.success) {
                        ZonesTable.ajax.reload();
                        showSuccessToast('Zone deleted successfully');
                    } else {
                        showErrorToast(data.message || 'Failed to delete zone');
                    }
                },
                error: function (xhr) {
                    console.error('Delete error:', xhr);
                    let errorMessage = 'Error deleting zone';

                    if (xhr.responseJSON && xhr.responseJSON.message) {
                        errorMessage = xhr.responseJSON.message;
                    } else if (xhr.status === 400) {
                        errorMessage = 'Bad request - please check the zone data';
                    } else if (xhr.status === 404) {
                        errorMessage = 'Zone not found';
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
    window.ZoneTableModule = {
        init: init,
        // Add any other methods you want to expose
        reloadTable: function () {
            if (ZonesTable) {
                ZonesTable.ajax.reload();
            }
        }
    };
})();

// Initialize the module
ZoneTableModule.init();