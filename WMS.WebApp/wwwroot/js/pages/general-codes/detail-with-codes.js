// Code Type With Codes Module - jQuery AJAX Version
(function () {
    // Private variables
    let currentCodeTypeId = '';
    let draggedElement = null;

    // Initialize module
    function init() {
        document.addEventListener('DOMContentLoaded', function () {
            initializePage();
            setupEventHandlers();
            setupSortable();
        });
    }

    // Initialize page data
    function initializePage() {
        // Get code type ID from URL or data attribute
        const pathParts = window.location.pathname.split('/');
        const idIndex = pathParts.indexOf('ViewCodeType') + 1;
        if (idIndex > 0 && pathParts[idIndex]) {
            currentCodeTypeId = pathParts[idIndex];
        }
    }

    // Setup event handlers
    function setupEventHandlers() {
        // Quick add form handler
        $("#quick-add-form").on("submit", function (e) {
            e.preventDefault();
            handleQuickAdd();
        });

        // Edit form handler
        $("#edit-code-form").on("submit", function (e) {
            e.preventDefault();
            handleEditCode();
        });

        // Auto-format code names
        setupCodeNameFormatting();
    }

    // Setup drag and drop for reordering
    function setupSortable() {
        const sortableContainer = document.getElementById('sortable-codes');
        if (!sortableContainer) return;

        const draggableItems = sortableContainer.querySelectorAll('.code-item[draggable="true"]');

        draggableItems.forEach(item => {
            item.addEventListener('dragstart', handleDragStart);
            item.addEventListener('dragover', handleDragOver);
            item.addEventListener('drop', handleDrop);
            item.addEventListener('dragend', handleDragEnd);
            item.addEventListener('dragenter', handleDragEnter);
            item.addEventListener('dragleave', handleDragLeave);
        });
    }

    // Drag and drop handlers
    function handleDragStart(e) {
        draggedElement = this;
        this.classList.add('opacity-50', 'transform', 'rotate-2');
        e.dataTransfer.effectAllowed = 'move';
        e.dataTransfer.setData('text/html', this.outerHTML);
    }

    function handleDragEnter(e) {
        if (this !== draggedElement) {
            this.classList.add('border-blue-400', 'bg-blue-50', 'dark:bg-blue-900/20');
        }
    }

    function handleDragLeave(e) {
        this.classList.remove('border-blue-400', 'bg-blue-50', 'dark:bg-blue-900/20');
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

        this.classList.remove('border-blue-400', 'bg-blue-50', 'dark:bg-blue-900/20');

        if (draggedElement !== this) {
            const container = this.parentNode;
            const allItems = Array.from(container.querySelectorAll('.code-item'));
            const draggedIndex = allItems.indexOf(draggedElement);
            const targetIndex = allItems.indexOf(this);

            if (draggedIndex < targetIndex) {
                container.insertBefore(draggedElement, this.nextSibling);
            } else {
                container.insertBefore(draggedElement, this);
            }

            // Update sequences and save
            updateSequencesAndSave(container);
        }

        return false;
    }

    function handleDragEnd(e) {
        this.classList.remove('opacity-50', 'transform', 'rotate-2');

        // Remove drag indicators from all items
        const allItems = document.querySelectorAll('.code-item');
        allItems.forEach(item => {
            item.classList.remove('border-blue-400', 'bg-blue-50', 'dark:bg-blue-900/20');
        });

        draggedElement = null;
    }

    // Update sequences after reordering - jQuery AJAX
    function updateSequencesAndSave(container) {
        const items = container.querySelectorAll('.code-item');
        const reorderData = [];

        items.forEach((item, index) => {
            const codeId = item.getAttribute('data-code-id');
            const newSequence = index + 1;
            reorderData.push({ CodeId: codeId, NewSequence: newSequence });

            // Update the displayed sequence number
            const sequenceBadge = item.querySelector('.w-10.h-10');
            if (sequenceBadge) {
                sequenceBadge.textContent = newSequence;
            }

            // Update data attribute
            item.setAttribute('data-sequence', newSequence);
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
                    // Update statistics
                    updateLastSequenceDisplay();
                } else {
                    showErrorToast('Failed to save new order: ' + (result.message || 'Unknown error'));
                    // Reload page to restore original order
                    window.location.reload();
                }
            },
            error: function (xhr) {
                console.error('Error saving reorder:', xhr);
                showErrorToast('Failed to save new order');
                window.location.reload();
            }
        });
    }

    // Modal functions
    function openQuickAddModal() {
        $('#quick-code-name').val('');
        $('#quick-code-detail').val('');

        // Set next sequence number
        const codes = document.querySelectorAll('.code-item');
        let maxSequence = 0;
        codes.forEach(item => {
            const sequence = parseInt(item.getAttribute('data-sequence') || '0');
            if (sequence > maxSequence) {
                maxSequence = sequence;
            }
        });
        $('#quick-code-sequence').val(maxSequence + 1);

        $('#quick-add-modal').removeClass('hidden');
        $('#quick-code-name').focus();
    }

    function closeQuickAddModal() {
        $('#quick-add-modal').addClass('hidden');
        clearFieldErrors('quick');
    }

    function editCode(codeId, name, detail, sequence) {
        $('#edit-code-id').val(codeId);
        $('#edit-code-name').val(name);
        $('#edit-code-detail').val(detail || '');
        $('#edit-code-sequence').val(sequence);

        $('#edit-code-modal').removeClass('hidden');
        $('#edit-code-name').focus();
    }

    function closeEditModal() {
        $('#edit-code-modal').addClass('hidden');
        clearFieldErrors('edit');
    }

    // Handle quick add form submission - jQuery AJAX
    function handleQuickAdd() {
        const warehouseId = $('#currentWarehouseId').val();

        const formData = {
            Name: $('#quick-code-name').val().trim(),
            Detail: $('#quick-code-detail').val().trim() || null,
            Sequence: parseInt($('#quick-code-sequence').val()),
            GeneralCodeTypeId: currentCodeTypeId,
            WarehouseId: warehouseId
        };

        if (!formData.Name) {
            showFieldError('quick-name-error', 'Code name is required');
            return;
        }

        // Show loading state
        const submitBtn = $('#quick-add-form button[type="submit"]');
        const originalText = submitBtn.text();
        submitBtn.prop('disabled', true).text('Adding...');

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
                    clearAllDrafts();
                    closeQuickAddModal();

                    // Reload page to show new code
                    setTimeout(() => {
                        window.location.reload();
                    }, 1000);
                } else {
                    if (result.errors) {
                        $.each(result.errors, function (key, value) {
                            if (key.toLowerCase().includes('name')) {
                                showFieldError('quick-name-error', value[0]);
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
                                showFieldError('quick-name-error', value[0]);
                            }
                        });
                    } else {
                        showErrorToast(xhr.responseJSON.message || 'Failed to add code');
                    }
                } else {
                    showErrorToast('Failed to add code');
                }
            },
            complete: function () {
                // Reset button state
                submitBtn.prop('disabled', false).text(originalText);
            }
        });
    }

    // Handle edit code form submission - jQuery AJAX
    function handleEditCode() {
        const codeId = $('#edit-code-id').val();
        const formData = {
            Name: $('#edit-code-name').val().trim(),
            Detail: $('#edit-code-detail').val().trim() || null,
            Sequence: parseInt($('#edit-code-sequence').val())
        };

        if (!formData.Name) {
            showFieldError('edit-name-error', 'Code name is required');
            return;
        }

        // Show loading state
        const submitBtn = $('#edit-code-form button[type="submit"]');
        const originalText = submitBtn.text();
        submitBtn.prop('disabled', true).text('Updating...');

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
                    closeEditModal();

                    // Reload page to show updated code
                    setTimeout(() => {
                        window.location.reload();
                    }, 1000);
                } else {
                    if (result.errors) {
                        $.each(result.errors, function (key, value) {
                            if (key.toLowerCase().includes('name')) {
                                showFieldError('edit-name-error', value[0]);
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
                                showFieldError('edit-name-error', value[0]);
                            }
                        });
                    } else {
                        showErrorToast(xhr.responseJSON.message || 'Failed to update code');
                    }
                } else {
                    showErrorToast('Failed to update code');
                }
            },
            complete: function () {
                // Reset button state
                submitBtn.prop('disabled', false).text(originalText);
            }
        });
    }

    // Delete code confirmation
    function deleteCode(codeId, codeName) {
        if (confirm(`Are you sure you want to delete the code "${codeName}"? This action cannot be undone.`)) {
            performDeleteCode(codeId);
        }
    }

    // Perform delete code - jQuery AJAX
    function performDeleteCode(codeId) {
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

                    // Remove the code item from DOM with animation
                    const codeItem = document.querySelector(`[data-code-id="${codeId}"]`);
                    if (codeItem) {
                        codeItem.style.transition = 'all 0.3s ease';
                        codeItem.style.opacity = '0';
                        codeItem.style.transform = 'translateX(-100%)';

                        setTimeout(() => {
                            codeItem.remove();
                            updateStatistics();
                            updateSequenceNumbers();

                            // Show empty state if no codes left
                            const remainingCodes = document.querySelectorAll('.code-item');
                            if (remainingCodes.length === 0) {
                                window.location.reload();
                            }
                        }, 300);
                    }
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

    // Setup code name formatting
    function setupCodeNameFormatting() {
        $('#quick-code-name, #edit-code-name').on('input', function () {
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
        $('#quick-code-name, #edit-code-name').on('blur', function () {
            const value = $(this).val().trim();
            if (value && !value.match(/^[A-Z_][A-Z0-9_]*$/)) {
                showWarningToast('Consider using UPPERCASE_WITH_UNDERSCORES format');
            }
        });
    }

    // Update statistics display
    function updateStatistics() {
        const codeItems = document.querySelectorAll('.code-item');
        const totalCount = codeItems.length;

        // Update total codes count in statistics
        const totalCodesElement = document.querySelector('.card-body .grid .card-body h6');
        if (totalCodesElement) {
            totalCodesElement.textContent = totalCount;
        }

        updateLastSequenceDisplay();
    }

    // Update last sequence display
    function updateLastSequenceDisplay() {
        const codeItems = document.querySelectorAll('.code-item');
        let maxSequence = 0;

        codeItems.forEach(item => {
            const sequence = parseInt(item.getAttribute('data-sequence') || '0');
            if (sequence > maxSequence) {
                maxSequence = sequence;
            }
        });

        // Update last sequence in statistics
        const lastSequenceElements = document.querySelectorAll('.card-body .grid .card-body h6');
        if (lastSequenceElements.length >= 4) {
            lastSequenceElements[3].textContent = maxSequence.toString();
        }
    }

    // Update sequence numbers after deletion
    function updateSequenceNumbers() {
        const codeItems = document.querySelectorAll('.code-item');

        codeItems.forEach((item, index) => {
            const newSequence = index + 1;
            const sequenceBadge = item.querySelector('.w-10.h-10');
            if (sequenceBadge) {
                sequenceBadge.textContent = newSequence;
            }
            item.setAttribute('data-sequence', newSequence);
        });
    }

    // Utility functions
    function showFieldError(elementId, message) {
        $('#' + elementId).text(message).removeClass('hidden');
    }

    function clearFieldErrors(prefix) {
        $(`[id^="${prefix}"][id$="-error"]`).addClass('hidden').text('');
    }

    function getCurrentWarehouseId() {
        // You might want to get this from a global variable or data attribute
        return $('[data-warehouse-id]').attr('data-warehouse-id') || '';
    }

    // Keyboard shortcuts
    function setupKeyboardShortcuts() {
        $(document).on('keydown', function (e) {
            // Ctrl/Cmd + N to add new code
            if ((e.ctrlKey || e.metaKey) && e.key === 'n') {
                e.preventDefault();
                const addButton = $('[onclick="openQuickAddModal()"]');
                if (addButton.length && !addButton.prop('disabled')) {
                    openQuickAddModal();
                }
            }

            // Escape to close modals
            if (e.key === 'Escape') {
                closeQuickAddModal();
                closeEditModal();
            }
        });
    }

    // Initialize keyboard shortcuts
    function initKeyboardShortcuts() {
        setupKeyboardShortcuts();

        // Show keyboard shortcuts hint
        const addButton = $('[onclick="openQuickAddModal()"]');
        if (addButton.length) {
            addButton.attr('title', 'Add Code (Ctrl+N)');
        }
    }

    // Auto-save draft functionality
    function setupAutoSave() {
        $('#quick-code-name, #edit-code-name, #quick-code-detail, #edit-code-detail').on('input', function () {
            const key = `draft_${this.id}`;
            if ($(this).val().trim()) {
                localStorage.setItem(key, $(this).val());
            } else {
                localStorage.removeItem(key);
            }
        });

        // Restore drafts when modals open
        $(document).on('focusin', '#quick-code-name, #edit-code-name, #quick-code-detail, #edit-code-detail', function () {
            const key = `draft_${this.id}`;
            const draft = localStorage.getItem(key);
            if (draft && !$(this).val()) {
                $(this).val(draft);
                showInfoToast('Draft restored');
            }
        });
    }

    // Clear drafts when forms are successfully submitted
    function clearDrafts(prefix) {
        const keys = Object.keys(localStorage).filter(key => key.startsWith(`draft_${prefix}`));
        keys.forEach(key => localStorage.removeItem(key));
    }
    function clearAllDrafts() {
        const keys = Object.keys(localStorage).filter(key => key.startsWith("draft_"));
        keys.forEach(key => localStorage.removeItem(key));
    }

    // Enhanced initialization
    function enhancedInit() {
        initKeyboardShortcuts();
        setupAutoSave();
    }

    // Expose functions to global scope for onclick handlers
    window.openQuickAddModal = openQuickAddModal;
    window.closeQuickAddModal = closeQuickAddModal;
    window.editCode = editCode;
    window.closeEditModal = closeEditModal;
    window.deleteCode = deleteCode;

    // Expose public methods
    window.CodeTypeWithCodesModule = {
        init: init,
        enhancedInit: enhancedInit
    };
})();

// Initialize the module
CodeTypeWithCodesModule.init();
CodeTypeWithCodesModule.enhancedInit();