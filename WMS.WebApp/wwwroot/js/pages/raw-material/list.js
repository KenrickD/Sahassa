$(document).ready(function () {
    let table;
    let isGroupedByBatch = false;
    let batchViewMode = 'pallet'; // 'pallet' or 'item'
    let searchTerms = { batches: [], materials: [], pallets: [] };
    //processPendingReleases();
    console.log("Raw material list.js loaded");
    // Fetch and display grand totals immediately
    fetchGrandTotals();

    // Fetch the available search terms
    fetchSearchTerms();

    // Initialize DataTable
    initializeDataTable();

    function processPendingReleases() {
        $.ajax({
            url: '/RawMaterial/ProcessPendingReleases',
            type: 'POST',
            headers: {
                'X-CSRF-TOKEN': $('input[name="__RequestVerificationToken"]').val()
            },
            success: function (response) {
                if (response.success) {
                    console.log("Release processing:", response.message);

                    // If any releases were processed, refresh the data
                    if (response.message === "Pending releases processed successfully") {
                        // Re-fetch the totals and reload table data
                        fetchGrandTotals();
                        if (table) {
                            table.ajax.reload();
                        }
                    }
                } else {
                    console.error("Failed to process releases:", response.message);
                }
            },
            error: function (xhr, status, error) {
                console.error("Error processing releases:", xhr.status, error);
            }
        });
    }

    // Handle export to Excel button click
    $('#export-excel-btn').on('click', function () {
        // Show loading indicator
        const $btn = $(this);
        const originalHtml = $btn.html();
        $btn.html('<iconify-icon icon="eos-icons:loading" class="icon text-xl line-height-1 animate-spin"></iconify-icon> Exporting...');
        $btn.prop('disabled', true);

        // Get any filter value from the DataTable
        const searchValue = table ? table.search() : '';

        // Make the export request
        window.location.href = `/RawMaterial/ExportToExcel?searchTerm=${encodeURIComponent(searchValue)}`;

        // Restore button after a delay
        setTimeout(function () {
            $btn.html(originalHtml);
            $btn.prop('disabled', false);
        }, 2000);
    });

    // Fetch grand totals from the server
    function fetchGrandTotals() {
        $.ajax({
            url: '/RawMaterial/GetRawMaterialTotals',
            type: 'GET',
            dataType: 'json',
            success: function (data) {
                updateGrandTotalsDisplay(data.totalBalancePallet, data.totalBalanceQty);
            },
            error: function () {
                console.error('Failed to load grand totals');
                // Show a fallback message or use cached values
                updateGrandTotalsDisplay(0, 0);
            }
        });
    }

    // Update the grand totals display in the UI
    function updateGrandTotalsDisplay(totalBalancePallet, totalBalanceQty) {
        $('#grand-total-pallets').text(totalBalancePallet.toLocaleString());
        $('#grand-total-qty').text(totalBalanceQty.toLocaleString());
    }

    // Group by Batch toggle event handler
    $('#group-by-batch').on('change', function () {
        isGroupedByBatch = $(this).is(':checked');
        console.log("Group by Batch toggled to:", isGroupedByBatch);

        // Show/hide sub-grouping options with animation
        if (isGroupedByBatch) {
            showSubGrouping();
        } else {
            hideSubGrouping();
        }

        // Reinitialize table
        reinitializeTable();
    });

    // Additional CSS for smooth transitions
    $('#sub-grouping-options').css({
        'transition': 'all 0.3s ease',
        'opacity': '0'
    });

    // Show sub-grouping with animation
    function showSubGrouping() {
        $('#sub-grouping-options').show().animate({ opacity: 1 }, 300);
    }

    // Hide sub-grouping with animation
    function hideSubGrouping() {
        $('#sub-grouping-options').animate({ opacity: 0 }, 300, function () {
            $(this).hide();
        });
    }

    // Batch view mode selector event handler
    $('#batch-view-mode').on('change', function () {
        batchViewMode = $(this).val();
        console.log("Batch view mode changed to:", batchViewMode);

        // Only reinitialize if we're in grouped mode
        if (isGroupedByBatch) {
            reinitializeTable();
        }
    });

    function reinitializeTable() {
        // Destroy existing table instance
        if (table) {
            table.destroy();
            $('#raw-materials-table').empty();

            // Recreate the table structure
            $('#raw-materials-table').html(`
                <thead id="table-header"></thead>
                <tbody></tbody>
            `);
        }

        // Reinitialize table with new configuration
        initializeDataTable();

        // Re-apply autocomplete after changing view
        setTimeout(function () {
            applyAutocomplete();
        }, 300);
    }

    // Function to fetch available search terms
    function fetchSearchTerms() {
        console.log("Fetching search terms...");

        // Fetch batch numbers
        $.ajax({
            url: '/RawMaterial/GetBatchNumbers',
            type: 'GET',
            success: function (data) {
                searchTerms.batches = data;
                console.log("Batch numbers loaded:", data.length);
            },
            error: function (err) {
                console.error("Error loading batch numbers:", err);
            }
        });

        // Fetch material numbers
        $.ajax({
            url: '/RawMaterial/GetMaterialNumbers',
            type: 'GET',
            success: function (data) {
                searchTerms.materials = data;
                console.log("Material numbers loaded:", data.length);
            },
            error: function (err) {
                console.error("Error loading material numbers:", err);
            }
        });

        // Fetch pallet codes
        $.ajax({
            url: '/RawMaterial/GetPalletCodes',
            type: 'GET',
            success: function (data) {
                searchTerms.pallets = data;
                console.log("Pallet codes loaded:", data.length);
            },
            error: function (err) {
                console.error("Error loading pallet codes:", err);
            }
        });
    }

    // Function to apply autocomplete to search input
    function applyAutocomplete() {
        console.log("Applying autocomplete...");

        // Find the search input with multiple fallback selectors
        let searchInput = $('.dataTables_filter input[type="search"]');

        if (searchInput.length === 0) {
            searchInput = $('.dataTables_filter input');
        }

        if (searchInput.length === 0) {
            searchInput = $('input[aria-controls="raw-materials-table"]');
        }

        if (searchInput.length === 0) {
            console.warn("Search input not found, checking if table exists...");

            if ($('.dataTables_wrapper').length === 0) {
                console.warn("DataTables wrapper not found yet, retrying in 500ms");
                setTimeout(applyAutocomplete, 500);
                return;
            }

            console.error("DataTables wrapper exists but search input not found. Stopping autocomplete attempts.");
            return;
        }

        console.log("Search input found, initializing autocomplete");

        // Apply custom styling to the search input
        searchInput.css({
            'width': '250px',
            'padding': '8px 12px',
            'border-radius': '4px'
        });

        // Determine source data based on current view mode
        let sourceData;
        if (!isGroupedByBatch) {
            // Material view - search by materials
            sourceData = searchTerms.materials;
        } else {
            // Both pallet and item level in batch mode - search by batch numbers
            sourceData = searchTerms.batches;
        }

        // Destroy any existing autocomplete instance
        if (searchInput.hasClass('ui-autocomplete-input')) {
            try {
                searchInput.autocomplete('destroy');
                console.log("Destroyed existing autocomplete");
            } catch (e) {
                console.warn("Error destroying existing autocomplete:", e);
            }
        }

        // Initialize the autocomplete
        searchInput.autocomplete({
            source: sourceData,
            minLength: 1,
            appendTo: $('.dataTables_filter'),
            position: {
                my: "left top",
                at: "left bottom+10",
                collision: "none"
            },
            select: function (event, ui) {
                searchInput.val(ui.item.value);
                searchInput.trigger('keyup');
                return false;
            },
            open: function () {
                $('.ui-autocomplete').addClass('custom-autocomplete');
            }
        });

        // Custom render for autocomplete items
        if (searchInput.autocomplete("instance")) {
            searchInput.autocomplete("instance")._renderItem = function (ul, item) {
                return $("<li>")
                    .append("<div class='autocomplete-item'>" + item.label + "</div>")
                    .appendTo(ul);
            };
        }

        console.log("Autocomplete successfully initialized");
    }

    // Function to get current view configuration
    function getCurrentViewConfig() {
        if (!isGroupedByBatch) {
            return {
                type: 'material',
                url: '/RawMaterial/GetRawMaterials',
                searchPlaceholder: "Search raw materials...",
                infoText: "Showing _START_ to _END_ of _TOTAL_ raw materials",
                zeroRecords: "No matching raw materials found",
                emptyTable: "No raw materials available"
            };
        } else if (batchViewMode === 'pallet') {
            return {
                type: 'pallet',
                url: '/RawMaterial/GetRawMaterialsByPallet',
                searchPlaceholder: "Search by batch number...",
                infoText: "Showing _START_ to _END_ of _TOTAL_ pallets",
                zeroRecords: "No matching pallets found",
                emptyTable: "No pallets available"
            };
        } else {
            return {
                type: 'item',
                url: '/RawMaterial/GetRawMaterialsByBatch',
                searchPlaceholder: "Search by batch number...",
                infoText: "Showing _START_ to _END_ of _TOTAL_ items",
                zeroRecords: "No matching items found",
                emptyTable: "No items available"
            };
        }
    }

    // Function to initialize DataTable based on current configuration
    function initializeDataTable() {
        const config = getCurrentViewConfig();
        console.log("Initializing DataTable for view type:", config.type);

        // Update headers and columns based on view type
        const tableHeader = $('#table-header');
        let columns;

        if (config.type === 'material') {
            // Material-level view
            tableHeader.html(`
                <tr>
                    <th class="text-neutral-800 dark:text-white text-left">Material No</th>
                    <th class="text-neutral-800 dark:text-white text-left">Description</th>
                    <th class="text-neutral-800 dark:text-white text-left">Group</th>
                    <th class="text-neutral-800 dark:text-white text-left">Total Bal Qty</th>
                    <th class="text-neutral-800 dark:text-white text-left">Total Bal Plt</th>
                    <th class="text-neutral-800 dark:text-white text-left">Actions</th>
                </tr>
            `);
            columns = [
                { data: 'materialNo', className: 'text-left' },
                { data: 'description', className: 'text-left' },
                {
                    data: null,
                    className: 'text-left',
                    orderable: false,
                    render: function (data, type, row) {
                        return generateGroupCheckboxes(row);
                    }
                },
                { data: 'totalBalanceQty', className: 'text-left' },
                { data: 'totalBalancePallet', className: 'text-left' },
                {
                    data: 'id',
                    className: 'text-left',
                    orderable: false,
                    render: function (data, type, row) {
                        return generateActionButtons(data, row, config.type);
                    }
                }
            ];
        } else if (config.type === 'pallet') {
            // Pallet-level view
            tableHeader.html(`
                <tr>
                    <th class="text-neutral-800 dark:text-white text-left">Batch No</th>
                    <th class="text-neutral-800 dark:text-white text-left">MHU</th>
                    <th class="text-neutral-800 dark:text-white text-left">HU</th>
                    <th class="text-neutral-800 dark:text-white text-left">Material No</th>
                    <th class="text-neutral-800 dark:text-white text-left">Group</th>
                    <th class="text-neutral-800 dark:text-white text-left">Received Date</th>
                    <th class="text-neutral-800 dark:text-white text-left">Actions</th>
                </tr>
            `);
            columns = [
                { data: 'batchNo', className: 'text-left' },
                { data: 'palletCode', className: 'text-left' },
                {
                    data: 'itemCode',
                    className: 'text-left',
                    render: function (data, type, row) {
                        // If there are multiple item codes, show with tooltip
                        if (row.itemCount > 1) {
                            return `<span title="${row.allItemCodes}">${data} (+${row.itemCount - 1})</span>`;
                        }
                        return data;
                    }
                },
                { data: 'materialNo', className: 'text-left' },
                {
                    data: null,
                    className: 'text-left',
                    orderable: false,
                    render: function (data, type, row) {
                        return generateGroupCheckboxes(row);
                    }
                },
                {
                    data: 'receivedDate',
                    className: 'text-left',
                    render: function (data) {
                        return data ? new Date(data).toLocaleDateString() : '';
                    }
                },
                {
                    data: 'id',
                    className: 'text-left',
                    orderable: false,
                    render: function (data, type, row) {
                        return generateActionButtons(data, row, config.type);
                    }
                }
            ];
        } else {
            // Item-level view
            tableHeader.html(`
                <tr>
                    <th class="text-neutral-800 dark:text-white text-left">Batch No</th>
                    <th class="text-neutral-800 dark:text-white text-left">MHU</th>
                    <th class="text-neutral-800 dark:text-white text-left">HU</th>
                    <th class="text-neutral-800 dark:text-white text-left">Material No</th>
                    <th class="text-neutral-800 dark:text-white text-left">Group</th>
                    <th class="text-neutral-800 dark:text-white text-left">Received Date</th>
                    <th class="text-neutral-800 dark:text-white text-left">Actions</th>
                </tr>
            `);
            columns = [
                { data: 'batchNo', className: 'text-left' },
                { data: 'palletCode', className: 'text-left' },
                { data: 'itemCode', className: 'text-left' },
                { data: 'materialNo', className: 'text-left' },
                {
                    data: null,
                    className: 'text-left',
                    orderable: false,
                    render: function (data, type, row) {
                        return generateGroupCheckboxes(row);
                    }
                },
                {
                    data: 'receivedDate',
                    className: 'text-left',
                    render: function (data) {
                        return data ? new Date(data).toLocaleDateString() : '';
                    }
                },
                {
                    data: 'id',
                    className: 'text-left',
                    orderable: false,
                    render: function (data, type, row) {
                        return generateActionButtons(data, row, config.type);
                    }
                }
            ];
        }

        // Initialize DataTable
        table = $('#raw-materials-table').DataTable({
            processing: true,
            serverSide: true,
            responsive: true,
            destroy: true,
            ajax: {
                url: config.url,
                type: 'POST',
                headers: {
                    'X-CSRF-TOKEN': $('meta[name="csrf-token"]').attr('content')
                }
            },
            columns: columns,
            dom: '<"flex justify-between items-center mb-4"<"flex"f><"flex-1 text-right"l>>rt<"flex justify-between items-center mt-4"<"flex-1"i><"flex"p>>',
            language: {
                search: "",
                searchPlaceholder: config.searchPlaceholder,
                lengthMenu: "_MENU_ per page",
                info: config.infoText,
                paginate: {
                    first: '<iconify-icon icon="heroicons-outline:chevron-double-left"></iconify-icon>',
                    last: '<iconify-icon icon="heroicons-outline:chevron-double-right"></iconify-icon>',
                    next: '<iconify-icon icon="heroicons-outline:chevron-right"></iconify-icon>',
                    previous: '<iconify-icon icon="heroicons-outline:chevron-left"></iconify-icon>'
                },
                zeroRecords: config.zeroRecords,
                emptyTable: config.emptyTable
            },
            rowCallback: function (row, data) {
                $(row).addClass('clickable-row');
                $(row).attr('data-row-id', data.id);

                if (config.type === 'material') {
                    $(row).attr('data-url', `/RawMaterial/Details/${data.id}`);
                } else {
                    $(row).attr('data-url', `/RawMaterial/Details/${data.rawMaterialId}`);
                }
            },
            initComplete: function () {
                console.log("DataTable initialization complete");

                const self = this;
                setTimeout(function () {
                    if ($('.dataTables_filter input').length > 0) {
                        applyAutocomplete();
                    } else {
                        self.api().draw(false);
                        setTimeout(applyAutocomplete, 200);
                    }
                }, 100);
            }
        });

        // Re-apply autocomplete after table draw
        table.on('draw.dt', function () {
            setTimeout(function () {
                if ($('.dataTables_filter input').length > 0) {
                    applyAutocomplete();
                }
            }, 100);
        });
    }

    // Helper function to generate group checkboxes
    function generateGroupCheckboxes(row) {
        const hasEditAccess = row.hasEditAccess || false;
        const rawMaterialId = row.rawMaterialId || row.id;
        const disabled = hasEditAccess ? '' : 'disabled';
        const disabledClass = hasEditAccess ? '' : 'opacity-50 cursor-not-allowed';

        const groups = [
            { field: 'group3', label: '3', value: row.group3 || false },
            { field: 'group4_1', label: '4.1', value: row.group4_1 || false },
            { field: 'group6', label: '6', value: row.group6 || false },
            { field: 'group8', label: '8', value: row.group8 || false },
            { field: 'group9', label: '9', value: row.group9 || false },
            { field: 'ndg', label: 'NDG', value: row.ndg || false },
            { field: 'scentaurus', label: 'SCENTAURUS', value: row.scentaurus || false }
        ];

        let html = '<div class="flex flex-wrap gap-1">';

        groups.forEach(group => {
            const checked = group.value ? 'checked' : '';
            const checkboxId = `${group.field}_${rawMaterialId}`;

            html += `
                <label class="flex items-center gap-1 text-xs ${disabledClass}" title="${group.label}">
                    <input type="checkbox" 
                           id="${checkboxId}"
                           class="group-checkbox w-3 h-3" 
                           data-raw-material-id="${rawMaterialId}"
                           data-field="${group.field}"
                           ${checked} 
                           ${disabled}>
                    <span class="text-xs">${group.label}</span>
                </label>
            `;
        });

        html += '</div>';
        return html;
    }

    // Helper function to generate action buttons
    function generateActionButtons(data, row, viewType) {
        let actions = '';
        if (row.hasEditAccess) {
            const rawMaterialId = viewType === 'material' ? data : row.rawMaterialId;

            actions += `<a href="/RawMaterial/Release?rawmaterialId=${rawMaterialId}" 
                class="w-8 h-8 bg-primary-50 dark:bg-primary-600/10 text-primary-600 dark:text-primary-400 rounded-full inline-flex items-center justify-center mr-1"
                title="Release Raw Material">
                <iconify-icon icon="material-symbols:unarchive"></iconify-icon>
            </a>`;
            actions += `<a href="/RawMaterial/Edit/${rawMaterialId}" 
                class="w-8 h-8 bg-success-100 dark:bg-success-600/25 text-success-600 dark:text-success-400 rounded-full inline-flex items-center justify-center mr-1"
                title="Edit Raw Material">
                <iconify-icon icon="lucide:edit"></iconify-icon>
            </a>`;
        }
        return actions;
    }

    // Row double-click handler
    $(document).on('dblclick', '.clickable-row', function () {
        const url = $(this).data('url');
        if (url) {
            window.location.href = url;
        }
    });

    // Group checkbox change handler
    $(document).on('change', '.group-checkbox', function () {
        const $checkbox = $(this);
        const rawMaterialId = $checkbox.data('raw-material-id');
        const fieldName = $checkbox.data('field');
        const isChecked = $checkbox.is(':checked');

        // Disable the checkbox to prevent multiple clicks
        $checkbox.prop('disabled', true);

        // Send AJAX request to update the field
        $.ajax({
            url: '/RawMaterial/UpdateRawMaterialGroupField',
            type: 'POST',
            data: {
                rawMaterialId: rawMaterialId,
                fieldName: fieldName,
                value: isChecked,
                __RequestVerificationToken: $('input[name="__RequestVerificationToken"]').val()
            },
            success: function (response) {
                if (response.success) {
                    // Show success message
                    toastr.success(`${fieldName.toUpperCase()} updated successfully`);
                    //reload the table to reflect changes
                    table.ajax.reload(null, false);
                } else {
                    // Revert checkbox state and show error
                    $checkbox.prop('checked', !isChecked);
                    toastr.error(response.message || 'Failed to update group field');
                }
            },
            error: function (xhr, status, error) {
                // Revert checkbox state and show error
                $checkbox.prop('checked', !isChecked);
                toastr.error('Failed to update group field: ' + error);
            },
            complete: function () {
                // Re-enable the checkbox
                $checkbox.prop('disabled', false);
            }
        });
    });
});