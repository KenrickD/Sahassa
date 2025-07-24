// Global variables
let selectedFinishedGoods = new Set();
let availableFinishedGoods = [];
let jobReleaseConfig = {};
let globalConflictData = {};

$(document).ready(function () {
    initializeCreateJobRelease();
});

//Main initialization
function initializeCreateJobRelease() {
    // Load available finished goods
    loadAvailableFinishedGoods();

    // Set default release date to tomorrow
    const tomorrow = new Date();
    tomorrow.setDate(tomorrow.getDate() + 1);
    document.getElementById('global-release-date').value = tomorrow.toISOString().split('T')[0];

    // Set up event listeners
    setupEventListeners();
}

//Set up all event handlers
function setupEventListeners() {
    // Step navigation
    document.getElementById('next-to-step-2').addEventListener('click', () => goToStep(2));
    document.getElementById('prev-to-step-1').addEventListener('click', () => goToStep(1));
    document.getElementById('next-to-step-3').addEventListener('click', () => goToStep(3));
    document.getElementById('prev-to-step-2').addEventListener('click', () => goToStep(2));

    // Finished good selection
    document.getElementById('select-all-finishedgoods').addEventListener('change', toggleAllFinishedGoods);
    document.getElementById('finishedgood-search').addEventListener('input', filterFinishedGoods);

    // Global settings
    document.getElementById('apply-global-settings').addEventListener('click', applyGlobalSettings);

    // Submit
    document.getElementById('submit-job-release').addEventListener('click', submitJobRelease);

    // Set up Step 2 event listeners (using event delegation)
    setupStep2EventListeners();
}

//Load finished goods from API
function loadAvailableFinishedGoods() {
    // Show loading state
    const tbody = document.getElementById('finishedgoods-table-body');
    tbody.innerHTML = '<tr><td colspan="5" class="text-center">Loading finished goods...</td></tr>';

    fetch('/FinishedGood/GetAvailableFinishedGoodsForJobRelease')
        .then(response => {
            if (!response.ok) {
                throw new Error('HTTP error! status: ' + response.status);
            }
            return response.json();
        })
        .then(data => {
            console.log('Loaded finished goods:', data);
            if (data.success && data.data) {
                availableFinishedGoods = data.data;
            } else {
                availableFinishedGoods = data;
            }
            renderFinishedGoodsTable();
        })
        .catch(error => {
            console.error('Error loading finished goods:', error);
            tbody.innerHTML = '<tr><td colspan="5" class="text-center text-danger">Failed to load finished goods. Please try again.</td></tr>';
            toastr.error('Failed to load available finished goods');
        });
}

//Render Step 1 table
function renderFinishedGoodsTable() {
    const tbody = document.getElementById('finishedgoods-table-body');

    if (!availableFinishedGoods || availableFinishedGoods.length === 0) {
        tbody.innerHTML = '<tr><td colspan="5" class="text-center">No finished goods available for release.</td></tr>';
        return;
    }

    tbody.innerHTML = '';

    availableFinishedGoods.forEach(function (finishedGood) {
        const row = document.createElement('tr');
        row.className = 'finishedgood-row';
        row.dataset.finishedgoodId = finishedGood.id;

        row.innerHTML = `
            <td>
                <input type="checkbox" class="form-check-input finishedgood-checkbox" 
                       data-finishedgood-id="${finishedGood.id}">
            </td>
            <td>${finishedGood.sku || ''}</td>
            <td>${finishedGood.description || ''}</td>
            <td>${finishedGood.balanceQty || 0}</td>
            <td>${finishedGood.balancePallets || 0}</td>
        `;

        // Add click handler for row selection
        row.addEventListener('click', function (e) {
            if (e.target.type !== 'checkbox') {
                const checkbox = row.querySelector('.finishedgood-checkbox');
                checkbox.checked = !checkbox.checked;
                checkbox.dispatchEvent(new Event('change'));
            }
        });

        // Add change handler for checkbox
        const checkbox = row.querySelector('.finishedgood-checkbox');
        checkbox.addEventListener('change', function () {
            const finishedGoodId = this.dataset.finishedgoodId;
            if (this.checked) {
                selectedFinishedGoods.add(finishedGoodId);
            } else {
                selectedFinishedGoods.delete(finishedGoodId);
            }
            updateSelectionSummary();
            updateStepButtons();
        });

        tbody.appendChild(row);
    });
}

// Select/deselect all finished goods
function toggleAllFinishedGoods() {
    const selectAllCheckbox = document.getElementById('select-all-finishedgoods');
    const finishedGoodCheckboxes = document.querySelectorAll('.finishedgood-checkbox');

    finishedGoodCheckboxes.forEach(function (checkbox) {
        checkbox.checked = selectAllCheckbox.checked;
        const finishedGoodId = checkbox.dataset.finishedgoodId;

        if (selectAllCheckbox.checked) {
            selectedFinishedGoods.add(finishedGoodId);
        } else {
            selectedFinishedGoods.delete(finishedGoodId);
        }
    });

    updateSelectionSummary();
    updateStepButtons();
}

// Update selection badge
function updateSelectionSummary() {
    const count = selectedFinishedGoods.size;
    const badge = document.getElementById('selected-count');
    badge.textContent = count + ' finished good' + (count !== 1 ? 's' : '') + ' selected';

    // Update row styling
    document.querySelectorAll('.finishedgood-row').forEach(function (row) {
        const finishedGoodId = row.dataset.finishedgoodId;
        if (selectedFinishedGoods.has(finishedGoodId)) {
            row.classList.add('selected');
        } else {
            row.classList.remove('selected');
        }
    });
}

// Enable/disable navigation buttons
function updateStepButtons() {
    // Enable/disable next button based on selection
    const nextButton = document.getElementById('next-to-step-2');
    nextButton.disabled = selectedFinishedGoods.size === 0;
}

// Navigate between steps
function goToStep(stepNumber) {
    // Special handling for step 3 - validate before proceeding
    if (stepNumber === 3) {
        validateAndProceedToStep3();
        return;
    }

    // Hide all step contents
    document.querySelectorAll('.step-content').forEach(function (content) {
        content.style.display = 'none';
    });

    // Show target step content
    document.getElementById('step-' + stepNumber).style.display = 'block';

    // Update step indicator
    updateStepIndicator(stepNumber);

    // Perform step-specific actions
    if (stepNumber === 2) {
        loadFinishedGoodConfigurations();
    } else if (stepNumber === 3) {
        loadReviewSummary();
    }
}

//Show specific step
function showStep(stepNumber) {
    // Hide all step contents
    document.querySelectorAll('.step-content').forEach(function (content) {
        content.style.display = 'none';
    });

    // Show target step content
    document.getElementById('step-' + stepNumber).style.display = 'block';

    // Update step indicator
    updateStepIndicator(stepNumber);

    // Perform step-specific actions
    if (stepNumber === 2) {
        loadFinishedGoodConfigurations();
    } else if (stepNumber === 3) {
        loadReviewSummary();
    }
}

// Update step progress indicator
function updateStepIndicator(activeStep) {
    document.querySelectorAll('.step').forEach(function (step, index) {
        const stepNum = index + 1;
        step.classList.remove('step-active', 'step-completed');

        if (stepNum < activeStep) {
            step.classList.add('step-completed');
        } else if (stepNum === activeStep) {
            step.classList.add('step-active');
        }
    });
}

// Search/filter finished goods
function filterFinishedGoods() {
    const searchTerm = document.getElementById('finishedgood-search').value.toLowerCase();
    const rows = document.querySelectorAll('.finishedgood-row');

    rows.forEach(function (row) {
        const sku = row.children[1].textContent.toLowerCase();
        const description = row.children[2].textContent.toLowerCase();

        if (sku.includes(searchTerm) || description.includes(searchTerm)) {
            row.style.display = '';
        } else {
            row.style.display = 'none';
        }
    });
}

// Load Step 2 data
function loadFinishedGoodConfigurations() {
    const container = document.getElementById('selected-finishedgoods-container');
    container.innerHTML = '<div class="text-center p-4"><iconify-icon icon="mdi:loading" class="animate-spin"></iconify-icon> Loading finished good configurations...</div>';

    // Get selected finished good IDs
    const selectedFinishedGoodIds = Array.from(selectedFinishedGoods);

    if (selectedFinishedGoodIds.length === 0) {
        container.innerHTML = '<div class="alert alert-warning">No finished goods selected.</div>';
        return;
    }

    // Fetch inventory data for selected finished goods
    fetch('/FinishedGood/GetFinishedGoodInventoryForJobRelease', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
            'X-CSRF-TOKEN': $('input[name="__RequestVerificationToken"]').val()
        },
        body: JSON.stringify(selectedFinishedGoodIds)
    })
        .then(response => {
            if (!response.ok) {
                throw new Error('HTTP error! status: ' + response.status);
            }
            return response.json();
        })
        .then(data => {
            console.log('Loaded finished good inventory:', data);
            const inventoryData = data.success ? data.data : data;
            renderFinishedGoodConfigurations(inventoryData);
            updateStep2Buttons();
        })
        .catch(error => {
            console.error('Error loading finished good inventory:', error);
            container.innerHTML = '<div class="alert alert-danger">Failed to load finished good inventory. Please try again.</div>';
            toastr.error('Failed to load finished good inventory');
        });
}

// Render Step 2 UI
function renderFinishedGoodConfigurations(finishedGoodsData) {
    const container = document.getElementById('selected-finishedgoods-container');
    container.innerHTML = '';

    console.log('Rendering', finishedGoodsData.length, 'finished good configurations...');

    finishedGoodsData.forEach(function (finishedGood) {
        const finishedGoodCard = createFinishedGoodConfigCard(finishedGood);
        container.appendChild(finishedGoodCard);
    });

    // Wait for DOM to be fully rendered, then check conflicts for all finished goods
    setTimeout(() => {
        console.log('Starting conflict check for all finished goods after DOM rendering...');
        checkConflictsForAllFinishedGoods(finishedGoodsData);
    }, 1500); // Longer delay to ensure everything is rendered
}

// Create finished good cards
function createFinishedGoodConfigCard(finishedGood) {
    const cardDiv = document.createElement('div');
    cardDiv.className = 'finishedgood-config-card';
    cardDiv.dataset.finishedgoodId = finishedGood.id;

    const description = finishedGood.description || 'No Description';

    cardDiv.innerHTML = `
        <div class="finishedgood-config-header">
            <div class="d-flex justify-content-between align-items-center w-100">
                <div>
                    <h5 class="mb-1">${finishedGood.sku} - ${description}</h5>
                    <small class="text-muted">Balance: ${finishedGood.totalBalanceQty} items, ${finishedGood.totalBalancePallet} pallets</small>
                </div>
                <div class="form-check">
                    <input type="checkbox" class="form-check-input finishedgood-include-checkbox"
                           id="include-${finishedGood.id}" data-finishedgood-id="${finishedGood.id}" checked>
                    <label class="form-check-label" for="include-${finishedGood.id}">Include in Release</label>
                </div>
            </div>
        </div>
        <div class="finishedgood-config-content" id="content-${finishedGood.id}">
            ${createFinishedGoodReleaseTable(finishedGood)}
        </div>
    `;

    // Add event listeners
    const includeCheckbox = cardDiv.querySelector('.finishedgood-include-checkbox');
    includeCheckbox.addEventListener('change', function () {
        const content = cardDiv.querySelector('.finishedgood-config-content');
        content.style.display = this.checked ? 'block' : 'none';
        content.style.opacity = this.checked ? '1' : '0.5';

        if (!this.checked) {
            // Uncheck all pallets and items when excluding finished good
            const checkboxes = content.querySelectorAll('input[type="checkbox"]');
            checkboxes.forEach(cb => {
                if (!cb.disabled) {
                    cb.checked = false;
                }
            });
        }
        updateStep2Buttons();
    });

    // Don't check conflicts here - we'll do it later in batch
    return cardDiv;
}

// Create release table
function createFinishedGoodReleaseTable(finishedGood) {
    return `
        <table class="table table-bordered align-middle">
            <thead>
                <tr>
                    <th style="width: 10%;">Select</th>
                    <th style="width: 25%;">Release Type</th>
                    <th>Pallets & Items</th>
                </tr>
            </thead>
            <tbody>
                ${finishedGood.receives.map(function (receive) { return createReceiveRow(finishedGood.id, receive); }).join('')}
            </tbody>
        </table>
    `;
}

// Create receive rows
function createReceiveRow(finishedGoodId, receive) {
    const receiveId = receive.id;
    const hasAvailableItems = receive.pallets.some(function (p) {
        return !p.isReleased || p.items.some(function (i) { return !i.isReleased; });
    });

    if (!hasAvailableItems) {
        return ''; // Skip receives with no available items
    }

    const receivedDate = new Date(receive.receivedDate).toLocaleDateString();
    const batchInfo = receive.batchNo || 'N/A';

    return `
        <tr data-finishedgood-id="${finishedGoodId}" data-receive-id="${receiveId}">
            <td class="text-center">
                <input type="checkbox" class="receive-checkbox"
                       data-finishedgood-id="${finishedGoodId}" data-receive-id="${receiveId}">
            </td>
            <td>
                <select class="form-select release-type"
                        data-finishedgood-id="${finishedGoodId}" data-receive-id="${receiveId}" disabled>
                    <option value="Partial">Partial</option>
                    <option value="Full">Full</option>
                </select>
            </td>
            <td>
                <div class="mb-2">
                    <button type="button" class="btn btn-link btn-sm p-0 toggle-all-items"
                            data-finishedgood-id="${finishedGoodId}" data-receive-id="${receiveId}">
                        Show All Items
                    </button>
                    <small class="text-muted ms-2">Batch: ${batchInfo} | Received: ${receivedDate}</small>
                </div>
                <div class="pallets-container" style="display: none;">
                    ${receive.pallets.map(function (pallet) { return createPalletHtml(finishedGoodId, receiveId, pallet); }).join('')}
                </div>
            </td>
        </tr>
    `;
}

// Create pallet HTML
function createPalletHtml(finishedGoodId, receiveId, pallet) {
    const palletId = pallet.id;
    const availableItems = pallet.items.filter(function (item) { return !item.isReleased; });
    const releasedItems = pallet.items.filter(function (item) { return item.isReleased; });
    const isEntirelyReleased = pallet.isReleased || availableItems.length === 0;

    // Skip fully released pallets
    if (isEntirelyReleased && availableItems.length === 0) {
        return '';
    }

    // Use the same card structure as raw material
    return `
        <div class="card p-2 me-2 mb-2" style="min-width: 220px; max-width: 250px; display: inline-block;">
            <div class="d-flex justify-content-between align-items-center mb-1">
                <div class="form-check">
                    <input type="checkbox" class="form-check-input pallet-checkbox"
                           id="pallet-${pallet.id}"
                           data-finishedgood-id="${finishedGoodId}"
                           data-receive-id="${receiveId}"
                           data-pallet-id="${palletId}"
                           data-released="${isEntirelyReleased.toString().toLowerCase()}"
                           ${isEntirelyReleased ? 'checked disabled' : 'disabled'}>
                    <label class="form-check-label pallet-code-label" for="pallet-${pallet.id}">
                        <strong>${pallet.palletCode}</strong>
                        <small class="text-muted ms-2">(${availableItems.length}/${pallet.items.length} available)</small>
                    </label>
                </div>
            </div>
            <div class="pallet-items mt-2" style="display: none;">
                <div class="card-subtitle mb-2 text-muted small">Individual Items</div>
                <ul class="list-unstyled mb-0" style="max-height: 150px; overflow-y: auto;">
                    ${pallet.items.map(function (item) { return createItemHtml(finishedGoodId, receiveId, palletId, item); }).join('')}
                </ul>
            </div>
        </div>
    `;
}

// Create item HTML
function createItemHtml(finishedGoodId, receiveId, palletId, item, isReleased = false) {
    return `
        <li class="mb-1">
            <div class="form-check">
                <input type="checkbox" class="form-check-input item-checkbox"
                       id="item-${item.id}"
                       data-finishedgood-id="${finishedGoodId}"
                       data-receive-id="${receiveId}"
                       data-pallet-id="${palletId}"
                       data-item-id="${item.id}"
                       data-released="${item.isReleased.toString().toLowerCase()}"
                       ${item.isReleased ? 'checked disabled' : 'disabled'}>
                <label class="form-check-label item-code-label" for="item-${item.id}">
                    ${item.itemCode || 'No Code'}
                </label>
            </div>
        </li>
    `;
}

// Event handling & interaction logic functions

// Event delegation for Step 2
function setupStep2EventListeners() {
    // Set up event delegation for dynamically created elements
    const container = document.getElementById('selected-finishedgoods-container');

    // Receive checkbox behavior
    container.addEventListener('change', function (e) {
        if (e.target.classList.contains('receive-checkbox')) {
            handleReceiveCheckboxChange(e.target);
        }
    });

    // Release type behavior
    container.addEventListener('change', function (e) {
        if (e.target.classList.contains('release-type')) {
            handleReleaseTypeChange(e.target);
        }
    });

    // Pallet checkbox behavior
    container.addEventListener('change', function (e) {
        if (e.target.classList.contains('pallet-checkbox')) {
            handlePalletCheckboxChange(e.target);
        }
    });

    // Item checkbox behavior
    container.addEventListener('change', function (e) {
        if (e.target.classList.contains('item-checkbox')) {
            handleItemCheckboxChange(e.target);
        }
    });

    // Toggle all items button
    container.addEventListener('click', function (e) {
        if (e.target.classList.contains('toggle-all-items')) {
            toggleAllItemsForReceive(e.target);
        }
    });

    // Toggle pallet items button
    container.addEventListener('click', function (e) {
        if (e.target.classList.contains('toggle-pallet-items')) {
            togglePalletItems(e.target);
        }
    });
}

// Receive selection logic
function handleReceiveCheckboxChange(checkbox) {
    const finishedGoodId = checkbox.dataset.finishedgoodId;
    const receiveId = checkbox.dataset.receiveId;
    const row = checkbox.closest('tr');

    const releaseTypeSelect = row.querySelector('.release-type');
    const palletCheckboxes = row.querySelectorAll('.pallet-checkbox:not([data-released="true"])');
    const itemCheckboxes = row.querySelectorAll('.item-checkbox:not([data-released="true"])');

    if (checkbox.checked) {
        // Enable release type
        releaseTypeSelect.disabled = false;

        // Enable pallet and item checkboxes (but don't check them automatically)
        palletCheckboxes.forEach(function (cb) {
            cb.disabled = false; // Enable the checkbox
            // Don't automatically check - let user choose
        });
        itemCheckboxes.forEach(function (cb) {
            cb.disabled = false; // Enable the checkbox
            // Don't automatically check - let user choose
        });

        // If full release type is already selected, then check all
        if (releaseTypeSelect.value === 'Full') {
            palletCheckboxes.forEach(function (cb) {
                cb.checked = true;
                cb.disabled = true;
                cb.dispatchEvent(new Event('change'));
            });
            itemCheckboxes.forEach(function (cb) {
                cb.checked = true;
                cb.disabled = true;
            });
        }

        // Show the pallets container
        const palletsContainer = row.querySelector('.pallets-container');
        if (palletsContainer) {
            palletsContainer.style.display = 'block';
        }
    } else {
        // Disable everything and uncheck
        releaseTypeSelect.disabled = true;
        releaseTypeSelect.value = 'Partial';

        palletCheckboxes.forEach(function (cb) {
            cb.checked = false;
            cb.disabled = true; // Disable when receive is unchecked
        });
        itemCheckboxes.forEach(function (cb) {
            cb.checked = false;
            cb.disabled = true; // Disable when receive is unchecked
        });

        // Hide the pallets container
        const palletsContainer = row.querySelector('.pallets-container');
        if (palletsContainer) {
            palletsContainer.style.display = 'none';
        }

        // Hide all item containers
        row.querySelectorAll('.pallet-items').forEach(function (container) {
            container.style.display = 'none';
        });
    }

    updateStep2Buttons();
}

// Release type logic
function handleReleaseTypeChange(select) {
    const finishedGoodId = select.dataset.finishedgoodId;
    const receiveId = select.dataset.receiveId;
    const row = select.closest('tr');
    const receiveCheckbox = row.querySelector('.receive-checkbox');

    // Only proceed if the receive checkbox is checked
    if (!receiveCheckbox.checked) {
        return;
    }

    const palletCheckboxes = row.querySelectorAll('.pallet-checkbox:not([data-released="true"])');
    const itemCheckboxes = row.querySelectorAll('.item-checkbox:not([data-released="true"])');

    if (select.value === 'Full') {
        // Check and disable all pallets/items
        palletCheckboxes.forEach(function (cb) {
            cb.checked = true;
            cb.disabled = true;
            cb.dispatchEvent(new Event('change'));
        });
        itemCheckboxes.forEach(function (cb) {
            cb.checked = true;
            cb.disabled = true;
        });

        // Show all items for visibility
        row.querySelectorAll('.pallet-items').forEach(function (container) {
            container.style.display = 'block';
        });
    } else {
        // Partial - enable individual selection
        palletCheckboxes.forEach(function (cb) {
            cb.checked = false;
            cb.disabled = false; // Enable for individual selection
            cb.dispatchEvent(new Event('change'));
        });
        itemCheckboxes.forEach(function (cb) {
            cb.checked = false;
            cb.disabled = false; // Enable for individual selection
        });
    }

    updateStep2Buttons();
}

// Pallet selection logic
function handlePalletCheckboxChange(checkbox) {
    // Don't proceed if disabled (e.g., already released items)
    if (checkbox.disabled) return;

    const finishedGoodId = checkbox.dataset.finishedgoodId;
    const receiveId = checkbox.dataset.receiveId;
    const palletId = checkbox.dataset.palletId;
    const row = checkbox.closest('tr');
    const receiveCheckbox = row.querySelector('.receive-checkbox');

    // If user tries to check pallet without receive being checked, auto-check receive
    if (checkbox.checked && receiveCheckbox && !receiveCheckbox.checked && !receiveCheckbox.indeterminate) {
        console.log('🔧 Auto-checking receive checkbox because pallet was selected');
        receiveCheckbox.checked = true;
        receiveCheckbox.dispatchEvent(new Event('change'));
    }

    const card = checkbox.closest('.card');
    const itemsContainer = card.querySelector('.pallet-items');
    const itemCheckboxes = card.querySelectorAll('.item-checkbox:not([data-released="true"])');

    if (checkbox.checked) {
        // Show items and check all items in this pallet
        itemsContainer.style.display = 'block';
        itemCheckboxes.forEach(function (cb) {
            cb.checked = true;
            cb.disabled = true; // Disable individual items when pallet is selected
        });
    } else {
        // Keep items visible but uncheck them and re-enable for individual selection
        itemCheckboxes.forEach(function (cb) {
            cb.checked = false;
            cb.disabled = false; // Re-enable individual items when pallet is deselected
        });
    }

    // Update receive checkbox state
    updateReceiveCheckboxState(checkbox);
    updateStep2Buttons();
}

// Item selection logic
function handleItemCheckboxChange(checkbox) {
    // Don't proceed if disabled
    if (checkbox.disabled) return;

    const finishedGoodId = checkbox.dataset.finishedgoodId;
    const receiveId = checkbox.dataset.receiveId;
    const palletId = checkbox.dataset.palletId;
    const row = checkbox.closest('tr');
    const receiveCheckbox = row.querySelector('.receive-checkbox');

    // If user tries to check item without receive being checked, auto-check receive
    if (checkbox.checked && receiveCheckbox && !receiveCheckbox.checked && !receiveCheckbox.indeterminate) {
        console.log('🔧 Auto-checking receive checkbox because item was selected');
        receiveCheckbox.checked = true;
        receiveCheckbox.dispatchEvent(new Event('change'));
    }

    // If any item in a pallet is selected, uncheck the pallet checkbox
    const palletCheckbox = row.querySelector(`.pallet-checkbox[data-pallet-id="${palletId}"]`);
    if (palletCheckbox && checkbox.checked) {
        palletCheckbox.checked = false;
    }

    // Update receive checkbox state
    updateReceiveCheckboxState(checkbox);
    updateStep2Buttons();
}

// Show/hide items for receive
function toggleAllItemsForReceive(button) {
    const finishedGoodId = button.dataset.finishedgoodId;
    const receiveId = button.dataset.receiveId;
    const row = button.closest('tr');
    const itemContainers = row.querySelectorAll('.pallet-items');

    // Count visible containers
    let visibleCount = 0;
    itemContainers.forEach(function (container) {
        if (container.style.display === 'block') {
            visibleCount++;
        }
    });

    // Toggle visibility
    const shouldShow = visibleCount < itemContainers.length;
    itemContainers.forEach(function (container) {
        container.style.display = shouldShow ? 'block' : 'none';
    });

    button.textContent = shouldShow ? 'Hide All Items' : 'Show All Items';

    // If we're showing items, retry conflict checking for this finished good
    if (shouldShow) {
        setTimeout(() => {
            retryConflictCheckForFinishedGood(finishedGoodId);
        }, 100);
    }
}

function togglePalletItems(button) {
    const palletId = button.dataset.palletId;
    const card = button.closest('.card');
    const palletItemsContainer = card.querySelector('.pallet-items');

    if (!palletItemsContainer) return;

    const isVisible = palletItemsContainer.style.display !== 'none';
    const shouldShow = !isVisible;

    // Toggle visibility
    palletItemsContainer.style.display = shouldShow ? 'block' : 'none';
    button.textContent = shouldShow ? 'Hide Items' : 'Show Items';
}

// Update receive checkbox state based on pallet/item selections
function updateReceiveCheckboxState(changedCheckbox) {
    const receiveId = changedCheckbox.dataset.receiveId;
    const row = changedCheckbox.closest('tr');
    const receiveCheckbox = row.querySelector('.receive-checkbox');

    if (!receiveCheckbox) return;

    const palletCheckboxes = row.querySelectorAll('.pallet-checkbox:not([data-released="true"])');
    const itemCheckboxes = row.querySelectorAll('.item-checkbox:not([data-released="true"])');

    const checkedPallets = Array.from(palletCheckboxes).filter(cb => cb.checked);
    const checkedItems = Array.from(itemCheckboxes).filter(cb => cb.checked);

    const hasSelections = checkedPallets.length > 0 || checkedItems.length > 0;
    const allSelected = checkedPallets.length === palletCheckboxes.length && checkedItems.length === itemCheckboxes.length;

    // FIX: Properly set checkbox state without indeterminate
    if (hasSelections) {
        receiveCheckbox.checked = true;
        receiveCheckbox.indeterminate = false;
    } else {
        receiveCheckbox.checked = false;
        receiveCheckbox.indeterminate = false;
    }

    console.log(`📋 Updated receive checkbox - Checked: ${receiveCheckbox.checked}, Indeterminate: ${receiveCheckbox.indeterminate}`);
}

// Include/exclude finished good
function toggleFinishedGoodInclusion(finishedGoodId, isIncluded) {
    const content = document.getElementById('content-' + finishedGoodId);
    const checkboxes = content.querySelectorAll('input[type="checkbox"]');

    if (isIncluded) {
        content.style.opacity = '1';
        content.style.pointerEvents = 'auto';
    } else {
        content.style.opacity = '0.5';
        content.style.pointerEvents = 'none';

        // Uncheck all checkboxes in this finished good
        checkboxes.forEach(function (cb) {
            if (!cb.disabled) {
                cb.checked = false;
                cb.dispatchEvent(new Event('change'));
            }
        });
    }

    updateStep2Buttons();
}

// Update Step 2 navigation
function updateStep2Buttons() {
    // Check if any items are selected across all finished goods
    const hasSelections = document.querySelectorAll('.finishedgood-include-checkbox:checked').length > 0 &&
        (document.querySelectorAll('.pallet-checkbox:checked:not([data-released="true"])').length > 0 ||
            document.querySelectorAll('.item-checkbox:checked:not([data-released="true"])').length > 0);

    document.getElementById('next-to-step-3').disabled = !hasSelections;
}

// Apply global date/remarks
function applyGlobalSettings() {
    const globalDate = document.getElementById('global-release-date').value;
    const globalRemarks = document.getElementById('job-remarks').value;

    if (!globalDate) {
        toastr.warning('Please set a global release date first');
        return;
    }

    // Apply release date to all selected receives
    const selectedReceives = document.querySelectorAll('.receive-checkbox:checked');
    selectedReceives.forEach(function (receiveCheckbox) {
        const row = receiveCheckbox.closest('tr');
        // Note: In this implementation, we're using the global date for all releases
        // The actual release date will be set during data collection
    });

    window.globalJobRemarks = globalRemarks;
    toastr.success('Global settings applied to all selected items');
}
function checkConflictsForAllFinishedGoods(finishedGoodsData) {
    console.log('🔄 Starting conflict checks for all finished goods...');

    // First, expand all item lists to make sure DOM is ready
    setTimeout(() => {
        console.log('📂 Expanding all item lists first...');
        const toggleButtons = document.querySelectorAll('.toggle-all-items');
        toggleButtons.forEach(button => {
            if (button.textContent.includes('Show')) {
                button.click();
            }
        });

        // Then wait a bit more and start conflict checking
        setTimeout(() => {
            console.log('🔍 Starting conflict checks...');
            checkBatchFinishedGoodConflicts(finishedGoodsData);
        }, 1000);
    }, 500);
}

async function checkBatchFinishedGoodConflicts(finishedGoodsData) {
    try {
        const finishedGoodIds = finishedGoodsData.map(fg => fg.id);
        console.log('🚀 Checking conflicts for finished goods:', finishedGoodIds);

        const response = await fetch('/FinishedGood/CheckBatchFinishedGoodReleaseConflicts', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'X-CSRF-TOKEN': $('input[name="__RequestVerificationToken"]').val(),
                'X-Requested-With': 'XMLHttpRequest'
            },
            body: JSON.stringify(finishedGoodIds)  // Send array directly
        });

        if (!response.ok) {
            console.error('❌ Batch conflict request failed:', response.status);
            return;
        }

        const data = await response.json();

        // FIX: Use data.conflicts instead of data.data
        if (data.success && data.conflicts) {
            console.log('✅ Batch conflict check completed successfully');
            console.log('📊 Raw conflict data:', data.conflicts);

            // Store conflict data and apply visual indicators for all finished goods
            let totalConflicts = 0;
            Object.keys(data.conflicts).forEach(finishedGoodId => {
                const finishedGoodConflicts = data.conflicts[finishedGoodId];
                globalConflictData[finishedGoodId] = finishedGoodConflicts;

                // FIX: Access the new structure - pallets and items are direct properties
                const palletConflictCount = Object.keys(finishedGoodConflicts.pallets || {}).length;
                const itemConflictCount = Object.keys(finishedGoodConflicts.items || {}).length;

                if (palletConflictCount > 0 || itemConflictCount > 0) {
                    console.log(`🔥 Finished Good ${finishedGoodId}: ${palletConflictCount} pallet conflicts, ${itemConflictCount} item conflicts`);
                    totalConflicts += palletConflictCount + itemConflictCount;

                    // Apply visual indicators
                    applyVisualConflictIndicators(finishedGoodId);
                } else {
                    console.log(`✅ Finished Good ${finishedGoodId}: No conflicts detected`);
                }
            });

            if (totalConflicts > 0) {
                console.log(`⚠️ Total conflicts detected: ${totalConflicts}`);
                showConflictLegend();
            } else {
                console.log('🎉 No conflicts detected across all finished goods!');
            }
        } else {
            console.log('❌ No conflict data returned from server');
            console.log('📊 Full response:', data);
        }
    } catch (error) {
        console.error('❌ Error in batch conflict checking:', error);
    }
}

async function checkReleaseConflictsForFinishedGood(finishedGoodId) {
    console.log('⚠️ Using individual finished good conflict check for:', finishedGoodId);

    try {
        const response = await fetch('/FinishedGood/CheckFinishedGoodReleaseConflicts', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'X-CSRF-TOKEN': $('input[name="__RequestVerificationToken"]').val(),
                'X-Requested-With': 'XMLHttpRequest'
            },
            body: JSON.stringify({ finishedGoodId: finishedGoodId })
        });

        if (!response.ok) {
            console.error('❌ Individual conflict request failed:', response.status);
            return;
        }

        const data = await response.json();

        if (data.success && data.conflicts) {
            console.log('✅ Individual conflict check completed for:', finishedGoodId);
            console.log('📊 Individual conflict data:', data.conflicts);

            // Store conflict data
            globalConflictData[finishedGoodId] = data.conflicts;

            // Apply visual indicators
            applyVisualConflictIndicators(finishedGoodId);

            // FIX: Use the new structure
            const palletConflictCount = Object.keys(data.conflicts.pallets || {}).length;
            const itemConflictCount = Object.keys(data.conflicts.items || {}).length;

            if (palletConflictCount > 0 || itemConflictCount > 0) {
                showConflictLegend();
            }
        }
    } catch (error) {
        console.error('❌ Error in individual conflict checking:', error);
    }
}

function applyVisualConflictIndicators(finishedGoodId) {
    console.log('🎨 Applying visual conflict indicators for finished good:', finishedGoodId);

    const conflicts = globalConflictData[finishedGoodId];
    if (!conflicts) {
        console.log('❌ No conflict data found for finished good:', finishedGoodId);
        return;
    }

    console.log('📊 Conflict data structure:', conflicts);

    // FIX: Use the new structure - conflicts.pallets and conflicts.items
    // Apply pallet conflicts
    if (conflicts.pallets) {
        console.log('🎯 Processing pallet conflicts:', Object.keys(conflicts.pallets));
        Object.keys(conflicts.pallets).forEach(palletId => {
            const conflict = conflicts.pallets[palletId];
            const palletCheckbox = document.querySelector(`.pallet-checkbox[data-pallet-id="${palletId}"]`);

            if (palletCheckbox) {
                console.log('🚨 Applying pallet conflict for:', palletId, conflict);
                applyPalletConflictStyling(palletCheckbox, conflict);
            } else {
                console.log('❌ Pallet checkbox not found for:', palletId);
            }
        });
    }

    // Apply item conflicts
    if (conflicts.items) {
        console.log('🎯 Processing item conflicts:', Object.keys(conflicts.items));
        Object.keys(conflicts.items).forEach(itemId => {
            const conflict = conflicts.items[itemId];
            const itemCheckbox = document.querySelector(`.item-checkbox[data-item-id="${itemId}"]`);

            if (itemCheckbox) {
                console.log('🚨 Applying item conflict for:', itemId, conflict);
                applyItemConflictStyling(itemCheckbox, conflict);
            } else {
                console.log('❌ Item checkbox not found for:', itemId);
            }
        });
    }

    console.log('✅ Conflict indicators applied for finished good:', finishedGoodId);
}

function applyPalletConflictStyling(palletCheckbox, conflict) {
    console.log('🚨 Applying pallet conflict styling:', conflict);

    const label = palletCheckbox.closest('.form-check').querySelector('label');
    const palletCard = palletCheckbox.closest('.card');

    if (!label || !palletCard) {
        console.log('❌ Could not find label or card elements');
        return;
    }

    // Add visual styling classes
    label.classList.add('conflict-highlighted');
    palletCard.classList.add('pallet-conflict');

    // FIX: Check if this is an already released item
    const isAlreadyReleased = palletCheckbox.dataset.released === 'true';
    console.log(`🔍 Pallet released status: ${palletCheckbox.dataset.released}, isAlreadyReleased: ${isAlreadyReleased}`);

    if (isAlreadyReleased) {
        // For already released items: keep them checked but disable interaction
        palletCheckbox.checked = true;  // Keep checked
        palletCheckbox.disabled = true; // Keep disabled
        console.log('🔒 Pallet is already released - keeping checked and disabled');
    } else {
        // For conflict items: uncheck and disable
        palletCheckbox.checked = false;
        palletCheckbox.disabled = true;
        console.log('⚠️ Pallet has conflict - unchecking and disabling');
    }

    // Make it non-interactive regardless
    palletCheckbox.style.pointerEvents = 'none';

    // Also disable the label to prevent clicking
    label.style.pointerEvents = 'none';
    label.style.cursor = 'not-allowed';

    // Add tooltip
    const tooltipText = getConflictTooltipText(conflict);
    addTooltip(label, tooltipText);

    console.log('✅ Applied pallet conflict styling successfully');
}

function applyItemConflictStyling(itemCheckbox, conflict) {
    console.log('🚨 Applying item conflict styling:', conflict);

    const label = itemCheckbox.closest('.form-check').querySelector('label');
    const listItem = itemCheckbox.closest('li');

    if (!label || !listItem) {
        console.log('❌ Could not find label or list item elements');
        return;
    }

    // Add visual styling classes
    label.classList.add('conflict-highlighted');
    listItem.classList.add('item-conflict');

    // Check if this is an already released item
    const isAlreadyReleased = itemCheckbox.dataset.released === 'true';
    console.log(`🔍 Item released status: ${itemCheckbox.dataset.released}, isAlreadyReleased: ${isAlreadyReleased}`);

    if (isAlreadyReleased) {
        // For already released items: keep them checked but disable interaction
        itemCheckbox.checked = true;  // Keep checked
        itemCheckbox.disabled = true; // Keep disabled
        console.log('🔒 Item is already released - keeping checked and disabled');
    } else {
        // For conflict items: uncheck and disable
        itemCheckbox.checked = false;
        itemCheckbox.disabled = true;
        console.log('⚠️ Item has conflict - unchecking and disabling');
    }

    // Make it non-interactive regardless
    itemCheckbox.style.pointerEvents = 'none';

    // Also disable the label to prevent clicking
    label.style.pointerEvents = 'none';
    label.style.cursor = 'not-allowed';

    // Add tooltip
    const tooltipText = getConflictTooltipText(conflict);
    addTooltip(label, tooltipText);

    console.log('✅ Applied item conflict styling successfully');
}

function retryConflictCheckForFinishedGood(finishedGoodId) {
    console.log('♻️ Retrying conflict check for finished good:', finishedGoodId);

    const finishedGoodCard = document.querySelector(`[data-finishedgood-id="${finishedGoodId}"]`);
    if (finishedGoodCard) {
        // Show all items first if they're not already shown
        const toggleButtons = finishedGoodCard.querySelectorAll('.toggle-all-items');
        toggleButtons.forEach(button => {
            if (button.textContent.includes('Show')) {
                button.click();
            }
        });
    }

    // Wait for DOM to update, then apply styling
    setTimeout(() => {
        if (globalConflictData[finishedGoodId]) {
            console.log('♻️ Using cached conflict data');
            applyVisualConflictIndicators(finishedGoodId);
        } else {
            console.log('🔍 Re-fetching conflict data');
            checkReleaseConflictsForFinishedGood(finishedGoodId);
        }
    }, 200);
}

function getConflictTooltipText(conflict) {
    switch (conflict.type) {
        case 'EntirePalletScheduled':
            return `This pallet is already scheduled for release in Job ${conflict.jobId || 'N/A'}`;
        case 'ItemScheduled':
            return `This item is already scheduled for release in Job ${conflict.jobId || 'N/A'}`;
        case 'ParentPalletScheduled':
            return `The parent pallet is scheduled for release, this item is unavailable`;
        default:
            return 'This item/pallet is already scheduled for release';
    }
}
function debugConflictData(finishedGoodId) {
    console.log('🔍 DEBUG: Conflict data for finished good:', finishedGoodId);

    const conflicts = globalConflictData[finishedGoodId];
    if (!conflicts) {
        console.log('❌ No conflict data stored');
        return;
    }

    console.log('📊 Stored conflict data:', conflicts);
    console.log('📊 Pallets conflicts:', conflicts.pallets);
    console.log('📊 Items conflicts:', conflicts.items);

    if (conflicts.pallets) {
        Object.keys(conflicts.pallets).forEach(palletId => {
            console.log(`🎯 Pallet ${palletId}:`, conflicts.pallets[palletId]);
            const checkbox = document.querySelector(`.pallet-checkbox[data-pallet-id="${palletId}"]`);
            console.log(`🎯 Checkbox found:`, !!checkbox);
        });
    }

    if (conflicts.items) {
        Object.keys(conflicts.items).forEach(itemId => {
            console.log(`🎯 Item ${itemId}:`, conflicts.items[itemId]);
            const checkbox = document.querySelector(`.item-checkbox[data-item-id="${itemId}"]`);
            console.log(`🎯 Checkbox found:`, !!checkbox);
        });
    }
}
function addTooltip(element, text) {
    element.setAttribute('title', text);
    element.setAttribute('data-bs-toggle', 'tooltip');
    element.setAttribute('data-bs-placement', 'top');

    // Initialize Bootstrap tooltip if available
    if (typeof bootstrap !== 'undefined' && bootstrap.Tooltip) {
        new bootstrap.Tooltip(element);
    }
}

function showConflictLegend() {
    const legend = document.getElementById('conflict-legend');
    if (legend) {
        legend.style.display = 'block';
        console.log('📋 Conflict legend displayed');
    }
}

function loadReviewSummary() {
    console.log('📊 Loading review summary...');

    // Collect all selections for review
    const summary = collectJobReleaseData();

    // Update summary counts
    document.getElementById('summary-finishedgood-count').textContent = summary.finishedGoodCount;
    document.getElementById('summary-pallet-count').textContent = summary.totalPallets;
    document.getElementById('summary-item-count').textContent = summary.totalItems;
    document.getElementById('summary-release-date').textContent = summary.releaseDate || 'Not set';

    // Generate detailed review
    const reviewContainer = document.getElementById('release-review-details');
    reviewContainer.innerHTML = generateDetailedReview(summary);

    console.log('✅ Review summary loaded');
}

function collectJobReleaseData() {
    const jobData = {
        finishedGoodCount: 0,
        totalPallets: 0,
        totalItems: 0,
        releaseDate: null,
        finishedGoods: []
    };

    // Get all included finished goods
    const includedFinishedGoods = document.querySelectorAll('.finishedgood-include-checkbox:checked');
    jobData.finishedGoodCount = includedFinishedGoods.length;

    // Use the global release date
    const globalReleaseDate = document.getElementById('global-release-date').value;
    jobData.releaseDate = globalReleaseDate;

    includedFinishedGoods.forEach(function (finishedGoodCheckbox) {
        const finishedGoodId = finishedGoodCheckbox.dataset.finishedgoodId;
        const finishedGoodCard = finishedGoodCheckbox.closest('.finishedgood-config-card');

        // Get sku from h5 element
        const headerText = finishedGoodCard.querySelector('h5').textContent.split(' - ')[0];

        const finishedGoodData = {
            finishedGoodId: finishedGoodId,
            sku: headerText,
            receives: []
        };

        // Get selected receives for this finished good
        const selectedReceives = finishedGoodCard.querySelectorAll('.receive-checkbox:checked');

        selectedReceives.forEach(function (receiveCheckbox) {
            const receiveId = receiveCheckbox.dataset.receiveId;
            const row = receiveCheckbox.closest('tr');
            const releaseType = row.querySelector('.release-type').value;

            const receiveData = {
                receiveId: receiveId,
                releaseType: releaseType,
                releaseDate: globalReleaseDate,
                pallets: [],
                items: []
            };

            // Count selected pallets and items
            const selectedPallets = row.querySelectorAll('.pallet-checkbox:checked:not([data-released="true"])');
            const selectedItems = row.querySelectorAll('.item-checkbox:checked:not([data-released="true"])');

            jobData.totalPallets += selectedPallets.length;

            selectedPallets.forEach(function (palletCb) {
                const palletCard = palletCb.closest('.card');
                const palletCodeElement = palletCard.querySelector('label strong');
                receiveData.pallets.push({
                    palletId: palletCb.dataset.palletId,
                    palletCode: palletCodeElement ? palletCodeElement.textContent : 'Unknown',
                    code: palletCodeElement ? palletCodeElement.textContent : 'Unknown' // Add 'code' for backward compatibility
                });
            });

            selectedItems.forEach(function (itemCb) {
                // Skip items that are part of selected pallets
                const palletId = itemCb.dataset.palletId;
                const isPalletSelected = Array.from(selectedPallets).some(function (p) {
                    return p.dataset.palletId === palletId;
                });

                if (!isPalletSelected) {
                    jobData.totalItems += 1;
                    const itemLabel = itemCb.closest('.form-check').querySelector('label');
                    receiveData.items.push({
                        itemId: itemCb.dataset.itemId,
                        itemCode: itemLabel ? itemLabel.textContent : 'Unknown',
                        code: itemLabel ? itemLabel.textContent : 'Unknown' // Add 'code' for backward compatibility
                    });
                }
            });

            if (receiveData.pallets.length > 0 || receiveData.items.length > 0) {
                finishedGoodData.receives.push(receiveData);
            }
        });

        if (finishedGoodData.receives.length > 0) {
            jobData.finishedGoods.push(finishedGoodData);
        }
    });

    return jobData;
}

function generateDetailedReview(summary) {
    if (summary.finishedGoods.length === 0) {
        return '<div class="alert alert-warning"><iconify-icon icon="lucide:alert-triangle" class="me-2"></iconify-icon>No finished goods or items selected for release.</div>';
    }

    let html = '<div class="job-review-summary">';

    // Add validation warnings
    const warnings = validateJobRelease(summary);
    if (warnings.length > 0) {
        html += '<div class="alert alert-warning mb-3"><h6 class="mb-2"><iconify-icon icon="lucide:alert-triangle" class="me-2"></iconify-icon>Please Review:</h6><ul class="mb-0">';
        warnings.forEach(function (warning) {
            html += '<li>' + warning + '</li>';
        });
        html += '</ul></div>';
    }

    // Job overview
    html += `
        <div class="alert alert-info mb-3">
            <h6 class="mb-2"><iconify-icon icon="lucide:info" class="me-2"></iconify-icon>Job Overview:</h6>
            <div class="row">
                <div class="col-md-3"><strong>Finished Goods:</strong> ${summary.finishedGoodCount}</div>
                <div class="col-md-3"><strong>Total Pallets:</strong> ${summary.totalPallets}</div>
                <div class="col-md-3"><strong>Individual Items:</strong> ${summary.totalItems}</div>
                <div class="col-md-3"><strong>Release Date:</strong> ${summary.releaseDate ? new Date(summary.releaseDate).toLocaleDateString() : 'Not set'}</div>
            </div>
        </div>
    `;

    // Finished good breakdown - Enhanced to match raw material module
    summary.finishedGoods.forEach(function (finishedGood, index) {
        html += `
            <div class="finishedgood-review-card mb-3">
                <div class="card border-primary">
                    <div class="card-header bg-primary text-white">
                        <h6 class="mb-0">
                            <iconify-icon icon="lucide:package" class="me-2"></iconify-icon>
                            ${finishedGood.sku}
                        </h6>
                    </div>
                    <div class="card-body">
        `;

        let finishedGoodPallets = 0;
        let finishedGoodItems = 0;

        finishedGood.receives.forEach(function (receive, receiveIndex) {
            finishedGoodPallets += receive.pallets.length;
            finishedGoodItems += receive.items.length;

            html += `
                <div class="receive-review mb-3 ${receiveIndex > 0 ? 'border-top pt-3' : ''}">
                    <div class="d-flex justify-content-between align-items-start mb-2">
                        <h6 class="text-primary mb-0">
                            <iconify-icon icon="lucide:calendar" class="me-1"></iconify-icon>
                            Batch Release #${receiveIndex + 1}
                        </h6>
                        <span class="badge bg-${receive.releaseType === 'Full' ? 'success' : 'warning'}">${receive.releaseType}</span>
                    </div>
                    <div class="row text-sm">
                        <div class="col-md-6">
                            <strong>Release Date:</strong> ${receive.releaseDate ? new Date(receive.releaseDate).toLocaleDateString() : 'Not set'}
                        </div>
                        <div class="col-md-6">
                            <strong>Items:</strong> ${receive.pallets.length} pallet(s), ${receive.items.length} individual item(s)
                        </div>
                    </div>
            `;

            // Show pallet details with enhanced formatting
            if (receive.pallets.length > 0) {
                html += '<div class="mt-2"><strong>Pallets:</strong> ';
                html += receive.pallets.map(function (pallet) {
                    return '<span class="badge bg-secondary me-1">' + (pallet.palletCode || pallet.code || 'Unknown') + '</span>';
                }).join('');
                html += '</div>';
            }

            // Show individual item details with enhanced formatting
            if (receive.items.length > 0) {
                html += '<div class="mt-2"><strong>Individual Items:</strong> ';
                html += receive.items.map(function (item) {
                    return '<span class="badge bg-light text-dark me-1">' + (item.itemCode || item.code || 'Unknown') + '</span>';
                }).join('');
                html += '</div>';
            }

            html += '</div>';
        });

        html += `
                        <div class="mt-3 pt-3 border-top">
                            <div class="row text-muted small">
                                <div class="col-md-6">
                                    <strong>Finished Good Total:</strong> ${finishedGoodPallets} pallets, ${finishedGoodItems} items
                                </div>
                                <div class="col-md-6">
                                    <strong>Receives:</strong> ${finishedGood.receives.length}
                                </div>
                            </div>
                        </div>
                    </div>
                </div>
            </div>
        `;
    });

    html += '</div>';
    return html;
}

function validateJobRelease(jobReleaseData) {
    const warnings = [];

    // Check if release date is set
    if (!jobReleaseData.releaseDate) {
        warnings.push('Please set a global release date in Step 2.');
    }

    // Check if release date is in the past
    if (jobReleaseData.releaseDate) {
        const releaseDate = new Date(jobReleaseData.releaseDate);
        const today = new Date();
        today.setHours(0, 0, 0, 0);
        releaseDate.setHours(0, 0, 0, 0);

        if (releaseDate < today) {
            warnings.push('Release date is in the past. Please select a future date.');
        }
    }

    // Check if any finished goods have no receives selected
    const finishedGoodsWithoutReceives = jobReleaseData.finishedGoods.filter(function (finishedGood) {
        return finishedGood.receives.length === 0;
    });

    if (finishedGoodsWithoutReceives.length > 0) {
        warnings.push('Some finished goods have no receives selected: ' +
            finishedGoodsWithoutReceives.map(function (fg) { return fg.sku; }).join(', '));
    }

    // Check total items
    if (jobReleaseData.totalPallets === 0 && jobReleaseData.totalItems === 0) {
        warnings.push('No pallets or items selected for release.');
    }

    return warnings;
}

function validateAndProceedToStep3() {
    const button = document.getElementById('next-to-step-3');
    const originalText = button.innerHTML;

    // Show loading state
    button.innerHTML = '<iconify-icon icon="mdi:loading" class="animate-spin"></iconify-icon> Validating...';
    button.disabled = true;

    // Collect current job data for validation
    const jobData = collectJobReleaseData();

    if (jobData.finishedGoods.length === 0) {
        toastr.warning('Please configure at least one finished good for release.');
        button.innerHTML = originalText;
        button.disabled = false;
        return;
    }

    // Call validation endpoint
    fetch('/FinishedGood/ValidateJobReleaseConflicts', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
            'X-CSRF-TOKEN': $('input[name="__RequestVerificationToken"]').val(),
            'X-Requested-With': 'XMLHttpRequest'
        },
        body: JSON.stringify({
            finishedGoods: jobData.finishedGoods
        })
    })
        .then(function (response) {
            if (!response.ok) {
                throw new Error('Validation request failed');
            }
            return response.json();
        })
        .then(function (data) {
            if (data.success) {
                // No conflicts, proceed to step 3
                showStep(3);
                toastr.success('Validation passed! Ready for review.');
            } else {
                // Show conflicts to user
                showJobReleaseConflicts(data.conflicts || []);
                toastr.error('Conflicts detected. Please resolve before proceeding.');
            }
        })
        .catch(function (error) {
            console.error('Validation error:', error);
            toastr.error('Failed to validate job release. Please try again.');
        })
        .finally(function () {
            // Restore button state
            button.innerHTML = originalText;
            button.disabled = false;
        });
}

function showJobReleaseConflicts(conflicts) {
    if (!conflicts || conflicts.length === 0) {
        return;
    }

    let conflictHtml = `
        <div class="alert alert-danger conflict-alert">
            <h6><iconify-icon icon="lucide:alert-triangle" class="me-2"></iconify-icon>Job Release Conflicts Detected</h6>
            <p>The following conflicts prevent proceeding to review. Please remove conflicting items/pallets:</p>
            <ul class="mb-0">
    `;

    conflicts.forEach(function (conflict) {
        let conflictDescription = '';
        switch (conflict.conflictType) {
            case 'EntirePalletAlreadyScheduled':
                conflictDescription = `Entire pallet ${conflict.palletCode} is already scheduled in another job`;
                break;
            case 'IndividualItemsAlreadyScheduled':
                conflictDescription = `Items ${conflict.conflictingItems.join(', ')} from pallet ${conflict.palletCode} are already scheduled`;
                break;
            case 'IndividualItemAlreadyScheduled':
                conflictDescription = `Item ${conflict.conflictingItems[0]} is already scheduled in another job`;
                break;
            default:
                conflictDescription = `Conflict detected for pallet ${conflict.palletCode}`;
        }

        conflictHtml += `<li><strong>${conflict.sku}:</strong> ${conflictDescription}</li>`;
    });

    conflictHtml += `
            </ul>
        </div>
    `;

    // Show conflicts at the top of step 2
    const step2Container = document.getElementById('step-2');
    const existingAlert = step2Container.querySelector('.conflict-alert');
    if (existingAlert) {
        existingAlert.remove();
    }

    const stepHeader = step2Container.querySelector('.step-header');
    stepHeader.insertAdjacentHTML('afterend', conflictHtml);

    // Scroll to conflicts
    step2Container.querySelector('.conflict-alert').scrollIntoView({
        behavior: 'smooth',
        block: 'start'
    });
}
function submitJobRelease() {
    const submitButton = document.getElementById('submit-job-release');
    const originalText = submitButton.innerHTML;

    // Collect and validate job release data
    const jobReleaseData = collectJobReleaseData();

    // Validate before submission
    const warnings = validateJobRelease(jobReleaseData);
    if (warnings.length > 0) {
        let warningMessage = 'Please fix the following issues before submitting:\n\n';
        warnings.forEach(function (warning, index) {
            warningMessage += (index + 1) + '. ' + warning + '\n';
        });

        toastr.error('Validation failed. Please check the review section.');
        alert(warningMessage);
        return;
    }

    // Show loading state
    submitButton.innerHTML = '<iconify-icon icon="mdi:loading" class="animate-spin"></iconify-icon> Creating Job Release...';
    submitButton.disabled = true;

    // Add global job remarks
    jobReleaseData.jobRemarks = window.globalJobRemarks || '';

    // Prepare submission payload
    const payload = {
        finishedGoods: jobReleaseData.finishedGoods,
        jobRemarks: jobReleaseData.jobRemarks
    };

    console.log('Submitting job release:', payload);

    // Submit to backend
    fetch('/FinishedGood/SubmitJobRelease', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
            'X-CSRF-TOKEN': $('input[name="__RequestVerificationToken"]').val(),
            'X-Requested-With': 'XMLHttpRequest'
        },
        body: JSON.stringify(payload)
    })
        .then(function (response) {
            if (!response.ok) {
                return response.json().then(function (data) {
                    return Promise.reject(data);
                });
            }
            return response.json();
        })
        .then(function (data) {
            if (data.success) {
                toastr.success('Job release created successfully!');
                sessionStorage.setItem('jobReleaseSuccessMessage', 'Job release created successfully! Total: ' +
                    jobReleaseData.finishedGoodCount + ' finished goods, ' +
                    jobReleaseData.totalPallets + ' pallets, ' +
                    jobReleaseData.totalItems + ' items.');

                // Small delay to show success message before redirect
                setTimeout(function () {
                    window.location.href = '/FinishedGood/JobReleases';
                }, 1000);
            } else {
                throw new Error(data.message || 'Failed to create job release');
            }
        })
        .catch(function (error) {
            console.error('Error submitting job release:', error);
            toastr.error(error.message || 'Failed to create job release');

            // Reset button
            submitButton.innerHTML = originalText;
            submitButton.disabled = false;
        });
}


//DEBUG
function debugCheckboxStates() {
    console.log('🔍 === CHECKBOX STATES DEBUG ===');

    const receiveCheckboxes = document.querySelectorAll('.receive-checkbox');
    receiveCheckboxes.forEach((checkbox, index) => {
        const receiveId = checkbox.dataset.receiveId;
        console.log(`Receive ${index + 1} (${receiveId}):`);
        console.log(`  ✅ Checked: ${checkbox.checked}`);
        console.log(`  ➖ Indeterminate: ${checkbox.indeterminate}`);
        console.log(`  🔒 Disabled: ${checkbox.disabled}`);

        const row = checkbox.closest('tr');
        if (row) {
            const pallets = row.querySelectorAll('.pallet-checkbox:checked');
            const items = row.querySelectorAll('.item-checkbox:checked');
            console.log(`  📦 Selected pallets: ${pallets.length}`);
            console.log(`  📋 Selected items: ${items.length}`);
        }
    });

    return {
        totalReceiveCheckboxes: receiveCheckboxes.length,
        checkedReceives: document.querySelectorAll('.receive-checkbox:checked').length,
        indeterminateReceives: Array.from(receiveCheckboxes).filter(cb => cb.indeterminate).length
    };
}
function debugDataCollectionDetailed() {
    console.log('🔍 === DETAILED DATA COLLECTION DEBUG ===');

    // 1. Check finished good include checkboxes
    const includeCheckboxes = document.querySelectorAll('.finishedgood-include-checkbox:checked');
    console.log(`🔍 Include checkboxes checked: ${includeCheckboxes.length}`);

    includeCheckboxes.forEach((cb, index) => {
        const finishedGoodId = cb.dataset.finishedgoodId;
        console.log(`  ${index + 1}: ID = ${finishedGoodId}`);

        // Find the card for this finished good
        const card = cb.closest('.finishedgood-config-card');
        if (card) {
            console.log(`    ✅ Found card for finished good ${finishedGoodId}`);

            // Check receives in this card
            const receives = card.querySelectorAll('.receive-checkbox');
            const selectedReceives = card.querySelectorAll('.receive-checkbox:checked');
            console.log(`    📋 Total receives: ${receives.length}, Selected: ${selectedReceives.length}`);

            selectedReceives.forEach((receiveCheckbox, receiveIndex) => {
                const receiveId = receiveCheckbox.dataset.receiveId;
                console.log(`      Receive ${receiveIndex + 1}: ID = ${receiveId}`);

                const row = receiveCheckbox.closest('tr');
                if (row) {
                    const pallets = row.querySelectorAll('.pallet-checkbox:checked:not([data-released="true"])');
                    const items = row.querySelectorAll('.item-checkbox:checked:not([data-released="true"])');
                    console.log(`        🎯 Pallets: ${pallets.length}, Items: ${items.length}`);

                    // Log individual items
                    items.forEach((item, itemIndex) => {
                        console.log(`          Item ${itemIndex + 1}: ID = ${item.dataset.itemId}`);
                    });
                } else {
                    console.log('        ❌ No row found for receive checkbox');
                }
            });
        } else {
            console.log(`    ❌ No card found for finished good ${finishedGoodId}`);
        }
    });

    // 2. Now test actual collection
    console.log('🔍 === RUNNING ACTUAL COLLECTION ===');
    const result = collectJobReleaseData();

    return result;
}
