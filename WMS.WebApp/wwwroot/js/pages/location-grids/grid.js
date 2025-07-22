// Location Grid Module - Updated with Print Functionality and Unlimited Modal
(function () {
    // Private variables
    let gridData = null;
    let currentZoneId = null;
    let currentZoom = 1;
    let filteredLocations = [];
    let searchTimeout = null;

    // Location linking variables
    let currentLocationId = null;
    let availableLinkableItems = [];
    let selectedItemIds = new Set();
    let currentLocationCapacity = { current: 0, max: 0 };

    // Unlimited modal variables
    let unlimitedCurrentItems = [];
    let unlimitedCurrentPage = 1;
    let unlimitedTotalPages = 1;
    let unlimitedPageSize = 10;
    let unlimitedSearchTimeout = null;
    let unlimitedTotalItems = 0;
    let unlimitedAvailableItems = [];
    let unlimitedSelectedItemIds = new Set();
    let unlimitedAvailableSearchTimeout = null;

    // Initialize module
    function init() {
        document.addEventListener('DOMContentLoaded', function () {
            setupEventHandlers();
            loadInitialData();
        });
    }

    // Show loading state
    function showLoading() {
        $('#loadingState').removeClass('hidden');
        $('#gridContainer').addClass('hidden');
        $('#noDataState').addClass('hidden');
    }

    // Hide loading state and show grid
    function showGrid() {
        $('#loadingState').addClass('hidden');
        $('#gridContainer').removeClass('hidden');
        $('#noDataState').addClass('hidden');

        $('#locationGrid').show();
        $('#queueLocationGrid').hide();
        $('#filterAndZoomRowDiv').show();
    }

    // Show no data state
    function showNoData() {
        $('#loadingState').addClass('hidden');
        $('#gridContainer').addClass('hidden');
        $('#noDataState').removeClass('hidden');
    }

    // Hide loading (generic function)
    function hideLoading() {
        $('#loadingState').addClass('hidden');
    }

    // Show error
    function showError(message) {
        console.error('Grid error:', message);
        if (typeof showErrorToast === 'function') {
            showErrorToast(message);
        } else {
            alert(message);
        }
        showNoData();
    }

    // Setup event handlers
    function setupEventHandlers() {
        // Zone selector
        $('#zoneSelector').on('change', function () {
            const zoneId = $(this).val();
            if (zoneId) {
                currentZoneId = zoneId;
                $('#currentZoneId').val(zoneId);
                loadLocationData(zoneId);
            }
        });

        // Search functionality
        $('#locationSearch').on('input', function () {
            clearTimeout(searchTimeout);
            const searchTerm = $(this).val().trim();

            searchTimeout = setTimeout(() => {
                handleSearch(searchTerm);
            }, 300);
        });

        // Status filter
        $('#statusFilter').on('change', function () {
            applyFilters();
        });

        // Row filter
        $('#rowFilter').on('change', function () {
            applyFilters();
        });

        // Zoom controls
        $('#zoomIn').on('click', function () {
            zoomGrid(currentZoom + 0.2);
        });

        $('#zoomOut').on('click', function () {
            zoomGrid(Math.max(0.5, currentZoom - 0.2));
        });

        $('#zoomReset').on('click', function () {
            zoomGrid(1);
        });

        // Modal close
        $('#closeModal').on('click', function (e) {
            hideLocationModal();
        });

        // Escape key to close modals
        $(document).on('keydown', function (e) {
            if (e.key === 'Escape') {
                hideLocationModal();
                hidePrintOptionsModal();
                hideUnlimitedLocationModal();
            }
        });

        // Print Layout functionality
        $('#printLayoutBtn').on('click', function () {
            showPrintOptionsModal();
        });

        $('#closePrintModal, #cancelPrintBtn').on('click', function () {
            hidePrintOptionsModal();
        });

        $('#generatePrintBtn').on('click', function () {
            generateLayoutPDF();
        });

        setupLinkingEventHandlers();
        setupUnlimitedModalEventHandlers();
    }

    // Show print options modal
    function showPrintOptionsModal() {
        if (!currentZoneId) {
            if (typeof showWarningToast === 'function') {
                showWarningToast('Please select a zone first');
            } else {
                alert('Please select a zone first');
            }
            return;
        }

        // Get current zone name
        const selectedZone = $('#zoneSelector option:selected').text();
        $('#printZoneName').text(selectedZone);

        // Update current filters display
        updateCurrentFiltersDisplay();

        $('#printOptionsModal').removeClass('hidden');
        $('body').addClass('overflow-hidden');
    }

    // Hide print options modal
    function hidePrintOptionsModal() {
        $('#printOptionsModal').addClass('hidden');
        $('body').removeClass('overflow-hidden');
    }

    // Update current filters display
    function updateCurrentFiltersDisplay() {
        const statusFilter = $('#statusFilter').val();
        const rowFilter = $('#rowFilter').val();
        const searchTerm = $('#locationSearch').val().trim();

        $('#filterStatusDisplay').text(statusFilter === 'all' ? 'All Status' :
            statusFilter === 'available' ? 'Available Only' : (statusFilter === 'partial' ? 'Partial Only' : 'Occupied Only'));

        $('#filterRowDisplay').text(rowFilter === 'all' ? 'All Rows' : `Row ${rowFilter}`);

        $('#filterSearchDisplay').text(searchTerm || 'None');
    }

    // Generate layout PDF
    function generateLayoutPDF() {
        const printOption = $('input[name="printOption"]:checked').val();
        const includeAllLocations = printOption === 'all';

        // Show loading state
        const generateBtn = $('#generatePrintBtn');
        const originalText = generateBtn.html();
        generateBtn.prop('disabled', true).html(`
            <iconify-icon icon="heroicons:arrow-path" class="animate-spin mr-2"></iconify-icon>
            Generating PDF...
        `);

        const requestData = {
            zoneId: currentZoneId,
            includeAllLocations: includeAllLocations,
            statusFilter: includeAllLocations ? null : $('#statusFilter').val(),
            rowFilter: includeAllLocations ? null : $('#rowFilter').val(),
            searchTerm: includeAllLocations ? null : $('#locationSearch').val().trim()
        };

        $.ajax({
            url: '/LocationGrid/GenerateLayoutPDF',
            type: 'POST',
            data: requestData,
            headers: {
                'X-CSRF-TOKEN': $('meta[name="csrf-token"]').attr('content')
            },
            xhrFields: {
                responseType: 'blob'
            },
            success: function (data, status, xhr) {
                // Create download link
                const blob = new Blob([data], { type: 'application/pdf' });
                const url = window.URL.createObjectURL(blob);
                const link = document.createElement('a');
                link.href = url;

                // Get filename from response headers or use default
                const zoneName = $('#zoneSelector option:selected').text().trim().replace(/[^a-zA-Z0-9]/g, '_');
                const now = new Date();
                const timestamp = now.getFullYear() +
                    String(now.getMonth() + 1).padStart(2, '0') +
                    String(now.getDate()).padStart(2, '0') + '_' +
                    String(now.getHours()).padStart(2, '0') +
                    String(now.getMinutes()).padStart(2, '0') +
                    String(now.getSeconds()).padStart(2, '0');
                link.download = `LocationGridLayout_${zoneName}_${timestamp}.pdf`;

                document.body.appendChild(link);
                link.click();
                document.body.removeChild(link);
                window.URL.revokeObjectURL(url);

                // Hide modal and show success
                hidePrintOptionsModal();
                if (typeof showSuccessToast === 'function') {
                    showSuccessToast('Layout PDF generated successfully');
                } else {
                    alert('Layout PDF generated successfully');
                }
            },
            error: function (xhr) {
                console.error('Error generating PDF:', xhr);
                let errorMessage = 'Failed to generate PDF';

                if (xhr.responseJSON && xhr.responseJSON.message) {
                    errorMessage = xhr.responseJSON.message;
                } else if (xhr.status === 404) {
                    errorMessage = 'PDF generation service not found';
                } else if (xhr.status === 500) {
                    errorMessage = 'Server error occurred while generating PDF';
                }

                if (typeof showErrorToast === 'function') {
                    showErrorToast(errorMessage);
                } else {
                    alert(errorMessage);
                }
            },
            complete: function () {
                // Restore button state
                generateBtn.prop('disabled', false).html(originalText);
            }
        });
    }

    // Load initial data
    function loadInitialData() {
        const zoneId = $('#currentZoneId').val();
        if (zoneId && zoneId !== '00000000-0000-0000-0000-000000000000') {
            currentZoneId = zoneId;
            loadLocationData(zoneId);
        }
    }

    // Load location data from server
    function loadLocationData(zoneId) {
        showLoading();

        $.ajax({
            url: `/LocationGrid/GetLocationData`,
            type: 'GET',
            data: { zoneId: zoneId },
            success: function (data) {
                if (data.error) {
                    showError(data.error);
                    return;
                }

                gridData = data;
                filteredLocations = data.locations || [];
                buildRowFilter();
                renderGrid();
            },
            error: function (xhr) {
                console.error('Error loading location data:', xhr);
                showError('Failed to load location data');
            }
        });
    }

    // Build row filter dropdown
    function buildRowFilter() {
        const rowFilter = $('#rowFilter');
        const currentValue = rowFilter.val();

        // Clear existing options except "All Rows"
        rowFilter.find('option:not(:first)').remove();

        if (!gridData || !gridData.locations || !gridData.locations.length) return;

        // Get unique rows and sort them
        const rows = [...new Set(gridData.locations.map(l => l.row).filter(r => r))].sort();

        rows.forEach(row => {
            rowFilter.append(`<option value="${row}">Row ${row}</option>`);
        });

        // Restore selection if still valid
        if (currentValue && currentValue !== 'all') {
            rowFilter.val(currentValue);
        }
    }

    // Apply filters
    function applyFilters() {
        if (!gridData || !gridData.locations) return;

        const statusFilter = $('#statusFilter').val();
        const rowFilter = $('#rowFilter').val();

        filteredLocations = gridData.locations.filter(location => {
            // Status filter
            if (statusFilter !== 'all') {
                const status = location.statusName.toLowerCase();
                if (status !== statusFilter) return false;
            }

            // Row filter
            if (rowFilter !== 'all' && location.row !== rowFilter) {
                return false;
            }

            return true;
        });

        renderGrid();
    }

    // Handle search
    function handleSearch(searchTerm) {
        // Remove previous highlights
        $('.location-cell').removeClass('highlighted');

        if (!searchTerm || !gridData || !gridData.locations) return;

        // Find matching locations
        const matches = gridData.locations.filter(location =>
            location.code.toLowerCase().includes(searchTerm.toLowerCase()) ||
            location.name.toLowerCase().includes(searchTerm.toLowerCase())
        );

        // Highlight matches
        matches.forEach(location => {
            const cell = $(`.location-cell[data-location-id="${location.id}"]`);
            cell.addClass('highlighted');
        });

        // If single match, scroll to it
        if (matches.length === 1) {
            scrollToLocation(matches[0].id);
        }

        // Show search results count
        if (matches.length > 0) {
            if (typeof showInfoToast === 'function') {
                showInfoToast(`Found ${matches.length} location(s) matching "${searchTerm}"`);
            }
        } else {
            if (typeof showWarningToast === 'function') {
                showWarningToast(`No locations found matching "${searchTerm}"`);
            }
        }
    }

    // Scroll to specific location
    function scrollToLocation(locationId) {
        const cell = $(`.location-cell[data-location-id="${locationId}"]`);
        if (cell.length) {
            const container = $('#gridContainer .overflow-auto');
            const cellOffset = cell.offset();
            const containerOffset = container.offset();

            if (cellOffset && containerOffset) {
                container.scrollTop(
                    container.scrollTop() + cellOffset.top - containerOffset.top - 100
                );
                container.scrollLeft(
                    container.scrollLeft() + cellOffset.left - containerOffset.left - 100
                );
            }
        }
    }

    // Render the grid
    function renderGrid() {
        if (!gridData || !filteredLocations || !filteredLocations.length) {
            showNoData();
            return;
        }

        if (gridData && gridData.zone) {
            const zoneName = gridData.zone.name.toUpperCase();

            if (zoneName.includes('QUEUE')) {
                renderQueueGrid();
                return;
            } else if (zoneName.includes('REFRIGERATED')) {
                renderRefrigeratedGrid();
                return;
            }
        }

        const container = $('#locationGrid');

        // Group locations by row and bay for easier rendering
        const locationMap = new Map();
        filteredLocations.forEach(location => {
            const key = `${location.row}-${location.bay}`;
            if (!locationMap.has(key)) {
                locationMap.set(key, []);
            }
            locationMap.get(key).push(location);
        });

        // Calculate grid dimensions - hardcoded to P (26 rows) for now
        const maxRowLetter = 'P'; // Hardcoded first
        const maxBay = filteredLocations.length > 0 ? Math.max(...filteredLocations.map(l => l.bay)) : 16;
        const maxLevel = 5; // Fixed as per requirements

        // Build grid HTML
        let gridHTML = buildGridStructure(locationMap, maxRowLetter, maxBay, maxLevel);

        container.html(gridHTML);

        // Setup event handlers for location cells
        setupLocationCellHandlers();

        // Show grid
        showGrid();
    }

    // Convert row letter to number for grid calculations
    function rowLetterToNumber(rowLetter) {
        return rowLetter.charCodeAt(0) - 64; // A=1, B=2, etc.
    }

    // Build the grid structure
    function buildGridStructure(locationMap, maxRowLetter, maxBay, maxLevel) {
        let html = '<div class="grid-wrapper">';

        // Configuration for row grouping and spacing - EASY TO MODIFY HERE
        const rowGroups = [
            ['P'],           // Group 1: P alone
            ['O', 'N'],      // Group 2: O, N together  
            ['M', 'L'],      // Group 3: M, L together
            ['K', 'J'],      // Group 4: K, J together
            ['I', 'H'],      // Group 5: I, H together
            ['G', 'F'],      // Group 6: G, F together
            ['E', 'D'],      // Group 7: E, D together
            ['C', 'B'],      // Group 8: C, B together
            ['A']            // Group 9: A alone
        ];

        // Build the row order with spaces automatically
        const rowOrder = [];
        rowGroups.forEach((group, index) => {
            rowOrder.push(...group);
            // Add space after each group except the last one
            if (index < rowGroups.length - 1) {
                rowOrder.push(''); // Empty string = spacer
            }
        });

        // Calculate grid columns dynamically
        const totalCols = rowOrder.length + 1; // +1 for Bay column
        const gridCols = rowOrder.map(row => row === '' ? '10px' : '60px').join(' ') + ' 60px';

        // Calculate dimensions
        const totalRows = 1 + (maxBay * 6); // 1 header + (5 levels + 1 separator) per bay

        html += `<div class="location-grid-table" style="
        display: grid; 
        grid-template-columns: ${gridCols}; 
        grid-template-rows: 40px repeat(${totalRows - 1}, 40px);
        gap: 1px;
        position: relative;
    ">`;

        // Build header row
        rowOrder.forEach(rowLetter => {
            if (rowLetter === '') {
                html += '<div class="row-spacer"></div>';
            } else {
                html += `<div class="row-header sticky-header">${rowLetter}</div>`;
            }
        });
        html += '<div class="grid-corner sticky-header">Bay</div>';

        // Build bay groups
        for (let bay = 1; bay <= maxBay; bay++) {
            for (let level = maxLevel; level >= 1; level--) {
                // Build cells for each position
                rowOrder.forEach(rowLetter => {
                    if (rowLetter === '') {
                        html += '<div class="location-spacer"></div>';
                    } else {
                        const location = findLocation(locationMap, rowLetter, bay, level);
                        html += buildLocationCell(location, rowLetter, bay, level);
                    }
                });

                // Add bay header for first level only
                if (level === maxLevel) {
                    html += `<div class="bay-number-header sticky-right" style="
                    grid-row: span ${maxLevel}; 
                    writing-mode: vertical-rl; 
                    text-orientation: mixed;
                    display: flex;
                    align-items: center;
                    justify-content: center;">
                    <div class="bay-bracket-right">
                        <span class="bay-number">${bay.toString().padStart(2, '0')}</span>
                    </div>
                 </div>`;
                }
            }

            // Add separator row between bays
            if (bay < maxBay) {
                rowOrder.forEach(rowLetter => {
                    html += rowLetter === '' ? '<div class="bay-separator-spacer"></div>' : '<div class="bay-separator"></div>';
                });
                html += '<div class="bay-separator sticky-right"></div>';
            }
        }

        html += '</div></div>';
        return html;
    }

    // Find location by coordinates
    function findLocation(locationMap, rowLetter, bay, level) {
        const key = `${rowLetter}-${bay}`;
        const locations = locationMap.get(key) || [];
        return locations.find(l => l.level === level);
    }

    // Build individual location cell
    function buildLocationCell(location, rowLetter, bay, level) {
        if (!location) {
            // Empty cell - NO inline styles, let CSS handle ALL styling including text color
            return `<div class="location-cell empty">
                    <span class="empty-text">-</span>
                </div>`;
        }

        const status = location.statusName;
        const statusClass = getStatusClass(status);

        return `<div class="location-cell ${statusClass}" 
                 data-location-id="${location.id}"
                 data-location-code="${location.code}"
                 data-location-barcode="${location.barcode}"
                 data-row="${rowLetter}" 
                 data-bay="${bay}" 
                 data-level="${level}"
                 data-status="${status}"
                 data-inventory-count="${location.inventoryCount || 0}"
                 data-total-quantity="${location.totalQuantity || 0}">
                <span class="location-code">${location.barcode}</span>
            </div>`;
    }

    // Get CSS class for status
    function getStatusClass(status) {
        switch (status) {
            case 'Available': return 'available';
            case 'Partial': return 'partial';
            case 'Occupied': return 'occupied';
            case 'Reserved': return 'reserved';
            case 'Maintenance': return 'maintenance';
            case 'Blocked': return 'blocked';
            default: return 'available';
        }
    }

    // Setup location cell event handlers
    function setupLocationCellHandlers() {
        // Click handler
        $('.location-cell').off('click').on('click', function () {
            const locationId = $(this).data('location-id');
            if (locationId) {
                showLocationDetails(locationId);
            }
        });

        // Hover handlers for tooltip
        $('.location-cell').off('mouseenter mouseleave')
            .on('mouseenter', function (e) {
                const locationData = {
                    id: $(this).data('location-id'),
                    code: $(this).data('location-code'),
                    barcode: $(this).data('location-barcode'),
                    status: $(this).attr('data-status'),
                    inventoryCount: $(this).attr('data-inventory-count'),
                    totalQuantity: $(this).attr('data-total-quantity')
                };
                showTooltip(e, locationData);
            })
            .on('mouseleave', function () {
                hideTooltip();
            })
            .on('mousemove', function (e) {
                updateTooltipPositionFixed(e);
            });
    }

    // Render location modal content
    function renderLocationModal(data) {
        $('#modalLocationCode').text(data.barcode || 'Unknown Location');
        $('#modalLocationName').text(data.name || 'Location Details');

        const utilizationPercent = data.maxItems > 0 ?
            Math.round((data.currentItems / data.maxItems) * 100) : 0;
        const statusText = utilizationPercent == 0 ? 'Available' : (utilizationPercent == 100 ? 'Occupied' : 'Partial');

        const modalContent = `
            <div class="grid grid-cols-1 md:grid-cols-2 gap-6">
                <!-- Location Info -->
                <div class="space-y-4">
                    <div class="bg-gray-50 dark:bg-gray-700 rounded-lg p-4">
                        <h4 class="font-semibold text-gray-900 dark:text-white mb-3">Location Information</h4>
                        <div class="space-y-2 text-sm">
                            <div class="flex justify-between">
                                <span class="text-gray-600 dark:text-gray-400">Zone:</span>
                                <span class="font-medium">${data.zoneName || 'Unknown'}</span>
                            </div>
                            <div class="flex justify-between">
                                <span class="text-gray-600 dark:text-gray-400">Position:</span>
                                <span class="font-medium">Row ${data.row || '?'}, Bay ${(data.bay || 0).toString().padStart(2, '0')}, Level ${data.level || '?'}</span>
                            </div>
                            <div class="flex justify-between">
                                <span class="text-gray-600 dark:text-gray-400">Status:</span>
                                <span class="inline-flex px-2 py-1 text-xs font-medium rounded-full ${data.isEmpty ? 'bg-green-100 text-green-800' : 'bg-red-100 text-red-800'}">
                                    ${statusText}
                                </span>
                            </div>
                        </div>
                    </div>

                    <!-- Capacity Info -->
                    <div class="bg-gray-50 dark:bg-gray-700 rounded-lg p-4">
                        <h4 class="font-semibold text-gray-900 dark:text-white mb-3">Capacity</h4>
                        <div class="space-y-3">
                            <div>
                                <div class="flex justify-between text-sm mb-1">
                                    <span>Items: ${data.currentItems || 0} / ${data.maxItems || 0}</span>
                                    <span>${utilizationPercent}%</span>
                                </div>
                                <div class="w-full bg-gray-200 rounded-full h-2">
                                    <div class="bg-blue-600 h-2 rounded-full" style="width: ${utilizationPercent}%"></div>
                                </div>
                            </div>
                            <div class="grid grid-cols-2 gap-4 text-sm hidden">
                                <div>
                                    <span class="text-gray-600 dark:text-gray-400">Weight:</span>
                                    <div class="font-medium">${(data.currentWeight || 0).toFixed(2)} / ${data.maxWeight || 0} kg</div>
                                </div>
                                <div>
                                    <span class="text-gray-600 dark:text-gray-400">Volume:</span>
                                    <div class="font-medium">${(data.currentVolume || 0).toFixed(2)} / ${data.maxVolume || 0} m³</div>
                                </div>
                            </div>
                        </div>
                    </div>
                </div>

                <!-- Inventory List -->
                <div>
                    <h4 class="font-semibold text-gray-900 dark:text-white mb-3">Inventory Items (${(data.inventories || []).length})</h4>
                    <div class="max-h-96 overflow-y-auto space-y-2">
                        ${(data.inventories || []).length > 0 ?
                (data.inventories || []).map(item => `
                                <div class="bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-600 rounded-lg p-3">
                                    <div class="flex justify-between items-start">
                                        <div class="flex-1">
                                            <h5 class="font-medium text-gray-900 dark:text-white">${item.productName || 'Unknown Product'}</h5>
                                            <p class="text-sm text-gray-600 dark:text-gray-400">MHU: ${item.mainHU || 'N/A'}</p>
                                        </div>
                                        <span class="inline-flex px-2 py-1 text-xs font-medium dark:bg-gray-100 text-gray-800 rounded-full">
                                            ${item.quantity || 0} units
                                        </span>
                                    </div>
                                    <div class="mt-2 grid grid-cols-2 gap-2 text-xs text-gray-600 dark:text-gray-400">
                                        <div>Batch No: ${item.lotNumber || 'N/A'}</div>
                                        <div>Received: ${item.receivedDate ? new Date(item.receivedDate).toLocaleDateString() : 'N/A'}</div>
                                    </div>
                                </div>
                            `).join('') :
                `<div class="text-center py-8 text-gray-500">
                                <iconify-icon icon="heroicons:cube-transparent" class="text-4xl mb-2"></iconify-icon>
                                <p>No inventory items in this location</p>
                            </div>`
            }
                    </div>
                </div>
            </div>
        `;

        $('#modalContent').html(modalContent);
    }

    // Hide location modal
    function hideLocationModal() {
        $('#locationModal').addClass('hidden');
        $('body').removeClass('overflow-hidden');
    }

    // Show tooltip
    function showTooltip(event, locationData) {
        const tooltip = $('#locationTooltip');
        const content = $('#tooltipContent');

        if (!locationData.id) {
            content.html('<div class="text-center">Empty Location</div>');
        } else {
            const textStatusColor = locationData.status == 'Available' ? 'text-green-400' : (locationData.status == 'Partial' ? 'text-amber-400' : 'text-red-400');

            content.html(`
                <div class="space-y-1">
                    <div class="font-semibold">${locationData.barcode || 'Unknown'}</div>
                    <div class="text-xs">
                        Status: <span class="font-medium ${textStatusColor}">${locationData.status || 'unknown'}</span>
                    </div>
                    ${(locationData.inventoryCount || 0) > 0 ?
                    `<div class="text-xs">Items: ${locationData.inventoryCount}</div>` :
                    '<div class="text-xs text-gray-400">No inventory</div>'
                }
                    <div class="text-xs text-gray-400 mt-1">Click for details</div>
                </div>
            `);
        }

        updateTooltipPositionFixed(event);
        tooltip.removeClass('hidden');
    }

    // Hide tooltip
    function hideTooltip() {
        $('#locationTooltip').addClass('hidden');
    }

    // Zoom grid
    function zoomGrid(newZoom) {
        currentZoom = Math.max(0.5, Math.min(3, newZoom)); // Limit zoom between 0.5x and 3x
        $('#locationGrid').css('transform', `scale(${currentZoom})`);

        // Update zoom button states
        $('#zoomOut').prop('disabled', currentZoom <= 0.5);
        $('#zoomIn').prop('disabled', currentZoom >= 3);
    }

    // Initialize linking functionality 
    function setupLinkingEventHandlers() {
        // Search functionality
        $('#linkableItemsSearch').on('input', function () {
            const searchTerm = $(this).val().trim();
            filterLinkableItems();
        });

        // Client filter
        $('#linkableItemsClientFilter').on('change', function () {
            loadAvailableLinkableItems();
        });

        // Type filter
        $('#linkableItemsTypeFilter').on('change', function () {
            filterLinkableItems();
        });

        // Select all checkbox
        $('#selectAllLinkableItemsCheckbox').on('change', function () {
            const isChecked = $(this).is(':checked');
            const visibleCheckboxes = getVisibleLinkableItemCheckboxes();

            visibleCheckboxes.prop('checked', isChecked);

            // Update selected items set
            visibleCheckboxes.each(function () {
                const itemId = $(this).data('item-id');
                if (isChecked) {
                    selectedItemIds.add(itemId);
                } else {
                    selectedItemIds.delete(itemId);
                }
            });

            updateLinkingUI();
        });

        // Select all button
        $('#selectAllLinkableItems').on('click', function () {
            $('#selectAllLinkableItemsCheckbox').prop('checked', true).trigger('change');
        });

        // Deselect all button
        $('#deselectAllLinkableItems').on('click', function () {
            $('#selectAllLinkableItemsCheckbox').prop('checked', false).trigger('change');
        });

        // Link selected items button
        $('#linkSelectedItems').on('click', function () {
            if (selectedItemIds.size > 0 && !$(this).prop('disabled')) {
                linkSelectedItemsToLocation();
            }
        });

        // Individual item checkbox change
        $(document).on('change', '.linkable-item-checkbox', function () {
            const itemId = $(this).data('item-id');
            const isChecked = $(this).is(':checked');

            if (isChecked) {
                selectedItemIds.add(itemId);
            } else {
                selectedItemIds.delete(itemId);
            }

            updateLinkingUI();
        });
    }

    // Updated showLocationDetails function to include linking functionality
    function showLocationDetails(locationId) {
        currentLocationId = locationId;
        selectedItemIds.clear();

        $.ajax({
            url: `/LocationGrid/GetLocationDetails`,
            type: 'GET',
            data: { locationId: locationId },
            success: function (data) {
                if (data.error) {
                    showErrorToast(data.error);
                    return;
                }

                renderLocationModal(data);
                $('#locationModal').removeClass('hidden');
                $('body').addClass('overflow-hidden');

                // Load available linkable items
                loadAvailableLinkableItems();
            },
            error: function (xhr) {
                console.error('Error loading location details:', xhr);
                showErrorToast('Failed to load location details');
            }
        });
    }

    // Load available linkable items
    function loadAvailableLinkableItems() {
        if (!currentLocationId) return;

        const searchTerm = $('#linkableItemsSearch').val().trim();
        const clientId = $('#linkableItemsClientFilter').val();
        const itemType = $('#linkableItemsTypeFilter').val();

        // Show loading state
        $('#linkableItemsTableBody').html(`
        <tr>
            <td colspan="6" class="px-6 py-8 text-center text-gray-500 dark:text-gray-400">
                <iconify-icon icon="heroicons:arrow-path" class="animate-spin text-2xl mb-2"></iconify-icon>
                <div>Loading available items...</div>
            </td>
        </tr>
    `);

        const params = {
            locationId: currentLocationId,
            searchTerm: searchTerm || null,
            clientId: clientId || null,
            itemType: itemType || null
        };

        $.ajax({
            url: '/LocationGrid/GetAvailableLinkableItems',
            type: 'GET',
            data: params,
            success: function (response) {
                if (response.error) {
                    showErrorToast(response.error);
                    return;
                }

                availableLinkableItems = response.availableItems || [];
                currentLocationCapacity = {
                    current: response.currentLocationItemCount,
                    max: response.maxItems,
                    available: response.availableCapacity
                };

                // Populate client filter
                populateClientFilter(response.availableClients || []);

                // Update capacity info
                updateCapacityInfo();

                // Render items table
                renderLinkableItemsTable();

                // Update UI state
                updateLinkingUI();
            },
            error: function (xhr) {
                console.error('Error loading available items:', xhr);
                showErrorToast('Failed to load available items');
                $('#linkableItemsTableBody').html(`
                <tr>
                    <td colspan="6" class="px-6 py-8 text-center text-red-500">
                        <iconify-icon icon="heroicons:exclamation-triangle" class="text-2xl mb-2"></iconify-icon>
                        <div>Failed to load available items</div>
                    </td>
                </tr>
            `);
            }
        });
    }

    // Helper functions for linking functionality
    function populateClientFilter(clients) {
        const select = $('#linkableItemsClientFilter');
        const currentValue = select.val();

        select.html('<option value="">All Clients</option>');

        clients.forEach(client => {
            select.append(`<option value="${client.id}">${escapeHtml(client.name)}</option>`);
        });

        if (currentValue && clients.some(c => c.id === currentValue)) {
            select.val(currentValue);
        }
    }

    function updateCapacityInfo() {
        const capacityText = `${currentLocationCapacity.current}/${currentLocationCapacity.max}`;
        const availableText = `(${currentLocationCapacity.available} available)`;

        $('#modalCapacityInfo').html(`
        ${capacityText} 
        <span class="text-green-600 dark:text-green-400">${availableText}</span>
    `);
    }

    function renderLinkableItemsTable() {
        const tbody = $('#linkableItemsTableBody');

        if (!availableLinkableItems || availableLinkableItems.length === 0) {
            tbody.html(`
            <tr>
                <td colspan="6" class="px-6 py-8 text-center text-gray-500 dark:text-gray-400">
                    <iconify-icon icon="heroicons:cube-transparent" class="text-4xl mb-2"></iconify-icon>
                    <div>No items available for linking</div>
                </td>
            </tr>
        `);
            return;
        }

        let html = '';
        availableLinkableItems.forEach(item => {
            const isSelected = selectedItemIds.has(item.id);
            const typeDisplay = getItemTypeDisplay(item.type);
            const receiveDate = new Date(item.receiveDate).toLocaleDateString();

            html += `
            <tr class="linkable-item-row hover:bg-gray-50 dark:hover:bg-gray-700" 
                data-item-type="${item.type}">
                <td class="px-3 py-2">
                    <input type="checkbox" 
                           class="linkable-item-checkbox form-checkbox h-4 w-4 text-blue-600" 
                           data-item-id="${item.id}"
                           data-item-type="${item.type}"
                           ${isSelected ? 'checked' : ''}>
                </td>
                <td class="px-3 py-2">
                    <span class="inline-flex items-center px-2 py-1 rounded-full text-xs font-medium ${getItemTypeBadgeClass(item.type)}">
                        ${escapeHtml(typeDisplay)}
                    </span>
                </td>
                <td class="px-3 py-2">
                    <div class="text-sm font-medium text-gray-900 dark:text-white" title="${escapeHtml(item.displayName)}">
                        ${truncateText(escapeHtml(item.displayName), 30)}
                    </div>
                    ${item.additionalInfo ? `<div class="text-xs text-gray-500">${escapeHtml(item.additionalInfo)}</div>` : ''}
                </td>
                <td class="px-3 py-2">
                    <span class="text-sm font-mono text-gray-600 dark:text-gray-400">
                        ${escapeHtml(item.skuCode)}
                    </span>
                </td>
                <td class="px-3 py-2">
                    <span class="text-sm text-gray-600 dark:text-gray-400">
                        ${escapeHtml(item.clientName)}
                    </span>
                </td>
                <td class="px-3 py-2">
                    <span class="text-sm text-gray-600 dark:text-gray-400">
                        ${receiveDate}
                    </span>
                </td>
                <td class="px-3 py-2">
                    <span class="text-sm text-gray-600 dark:text-gray-400">
                        ${escapeHtml(item.locationAndZoneName)}
                    </span>
                </td>
            </tr>
        `;
        });

        tbody.html(html);
    }

    function filterLinkableItems() {
        const searchTerm = $('#linkableItemsSearch').val().toLowerCase().trim();
        const typeFilter = $('#linkableItemsTypeFilter').val();

        $('.linkable-item-row').each(function () {
            const row = $(this);
            let showRow = true;

            if (typeFilter) {
                const itemType = row.data('item-type').toString();
                if (itemType !== typeFilter) {
                    showRow = false;
                }
            }

            if (showRow && searchTerm) {
                const text = row.text().toLowerCase();
                if (!text.includes(searchTerm)) {
                    showRow = false;
                }
            }

            if (showRow) {
                row.show().removeAttr('data-filtered');
            } else {
                row.hide().attr('data-filtered', 'hidden');
            }
        });

        updateLinkingUI();
    }

    function getVisibleLinkableItemCheckboxes() {
        return $('.linkable-item-row:not([data-filtered="hidden"]) .linkable-item-checkbox');
    }

    function updateLinkingUI() {
        const visibleCheckboxes = getVisibleLinkableItemCheckboxes();
        const checkedVisible = visibleCheckboxes.filter(':checked').length;
        const totalVisible = visibleCheckboxes.length;
        const selectedCount = selectedItemIds.size;

        const headerCheckbox = $('#selectAllLinkableItemsCheckbox');
        if (totalVisible === 0) {
            headerCheckbox.prop('indeterminate', false).prop('checked', false);
        } else if (checkedVisible === 0) {
            headerCheckbox.prop('indeterminate', false).prop('checked', false);
        } else if (checkedVisible === totalVisible) {
            headerCheckbox.prop('indeterminate', false).prop('checked', true);
        } else {
            headerCheckbox.prop('indeterminate', true).prop('checked', false);
        }

        $('#selectedItemCount').text(selectedCount);

        const canLink = selectedCount > 0 && selectedCount <= currentLocationCapacity.available;
        $('#linkSelectedItems').prop('disabled', !canLink);

        if (selectedCount > currentLocationCapacity.available) {
            $('#linkSelectedItems').addClass('btn-warning').removeClass('btn-success')
                .find('span').text(`${selectedCount} (Exceeds capacity!)`);
        } else {
            $('#linkSelectedItems').removeClass('btn-warning').addClass('btn-success')
                .find('span').text(selectedCount);
        }
    }

    function linkSelectedItemsToLocation() {
        if (selectedItemIds.size === 0) return;

        const selectedItems = Array.from(selectedItemIds).map(itemId => {
            const checkbox = $(`.linkable-item-checkbox[data-item-id="${itemId}"]`);
            return {
                itemId: itemId,
                itemType: parseInt(checkbox.data('item-type'))
            };
        });

        const linkButton = $('#linkSelectedItems');
        const originalText = linkButton.html();
        linkButton.prop('disabled', true).html(`
        <iconify-icon icon="heroicons:arrow-path" class="animate-spin mr-2"></iconify-icon>
        Linking...
    `);

        $.ajax({
            url: '/LocationGrid/LinkItemsToLocation',
            type: 'POST',
            contentType: 'application/json',
            headers: {
                'X-CSRF-TOKEN': $('meta[name="csrf-token"]').attr('content'),
                'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val()
            },
            data: JSON.stringify({
                locationId: currentLocationId,
                items: selectedItems
            }),
            success: function (response) {
                if (response.success) {
                    showSuccessToast(`Successfully linked ${response.linkedItemsCount} items to location`);

                    currentLocationCapacity.current = response.newLocationItemCount;
                    currentLocationCapacity.available = currentLocationCapacity.max - currentLocationCapacity.current;

                    selectedItemIds.clear();
                    loadAvailableLinkableItems();
                    showLocationDetails(currentLocationId);
                    updateCapacityInfo();
                } else {
                    showErrorToast(response.message || 'Failed to link items');
                }
            },
            error: function (xhr) {
                console.error('Error linking items:', xhr);
                let errorMessage = 'Failed to link items to location';

                if (xhr.responseJSON && xhr.responseJSON.message) {
                    errorMessage = xhr.responseJSON.message;
                }

                showErrorToast(errorMessage);
            },
            complete: function () {
                linkButton.prop('disabled', false).html(originalText);
            }
        });
    }

    // Helper functions
    function getItemTypeDisplay(type) {
        switch (parseInt(type)) {
            case 1: return 'Inventory';
            case 2: return 'GIV FG Pallet';
            case 3: return 'GIV RM Pallet';
            default: return 'Unknown';
        }
    }

    function getItemTypeBadgeClass(type) {
        switch (parseInt(type)) {
            case 1: return 'bg-blue-100 text-blue-800 dark:bg-blue-900/25 dark:text-blue-400';
            case 2: return 'bg-green-100 text-green-800 dark:bg-green-900/25 dark:text-green-400';
            case 3: return 'bg-purple-100 text-purple-800 dark:bg-purple-900/25 dark:text-purple-400';
            default: return 'bg-gray-100 text-gray-800 dark:bg-gray-900/25 dark:text-gray-400';
        }
    }

    function truncateText(text, maxLength) {
        if (text.length <= maxLength) return text;
        return text.substring(0, maxLength) + '...';
    }

    function updateTooltipPositionFixed(event) {
        const tooltip = $('#locationTooltip');
        const cell = $(event.target).closest('.location-cell');

        if (!cell.length) return;

        const cellRect = cell[0].getBoundingClientRect();
        const tooltipX = cellRect.right + 10;
        const tooltipY = cellRect.top + (cellRect.height / 2) - (tooltip.outerHeight() / 2);

        const viewportWidth = window.innerWidth;
        const viewportHeight = window.innerHeight;
        const tooltipWidth = tooltip.outerWidth();
        const tooltipHeight = tooltip.outerHeight();

        let finalX = tooltipX;
        let finalY = tooltipY;

        if (finalX + tooltipWidth > viewportWidth) {
            finalX = cellRect.left - tooltipWidth - 10;
        }

        if (finalY < 10) finalY = 10;
        if (finalY + tooltipHeight > viewportHeight - 10) {
            finalY = viewportHeight - tooltipHeight - 10;
        }

        tooltip.css({
            left: finalX - 100 + 'px',
            top: finalY - 65 + 'px',
            position: 'fixed'
        });
    }

    // ===== QUEUE ZONE GRID FUNCTIONS =====
    function renderQueueGrid() {
        const container = $('#queueLocationGrid');

        if (!gridData || !filteredLocations || !filteredLocations.length) {
            showNoData();
            return;
        }

        // Build queue grid HTML
        let gridHTML = buildQueueGridStructure(filteredLocations, gridData.zone);

        container.html(gridHTML);

        // Setup event handlers for queue location cells
        setupQueueLocationCellHandlers();

        // Show queue grid, hide racking grid
        showQueueGrid();
    }

    // Build Queue Grid Structure
    function buildQueueGridStructure(locations, zoneInfo) {
        // Create location map by code for easy lookup
        const locationMap = new Map();
        locations.forEach(location => {
            locationMap.set(location.code.toUpperCase(), location);
        });

        let html = '<div class="queue-grid-wrapper">';
        html += '<div class="queue-warehouse-layout queue-grid-fade-in">';

        // Left Stack Container for Q6, Q5, Q4 ONLY (no Q3!)
        html += '<div class="queue-left-stack">';
        html += buildQueueLocationCell(locationMap.get('Q6'), 'Q6');
        html += buildQueueLocationCell(locationMap.get('Q5'), 'Q5');
        html += buildQueueLocationCell(locationMap.get('Q4'), 'Q4');
        html += '</div>';

        // Q7 Position
        html += '<div class="queue-q7-position">';
        html += buildQueueLocationCell(locationMap.get('Q7'), 'Q7');
        html += '</div>';

        // Racking Zone
        html += '<div class="queue-racking-position">';
        html += buildQueueStaticZone('RACKING', 'RACKING ZONE', 'Switch to Racking layout');
        html += '</div>';

        // Q3 Position (separate from left stack)
        html += '<div class="queue-q3-position">';
        html += buildQueueLocationCell(locationMap.get('Q3'), 'Q3');
        html += '</div>';

        // Empty areas for grid alignment
        html += '<div class="queue-empty-area"></div>';

        // Q2 Position
        html += '<div class="queue-q2-position">';
        html += buildQueueLocationCell(locationMap.get('Q2'), 'Q2');
        html += '</div>';

        // Empty area 2
        html += '<div class="queue-empty-area2"></div>';

        // Q1 Staging Area
        html += '<div class="queue-staging-position">';
        const q1Location = locationMap.get('Q1') || locationMap.get('STAGING AREA (Q1)');
        html += buildQueueStagingArea(q1Location);
        html += '</div>';

        // Spacers
        html += '<div class="queue-spacer-1"></div>';
        html += '<div class="queue-spacer-2"></div>';

        // Refrigerated Zone
        html += '<div class="queue-refrigerated-position">';
        html += buildQueueStaticZone('REFRIGERATED', 'REEFER ZONE', 'Switch to Refrigerated layout');
        html += '</div>';

        html += '</div></div>';
        return html;
    }

    // Build Individual Queue Location Cell
    function buildQueueLocationCell(location, queueCode) {
        if (!location) {
            // Empty placeholder for missing queue location
            return `<div class="queue-location available" 
                 data-location-code="${queueCode}"
                 data-status="available"
                 title="Queue Location ${queueCode} - No data">
                <span class="queue-location-code">${queueCode}</span>
            </div>`;
        }

        const status = location.statusName || 'available';
        const statusClass = getQueueStatusClass(status);

        return `<div class="queue-location ${statusClass}" 
             data-location-id="${location.id}"
             data-location-code="${location.code}"
             data-location-barcode="${location.barcode || location.code}"
             data-status="${status}"
             data-inventory-count="${location.inventoryCount || 0}"
             data-total-quantity="${location.totalQuantity || 0}"
             title="Queue Location ${location.code} - ${status}">
            <span class="queue-location-code">${location.name}</span>
        </div>`;
    }

    // Build Queue Staging Area (Q1)
    function buildQueueStagingArea(location) {
        if (!location) {
            return `<div class="queue-staging-area" 
                 data-location-code="Q1"
                 data-status="available"
                 title="Staging Area (Q1) - No data">
                Staging Area (Q1)
            </div>`;
        }

        const status = location.statusName || 'available';
        const statusClass = getQueueStatusClass(status);

        return `<div class="queue-staging-area ${statusClass}" 
             data-location-id="${location.id}"
             data-location-code="${location.code}"
             data-location-barcode="${location.barcode || location.code}"
             data-status="${status}"
             data-inventory-count="${location.inventoryCount || 0}"
             data-total-quantity="${location.totalQuantity || 0}"
             title="Staging Area (${location.code}) - ${status}">
            ${location.name}
        </div>`;
    }

    // Build Static Zone (Racking/Refrigerated)
    function buildQueueStaticZone(zoneType, zoneLabel, tooltip) {
        const zoneClass = zoneType.toLowerCase() === 'racking' ? 'queue-racking-zone' : 'queue-refrigerated-zone';

        return `<div class="${zoneClass}" 
             data-zone-type="${zoneType}"
             title="${tooltip}">
            ${zoneLabel}
            <div class="queue-zone-info">Click to switch layout</div>
        </div>`;
    }

    // Get CSS class for queue location status
    function getQueueStatusClass(status) {
        switch (status.toLowerCase()) {
            case 'available': return 'available';
            case 'partial': return 'partial';
            case 'occupied': return 'occupied';
            case 'reserved': return 'reserved';
            case 'maintenance': return 'maintenance';
            case 'blocked': return 'blocked';
            default: return 'available';
        }
    }

    // Setup Queue Location Cell Event Handlers
    function setupQueueLocationCellHandlers() {
        // Click handler for queue locations (Q1-Q7)
        $('.queue-location, .queue-staging-area').off('click').on('click', function () {
            const locationId = $(this).data('location-id');
            if (locationId) {
                showUnlimitedLocationModal(locationId);
            } else {
                // Handle missing location data
                const locationCode = $(this).data('location-code');
                if (typeof showWarningToast === 'function') {
                    showWarningToast(`No data available for ${locationCode}`);
                }
            }
        });

        // Hover handler for queue locations
        $('.queue-location, .queue-staging-area').off('mouseenter mouseleave')
            .on('mouseenter', function () {
                const locationCode = $(this).data('location-code');
                const status = $(this).data('status');
                const inventoryCount = $(this).data('inventory-count') || 0;
                const totalQuantity = $(this).data('total-quantity') || 0;

                // Show tooltip with location info
                const tooltipText = `${locationCode} - ${status}\nItems: ${inventoryCount}\nQuantity: ${totalQuantity}`;
                $(this).attr('title', tooltipText);
            });

        // Click handler for static zones (zone switching)
        $('.queue-racking-zone, .queue-refrigerated-zone').off('click').on('click', function () {
            const zoneType = $(this).data('zone-type');
            handleQueueZoneSwitching(zoneType);
        });

        // Hover handler for static zones
        $('.queue-racking-zone, .queue-refrigerated-zone').off('mouseenter')
            .on('mouseenter', function () {
                const zoneType = $(this).data('zone-type');
                $(this).attr('title', `${zoneType} Zone - Click to switch layout`);
            });
    }

    // Handle Zone Switching from Static Zones
    function handleQueueZoneSwitching(targetZoneType) {
        // Show confirmation dialog
        const zoneDisplayName = targetZoneType.charAt(0).toUpperCase() + targetZoneType.slice(1).toLowerCase();

        if (confirm(`Switch to ${zoneDisplayName} zone layout?`)) {
            // Find the target zone in the dropdown
            const $zoneSelector = $('#zoneSelector');
            const targetOption = $zoneSelector.find('option').filter(function () {
                return $(this).text().toUpperCase().includes(targetZoneType.toUpperCase());
            });

            if (targetOption.length > 0) {
                // Switch to target zone
                $zoneSelector.val(targetOption.val()).trigger('change');
            } else {
                // Zone not found in dropdown
                if (typeof showWarningToast === 'function') {
                    showWarningToast(`${zoneDisplayName} zone not available`);
                } else {
                    alert(`${zoneDisplayName} zone not available`);
                }
            }
        }
    }

    // Show Queue Grid (hide racking grid)
    function showQueueGrid() {
        $('#loadingState').addClass('hidden');
        $('#queueLocationGrid').removeClass('hidden');
        $('#locationGrid').addClass('hidden'); // Hide racking grid
        $('#gridContainer').removeClass('hidden');
        $('#noDataState').addClass('hidden');

        $('#queueLocationGrid').show();
        $('#locationGrid').hide();
        $('#filterAndZoomRowDiv').hide();
    }

    // ===== REFRIGERATED ZONE GRID FUNCTIONS =====

    // Render Refrigerated Grid (Q1-Q7 grey, Refrigerated zone green)
    function renderRefrigeratedGrid() {
        const container = $('#queueLocationGrid'); // Reuse same container

        // Build refrigerated grid HTML
        let gridHTML = buildRefrigeratedGridStructure(filteredLocations, gridData.zone);

        container.html(gridHTML);

        // Setup event handlers for refrigerated layout
        setupRefrigeratedLocationCellHandlers();

        // Show grid (same container as queue)
        showQueueGrid();
    }

    // Build Refrigerated Grid Structure
    function buildRefrigeratedGridStructure(locations, zoneInfo) {
        // Create location map by code for easy lookup
        const locationMap = new Map();
        locations.forEach(location => {
            locationMap.set(location.code.toUpperCase(), location);
        });

        let html = '<div class="queue-grid-wrapper">';
        html += '<div class="queue-warehouse-layout queue-grid-fade-in">';

        // Left Stack Container for Q6, Q5, Q4 (now grey/static)
        html += '<div class="queue-left-stack">';
        html += buildRefrigeratedLocationCell(locationMap.get('Q6'), 'QUEUE ZONE');
        html += buildRefrigeratedLocationCell(locationMap.get('Q5'), 'QUEUE ZONE');
        html += buildRefrigeratedLocationCell(locationMap.get('Q4'), 'QUEUE ZONE');
        html += '</div>';

        // Q7 Position (grey/static)
        html += '<div class="queue-q7-position">';
        html += buildRefrigeratedLocationCell(locationMap.get('Q7'), 'QUEUE ZONE');
        html += '</div>';

        // Racking Zone (stays grey/static)
        html += '<div class="queue-racking-position">';
        html += buildRefrigeratedStaticZone('RACKING', 'RACKING ZONE', 'Switch to Racking layout');
        html += '</div>';

        // Q3 Position (grey/static)
        html += '<div class="queue-q3-position">';
        html += buildRefrigeratedLocationCell(locationMap.get('Q3'), 'QUEUE ZONE');
        html += '</div>';

        // Empty areas for grid alignment
        html += '<div class="queue-empty-area"></div>';

        // Q2 Position (grey/static)
        html += '<div class="queue-q2-position">';
        html += buildRefrigeratedLocationCell(locationMap.get('Q2'), 'QUEUE ZONE');
        html += '</div>';

        // Empty area 2
        html += '<div class="queue-empty-area2"></div>';

        // Q1 Staging Area (grey/static)
        html += '<div class="queue-staging-position">';
        const q1Location = locationMap.get('Q1') || locationMap.get('QUEUE ZONE');
        html += buildRefrigeratedStagingArea(q1Location);
        html += '</div>';

        // Spacers
        html += '<div class="queue-spacer-1"></div>';
        html += '<div class="queue-spacer-2"></div>';

        // Refrigerated Zone (now active/green)
        html += '<div class="queue-refrigerated-position">';
        html += buildRefrigeratedActiveZone(locationMap);
        html += '</div>';

        html += '</div></div>';
        return html;
    }

    // Build Refrigerated Location Cell (Q2-Q7 - grey/static)
    function buildRefrigeratedLocationCell(location, queueCode) {
        // Always grey regardless of data - these are navigation elements
        return `<div class="refrigerated-location" 
                 data-location-code="${queueCode}"
                 data-zone-switch="QUEUE"
                 title="Switch to Queue zone - ${queueCode}">
                <span class="queue-location-code">${queueCode}</span>
            </div>`;
    }

    // Build Refrigerated Staging Area (Q1 - grey/static)
    function buildRefrigeratedStagingArea(location) {
        return `<div class="refrigerated-staging-area" 
                 data-location-code="Q1"
                 data-zone-switch="QUEUE"
                 title="Switch to Queue zone - Staging Area">
                QUEUE ZONE
            </div>`;
    }

    // Build Refrigerated Active Zone (green - the main refrigerated area)
    function buildRefrigeratedActiveZone(locationMap) {
        // Look for refrigerated zone location data
        const refrigeratedLocation = locationMap.get('R1') ||
            locationMap.get('REEFER') ||
            locationMap.get('COLD');

        if (!refrigeratedLocation) {
            return `<div class="refrigerated-active-zone available" 
                     data-location-code="REFRIGERATED"
                     data-status="available"
                     title="Refrigerated Zone - No data">
                    REFRIGERATED ZONE
                </div>`;
        }

        const status = refrigeratedLocation.statusName || 'available';
        // Force status to available or partial only (never occupied/red)
        const allowedStatus = (status.toLowerCase() === 'partial') ? 'partial' : 'available';

        return `<div class="refrigerated-active-zone ${allowedStatus}" 
                 data-location-id="${refrigeratedLocation.id}"
                 data-location-code="${refrigeratedLocation.code}"
                 data-location-barcode="${refrigeratedLocation.barcode || refrigeratedLocation.code}"
                 data-status="${allowedStatus}"
                 data-inventory-count="${refrigeratedLocation.inventoryCount || 0}"
                 data-total-quantity="${refrigeratedLocation.totalQuantity || 0}"
                 title="Refrigerated Zone - ${allowedStatus}">
                 ${refrigeratedLocation.name} (${refrigeratedLocation.code})
            </div>`;
    }

    // Build Static Zone for Refrigerated Layout (Racking stays grey)
    function buildRefrigeratedStaticZone(zoneType, zoneLabel, tooltip) {
        return `<div class="queue-racking-zone" 
                 data-zone-type="${zoneType}"
                 title="${tooltip}">
                ${zoneLabel}
                <div class="queue-zone-info">Click to switch layout</div>
            </div>`;
    }

    // Setup Refrigerated Location Cell Event Handlers
    function setupRefrigeratedLocationCellHandlers() {
        // Click handler for grey Q1-Q7 locations (zone switching)
        $('.refrigerated-location, .refrigerated-staging-area').off('click').on('click', function () {
            const targetZone = $(this).data('zone-switch');
            const locationCode = $(this).data('location-code');
            handleRefrigeratedZoneSwitching(targetZone, locationCode);
        });

        // Click handler for active refrigerated zone (inventory management)
        $('.refrigerated-active-zone').off('click').on('click', function () {
            const locationId = $(this).data('location-id');
            if (locationId) {
                showUnlimitedLocationModal(locationId);
            } else {
                if (typeof showWarningToast === 'function') {
                    showWarningToast('No refrigerated zone data available');
                }
            }
        });

        // Click handler for racking zone (zone switching)
        $('.queue-racking-zone').off('click').on('click', function () {
            const zoneType = $(this).data('zone-type');
            handleQueueZoneSwitching(zoneType);
        });

        // Hover handlers
        $('.refrigerated-location, .refrigerated-staging-area, .refrigerated-active-zone').off('mouseenter')
            .on('mouseenter', function () {
                const locationCode = $(this).data('location-code');
                const action = $(this).hasClass('refrigerated-active-zone') ?
                    'Manage refrigerated inventory' : 'Switch to Queue zone';
                $(this).attr('title', `${locationCode} - ${action}`);
            });
    }

    // Handle Zone Switching from Refrigerated Layout
    function handleRefrigeratedZoneSwitching(targetZoneType, fromLocation) {
        if (confirm(`Switch to ${targetZoneType} zone layout from ${fromLocation}?`)) {
            // Find the target zone in the dropdown
            const $zoneSelector = $('#zoneSelector');
            const targetOption = $zoneSelector.find('option').filter(function () {
                return $(this).text().toUpperCase().includes(targetZoneType.toUpperCase());
            });

            if (targetOption.length > 0) {
                // Switch to target zone
                $zoneSelector.val(targetOption.val()).trigger('change');
            } else {
                // Zone not found in dropdown
                if (typeof showWarningToast === 'function') {
                    showWarningToast(`${targetZoneType} zone not available`);
                } else {
                    alert(`${targetZoneType} zone not available`);
                }
            }
        }
    }

    // ===== UNLIMITED MODAL FUNCTIONS =====

    // Show Unlimited Location Modal (new modal for queue locations)
    function showUnlimitedLocationModal(locationId) {
        // Clear previous data
        $('#unlimitedLocationModal').removeData();

        // Set current location ID
        currentLocationId = locationId;

        // Load location details
        loadUnlimitedLocationDetails(locationId);

        // Show modal
        $('#unlimitedLocationModal').removeClass('hidden');
        $('body').addClass('overflow-hidden');
    }

    // Hide unlimited location modal
    function hideUnlimitedLocationModal() {
        $('#unlimitedLocationModal').addClass('hidden');
        $('body').removeClass('overflow-hidden');
    }

    // Load Unlimited Location Details
    function loadUnlimitedLocationDetails(locationId) {
        if (!locationId) return;

        // Show loading in modal
        $('#unlimitedLocationModal .modal-content').addClass('loading');

        // AJAX call to get location details (same endpoint as regular modal)
        $.ajax({
            url: `/LocationGrid/GetLocationDetails`,
            type: 'GET',
            data: { locationId: locationId },
            success: function (data) {
                if (data.error) {
                    if (typeof showErrorToast === 'function') {
                        showErrorToast(data.error);
                    }
                    hideUnlimitedLocationModal();
                    return;
                }
                populateUnlimitedLocationModal(data);
            },
            error: function (xhr) {
                console.error('Failed to load location details:', xhr);
                if (typeof showErrorToast === 'function') {
                    showErrorToast('Failed to load location details');
                }
                hideUnlimitedLocationModal();
            },
            complete: function () {
                $('#unlimitedLocationModal .modal-content').removeClass('loading');
            }
        });
    }

    // Populate Unlimited Location Modal with data
    function populateUnlimitedLocationModal(locationData) {
        // Update modal header with location code
        $('#modalUnlimitedLocationCode').text(`${locationData.code} Location Details`);
        $('#modalUnlimitedLocationName').text(locationData.name || '');

        // Populate basic location info
        $('#unlimitedLocationCode').text(locationData.code || '');
        $('#unlimitedLocationName').text(locationData.name || '');
        $('#unlimitedLocationZone').text(locationData.zoneName || '');
        $('#unlimitedLocationStatus').text(locationData.statusName || '');

        // Reset pagination state
        unlimitedCurrentPage = 1;
        unlimitedPageSize = 10;
        $('#unlimitedPageSize').val(10);
        $('#unlimitedCurrentItemsSearch').val('');

        // Reset available items state
        unlimitedSelectedItemIds.clear();
        $('#unlimitedItemSearch').val('');
        $('#unlimitedItemsClientFilter').val('');
        $('#unlimitedItemsTypeFilter').val('');

        // Load both current items and available items
        loadUnlimitedCurrentItems();
        loadUnlimitedAvailableItems();
    }

    // ===== UNLIMITED MODAL CURRENT ITEMS FUNCTIONS =====

    // Load unlimited current items with pagination
    function loadUnlimitedCurrentItems() {
        if (!currentLocationId) return;

        const searchTerm = $('#unlimitedCurrentItemsSearch').val().trim();

        // Show loading
        $('#unlimitedCurrentItemsTableBody').html(`
            <tr>
                <td colspan="6" class="px-6 py-8 text-center text-gray-500 dark:text-gray-400">
                    <iconify-icon icon="heroicons:arrow-path" class="animate-spin text-2xl mb-2"></iconify-icon>
                    <div>Loading items...</div>
                </td>
            </tr>
        `);

        $.ajax({
            url: '/LocationGrid/GetUnlimitedLocationCurrentItems',
            type: 'GET',
            data: {
                locationId: currentLocationId,
                page: unlimitedCurrentPage,
                pageSize: unlimitedPageSize,
                searchTerm: searchTerm || null
            },
            success: function (response) {
                if (response.error) {
                    showErrorToast(response.error);
                    return;
                }

                unlimitedCurrentItems = response.items || [];
                unlimitedTotalPages = response.totalPages || 1;
                unlimitedTotalItems = response.totalItems || 0;

                renderUnlimitedCurrentItemsTable();
                updateUnlimitedPagination();
            },
            error: function (xhr) {
                console.error('Error loading unlimited current items:', xhr);
                showErrorToast('Failed to load current items');
                $('#unlimitedCurrentItemsTableBody').html(`
                    <tr>
                        <td colspan="6" class="px-6 py-8 text-center text-red-500">
                            <iconify-icon icon="heroicons:exclamation-triangle" class="text-2xl mb-2"></iconify-icon>
                            <div>Failed to load items</div>
                        </td>
                    </tr>
                `);
            }
        });
    }

    // Render unlimited current items table
    function renderUnlimitedCurrentItemsTable() {
        const tbody = $('#unlimitedCurrentItemsTableBody');
        $('#unlimitedTotalItemsCount').text(unlimitedTotalItems);

        if (!unlimitedCurrentItems || unlimitedCurrentItems.length === 0) {
            tbody.html(`
                <tr>
                    <td colspan="6" class="px-6 py-8 text-center text-gray-500 dark:text-gray-400">
                        <iconify-icon icon="heroicons:cube-transparent" class="text-4xl mb-2"></iconify-icon>
                        <div>No items in this location</div>
                    </td>
                </tr>
            `);
            return;
        }

        let html = '';
        unlimitedCurrentItems.forEach(item => {
            const typeClass = getUnlimitedItemTypeBadgeClass(item.type);
            const receivedDate = new Date(item.receivedDate).toLocaleDateString();

            html += `
                <tr class="hover:bg-gray-50 dark:hover:bg-gray-700">
                    <td class="px-3 py-2">
                        <span class="inline-flex items-center px-2 py-1 rounded-full text-xs font-medium ${typeClass}">
                            ${escapeHtml(item.type)}
                        </span>
                    </td>
                    <td class="px-3 py-2">
                        <div class="text-sm font-medium text-gray-900 dark:text-white" title="${escapeHtml(item.name)}">
                            ${truncateText(escapeHtml(item.name), 25)}
                        </div>
                        ${item.batchLot ? `<div class="text-xs text-gray-500">Batch: ${escapeHtml(item.batchLot)}</div>` : ''}
                    </td>
                    <td class="px-3 py-2">
                        <span class="text-sm font-mono text-gray-600 dark:text-gray-400">
                            ${escapeHtml(item.mainHU || '-')}
                        </span>
                    </td>
                    <td class="px-3 py-2">
                        <span class="text-sm text-gray-600 dark:text-gray-400">
                            ${escapeHtml(item.client || '-')}
                        </span>
                    </td>
                    <td class="px-3 py-2">
                        <span class="text-sm font-medium text-gray-900 dark:text-white">
                            ${item.quantity}
                        </span>
                    </td>
                    <td class="px-3 py-2">
                        <span class="text-sm text-gray-600 dark:text-gray-400">
                            ${receivedDate}
                        </span>
                    </td>
                </tr>
            `;
        });

        tbody.html(html);
    }

    // Update unlimited pagination
    function updateUnlimitedPagination() {
        const from = unlimitedTotalItems === 0 ? 0 : ((unlimitedCurrentPage - 1) * unlimitedPageSize) + 1;
        const to = Math.min(unlimitedCurrentPage * unlimitedPageSize, unlimitedTotalItems);

        $('#unlimitedShowingFrom').text(from);
        $('#unlimitedShowingTo').text(to);
        $('#unlimitedShowingTotal').text(unlimitedTotalItems);

        // Build pagination controls
        let paginationHtml = '';

        // Previous button
        if (unlimitedCurrentPage > 1) {
            paginationHtml += `<button class="unlimited-page-btn px-3 py-1 text-sm border border-gray-300 rounded-md hover:bg-gray-50" data-page="${unlimitedCurrentPage - 1}">Previous</button>`;
        }

        // Page numbers
        const startPage = Math.max(1, unlimitedCurrentPage - 2);
        const endPage = Math.min(unlimitedTotalPages, unlimitedCurrentPage + 2);

        for (let i = startPage; i <= endPage; i++) {
            const activeClass = i === unlimitedCurrentPage ? 'bg-blue-500 text-white' : 'border-gray-300 hover:bg-gray-50';
            paginationHtml += `<button class="unlimited-page-btn px-3 py-1 text-sm border rounded-md ${activeClass}" data-page="${i}">${i}</button>`;
        }

        // Next button
        if (unlimitedCurrentPage < unlimitedTotalPages) {
            paginationHtml += `<button class="unlimited-page-btn px-3 py-1 text-sm border border-gray-300 rounded-md hover:bg-gray-50" data-page="${unlimitedCurrentPage + 1}">Next</button>`;
        }

        $('#unlimitedPaginationControls').html(paginationHtml);
    }

    // Helper function for type badge classes
    function getUnlimitedItemTypeBadgeClass(type) {
        switch (type) {
            case 'Inventory': return 'bg-blue-100 text-blue-800 dark:bg-blue-900/25 dark:text-blue-400';
            case 'GIV FG Pallet': return 'bg-green-100 text-green-800 dark:bg-green-900/25 dark:text-green-400';
            case 'GIV RM Pallet': return 'bg-purple-100 text-purple-800 dark:bg-purple-900/25 dark:text-purple-400';
            default: return 'bg-gray-100 text-gray-800 dark:bg-gray-900/25 dark:text-gray-400';
        }
    }

    // ===== UNLIMITED MODAL AVAILABLE ITEMS FUNCTIONS =====

    // Load available items for unlimited linking
    function loadUnlimitedAvailableItems() {
        if (!currentLocationId) return;

        const searchTerm = $('#unlimitedItemSearch').val().trim();
        const clientId = $('#unlimitedItemsClientFilter').val();
        const itemType = $('#unlimitedItemsTypeFilter').val();

        // Show loading state
        $('#unlimitedItemsTableBody').html(`
            <tr>
                <td colspan="6" class="px-6 py-8 text-center text-gray-500 dark:text-gray-400">
                    <iconify-icon icon="heroicons:arrow-path" class="animate-spin text-2xl mb-2"></iconify-icon>
                    <div>Loading available items...</div>
                </td>
            </tr>
        `);

        const params = {
            locationId: currentLocationId,
            searchTerm: searchTerm || null,
            clientId: clientId || null,
            itemType: itemType || null
        };

        $.ajax({
            url: '/LocationGrid/GetAvailableLinkableItems',
            type: 'GET',
            data: params,
            success: function (response) {
                if (response.error) {
                    showErrorToast(response.error);
                    return;
                }

                unlimitedAvailableItems = response.availableItems || [];

                // Populate client filter
                populateUnlimitedClientFilter(response.availableClients || []);

                // Render items table
                renderUnlimitedAvailableItemsTable();

                // Update UI state
                updateUnlimitedLinkingUI();
            },
            error: function (xhr) {
                console.error('Error loading unlimited available items:', xhr);
                showErrorToast('Failed to load available items');
                $('#unlimitedItemsTableBody').html(`
                    <tr>
                        <td colspan="6" class="px-6 py-8 text-center text-red-500">
                            <iconify-icon icon="heroicons:exclamation-triangle" class="text-2xl mb-2"></iconify-icon>
                            <div>Failed to load available items</div>
                        </td>
                    </tr>
                `);
            }
        });
    }

    // Populate client filter for unlimited modal
    function populateUnlimitedClientFilter(clients) {
        const select = $('#unlimitedItemsClientFilter');
        const currentValue = select.val();

        select.html('<option value="">All Clients</option>');

        clients.forEach(client => {
            select.append(`<option value="${client.id}">${escapeHtml(client.name)}</option>`);
        });

        if (currentValue && clients.some(c => c.id === currentValue)) {
            select.val(currentValue);
        }
    }

    // Render unlimited available items table
    function renderUnlimitedAvailableItemsTable() {
        const tbody = $('#unlimitedItemsTableBody');

        if (!unlimitedAvailableItems || unlimitedAvailableItems.length === 0) {
            tbody.html(`
                <tr>
                    <td colspan="6" class="px-6 py-8 text-center text-gray-500 dark:text-gray-400">
                        <iconify-icon icon="heroicons:cube-transparent" class="text-4xl mb-2"></iconify-icon>
                        <div>No items available for linking</div>
                    </td>
                </tr>
            `);
            return;
        }

        let html = '';
        unlimitedAvailableItems.forEach(item => {
            const isSelected = unlimitedSelectedItemIds.has(item.id);
            const typeDisplay = getItemTypeDisplay(item.type);
            const receiveDate = new Date(item.receiveDate).toLocaleDateString();

            html += `
                <tr class="unlimited-item-row hover:bg-gray-50 dark:hover:bg-gray-700" 
                    data-item-type="${item.type}">
                    <td class="px-3 py-2">
                        <input type="checkbox" 
                               class="unlimited-item-checkbox form-checkbox h-4 w-4 text-blue-600" 
                               data-item-id="${item.id}"
                               data-item-type="${item.type}"
                               ${isSelected ? 'checked' : ''}>
                    </td>
                    <td class="px-3 py-2">
                        <span class="inline-flex items-center px-2 py-1 rounded-full text-xs font-medium ${getItemTypeBadgeClass(item.type)}">
                            ${escapeHtml(typeDisplay)}
                        </span>
                    </td>
                    <td class="px-3 py-2">
                        <div class="text-sm font-medium text-gray-900 dark:text-white" title="${escapeHtml(item.displayName)}">
                            ${truncateText(escapeHtml(item.displayName), 30)}
                        </div>
                        ${item.additionalInfo ? `<div class="text-xs text-gray-500">${escapeHtml(item.additionalInfo)}</div>` : ''}
                    </td>
                    <td class="px-3 py-2">
                        <span class="text-sm font-mono text-gray-600 dark:text-gray-400">
                            ${escapeHtml(item.skuCode)}
                        </span>
                    </td>
                    <td class="px-3 py-2">
                        <span class="text-sm text-gray-600 dark:text-gray-400">
                            ${escapeHtml(item.clientName)}
                        </span>
                    </td>
                    <td class="px-3 py-2">
                        <span class="text-sm text-gray-600 dark:text-gray-400">
                            ${receiveDate}
                        </span>
                    </td>
                   <td class="px-3 py-2">
                        <span class="text-sm text-gray-600 dark:text-gray-400">
                            ${escapeHtml(item.locationAndZoneName)}
                        </span>
                    </td>
                </tr>
            `;
        });

        tbody.html(html);
    }

    // Filter unlimited available items
    function filterUnlimitedAvailableItems() {
        const searchTerm = $('#unlimitedItemSearch').val().toLowerCase().trim();
        const typeFilter = $('#unlimitedItemsTypeFilter').val();

        $('.unlimited-item-row').each(function () {
            const row = $(this);
            let showRow = true;

            if (typeFilter) {
                const itemType = row.data('item-type').toString();
                if (itemType !== typeFilter) {
                    showRow = false;
                }
            }

            if (showRow && searchTerm) {
                const text = row.text().toLowerCase();
                if (!text.includes(searchTerm)) {
                    showRow = false;
                }
            }

            if (showRow) {
                row.show().removeAttr('data-filtered');
            } else {
                row.hide().attr('data-filtered', 'hidden');
            }
        });

        updateUnlimitedLinkingUI();
    }

    // Get visible unlimited item checkboxes
    function getVisibleUnlimitedItemCheckboxes() {
        return $('.unlimited-item-row:not([data-filtered="hidden"]) .unlimited-item-checkbox');
    }

    // Update unlimited linking UI
    function updateUnlimitedLinkingUI() {
        const visibleCheckboxes = getVisibleUnlimitedItemCheckboxes();
        const checkedVisible = visibleCheckboxes.filter(':checked').length;
        const totalVisible = visibleCheckboxes.length;
        const selectedCount = unlimitedSelectedItemIds.size;

        const headerCheckbox = $('#selectAllUnlimitedItemsCheckbox');
        if (totalVisible === 0) {
            headerCheckbox.prop('indeterminate', false).prop('checked', false);
        } else if (checkedVisible === 0) {
            headerCheckbox.prop('indeterminate', false).prop('checked', false);
        } else if (checkedVisible === totalVisible) {
            headerCheckbox.prop('indeterminate', false).prop('checked', true);
        } else {
            headerCheckbox.prop('indeterminate', true).prop('checked', false);
        }

        $('#unlimitedSelectedCount').text(selectedCount);

        // No capacity checking for unlimited locations - just enable if items selected
        const canLink = selectedCount > 0;
        $('#unlimitedLinkSelectedBtn').prop('disabled', !canLink);
    }

    // Link selected items to unlimited location
    function linkSelectedUnlimitedItems() {
        if (unlimitedSelectedItemIds.size === 0) {
            if (typeof showWarningToast === 'function') {
                showWarningToast('Please select items to link');
            }
            return;
        }

        const selectedItems = Array.from(unlimitedSelectedItemIds).map(itemId => {
            const checkbox = $(`.unlimited-item-checkbox[data-item-id="${itemId}"]`);
            return {
                itemId: itemId,
                itemType: parseInt(checkbox.data('item-type'))
            };
        });

        const linkButton = $('#unlimitedLinkSelectedBtn');
        const originalText = linkButton.html();
        linkButton.prop('disabled', true).html(`
            <iconify-icon icon="heroicons:arrow-path" class="animate-spin mr-2"></iconify-icon>
            Linking...
        `);

        $.ajax({
            url: '/LocationGrid/LinkItemsToLocation',
            type: 'POST',
            contentType: 'application/json',
            headers: {
                'X-CSRF-TOKEN': $('meta[name="csrf-token"]').attr('content'),
                'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val()
            },
            data: JSON.stringify({
                locationId: currentLocationId,
                items: selectedItems
            }),
            success: function (response) {
                if (response.success) {
                    showSuccessToast(`Successfully linked ${response.linkedItemsCount} items to location`);

                    // Clear selections and reload both sections
                    unlimitedSelectedItemIds.clear();
                    loadUnlimitedAvailableItems();
                    loadUnlimitedCurrentItems(); // Refresh current items

                    updateUnlimitedLinkingUI();
                } else {
                    showErrorToast(response.message || 'Failed to link items');
                }
            },
            error: function (xhr) {
                console.error('Error linking items to unlimited location:', xhr);
                let errorMessage = 'Failed to link items to location';

                if (xhr.responseJSON && xhr.responseJSON.message) {
                    errorMessage = xhr.responseJSON.message;
                }

                showErrorToast(errorMessage);
            },
            complete: function () {
                linkButton.prop('disabled', false).html(originalText);
            }
        });
    }

    // Filter unlimited available items
    //function filterUnlimitedAvailableItems() {
    //    const searchTerm = $('#unlimitedItemSearch').val().toLowerCase().trim();
    //    const statusFilter = $('#unlimitedItemStatusFilter').val();

    //    $('.unlimited-available-item').each(function () {
    //        const item = $(this);
    //        let showItem = true;

    //        // Apply search filter
    //        if (searchTerm) {
    //            const text = item.text().toLowerCase();
    //            if (!text.includes(searchTerm)) {
    //                showItem = false;
    //            }
    //        }

    //        // Apply status filter (if needed)
    //        if (statusFilter && showItem) {
    //            // You can add status filtering logic here if your items have status
    //            // For now, we assume all items are available
    //        }

    //        if (showItem) {
    //            item.show();
    //        } else {
    //            item.hide();
    //        }
    //    });

    //    updateUnlimitedLinkingUI();
    //}

    // Setup unlimited modal event handlers
    function setupUnlimitedModalEventHandlers() {
        // Unlimited search with debounce
        $('#unlimitedCurrentItemsSearch').on('input', function () {
            clearTimeout(unlimitedSearchTimeout);
            unlimitedSearchTimeout = setTimeout(() => {
                unlimitedCurrentPage = 1; // Reset to first page
                loadUnlimitedCurrentItems();
            }, 300);
        });

        // Page size change
        $('#unlimitedPageSize').on('change', function () {
            unlimitedPageSize = parseInt($(this).val());
            unlimitedCurrentPage = 1; // Reset to first page
            loadUnlimitedCurrentItems();
        });

        // Pagination clicks
        $(document).on('click', '.unlimited-page-btn', function () {
            const page = parseInt($(this).data('page'));
            if (page && page !== unlimitedCurrentPage) {
                unlimitedCurrentPage = page;
                loadUnlimitedCurrentItems();
            }
        });

        // === Available Items Event Handlers ===

        // Available items search with debounce
        $('#unlimitedItemSearch').on('input', function () {
            clearTimeout(unlimitedAvailableSearchTimeout);
            unlimitedAvailableSearchTimeout = setTimeout(() => {
                filterUnlimitedAvailableItems();
            }, 300);
        });

        // Client filter for available items
        $('#unlimitedItemsClientFilter').on('change', function () {
            loadUnlimitedAvailableItems(); // Reload with new filter
        });

        // Type filter for available items
        $('#unlimitedItemsTypeFilter').on('change', function () {
            filterUnlimitedAvailableItems();
        });

        // Select all checkbox
        $('#selectAllUnlimitedItemsCheckbox').on('change', function () {
            const isChecked = $(this).is(':checked');
            const visibleCheckboxes = getVisibleUnlimitedItemCheckboxes();

            visibleCheckboxes.prop('checked', isChecked);

            // Update selected items set
            visibleCheckboxes.each(function () {
                const itemId = $(this).data('item-id');
                if (isChecked) {
                    unlimitedSelectedItemIds.add(itemId);
                } else {
                    unlimitedSelectedItemIds.delete(itemId);
                }
            });

            updateUnlimitedLinkingUI();
        });

        // Select all button
        $('#selectAllUnlimitedItems').on('click', function () {
            $('#selectAllUnlimitedItemsCheckbox').prop('checked', true).trigger('change');
        });

        // Deselect all button
        $('#deselectAllUnlimitedItems').on('click', function () {
            $('#selectAllUnlimitedItemsCheckbox').prop('checked', false).trigger('change');
        });

        // Individual item checkbox change
        $(document).on('change', '.unlimited-item-checkbox', function () {
            const itemId = $(this).data('item-id');
            const isChecked = $(this).is(':checked');

            if (isChecked) {
                unlimitedSelectedItemIds.add(itemId);
            } else {
                unlimitedSelectedItemIds.delete(itemId);
            }

            updateUnlimitedLinkingUI();
        });

        // Link selected items button
        $('#unlimitedLinkSelectedBtn').on('click', function () {
            if (unlimitedSelectedItemIds.size > 0 && !$(this).prop('disabled')) {
                linkSelectedUnlimitedItems();
            }
        });

        // Close unlimited location modal handlers
        $('#closeUnlimitedLocationModal, #closeUnlimitedLocationModalBtn').on('click', function () {
            hideUnlimitedLocationModal();
        });

        // Close modal when clicking outside
        $('#unlimitedLocationModal').on('click', function (e) {
            if (e.target === this) {
                hideUnlimitedLocationModal();
            }
        });

        // Refresh unlimited location data
        $('#unlimitedRefreshBtn').on('click', function () {
            if (currentLocationId) {
                loadUnlimitedLocationDetails(currentLocationId);
            }
        });
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
    window.LocationGridModule = {
        init: init,
        loadLocationData: loadLocationData,
        refreshGrid: function () {
            if (currentZoneId) {
                loadLocationData(currentZoneId);
            }
        }
    };
})();

// Initialize the module
LocationGridModule.init();