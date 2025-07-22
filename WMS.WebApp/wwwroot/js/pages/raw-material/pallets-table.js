(function () {
    let palletsTable;
    const receiveId = document.getElementById("pallets-table").dataset.receiveId;
    const token = document.querySelector('input[name="__RequestVerificationToken"]').value;

    function formatDate(dateStr) {
        if (!dateStr) return '';
        const date = new Date(dateStr);
        return date.toISOString().split('T')[0];
    }

    function initPalletsTable() {
        const receiveId = document.getElementById("pallets-table").dataset.receiveId;
        const isGrouped = document.getElementById("pallets-table").dataset.isGrouped === 'true';
        const groupId = document.getElementById("pallets-table").dataset.groupId;

        // Build the URL with all query parameters
        let cleanGroupId = groupId;
        if (groupId && typeof groupId === 'string') {
            cleanGroupId = groupId.replace(/^["']|["']$/g, '');
        }
        let url = `/RawMaterial/GetPaginatedPallets?receiveId=${encodeURIComponent(receiveId)}`;
        if (isGrouped) {
            url += `&isGrouped=true`;
            if (cleanGroupId && cleanGroupId !== 'undefined' && cleanGroupId !== '') {
                url += `&groupId=${encodeURIComponent(cleanGroupId)}`;
            }
        }
        console.log('Final URL:', url);
        palletsTable = $('#pallets-table').DataTable({
            processing: true,
            serverSide: true,
            ajax: {
                url: url,
                type: 'POST',
                contentType: 'application/json',
                headers: {
                    'X-CSRF-TOKEN': token,
                    'X-Requested-With': 'XMLHttpRequest'
                },
                data: function (d) {
                    return JSON.stringify(d);
                },
                error: function (xhr, error, thrown) {
                    console.error('Failed to load pallet data:', error);
                }
            },
            columns: [
                {
                    data: 'palletCode',
                    className: 'text-left',
                    // Make sure this column is searchable
                    searchable: true
                },
                { data: 'packSize', className: 'text-left' },
                { data: 'quantity', className: 'text-left' },
                {
                    data: null,
                    className: 'text-left',
                    render: data => `${data.quantityBalance} / ${data.quantity}`
                },
                {
                    data: 'rM_ReceivePalletItems',
                    className: 'text-left',
                    // Add search capability for item codes
                    render: function (items, type, row) {
                        // For search type, return a concatenated string of all item codes
                        if (type === 'filter' || type === 'sort') {
                            return items && items.length
                                ? items.map(item => item.itemCode).join(' ')
                                : '';
                        }

                        // For display type, show the formatted table
                        if (!items || !items.length) return '<span>No Items</span>';

                        let rows = items.map(item => `
                            <tr>
                                <td>${item.itemCode}</td>
                                <td>${item.remarks ?? ''}</td>
                                <td>${formatDate(item.prodDate)}</td>
                            </tr>
                        `).join('');

                        return `
                            <table class="table table-sm table-borderless mb-0">
                                <thead>
                                    <tr><th>HU</th><th>Remarks</th><th>Prod Date</th></tr>
                                </thead>
                                <tbody>${rows}</tbody>
                            </table>`;
                    }
                },
                {
                    data: 'locationName',
                    className: 'text-left',
                    render: function (data, type, row) {
                        return `<span class="location-cell" data-id="${row.id}" data-code="${row.palletCode}" data-current="${data || ''}" data-has-edit="${row.hasEditAccess}">${data || '-'}</span>`;
                    }
                },
                {
                    data: 'id',
                    className: 'text-center',
                    render: id => `<a href="/RawMaterial/ViewPhotoAttachment?palletId=${id}" data-modal="true" class="btn btn-sm btn-primary">Photos</a>`
                },
                {
                    data: null,
                    className: 'text-center',
                    orderable: false,
                    render: function (data, type, row) {
                        if (row.hasEditAccess) {
                            return `<a href="/RawMaterial/EditPallet?id=${row.id}&receiveId=${receiveId}" class="btn btn-sm btn-warning">Edit</a>`;
                        }
                        return '';
                    }
                }
            ],
            order: [[0, 'desc']],
            dom: '<"flex justify-between items-center mb-4"<"flex"f><"flex-1 text-right"l>>rt<"flex justify-between items-center mt-4"<"flex-1"i><"flex"p>>',
            language: {
                search: "",
                searchPlaceholder: "Search pallets and items...",
                lengthMenu: "_MENU_ per page",
                info: "Showing _START_ to _END_ of _TOTAL_ pallets",
                paginate: {
                    first: '<iconify-icon icon="heroicons-outline:chevron-double-left"></iconify-icon>',
                    last: '<iconify-icon icon="heroicons-outline:chevron-double-right"></iconify-icon>',
                    next: '<iconify-icon icon="heroicons-outline:chevron-right"></iconify-icon>',
                    previous: '<iconify-icon icon="heroicons-outline:chevron-left"></iconify-icon>'
                },
                zeroRecords: "No matching pallets found",
                emptyTable: "No pallets available"
            },
            // Enable client-side search for better matching
            search: {
                smart: true,
                regex: false,
                caseInsensitive: true
            },
            // Add these options to improve search functionality
            searchDelay: 400,
            // Add a custom search function to improve matching for both pallet codes and item codes
            initComplete: function () {
                const api = this.api();

                // Override the default search function
                $.fn.dataTable.ext.search.push(
                    function (settings, data, dataIndex, rowData) {
                        // If no search term, include all rows
                        const searchTerm = $('.dataTables_filter input').val().toLowerCase();
                        if (!searchTerm) return true;

                        // Check MHU (pallet code)
                        const palletCode = rowData.palletCode?.toLowerCase() || '';
                        if (palletCode.includes(searchTerm)) return true;

                        // Check HU (item codes)
                        const items = rowData.rM_ReceivePalletItems || [];
                        for (let i = 0; i < items.length; i++) {
                            const itemCode = items[i].itemCode?.toLowerCase() || '';
                            if (itemCode.includes(searchTerm)) return true;
                        }

                        // Check other searchable columns (remarks, etc.)
                        for (let i = 0; i < data.length; i++) {
                            if (data[i].toLowerCase().includes(searchTerm)) return true;
                        }

                        return false;
                    }
                );

                // Handle keyup events on the search input
                $('.dataTables_filter input')
                    .off('keyup.DT search.DT input.DT paste.DT cut.DT')
                    .on('keyup', function () {
                        api.draw();
                    });
            }
        });
    }

    // Updated event handler for individual boolean fields
    $('#pallets-table').on('change', '.group-checkbox', function () {
        const checkbox = $(this);
        const palletId = checkbox.data('id');
        const fieldName = checkbox.data('field');
        const isChecked = checkbox.is(':checked');

        const formData = new FormData();
        formData.append("palletId", palletId);
        formData.append("fieldName", fieldName);
        formData.append("value", isChecked);
        formData.append("__RequestVerificationToken", token);

        fetch('/RawMaterial/UpdateGroupFieldInline', {
            method: 'POST',
            body: formData
        })
            .then(res => {
                if (res.ok) {
                    toastr.success(`Group ${fieldName.replace('group', '')} updated successfully.`);
                } else {
                    toastr.error(`Failed to update Group ${fieldName.replace('group', '')}.`);
                    // Revert the checkbox state if update failed
                    checkbox.prop('checked', !isChecked);
                }
            })
            .catch(() => {
                toastr.error("An error occurred.");
                // Revert the checkbox state if update failed
                checkbox.prop('checked', !isChecked);
            });
    });

    async function fetchLocations() {
        const res = await fetch('/RawMaterial/GetAllLocations');
        return await res.json();
    }

    document.addEventListener("DOMContentLoaded", function () {
        const successMessage = sessionStorage.getItem("successMessage");
        if (successMessage) {
            toastr.success(successMessage);
            sessionStorage.removeItem("successMessage");
        }

        initPalletsTable();

        document.getElementById('pallets-table').addEventListener('dblclick', async function (e) {
            const span = e.target.closest('.location-cell');
            if (!span) return;

            const hasEdit = span.dataset.hasEdit === 'true';
            if (!hasEdit) return;

            const currentText = span.innerText.trim();
            const palletId = span.dataset.id;
            const palletCode = span.dataset.code;
            const locations = await fetchLocations();

            if (span.querySelector('select')) return;

            const select = document.createElement('select');
            select.className = 'form-control form-select';

            const defaultOption = new Option('-- Select Location --', '');
            select.appendChild(defaultOption);

            for (const loc of locations) {
                const opt = new Option(loc.display, loc.id);
                if (loc.display === currentText) opt.selected = true;
                select.appendChild(opt);
            }

            span.innerHTML = '';
            span.appendChild(select);
            select.focus();

            select.addEventListener('mousedown', e => e.stopPropagation());

            select.addEventListener('change', function () {
                const newVal = select.value;
                const selectedText = select.options[select.selectedIndex].text;

                if (newVal === currentText || !newVal) {
                    span.innerText = currentText;
                    return;
                }

                if (confirm('Are you sure you want to update the location?')) {
                    const formData = new FormData();
                    formData.append("palletId", palletId);
                    formData.append("locationId", newVal);
                    formData.append("__RequestVerificationToken", token);

                    fetch('/RawMaterial/UpdateLocationInline', {
                        method: 'POST',
                        body: formData
                    }).then(res => {
                        if (res.ok) {
                            sessionStorage.setItem("successMessage", `Location for pallet ${palletCode} has been updated.`);
                            setTimeout(() => location.reload(), 800);
                        } else {
                            toastr.error("Failed to update location.");
                            span.innerText = currentText;
                        }
                    }).catch(() => {
                        toastr.error("Error occurred.");
                        span.innerText = currentText;
                    });
                } else {
                    span.innerText = currentText;
                }
            });

            select.addEventListener('keydown', function (evt) {
                if (evt.key === "Escape") {
                    span.innerText = currentText;
                }
            });
        });
    });
})();