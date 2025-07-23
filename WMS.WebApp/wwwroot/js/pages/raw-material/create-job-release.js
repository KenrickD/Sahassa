// Global variables
let selectedMaterials = new Set();
let availableMaterials = [];
let jobReleaseConfig = {};
let globalConflictData = {};
$(document).ready(function () {
    initializeCreateJobRelease();
});

function initializeCreateJobRelease() {
    // Load available materials
    loadAvailableMaterials();

    // Set default release date to tomorrow
    const tomorrow = new Date();
    tomorrow.setDate(tomorrow.getDate() + 1);
    document.getElementById('global-release-date').value = tomorrow.toISOString().split('T')[0];

    // Set up event listeners
    setupEventListeners();
}

function setupEventListeners() {
    // Step navigation
    document.getElementById('next-to-step-2').addEventListener('click', () => goToStep(2));
    document.getElementById('prev-to-step-1').addEventListener('click', () => goToStep(1));
    document.getElementById('next-to-step-3').addEventListener('click', () => goToStep(3));
    document.getElementById('prev-to-step-2').addEventListener('click', () => goToStep(2));

    // Material selection
    document.getElementById('select-all-materials').addEventListener('change', toggleAllMaterials);
    document.getElementById('material-search').addEventListener('input', filterMaterials);

    // Global settings
    document.getElementById('apply-global-settings').addEventListener('click', applyGlobalSettings);

    // Submit
    document.getElementById('submit-job-release').addEventListener('click', submitJobRelease);

    // Set up Step 2 event listeners (using event delegation)
    setupStep2EventListeners();
}

function loadAvailableMaterials() {
    // Show loading state
    const tbody = document.getElementById('materials-table-body');
    tbody.innerHTML = '<tr><td colspan="5" class="text-center">Loading materials...</td></tr>';

    fetch('/RawMaterial/GetAvailableMaterialsForJobRelease')
        .then(response => {
            if (!response.ok) {
                throw new Error('HTTP error! status: ' + response.status);
            }
            return response.json();
        })
        .then(data => {
            console.log('Loaded materials:', data);
            availableMaterials = data;
            renderMaterialsTable();
        })
        .catch(error => {
            console.error('Error loading materials:', error);
            tbody.innerHTML = '<tr><td colspan="5" class="text-center text-danger">Failed to load materials. Please refresh the page.</td></tr>';
            toastr.error('Failed to load available materials');
        });
}

function renderMaterialsTable() {
    const tbody = document.getElementById('materials-table-body');
    tbody.innerHTML = '';

    if (!availableMaterials || availableMaterials.length === 0) {
        tbody.innerHTML = '<tr><td colspan="5" class="text-center text-muted">No materials with available inventory found.</td></tr>';
        return;
    }

    availableMaterials.forEach(material => {
        const row = document.createElement('tr');
        row.className = 'material-row';
        row.dataset.materialId = material.id;

        const description = material.description || '-';

        row.innerHTML = `
            <td>
                <input type="checkbox" class="form-check-input material-checkbox"
                       data-material-id="${material.id}">
            </td>
            <td><strong>${material.materialNo}</strong></td>
            <td>${description}</td>
            <td class="text-center">${material.balanceQty}</td>
            <td class="text-center">${material.balancePallets}</td>
        `;

        // Add click handler for row
        row.addEventListener('click', function (e) {
            if (e.target.type !== 'checkbox') {
                const checkbox = row.querySelector('.material-checkbox');
                checkbox.checked = !checkbox.checked;
                checkbox.dispatchEvent(new Event('change'));
            }
        });

        // Add change handler for checkbox
        const checkbox = row.querySelector('.material-checkbox');
        checkbox.addEventListener('change', function () {
            toggleMaterialSelection(material.id, checkbox.checked);
        });

        tbody.appendChild(row);
    });
}

function toggleMaterialSelection(materialId, isSelected) {
    if (isSelected) {
        selectedMaterials.add(materialId);
    } else {
        selectedMaterials.delete(materialId);
    }

    updateMaterialSelectionUI();
    updateStepButtons();
}

function toggleAllMaterials() {
    const selectAllCheckbox = document.getElementById('select-all-materials');
    const materialCheckboxes = document.querySelectorAll('.material-checkbox');

    materialCheckboxes.forEach(function (checkbox) {
        checkbox.checked = selectAllCheckbox.checked;
        toggleMaterialSelection(checkbox.dataset.materialId, selectAllCheckbox.checked);
    });
}

function updateMaterialSelectionUI() {
    // Update selected count
    const count = selectedMaterials.size;
    document.getElementById('selected-count').textContent = count + ' material' + (count !== 1 ? 's' : '') + ' selected';

    // Update row styling
    document.querySelectorAll('.material-row').forEach(function (row) {
        const materialId = row.dataset.materialId;
        if (selectedMaterials.has(materialId)) {
            row.classList.add('selected');
        } else {
            row.classList.remove('selected');
        }
    });
}

function updateStepButtons() {
    // Enable/disable next button based on selection
    const nextButton = document.getElementById('next-to-step-2');
    nextButton.disabled = selectedMaterials.size === 0;
}

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
        loadMaterialConfigurations();
    } else if (stepNumber === 3) {
        loadReviewSummary();
    }
}

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
        loadMaterialConfigurations();
    } else if (stepNumber === 3) {
        loadReviewSummary();
    }
}

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

function filterMaterials() {
    const searchTerm = document.getElementById('material-search').value.toLowerCase();
    const rows = document.querySelectorAll('.material-row');

    rows.forEach(function (row) {
        const materialNo = row.children[1].textContent.toLowerCase();
        const description = row.children[2].textContent.toLowerCase();

        if (materialNo.includes(searchTerm) || description.includes(searchTerm)) {
            row.style.display = '';
        } else {
            row.style.display = 'none';
        }
    });
}

// Step 2 functions
function loadMaterialConfigurations() {
    const container = document.getElementById('selected-materials-container');
    container.innerHTML = '<div class="text-center p-4"><iconify-icon icon="mdi:loading" class="animate-spin"></iconify-icon> Loading material configurations...</div>';

    // Get selected material IDs
    const selectedMaterialIds = Array.from(selectedMaterials);

    if (selectedMaterialIds.length === 0) {
        container.innerHTML = '<div class="alert alert-warning">No materials selected.</div>';
        return;
    }

    // Fetch inventory data for selected materials
    fetch('/RawMaterial/GetMaterialInventoryForJobRelease', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
            'X-CSRF-TOKEN': $('input[name="__RequestVerificationToken"]').val()
        },
        body: JSON.stringify({ materialIds: selectedMaterialIds })
    })
        .then(response => {
            if (!response.ok) {
                throw new Error('HTTP error! status: ' + response.status);
            }
            return response.json();
        })
        .then(data => {
            console.log('Loaded material inventory:', data);
            renderMaterialConfigurations(data);
            updateStep2Buttons();
        })
        .catch(error => {
            console.error('Error loading material inventory:', error);
            container.innerHTML = '<div class="alert alert-danger">Failed to load material inventory. Please try again.</div>';
        });
}

function renderMaterialConfigurations(materialsData) {
    const container = document.getElementById('selected-materials-container');
    container.innerHTML = '';

    console.log('Rendering', materialsData.length, 'material configurations...');

    materialsData.forEach(function (material) {
        const materialCard = createMaterialConfigCard(material);
        container.appendChild(materialCard);
    });

    // Wait for DOM to be fully rendered, then check conflicts for all materials
    setTimeout(() => {
        console.log('Starting conflict check for all materials after DOM rendering...');
        checkConflictsForAllMaterials(materialsData);
    }, 1500); // Longer delay to ensure everything is rendered
}
function checkConflictsForAllMaterials(materialsData) {
    console.log('🔄 Starting conflict checks for all materials...');

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
            const promises = materialsData.map(material => {
                console.log('Checking conflicts for material:', material.id, '-', material.materialNo);
                return checkReleaseConflictsForMaterial(material.id);
            });

            Promise.all(promises).then(() => {
                console.log('✅ All conflict checks completed');
                showConflictLegend();
            }).catch(error => {
                console.error('❌ Error in batch conflict checking:', error);
            });
        }, 1000);
    }, 500);
}
async function checkBatchMaterialConflicts(materialsData) {
    try {
        const materialIds = materialsData.map(material => material.id);
        console.log('🚀 Checking conflicts for materials:', materialIds);

        const response = await fetch('/RawMaterial/CheckBatchMaterialReleaseConflicts', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'X-CSRF-TOKEN': $('input[name="__RequestVerificationToken"]').val(),
                'X-Requested-With': 'XMLHttpRequest'
            },
            body: JSON.stringify({ materialIds: materialIds })
        });

        if (!response.ok) {
            console.error('❌ Batch conflict request failed:', response.status);
            return;
        }

        const data = await response.json();

        if (data.success && data.conflicts) {
            console.log('✅ Batch conflict check completed successfully');

            // Store conflict data and apply visual indicators for all materials
            let totalConflicts = 0;
            Object.keys(data.conflicts).forEach(materialId => {
                const materialConflicts = data.conflicts[materialId];
                globalConflictData[materialId] = materialConflicts;

                const palletConflictCount = Object.keys(materialConflicts.pallets || {}).length;
                const itemConflictCount = Object.keys(materialConflicts.items || {}).length;

                if (palletConflictCount > 0 || itemConflictCount > 0) {
                    console.log(`🔥 Material ${materialId}: ${palletConflictCount} pallet conflicts, ${itemConflictCount} item conflicts`);
                    totalConflicts += palletConflictCount + itemConflictCount;

                    // Apply visual indicators
                    applyVisualConflictIndicators(materialId);
                }
            });

            if (totalConflicts > 0) {
                console.log(`🚨 Total conflicts found: ${totalConflicts}`);
                showConflictLegend();
            } else {
                console.log('✅ No conflicts found across all materials');
            }
        }
    } catch (error) {
        console.error('💥 Error in batch conflict check:', error);
    }
}
function createMaterialConfigCard(material) {
    const cardDiv = document.createElement('div');
    cardDiv.className = 'material-config-card';
    cardDiv.dataset.materialId = material.id;

    const description = material.description || 'No Description';

    cardDiv.innerHTML = `
        <div class="material-config-header">
            <div class="d-flex justify-content-between align-items-center w-100">
                <div>
                    <h5 class="mb-1">${material.materialNo} - ${description}</h5>
                    <small class="text-muted">Balance: ${material.totalBalanceQty} items, ${material.totalBalancePallet} pallets</small>
                </div>
                <div class="form-check">
                    <input type="checkbox" class="form-check-input material-include-checkbox"
                           id="include-${material.id}" data-material-id="${material.id}" checked>
                    <label class="form-check-label" for="include-${material.id}">Include in Release</label>
                </div>
            </div>
        </div>
        <div class="material-config-content" id="content-${material.id}">
            ${createMaterialReleaseTable(material)}
        </div>
    `;

    // Add event listeners
    const includeCheckbox = cardDiv.querySelector('.material-include-checkbox');
    includeCheckbox.addEventListener('change', function () {
        const content = cardDiv.querySelector('.material-config-content');
        content.style.display = this.checked ? 'block' : 'none';
        updateStep2Buttons();
    });

    // Don't check conflicts here - we'll do it later in batch
    return cardDiv;
}

function createMaterialReleaseTable(material) {
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
                ${material.receives.map(function (receive) { return createReceiveRow(material.id, receive); }).join('')}
            </tbody>
        </table>
    `;
}

function createReceiveRow(materialId, receive) {
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
        <tr data-material-id="${materialId}" data-receive-id="${receiveId}">
            <td class="text-center">
                <input type="checkbox" class="receive-checkbox"
                       data-material-id="${materialId}" data-receive-id="${receiveId}">
            </td>
            <td>
                <select class="form-select release-type"
                        data-material-id="${materialId}" data-receive-id="${receiveId}" disabled>
                    <option value="Partial">Partial</option>
                    <option value="Full">Full</option>
                </select>
            </td>
            <td>
                <div class="mb-2">
                    <button type="button" class="btn btn-link btn-sm p-0 toggle-all-items"
                            data-material-id="${materialId}" data-receive-id="${receiveId}">
                        Show All Items
                    </button>
                    <small class="text-muted ms-2">Batch: ${batchInfo} | Received: ${receivedDate}</small>
                </div>
                <div class="pallets-container">
                    ${receive.pallets.map(function (pallet) { return createPalletCard(materialId, receiveId, pallet); }).join('')}
                </div>
            </td>
        </tr>
    `;
}

function createPalletCard(materialId, receiveId, pallet) {
    if (pallet.isReleased && pallet.items.every(function (i) { return i.isReleased; })) {
        return ''; // Skip fully released pallets
    }

    return `
        <div class="card p-2 me-2 mb-2" style="min-width: 220px; max-width: 250px; display: inline-block;">
            <div class="d-flex justify-content-between align-items-center mb-1">
                <div class="form-check">
                    <input type="checkbox" class="form-check-input pallet-checkbox"
                           id="pallet-${pallet.id}"
                           data-material-id="${materialId}"
                           data-receive-id="${receiveId}"
                           data-pallet-id="${pallet.id}"
                           data-released="${pallet.isReleased.toString().toLowerCase()}"
                           ${pallet.isReleased ? 'checked disabled' : 'disabled'}>
                    <label class="form-check-label pallet-code-label" for="pallet-${pallet.id}">
                        <strong>${pallet.palletCode}</strong>
                    </label>
                </div>
            </div>
            <div class="pallet-items mt-2" style="display: none;">
                <div class="card-subtitle mb-2 text-muted small">Individual Items</div>
                <ul class="list-unstyled mb-0" style="max-height: 150px; overflow-y: auto;">
                    ${pallet.items.map(function (item) { return createItemCheckbox(materialId, receiveId, pallet.id, item); }).join('')}
                </ul>
            </div>
        </div>
    `;
}

function createItemCheckbox(materialId, receiveId, palletId, item) {
    return `
        <li class="mb-1">
            <div class="form-check">
                <input type="checkbox" class="form-check-input item-checkbox"
                       id="item-${item.id}"
                       data-material-id="${materialId}"
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

function setupStep2EventListeners() {
    // Set up event delegation for dynamically created elements
    const container = document.getElementById('selected-materials-container');

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
}

function handleReceiveCheckboxChange(checkbox) {
    const materialId = checkbox.dataset.materialId;
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
    } else {
        // Disable everything and uncheck
        releaseTypeSelect.disabled = true;

        palletCheckboxes.forEach(function (cb) {
            cb.checked = false;
            cb.disabled = true; // Disable when receive is unchecked
        });
        itemCheckboxes.forEach(function (cb) {
            cb.checked = false;
            cb.disabled = true; // Disable when receive is unchecked
        });

        // Hide all item containers
        row.querySelectorAll('.pallet-items').forEach(function (container) {
            container.style.display = 'none';
        });
    }

    updateStep2Buttons();
}

function handleReleaseTypeChange(select) {
    const materialId = select.dataset.materialId;
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
}

function handlePalletCheckboxChange(checkbox) {
    // Don't proceed if disabled (e.g., already released items)
    if (checkbox.disabled) return;

    // Check if the parent receive checkbox is checked
    const row = checkbox.closest('tr');
    const receiveCheckbox = row.querySelector('.receive-checkbox');
    if (!receiveCheckbox.checked) {
        // Prevent checking if receive is not selected
        checkbox.checked = false;
        return;
    }

    const palletId = checkbox.dataset.palletId;
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
        // Don't hide the container - user might want to select individual items
        itemCheckboxes.forEach(function (cb) {
            cb.checked = false;
            cb.disabled = false; // Re-enable individual items when pallet is deselected
        });
    }

    updateStep2Buttons();
}

function handleItemCheckboxChange(checkbox) {
    // Don't proceed if disabled
    if (checkbox.disabled) return;

    // Check if the parent receive checkbox is checked
    const row = checkbox.closest('tr');
    const receiveCheckbox = row.querySelector('.receive-checkbox');
    if (!receiveCheckbox.checked) {
        // Prevent checking if receive is not selected
        checkbox.checked = false;
        return;
    }

    updateStep2Buttons();
}

function toggleAllItemsForReceive(button) {
    const materialId = button.dataset.materialId;
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

    // If we're showing items, retry conflict checking for this material
    if (shouldShow) {
        setTimeout(() => {
            retryConflictCheckForMaterial(materialId);
        }, 100);
    }
}



function toggleMaterialInclusion(materialId, isIncluded) {
    const content = document.getElementById('content-' + materialId);
    const checkboxes = content.querySelectorAll('input[type="checkbox"]');

    if (isIncluded) {
        content.style.opacity = '1';
        content.style.pointerEvents = 'auto';
    } else {
        content.style.opacity = '0.5';
        content.style.pointerEvents = 'none';

        // Uncheck all checkboxes in this material
        checkboxes.forEach(function (cb) {
            if (!cb.disabled) {
                cb.checked = false;
                cb.dispatchEvent(new Event('change'));
            }
        });
    }

    updateStep2Buttons();
}

function updateStep2Buttons() {
    // Check if any items are selected across all materials
    const hasSelections = document.querySelectorAll('.material-include-checkbox:checked').length > 0 &&
        (document.querySelectorAll('.pallet-checkbox:checked:not([data-released="true"])').length > 0 ||
            document.querySelectorAll('.item-checkbox:checked:not([data-released="true"])').length > 0);

    document.getElementById('next-to-step-3').disabled = !hasSelections;
}

function applyGlobalSettings() {
    const globalDate = document.getElementById('global-release-date').value;
    const globalRemarks = document.getElementById('job-remarks').value;

    if (!globalDate) {
        toastr.warning('Please set a global release date first');
        return;
    }

    window.globalJobRemarks = globalRemarks;
    toastr.success('Global settings applied');
}

function loadReviewSummary() {
    // Collect all selections for review
    const summary = collectJobReleaseData();

    // Update summary counts
    document.getElementById('summary-material-count').textContent = summary.materialCount;
    document.getElementById('summary-pallet-count').textContent = summary.totalPallets;
    document.getElementById('summary-item-count').textContent = summary.totalItems;
    document.getElementById('summary-release-date').textContent = summary.releaseDate || 'Not set';

    // Generate detailed review
    const reviewContainer = document.getElementById('release-review-details');
    reviewContainer.innerHTML = generateDetailedReview(summary);
}

function collectJobReleaseData() {
    const jobData = {
        materialCount: 0,
        totalPallets: 0,
        totalItems: 0,
        releaseDate: null,
        materials: []
    };

    // Get all included materials
    const includedMaterials = document.querySelectorAll('.material-include-checkbox:checked');
    jobData.materialCount = includedMaterials.length;

    // Use the global release date
    const globalReleaseDate = document.getElementById('global-release-date').value;
    jobData.releaseDate = globalReleaseDate;

    includedMaterials.forEach(function (materialCheckbox) {
        const materialId = materialCheckbox.dataset.materialId;
        const materialCard = materialCheckbox.closest('.material-config-card');
        const materialNo = materialCard.querySelector('h5').textContent.split(' - ')[0];

        const materialData = {
            materialId: materialId,
            materialNo: materialNo,
            receives: []
        };

        // Get selected receives for this material
        const selectedReceives = materialCard.querySelectorAll('.receive-checkbox:checked');

        selectedReceives.forEach(function (receiveCheckbox) {
            const receiveId = receiveCheckbox.dataset.receiveId;
            const row = receiveCheckbox.closest('tr');
            const releaseType = row.querySelector('.release-type').value;

            const receiveData = {
                receiveId: receiveId,
                releaseType: releaseType,
                releaseDate: globalReleaseDate, // Use global date for all receives
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
                    palletCode: palletCodeElement ? palletCodeElement.textContent : 'Unknown'
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
                        itemCode: itemLabel ? itemLabel.textContent : 'Unknown'
                    });
                }
            });

            if (receiveData.pallets.length > 0 || receiveData.items.length > 0) {
                materialData.receives.push(receiveData);
            }
        });

        if (materialData.receives.length > 0) {
            jobData.materials.push(materialData);
        }
    });

    return jobData;
}

function generateDetailedReview(summary) {
    if (summary.materials.length === 0) {
        return '<div class="alert alert-warning"><iconify-icon icon="lucide:alert-triangle" class="me-2"></iconify-icon>No materials or items selected for release.</div>';
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
                <div class="col-md-3"><strong>Materials:</strong> ${summary.materialCount}</div>
                <div class="col-md-3"><strong>Total Pallets:</strong> ${summary.totalPallets}</div>
                <div class="col-md-3"><strong>Individual Items:</strong> ${summary.totalItems}</div>
                <div class="col-md-3"><strong>Release Date:</strong> ${summary.releaseDate ? new Date(summary.releaseDate).toLocaleDateString() : 'Not set'}</div>
            </div>
        </div>
    `;

    // Material breakdown
    summary.materials.forEach(function (material, index) {
        html += `
            <div class="material-review-card mb-3">
                <div class="card border-primary">
                    <div class="card-header bg-primary text-white">
                        <h6 class="mb-0">
                            <iconify-icon icon="lucide:package" class="me-2"></iconify-icon>
                            ${material.materialNo}
                        </h6>
                    </div>
                    <div class="card-body">
        `;

        let materialPallets = 0;
        let materialItems = 0;

        material.receives.forEach(function (receive, receiveIndex) {
            materialPallets += receive.pallets.length;
            materialItems += receive.items.length;

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

            // Show pallet details
            if (receive.pallets.length > 0) {
                html += '<div class="mt-2"><strong>Pallets:</strong> ';
                html += receive.pallets.map(function (pallet) {
                    return '<span class="badge bg-secondary me-1">' + pallet.palletCode + '</span>';
                }).join('');
                html += '</div>';
            }

            // Show individual item details
            if (receive.items.length > 0) {
                html += '<div class="mt-2"><strong>Individual Items:</strong> ';
                html += receive.items.map(function (item) {
                    return '<span class="badge bg-light text-dark me-1">' + item.itemCode + '</span>';
                }).join('');
                html += '</div>';
            }

            html += '</div>';
        });

        html += `
                        <div class="mt-3 pt-3 border-top">
                            <div class="row text-muted small">
                                <div class="col-md-6">
                                    <strong>Material Total:</strong> ${materialPallets} pallets, ${materialItems} items
                                </div>
                                <div class="col-md-6">
                                    <strong>Receives:</strong> ${material.receives.length}
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

function validateJobRelease(summary) {
    const warnings = [];

    // Check if release date is set
    if (!summary.releaseDate) {
        warnings.push('Release date is not set. Please set a global release date in Step 2.');
    }

    // Check if release date is in the past
    if (summary.releaseDate) {
        const releaseDate = new Date(summary.releaseDate);
        const today = new Date();
        today.setHours(0, 0, 0, 0);
        releaseDate.setHours(0, 0, 0, 0);

        if (releaseDate < today) {
            warnings.push('Release date is in the past. Please select a future date.');
        }
    }

    // Check if any materials have no receives selected
    const materialsWithoutReceives = summary.materials.filter(function (material) {
        return material.receives.length === 0;
    });

    if (materialsWithoutReceives.length > 0) {
        warnings.push('Some materials have no receives selected: ' +
            materialsWithoutReceives.map(function (m) { return m.materialNo; }).join(', '));
    }

    // Check total items
    if (summary.totalPallets === 0 && summary.totalItems === 0) {
        warnings.push('No pallets or items selected for release.');
    }

    return warnings;
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
        materials: jobReleaseData.materials,
        jobRemarks: jobReleaseData.jobRemarks
    };

    console.log('Submitting job release:', payload);

    // Submit to backend
    fetch('/RawMaterial/SubmitJobRelease', {
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
                    jobReleaseData.materialCount + ' materials, ' +
                    jobReleaseData.totalPallets + ' pallets, ' +
                    jobReleaseData.totalItems + ' items.');

                // Small delay to show success message before redirect
                setTimeout(function () {
                    window.location.href = '/RawMaterial/JobReleases';
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
function validateAndProceedToStep3() {
    const button = document.getElementById('next-to-step-3');
    const originalText = button.innerHTML;

    // Show loading state
    button.innerHTML = '<iconify-icon icon="mdi:loading" class="animate-spin"></iconify-icon> Validating...';
    button.disabled = true;

    // Collect current job data for validation
    const jobData = collectJobReleaseData();

    if (jobData.materials.length === 0) {
        toastr.warning('Please configure at least one material for release.');
        button.innerHTML = originalText;
        button.disabled = false;
        return;
    }

    // Call validation endpoint
    fetch('/RawMaterial/ValidateJobReleaseConflicts', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
            'X-CSRF-TOKEN': $('input[name="__RequestVerificationToken"]').val(),
            'X-Requested-With': 'XMLHttpRequest'
        },
        body: JSON.stringify({
            materials: jobData.materials
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

        conflictHtml += `<li><strong>${conflict.materialNo}:</strong> ${conflictDescription}</li>`;
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
// Function to check for release conflicts in real-time
async function checkReleaseConflictsForMaterial(materialId) {
    console.log('⚠️ Using deprecated single material conflict check. Consider using batch method.');

    try {
        const response = await fetch('/RawMaterial/CheckMaterialReleaseConflicts', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'X-CSRF-TOKEN': $('input[name="__RequestVerificationToken"]').val(),
                'X-Requested-With': 'XMLHttpRequest'
            },
            body: JSON.stringify({ materialId: materialId })
        });

        if (!response.ok) {
            console.error('❌ Request failed:', response.status);
            return;
        }

        const data = await response.json();

        if (data.success && data.conflicts) {
            globalConflictData[materialId] = data.conflicts;

            const palletConflictCount = Object.keys(data.conflicts.pallets || {}).length;
            const itemConflictCount = Object.keys(data.conflicts.items || {}).length;

            if (palletConflictCount > 0 || itemConflictCount > 0) {
                applyVisualConflictIndicators(materialId);
                showConflictLegend();
            }
        }
    } catch (error) {
        console.error('💥 Error in conflict check:', error);
    }
}
// Function to show the conflict legend
function showConflictLegend() {
    const legend = document.getElementById('conflict-legend');
    if (legend) {
        legend.style.display = 'block';
    }
}
// Function to apply visual indicators for conflicts
function applyVisualConflictIndicators(materialId) {
    console.log('🎨 Applying visual indicators for material:', materialId);

    // Find the correct material card - use the one with the material configuration content
    const materialCard = document.querySelector(`div.material-config-card[data-material-id="${materialId}"]`);
    if (!materialCard) {
        console.log('❌ Material configuration card not found for:', materialId);
        return;
    }

    const conflicts = globalConflictData[materialId];
    if (!conflicts) {
        console.log('❌ No conflicts data for material:', materialId);
        return;
    }

    // Apply pallet-level conflicts
    if (conflicts.pallets) {
        Object.keys(conflicts.pallets).forEach(palletId => {
            console.log('🎯 Applying styling to pallet:', palletId);

            // Find pallet checkbox within this specific material card
            const palletCheckbox = materialCard.querySelector(`input[data-pallet-id="${palletId}"]`);

            if (palletCheckbox) {
                console.log('✅ Found pallet checkbox, applying styling');
                const conflict = conflicts.pallets[palletId];
                applyPalletConflictStyling(palletCheckbox, conflict);
            } else {
                console.log('❌ Pallet checkbox not found for:', palletId);
            }
        });
    }

    // Apply item-level conflicts
    if (conflicts.items) {
        Object.keys(conflicts.items).forEach(itemId => {
            console.log('🎯 Applying styling to item:', itemId);

            // Find item checkbox within this specific material card
            const itemCheckbox = materialCard.querySelector(`input[data-item-id="${itemId}"]`);

            if (itemCheckbox) {
                console.log('✅ Found item checkbox, applying styling');
                const conflict = conflicts.items[itemId];
                applyItemConflictStyling(itemCheckbox, conflict);
            } else {
                console.log('❌ Item checkbox not found for:', itemId);
            }
        });
    }
}
function retryConflictCheckForMaterial(materialId) {
    console.log('🔄 Retrying conflict check for material:', materialId);

    // First ensure items are visible
    const materialCard = document.querySelector(`div.material-config-card[data-material-id="${materialId}"]`);
    if (materialCard) {
        const toggleButtons = materialCard.querySelectorAll('.toggle-all-items');
        toggleButtons.forEach(button => {
            if (button.textContent.includes('Show')) {
                button.click();
            }
        });
    }

    // Wait for DOM to update, then apply styling
    setTimeout(() => {
        if (globalConflictData[materialId]) {
            console.log('♻️ Using cached conflict data');
            applyVisualConflictIndicators(materialId);
        } else {
            console.log('🔍 Re-fetching conflict data');
            checkReleaseConflictsForMaterial(materialId);
        }
    }, 200);
}
// Function to apply styling to conflicted pallets
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

    // Disable and uncheck checkbox - make it non-interactive
    palletCheckbox.disabled = true;
    palletCheckbox.checked = false;
    palletCheckbox.style.pointerEvents = 'none'; // Extra protection against interaction

    // Also disable the label to prevent clicking
    label.style.pointerEvents = 'none';
    label.style.cursor = 'not-allowed';

    // Add tooltip
    const tooltipText = getConflictTooltipText(conflict);
    addTooltip(label, tooltipText);

    console.log('✅ Applied pallet conflict styling successfully');
}

// Enhanced function to apply styling to conflicted items
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

    // Disable and uncheck checkbox - make it non-interactive
    itemCheckbox.disabled = true;
    itemCheckbox.checked = false;
    itemCheckbox.style.pointerEvents = 'none'; // Extra protection against interaction

    // Also disable the label to prevent clicking
    label.style.pointerEvents = 'none';
    label.style.cursor = 'not-allowed';

    // Add tooltip
    const tooltipText = getConflictTooltipText(conflict);
    addTooltip(label, tooltipText);

    console.log('✅ Applied item conflict styling successfully');
}

// Function to generate tooltip text based on conflict type
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

// Function to add tooltip to an element
function addTooltip(element, text) {
    element.setAttribute('title', text);
    element.setAttribute('data-bs-toggle', 'tooltip');
    element.setAttribute('data-bs-placement', 'top');

    // Initialize Bootstrap tooltip if available
    if (typeof bootstrap !== 'undefined' && bootstrap.Tooltip) {
        new bootstrap.Tooltip(element);
    }
}