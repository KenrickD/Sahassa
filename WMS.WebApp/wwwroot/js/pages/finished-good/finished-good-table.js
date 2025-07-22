(function () {
    const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;

    $(document).ready(function () {
        //processPendingFinishedGoodReleases();
        function processPendingFinishedGoodReleases() {
            const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;

            $.ajax({
                url: '/FinishedGood/ProcessPendingFinishedGoodReleases',
                type: 'POST',
                headers: {
                    'X-CSRF-TOKEN': token
                },
                success: function (response) {
                    if (response.success) {
                        console.log("FG Release processing:", response.message);

                        // If any releases were processed, refresh the data
                        if (response.message === "Pending finished good releases processed successfully") {
                            // Reload table data
                            $('#FGDataTable').DataTable().ajax.reload();
                        }
                    } else {
                        console.error("Failed to process FG releases:", response.message);
                    }
                },
                error: function (xhr, status, error) {
                    console.error("Error processing FG releases:", error);
                }
            });
        }
        const table = $('#FGDataTable').DataTable({
            processing: true,
            serverSide: true,
            ajax: {
                url: $('#FGDataTable').data('url'),
                type: 'POST',
                contentType: 'application/json',
                headers: {
                    'X-CSRF-TOKEN': token,
                    'X-Requested-With': 'XMLHttpRequest'
                },
                data: function (d) {
                    return JSON.stringify(d);
                },
                dataSrc: function (json) {
                    if (json.hasEditAccess) {
                        $('#addFinishedGoodBtn').removeClass('d-none').removeClass('hidden');
                    } else {
                        $('#addFinishedGoodBtn').addClass('d-none').addClass('hidden');
                    }

                    // Update grand totals display
                    updateGrandTotalsDisplay(json.grandTotalBalanceQty, json.grandTotalBalancePallet);

                    return json.data;
                },
                error: function (xhr, error, thrown) {
                    console.error('Failed to load Finished Goods data:', error);
                }
            },
            columns: [
                { data: 'sku' },
                { data: 'description' },
                {
                    data: null,
                    orderable: false,
                    render: function (data, type, row) {
                        const hasEditAccess = row.hasEditAccess || false;
                        const finishedGoodId = row.id;
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
                            const checkboxId = `${group.field}_${finishedGoodId}`;

                            html += `
                                <label class="flex items-center gap-1 text-xs ${disabledClass}" title="${group.label}">
                                    <input type="checkbox" 
                                           id="${checkboxId}"
                                           class="group-checkbox w-3 h-3" 
                                           data-id="${finishedGoodId}"
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
                },
                { data: 'totalBalanceQty' },
                { data: 'totalBalancePallet' },
                {
                    data: 'id',
                    orderable: false,
                    render: function (data, type, row) {
                        let actions = '';

                        if (row.hasEditAccess) {
                            actions += `<a href="/FinishedGood/Release?finishedgoodId=${data}" 
                                class="w-8 h-8 bg-primary-50 dark:bg-primary-600/10 text-primary-600 dark:text-primary-400 rounded-full inline-flex items-center justify-center mr-1"
                                title="Release Finished Goods">
                                <iconify-icon icon="material-symbols:unarchive"></iconify-icon>
                            </a>`;

                            actions += `<a href="/FinishedGood/Edit?id=${data}" 
                                class="w-8 h-8 bg-success-100 dark:bg-success-600/25 text-success-600 dark:text-success-400 rounded-full inline-flex items-center justify-center mr-1"
                                title="Edit Finished Goods">
                                <iconify-icon icon="lucide:edit"></iconify-icon>
                            </a>`;
                        }

                        return actions;
                    }
                }
            ],
            dom: '<"flex justify-between items-center mb-4"<"flex"f><"flex-1 text-right"l>>rt<"flex justify-between items-center mt-4"<"flex-1"i><"flex"p>>',
            language: {
                search: "",
                searchPlaceholder: "Search finished goods...",
                lengthMenu: "_MENU_ per page",
                info: "Showing _START_ to _END_ of _TOTAL_ finished goods",
                paginate: {
                    first: '<iconify-icon icon="heroicons-outline:chevron-double-left"></iconify-icon>',
                    last: '<iconify-icon icon="heroicons-outline:chevron-double-right"></iconify-icon>',
                    next: '<iconify-icon icon="heroicons-outline:chevron-right"></iconify-icon>',
                    previous: '<iconify-icon icon="heroicons-outline:chevron-left"></iconify-icon>'
                },
                zeroRecords: "No matching finished goods found",
                emptyTable: "No finished goods available"
            }
        });

        // Function to update grand totals display
        function updateGrandTotalsDisplay(totalQty, totalPallet) {
            // Update the grand totals in the dedicated panel
            $('#grand-total-qty').text((totalQty || 0).toLocaleString());
            $('#grand-total-pallets').text((totalPallet || 0).toLocaleString());
        }

        // Handle checkbox changes
        $('#FGDataTable tbody').on('change', '.group-checkbox', function () {
            const $checkbox = $(this);
            const finishedGoodId = $checkbox.data('id');
            const fieldName = $checkbox.data('field');
            const isChecked = $checkbox.is(':checked');

            // Disable checkbox during AJAX call
            $checkbox.prop('disabled', true);

            $.ajax({
                url: '/FinishedGood/UpdateGroupField',
                type: 'POST',
                data: {
                    finishedGoodId: finishedGoodId,
                    fieldName: fieldName,
                    value: isChecked,
                    __RequestVerificationToken: token
                },
                success: function (response) {
                    if (response && response.success) {
                        // Show success message
                        toastr.success(`${fieldName.charAt(0).toUpperCase() + fieldName.slice(1)} updated successfully`);
                    } else {
                        // Revert checkbox state and show error
                        $checkbox.prop('checked', !isChecked);
                        toastr.error(response?.message || 'Failed to update group field');
                    }
                },
                error: function (xhr, status, error) {
                    // Revert checkbox state and show error
                    $checkbox.prop('checked', !isChecked);
                    toastr.error('Failed to update group field: ' + error);
                },
                complete: function () {
                    // Re-enable checkbox
                    $checkbox.prop('disabled', false);
                }
            });
        });

        $('#FGDataTable tbody').on('dblclick', 'tr', function () {
            const data = table.row(this).data();
            if (data?.id) {
                window.location.href = `/FinishedGood/Details/${data.id}`;
            }
        });

        const successMessage = sessionStorage.getItem("successMessage");
        if (successMessage) {
            toastr.success(successMessage);
            sessionStorage.removeItem("successMessage");
        }
    });
})();