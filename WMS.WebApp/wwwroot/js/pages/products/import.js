// Product Import Module with jQuery AJAX
(function () {
    let currentStep = 1;
    let selectedFile = null;
    let validationResults = null;

    // Initialize the module
    function init() {
        document.addEventListener('DOMContentLoaded', function () {
            setupEventHandlers();
        });
    }

    // Set up event handlers
    function setupEventHandlers() {
        // File input change handler
        $('#fileInput').on('change', function (e) {
            handleFileSelection(e.target.files[0]);
        });

        // Drag and drop handlers
        const dropZone = $('.border-dashed');
        if (dropZone.length) {
            dropZone.on('dragover', handleDragOver);
            dropZone.on('dragleave', handleDragLeave);
            dropZone.on('drop', handleFileDrop);
        }
    }

    // Handle file selection
    function handleFileSelection(file) {
        if (!file) {
            clearFile();
            return;
        }

        // Validate file type
        if (!file.name.toLowerCase().endsWith('.xlsx')) {
            showErrorToast('Please select an Excel file (.xlsx)');
            return;
        }

        // Validate file size (max 10MB)
        if (file.size > 10 * 1024 * 1024) {
            showErrorToast('File size must be less than 10MB');
            return;
        }

        selectedFile = file;
        displayFileInfo(file);
    }

    // Display file information
    function displayFileInfo(file) {
        $('#file-name').text(file.name);
        $('#file-size').text(formatFileSize(file.size));
        $('#file-info').removeClass('hidden');
    }

    // Clear selected file
    function clearFile() {
        selectedFile = null;
        $('#fileInput').val('');
        $('#file-info').addClass('hidden');
    }

    // Validate file using jQuery AJAX
    function validateFile() {
        if (!selectedFile) {
            showErrorToast('Please select a file first');
            return;
        }

        const formData = new FormData();
        formData.append('file', selectedFile);

        // Show loading
        showOnProgressLoading('Validating file...');

        $.ajax({
            url: '/Product/ValidateImport',
            type: 'POST',
            data: formData,
            processData: false,
            contentType: false,
            headers: {
                'X-Requested-With': 'XMLHttpRequest',
                'X-CSRF-TOKEN': $('meta[name="csrf-token"]').attr('content')
            },
            success: function (data) {
                Swal.close();
                validationResults = data;
                showValidationResults(data);
                goToStep(2);
            },
            error: function (xhr, status, error) {
                Swal.close();
                console.error('Validation error:', error);

                let errorMessage = 'An error occurred during validation';
                if (xhr.responseJSON && xhr.responseJSON.message) {
                    errorMessage = xhr.responseJSON.message;
                } else if (xhr.status === 400) {
                    errorMessage = 'Invalid file format or content';
                } else if (xhr.status === 413) {
                    errorMessage = 'File size too large';
                } else if (xhr.status === 500) {
                    errorMessage = 'Server error occurred during validation';
                }

                showErrorToast(errorMessage);
            }
        });
    }

    // Show validation results
    function showValidationResults(data) {
        const resultsContainer = $('#validation-results');

        let html = `
            <div class="mb-6">
                <div class="grid grid-cols-1 md:grid-cols-3 gap-4">
                    <div class="bg-blue-50 dark:bg-blue-900/20 border border-blue-200 dark:border-blue-800 rounded-lg p-4">
                        <div class="flex items-center">
                            <iconify-icon icon="solar:document-outline" class="text-blue-600 text-2xl mr-3"></iconify-icon>
                            <div>
                                <div class="text-2xl font-bold text-blue-900 dark:text-blue-100">${data.totalRows}</div>
                                <div class="text-sm text-blue-600 dark:text-blue-300">Total Rows</div>
                            </div>
                        </div>
                    </div>
                    
                    <div class="bg-green-50 dark:bg-green-900/20 border border-green-200 dark:border-green-800 rounded-lg p-4">
                        <div class="flex items-center">
                            <iconify-icon icon="solar:check-circle-outline" class="text-green-600 text-2xl mr-3"></iconify-icon>
                            <div>
                                <div class="text-2xl font-bold text-green-900 dark:text-green-100">${data.validItems}</div>
                                <div class="text-sm text-green-600 dark:text-green-300">Valid Items</div>
                            </div>
                        </div>
                    </div>
                    
                    <div class="bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800 rounded-lg p-4">
                        <div class="flex items-center">
                            <iconify-icon icon="solar:close-circle-outline" class="text-red-600 text-2xl mr-3"></iconify-icon>
                            <div>
                                <div class="text-2xl font-bold text-red-900 dark:text-red-100">${data.errors.length}</div>
                                <div class="text-sm text-red-600 dark:text-red-300">Errors</div>
                            </div>
                        </div>
                    </div>
                </div>
            </div>
        `;

        // Show errors if any
        if (data.errors && data.errors.length > 0) {
            html += `
                <div class="mb-6">
                    <h6 class="text-red-600 font-semibold mb-3 flex items-center gap-2">
                        <iconify-icon icon="solar:danger-outline"></iconify-icon>
                        Validation Errors
                    </h6>
                    <div class="bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800 rounded-lg p-4 max-h-60 overflow-y-auto">
                        <ul class="list-disc list-inside space-y-1 text-sm text-red-800 dark:text-red-200">
            `;

            data.errors.forEach(error => {
                html += `<li>${escapeHtml(error)}</li>`;
            });

            html += `
                        </ul>
                    </div>
                </div>
            `;
        }

        // Show warnings if any
        if (data.warnings && data.warnings.length > 0) {
            html += `
                <div class="mb-6">
                    <h6 class="text-yellow-600 font-semibold mb-3 flex items-center gap-2">
                        <iconify-icon icon="solar:warning-outline"></iconify-icon>
                        Warnings
                    </h6>
                    <div class="bg-yellow-50 dark:bg-yellow-900/20 border border-yellow-200 dark:border-yellow-800 rounded-lg p-4 max-h-60 overflow-y-auto">
                        <ul class="list-disc list-inside space-y-1 text-sm text-yellow-800 dark:text-yellow-200">
            `;

            data.warnings.forEach(warning => {
                html += `<li>${escapeHtml(warning)}</li>`;
            });

            html += `
                        </ul>
                    </div>
                </div>
            `;
        }

        // Action buttons
        html += `
            <div class="flex gap-3 justify-end">
                <button type="button" onclick="goToStep(1)" class="btn bg-gray-500 hover:bg-gray-600 text-white px-6 py-2">
                    Back
                </button>
        `;

        if (data.validItems > 0 && data.errors.length === 0) {
            html += `
                <button type="button" onclick="processImport()" class="btn btn-primary text-sm btn-sm px-3 py-3 rounded-lg flex items-center gap-2">
                    <iconify-icon icon="solar:upload-outline"></iconify-icon>
                    Import ${data.validItems} Products
                </button>
            `;
        }

        html += `</div>`;

        resultsContainer.html(html);
    }

    // Process import using jQuery AJAX
    function processImport() {
        if (!selectedFile) {
            showErrorToast('No file selected');
            return;
        }

        const formData = new FormData();
        formData.append('file', selectedFile);

        // Show loading
        showOnProgressLoading('Importing products...');

        $.ajax({
            url: '/Product/ProcessImport',
            type: 'POST',
            data: formData,
            processData: false,
            contentType: false,
            headers: {
                'X-Requested-With': 'XMLHttpRequest',
                'X-CSRF-TOKEN': $('meta[name="csrf-token"]').attr('content')
            },
            success: function (data) {
                Swal.close();
                showImportResults(data);
                goToStep(3);
            },
            error: function (xhr, status, error) {
                Swal.close();
                console.error('Import error:', error);

                let errorMessage = 'An error occurred during import';
                if (xhr.responseJSON && xhr.responseJSON.message) {
                    errorMessage = xhr.responseJSON.message;
                } else if (xhr.status === 400) {
                    errorMessage = 'Invalid file format or data';
                } else if (xhr.status === 413) {
                    errorMessage = 'File size too large';
                } else if (xhr.status === 500) {
                    errorMessage = 'Server error occurred during import';
                }

                showErrorToast(errorMessage);
            }
        });
    }

    // Show import results
    function showImportResults(data) {
        const resultsContainer = $('#import-results');

        let html = `
            <div class="mb-6">
                <div class="text-center mb-6">
                    <div class="inline-flex items-center justify-center w-16 h-16 rounded-full ${data.success ? 'bg-green-100 text-green-600' : 'bg-yellow-100 text-yellow-600'} mb-4">
                        <iconify-icon icon="${data.success ? 'solar:check-circle-bold' : 'solar:warning-outline'}" class="text-3xl"></iconify-icon>
                    </div>
                    <h5 class="font-semibold mb-2">${data.success ? 'Import Completed Successfully!' : 'Import Completed with Issues'}</h5>
                    <p class="text-gray-600 dark:text-gray-400">
                        ${data.success ?
                `All ${data.successCount} products have been imported successfully.` :
                `${data.successCount} products imported, ${data.errorCount} failed.`
            }
                    </p>
                </div>

                <div class="grid grid-cols-1 md:grid-cols-4 gap-4 mb-6">
                    <div class="bg-blue-50 dark:bg-blue-900/20 border border-blue-200 dark:border-blue-800 rounded-lg p-4 text-center">
                        <div class="text-2xl font-bold text-blue-900 dark:text-blue-100">${data.totalRows || 0}</div>
                        <div class="text-sm text-blue-600 dark:text-blue-300">Total Rows</div>
                    </div>
                    
                    <div class="bg-blue-50 dark:bg-blue-900/20 border border-blue-200 dark:border-blue-800 rounded-lg p-4 text-center">
                        <div class="text-2xl font-bold text-blue-900 dark:text-blue-100">${data.processedRows || 0}</div>
                        <div class="text-sm text-green-600 dark:text-green-300">Processed</div>
                    </div>
                    
                    <div class="bg-green-50 dark:bg-green-900/20 border border-green-200 dark:border-green-800 rounded-lg p-4 text-center">
                        <div class="text-2xl font-bold text-green-900 dark:text-green-100">${data.successCount || 0}</div>
                        <div class="text-sm text-green-600 dark:text-green-300">Success</div>
                    </div>
                    
                    <div class="bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800 rounded-lg p-4 text-center">
                        <div class="text-2xl font-bold text-red-900 dark:text-red-100">${data.errorCount || 0}</div>
                        <div class="text-sm text-red-600 dark:text-red-300">Errors</div>
                    </div>
                </div>
            </div>
        `;

        // Show detailed results if available
        if (data.results && data.results.length > 0) {
            html += `
                <div class="mb-6">
                    <h6 class="font-semibold mb-3">Import Details</h6>
                    <div class="bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-lg overflow-hidden">
                        <div class="max-h-80 overflow-y-auto">
                            <table class="min-w-full divide-y divide-gray-200 dark:divide-gray-600">
                                <thead class="bg-gray-50 dark:bg-neutral-800 sticky top-0 z-10">
                                    <tr>
                                        <th class="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">Row</th>
                                        <th class="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">Product</th>
                                        <th class="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">SKU</th>
                                        <th class="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">Status</th>
                                        <th class="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">Message</th>
                                    </tr>
                                </thead>
                                <tbody class="bg-white dark:bg-gray-800 divide-y divide-gray-200 dark:divide-gray-700">
            `;

            data.results.forEach(result => {
                const statusClass = result.status === 'Success' ? 'text-green-600 bg-green-100 dark:bg-green-900/20 dark:text-green-400' : 'text-red-600 bg-red-100 dark:bg-red-900/20 dark:text-red-400';
                html += `
                    <tr>
                        <td class="px-4 py-3 text-sm text-gray-600 dark:text-gray-400">${result.rowNumber}</td>
                        <td class="px-4 py-3 text-sm text-gray-600 dark:text-gray-400">${escapeHtml(result.productName || '')}</td>
                        <td class="px-4 py-3 text-sm text-gray-600 dark:text-gray-400 font-mono">${escapeHtml(result.productSKU || '')}</td>
                        <td class="px-4 py-3">
                            <span class="inline-flex px-2 py-1 text-xs font-semibold rounded-full ${statusClass}">
                                ${result.status}
                            </span>
                        </td>
                        <td class="px-4 py-3 text-sm text-gray-600 dark:text-gray-400">${escapeHtml(result.message || '')}</td>
                    </tr>
                `;
            });

            html += `
                                </tbody>
                            </table>
                        </div>
                    </div>
                </div>
            `;
        }

        // Show errors if any
        if (data.errors && data.errors.length > 0) {
            html += `
                <div class="mb-6">
                    <h6 class="text-red-600 font-semibold mb-3 flex items-center gap-2">
                        <iconify-icon icon="solar:danger-outline"></iconify-icon>
                        Import Errors
                    </h6>
                    <div class="bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800 rounded-lg p-4 max-h-60 overflow-y-auto">
                        <ul class="list-disc list-inside space-y-1 text-sm text-red-800 dark:text-red-200">
            `;

            data.errors.forEach(error => {
                html += `<li>${escapeHtml(error)}</li>`;
            });

            html += `
                        </ul>
                    </div>
                </div>
            `;
        }

        // Action buttons
        html += `
            <div class="flex gap-3 justify-center">
                <button type="button" onclick="startOver()" class="btn bg-gray-500 hover:bg-gray-600 text-white px-6 py-2">
                    Import Another File
                </button>
                <a href="/Product" class="btn btn-primary px-6 py-2">
                    Go to Products
                </a>
            </div>
        `;

        resultsContainer.html(html);
    }

    // Navigate to specific step
    function goToStep(step) {
        // Hide all steps
        $('.import-step').removeClass('active').addClass('hidden');

        // Show target step
        const targetStep = $(`#step-${getStepName(step)}`);
        if (targetStep.length) {
            targetStep.removeClass('hidden').addClass('active');
        }

        // Update step indicators
        updateStepIndicators(step);
        currentStep = step;
    }

    // Get step name by number
    function getStepName(step) {
        const stepNames = { 1: 'upload', 2: 'validation', 3: 'results' };
        return stepNames[step] || 'upload';
    }

    // Update step indicators
    function updateStepIndicators(activeStep) {
        $('.step-item').each(function (index) {
            const stepNumber = index + 1;
            $(this).removeClass('active completed');

            if (stepNumber < activeStep) {
                $(this).addClass('completed');
            } else if (stepNumber === activeStep) {
                $(this).addClass('active');
            }
        });
    }

    // Start over
    function startOver() {
        clearFile();
        validationResults = null;
        goToStep(1);
    }

    // Drag and drop handlers
    function handleDragOver(e) {
        e.preventDefault();
        $(e.currentTarget).addClass('border-blue-400 bg-blue-50 dark:bg-blue-900/20');
    }

    function handleDragLeave(e) {
        e.preventDefault();
        $(e.currentTarget).removeClass('border-blue-400 bg-blue-50 dark:bg-blue-900/20');
    }

    function handleFileDrop(e) {
        e.preventDefault();
        $(e.currentTarget).removeClass('border-blue-400 bg-blue-50 dark:bg-blue-900/20');

        const files = e.originalEvent.dataTransfer.files;
        if (files.length > 0) {
            handleFileSelection(files[0]);
        }
    }

    //// Utility functions
    //function formatFileSize(bytes) {
    //    if (bytes === 0) return '0 Bytes';
    //    const k = 1024;
    //    const sizes = ['Bytes', 'KB', 'MB', 'GB'];
    //    const i = Math.floor(Math.log(bytes) / Math.log(k));
    //    return parseFloat((bytes / Math.pow(k, i)).toFixed(2)) + ' ' + sizes[i];
    //}

    //function escapeHtml(text) {
    //    if (!text) return '';
    //    const map = {
    //        '&': '&amp;',
    //        '<': '&lt;',
    //        '>': '&gt;',
    //        '"': '&quot;',
    //        "'": '&#039;'
    //    };
    //    return text.toString().replace(/[&<>"']/g, function (m) { return map[m]; });
    //}

    // Expose public methods
    window.ProductImportModule = {
        init: init,
        validateFile: validateFile,
        processImport: processImport,
        goToStep: goToStep,
        clearFile: clearFile,
        startOver: startOver
    };
})();

// Initialize the module
ProductImportModule.init();

// Global functions for HTML onclick handlers
function validateFile() {
    ProductImportModule.validateFile();
}

function processImport() {
    ProductImportModule.processImport();
}

function goToStep(step) {
    ProductImportModule.goToStep(step);
}

function clearFile() {
    ProductImportModule.clearFile();
}

function startOver() {
    ProductImportModule.startOver();
}