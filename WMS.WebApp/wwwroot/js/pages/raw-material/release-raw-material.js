import { initReleasePage } from '/js/release-shared.js';

document.addEventListener('DOMContentLoaded', function () {
    // Set up receive checkbox behavior
    setupReceiveCheckboxes();

    // Set up pallet checkbox behavior
    setupPalletCheckboxes();

    // Set up item checkbox behavior
    setupItemCheckboxes();

    // Set up release type behavior
    setupReleaseTypeBehavior();

    // Set up show/hide toggle behavior
    setupShowHideToggle();

    // Override the default release button click handler
    const releaseBtn = document.getElementById('releaseBtn');
    if (releaseBtn) {
        releaseBtn.addEventListener('click', function (e) {
            e.preventDefault();
            submitRelease();
        });
    }
});

// New function to handle release type changes
function setupReleaseTypeBehavior() {
    document.querySelectorAll('.release-type').forEach(select => {
        select.addEventListener('change', function () {
            const receiveId = this.dataset.receiveId;
            const row = this.closest('tr');
            const palletCheckboxes = row.querySelectorAll('.pallet-checkbox:not([data-released="true"])');
            const itemCheckboxes = row.querySelectorAll('.item-checkbox:not([data-released="true"])');

            if (this.value === 'Full') {
                // Check all pallets and items
                palletCheckboxes.forEach(checkbox => {
                    checkbox.checked = true;
                    checkbox.disabled = true;

                    // Also trigger the change event to handle child items
                    checkbox.dispatchEvent(new Event('change'));
                });

                // Disable all individual item checkboxes
                itemCheckboxes.forEach(checkbox => {
                    checkbox.checked = true;
                    checkbox.disabled = true;
                });

                // Show all pallet items for visibility
                row.querySelectorAll('.pallet-items').forEach(itemsContainer => {
                    itemsContainer.style.display = 'block';
                });
            } else {
                // Partial - enable individual selection
                palletCheckboxes.forEach(checkbox => {
                    checkbox.checked = false;
                    checkbox.disabled = false;

                    // Also trigger the change event to handle child items
                    checkbox.dispatchEvent(new Event('change'));
                });

                // Enable all individual item checkboxes
                itemCheckboxes.forEach(checkbox => {
                    checkbox.checked = false;
                    checkbox.disabled = false;
                });
            }
        });
    });
}

// Function to set up receive checkbox behavior
function setupReceiveCheckboxes() {
    document.querySelectorAll('.receive-checkbox').forEach(checkbox => {
        checkbox.addEventListener('change', function () {
            const receiveId = this.dataset.receiveId;
            const row = this.closest('tr');
            const releaseTypeSelect = row.querySelector('.release-type');
            const releaseDateContainer = row.querySelector('.receive-release-date-container');
            const palletCheckboxes = row.querySelectorAll('.pallet-checkbox:not([data-released="true"])');
            const itemCheckboxes = row.querySelectorAll('.item-checkbox:not([data-released="true"])');

            if (this.checked) {
                // Enable the release type dropdown
                releaseTypeSelect.disabled = false;

                // Show the release date container
                releaseDateContainer.style.display = 'block';

                // Set default date to tomorrow
                const dateInput = releaseDateContainer.querySelector('input[type="date"]');
                if (dateInput && !dateInput.value) {
                    const tomorrow = new Date();
                    tomorrow.setDate(tomorrow.getDate() + 1);
                    dateInput.value = tomorrow.toISOString().split('T')[0];
                }

                // If release type is already set to Full, check all items
                if (releaseTypeSelect.value === 'Full') {
                    palletCheckboxes.forEach(cb => {
                        cb.checked = true;
                        cb.disabled = true;
                        cb.dispatchEvent(new Event('change'));
                    });

                    itemCheckboxes.forEach(cb => {
                        cb.checked = true;
                        cb.disabled = true;
                    });
                } else {
                    // Otherwise enable individual selection (Partial)
                    palletCheckboxes.forEach(cb => {
                        cb.disabled = false;
                    });

                    itemCheckboxes.forEach(cb => {
                        cb.disabled = false;
                    });
                }
            } else {
                // Disable the release type dropdown
                releaseTypeSelect.disabled = true;

                // Hide the release date container
                releaseDateContainer.style.display = 'none';

                // Uncheck and disable all pallet and item checkboxes in this row
                palletCheckboxes.forEach(cb => {
                    cb.checked = false;
                    cb.disabled = true;
                });

                itemCheckboxes.forEach(cb => {
                    cb.checked = false;
                    cb.disabled = true;
                });

                // Hide all pallet items
                row.querySelectorAll('.pallet-items').forEach(itemsContainer => {
                    itemsContainer.style.display = 'none';
                });
            }
        });

        // Initialize the state based on current checkbox value
        checkbox.dispatchEvent(new Event('change'));
    });
}

// Function to set up pallet checkbox behavior
function setupPalletCheckboxes() {
    document.querySelectorAll('.pallet-checkbox').forEach(checkbox => {
        checkbox.addEventListener('change', function () {
            if (this.disabled) return; // Skip if disabled

            const card = this.closest('.card');
            const palletId = this.dataset.palletId;
            const receiveId = this.dataset.receiveId;
            const itemsContainer = card.querySelector('.pallet-items');
            const itemCheckboxes = card.querySelectorAll('.item-checkbox:not([data-released="true"])');

            // If pallet is checked, check all items in the pallet and disable them
            if (this.checked) {
                itemCheckboxes.forEach(itemCheckbox => {
                    itemCheckbox.checked = true;
                    itemCheckbox.disabled = true;
                });
            } else {
                // Uncheck and enable all items that are not already released
                itemCheckboxes.forEach(itemCheckbox => {
                    itemCheckbox.checked = false;
                    itemCheckbox.disabled = false;
                });
            }

            // Always show items when pallet checkbox is changed
            if (itemsContainer) {
                itemsContainer.style.display = 'block';
            }
        });
    });
}

// Function to set up item checkbox behavior
function setupItemCheckboxes() {
    document.querySelectorAll('.item-checkbox').forEach(checkbox => {
        checkbox.addEventListener('change', function () {
            if (this.disabled) return; // Skip if disabled

            const li = this.closest('li');
            const dateContainer = li.querySelector('.item-release-date-container');
            const card = this.closest('.card');
            const palletCheckbox = card.querySelector('.pallet-checkbox');
            const allItemCheckboxes = card.querySelectorAll('.item-checkbox:not([data-released="true"])');


            // Check if all items in this pallet are now checked
            const allChecked = Array.from(allItemCheckboxes).every(cb => cb.checked);

            // Update pallet checkbox visibility
            if (palletCheckbox && !palletCheckbox.disabled) {
                if (allChecked) {
                    //// Optional: Add visual indication that all items are selected
                    //card.classList.add('all-items-selected');

                    //// Optional: Show a tooltip or message
                    //const infoMsg = document.createElement('div');
                    //infoMsg.className = 'text-info small mt-1';
                    //infoMsg.innerText = 'All items selected. You can select the entire pallet instead.';

                    // Only add if it doesn't exist yet
                    if (!card.querySelector('.all-items-info')) {
                        infoMsg.classList.add('all-items-info');
                        palletCheckbox.closest('.form-check').appendChild(infoMsg);
                    }
                } else {
                    // Remove indicators if not all items are checked
                    card.classList.remove('all-items-selected');
                    const infoMsg = card.querySelector('.all-items-info');
                    if (infoMsg) infoMsg.remove();
                }
            }
        });
    });
}

// Function to set up show/hide toggle behavior
function setupShowHideToggle() {
    document.querySelectorAll('.toggle-all-items').forEach(button => {
        button.addEventListener('click', function (e) {
            e.preventDefault();

            const receiveId = this.dataset.receiveId;
            const row = this.closest('tr');
            const palletItemContainers = row.querySelectorAll(`.pallet-items[data-receive-id="${receiveId}"]`);

            // Count how many are currently visible
            let visibleCount = 0;
            palletItemContainers.forEach(container => {
                if (container.style.display === 'block') {
                    visibleCount++;
                }
            });

            // If all are visible, hide all; otherwise show all
            const shouldShow = visibleCount < palletItemContainers.length;

            palletItemContainers.forEach(container => {
                container.style.display = shouldShow ? 'block' : 'none';
            });
        });
    });
}

// Function to collect release data for the payload
function collectReleaseData() {
    // Remove references to enableScheduledRelease since it's been removed
    // const isScheduled = document.getElementById('enableScheduledRelease').checked;
    const itemReleaseDates = {};
    const palletReleases = [];

    // Default date is tomorrow
    const tomorrow = new Date();
    tomorrow.setDate(tomorrow.getDate() + 1);
    const defaultDate = tomorrow.toISOString().split('T')[0];

    // Get all checked pallets first
    const checkedPalletIds = new Set();
    document.querySelectorAll('.pallet-checkbox:checked').forEach(checkbox => {
        if (checkbox.dataset.released !== 'true') {
            checkedPalletIds.add(checkbox.dataset.palletId);
        }
    });

    // Get all receive dates
    const receiveDates = {};
    document.querySelectorAll('.receive-checkbox:checked').forEach(checkbox => {
        const receiveId = checkbox.dataset.receiveId;
        const row = checkbox.closest('tr');
        const dateInput = row.querySelector('.receive-release-date');

        if (dateInput && dateInput.value) {
            receiveDates[receiveId] = dateInput.value;
        } else {
            receiveDates[receiveId] = defaultDate;
        }
    });

    // Collect pallet releases
    document.querySelectorAll('.pallet-checkbox:checked').forEach(checkbox => {
        if (checkbox.dataset.released !== 'true') {
            const palletId = checkbox.dataset.palletId;
            const receiveId = checkbox.dataset.receiveId;

            // Use the release date from the receive
            const releaseDate = receiveDates[receiveId] || defaultDate;

            palletReleases.push({
                palletId: palletId,
                releaseDate: releaseDate,
                releaseEntirePallet: true
            });
        }
    });

    // Collect individual item releases (only for items not part of a whole pallet release)
    document.querySelectorAll('.item-checkbox:checked').forEach(checkbox => {
        if (checkbox.dataset.released !== 'true') {
            const itemId = checkbox.dataset.itemId;
            const palletId = checkbox.dataset.palletId;
            const receiveId = checkbox.dataset.receiveId;

            // Skip if this item's pallet is already included as a whole
            if (checkedPalletIds.has(palletId)) {
                return;
            }

            // Use the release date from the receive
            const releaseDate = receiveDates[receiveId] || defaultDate;

            itemReleaseDates[itemId] = releaseDate;
        }
    });

    return {
        itemReleaseDates: itemReleaseDates,
        palletReleases: palletReleases
    };
}

// Function to submit the release data
function submitRelease() {
    // Get additional data first so we know which pallets are selected
    const additionalData = collectReleaseData();

    // Get selected pallets
    const selectedPalletIds = additionalData.palletReleases.map(pr => pr.palletId);

    // Get selected item IDs that aren't part of selected pallets
    const selectedItems = [];
    document.querySelectorAll('.item-checkbox:checked').forEach(checkbox => {
        if (checkbox.dataset.released !== 'true') {
            const itemId = checkbox.dataset.itemId;
            const palletId = checkbox.dataset.palletId;

            // Only include items that aren't part of a selected pallet
            if (!selectedPalletIds.includes(palletId)) {
                selectedItems.push(itemId);
            }
        }
    });

    // If no items are selected individually or via pallets, show error
    if (selectedItems.length === 0 && additionalData.palletReleases.length === 0) {
        const errorContainer = document.getElementById('errorContainer');
        if (errorContainer) {
            errorContainer.innerHTML = 'Please select at least one item or pallet to release.';
            errorContainer.style.display = 'block';
        }
        return;
    }

    // Create the payload
    const payload = {
        rawMaterialId: window.rawMaterialId,
        itemIds: selectedItems,
        itemReleaseDates: additionalData.itemReleaseDates,
        palletReleases: additionalData.palletReleases
    };

    console.log('Submitting payload:', payload);

    // Get the anti-forgery token
    const token = document.querySelector('input[name="__RequestVerificationToken"]').value;

    // Submit the data
    fetch('/RawMaterial/Submit', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
            'X-CSRF-TOKEN': token,
            'X-Requested-With': 'XMLHttpRequest'
        },
        body: JSON.stringify(payload)
    })
        .then(response => {
            if (!response.ok) {
                return response.text().then(text => {
                    console.error('Response text:', text);
                    throw new Error(`Server responded with status: ${response.status}`);
                });
            }
            return response.text().then(text => {
                // Try to parse as JSON, but handle empty responses
                if (text) {
                    try {
                        return JSON.parse(text);
                    } catch (e) {
                        console.warn('Response is not valid JSON:', text);
                        return { success: response.ok };
                    }
                } else {
                    return { success: response.ok };
                }
            });
        })
        .then(data => {
            if (data.success) {
                // Show success message
                alert('Released successfully.');
                // Redirect
                window.location.href = '/RawMaterial/Datatable';
            } else {
                // Show error message
                const errorContainer = document.getElementById('errorContainer');
                if (errorContainer) {
                    errorContainer.innerHTML = data.message || 'An error occurred during release.';
                    errorContainer.style.display = 'block';
                }
            }
        })
        .catch(error => {
            console.error('Submission error:', error);
            const errorContainer = document.getElementById('errorContainer');
            if (errorContainer) {
                errorContainer.innerHTML = 'Failed to submit: ' + error.message;
                errorContainer.style.display = 'block';
            }
        });
}