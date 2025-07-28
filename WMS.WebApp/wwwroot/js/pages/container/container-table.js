// Container Table Module
(function () {
    // Private variables
    let ContainerTable;
    let currentUploadContainerId = null;
    let currentPhotos = [];
    let currentPhotoPage = 1;
    const photosPerPage = 12;
    let uploadFiles = [];

    // Slideshow variables
    let slideshowPhotos = [];
    let currentSlideshowIndex = 0;
    let currentZoom = 1;
    let currentReplacePhotoId = null;
    let viewMode = 'fit'; // 'fit' or 'actual'
    let originalImageSize = { width: 0, height: 0 };
    let containerSize = { width: 0, height: 0 };

    // Container Pallets Modal Variables
    let currentContainerPalletsId = null;
    let containerPallets = [];
    let currentPalletPage = 1;
    let palletPageSize = 10;
    let palletTotalPages = 1;
    let palletTotalCount = 0;
    let palletSearchTimeout = null;

    // View Attachments Modal Variables
    let currentAttachmentContainerId = null;
    let currentAttachments = [];

    const CONTAINER_CONFIG = {
        import: {
            columns: [
                { key: 'containerNo_GW', title: 'Container No', visible: true },
                { key: 'concatePO', title: 'POs', visible: true },
                { key: 'plannedDelivery_GW', title: 'Planned Delivery', visible: true },
                { key: 'remarks', title: 'Remarks', visible: true },
                { key: 'unstuffedBy', title: 'Unstuffed By', visible: true },
                { key: 'unstuffedDate', title: 'Unstuffed Date', visible: true },
                { key: 'containerURL', title: 'URL', visible: true },
                { key: 'isLoose', title: 'IsLoose', visible: true },
                { key: 'isSamplingArrAtWarehouse', title: 'IsSampling', visible: true },
                { key: 'isGinger', title: 'IsGinger', visible: true },
                { key: 'photos', title: 'Photos', visible: true },
                { key: 'addPhoto', title: 'Add Photo', visible: true },
                { key: 'attachments', title: 'ATCH', visible: true },
                { key: 'report', title: 'Report', visible: true }
            ]
        },
        export: {
            columns: [
                { key: 'jobReference', title: 'Job Reference', visible: true },
                { key: 'containerNo_GW', title: 'Container No', visible: true },
                { key: 'sealNo', title: 'Seal', visible: true },
                { key: 'size', title: 'Size', visible: true },
                { key: 'remarks', title: 'Remarks', visible: true },
                { key: 'stuffedDate', title: 'Stuffing Date', visible: true },
                { key: 'stuffedBy', title: 'Stuffing By', visible: true },
                { key: 'containerURL', title: 'URL', visible: true },
                { key: 'photos', title: 'Photos', visible: true },
                { key: 'addPhoto', title: 'Add Photo', visible: true },
                { key: 'report', title: 'Report', visible: true }
            ]
        }
    };
    // Initialize module
    function init() {
        document.addEventListener('DOMContentLoaded', function () {
            if (document.getElementById('container-table')) {
                initializeContainerDataTable();
                setupEventHandlers();
            }
        });
    }

    // Table initialization function
    function initializeContainerDataTable() {
        const token = document.querySelector('input[name="__RequestVerificationToken"]').value;
        const containerType = window.containerType || 'import';
        const config = CONTAINER_CONFIG[containerType];

        const canEdit = window.hasEditAccess === true || window.hasEditAccess === 'true';
        const canDelete = window.hasDeleteAccess === true || window.hasDeleteAccess === 'true';
        const canView = window.hasViewAccess === true || window.hasViewAccess === 'true';

        // Build table headers dynamically
        buildTableHeaders(config.columns);

        ContainerTable = $('#container-table').DataTable({
            processing: true,
            serverSide: true,
            responsive: true,
            ajax: {
                url: '/Container/GetPaginatedContainers',
                type: 'POST',
                headers: {
                    'X-Requested-With': 'XMLHttpRequest'
                },
                data: function (d) {
                    d.__RequestVerificationToken = token;
                    d.containerType = containerType; // Send container type to server
                    return d;
                },
                error: function (xhr, error) {
                    console.error('Failed to load containers:', error);
                }
            },
            columns: buildDynamicColumns(config.columns, canEdit, canDelete, canView),
            columnDefs: [
                { targets: '_all', sortable: true }
            ],
            order: containerType === 'export' ? [[5, 'desc']] : [[5, 'desc']], // Sort by date column
            dom: '<"flex justify-between items-center mb-4"<"flex"f><"flex-1 text-right"l>>rt<"flex justify-between items-center mt-4"<"flex-1"i><"flex"p>>',
            language: {
                search: "",
                searchPlaceholder: `Search ${containerType} containers...`,
                lengthMenu: "_MENU_ per page",
                info: `Showing _START_ to _END_ of _TOTAL_ ${containerType} containers`,
                paginate: {
                    first: '<iconify-icon icon="heroicons-outline:chevron-double-left"></iconify-icon>',
                    last: '<iconify-icon icon="heroicons-outline:chevron-double-right"></iconify-icon>',
                    next: '<iconify-icon icon="heroicons-outline:chevron-right"></iconify-icon>',
                    previous: '<iconify-icon icon="heroicons-outline:chevron-left"></iconify-icon>'
                },
                zeroRecords: `No matching ${containerType} containers found`,
                emptyTable: `No ${containerType} container data available`
            },
            drawCallback: function () {
                setupRowEventHandlers();
                applyDarkModeToTable();
            }
        });
    }

    // ADD these new helper functions:
    function buildTableHeaders(columns) {
        const headerRow = $('#table-header');
        headerRow.empty();

        columns.forEach(col => {
            if (col.visible) {
                let headerClass = 'text-neutral-800 dark:text-white';
                let headerContent = `<div class="flex items-center gap-2">${col.title}</div>`;

                // Special handling for checkbox columns
                if (col.key === 'isLoose' || col.key === 'isSamplingArrAtWarehouse' || col.key == 'isGinger') {
                    headerContent = `<div class="form-check style-check flex items-center">
                    <label class="ms-2 form-check-label">${col.title}</label>
                </div>`;
                }

                headerRow.append(`<th scope="col" class="${headerClass}">${headerContent}</th>`);
            }
        });
    }

    function buildDynamicColumns(columns, canEdit, canDelete, canView) {
        const dynamicColumns = [];

        columns.forEach(col => {
            if (col.visible) {
                switch (col.key) {
                    case 'jobReference':
                        dynamicColumns.push({
                            data: 'jobReference',
                            render: function (data) {
                                return data ? `<span class="text-sm font-medium text-gray-900 dark:text-white">${escapeHtml(data)}</span>` : '<span class="text-gray-400">-</span>';
                            }
                        });
                        break;

                    case 'containerNo_GW':
                        dynamicColumns.push({
                            data: 'containerNo_GW',
                            render: function (data) {
                                return data ? `<span class="text-sm font-medium text-gray-900 dark:text-white">${escapeHtml(data)}</span>` : '<span class="text-gray-400">-</span>';
                            }
                        });
                        break;

                    case 'sealNo':
                        dynamicColumns.push({
                            data: 'sealNo',
                            render: function (data, type, row) {
                                if (!canEdit) {
                                    return data ? `<span class="text-sm text-gray-600 dark:text-gray-400">${escapeHtml(data)}</span>` : '<span class="text-gray-400">-</span>';
                                }
                                return `<input type="text" class="form-control form-control-sm inline-edit w-full px-3 py-1.5 text-sm border border-neutral-200 dark:border-neutral-600 rounded-lg text-gray-900 dark:text-white"
                                           data-id="${row.containerId}" data-field="SealNo" value="${escapeHtml(data || '')}" />`;
                            }
                        });
                        break;

                    case 'size':
                        dynamicColumns.push({
                            data: 'size',
                            render: function (data, type, row) {
                                if (!canEdit) {
                                    return data ? `<span class="text-sm text-gray-600 dark:text-gray-400">${data}</span>` : '<span class="text-gray-400">-</span>';
                                }
                                return `<input type="number" class="form-control form-control-sm inline-edit w-full px-3 py-1.5 text-sm border border-neutral-200 dark:border-neutral-600 rounded-lg text-gray-900 dark:text-white"
                                           data-id="${row.containerId}" data-field="Size" value="${data || ''}" />`;
                            }
                        });
                        break;

                    case 'stuffedDate':
                        dynamicColumns.push({
                            data: 'stuffedDate',
                            render: function (data, type, row) {
                                if (!canEdit) {
                                    if (!data) return '<span class="text-gray-400">-</span>';
                                    const formatted = new Date(data).toISOString().split('T')[0];
                                    return `<span class="text-sm text-gray-600 dark:text-gray-400">${formatted}</span>`;
                                }
                                const val = data ? new Date(data).toISOString().split('T')[0] : '';
                                return `<input type="date" class="form-control form-control-sm inline-edit w-full px-3 py-1.5 text-sm border border-neutral-200 dark:border-neutral-600 rounded-lg text-gray-900 dark:text-white"
                                           data-id="${row.containerId}" data-field="StuffedDate" value="${val}" />`;
                            }
                        });
                        break;

                    case 'stuffedBy':
                        dynamicColumns.push({
                            data: 'stuffedBy',
                            render: function (data, type, row) {
                                if (!canEdit) {
                                    return data ? `<span class="text-sm text-gray-600 dark:text-gray-400">${escapeHtml(data)}</span>` : '<span class="text-gray-400">-</span>';
                                }
                                return `<input type="text" class="form-control form-control-sm inline-edit w-full px-3 py-1.5 text-sm border border-neutral-200 dark:border-neutral-600 rounded-lg text-gray-900 dark:text-white"
                                           data-id="${row.containerId}" data-field="StuffedBy" value="${escapeHtml(data || '')}" />`;
                            }
                        });
                        break;

                    case 'concatePO':
                        dynamicColumns.push({
                            data: 'concatePO',
                            render: function (data, type, row) {
                                if (!data) {
                                    return '<span class="text-gray-400">-</span>';
                                }
                                if (type === 'export' || type === 'print' || type === 'type') {
                                    return data;
                                }
                                const fullText = escapeHtml(data);
                                const maxLength = 11;
                                if (fullText.length <= maxLength) {
                                    return `<span class="text-sm font-medium text-gray-900 dark:text-white">${fullText}</span>`;
                                }
                                const truncatedText = fullText.substring(0, maxLength);
                                return `<span class="text-sm font-medium text-gray-900 dark:text-white truncate-with-tooltip cursor-help" 
                                          title="${fullText}"
                                          data-full-text="${fullText}">
                                        ${truncatedText}...
                                    </span>`;
                            }
                        });
                        break;

                    case 'plannedDelivery_GW':
                        dynamicColumns.push({
                            data: 'plannedDelivery_GW',
                            render: function (data) {
                                if (!data) return '<span class="text-gray-400">-</span>';
                                const formatted = new Date(data).toISOString().split('T')[0];
                                return `<span class="text-sm text-gray-600 dark:text-gray-400">${formatted}</span>`;
                            }
                        });
                        break;

                    case 'remarks':
                        dynamicColumns.push({
                            data: 'remarks',
                            render: function (data, type, row) {
                                if (!canEdit) {
                                    return data ? `<span class="text-sm text-gray-600 dark:text-gray-400">${escapeHtml(data)}</span>` : '<span class="text-gray-400">-</span>';
                                }
                                return `<input type="text" class="form-control form-control-sm inline-edit w-full px-3 py-1.5 text-sm border border-neutral-200 dark:border-neutral-600 rounded-lg text-gray-900 dark:text-white"
                                           data-id="${row.containerId}" data-field="Remarks" value="${escapeHtml(data || '')}" />`;
                            }
                        });
                        break;

                    case 'unstuffedBy':
                        dynamicColumns.push({
                            data: 'unstuffedBy',
                            render: function (data, type, row) {
                                if (!canEdit) {
                                    return data ? `<span class="text-sm text-gray-600 dark:text-gray-400">${escapeHtml(data)}</span>` : '<span class="text-gray-400">-</span>';
                                }
                                return `<input type="text" class="form-control form-control-sm inline-edit w-full px-3 py-1.5 text-sm border border-neutral-200 dark:border-neutral-600 rounded-lg text-gray-900 dark:text-white"
                                           data-id="${row.containerId}" data-field="UnstuffedBy" value="${escapeHtml(data || '')}" />`;
                            }
                        });
                        break;

                    case 'unstuffedDate':
                        dynamicColumns.push({
                            data: 'unstuffedDate',
                            render: function (data, type, row) {
                                if (!canEdit) {
                                    if (!data) return '<span class="text-gray-400">-</span>';
                                    const formatted = new Date(data).toISOString().split('T')[0];
                                    return `<span class="text-sm text-gray-600 dark:text-gray-400">${formatted}</span>`;
                                }
                                const val = data ? new Date(data).toISOString().split('T')[0] : '';
                                return `<input type="date" class="form-control form-control-sm inline-edit w-full px-3 py-1.5 text-sm border border-neutral-200 dark:border-neutral-600 rounded-lg text-gray-900 dark:text-white"
                                           data-id="${row.containerId}" data-field="UnstuffedDate" value="${val}" />`;
                            }
                        });
                        break;

                    case 'containerURL':
                        dynamicColumns.push({
                            data: 'containerURL',
                            render: function (data) {
                                if (!data) return '<span class="text-gray-400">No URL</span>';
                                return `<a href="${escapeHtml(data)}" target="_blank" 
                                       class="inline-flex items-center gap-1 px-3 py-1.5 text-sm font-medium text-primary-600 dark:text-primary-400 bg-primary-50 dark:bg-primary-900/20 rounded-lg hover:bg-primary-100 dark:hover:bg-primary-900/30">
                                        <iconify-icon icon="mdi:external-link" class="text-base"></iconify-icon>
                                        View
                                    </a>`;
                            }
                        });
                        break;

                    case 'isLoose':
                        dynamicColumns.push({
                            data: 'isLoose',
                            width: '5%',
                            render: function (data) {
                                const isChecked = data === true ? 'checked' : '';
                                return `<div class="form-check style-check flex items-center">
                                        <input class="form-check-input product-select disabled" disabled type="checkbox" value="${data}" ${isChecked}>
                                    </div>`;
                            }
                        });
                        break;

                    case 'isSamplingArrAtWarehouse':
                        dynamicColumns.push({
                            data: 'isSamplingArrAtWarehouse',
                            width: '5%',
                            render: function (data) {
                                const isChecked = data === true ? 'checked' : '';
                                return `<div class="form-check style-check flex items-center">
                                        <input class="form-check-input product-select disabled" disabled type="checkbox" value="${data}" ${isChecked}>
                                    </div>`;
                            }
                        });
                        break;
                    case 'isGinger':
                        dynamicColumns.push({
                            data: 'isGinger',
                            width: '5%',
                            render: function (data) {
                                const isChecked = data === true ? 'checked' : '';
                                return `<div class="form-check style-check flex items-center">
                                        <input class="form-check-input product-select disabled" disabled type="checkbox" value="${data}" ${isChecked}>
                                    </div>`;
                            }
                        });
                        break;
                    case 'photos':
                        dynamicColumns.push({
                            data: 'containerPhotos',
                            orderable: false,
                            searchable: false,
                            render: function (photos, type, row) {
                                if (!photos || !photos.length) {
                                    return '<span class="text-gray-400 text-sm">No Photos</span>';
                                }
                                const photoCount = photos.length;
                                return `<button class="view-photos-btn inline-flex items-center gap-2 px-3 py-1.5 text-sm font-medium text-blue-600 dark:text-blue-400 bg-blue-50 dark:bg-blue-900/20 rounded-lg hover:bg-blue-100 dark:hover:bg-blue-900/30" 
                                           data-id="${row.containerId}" data-photos='${JSON.stringify(photos)}' title="View Photos">
                                        <iconify-icon icon="mdi:image-multiple" class="text-base"></iconify-icon>
                                        ${photoCount} Photo${photoCount > 1 ? 's' : ''}
                                    </button>`;
                            }
                        });
                        break;

                    case 'addPhoto':
                        dynamicColumns.push({
                            data: null,
                            orderable: false,
                            searchable: false,
                            render: function (data, type, row) {
                                if (!canEdit) return '<span class="text-gray-400">-</span>';
                                return `<button class="upload-photo-btn inline-flex items-center gap-2 px-3 py-1.5 text-sm font-medium text-green-600 dark:text-green-400 bg-green-50 dark:bg-green-900/20 rounded-lg hover:bg-green-100 dark:hover:bg-green-900/30" 
                                           data-id="${row.containerId}" title="Upload Photos">
                                        <iconify-icon icon="mdi:cloud-upload" class="text-base"></iconify-icon>
                                        Upload
                                    </button>`;
                            }
                        });
                        break;

                    case 'attachments':
                        dynamicColumns.push({
                            data: null,
                            orderable: false,
                            searchable: false,
                            render: function (data, type, row) {
                                if (!row.jobId && row.jobId == 0) return '<span class="text-gray-400">-</span>';
                                return `<button class="view-attachments-btn inline-flex items-center gap-2 px-3 py-1.5 text-sm font-medium text-indigo-600 dark:text-indigo-400 bg-indigo-50 dark:bg-indigo-900/20 rounded-lg hover:bg-indigo-100 dark:hover:bg-indigo-900/30" 
                                            data-id="${row.containerId}" 
                                            data-job-id="${row.jobId}"
                                            title="View Attachments">
                                        <iconify-icon icon="mdi:paperclip" class="text-base"></iconify-icon>
                                        Attachments
                                    </button>`;
                            }
                        });
                        break;

                    case 'report':
                        dynamicColumns.push({
                            data: null,
                            orderable: false,
                            searchable: false,
                            render: function (data, type, row) {
                                if (!canView) return '<span class="text-gray-400">-</span>';

                                const containerType = window.containerType || 'import';
                                const dateField = containerType === 'export' ? 'stuffedDate' : 'unstuffedDate';

                                let actionsHtml = '<div class="flex items-center gap-2">';

                                if (row[dateField]) {
                                    actionsHtml += `
                                    <button class="generate-report-btn w-8 h-8 bg-success-100 dark:bg-success-600/25 text-success-600 dark:text-success-400 rounded-full inline-flex items-center justify-center hover:bg-success-200 dark:hover:bg-success-600/35" 
                                            data-id="${row.containerId}" title="Generate Report">
                                        <iconify-icon icon="mdi:file-excel-outline" class="text-base"></iconify-icon>
                                    </button>`;

                                    actionsHtml += `
                                    <button class="link-receive-btn inline-flex items-center gap-2 px-3 py-1.5 text-sm font-medium text-purple-600 dark:text-purple-400 bg-purple-50 dark:bg-purple-900/20 rounded-lg hover:bg-purple-100 dark:hover:bg-purple-900/30"
                                            data-id="${row.containerId}" 
                                            data-container-no="${row.containerNo_GW}"
                                            title="Receive Pallets">
                                        <iconify-icon icon="mdi:link-variant" class="text-base"></iconify-icon>
                                        Receive Pallets
                                    </button>`;
                                } else {
                                    actionsHtml += `
                                    <button class="link-receive-btn inline-flex items-center gap-2 px-3 py-1.5 text-sm font-medium text-purple-600 dark:text-purple-400 bg-purple-50 dark:bg-purple-900/20 rounded-lg hover:bg-purple-100 dark:hover:bg-purple-900/30"
                                           data-id="${row.containerId}" 
                                           data-container-no="${row.containerNo_GW}"
                                           title="Receive Pallets">
                                        <iconify-icon icon="mdi:link-variant" class="text-base"></iconify-icon>
                                        Receive Pallets
                                    </button>`;
                                }

                                actionsHtml += '</div>';
                                return actionsHtml;
                            }
                        });
                        break;
                }
            }
        });

        return dynamicColumns;
    }
    
    // Set up event handlers
    function setupEventHandlers() {
        const token = document.querySelector('input[name="__RequestVerificationToken"]').value;

        // Upload modal handlers
        $('#uploadInput').on('change', function () {
            const files = Array.from(this.files);
            uploadFiles = files;
            updateUploadPreview();
        });

        $('#clearAllPhotos').on('click', function () {
            uploadFiles = [];
            $('#uploadInput').val('');
            updateUploadPreview();
        });

        $('#uploadConfirmBtn').on('click', function () {
            if (!uploadFiles.length) {
                showErrorToast("Please select at least one photo to upload.");
                return;
            }

            if (!currentUploadContainerId) {
                showErrorToast("Container ID not found.");
                return;
            }

            const formData = new FormData();
            formData.append("Username", "admin");
            formData.append("Id", currentUploadContainerId);
            formData.append("__RequestVerificationToken", token);

            uploadFiles.forEach(file => {
                formData.append("Photos", file);
            });

            $('#uploadConfirmBtn').prop('disabled', true).html(`
                <iconify-icon icon="heroicons:arrow-path" class="animate-spin"></iconify-icon>
                Uploading...
            `);

            $.ajax({
                url: '/Container/Update',
                method: 'POST',
                data: formData,
                contentType: false,
                processData: false,
                success: function () {
                    showSuccessToast(`${uploadFiles.length} photo(s) uploaded successfully.`);
                    ContainerTable.ajax.reload(null, false);
                    closeModal('uploadPhotoModal');
                    resetUploadModal();
                },
                error: function () {
                    showErrorToast("Failed to upload photos.");
                },
                complete: function () {
                    $('#uploadConfirmBtn').prop('disabled', false).html(`
                        <iconify-icon icon="heroicons:cloud-arrow-up"></iconify-icon>
                        Upload <span id="uploadBtnCount">${uploadFiles.length}</span> Photos
                    `);
                }
            });
        });

        // Modal close handlers
        $('#closePhotoGalleryModal, #closePhotoGalleryModalBtn').on('click', function () {
            closeModal('photoGalleryModal');
        });

        $('#closeUploadPhotoModal, #cancelUploadBtn').on('click', function () {
            closeModal('uploadPhotoModal');
        });

        $('#closeSlideshowModal').on('click', function () {
            closeModal('slideshowModal');
        });

        // Slideshow controls
        $('#prevImageBtn').on('click', function () {
            navigateSlideshow(-1); // Go to previous image
        });

        $('#nextImageBtn').on('click', function () {
            navigateSlideshow(1); // Go to next image
        });

        // View mode toggle handlers
        $('#fitToWindowBtn').on('click', function () {
            setViewMode('fit');
        });

        $('#actualSizeBtn').on('click', function () {
            setViewMode('actual');
        });

        // Image click-to-zoom toggle
        $(document).on('click', '#slideshowImage', function (e) {
            e.preventDefault();
            e.stopPropagation();
            toggleZoom();
        });

        // Photo pagination handlers
        $('#prevPhotoPage').on('click', function () {
            if (currentPhotoPage > 1) {
                currentPhotoPage--;
                renderPhotos();
            }
        });

        $('#nextPhotoPage').on('click', function () {
            const totalPages = Math.ceil(currentPhotos.length / photosPerPage);
            if (currentPhotoPage < totalPages) {
                currentPhotoPage++;
                renderPhotos();
            }
        });

        // Replace photo input handler
        $('#replacePhotoInput').on('change', function () {
            const file = this.files[0];
            if (!file || !currentReplacePhotoId) return;

            const formData = new FormData();
            formData.append("file", file);

            $.ajax({
                url: `/Container/ReplacePhoto/${currentReplacePhotoId}`,
                method: 'POST',
                data: formData,
                contentType: false,
                processData: false,
                success: function () {
                    showSuccessToast("Photo replaced successfully.");
                    ContainerTable.ajax.reload(null, false);
                    closeModal('photoGalleryModal');
                    closeModal('slideshowModal');
                },
                error: function () {
                    showErrorToast("Failed to replace photo.");
                }
            });

            // Reset
            currentReplacePhotoId = null;
            $('#replacePhotoInput').val('');
        });

        // Close modals when clicking outside
        $('#photoGalleryModal, #uploadPhotoModal, #slideshowModal').on('click', function (e) {
            if (e.target === this) {
                closeModal(this.id);
            }
        });

        // Keyboard navigation
        $(document).on('keydown', function (e) {
            if ($('#slideshowModal').hasClass('hidden')) {
                if (e.key === 'Escape') {
                    closeModal('photoGalleryModal');
                    closeModal('uploadPhotoModal');
                }
            } else {
                switch (e.key) {
                    case 'Escape':
                        closeModal('slideshowModal');
                        break;
                    case 'ArrowLeft':
                        navigateSlideshow(-1);
                        break;
                    case 'ArrowRight':
                        navigateSlideshow(1);
                        break;
                    case '+':
                    case '=':
                        // Removed manual zoom controls
                        break;
                    case '-':
                        // Removed manual zoom controls
                        break;
                    case '0':
                        setViewMode('fit');
                        break;
                    case '1':
                        setViewMode('actual');
                        break;
                    case ' ':
                        e.preventDefault();
                        toggleZoom();
                        break;
                }
            }
        });

        setupContainerPalletsHandlers();

        // View Attachments modal handlers
        $('#closeViewAttachmentsModal, #closeViewAttachmentsModalBtn').on('click', function () {
            closeModal('viewAttachmentsModal');
        });

        // Close modal when clicking outside
        $('#viewAttachmentsModal').on('click', function (e) {
            if (e.target === this) {
                closeModal('viewAttachmentsModal');
            }
        });

        // View attachments handlers
        $(document).on('click', '.view-attachments-btn', function (e) {
            e.preventDefault();
            e.stopPropagation();

            const containerId = $(this).data('id');
            const jobId = $(this).data('job-id');

            console.log('View attachments clicked:', { containerId, jobId }); // Debug log

            if (!jobId || jobId <= 0) {
                showErrorToast('No job ID found for this container');
                return;
            }

            currentAttachmentContainerId = containerId;
            showAttachmentsModal(containerId, jobId);
        });
        // Generate report handlers using event delegation
        $(document).on('click', '.generate-report-btn', function (e) {
            e.preventDefault();
            e.stopPropagation();

            const containerId = $(this).data('id');
            console.log('Generate report clicked for container:', containerId); // Debug log

            if (!containerId) {
                showErrorToast('Container ID not found');
                return;
            }

            // Show loading state on button
            const $button = $(this);
            const originalHtml = $button.html();
            $button.prop('disabled', true).html(`
                <iconify-icon icon="heroicons:arrow-path" class="animate-spin text-base"></iconify-icon>
            `);

            // Navigate to download URL
            window.location.href = `/Container/DownloadReport/${containerId}`;

            // Restore button after a delay (in case download doesn't work)
            setTimeout(() => {
                $button.prop('disabled', false).html(originalHtml);
            }, 3000);
        });
    }

    // Set up row-specific event handlers
    function setupRowEventHandlers() {
        const token = document.querySelector('input[name="__RequestVerificationToken"]').value;

        // Inline edit handlers
        if (window.hasEditAccess) {
            $('.inline-edit').off('change').on('change', function () {
                const formData = new FormData();
                formData.append("Username", "admin");
                formData.append("Id", $(this).data('id'));
                formData.append($(this).data('field'), $(this).val());
                formData.append("__RequestVerificationToken", token);

                $.ajax({
                    url: '/Container/Update',
                    method: 'POST',
                    data: formData,
                    contentType: false,
                    processData: false,
                    success: function () {
                        showSuccessToast("Container updated successfully.");
                    },
                    error: function () {
                        showErrorToast("Failed to update container.");
                    }
                });
            });
        }

        // View photos handlers
        $('.view-photos-btn').off('click').on('click', function (e) {
            e.preventDefault();
            e.stopPropagation();

            try {
                const photosData = $(this).data('photos');
                let photos;

                if (typeof photosData === 'string') {
                    photos = JSON.parse(photosData);
                } else if (Array.isArray(photosData)) {
                    photos = photosData;
                } else {
                    console.error('Invalid photos data:', photosData);
                    showErrorToast('Invalid photo data');
                    return;
                }

                if (!photos || photos.length === 0) {
                    showErrorToast('No photos to display');
                    return;
                }

                showPhotoGallery(photos);
            } catch (error) {
                console.error('Error parsing photos data:', error);
                showErrorToast('Error loading photos');
            }
        });

        // Upload photo handlers
        $('.upload-photo-btn').off('click').on('click', function () {
            currentUploadContainerId = $(this).data('id');
            showModal('uploadPhotoModal');
        });
    }

    // Show photo gallery modal
    function showPhotoGallery(photos) {
        currentPhotos = photos || [];
        currentPhotoPage = 1;

        $('#photoCount').text(currentPhotos.length);

        if (currentPhotos.length === 0) {
            $('#photo-gallery').html('<div class="col-span-full text-center py-8"><span class="text-gray-400">No photos available</span></div>');
            $('#photoPagination').hide();
        } else {
            $('#photoPagination').show();
            renderPhotos();
        }

        showModal('photoGalleryModal');
    }

    // Render photos with pagination
    function renderPhotos() {
        const galleryContainer = $('#photo-gallery');
        galleryContainer.empty();

        const startIndex = (currentPhotoPage - 1) * photosPerPage;
        const endIndex = startIndex + photosPerPage;
        const photosToShow = currentPhotos.slice(startIndex, endIndex);

        const canDelete = window.hasDeleteAccess === true || window.hasDeleteAccess === 'true';
        const canEdit = window.hasEditAccess === true || window.hasEditAccess === 'true';

        photosToShow.forEach(function (photo, index) {
            if (!photo.url) {
                console.warn('Photo missing URL:', photo);
                return;
            }

            const globalIndex = startIndex + index;
            const photoElement = `
                <div class="flex flex-col">
                    <!-- Image -->
                    <div class="relative mb-2">
                        <img src="${escapeHtml(photo.url)}" 
                             class="w-full h-40 object-cover rounded-lg border border-neutral-200 dark:border-neutral-600 cursor-pointer hover:opacity-90 transition-opacity photo-thumbnail" 
                             alt="Container Photo ${globalIndex + 1}"
                             data-index="${globalIndex}"
                             onerror="this.src='data:image/svg+xml;base64,PHN2ZyB3aWR0aD0iMjQiIGhlaWdodD0iMjQiIHZpZXdCb3g9IjAgMCAyNCAyNCIgZmlsbD0ibm9uZSIgeG1sbnM9Imh0dHA6Ly93d3cudzMub3JnLzIwMDAvc3ZnIj4KPHBhdGggZD0iTTEyIDJMMTMuMDkgOC4yNkwyMCA5TDEzLjA5IDE1Ljc0TDEyIDIyTDEwLjkxIDE1Ljc0TDQgOUwxMC45MSA4LjI2TDEyIDJaIiBzdHJva2U9IiM5Q0E0QUYiIHN0cm9rZS13aWR0aD0iMiIgc3Ryb2tlLWxpbmVjYXA9InJvdW5kIiBzdHJva2UtbGluZWpvaW49InJvdW5kIi8+Cjwvc3ZnPgo='; this.alt='Image not available';">
                    </div>
                    
                    <!-- Action Buttons -->
                    <div class="flex items-center gap-2 justify-center">
                        ${canEdit ? `
                            <button class="replace-photo-btn flex items-center gap-1 px-2 py-1 text-xs font-medium text-yellow-600 dark:text-yellow-400 bg-yellow-50 dark:bg-yellow-900/20 rounded hover:bg-yellow-100 dark:hover:bg-yellow-900/30 transition-colors" 
                                    data-id="${photo.id}" title="Replace Photo">
                                <iconify-icon icon="mdi:file-replace-outline" class="text-sm"></iconify-icon>
                                Replace
                            </button>
                        ` : ''}
                        ${canDelete ? `
                            <button class="delete-photo-btn flex items-center gap-1 px-2 py-1 text-xs font-medium text-red-600 dark:text-red-400 bg-red-50 dark:bg-red-900/20 rounded hover:bg-red-100 dark:hover:bg-red-900/30 transition-colors" 
                                    data-id="${photo.id}" data-key="${photo.s3Key || ''}" title="Delete Photo">
                                <iconify-icon icon="mdi:trash-can-outline" class="text-sm"></iconify-icon>
                                Delete
                            </button>
                        ` : ''}
                    </div>
                </div>
            `;
            galleryContainer.append(photoElement);
        });

        updatePhotoPagination();
        setupPhotoActionHandlers();
    }

    // Update pagination controls
    function updatePhotoPagination() {
        const totalPages = Math.ceil(currentPhotos.length / photosPerPage);

        $('#photoPageInfo').text(`Page ${currentPhotoPage} of ${totalPages}`);

        $('#prevPhotoPage').prop('disabled', currentPhotoPage === 1);
        $('#nextPhotoPage').prop('disabled', currentPhotoPage === totalPages);

        if (totalPages <= 1) {
            $('#photoPagination').hide();
        } else {
            $('#photoPagination').show();
        }
    }

    // Set up photo action handlers
    function setupPhotoActionHandlers() {
        // Photo thumbnail click handlers (open slideshow)
        $('.photo-thumbnail').off('click').on('click', function () {
            const index = parseInt($(this).data('index'));
            openSlideshow(index);
        });

        // Delete photo handlers
        $('.delete-photo-btn').off('click').on('click', function (e) {
            e.stopPropagation();
            const photoId = $(this).data('id');

            if (confirm('Are you sure you want to delete this photo?')) {
                const token = document.querySelector('input[name="__RequestVerificationToken"]').value;

                $.ajax({
                    url: `/Container/DeletePhoto/${photoId}`,
                    method: 'DELETE',
                    headers: { 'X-CSRF-TOKEN': token },
                    success: function () {
                        showSuccessToast("Photo deleted successfully.");
                        ContainerTable.ajax.reload(null, false);
                        closeModal('photoGalleryModal');
                    },
                    error: function () {
                        showErrorToast("Failed to delete photo.");
                    }
                });
            }
        });

        // Replace photo handlers
        $('.replace-photo-btn').off('click').on('click', function (e) {
            e.stopPropagation();
            currentReplacePhotoId = $(this).data('id');
            $('#replacePhotoInput').click();
        });
    }

    function openSlideshow(index) {
        slideshowPhotos = [...currentPhotos];
        currentSlideshowIndex = index;
        showModal('slideshowModal');

        // Wait for modal to be visible, then update image
        setTimeout(() => {
            updateSlideshowImage();
        }, 50);
    }

    function updateSlideshowImage() {
        if (!slideshowPhotos.length) return;

        const photo = slideshowPhotos[currentSlideshowIndex];
        const image = $('#slideshowImage');

        // Set image source and wait for load
        image.attr('src', photo.url);
        image.attr('alt', `Photo ${currentSlideshowIndex + 1} of ${slideshowPhotos.length}`);

        // Wait for image to load to get dimensions
        image.off('load').on('load', function () {
            originalImageSize.width = this.naturalWidth;
            originalImageSize.height = this.naturalHeight;

            // Update image info
            const fileSize = photo.fileSize ? formatFileSize(photo.fileSize) : '';
            $('#imageInfo').text(`(${originalImageSize.width}×${originalImageSize.height}${fileSize ? ' • ' + fileSize : ''})`);

            // Dynamically size the modal based on image aspect ratio
            resizeModalForImage();

            // Get accurate container size after modal resize
            measureContainerSize();

            // Apply current view mode
            if (viewMode === 'fit') {
                applyFitToWindowMode();
            } else {
                resetZoom();
            }
        });

        $('#currentPhotoIndex').text(currentSlideshowIndex + 1);
        $('#totalPhotosCount').text(slideshowPhotos.length);

        // Update navigation buttons
        updateNavigationButtons();
    }

    function resizeModalForImage() {
        if (originalImageSize.width === 0 || originalImageSize.height === 0) return;

        const imageAspectRatio = originalImageSize.width / originalImageSize.height;
        const viewportWidth = window.innerWidth;
        const viewportHeight = window.innerHeight;

        // Account for padding and margins (48px total padding + 120px for header/controls)
        const availableWidth = viewportWidth * 0.9;
        const availableHeight = viewportHeight * 0.9;
        const headerHeight = 120; // Approximate header + controls height

        let modalWidth, modalHeight;

        if (imageAspectRatio > 1.5) {
            // Wide landscape images - prioritize width
            modalWidth = Math.min(availableWidth, 1400);
            modalHeight = Math.min(availableHeight, modalWidth / 1.8 + headerHeight);
        } else if (imageAspectRatio < 0.7) {
            // Tall portrait images - prioritize height  
            modalHeight = availableHeight;
            modalWidth = Math.min(availableWidth, (modalHeight - headerHeight) * 1.2);
        } else {
            // Square-ish images - balanced approach
            modalWidth = Math.min(availableWidth, 1200);
            modalHeight = Math.min(availableHeight, modalWidth * 0.75 + headerHeight);
        }

        // Apply the calculated dimensions
        const $modal = $('#slideshowContainer');
        $modal.css({
            'width': `${modalWidth}px`,
            'height': `${modalHeight}px`,
            'max-width': 'none',
            'max-height': 'none'
        });
    }

    function measureContainerSize() {
        // Get the actual available space for the image
        const container = $('#imageContainer')[0];
        if (!container) return;

        const rect = container.getBoundingClientRect();

        // Account for padding (32px total) and potential scrollbars (20px)
        containerSize.width = rect.width - 32 - 20;
        containerSize.height = rect.height - 32 - 20;

        // Minimum size constraints
        containerSize.width = Math.max(containerSize.width, 200);
        containerSize.height = Math.max(containerSize.height, 150);
    }

    function updateNavigationButtons() {
        const $prevBtn = $('#prevImageBtn');
        const $nextBtn = $('#nextImageBtn');

        // Always show both buttons first
        $prevBtn.show();
        $nextBtn.show();

        // Then apply disabled states
        if (currentSlideshowIndex === 0) {
            $prevBtn.prop('disabled', true).addClass('opacity-30 cursor-not-allowed');
        } else {
            $prevBtn.prop('disabled', false).removeClass('opacity-30 cursor-not-allowed');
        }

        if (currentSlideshowIndex === slideshowPhotos.length - 1) {
            $nextBtn.prop('disabled', true).addClass('opacity-30 cursor-not-allowed');
        } else {
            $nextBtn.prop('disabled', false).removeClass('opacity-30 cursor-not-allowed');
        }

        // Only hide navigation if there's only one photo
        if (slideshowPhotos.length <= 1) {
            $prevBtn.hide();
            $nextBtn.hide();
        }

        // Debug: Log current state
        console.log('Navigation Update:', {
            currentIndex: currentSlideshowIndex,
            totalPhotos: slideshowPhotos.length,
            prevDisabled: $prevBtn.prop('disabled'),
            nextDisabled: $nextBtn.prop('disabled'),
            prevVisible: $prevBtn.is(':visible'),
            nextVisible: $nextBtn.is(':visible')
        });
    }

    function setViewMode(mode) {
        viewMode = mode;

        // Update button states
        if (mode === 'fit') {
            $('#fitToWindowBtn').addClass('bg-primary-600 text-white').removeClass('text-gray-700 dark:text-gray-300');
            $('#actualSizeBtn').removeClass('bg-primary-600 text-white').addClass('text-gray-700 dark:text-gray-300');
            applyFitToWindowMode();
        } else {
            $('#actualSizeBtn').addClass('bg-primary-600 text-white').removeClass('text-gray-700 dark:text-gray-300');
            $('#fitToWindowBtn').removeClass('bg-primary-600 text-white').addClass('text-gray-700 dark:text-gray-300');
            setActualSize();
        }
    }

    function toggleZoom() {
        if (viewMode === 'fit') {
            // Switch to 100% view
            setViewMode('actual');
        } else {
            // Switch back to fit view
            setViewMode('fit');
        }
    }

    function setActualSize() {
        currentZoom = 1;
        const image = $('#slideshowImage');
        image.css({
            'transform': 'scale(1)',
            'max-width': 'none',
            'max-height': 'none',
            'width': originalImageSize.width > 0 ? `${originalImageSize.width}px` : 'auto',
            'height': originalImageSize.height > 0 ? `${originalImageSize.height}px` : 'auto',
            'transform-origin': 'center center'
        });

        $('#zoomLevel').text('100%');
    }

    function applyFitToWindowMode() {
        if (originalImageSize.width === 0 || originalImageSize.height === 0 ||
            containerSize.width === 0 || containerSize.height === 0) return;

        // Calculate zoom to fit image in container
        const scaleX = containerSize.width / originalImageSize.width;
        const scaleY = containerSize.height / originalImageSize.height;

        // Choose the smaller scale to ensure entire image fits
        let optimalZoom = Math.min(scaleX, scaleY);

        // Set minimum zoom to prevent images from becoming too small
        const minZoom = 0.15; // 15% minimum
        optimalZoom = Math.max(optimalZoom, minZoom);

        // Don't scale up beyond 100% to maintain quality
        optimalZoom = Math.min(optimalZoom, 1);

        currentZoom = optimalZoom;

        const image = $('#slideshowImage');
        image.css({
            'transform': `scale(${currentZoom})`,
            'max-width': 'none',
            'max-height': 'none',
            'width': `${originalImageSize.width}px`,
            'height': `${originalImageSize.height}px`,
            'transform-origin': 'center center'
        });

        $('#zoomLevel').text(`${Math.round(currentZoom * 100)}%`);

        // Debug info (remove in production)
        console.log('Image Size:', originalImageSize);
        console.log('Container Size:', containerSize);
        console.log('Scale X:', scaleX.toFixed(3), 'Scale Y:', scaleY.toFixed(3));
        console.log('Optimal Zoom:', (optimalZoom * 100).toFixed(1) + '%');
    }

    function navigateSlideshow(direction) {
        const newIndex = currentSlideshowIndex + direction;

        console.log('Navigate:', {
            currentIndex: currentSlideshowIndex,
            direction: direction,
            newIndex: newIndex,
            totalPhotos: slideshowPhotos.length
        });

        if (newIndex >= 0 && newIndex < slideshowPhotos.length) {
            currentSlideshowIndex = newIndex;
            updateSlideshowImage();
        }
    }

    // Remove the old adjustZoom and resetZoom functions - they're no longer needed
    function formatFileSize(bytes) {
        if (!bytes) return '';
        const sizes = ['B', 'KB', 'MB', 'GB'];
        const i = Math.floor(Math.log(bytes) / Math.log(1024));
        return `${(bytes / Math.pow(1024, i)).toFixed(1)} ${sizes[i]}`;
    }

    // Upload functions
    function updateUploadPreview() {
        const previewContainer = $('#uploadPreview');
        const previewSection = $('#uploadPreviewSection');

        previewContainer.empty();

        if (uploadFiles.length === 0) {
            previewSection.addClass('hidden');
            updateUploadCounts(0);
            return;
        }

        previewSection.removeClass('hidden');

        uploadFiles.forEach((file, index) => {
            if (file.type.startsWith('image/')) {
                const reader = new FileReader();
                reader.onload = function (e) {
                    const preview = `
                        <div class="relative group w-[70px] h-[70px]">
                            <img src="${e.target.result}" class="w-[70px] h-[70px] object-cover rounded-lg border border-neutral-200 dark:border-neutral-600">
                            <div class="absolute inset-0 bg-black bg-opacity-0 group-hover:bg-opacity-20 transition-all duration-200 rounded-lg"></div>
                            <button type="button" class="remove-photo-btn absolute -top-1 -right-1 w-4 h-4 bg-red-500 text-white rounded-full inline-flex items-center justify-center opacity-0 group-hover:opacity-100 transition-opacity hover:bg-red-600" 
                                    data-index="${index}" title="Remove Photo">
                                <iconify-icon icon="heroicons:x-mark" class="text-xs"></iconify-icon>
                            </button>
                        </div>
                    `;
                    previewContainer.append(preview);
                };
                reader.readAsDataURL(file);
            }
        });

        updateUploadCounts(uploadFiles.length);

        setTimeout(() => {
            $('.remove-photo-btn').off('click').on('click', function () {
                const index = parseInt($(this).data('index'));
                removePhotoFromUpload(index);
            });
        }, 100);
    }

    function removePhotoFromUpload(index) {
        uploadFiles.splice(index, 1);
        updateUploadPreview();
    }

    function updateUploadCounts(count) {
        $('#uploadCountDisplay').text(count);
        $('#selectedFilesCount').text(count);
        $('#uploadBtnCount').text(count);

        if (count > 0) {
            $('#uploadConfirmBtn').prop('disabled', false);
        } else {
            $('#uploadConfirmBtn').prop('disabled', true);
        }
    }

    function resetUploadModal() {
        $('#uploadInput').val('');
        $('#uploadPreview').empty();
        $('#uploadPreviewSection').addClass('hidden');
        uploadFiles = [];
        currentUploadContainerId = null;
        updateUploadCounts(0);
    }

    // Modal functions
    function showModal(modalId) {
        $('#' + modalId).removeClass('hidden');
    }

    function closeModal(modalId) {
        $('#' + modalId).addClass('hidden');

        if (modalId === 'uploadPhotoModal') {
            resetUploadModal();
        } else if (modalId === 'slideshowModal') {
            currentZoom = 1;
            currentSlideshowIndex = 0;
            slideshowPhotos = [];
            viewMode = 'fit';
            originalImageSize = { width: 0, height: 0 };
            containerSize = { width: 0, height: 0 };

            // Reset image styles
            $('#slideshowImage').css({
                'transform': '',
                'max-width': '',
                'max-height': '',
                'width': '',
                'height': '',
                'transform-origin': ''
            });

            // Reset modal size
            $('#slideshowContainer').css({
                'width': '',
                'height': '',
                'max-width': '90vw',
                'max-height': '90vh'
            });
        } else if (modalId === 'viewAttachmentsModal') {
            currentAttachmentContainerId = null;
            currentAttachments = [];

            // Reset modal state
            $('#attachmentsLoading').addClass('hidden');
            $('#attachmentsTableContainer').addClass('hidden');
            $('#noAttachmentsMessage').addClass('hidden');
            $('#attachmentsError').addClass('hidden');
            $('#attachmentCount').text('0');
            $('#attachmentsTableBody').empty();
        }
    }


    function setupContainerPalletsHandlers() {
        // Updated button click handler
        $(document).on('click', '.link-receive-btn', function () {
            currentContainerPalletsId = $(this).data('id');
            const containerNo = $(this).data('container-no');

            $('#containerPalletsInfo').text(`Container: ${containerNo}`);
            $('#containerPalletsModal').removeClass('hidden');

            // Reset pagination
            currentPalletPage = 1;
            palletPageSize = 10;
            $('#palletPageSize').val(10);
            $('#palletSearchInput').val('');

            loadContainerPallets();
        });

        // Close modal
        $('#closeContainerPalletsModal').on('click', function () {
            $('#containerPalletsModal').addClass('hidden');
            resetContainerPalletsModal();
        });

        // Search input with debounce
        $('#palletSearchInput').on('input', function () {
            clearTimeout(palletSearchTimeout);
            palletSearchTimeout = setTimeout(() => {
                currentPalletPage = 1;
                loadContainerPallets();
            }, 300);
        });

        // Page size change
        $('#palletPageSize').on('change', function () {
            palletPageSize = parseInt($(this).val());
            currentPalletPage = 1;
            loadContainerPallets();
        });

        // Pagination handlers
        $('#palletFirstPage').on('click', () => { goToPalletPage(1); });
        $('#palletPrevPage').on('click', () => { goToPalletPage(currentPalletPage - 1); });
        $('#palletNextPage').on('click', () => { goToPalletPage(currentPalletPage + 1); });
        $('#palletLastPage').on('click', () => { goToPalletPage(palletTotalPages); });

        // Items detail modal
        $('#closePalletItemsModal').on('click', function () {
            $('#palletItemsModal').addClass('hidden');
        });

        // Close modals when clicking outside
        $('#containerPalletsModal, #palletItemsModal').on('click', function (e) {
            if (e.target === this) {
                $(this).addClass('hidden');
            }
        });

        $(document).on('click', '.view-pallet-items-btn', function (e) {
            e.preventDefault();
            e.stopPropagation();

            const palletId = $(this).data('pallet-id');
            const palletCode = $(this).data('pallet-code');

            if (!palletId) {
                showErrorToast('Pallet ID not found');
                return;
            }

            showPalletItems(palletId, palletCode);
        });
    }

    function loadContainerPallets() {
        if (!currentContainerPalletsId) return;

        // Show loading
        $('#containerPalletsTableBody').html(`
        <tr>
            <td colspan="9" class="px-6 py-8 text-center text-gray-500">
                <div class="animate-spin rounded-full h-8 w-8 border-b-2 border-primary-600 mx-auto mb-4"></div>
                <p>Loading pallets...</p>
            </td>
        </tr>
    `);

        const searchTerm = $('#palletSearchInput').val();

        $.ajax({
            url: '/Container/GetContainerPallets',
            type: 'GET',
            data: {
                containerId: currentContainerPalletsId,
                page: currentPalletPage,
                pageSize: palletPageSize,
                searchTerm: searchTerm
            },
            success: function (response) {
                if (response.success) {
                    containerPallets = response.pallets || [];
                    palletTotalCount = response.totalCount || 0;
                    palletTotalPages = response.totalPages || 1;
                    currentPalletPage = response.currentPage || 1;

                    renderContainerPalletsTable();
                    updatePalletPagination();
                } else {
                    showErrorToast(response.error || 'Failed to load pallets');
                }
            },
            error: function () {
                showErrorToast('Failed to load pallets');
                $('#containerPalletsTableBody').html(`
                <tr>
                    <td colspan="9" class="px-6 py-8 text-center text-red-500">
                        <iconify-icon icon="heroicons:exclamation-triangle" class="text-2xl mb-2"></iconify-icon>
                        <div>Failed to load pallets</div>
                    </td>
                </tr>
            `);
            }
        });
    }

    function renderContainerPalletsTable() {
        const tbody = $('#containerPalletsTableBody');
        const hasEditAccess = window.hasEditAccess === true || window.hasEditAccess === 'true';

        if (containerPallets.length === 0) {
            tbody.html(`
            <tr>
                <td colspan="9" class="px-6 py-8 text-center text-gray-500">
                    <iconify-icon icon="heroicons:inbox" class="text-3xl mb-2"></iconify-icon>
                    <div>No pallets found for this container</div>
                </td>
            </tr>
        `);
            return;
        }

        let html = '';
        containerPallets.forEach(pallet => {
            html += `
            <tr class="hover:bg-gray-50 dark:hover:bg-gray-700 transition-colors">
                <td class="px-4 py-3 text-sm font-medium text-gray-900 dark:text-white">
                    ${escapeHtml(pallet.palletCode || '')}
                </td>
                <td class="px-4 py-3 text-sm text-gray-600 dark:text-gray-400">
                    ${pallet.packSize || 0}
                </td>
                <td class="px-4 py-3 text-sm text-gray-600 dark:text-gray-400">
                    ${pallet.quantity || 0}
                </td>
                <td class="px-4 py-3 text-sm text-gray-600 dark:text-gray-400">
                    ${pallet.quantityBalance || 0}
                </td>
                <td class="px-4 py-3">
                    <button class="view-pallet-items-btn inline-flex items-center gap-1 px-3 py-1.5 text-sm font-medium text-blue-600 dark:text-blue-400 bg-blue-50 dark:bg-blue-900/20 rounded-lg hover:bg-blue-100 dark:hover:bg-blue-900/30 transition-colors" 
                            data-pallet-id="${pallet.id}" 
                            data-pallet-code="${escapeHtml(pallet.palletCode || '')}"
                            title="View pallet items">
                        <iconify-icon icon="mdi:package-variant" class="text-base"></iconify-icon>
                        View Items (${pallet.itemCount || 0})
                    </button>
                </td>
                <td class="px-4 py-3 text-sm text-gray-600 dark:text-gray-400">
                    ${escapeHtml(pallet.locationName || '-')}
                </td>
                <td class="px-4 py-3">
                    <div class="flex items-center gap-2">
                         <a href="/RawMaterial/ViewPhotoAttachment?palletId=${pallet.id}" 
                            target="_blank" 
                            rel="noopener noreferrer"
                            class="btn btn-sm btn-primary">
                            <iconify-icon icon="mdi:image-multiple" class="text-base"></iconify-icon>
                            Photos
                         </a>
                    </div>
                </td>
                <td class="px-4 py-3">
                    <div class="flex items-center gap-2">
                        ${hasEditAccess ? `
                            <a href="/RawMaterial/EditPallet?id=${pallet.id}&receiveId=${pallet.receiveId}"
                            target="_blank" 
                            rel="noopener noreferrer"
                               class="inline-flex items-center gap-1 px-3 py-1.5 text-sm font-medium text-green-600 dark:text-green-400 bg-green-50 dark:bg-green-900/20 rounded-lg hover:bg-green-100 dark:hover:bg-green-900/30 transition-colors"
                               title="Edit pallet">
                                <iconify-icon icon="mdi:pencil" class="text-base"></iconify-icon>
                                Edit
                            </a>
                        ` : `
                            <span class="text-sm text-gray-400">No actions</span>
                        `}
                    </div>
                </td>
            </tr>
        `;
        });

        tbody.html(html);
    }

    function showPalletItems(palletId, palletCode) {
        console.log('showPalletItems called:', { palletId, palletCode }); // Debug log

        // Update modal title
        $('#palletItemsTitle').text(`Items for Pallet: ${palletCode || 'Unknown'}`);

        // Show modal
        $('#palletItemsModal').removeClass('hidden');

        // Show loading state
        $('#palletItemsContent').html(`
        <div class="text-center py-12">
            <div class="animate-spin rounded-full h-12 w-12 border-b-2 border-primary-600 mx-auto mb-4"></div>
            <p class="text-gray-600 dark:text-gray-400">Loading pallet items...</p>
        </div>
    `);

        // Fetch pallet items
        $.ajax({
            url: `/Container/GetPalletItems`,
            type: 'GET',
            data: { palletId: palletId },
            timeout: 30000, // 30 second timeout
            success: function (response) {
                console.log('Pallet items response:', response); // Debug log

                if (response.success) {
                    renderPalletItems(response.items || []);
                } else {
                    $('#palletItemsContent').html(`
                    <div class="text-center py-12 text-red-500">
                        <iconify-icon icon="heroicons:exclamation-triangle" class="text-4xl mb-4"></iconify-icon>
                        <div class="text-lg font-medium mb-2">Failed to load items</div>
                        <div class="text-sm">${response.error || 'Unknown error occurred'}</div>
                    </div>
                `);
                }
            },
            error: function (xhr, status, error) {
                console.error('Error loading pallet items:', { xhr, status, error }); // Debug log

                let errorMessage = 'Error loading items';
                if (status === 'timeout') {
                    errorMessage = 'Request timed out. Please try again.';
                } else if (xhr.status === 404) {
                    errorMessage = 'Pallet not found';
                } else if (xhr.status === 500) {
                    errorMessage = 'Server error occurred';
                }

                $('#palletItemsContent').html(`
                <div class="text-center py-12 text-red-500">
                    <iconify-icon icon="heroicons:exclamation-triangle" class="text-4xl mb-4"></iconify-icon>
                    <div class="text-lg font-medium mb-2">${errorMessage}</div>
                    <div class="text-sm">Status: ${xhr.status} ${status}</div>
                    <button onclick="showPalletItems('${palletId}', '${palletCode}')" 
                            class="mt-4 btn btn-sm btn-primary">
                        Try Again
                    </button>
                </div>
            `);
            }
        });
    }
    function generateGroupCheckboxes(pallet) {
        const groups = [
            { key: 'group3', label: '3', color: 'text-blue-600' },
            { key: 'group6', label: '6', color: 'text-green-600' },
            { key: 'group8', label: '8', color: 'text-yellow-600' },
            { key: 'group9', label: '9', color: 'text-red-600' },
            { key: 'ndg', label: 'NDG', color: 'text-purple-600' },
            { key: 'scentaurus', label: 'SCENTAURUS', color: 'text-indigo-600' }
        ];

        return groups.map(group => {
            const isChecked = pallet[group.key] === true;
            return `
            <label class="flex items-center text-xs gap-1 ${isChecked ? group.color : 'text-gray-400'} cursor-default" 
                   title="${group.label} ${isChecked ? 'enabled' : 'disabled'}">
                <input type="checkbox" ${isChecked ? 'checked' : ''} disabled 
                       class="form-checkbox h-3 w-3 ${isChecked ? group.color : 'text-gray-400'}">
                <span class="select-none">${group.label}</span>
            </label>
        `;
        }).join('');
    }

    function renderPalletItems(items) {
        console.log('renderPalletItems called with:', items); // Debug log

        if (!items || items.length === 0) {
            $('#palletItemsContent').html(`
            <div class="text-center py-12 text-gray-500">
                <iconify-icon icon="heroicons:cube" class="text-4xl mb-4"></iconify-icon>
                <div class="text-lg font-medium mb-2">No Items Found</div>
                <div class="text-sm">This pallet contains no items</div>
            </div>
        `);
            return;
        }

        let html = `
        <div class="mb-4 p-4 bg-blue-50 dark:bg-blue-900/20 rounded-lg">
            <div class="flex items-center gap-2 text-blue-800 dark:text-blue-200">
                <iconify-icon icon="mdi:information" class="text-lg"></iconify-icon>
                <span class="font-medium">Found ${items.length} item${items.length > 1 ? 's' : ''} in this pallet</span>
            </div>
        </div>
        
        <div class="overflow-auto">
            <table class="min-w-full divide-y divide-gray-200 dark:divide-gray-600">
                <thead class="bg-gray-50 dark:bg-gray-700">
                    <tr>
                        <th class="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase">
                            HU Code
                        </th>
                        <th class="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase">
                            DG
                        </th>
                        <th class="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase">
                            Remarks
                        </th>
                        <th class="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase">
                            Production Date
                        </th>
                    </tr>
                </thead>
                <tbody class="bg-white dark:bg-gray-800 divide-y divide-gray-200 dark:divide-gray-600">
    `;

        items.forEach((item, index) => {
            const prodDate = item.prodDate ? new Date(item.prodDate).toLocaleDateString() : '-';
            const dgIcon = item.dg ?
                '<iconify-icon icon="mdi:check-circle" class="text-green-500 text-lg" title="DG Item"></iconify-icon>' :
                '<iconify-icon icon="mdi:minus-circle" class="text-gray-400 text-lg" title="Non-DG Item"></iconify-icon>';

            html += `
            <tr class="hover:bg-gray-50 dark:hover:bg-gray-700 transition-colors">
                <td class="px-4 py-3 text-sm font-medium text-gray-900 dark:text-white">
                    ${escapeHtml(item.itemCode || '-')}
                </td>
                <td class="px-4 py-3 text-sm text-center">
                    ${dgIcon}
                </td>
                <td class="px-4 py-3 text-sm text-gray-600 dark:text-gray-400">
                    ${escapeHtml(item.remarks || '-')}
                </td>
                <td class="px-4 py-3 text-sm text-gray-600 dark:text-gray-400">
                    ${prodDate}
                </td>
            </tr>
        `;
        });

        html += `
                </tbody>
            </table>
        </div>
    `;

        $('#palletItemsContent').html(html);
    }

    function updatePalletPagination() {
        $('#palletShowingFrom').text(palletTotalCount === 0 ? 0 : ((currentPalletPage - 1) * palletPageSize) + 1);
        $('#palletShowingTo').text(Math.min(currentPalletPage * palletPageSize, palletTotalCount));
        $('#palletTotalCount').text(palletTotalCount);
        $('#palletCurrentPage').text(currentPalletPage);
        $('#palletTotalPages').text(palletTotalPages);

        // Update button states
        $('#palletFirstPage, #palletPrevPage').prop('disabled', currentPalletPage === 1);
        $('#palletNextPage, #palletLastPage').prop('disabled', currentPalletPage === palletTotalPages);
    }

    function goToPalletPage(page) {
        if (page >= 1 && page <= palletTotalPages && page !== currentPalletPage) {
            currentPalletPage = page;
            loadContainerPallets();
        }
    }

    function resetContainerPalletsModal() {
        currentContainerPalletsId = null;
        containerPallets = [];
        currentPalletPage = 1;
        palletPageSize = 10;
        $('#palletSearchInput').val('');
    }
    // Show attachments modal and load data
    function showAttachmentsModal(containerId, jobId) {
        currentAttachments = [];
        showModal('viewAttachmentsModal');

        // Show loading state
        showAttachmentsLoading();

        // Load attachments data
        loadContainerAttachments(containerId, jobId);
    }

    function showAttachmentsLoading() {
        $('#attachmentsLoading').removeClass('hidden');
        $('#attachmentsTableContainer').addClass('hidden');
        $('#noAttachmentsMessage').addClass('hidden');
        $('#attachmentsError').addClass('hidden');
        $('#attachmentCount').text('0');
    }

    function showAttachmentsError() {
        $('#attachmentsLoading').addClass('hidden');
        $('#attachmentsTableContainer').addClass('hidden');
        $('#noAttachmentsMessage').addClass('hidden');
        $('#attachmentsError').removeClass('hidden');
        $('#attachmentCount').text('0');
    }

    function showNoAttachments() {
        $('#attachmentsLoading').addClass('hidden');
        $('#attachmentsTableContainer').addClass('hidden');
        $('#attachmentsError').addClass('hidden');
        $('#noAttachmentsMessage').removeClass('hidden');
        $('#attachmentCount').text('0');
    }

    function showAttachmentsTable() {
        $('#attachmentsLoading').addClass('hidden');
        $('#attachmentsError').addClass('hidden');
        $('#noAttachmentsMessage').addClass('hidden');
        $('#attachmentsTableContainer').removeClass('hidden');
    }

    function loadContainerAttachments(containerId, jobId) {
        $.ajax({
            url: `/Container/GetContainerAttachments`,
            data: { containerId: containerId },
            type: 'GET',
            timeout: 30000, // 30 second timeout
            success: function (response) {
                if (response.success) {
                    currentAttachments = response.attachments || [];

                    if (currentAttachments.length === 0) {
                        showNoAttachments();
                    } else {
                        renderAttachmentsTable();
                        showAttachmentsTable();
                        $('#attachmentCount').text(currentAttachments.length);
                    }
                } else {
                    console.error('Failed to load attachments:', response.error);
                    showAttachmentsError();
                    showErrorToast(response.error || 'Failed to load attachments');
                }
            },
            error: function (xhr, status, error) {
                console.error('Error loading container attachments:', { xhr, status, error });
                showAttachmentsError();

                let errorMessage = 'Failed to load attachments';
                if (status === 'timeout') {
                    errorMessage = 'Request timed out. Please try again.';
                } else if (xhr.status === 404) {
                    errorMessage = 'Container attachments not found';
                } else if (xhr.status === 500) {
                    errorMessage = 'Server error occurred';
                }

                showErrorToast(errorMessage);
            }
        });
    }

    function renderAttachmentsTable() {
        const tbody = $('#attachmentsTableBody');
        tbody.empty();

        if (currentAttachments.length === 0) {
            tbody.html(`
            <tr>
                <td colspan="4" class="px-6 py-8 text-center text-gray-500">
                    <iconify-icon icon="heroicons:document" class="text-3xl mb-2"></iconify-icon>
                    <div>No attachments available</div>
                </td>
            </tr>
        `);
            return;
        }

        currentAttachments.forEach((attachment, index) => {
            const fileName = escapeHtml(attachment.fileName || 'Unknown File');
            const reference = escapeHtml(attachment.attachmentReference || '-');
            const attachmentType = escapeHtml(attachment.attachmentType || 'Unknown');
            const filePath = attachment.filePath || '';

            // Determine if file is PDF for preview option
            const isPdf = fileName.toLowerCase().endsWith('.pdf') ||
                attachmentType.toLowerCase().includes('pdf');

            // Generate action buttons
            let actionButtons = '';

            if (isPdf && filePath) {
                actionButtons += `
                <button class="preview-attachment-btn inline-flex items-center gap-1 px-3 py-1.5 text-sm font-medium text-blue-600 dark:text-blue-400 bg-blue-50 dark:bg-blue-900/20 rounded-lg hover:bg-blue-100 dark:hover:bg-blue-900/30 mr-2" 
                        data-file-path="${escapeHtml(filePath)}" 
                        data-file-signed-url="${escapeHtml(attachment.downloadSignedUrl)}" 
                        data-file-name="${fileName}"
                        title="Preview PDF">
                    <iconify-icon icon="mdi:eye" class="text-base"></iconify-icon>
                    Preview
                </button>`;
            }

            if (filePath) {
                actionButtons += `
                <button class="download-attachment-btn inline-flex items-center gap-1 px-3 py-1.5 text-sm font-medium text-green-600 dark:text-green-400 bg-green-50 dark:bg-green-900/20 rounded-lg hover:bg-green-100 dark:hover:bg-green-900/30" 
                        data-file-path="${escapeHtml(filePath)}" 
                        data-file-signed-url="${escapeHtml(attachment.downloadSignedUrl)}" 
                        data-file-name="${fileName}"
                        title="Download File">
                    <iconify-icon icon="mdi:download" class="text-base"></iconify-icon>
                    Download
                </button>`;
            }

            if (!actionButtons) {
                actionButtons = '<span class="text-sm text-gray-400">No actions available</span>';
            }

            const row = `
            <tr class="hover:bg-gray-50 dark:hover:bg-gray-700 transition-colors">
                <td class="px-6 py-4 text-sm font-medium text-gray-900 dark:text-white">
                    <div class="flex items-center gap-2">
                        <iconify-icon icon="mdi:file-${isPdf ? 'pdf' : 'document'}-outline" class="text-lg ${isPdf ? 'text-red-500' : 'text-gray-500'}"></iconify-icon>
                        <span class="truncate max-w-xs" title="${fileName}">${fileName}</span>
                    </div>
                </td>
                <td class="px-6 py-4 text-sm text-gray-600 dark:text-gray-400">
                    <span class="truncate max-w-xs" title="${reference}">${reference}</span>
                </td>
                <td class="px-6 py-4 text-sm text-gray-600 dark:text-gray-400">
                    <span class="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium bg-gray-100 dark:bg-gray-700 text-gray-800 dark:text-gray-200">
                        ${attachmentType}
                    </span>
                </td>
                <td class="px-6 py-4 text-sm">
                    <div class="flex items-center gap-2">
                        ${actionButtons}
                    </div>
                </td>
            </tr>
        `;

            tbody.append(row);
        });

        // Setup action handlers
        setupAttachmentActionHandlers();
    }

    function setupAttachmentActionHandlers() {
        // Preview attachment handlers
        $('.preview-attachment-btn').off('click').on('click', function (e) {
            e.preventDefault();
            e.stopPropagation();

            const filePath = $(this).data('file-path');
            const fileName = $(this).data('file-name');

            if (filePath) {
                // Open PDF in new tab
                window.open(filePath, '_blank', 'noopener,noreferrer');
            } else {
                showErrorToast('File path not available for preview');
            }
        });

        // Download attachment handlers
        $('.download-attachment-btn').off('click').on('click', function (e) {
            e.preventDefault();
            e.stopPropagation();

            const fileUrl = $(this).data('file-signed-url');
            const fileName = $(this).data('file-name');
            const $button = $(this);

            if (fileUrl) {
                downloadFile(fileUrl, fileName, $button);
            } else {
                showErrorToast('File path not available for download');
            }
        });
    }
    async function downloadFile(url, fileName, $button) {
        // Show loading state on button
        const originalHtml = $button.html();
        $button.prop('disabled', true).html(`
        <iconify-icon icon="heroicons:arrow-path" class="animate-spin text-base"></iconify-icon>
        Downloading...
    `);

        try {
            const link = document.createElement('a');

            link.href = url;

            link.download = fileName;
            link.target = '_blank';
            document.body.appendChild(link);
            link.click();
            document.body.removeChild(link);

            showSuccessToast(`"${fileName}" download started`);

        } catch (error) {
            console.error('Download failed:', error);

            // Fallback: try opening in new tab with download attribute
            try {
                const link = document.createElement('a');
                link.href = url;
                link.download = fileName || 'attachment';
                link.target = '_blank';
                link.rel = 'noopener noreferrer';
                document.body.appendChild(link);
                link.click();
                document.body.removeChild(link);

                showSuccessToast('Download initiated (fallback method)');
            } catch (fallbackError) {
                console.error('Fallback download failed:', fallbackError);
                showErrorToast('Failed to download file. Please try opening the link directly.');
            }
        } finally {
            // Restore button state
            $button.prop('disabled', false).html(originalHtml);
        }
    }
    // Expose public methods
    window.ContainerTableModule = {
        init: init,
        reloadTable: function () {
            if (ContainerTable) {
                ContainerTable.ajax.reload();
            }
        }
    };
})();

// Initialize the module
ContainerTableModule.init();