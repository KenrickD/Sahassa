(function () {
    // Private variables
    let ProductsTable;

    // Initialize module
    function init() {
        document.addEventListener('DOMContentLoaded', function () {
            if (document.getElementById('products-table')) {
                initializeProductDataTable();
            }
        });
    }

    // Table initialization function
    function initializeProductDataTable() {
        ProductsTable = $('#products-table').DataTable({
            processing: true,
            serverSide: true,
            responsive: true,
            ajax: {
                url: '/Product/GetProducts',
                type: 'POST',
                headers: {
                    'X-CSRF-TOKEN': $('meta[name="csrf-token"]').attr('content')
                }
            },
            columns: [
                {
                    data: 'id',
                    width: '5%',
                    render: function (data, type, row, meta) {
                        return `<div class="form-check style-check flex items-center">
                                    <input class="form-check-input product-select" type="checkbox" value="${data}">
                                    <label class="ms-2 form-check-label">
                                        ${meta.row + 1}
                                    </label>
                                </div>`;
                    },
                    orderable: false
                },
                {
                    data: 'clientName',
                    render: function (data, type, row) {
                        return data ? `<span class="text-sm text-gray-600 dark:text-gray-400">${escapeHtml(data)}</span>` : '<span class="text-gray-400">-</span>';
                    }
                },
                {
                    data: null,
                    render: function (data, type, row) {
                        const imageUrl = row.imageUrl ? row.imageUrl : '/images/default-product.png';
                        return `<div class="flex items-center gap-3">
                                    <div>
                                        <div class="font-medium text-gray-900 dark:text-white text-sm">${escapeHtml(row.name)}</div>
                                    </div>
                                </div>`;
                    }
                },
                {
                    data: 'sku',
                    render: function (data) {
                        return data ? `<span class="text-sm text-gray-600 dark:text-gray-400 font-mono bg-gray-100 dark:bg-gray-800 px-2 py-1 rounded">${escapeHtml(data)}</span>` : '<span class="text-gray-400">-</span>';
                    }
                },
                {
                    data: 'productTypeName',
                    render: function (data, type, row) {
                        return data ? `<span class="text-sm text-gray-600 dark:text-gray-400">${escapeHtml(data)}</span>` : '<span class="text-gray-400">-</span>';
                    }
                },
                {
                    data: 'productCategoryName',
                    render: function (data, type, row) {
                        return data ? `<span class="text-sm text-gray-600 dark:text-gray-400">${escapeHtml(data)}</span>` : '<span class="text-gray-400">-</span>';
                    }
                },
                {
                    data: 'unitOfMeasureCodeName',
                    render: function (data, type, row) {
                        return data ? `<span class="text-sm text-gray-600 dark:text-gray-400">${escapeHtml(data)}</span>` : '<span class="text-gray-400">-</span>';
                    }
                },
                //{
                //    data: null,
                //    render: function (data, type, row) {
                //        const currentStock = row.currentStockLevel || 0;
                //        const minStock = row.minStockLevel || 0;
                //        const reorderPoint = row.reorderPoint || 0;

                //        let statusClass = 'text-green-600';
                //        let statusText = 'In Stock';

                //        if (currentStock <= 0) {
                //            statusClass = 'text-red-600';
                //            statusText = 'Out of Stock';
                //        } else if (currentStock <= reorderPoint) {
                //            statusClass = 'text-orange-600';
                //            statusText = 'Reorder Needed';
                //        } else if (currentStock <= minStock) {
                //            statusClass = 'text-yellow-600';
                //            statusText = 'Low Stock';
                //        }

                //        return `<div class="text-center">
                //                    <div class="text-sm font-medium text-gray-900 dark:text-white">${currentStock.toFixed(0)}</div>
                //                    <div class="text-xs ${statusClass}">${statusText}</div>
                //                </div>`;
                //    }
                //},
                {
                    data: 'isActive',
                    render: function (data) {
                        return data
                            ? `<span class="bg-success-100 dark:bg-success-600/25 text-success-600 dark:text-success-400 px-6 py-1.5 rounded-full font-medium text-sm">Active</span>`
                            : `<span class="bg-danger-100 dark:bg-danger-600/25 text-danger-600 dark:text-danger-400 px-6 py-1.5 rounded-full font-medium text-sm">Inactive</span>`;
                    }
                },
                {
                    data: 'id',
                    orderable: false,
                    render: function (data, type, row) {
                        let actions = `<div class="flex">
                            <a href="Product/View/${data}" class="w-8 h-8 bg-primary-50 dark:bg-primary-600/10 text-primary-600 dark:text-primary-400 rounded-full inline-flex items-center justify-center mr-1" title="View Product">
                                <iconify-icon icon="iconamoon:eye-light"></iconify-icon>
                            </a>`;

                        // Show edit button only if user has Write permission
                        if (row.hasWriteAccess) {
                            actions += `<a href="Product/Edit/${data}" class="w-8 h-8 bg-success-100 dark:bg-success-600/25 text-success-600 dark:text-success-400 rounded-full inline-flex items-center justify-center mr-1" title="Edit Product">
                                <iconify-icon icon="lucide:edit"></iconify-icon>
                            </a>`;
                        }

                        // Show delete button only if user has Delete permission
                        if (row.hasDeleteAccess) {
                            actions += `<a href="javascript:void(0)" class="delete-product w-8 h-8 bg-danger-100 dark:bg-danger-600/25 text-danger-600 dark:text-danger-400 rounded-full inline-flex items-center justify-center" data-id="${data}" title="Delete Product">
                                <iconify-icon icon="mingcute:delete-2-line"></iconify-icon>
                            </a>`;
                        }

                        actions += `</div>`;
                        return actions;
                    }
                }
            ],
            columnDefs: [
                { targets: [0, 7], sortable: false }
            ],
            order: [[2, 'asc']],
            dom: '<"flex justify-between items-center mb-4"<"flex"f><"flex-1 text-right"l>>rt<"flex justify-between items-center mt-4"<"flex-1"i><"flex"p>>',
            language: {
                search: "",
                searchPlaceholder: "Search Products...",
                lengthMenu: "_MENU_ per page",
                info: "Showing _START_ to _END_ of _TOTAL_ products",
                paginate: {
                    first: '<iconify-icon icon="heroicons-outline:chevron-double-left"></iconify-icon>',
                    last: '<iconify-icon icon="heroicons-outline:chevron-double-right"></iconify-icon>',
                    next: '<iconify-icon icon="heroicons-outline:chevron-right"></iconify-icon>',
                    previous: '<iconify-icon icon="heroicons-outline:chevron-left"></iconify-icon>'
                },
                zeroRecords: "No matching products found",
                emptyTable: "No products available"
            },

            drawCallback: function () {
                // Set up delete confirmation
                $('.delete-product').on('click', function () {
                    const productId = $(this).data('id');
                    confirmDelete(productId);
                });

                // Apply dark mode styling conditionally
                applyDarkModeToTable();
            }
        });

        // Set up additional event handlers
        setupEventHandlers();
    }

    // Set up event handlers
    function setupEventHandlers() {
        // Handle select all checkbox
        $('#select-all').on('change', function () {
            const isChecked = $(this).prop('checked');
            $('.product-select').prop('checked', isChecked);
        });

        // Set up dark mode handling
        applyEventForDataTableDarkMode();
    }

    // Delete confirmation function
    function confirmDelete(productId) {
        if (confirm('Are you sure you want to delete this product? This action cannot be undone.')) {
            $.ajax({
                url: `/Product/Delete`,
                type: 'POST',
                data: { productId: productId },
                headers: {
                    'X-CSRF-TOKEN': $('meta[name="csrf-token"]').attr('content')
                },
                success: function (data) {
                    if (data.success) {
                        ProductsTable.ajax.reload();
                        showSuccessToast('Product deleted successfully');
                    } else {
                        showErrorToast(data.message || 'Failed to delete product');
                    }
                },
                error: function (xhr) {
                    console.error('Delete error:', xhr);
                    let errorMessage = 'Error deleting product';

                    if (xhr.responseJSON && xhr.responseJSON.message) {
                        errorMessage = xhr.responseJSON.message;
                    } else if (xhr.status === 400) {
                        errorMessage = 'Bad request - please check the product data';
                    } else if (xhr.status === 404) {
                        errorMessage = 'Product not found';
                    } else if (xhr.status === 500) {
                        errorMessage = 'Server error occurred';
                    }

                    showErrorToast(errorMessage);
                }
            });
        }
    }

    // Utility function to escape HTML
    function escapeHtml(text) {
        if (!text) return '';
        const map = {
            '&': '&amp;',
            '<': '&lt;',
            '>': '&gt;',
            '"': '&quot;',
            "'": '&#039;'
        };
        return text.toString().replace(/[&<>"']/g, function (m) { return map[m]; });
    }

    // Expose public methods
    window.ProductTableModule = {
        init: init,
        reloadTable: function () {
            if (ProductsTable) {
                ProductsTable.ajax.reload();
            }
        }
    };
})();

// Initialize the module
ProductTableModule.init();