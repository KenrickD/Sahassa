function initializeDataTable(tableId, config) {
    const dtConfig = {
        processing: true,
        serverSide: true,
        responsive: config.enableResponsive,
        pageLength: config.defaultPageSize,
        ajax: {
            url: config.ajaxUrl,
            type: 'POST',
            headers: {
                'X-CSRF-TOKEN': document.querySelector('meta[name="csrf-token"]').content
            }
        },
        columns: processColumns(config.columns),
        order: [[config.defaultSortColumnIndex, config.defaultSortDirection ? 'asc' : 'desc']],
        dom: '<"flex flex-col md:flex-row justify-between items-start md:items-center mb-4"<"flex-1"f><"flex"l>>rt<"flex flex-col md:flex-row justify-between items-center mt-4"<"text-sm text-neutral-500 dark:text-neutral-400"i><"pagination"p>>',
        language: {
            search: "",
            searchPlaceholder: "Search...",
            lengthMenu: "_MENU_ per page",
            info: "Showing _START_ to _END_ of _TOTAL_ entries",
            paginate: {
                first: '<svg class="w-5 h-5" fill="currentColor" viewBox="0 0 20 20"><path fill-rule="evenodd" d="M15.707 15.707a1 1 0 01-1.414 0l-5-5a1 1 0 010-1.414l5-5a1 1 0 111.414 1.414L11.414 10l4.293 4.293a1 1 0 010 1.414z" clip-rule="evenodd"></path><path fill-rule="evenodd" d="M7.707 15.707a1 1 0 01-1.414 0l-5-5a1 1 0 010-1.414l5-5a1 1 0 011.414 1.414L3.414 10l4.293 4.293a1 1 0 010 1.414z" clip-rule="evenodd"></path></svg>',
                last: '<svg class="w-5 h-5" fill="currentColor" viewBox="0 0 20 20"><path fill-rule="evenodd" d="M4.293 15.707a1 1 0 001.414 0l5-5a1 1 0 000-1.414l-5-5a1 1 0 00-1.414 1.414L8.586 10l-4.293 4.293a1 1 0 000 1.414z" clip-rule="evenodd"></path><path fill-rule="evenodd" d="M12.293 15.707a1 1 0 001.414 0l5-5a1 1 0 000-1.414l-5-5a1 1 0 00-1.414 1.414L16.586 10l-4.293 4.293a1 1 0 000 1.414z" clip-rule="evenodd"></path></svg>',
                next: '<svg class="w-5 h-5" fill="currentColor" viewBox="0 0 20 20"><path fill-rule="evenodd" d="M7.293 14.707a1 1 0 010-1.414L10.586 10 7.293 6.707a1 1 0 011.414-1.414l4 4a1 1 0 010 1.414l-4 4a1 1 0 01-1.414 0z" clip-rule="evenodd"></path></svg>',
                previous: '<svg class="w-5 h-5" fill="currentColor" viewBox="0 0 20 20"><path fill-rule="evenodd" d="M12.707 5.293a1 1 0 010 1.414L9.414 10l3.293 3.293a1 1 0 01-1.414 1.414l-4-4a1 1 0 010-1.414l4-4a1 1 0 011.414 0z" clip-rule="evenodd"></path></svg>'
            },
            zeroRecords: config.emptyTableMessage,
            emptyTable: config.emptyTableMessage
        },
        drawCallback: function () {
            // Style the table for dark mode
            applyDarkModeToTable();

            // Setup event handlers for actions like delete
            setupActionHandlers(tableId);
        }
    };

    // Initialize DataTable
    const table = $(`#${tableId}`).DataTable(dtConfig);

    // Custom page size handling
    $(`#${tableId}-page-size`).on('change', function () {
        table.page.len($(this).val()).draw();
    });

    // Custom search handling
    let searchTimeout;
    $(`#${tableId}-search`).on('input', function () {
        clearTimeout(searchTimeout);
        searchTimeout = setTimeout(() => {
            table.search($(this).val()).draw();
        }, 300);
    });

    // Select all functionality
    $(`#select-all-${tableId}`).on('change', function () {
        const isChecked = $(this).prop('checked');
        $(`.item-select-${tableId}`).prop('checked', isChecked);
    });

    return table;
}

function processColumns(columns) {
    return columns.map(column => {
        const columnDef = {
            data: column.data,
            title: column.title,
            orderable: column.sortable,
            searchable: column.searchable,
            visible: column.visible
        };

        if (column.width) {
            columnDef.width = column.width;
        }

        if (column.className) {
            columnDef.className = column.className;
        }

        if (column.renderFunction) {
            // Convert string function to actual function
            columnDef.render = new Function('return ' + column.renderFunction)();
        }

        return columnDef;
    });
}

function applyDarkModeToTable() {
    if (document.documentElement.classList.contains('dark')) {
        $('.dataTables_wrapper select, .dataTables_wrapper input').addClass('bg-neutral-800 text-white border-neutral-700');
    }
}

function setupActionHandlers(tableId) {
    // Setup delete action
    $(`.delete-item-${tableId}`).on('click', function () {
        const id = $(this).data('id');
        const url = $(this).data('url');

        if (confirm('Are you sure you want to delete this item?')) {
            $.ajax({
                url: url,
                type: 'POST',
                headers: {
                    'X-CSRF-TOKEN': document.querySelector('meta[name="csrf-token"]').content
                },
                success: function () {
                    // Reload the table
                    $(`#${tableId}`).DataTable().ajax.reload();

                    // Show success message
                    showNotification('Item deleted successfully', 'success');
                },
                error: function (xhr) {
                    showNotification('Error deleting item', 'error');
                    console.error('Error:', xhr);
                }
            });
        }
    });
}

function showNotification(message, type = 'info') {
    const toast = $(`<div class="fixed bottom-4 right-4 px-6 py-3 rounded-lg text-white ${type === 'success' ? 'bg-green-600' : 'bg-red-600'}">${message}</div>`);
    $('body').append(toast);

    setTimeout(() => {
        toast.animate({ opacity: 0 }, 500, function () {
            $(this).remove();
        });
    }, 3000);
}