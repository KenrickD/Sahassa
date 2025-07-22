(function () {
    let itemsTable;
    let showGrouped = document.getElementById("items-table").dataset.showGrouped === "true";
    const receiveId = document.getElementById("items-table").dataset.receiveId;
    const token = $('input[name="__RequestVerificationToken"]').val();

    function getGroupedColumns() {
        return [
            { data: 'batchNo', title: 'Batch No' },
            {
                data: 'huList',
                title: '',
                render: function (data, type, row) {
                    let rows = '';
                    for (let i = 0; i < data.length; i++) {
                        const hu = data[i];
                        const remarks = row.remarks[i] || "";
                        const location = row.location[i] || "";
                        const canEdit = row.hasEditAccess;

                        const editButton = canEdit
                            ? `<a href="/RawMaterial/EditItem?hu=${hu}&receiveId=${receiveId}&grouped=${showGrouped}" 
                                   class="w-8 h-8 bg-warning-100 dark:bg-warning-600/25 text-warning-600 dark:text-warning-400 rounded-full inline-flex items-center justify-center"
                                   title="Edit Item">
                                   <iconify-icon icon="lucide:edit"></iconify-icon>
                               </a>`
                            : '';

                        rows += `
                            <tr>
                                <td>${hu}</td>
                                <td>${remarks}</td>
                                <td>${location}</td>
                                <td>${editButton}</td>
                            </tr>`;
                    }
                    return `
                        <table class="table table-sm table-borderless mb-0">
                            <thead><tr><th>HU</th><th>Remarks</th><th>Location</th><th>Actions</th></tr></thead>
                            <tbody>${rows}</tbody>
                        </table>`;
                }
            },
            {
                data: 'mhuList',
                title: 'MHU',
                render: mhuList => mhuList.filter((v, i, a) => a.indexOf(v) === i).join('<br/>')
            },
            { data: 'qty', title: 'Qty' },
            {
                data: null,
                title: 'Bal of Qty',
                render: row => `${row.balQty} / ${row.qty}`
            },
            {
                data: 'prodDate',
                title: 'Prod Date',
                render: d => formatDate(d)
            }
        ];
    }

    function getUngroupedColumns() {
        return [
            { data: 'batchNo', title: 'Batch No' },
            { data: 'hu', title: 'HU' },
            { data: 'mhu', title: 'MHU' },
            {
                data: 'prodDate',
                title: 'Prod Date',
                render: d => formatDate(d)
            },
            { data: 'remarks', title: 'Remarks' },
            { data: 'location', title: 'Location' },
            {
                data: 'hu',
                title: 'Actions',
                orderable: false,
                searchable: false,
                render: function (hu, type, row) {
                    if (row.hasEditAccess) {
                        return `<a href="/RawMaterial/EditItem?hu=${hu}&receiveId=${receiveId}&grouped=${showGrouped}" 
                            class="w-8 h-8 bg-warning-100 dark:bg-warning-600/25 text-warning-600 dark:text-warning-400 rounded-full inline-flex items-center justify-center"
                            title="Edit Item">
                            <iconify-icon icon="lucide:edit"></iconify-icon>
                        </a>`;
                    }
                    return '';
                }
            }
        ];
    }

    function formatDate(input) {
        if (!input) return '';
        const date = new Date(input);
        return date.toISOString().split('T')[0];
    }

    async function initItemsTable() {
        const groupInfo = await checkReceiveGroupInfo();

        if (itemsTable) {
            itemsTable.destroy();
            $('#items-table tbody').empty();
        }

        const columns = showGrouped ? getGroupedColumns() : getUngroupedColumns();

        let url = `/RawMaterial/GetPaginatedItems?receiveId=${receiveId}&grouped=${showGrouped}`;
        if (groupInfo.isGrouped && groupInfo.groupId) {
            url += `&isGrouped=true&groupId=${groupInfo.groupId}`;
        }

        itemsTable = $('#items-table').DataTable({
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
                    console.error('Failed to load item data:', error);
                }
            },
            columns: columns,
            order: [[0, 'desc']],
            dom: '<"flex justify-between items-center mb-4"<"flex"f><"flex-1 text-right"l>>rt<"flex justify-between items-center mt-4"<"flex-1"i><"flex"p>>',
            language: {
                search: "",
                searchPlaceholder: "Search items...",
                lengthMenu: "_MENU_ per page",
                info: "Showing _START_ to _END_ of _TOTAL_ items",
                paginate: {
                    first: '<iconify-icon icon="heroicons-outline:chevron-double-left"></iconify-icon>',
                    last: '<iconify-icon icon="heroicons-outline:chevron-double-right"></iconify-icon>',
                    next: '<iconify-icon icon="heroicons-outline:chevron-right"></iconify-icon>',
                    previous: '<iconify-icon icon="heroicons-outline:chevron-left"></iconify-icon>'
                },
                zeroRecords: "No matching items found",
                emptyTable: "No items available"
            }
        });

        $('#toggle-group-btn').text(showGrouped ? "View by Item" : "Group by Batch No");
    }

    async function checkReceiveGroupInfo() {
        try {
            const response = await fetch(`/RawMaterial/GetReceiveGroupInfo?receiveId=${receiveId}`);
            if (response.ok) {
                return await response.json();
            }
        } catch (error) {
            console.error('Error checking receive group info:', error);
        }
        return { isGrouped: false, groupId: null };
    }

    document.addEventListener("DOMContentLoaded", function () {
        document.getElementById("toggle-group-btn").addEventListener("click", function () {
            showGrouped = !showGrouped;
            document.getElementById("items-table").dataset.showGrouped = showGrouped.toString();
            initItemsTable();
        });

        initItemsTable();
    });
})();