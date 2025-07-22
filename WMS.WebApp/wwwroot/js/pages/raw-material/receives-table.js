(function () {
    let receivesTable;
    let showGrouped = false;
    const token = $('input[name="__RequestVerificationToken"]').val();
    // Check if dark mode is active by looking for the dark class on html or body
    const isDarkMode = document.documentElement.classList.contains('dark') ||
        document.body.classList.contains('dark');

    // Track date filter state
    let activeDateFilter = {
        startDate: '',
        endDate: ''
    };

    function formatDate(input) {
        if (!input) return '';
        const date = new Date(input);

        // Format as YYYY-MM-DD (ISO format)
        const isoDate = date.toISOString().split('T')[0];

        // Also format in a more human-readable format
        const options = { year: 'numeric', month: 'short', day: 'numeric' };
        const readableDate = date.toLocaleDateString(undefined, options);

        // Return both formats in a way that's both readable and searchable
        return `<span title="${isoDate}" data-date="${isoDate}" class="date-cell">${readableDate}</span>`;
    }

    function initReceivesTable() {
        const rawMaterialId = document.getElementById("receives-table").dataset.rawMaterialId;

        // Create date range filter inputs above the table
        const filterContainer = document.createElement('div');
        // Apply dark mode classes if needed
        filterContainer.className = isDarkMode
            ? 'date-filter mb-3 p-3 bg-gray-800 rounded text-gray-200'
            : 'date-filter mb-3 p-3 bg-gray-50 rounded';

        filterContainer.innerHTML = `
            <div class="flex flex-wrap gap-3 items-end">
                <div>
                    <label class="block text-sm font-medium ${isDarkMode ? 'text-gray-300' : 'text-gray-700'} mb-1">Date Range</label>
                    <div class="flex items-center gap-2">
                        <input type="date" id="date-filter-start" class="rounded ${isDarkMode ? 'bg-gray-700 border-gray-600 text-white' : 'border-gray-300'} shadow-sm">
                        <span>to</span>
                        <input type="date" id="date-filter-end" class="rounded ${isDarkMode ? 'bg-gray-700 border-gray-600 text-white' : 'border-gray-300'} shadow-sm">
                    </div>
                </div>
                <div>
                    <button id="apply-date-filter" class="btn btn-primary btn-sm">Apply Filter</button>
                    <button id="clear-date-filter" class="btn btn-secondary btn-sm ml-2">Clear</button>
                </div>
                <div id="active-date-filter" class="hidden w-full mt-1 py-1 px-2 ${isDarkMode ? 'bg-blue-900/30 text-blue-200' : 'bg-blue-100 text-blue-800'} rounded">
                    <span class="font-medium">Active date filter:</span> <span id="date-filter-text"></span>
                    <button id="remove-date-filter" class="ml-2 text-xs ${isDarkMode ? 'text-red-300 hover:text-red-200' : 'text-red-600 hover:text-red-700'}">
                        <iconify-icon icon="lucide:x-circle"></iconify-icon> Remove
                    </button>
                </div>
            </div>
        `;

        document.getElementById("receives-table").parentNode.insertBefore(filterContainer, document.getElementById("receives-table"));

        // Initialize date filter handlers
        document.getElementById('apply-date-filter').addEventListener('click', function () {
            // Get the date values
            activeDateFilter.startDate = document.getElementById('date-filter-start').value;
            activeDateFilter.endDate = document.getElementById('date-filter-end').value;

            if (activeDateFilter.startDate || activeDateFilter.endDate) {
                // Show active filter indicator
                const activeFilterEl = document.getElementById('active-date-filter');
                const filterTextEl = document.getElementById('date-filter-text');

                if (activeDateFilter.startDate && activeDateFilter.endDate) {
                    filterTextEl.textContent = `${activeDateFilter.startDate} to ${activeDateFilter.endDate}`;
                } else if (activeDateFilter.startDate) {
                    filterTextEl.textContent = `From ${activeDateFilter.startDate}`;
                } else {
                    filterTextEl.textContent = `Until ${activeDateFilter.endDate}`;
                }

                activeFilterEl.classList.remove('hidden');

                // Redraw the table to apply the date filter
                receivesTable.draw();
            }
        });

        document.getElementById('clear-date-filter').addEventListener('click', function () {
            // Clear date inputs
            document.getElementById('date-filter-start').value = '';
            document.getElementById('date-filter-end').value = '';

            // Clear date filter state
            activeDateFilter.startDate = '';
            activeDateFilter.endDate = '';

            // Hide active filter indicator
            document.getElementById('active-date-filter').classList.add('hidden');

            // Redraw the table to remove the date filter
            receivesTable.draw();
        });

        // Add event listener for remove filter button
        document.getElementById('remove-date-filter').addEventListener('click', function () {
            document.getElementById('clear-date-filter').click();
        });

        // Add a custom search function that includes date filters
        $.fn.dataTable.ext.search.push(function (settings, data, dataIndex, rowData) {
            // If no date filter is active, return true (include all rows)
            if (!activeDateFilter.startDate && !activeDateFilter.endDate) {
                return true;
            }

            // Get the date from the row data
            const rowDate = new Date(rowData.receivedDate);

            // Apply date filter
            if (activeDateFilter.startDate && activeDateFilter.endDate) {
                const startDate = new Date(activeDateFilter.startDate);
                const endDate = new Date(activeDateFilter.endDate + 'T23:59:59'); // End of day
                return rowDate >= startDate && rowDate <= endDate;
            } else if (activeDateFilter.startDate) {
                const startDate = new Date(activeDateFilter.startDate);
                return rowDate >= startDate;
            } else if (activeDateFilter.endDate) {
                const endDate = new Date(activeDateFilter.endDate + 'T23:59:59'); // End of day
                return rowDate <= endDate;
            }

            return true;
        });

        receivesTable = $('#receives-table').DataTable({
            processing: true,
            serverSide: true,
            ajax: {
                url: `/RawMaterial/GetPaginatedReceives?rawMaterialId=${rawMaterialId}`,
                type: 'POST',
                contentType: 'application/json',
                headers: {
                    'X-CSRF-TOKEN': token,
                    'X-Requested-With': 'XMLHttpRequest'
                },
                data: function (d) {
                    // Add date filter parameters to the request
                    if (activeDateFilter.startDate || activeDateFilter.endDate) {
                        d.search = d.search || {};
                        d.search.value = buildSearchWithDateFilter(d.search.value || '');
                    }

                    d.showGrouped = showGrouped;
                    return JSON.stringify(d);
                },
                error: function (xhr, error, thrown) {
                    console.error('Failed to load receive data:', error);
                }
            },
            columns: [
                {
                    data: 'receivedDate',
                    render: function (data, type, row) {
                        // Add a visual indicator for grouped items
                        let groupedIndicator = '';
                        if (row.isGrouped) {
                            groupedIndicator = `<span class="ml-2 badge badge-info" title="Group of ${row.receivesInGroup} receives">×${row.receivesInGroup}</span>`;
                        }
                        return formatDate(data) + groupedIndicator;
                    },
                    // Add an invisible field with the full year for searching
                    createdCell: function (td, cellData, rowData, row, col) {
                        const date = new Date(cellData);
                        const searchData = [
                            date.getFullYear(),           // Year
                            date.getMonth() + 1,          // Month (1-12)
                            date.getDate(),               // Day
                            date.toISOString().split('T')[0]  // Full date as YYYY-MM-DD
                        ].join(' ');

                        $(td).attr('data-search', searchData);
                    }
                },
                { data: 'packSize' },
                { data: 'quantity' },
                { data: 'totalPallets' },
                {
                    data: null,
                    render: data => `${data.balanceQuantity} / ${data.quantity}`
                },
                {
                    data: null,
                    render: data => `${data.balancePallets} / ${data.totalPallets}`
                },
                {
                    data: 'containerUrl',
                    render: url => url
                        ? `<a href="${url}" class="btn btn-sm btn-primary" target="_blank">View Container</a>`
                        : '<span class="text-muted">No URL</span>'
                },
                {
                    data: null,
                    render: function (data) {
                        // Check if this receive has a GroupId (part of a container group)
                        if (data.groupId) {
                            return `<a href="/RawMaterial/Pallets?ReceiveId=${data.id}&isGrouped=true&groupId=${data.groupId}" 
                     target="_blank" class="btn btn-sm btn-primary">Pallets (Grouped)</a>`;
                        } else {
                            return `<a href="/RawMaterial/Pallets?ReceiveId=${data.id}" 
                     target="_blank" class="btn btn-sm btn-primary">Pallets</a>`;
                        }
                    }
                },
                {
                    data: null,
                    render: data => {
                        const toggleText = showGrouped
                            ? "Show Items (Grouped by BatchNo)"
                            : "Show Items (Ungrouped)";
                        const toggleClass = showGrouped ? "btn-warning" : "btn-success";
                        // Pass both the receiveId and groupId if it exists
                        let url = `/RawMaterial/Items?ReceiveId=${data.id}&grouped=${showGrouped}`;
                        if (data.groupId) {
                            url += `&isGrouped=true&groupId=${data.groupId}`;
                        }
                        return `<a href="${url}" class="btn btn-sm btn-primary ${toggleClass}" target="_blank">${toggleText}</a>`;
                    }
                },
                {
                    data: null,
                    orderable: false,
                    searchable: false,
                    render: (data, type, row) => {
                        if (row.hasEditAccess) {
                            return `<a href="/RawMaterial/EditReceive?id=${row.id}&rawMaterialId=${rawMaterialId}&showGrouped=${showGrouped}" 
                                        class="w-8 h-8 bg-warning-100 dark:bg-warning-600/25 text-warning-600 dark:text-warning-400 rounded-full inline-flex items-center justify-center"
                                        title="Edit Receive">
                                        <iconify-icon icon="lucide:edit"></iconify-icon>
                                    </a>`;
                        }
                        return '';
                    }
                }
            ],
            order: [[0, 'desc']],
            dom: '<"flex justify-between items-center mb-4"<"flex"f><"flex-1 text-right"l>>rt<"flex justify-between items-center mt-4"<"flex-1"i><"flex"p>>',
            language: {
                search: "",
                searchPlaceholder: "Search receives...",
                lengthMenu: "_MENU_ per page",
                info: "Showing _START_ to _END_ of _TOTAL_ receives",
                paginate: {
                    first: '<iconify-icon icon="heroicons-outline:chevron-double-left"></iconify-icon>',
                    last: '<iconify-icon icon="heroicons-outline:chevron-double-right"></iconify-icon>',
                    next: '<iconify-icon icon="heroicons-outline:chevron-right"></iconify-icon>',
                    previous: '<iconify-icon icon="heroicons-outline:chevron-left"></iconify-icon>'
                },
                zeroRecords: "No matching receives found",
                emptyTable: "No receive data available"
            },
            // Use initComplete to apply additional adjustments after table initialization
            initComplete: function () {
                // Add dark mode classes to DataTables elements if in dark mode
                if (isDarkMode) {
                    // Style the search input
                    $('.dataTables_filter input').addClass('bg-gray-700 border-gray-600 text-white');

                    // Style the length select
                    $('.dataTables_length select').addClass('bg-gray-700 border-gray-600 text-white');

                    // Add a helper class to the table wrapper
                    $(this).closest('.dataTables_wrapper').addClass('dark-mode-table');
                }

                // Handle manual search box input to preserve date filters
                $('.dataTables_filter input').on('keyup', function () {
                    receivesTable.draw();
                });
            },
            // Add custom CSS class for grouped rows
            createdRow: function (row, data, dataIndex) {
                if (data.isGrouped) {
                    $(row).addClass('grouped-row');
                    // Add a tooltip to the row explaining it's a grouped row
                    $(row).attr('title', `This row represents ${data.receivesInGroup} receives with the same container`);
                }
            }
        });
    }

    // Helper function to build the search term with date filter
    function buildSearchWithDateFilter(baseSearch) {
        if (!activeDateFilter.startDate && !activeDateFilter.endDate) {
            return baseSearch;
        }

        // Start with the user's search term
        let searchTerm = baseSearch;

        // Add date filters
        if (activeDateFilter.startDate) {
            searchTerm = `__ds:${activeDateFilter.startDate} ${searchTerm}`;
        }

        if (activeDateFilter.endDate) {
            searchTerm = `__de:${activeDateFilter.endDate} ${searchTerm}`;
        }

        return searchTerm;
    }

    document.addEventListener("DOMContentLoaded", function () {
        const toggleBtn = document.getElementById("toggle-group-btn");

        toggleBtn.addEventListener("click", function () {
            showGrouped = !showGrouped;
            toggleBtn.textContent = showGrouped
                ? "Show Items (Grouped by BatchNo)"
                : "Show Items (Ungrouped)";
            receivesTable.ajax.reload();
        });

        if (document.getElementById("receives-table")) {
            initReceivesTable();
        }

        // Add some CSS for grouped rows
        const style = document.createElement('style');
        style.textContent = `
            .grouped-row {
                background-color: ${isDarkMode ? '#2d3748' : '#f3f4f6'};
            }
            .grouped-row:hover {
                background-color: ${isDarkMode ? '#4a5568' : '#e5e7eb'} !important;
            }
            .badge {
                padding: 0.25rem 0.5rem;
                border-radius: 9999px;
                font-size: 0.75rem;
                font-weight: 600;
            }
            .badge-info {
                background-color: #3182ce;
                color: white;
            }
        `;
        document.head.appendChild(style);
    });
})();