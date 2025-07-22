// SignalR Real-time Integration for Location Grid - Zone-Based Updates
(function () {
    // SignalR connection and state
    let warehouseConnection = null;
    let isConnected = false;
    let reconnectionAttempts = 0;
    const maxReconnectionAttempts = 5;
    const reconnectionDelay = 5000; // 5 seconds

    // Zone management
    let currentZoneId = null;
    let currentZoneName = null;
    let isJoiningZone = false;

    // Animation and UI state
    const animationQueue = [];
    let isProcessingAnimations = false;

    // Initialize SignalR connection
    function initializeSignalR() {
        console.log('Initializing SignalR connection...');

        // Create connection
        warehouseConnection = new signalR.HubConnectionBuilder()
            .withUrl('/warehouseHub')
            .withAutomaticReconnect({
                nextRetryDelayInMilliseconds: () => {
                    return Math.min(1000 * Math.pow(2, reconnectionAttempts), 30000); // Exponential backoff, max 30s
                }
            })
            .configureLogging(signalR.LogLevel.Information)
            .build();

        setupSignalREventHandlers();
        startConnection();
    }

    // Setup SignalR event handlers
    function setupSignalREventHandlers() {
        // Connection events
        warehouseConnection.onclose(onConnectionClosed);
        warehouseConnection.onreconnecting(onReconnecting);
        warehouseConnection.onreconnected(onReconnected);

        // Hub method handlers
        warehouseConnection.on('Connected', onConnected);
        warehouseConnection.on('JoinedZoneGroup', onJoinedZoneGroup);
        warehouseConnection.on('LeftZoneGroup', onLeftZoneGroup);
        warehouseConnection.on('SwitchedZoneGroup', onSwitchedZoneGroup);
        warehouseConnection.on('LocationUpdated', onLocationUpdated);
        warehouseConnection.on('LocationBatchUpdated', onLocationBatchUpdated);
        warehouseConnection.on('InventoryMoved', onInventoryMoved);
        warehouseConnection.on('WarehouseNotification', onWarehouseNotification);
        warehouseConnection.on('Error', onSignalRError);
        warehouseConnection.on('Pong', onPong);
    }

    // Start SignalR connection
    async function startConnection() {
        try {
            await warehouseConnection.start();
            console.log('SignalR connected successfully');
            isConnected = true;
            reconnectionAttempts = 0;

            // Update connection status in UI
            updateConnectionStatus(true);

            // Start heartbeat
            startHeartbeat();

            // Auto-join current zone if one is selected
            autoJoinCurrentZone();

        } catch (error) {
            console.error('SignalR connection failed:', error);
            isConnected = false;
            updateConnectionStatus(false);

            // Retry connection
            setTimeout(startConnection, reconnectionDelay);
            reconnectionAttempts++;
        }
    }

    // Connection event handlers
    function onConnected(connectionInfo) {
        //console.log('SignalR hub connected:', connectionInfo);
        console.log('SignalR hub connected.');
        showNotification('Connected to real-time updates', 'success');

        // Join current zone after connection
        autoJoinCurrentZone();
    }

    function onConnectionClosed(error) {
        isConnected = false;
        currentZoneId = null; // Reset zone when disconnected
        currentZoneName = null; // Reset zone when disconnected
        updateConnectionStatus(false);
        showNotification('Real-time updates disconnected', 'warning');
    }

    function onReconnecting(error) {
        updateConnectionStatus(false, 'Reconnecting...');
        showNotification('Reconnecting to real-time updates...', 'info');
    }

    function onReconnected(connectionId) {
        isConnected = true;
        updateConnectionStatus(true);
        showNotification('Real-time updates reconnected', 'success');

        // Rejoin current zone after reconnection
        autoJoinCurrentZone();
    }

    // Zone management event handlers
    function onJoinedZoneGroup(data) {
        currentZoneId = data.zoneId;
        currentZoneName = data.zoneName;
        isJoiningZone = false;
        updateConnectionStatus(true, `Receiving updates for zone ${currentZoneName}`);
        showNotification(`Now receiving real-time updates for current zone`, 'success', 3000);
    }

    function onLeftZoneGroup(data) {
        if (currentZoneId === data.zoneId) {
            currentZoneId = null;
            currentZoneName = null;
        }
        updateConnectionStatus(true, 'Connected - no zone selected');
    }

    function onSwitchedZoneGroup(data) {
        currentZoneId = data.toZoneId;
        currentZoneName = data.zoneName;
        isJoiningZone = false;
        updateConnectionStatus(true, `Receiving updates for zone ${currentZoneName}`);
        showNotification(`Switched to real-time updates for new zone`, 'success', 3000);
    }

    function onSignalRError(data) {
        isJoiningZone = false;
        showNotification(`SignalR error: ${data.message}`, 'error');
    }

    // Location update handlers
    function onLocationUpdated(locationUpdate) {

        // Add to animation queue
        queueLocationAnimation(locationUpdate);

        // Update location in grid immediately
        updateLocationInGrid(locationUpdate);

        // Show notification for significant changes
        if (shouldShowNotification(locationUpdate)) {
            showLocationChangeNotification(locationUpdate);
        }
    }

    function onLocationBatchUpdated(batchUpdate) {

        // Process each update in the batch
        batchUpdate.updates.forEach(update => {
            updateLocationInGrid(update);
            queueLocationAnimation(update);
        });

        // Show batch notification
        showNotification(`${batchUpdate.totalUpdates} locations updated`, 'info');
    }

    // Update location in the grid
    function updateLocationInGrid(locationUpdate) {
        try {
            const locationCell = document.querySelector(`[data-location-id="${locationUpdate.locationId}"]`);

            if (!locationCell) {
                console.warn('Location cell not found:', locationUpdate.locationId);
                return;
            }
            const oldStatusClass = getStatusClassSignalR(locationCell.getAttribute('data-status'));

            // Update cell data attributes
            locationCell.setAttribute('data-status', locationUpdate.status);
            locationCell.setAttribute('data-inventory-count', locationUpdate.inventoryCount);
            locationCell.setAttribute('data-total-quantity', locationUpdate.totalQuantity);

            // Update status class
            const newStatusClass = getStatusClassSignalR(locationUpdate.status);

            if (oldStatusClass !== newStatusClass) {
                locationCell.classList.remove(oldStatusClass);
                locationCell.classList.add(newStatusClass);
            }

            // Update inventory indicator
            const inventoryIndicator = locationCell.querySelector('.inventory-indicator');
            if (inventoryIndicator) {
                if (locationUpdate.inventoryCount > 0) {
                    inventoryIndicator.textContent = locationUpdate.inventoryCount;
                    inventoryIndicator.style.display = 'flex';
                } else {
                    inventoryIndicator.style.display = 'none';
                }
            }

            // Update tooltip data if location is being hovered
            if (locationCell.matches(':hover')) {
                updateTooltipContent(locationUpdate);
            }


        } catch (error) {
            console.error('Error updating location in grid:', error);
        }
    }

    // Animation system
    function queueLocationAnimation(locationUpdate) {
        animationQueue.push({
            locationId: locationUpdate.locationId,
            updateType: locationUpdate.updateType,
            timestamp: Date.now()
        });

        if (!isProcessingAnimations) {
            processAnimationQueue();
        }
    }

    async function processAnimationQueue() {
        if (animationQueue.length === 0) {
            isProcessingAnimations = false;
            return;
        }

        isProcessingAnimations = true;
        const animation = animationQueue.shift();

        try {
            await playLocationAnimation(animation);
        } catch (error) {
            console.error('Error playing animation:', error);
        }

        // Process next animation after a short delay
        setTimeout(processAnimationQueue, 200);
    }

    async function playLocationAnimation(animation) {
        const locationCell = document.querySelector(`[data-location-id="${animation.locationId}"]`);

        if (!locationCell) {
            return;
        }

        // Different animations based on update type
        switch (animation.updateType) {
            case 'InventoryAdded':
                await animateInventoryAdded(locationCell);
                break;
            case 'InventoryRemoved':
                await animateInventoryRemoved(locationCell);
                break;
            case 'StatusChanged':
                await animateStatusChange(locationCell);
                break;
            case 'UtilizationChanged':
                await animateUtilizationChange(locationCell);
                break;
            default:
                await animateGenericUpdate(locationCell);
        }
    }

    // Animation functions
    async function animateInventoryAdded(locationCell) {
        locationCell.classList.add('location-update-added');

        return new Promise(resolve => {
            setTimeout(() => {
                locationCell.classList.remove('location-update-added');
                resolve();
            }, 1000);
        });
    }

    async function animateInventoryRemoved(locationCell) {
        locationCell.classList.add('location-update-removed');

        return new Promise(resolve => {
            setTimeout(() => {
                locationCell.classList.remove('location-update-removed');
                resolve();
            }, 1000);
        });
    }

    async function animateStatusChange(locationCell) {
        locationCell.classList.add('location-status-changed');

        return new Promise(resolve => {
            setTimeout(() => {
                locationCell.classList.remove('location-status-changed');
                resolve();
            }, 800);
        });
    }

    async function animateUtilizationChange(locationCell) {
        locationCell.classList.add('location-utilization-changed');

        return new Promise(resolve => {
            setTimeout(() => {
                locationCell.classList.remove('location-utilization-changed');
                resolve();
            }, 600);
        });
    }

    async function animateGenericUpdate(locationCell) {
        locationCell.classList.add('location-updated');

        return new Promise(resolve => {
            setTimeout(() => {
                locationCell.classList.remove('location-updated');
                resolve();
            }, 500);
        });
    }

    // Notification system
    function shouldShowNotification(locationUpdate) {
        // Only show notifications for significant changes
        return locationUpdate.updateType === 'InventoryAdded' ||
            locationUpdate.updateType === 'InventoryRemoved' ||
            locationUpdate.updateType === 'StatusChanged';
    }

    function showLocationChangeNotification(locationUpdate) {
        let message = '';
        let type = 'info';

        switch (locationUpdate.updateType) {
            case 'InventoryAdded':
                message = `Inventory added to ${locationUpdate.locationCode}`;
                type = 'success';
                break;
            case 'InventoryRemoved':
                message = `Inventory removed from ${locationUpdate.locationCode}`;
                type = 'warning';
                break;
            case 'StatusChanged':
                message = `${locationUpdate.locationCode} status changed to ${locationUpdate.status}`;
                type = 'info';
                break;
        }

        if (message) {
            showNotification(message, type, 3000); // Auto-dismiss after 3 seconds
        }
    }

    function showNotification(message, type = 'info', autoDismiss = 5000) {
        // Create notification element
        const notification = document.createElement('div');
        notification.className = `notification notification-${type}`;
        notification.innerHTML = `
            <div class="notification-content">
                <span class="notification-icon">
                    ${getNotificationIcon(type)}
                </span>
                <span class="notification-message">${message}</span>
                <button class="notification-close" onclick="this.parentElement.parentElement.remove()">
                    <iconify-icon icon="heroicons:x-mark"></iconify-icon>
                </button>
            </div>
        `;

        // Add to page
        let notificationContainer = document.getElementById('signalr-notifications');
        if (!notificationContainer) {
            notificationContainer = document.createElement('div');
            notificationContainer.id = 'signalr-notifications';
            notificationContainer.className = 'notification-container';
            document.body.appendChild(notificationContainer);
        }

        notificationContainer.appendChild(notification);

        // Auto-dismiss
        if (autoDismiss > 0) {
            setTimeout(() => {
                if (notification.parentElement) {
                    notification.remove();
                }
            }, autoDismiss);
        }
    }

    function getNotificationIcon(type) {
        switch (type) {
            case 'success': return '<iconify-icon icon="heroicons:check-circle"></iconify-icon>';
            case 'warning': return '<iconify-icon icon="heroicons:exclamation-triangle"></iconify-icon>';
            case 'error': return '<iconify-icon icon="heroicons:x-circle"></iconify-icon>';
            default: return '<iconify-icon icon="heroicons:information-circle"></iconify-icon>';
        }
    }

    // Connection status indicator
    function updateConnectionStatus(connected, message = '') {
        let statusIndicator = document.getElementById('signalr-status');

        if (!statusIndicator) {
            statusIndicator = document.createElement('div');
            statusIndicator.id = 'signalr-status';
            statusIndicator.className = 'signalr-status-indicator';

            // Add to grid header or create a dedicated area
            const gridHeader = document.querySelector('.card-header');
            if (gridHeader) {
                gridHeader.appendChild(statusIndicator);
            }
        }

        const statusClass = connected ? 'connected' : 'disconnected';
        const statusText = message || (connected ? 'Real-time updates active' : 'Real-time updates offline');

        statusIndicator.className = `signalr-status-indicator ${statusClass}`;
        statusIndicator.innerHTML = `
            <span class="status-dot"></span>
            <span class="status-text">${statusText}</span>
        `;
    }

    // Heartbeat system
    function startHeartbeat() {
        if (!isConnected) return;

        setInterval(() => {
            if (warehouseConnection && warehouseConnection.state === signalR.HubConnectionState.Connected) {
                warehouseConnection.invoke('Ping').catch(err => {
                    console.error('Heartbeat failed:', err);
                });
            }
        }, 30000); // Ping every 30 seconds
    }

    function onPong(timestamp) {
        //console.log('SignalR heartbeat response:', timestamp);
    }

    // Future feature handlers
    function onInventoryMoved(movementUpdate) {
        showNotification(
            `${movementUpdate.productSKU} moved from ${movementUpdate.fromLocationCode} to ${movementUpdate.toLocationCode}`,
            'info'
        );
    }

    function onWarehouseNotification(notification) {
        showNotification(notification.message, notification.type, notification.autoDismissAfter * 1000);
    }
    // Zone management functions
    function autoJoinCurrentZone() {
        if (!isConnected || isJoiningZone) {
            return;
        }

        // Get current zone from the zone selector
        const zoneSelector = document.getElementById('zoneSelector');
        if (!zoneSelector || !zoneSelector.value) {
            updateConnectionStatus(true, 'Connected - select a zone to receive updates');
            return;
        }

        const selectedZoneId = zoneSelector.value;
        joinZoneGroup(selectedZoneId);
    }

    function joinZoneGroup(zoneId) {
        if (!isConnected || isJoiningZone || !zoneId) {
            return;
        }

        isJoiningZone = true;
        updateConnectionStatus(true, 'Joining zone...');

        warehouseConnection.invoke('JoinZoneGroup', zoneId).catch(err => {
            console.error('Failed to join zone group:', err);
            isJoiningZone = false;
            showNotification('Failed to join zone for real-time updates', 'error');
        });
    }

    function leaveZoneGroup(zoneId) {
        if (!isConnected || !zoneId) {
            return;
        }

        warehouseConnection.invoke('LeaveZoneGroup', zoneId).catch(err => {
            console.error('Failed to leave zone group:', err);
        });
    }

    function switchZoneGroup(fromZoneId, toZoneId) {
        if (!isConnected || isJoiningZone || !toZoneId) {
            return;
        }

        isJoiningZone = true;
        updateConnectionStatus(true, 'Switching zone...');

        warehouseConnection.invoke('SwitchZoneGroup', fromZoneId, toZoneId).catch(err => {
            console.error('Failed to switch zone group:', err);
            isJoiningZone = false;
            showNotification('Failed to switch zone for real-time updates', 'error');
        });
    }

    // Monitor zone selector changes
    function setupZoneMonitoring() {
        const zoneSelector = document.getElementById('zoneSelector');
        if (zoneSelector) {
            zoneSelector.addEventListener('change', function (event) {
                const newZoneId = event.target.value;
                const oldZoneId = currentZoneId;

                if (newZoneId && newZoneId !== oldZoneId) {
                    if (oldZoneId) {
                        switchZoneGroup(oldZoneId, newZoneId);
                    } else {
                        joinZoneGroup(newZoneId);
                    }
                } else if (!newZoneId && oldZoneId) {
                    leaveZoneGroup(oldZoneId);
                }
            });

        }
    }
    function getStatusClassSignalR(status) {
        switch (status) {
            case 'Available': return 'available';
            case 'Partial': return 'partial';
            case 'Occupied': return 'occupied';
            case 'Reserved': return 'reserved';
            case 'Maintenance': return 'maintenance';
            case 'Blocked': return 'blocked';
            default: return 'available';
        }
    }

    function updateTooltipContent(locationUpdate) {
        try {
            const tooltipContent = document.getElementById('tooltipContent');
            if (tooltipContent) {
                tooltipContent.innerHTML = `
                <div><strong>${locationUpdate.locationCode}</strong></div>
                <div>Status: ${locationUpdate.status}</div>
                <div>Items: ${locationUpdate.inventoryCount}</div>
                <div>Quantity: ${locationUpdate.totalQuantity}</div>
                <div>Updated: ${new Date(locationUpdate.updatedAt).toLocaleTimeString()}</div>
                <div>By: ${locationUpdate.updatedBy}</div>
            `;
            }
        } catch (error) {
            console.error('Error updating tooltip content:', error);
        }
    }

    // Public API
    window.WarehouseSignalR = {
        initialize: initializeSignalR,
        isConnected: () => isConnected,
        connection: () => warehouseConnection,
        currentZone: () => currentZoneId,

        // Zone management
        joinZone: joinZoneGroup,
        leaveZone: leaveZoneGroup,
        switchZone: switchZoneGroup,

        // Manual methods for testing
        ping: () => warehouseConnection?.invoke('Ping')
    };

    // Auto-initialize when DOM is ready
    document.addEventListener('DOMContentLoaded', function () {
        // Only initialize if we're on the location grid page
        if (document.getElementById('locationGrid')) {
            initializeSignalR();
            setupZoneMonitoring();
        }
    });

    // Cleanup on page unload
    window.addEventListener('beforeunload', function () {
        if (warehouseConnection) {
            warehouseConnection.stop();
        }
    });
})();