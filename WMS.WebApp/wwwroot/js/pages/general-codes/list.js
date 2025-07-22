// General Codes Index Module - jQuery AJAX Version
(function () {
    // Private variables
    let hierarchicalData = [];
    let filteredData = [];
    let searchTerm = '';

    // Initialize module
    function init() {
        document.addEventListener('DOMContentLoaded', function () {
            setupEventHandlers();
            loadHierarchicalData();
            loadStatistics();
        });
    }

    // Set up event handlers
    function setupEventHandlers() {
        // Search functionality
        $('#search-input').on('input', function () {
            clearTimeout(this.searchTimeout);
            this.searchTimeout = setTimeout(() => {
                searchTerm = $(this).val().toLowerCase();
                filterAndRenderData();
            }, 300);
        });

        // Expand/Collapse all buttons
        $('#expand-all').on('click', function () {
            expandCollapseAll(true);
        });

        $('#collapse-all').on('click', function () {
            expandCollapseAll(false);
        });

        // Refresh button
        $('#refresh-data').on('click', function () {
            loadHierarchicalData();
            loadStatistics();
        });

        // Add code form handler
        $('#add-code-form').on('submit', function (e) {
            e.preventDefault();
            handleAddCode();
        });

        // Edit code form handler
        $('#edit-code-form').on('submit', function (e) {
            e.preventDefault();
            handleEditCode();
        });

        // Auto-format code names
        setupCodeNameFormatting();
    }
    // Setup code name formatting
    function setupCodeNameFormatting() {
        $('#modal-code-name, #edit-modal-code-name').on('input', function () {
            let value = $(this).val();

            // Convert to uppercase and replace spaces with underscores
            value = value.toUpperCase().replace(/\s+/g, '_');

            // Remove characters that aren't letters, numbers, or underscores
            value = value.replace(/[^A-Z0-9_]/g, '');

            // Limit length
            if (value.length > 100) {
                value = value.substring(0, 100);
            }

            if ($(this).val() !== value) {
                $(this).val(value);

                // Show formatting hint
                if (value.length > 0) {
                    showInfoToast('Code name formatted automatically');
                }
            }
        });

        // Suggest common code patterns
        $('#modal-code-name').on('blur', function () {
            const value = $(this).val().trim();
            if (value && !value.match(/^[A-Z_][A-Z0-9_]*$/)) {
                showWarningToast('Consider using UPPERCASE_WITH_UNDERSCORES format');
            }
        });
    }
    // Load hierarchical data from server - jQuery AJAX
    function loadHierarchicalData() {
        showLoading(true);

        $.ajax({
            url: '/GeneralCode/GetHierarchicalData',
            type: 'POST',
            headers: {
                'X-CSRF-TOKEN': $('meta[name="csrf-token"]').attr('content')
            },
            success: function (result) {
                if (result.success) {
                    hierarchicalData = result.data || [];
                    filteredData = [...hierarchicalData];
                    renderHierarchicalData();
                    updateStatistics();
                } else {
                    showErrorToast('Failed to load data: ' + (result.message || 'Unknown error'));
                    showNoDataMessage();
                }
            },
            error: function (xhr) {
                console.error('Error loading hierarchical data:', xhr);
                showErrorToast('Failed to load general codes data');
                showNoDataMessage();
            },
            complete: function () {
                showLoading(false);
            }
        });
    }

    // Filter and render data based on search
    function filterAndRenderData() {
        if (!searchTerm) {
            filteredData = [...hierarchicalData];
        } else {
            filteredData = hierarchicalData.map(item => {
                // Check if code type matches search
                const typeMatches = item.codeType.name.toLowerCase().includes(searchTerm) ||
                    (item.codeType.description && item.codeType.description.toLowerCase().includes(searchTerm));

                // Filter codes that match search
                const matchingCodes = item.codes.filter(code =>
                    code.name.toLowerCase().includes(searchTerm) ||
                    (code.detail && code.detail.toLowerCase().includes(searchTerm))
                );

                // Include if type matches or has matching codes
                if (typeMatches || matchingCodes.length > 0) {
                    return {
                        ...item,
                        codes: matchingCodes,
                        isExpanded: searchTerm ? true : item.isExpanded // Auto-expand when searching
                    };
                }
                return null;
            }).filter(item => item !== null);
        }

        renderHierarchicalData();
    }

    // Render hierarchical data
    function renderHierarchicalData() {
        const $container = $('#codes-data-container');
        const $noDataMessage = $('#no-data-message');

        if (!$container.length) return;

        if (filteredData.length === 0) {
            $container.html('');
            $noDataMessage.removeClass('hidden');
            return;
        }

        $noDataMessage.addClass('hidden');

        let html = '<div class="space-y-4">';

        filteredData.forEach(item => {
            html += renderCodeTypeSection(item);
        });

        html += '</div>';
        $container.html(html);

        // Attach event handlers after rendering
        attachCodeTypeEventHandlers();
    }

    // Render individual code type section
    function renderCodeTypeSection(item) {
        const codeType = item.codeType;
        const codes = item.codes;
        const isExpanded = item.isExpanded;
        const hasWriteAccess = codeType.hasWriteAccess;
        const hasDeleteAccess = codeType.hasDeleteAccess;

        let html = `
            <div class="border border-gray-200 dark:border-neutral-600 rounded-lg overflow-hidden">
                <!-- Code Type Header -->
                <div class="bg-gray-50 dark:bg-neutral-700 px-6 py-4 cursor-pointer hover:bg-gray-100 dark:hover:bg-neutral-600 transition-colors"
                     onclick="toggleCodeType('${codeType.id}')">
                    <div class="flex items-center justify-between">
                        <div class="flex items-center gap-4">
                            <iconify-icon icon="solar:alt-arrow-right-outline" 
                                         class="text-gray-400 text-lg transition-transform ${isExpanded ? 'rotate-90' : ''}" 
                                         id="arrow-${codeType.id}"></iconify-icon>
                            <div>
                                <h3 class="font-semibold text-gray-900 dark:text-white text-lg">${escapeHtml(codeType.name)}</h3>
                                ${codeType.description ? `<p class="text-sm text-gray-600 dark:text-gray-400 mt-1">${escapeHtml(codeType.description)}</p>` : ''}
                            </div>
                            <span class="bg-blue-100 dark:bg-blue-600/25 text-blue-800 dark:text-blue-300 text-xs px-3 py-1 rounded-full font-medium">
                                ${codes.length} code${codes.length !== 1 ? 's' : ''}
                            </span>
                        </div>
                        <div class="flex items-center gap-2">
                            ${hasWriteAccess ? `
                            <a onclick="event.stopPropagation(); openAddCodeModal('${codeType.warehouseId}', '${codeType.id}', '${escapeHtml(codeType.name)}')"  class="w-8 h-8 bg-primary-50 dark:bg-primary-600/10 text-primary-600 dark:text-primary-400 rounded-full inline-flex items-center justify-center mr-1" title="Add Code">
                                <iconify-icon icon="ic:baseline-plus"></iconify-icon>
                            </a>
                            <a href="GeneralCode/EditCodeType/${codeType.id}" onclick="event.stopPropagation()" class="w-8 h-8 bg-success-100 dark:bg-success-600/25 text-success-600 dark:text-success-400 rounded-full inline-flex items-center justify-center mr-1" title="Edit Code Type">
                            <iconify-icon icon="lucide:edit"></iconify-icon>
                            </a>
                            ` : ''}
                            <a href="/GeneralCode/ViewCodeType/${codeType.id}" class="w-8 h-8 bg-primary-50 dark:bg-primary-600/10 text-primary-600 dark:text-primary-400 rounded-full inline-flex items-center justify-center mr-1" title="View Detail">
                                <iconify-icon icon="iconamoon:eye-light"></iconify-icon>
                            </a>
                            ${hasDeleteAccess && codes.length === 0 ? `
                            <a href="javascript:void(0)" onclick="event.stopPropagation(); confirmDeleteCodeType('${codeType.id}', '${escapeHtml(codeType.name)}')" class="delete-user w-8 h-8 bg-danger-100 dark:bg-danger-600/25 text-danger-600 dark:text-danger-400 rounded-full inline-flex items-center justify-center" title="Delete Code Type">
                                <iconify-icon icon="mingcute:delete-2-line"></iconify-icon>
                            </a>
                            ` : ''}
                        </div>
                    </div>
                </div>
                
                <!-- Codes Container -->
                <div id="codes-${codeType.id}" class="transition-all duration-300 ${isExpanded ? 'block' : 'hidden'}">
        `;

        if (codes.length > 0) {
            html += '<div class="p-6 space-y-3">';

            codes.forEach(code => {
                html += `
                <div class="flex items-center justify-between p-4 bg-gray-50 dark:bg-neutral-800 rounded-lg group hover:bg-gray-100 dark:hover:bg-neutral-700 transition-all duration-200 border border-gray-200 dark:border-neutral-700"
                     data-code-id="${code.id}" draggable="true">
                    <div class="flex items-center gap-4 flex-1">
                        <div class="w-8 h-8 bg-blue-500 text-white rounded-full flex items-center justify-center text-sm font-medium shadow-sm">
                            ${code.sequence}
                        </div>
                        <div class="flex-1 min-w-0">
                            <div class="font-medium text-gray-900 dark:text-white truncate">${escapeHtml(code.name)}</div>
                            ${code.detail ? `<div class="text-sm text-gray-600 dark:text-gray-400 mt-1 truncate">${escapeHtml(code.detail)}</div>` : ''}
                        </div>
                    </div>
                    <div class="flex items-center gap-2 ml-4">
                        ${code.hasWriteAccess ? `
                              <a href="javascript:void(0)" onclick="openEditCodeModal('${code.id}', '${escapeHtml(code.name)}', '${escapeHtml(code.detail || '')}', ${code.sequence})" class="w-8 h-8 bg-success-100 dark:bg-success-600/25 text-success-600 dark:text-success-400 rounded-full inline-flex items-center justify-center mr-1" title="Edit Code">
                                            <iconify-icon icon="lucide:edit"></iconify-icon>
                              </a>
                        ` : ''}
                        ${code.hasDeleteAccess ? `
                            <a href="javascript:void(0)" onclick="confirmDeleteCode('${code.id}', '${escapeHtml(code.name)}')" class="delete-user w-8 h-8 bg-danger-100 dark:bg-danger-600/25 text-danger-600 dark:text-danger-400 rounded-full inline-flex items-center justify-center" title="Delete Code">
                                <iconify-icon icon="mingcute:delete-2-line"></iconify-icon>
                            </a>
                        ` : ''}
                        <a href="javascript:void(0)" class="w-8 h-8 bg-gray-100 text-gray-500 hover:text-gray-700 hover:bg-gray-200 dark:bg-neutral-700 dark:text-gray-400 dark:hover:text-gray-200 dark:hover:bg-neutral-600 cursor-move rounded-lg flex items-center justify-center transition-all duration-200 border border-gray-200 dark:border-neutral-600" title="Drag to reorder">
                            <iconify-icon icon="solar:hamburger-menu-outline"></iconify-icon>
                        </a>
                    </div>
                </div>
            `;
            });

            html += '</div>';
        } else {
            html += `
                <div class="p-6 text-center text-gray-500 dark:text-gray-400">
                    <iconify-icon icon="solar:code-outline" class="text-3xl mb-2"></iconify-icon>
                    <p class="text-sm">No codes defined for this type</p>
                    ${codeType.hasWriteAccess ? `
                        <button onclick="openAddCodeModal('${codeType.warehouseId}', '${codeType.id}', '${escapeHtml(codeType.name)}')" 
                                class="mt-2 text-blue-600 hover:text-blue-800 dark:text-blue-400 text-sm font-medium">
                            Add First Code
                        </button>
                    ` : ''}
                </div>
            `;
        }

        html += '</div></div>';
        return html;
    }

    // Attach event handlers after rendering
    function attachCodeTypeEventHandlers() {
        // Setup drag and drop for reordering
        setupDragAndDrop();
    }

    // Setup drag and drop functionality
    function setupDragAndDrop() {
        const draggableItems = document.querySelectorAll('[draggable="true"]');

        draggableItems.forEach(item => {
            item.addEventListener('dragstart', handleDragStart);
            item.addEventListener('dragover', handleDragOver);
            item.addEventListener('drop', handleDrop);
            item.addEventListener('dragend', handleDragEnd);
        });
    }

    // Drag and drop handlers
    let draggedElement = null;

    function handleDragStart(e) {
        draggedElement = this;
        this.classList.add('opacity-50');
        e.dataTransfer.effectAllowed = 'move';
        e.dataTransfer.setData('text/html', this.outerHTML);
    }

    function handleDragOver(e) {
        if (e.preventDefault) {
            e.preventDefault();
        }
        e.dataTransfer.dropEffect = 'move';
        return false;
    }

    function handleDrop(e) {
        if (e.stopPropagation) {
            e.stopPropagation();
        }

        if (draggedElement !== this) {
            // Get the parent container to determine if they're in the same code type
            const draggedParent = draggedElement.closest('[id^="codes-"]');
            const dropParent = this.closest('[id^="codes-"]');

            if (draggedParent === dropParent) {
                // Same code type, perform reorder
                const allItems = Array.from(draggedParent.querySelectorAll('[data-code-id]'));
                const draggedIndex = allItems.indexOf(draggedElement);
                const targetIndex = allItems.indexOf(this);

                if (draggedIndex < targetIndex) {
                    this.parentNode.insertBefore(draggedElement, this.nextSibling);
                } else {
                    this.parentNode.insertBefore(draggedElement, this);
                }

                // Update sequences and save
                updateSequencesAndSave(draggedParent);
            }
        }

        return false;
    }

    function handleDragEnd(e) {
        this.classList.remove('opacity-50');
        draggedElement = null;
    }

    // Update sequences after reordering - jQuery AJAX
    function updateSequencesAndSave(container) {
        const items = container.querySelectorAll('[data-code-id]');
        const reorderData = [];

        items.forEach((item, index) => {
            const codeId = item.getAttribute('data-code-id');
            const newSequence = index + 1;
            reorderData.push({ CodeId: codeId, NewSequence: newSequence });

            // Update the displayed sequence number
            const sequenceElement = item.querySelector('.w-8.h-8');
            if (sequenceElement) {
                sequenceElement.textContent = newSequence;
            }
        });

        $.ajax({
            url: '/GeneralCode/ReorderCodes',
            type: 'POST',
            data: JSON.stringify(reorderData),
            contentType: 'application/json',
            headers: {
                'X-CSRF-TOKEN': $('meta[name="csrf-token"]').attr('content')
            },
            success: function (result) {
                if (result.success) {
                    showSuccessToast('Codes reordered successfully');
                } else {
                    showErrorToast('Failed to save new order: ' + (result.message || 'Unknown error'));
                    // Reload data to restore original order
                    loadHierarchicalData();
                }
            },
            error: function (xhr) {
                console.error('Error saving reorder:', xhr);
                showErrorToast('Failed to save new order');
                loadHierarchicalData();
            }
        });
    }

    // Toggle code type expansion
    function toggleCodeType(codeTypeId) {
        const $container = $(`#codes-${codeTypeId}`);
        const $arrow = $(`#arrow-${codeTypeId}`);

        if ($container.length && $arrow.length) {
            const isExpanded = !$container.hasClass('hidden');

            if (isExpanded) {
                $container.addClass('hidden');
                $arrow.removeClass('rotate-90');
            } else {
                $container.removeClass('hidden');
                $arrow.addClass('rotate-90');
            }

            // Update the data state
            const dataItem = hierarchicalData.find(item => item.codeType.id === codeTypeId);
            if (dataItem) {
                dataItem.isExpanded = !isExpanded;
            }
        }
    }

    // Expand/collapse all code types
    function expandCollapseAll(expand) {
        hierarchicalData.forEach(item => {
            item.isExpanded = expand;
            const $container = $(`#codes-${item.codeType.id}`);
            const $arrow = $(`#arrow-${item.codeType.id}`);

            if ($container.length && $arrow.length) {
                if (expand) {
                    $container.removeClass('hidden');
                    $arrow.addClass('rotate-90');
                } else {
                    $container.addClass('hidden');
                    $arrow.removeClass('rotate-90');
                }
            }
        });
    }

    // Modal functions
    function openAddCodeModal(warehouseId, codeTypeId, codeTypeName) {
        $('#modal-code-type-id').val(codeTypeId);
        $('#modal-warehouse-id').val(warehouseId);
        $('#modal-code-name').val('');
        $('#modal-code-detail').val('');
        $('#modal-code-sequence').val('1');

        // Get next sequence number
        const codeType = hierarchicalData.find(item => item.codeType.id === codeTypeId);
        if (codeType && codeType.codes.length > 0) {
            const maxSequence = Math.max(...codeType.codes.map(c => c.sequence));
            $('#modal-code-sequence').val(maxSequence + 1);
        }

        $('#add-code-modal').removeClass('hidden');
        $('#modal-code-name').focus();
    }

    function closeAddCodeModal() {
        $('#add-code-modal').addClass('hidden');
        // Clear any error messages
        $('#modal-name-error').addClass('hidden');
    }

    function openEditCodeModal(codeId, name, detail, sequence) {
        $('#edit-modal-code-id').val(codeId);
        $('#edit-modal-code-name').val(name);
        $('#edit-modal-code-detail').val(detail || '');
        $('#edit-modal-code-sequence').val(sequence);

        $('#edit-code-modal').removeClass('hidden');
        $('#edit-modal-code-name').focus();
    }

    function closeEditCodeModal() {
        $('#edit-code-modal').addClass('hidden');
        // Clear any error messages
        $('#edit-modal-name-error').addClass('hidden');
    }

    // Handle add code form submission - jQuery AJAX
    function handleAddCode() {
        const formData = {
            Name: $('#modal-code-name').val().trim(),
            Detail: $('#modal-code-detail').val().trim() || null,
            Sequence: parseInt($('#modal-code-sequence').val()),
            GeneralCodeTypeId: $('#modal-code-type-id').val(),
            WarehouseId: $('#modal-warehouse-id').val()
        };

        if (!formData.Name) {
            showFieldError('modal-name-error', 'Code name is required');
            return;
        }

        $.ajax({
            url: '/GeneralCode/CreateCode',
            type: 'POST',
            data: JSON.stringify(formData),
            contentType: 'application/json',
            headers: {
                'X-CSRF-TOKEN': $('meta[name="csrf-token"]').attr('content')
            },
            success: function (result) {
                if (result.success) {
                    showSuccessToast('Code added successfully');
                    closeAddCodeModal();
                    loadHierarchicalData(); // Refresh data
                } else {
                    if (result.errors) {
                        $.each(result.errors, function (key, value) {
                            if (key.toLowerCase().includes('name')) {
                                showFieldError('modal-name-error', value[0]);
                            }
                        });
                    } else {
                        showErrorToast(result.message || 'Failed to add code');
                    }
                }
            },
            error: function (xhr) {
                console.error('Error adding code:', xhr);

                if (xhr.responseJSON) {
                    var errors = xhr.responseJSON.errors;
                    if (errors) {
                        $.each(errors, function (key, value) {
                            if (key.toLowerCase().includes('name')) {
                                showFieldError('modal-name-error', value[0]);
                            }
                        });
                    } else {
                        showErrorToast(xhr.responseJSON.message || 'Failed to add code');
                    }
                } else {
                    showErrorToast('Failed to add code');
                }
            }
        });
    }

    // Handle edit code form submission - jQuery AJAX
    function handleEditCode() {
        const codeId = $('#edit-modal-code-id').val();
        const formData = {
            Name: $('#edit-modal-code-name').val().trim(),
            Detail: $('#edit-modal-code-detail').val().trim() || null,
            Sequence: parseInt($('#edit-modal-code-sequence').val())
        };

        if (!formData.Name) {
            showFieldError('edit-modal-name-error', 'Code name is required');
            return;
        }

        $.ajax({
            url: `/GeneralCode/UpdateCode/${codeId}`,
            type: 'POST',
            data: JSON.stringify(formData),
            contentType: 'application/json',
            headers: {
                'X-CSRF-TOKEN': $('meta[name="csrf-token"]').attr('content')
            },
            success: function (result) {
                if (result.success) {
                    showSuccessToast('Code updated successfully');
                    closeEditCodeModal();
                    loadHierarchicalData(); // Refresh data
                } else {
                    if (result.errors) {
                        $.each(result.errors, function (key, value) {
                            if (key.toLowerCase().includes('name')) {
                                showFieldError('edit-modal-name-error', value[0]);
                            }
                        });
                    } else {
                        showErrorToast(result.message || 'Failed to update code');
                    }
                }
            },
            error: function (xhr) {
                console.error('Error updating code:', xhr);

                if (xhr.responseJSON) {
                    var errors = xhr.responseJSON.errors;
                    if (errors) {
                        $.each(errors, function (key, value) {
                            if (key.toLowerCase().includes('name')) {
                                showFieldError('edit-modal-name-error', value[0]);
                            }
                        });
                    } else {
                        showErrorToast(xhr.responseJSON.message || 'Failed to update code');
                    }
                } else {
                    showErrorToast('Failed to update code');
                }
            }
        });
    }

    // Delete confirmations
    function confirmDeleteCodeType(codeTypeId, name) {
        if (confirm(`Are you sure you want to delete the code type "${name}"? This action cannot be undone.`)) {
            deleteCodeType(codeTypeId);
        }
    }

    function confirmDeleteCode(codeId, name) {
        if (confirm(`Are you sure you want to delete the code "${name}"? This action cannot be undone.`)) {
            deleteCode(codeId);
        }
    }

    // Delete functions - jQuery AJAX
    function deleteCodeType(codeTypeId) {
        $.ajax({
            url: '/GeneralCode/DeleteCodeType',
            type: 'POST',
            data: { codeTypeId: codeTypeId },
            headers: {
                'X-CSRF-TOKEN': $('meta[name="csrf-token"]').attr('content')
            },
            success: function (result) {
                if (result.success) {
                    showSuccessToast('Code type deleted successfully');
                    loadHierarchicalData();
                    loadStatistics();
                } else {
                    showErrorToast(result.message || 'Failed to delete code type');
                }
            },
            error: function (xhr) {
                console.error('Error deleting code type:', xhr);

                let errorMessage = 'Error deleting code type';
                if (xhr.responseJSON && xhr.responseJSON.message) {
                    errorMessage = xhr.responseJSON.message;
                } else if (xhr.status === 400) {
                    errorMessage = 'Bad request - please check the code type data';
                } else if (xhr.status === 404) {
                    errorMessage = 'Code type not found';
                } else if (xhr.status === 500) {
                    errorMessage = 'Server error occurred';
                }

                showErrorToast(errorMessage);
            }
        });
    }

    function deleteCode(codeId) {
        $.ajax({
            url: '/GeneralCode/DeleteCode',
            type: 'POST',
            data: { codeId: codeId },
            headers: {
                'X-CSRF-TOKEN': $('meta[name="csrf-token"]').attr('content')
            },
            success: function (result) {
                if (result.success) {
                    showSuccessToast('Code deleted successfully');
                    loadHierarchicalData();
                    loadStatistics();
                } else {
                    showErrorToast(result.message || 'Failed to delete code');
                }
            },
            error: function (xhr) {
                console.error('Error deleting code:', xhr);

                let errorMessage = 'Error deleting code';
                if (xhr.responseJSON && xhr.responseJSON.message) {
                    errorMessage = xhr.responseJSON.message;
                } else if (xhr.status === 400) {
                    errorMessage = 'Bad request - please check the code data';
                } else if (xhr.status === 404) {
                    errorMessage = 'Code not found';
                } else if (xhr.status === 500) {
                    errorMessage = 'Server error occurred';
                }

                showErrorToast(errorMessage);
            }
        });
    }

    // Utility functions
    function showLoading(show) {
        $('#loading-spinner').toggle(show);
    }

    function showNoDataMessage() {
        $('#codes-data-container').html('');
        $('#no-data-message').removeClass('hidden');
    }

    function updateStatistics() {
        const totalTypes = hierarchicalData.length;
        const totalCodes = hierarchicalData.reduce((sum, item) => sum + item.codes.length, 0);

        $('#code-types-count').text(totalTypes);
        $('#total-codes-count').text(totalCodes);
        //$('#last-updated').text(lastUpdated);
        //$('#current-warehouse').text(warehouseName); // You might want to get this from server
    }

    function loadStatistics() {
        // This could load additional statistics from server if needed
        //$('#current-warehouse').text('Current Warehouse'); // You might want to get this from server
    }

    function showFieldError(elementId, message) {
        $(`#${elementId}`).text(message).removeClass('hidden');
    }

    function getCurrentWarehouseId() {
        // You might want to get this from a global variable or data attribute
        return $('[data-warehouse-id]').attr('data-warehouse-id') || '';
    }
   
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

    // Expose functions to global scope for onclick handlers
    window.toggleCodeType = toggleCodeType;
    window.openAddCodeModal = openAddCodeModal;
    window.closeAddCodeModal = closeAddCodeModal;
    window.openEditCodeModal = openEditCodeModal;
    window.closeEditCodeModal = closeEditCodeModal;
    window.confirmDeleteCodeType = confirmDeleteCodeType;
    window.confirmDeleteCode = confirmDeleteCode;

    // Expose public methods
    window.GeneralCodesModule = {
        init: init,
        refresh: loadHierarchicalData
    };
})();

// Initialize the module
GeneralCodesModule.init();