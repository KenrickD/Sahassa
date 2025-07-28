(function () {
    let receivesTable;
    let showGrouped = true;

    const token = $('input[name="__RequestVerificationToken"]').val();

    function formatDate(input) {
        if (!input) return '';
        const date = new Date(input);
        return date.toISOString().split('T')[0];
    }

    function initReceivesTable() {
        const finishedGoodId = document.getElementById("receives-table").dataset.finishedGoodId;

        receivesTable = $('#receives-table').DataTable({
            processing: true,
            serverSide: true,
            ajax: {
                url: `/FinishedGood/GetPaginatedReceives?finishedGoodId=${finishedGoodId}`,
                type: 'POST',
                contentType: 'application/json',
                headers: {
                    "X-CSRF-TOKEN": token,
                    "X-Requested-With": "XMLHttpRequest"
                },
                data: function (d) {
                    d.showGrouped = showGrouped;
                    return JSON.stringify(d);
                },
                error: function (xhr, error, thrown) {
                    console.error('Failed to load FinishedGood receive data:', error);
                }
            },
            columns: [
                { data: 'receivedDate', render: data => formatDate(data) },
                { data: 'packSize' },
                { data: 'totalQuantity' },
                { data: 'totalPallet' },
                {
                    data: null,
                    render: data => `${data.balanceQuantity} / ${data.totalQuantity}`
                },
                {
                    data: null,
                    render: data => `${data.balancePallet} / ${data.totalPallet}`
                },
                {data : 'po'},
                {
                    data: 'id',
                    render: id =>
                        `<a href="/FinishedGood/Pallets?ReceiveId=${id}" target="_blank" class="btn btn-sm btn-primary">Pallets</a>`
                },
                {
                    data: null,
                    render: function (data) {
                        const toggleText = showGrouped
                            ? "Show Items (Grouped by BatchNo)"
                            : "Show Items (Ungrouped)";
                        const toggleClass = showGrouped ? "btn-warning" : "btn-success";

                        return `<a href="/FinishedGood/Items?ReceiveId=${data.id}&grouped=${showGrouped}" class="btn btn-sm btn-primary ${toggleClass}" target="_blank">${toggleText}</a>`;
                    }
                },
                {
                    data: null,
                    orderable: false,
                    render: function (data, type, row) {
                        if (row.hasEditAccess) {
                            return `<a href="/FinishedGood/EditReceive?id=${row.id}&finishedGoodId=${finishedGoodId}&showGrouped=${showGrouped}"
                                class="w-8 h-8 bg-primary-50 dark:bg-primary-600/10 text-primary-600 dark:text-primary-400 rounded-full inline-flex items-center justify-center mr-1"
                                title="Edit Receive">
                                <iconify-icon icon="lucide:edit"></iconify-icon>
                            </a>`;
                        }
                        return '';
                    }
                }
            ],
            order: [[0, 'desc']],
            dom: '<"flex justify-between items-center mb-4"<"flex"f><"flex-1 text-right"l>>rt<"flex justify-between items-center mt-4"<"flex-1"i><"flex"p>>',
            language: {
                search: "",
                searchPlaceholder: "Search receives...",
                lengthMenu: "_MENU_ per page",
                info: "Showing _START_ to _END_ of _TOTAL_ receives",
                paginate: {
                    first: '<iconify-icon icon="heroicons-outline:chevron-double-left"></iconify-icon>',
                    last: '<iconify-icon icon="heroicons-outline:chevron-double-right"></iconify-icon>',
                    next: '<iconify-icon icon="heroicons-outline:chevron-right"></iconify-icon>',
                    previous: '<iconify-icon icon="heroicons-outline:chevron-left"></iconify-icon>'
                },
                zeroRecords: "No matching receives found",
                emptyTable: "No receives available"
            }
        });
    }

    document.addEventListener("DOMContentLoaded", function () {
        const toggleBtn = document.getElementById("toggle-group-btn");

        toggleBtn.addEventListener("click", function () {
            showGrouped = !showGrouped;

            toggleBtn.textContent = showGrouped
                ? "Show Items (Grouped by BatchNo)"
                : "Show Items (Ungrouped)";

            receivesTable.ajax.reload();
        });

        if (document.getElementById("receives-table")) {
            initReceivesTable();
        }
    });
})();
