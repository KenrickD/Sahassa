// Location Table Module
(function () {
    // Private variables
    let LocationsTable;
    let currentFilters = {
        warehouseId: null,
        zoneId: null
    };

    // Initialize module
    function init() {
        document.addEventListener('DOMContentLoaded', function () {
            if (document.getElementById('locations-table')) {
                initializeFilters();
                initializeLocationDataTable();
                setupEventHandlers();
            }
        });
    }

    // Initialize filter dropdowns
    function initializeFilters() {
        // Show filter controls
        document.getElementById('filter-controls').style.display = 'flex';

        // Load warehouses
        loadWarehouses();
    }

    // Load warehouses for filter
    function loadWarehouses() {
        $.ajax({
            url: '/Warehouse/GetWarehousesForDropdown',
            type: 'GET',
            success: function (response) {
                const warehouseSelect = $('#warehouse-filter');
                warehouseSelect.empty();
                warehouseSelect.append('<option value="">All Warehouses</option>');

                if (response.success && response.data) {
                    response.data.forEach(function (warehouse) {
                        warehouseSelect.append(`<option value="${warehouse.id}">${warehouse.name}</option>`);
                    });
                }
            },
            error: function () {
                console.error('Failed to load warehouses for filter');
            }
        });
    }

    // Load zones based on selected warehouse
    function loadZones(warehouseId) {
        const zoneSelect = $('#zone-filter');
        zoneSelect.empty();
        zoneSelect.append('<option value="">All Zones</option>');

        if (!warehouseId) {
            zoneSelect.prop('disabled', true);
            return;
        }

        $.ajax({
            url: `/Location/GetZonesByWarehouse`,
            type: 'GET',
            data: { warehouseId: warehouseId },
            success: function (response) {
                if (response.success && response.data) {
                    response.data.forEach(function (zone) {
                        zoneSelect.append(`<option value="${zone.id}">${zone.name}</option>`);
                    });
                    zoneSelect.prop('disabled', false);
                }
            },
            error: function () {
                console.error('Failed to load zones for filter');
                zoneSelect.prop('disabled', true);
            }
        });
    }

    // Table initialization function
    function initializeLocationDataTable() {
        LocationsTable = $('#locations-table').DataTable({
            processing: true,
            serverSide: true,
            responsive: true,
            ajax: {
                url: '/Location/GetLocations',
                type: 'POST',
                headers: {
                    'X-CSRF-TOKEN': $('meta[name="csrf-token"]').attr('content')
                },
                data: function (d) {
                    // Add filter parameters to request
                    d.warehouseId = currentFilters.warehouseId;
                    d.zoneId = currentFilters.zoneId;
                    return d;
                }
            },
            columns: [
                {
                    data: 'id',
                    width: '5%',
                    render: function (data, type, row, meta) {
                        return `<div class="form-check style-check flex items-center">
                                    <input class="form-check-input location-select" type="checkbox" value="${data}">
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
                    data: 'zoneName',
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
                    data: null,
                    render: function (data, type, row) {
                        const parts = [];
                        if (row.row) parts.push(`Row ${row.row}`);
                        if (row.bay) parts.push(`Bay ${row.bay.toString().padStart(2, '0')}`);
                        if (row.level) parts.push(`Level ${row.level}`);

                        if (parts.length === 0) {
                            return '<span class="text-gray-400 text-sm">No position</span>';
                        }

                        return `<span class="text-sm text-gray-700 dark:text-gray-300">${parts.join(' - ')}</span>`;
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
                            <a href="Location/View/${data}" class="w-8 h-8 bg-primary-50 dark:bg-primary-600/10 text-primary-600 dark:text-primary-400 rounded-full inline-flex items-center justify-center mr-1" title="View Location">
                                <iconify-icon icon="iconamoon:eye-light"></iconify-icon>
                            </a>`;

                        // Show edit button only if user has Write permission
                        if (row.hasWriteAccess) {
                            actions += `<a href="Location/Edit/${data}" class="w-8 h-8 bg-success-100 dark:bg-success-600/25 text-success-600 dark:text-success-400 rounded-full inline-flex items-center justify-center mr-1" title="Edit Location">
                                <iconify-icon icon="lucide:edit"></iconify-icon>
                            </a>`;
                        }

                        // Show delete button only if user has Delete permission
                        if (row.hasDeleteAccess) {
                            actions += `<a href="javascript:void(0)" class="delete-location w-8 h-8 bg-danger-100 dark:bg-danger-600/25 text-danger-600 dark:text-danger-400 rounded-full inline-flex items-center justify-center" data-id="${data}" title="Delete Location">
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
            order: [[3, 'asc']],
            dom: '<"filter-controls-wrapper"><"flex justify-between items-center mb-4"<"flex"f><"flex-1 text-right"l>>rt<"flex justify-between items-center mt-4"<"flex-1"i><"flex"p>>',
            language: {
                search: "",
                searchPlaceholder: "Search Locations...",
                lengthMenu: "_MENU_ per page",
                info: "Showing _START_ to _END_ of _TOTAL_ locations",
                paginate: {
                    first: '<iconify-icon icon="heroicons-outline:chevron-double-left"></iconify-icon>',
                    last: '<iconify-icon icon="heroicons-outline:chevron-double-right"></iconify-icon>',
                    next: '<iconify-icon icon="heroicons-outline:chevron-right"></iconify-icon>',
                    previous: '<iconify-icon icon="heroicons-outline:chevron-left"></iconify-icon>'
                },
                zeroRecords: "No matching locations found",
                emptyTable: "No locations available"
            },

            drawCallback: function () {
                // Set up delete confirmation
                $('.delete-location').on('click', function () {
                    const locationId = $(this).data('id');
                    confirmDelete(locationId);
                });

                // Apply dark mode styling conditionally
                applyDarkModeToTable();
            },

            initComplete: function () {
                // Move filter controls to DataTables wrapper
                const filterControlsHtml = $('#filter-controls')[0].outerHTML;
                $('#filter-controls').remove();
                $('.filter-controls-wrapper').html(filterControlsHtml);
                $('#filter-controls').show();

                // Reinitialize filter event handlers after moving
                initializeFilterEventHandlers();
            }
        });
    }

    // Initialize filter event handlers
    function initializeFilterEventHandlers() {
        // Warehouse filter change
        $('#warehouse-filter').off('change').on('change', function () {
            const warehouseId = $(this).val();
            currentFilters.warehouseId = warehouseId || null;
            currentFilters.zoneId = null; // Reset zone when warehouse changes

            // Update zone dropdown
            loadZones(warehouseId);
            $('#zone-filter').val('');

            // Reload table
            if (LocationsTable) {
                LocationsTable.ajax.reload();
            }
        });

        // Zone filter change
        $('#zone-filter').off('change').on('change', function () {
            const zoneId = $(this).val();
            currentFilters.zoneId = zoneId || null;

            // Reload table
            if (LocationsTable) {
                LocationsTable.ajax.reload();
            }
        });

        // Clear filters
        $('#clear-filters').off('click').on('click', function () {
            // Reset filter values
            currentFilters.warehouseId = null;
            currentFilters.zoneId = null;

            // Reset dropdowns
            $('#warehouse-filter').val('');
            $('#zone-filter').val('').prop('disabled', true);

            // Reload table
            if (LocationsTable) {
                LocationsTable.ajax.reload();
            }
        });
    }

    // Set up event handlers
    function setupEventHandlers() {
        // Handle select all checkbox
        $('#select-all').on('change', function () {
            const isChecked = $(this).prop('checked');
            $('.location-select').prop('checked', isChecked);
        });

        // Set up dark mode handling
        applyEventForDataTableDarkMode();
    }

    // Delete confirmation function
    function confirmDelete(locationId) {
        if (confirm('Are you sure you want to delete this location? This action cannot be undone.')) {
            $.ajax({
                url: `/Location/Delete`,
                type: 'POST',
                data: { locationId: locationId },
                headers: {
                    'X-CSRF-TOKEN': $('meta[name="csrf-token"]').attr('content')
                },
                success: function (data) {
                    if (data.success) {
                        LocationsTable.ajax.reload();
                        showSuccessToast('Location deleted successfully');
                    } else {
                        showErrorToast(data.message || 'Failed to delete location');
                    }
                },
                error: function (xhr) {
                    console.error('Delete error:', xhr);
                    let errorMessage = 'Error deleting location';

                    if (xhr.responseJSON && xhr.responseJSON.message) {
                        errorMessage = xhr.responseJSON.message;
                    } else if (xhr.status === 400) {
                        errorMessage = 'Bad request - please check the location data';
                    } else if (xhr.status === 404) {
                        errorMessage = 'Location not found';
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
    window.LocationTableModule = {
        init: init,
        reloadTable: function () {
            if (LocationsTable) {
                LocationsTable.ajax.reload();
            }
        }
    };
})();

// Initialize the module
LocationTableModule.init();