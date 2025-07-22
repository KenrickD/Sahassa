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
        palletsTable = $('#pallets-table').DataTable({
            processing: true,
            serverSide: true,
            ajax: {
                url: `/FinishedGood/GetPaginatedPallets?receiveId=${receiveId}`,
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
                { data: 'palletCode', className: 'text-left' },
                { data: 'packSize', className: 'text-left' },
                { data: 'quantity', className: 'text-left' },
                {
                    data: null,
                    className: 'text-left',
                    render: data => `${data.quantityBalance} / ${data.quantity}`
                },
                {
                    data: 'fG_ReceivePalletItems',
                    className: 'text-left',
                    render: function (items) {
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
                    render: id => `<a href="/FinishedGood/ViewPhotoAttachment?palletId=${id}" data-modal="true" class="btn btn-sm btn-primary">Photos</a>`
                },
                {
                    data: null,
                    className: 'text-center',
                    orderable: false,
                    render: function (data, type, row) {
                        if (row.hasEditAccess) {
                            return `<a href="/FinishedGood/EditPallet?id=${row.id}&receiveId=${receiveId}" class="btn btn-sm btn-warning">Edit</a>`;
                        }
                        return '';
                    }
                }
            ],
            order: [[0, 'desc']],
            dom: '<"flex justify-between items-center mb-4"<"flex"f><"flex-1 text-right"l>>rt<"flex justify-between items-center mt-4"<"flex-1"i><"flex"p>>',
            language: {
                search: "",
                searchPlaceholder: "Search pallets...",
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

        fetch('/FinishedGood/UpdateGroupFieldInline', {
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
        const res = await fetch('/FinishedGood/GetAllLocations');
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

                    fetch('/FinishedGood/UpdateLocationInline', {
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