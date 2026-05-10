// Stations page JS extracted from Stations.cshtml
const PESOS_PER_HOUR = 20;
let pcList = [];
let selectedPC = null;

// Fetch all stations from backend
async function loadStationsFromDB() {
    try {
        const response = await fetch('/Home/GetAllStations', { method: 'GET' });
        const result = await response.json();
        
        if (result.success && result.stations) {
            // Map DB stations to pcList format
            pcList = result.stations.map(station => ({
                id: station.id,
                stationNumber: station.stationNumber,
                status: station.status,
                currentUser: station.currentUser,
                currentPlayerId: station.currentPlayerId,
                timeUsed: station.timeUsedMinutes,
                timePaid: station.timePaidMinutes,
                isPoweredOn: station.isPoweredOn,
                unavailable: station.isUnavailable,
                sessionStartTime: station.sessionStartTime
            }));
        } else {
            initializePCs(); // Fallback to client-side if DB empty
        }
    } catch (e) {
        console.error('Error loading stations:', e);
        initializePCs(); // Fallback
    }
}

// Initialize 40 PCs (client-side fallback)
function initializePCs() {
    pcList = [];
    for (let i = 1; i <= 40; i++) {
        pcList.push({
            id: i,
            stationNumber: i,
            status: 'offline',
            currentUser: null,
            currentPlayerId: null,
            timeUsed: 0,
            timePaid: 0,
            isPoweredOn: false,
            unavailable: false
        });
    }
}

// Render PC grid
function renderPCs() {
    const grid = document.getElementById('stationsGrid');
    if (!grid) return;
    grid.innerHTML = pcList.map(pc => {
        const dotClass = pc.status === 'active' ? 'dot-green' : pc.status === 'offline' ? 'dot-red' : pc.status === 'unavailable' ? 'dot-gray' : 'dot-gray';
        const statusLabel = pc.status === 'active' ? 'Active' : pc.status === 'offline' ? 'Offline' : pc.status === 'unavailable' ? 'Unavailable' : 'Available';
        const isSelected = selectedPC && selectedPC.id === pc.id;
        return `
        <div class="station-card ${pc.status === 'offline' ? 'offline' : ''} ${isSelected ? 'selected' : ''}" onclick="selectPC(${pc.id})">
            <div class="station-top">
                <span class="station-num">PC ${pc.stationNumber}</span>
                <span class="station-dot ${dotClass}"></span>
            </div>
            <div class="station-avatar">
                <i class="bi bi-display"></i>
            </div>
            <div class="station-username">${pc.currentUser || '—'}</div>
            <div class="station-game">${statusLabel}</div>
            ${pc.currentUser ? `<div class="station-time"><i class="bi bi-clock"></i> ${formatTime(pc.timePaid)}</div>` : ''}
        </div>`;
    }).join('');
    updateStats();
}

// Format time (minutes to h:mm format)
function formatTime(minutes) {
    const hours = Math.floor(minutes / 60);
    const mins = minutes % 60;
    return `${hours}h ${mins}m`;
}

// Select PC and show management panel
function selectPC(pcId) {
    selectedPC = pcList.find(p => p.id === pcId);
    if (!selectedPC) return;

    const emptyPanel = document.getElementById('emptyPanel');
    if (emptyPanel) emptyPanel.style.display = 'none';
    const managementPanel = document.getElementById('pcManagementPanel');
    if (managementPanel) managementPanel.style.display = 'block';

    const pcTitle = document.getElementById('pcTitle'); if (pcTitle) pcTitle.textContent = `PC ${selectedPC.stationNumber} Management`;
    const pcStatus = document.getElementById('pcStatus'); if (pcStatus) pcStatus.textContent = selectedPC.status.charAt(0).toUpperCase() + selectedPC.status.slice(1);
    const pcCurrentUser = document.getElementById('pcCurrentUser'); if (pcCurrentUser) pcCurrentUser.textContent = selectedPC.currentUser || '—';
    const pcTimeUsed = document.getElementById('pcTimeUsed'); if (pcTimeUsed) pcTimeUsed.textContent = selectedPC.currentUser ? `${formatTime(selectedPC.timeUsed)} / ${formatTime(selectedPC.timePaid)}` : '—';
    const pcPowerBtn = document.getElementById('pcPowerBtn'); if (pcPowerBtn) pcPowerBtn.textContent = selectedPC.isPoweredOn ? '⏹ Power Off' : '▶ Power On';
    renderPCs();
}

function closePCManagement() {
    selectedPC = null;
    const managementPanel = document.getElementById('pcManagementPanel'); if (managementPanel) managementPanel.style.display = 'none';
    const emptyPanel = document.getElementById('emptyPanel'); if (emptyPanel) emptyPanel.style.display = 'block';
    renderPCs();
}

// Guest login modal
function openGuestLoginModal() {
    if (!selectedPC) return;
    const modalPCTitle = document.getElementById('modalPCTitle'); if (modalPCTitle) modalPCTitle.textContent = `PC ${selectedPC.stationNumber} — Guest Login`;
    const guestModal = document.getElementById('guestLoginModal'); if (guestModal) guestModal.classList.add('active');
    const paymentAmount = document.getElementById('paymentAmount'); if (paymentAmount) paymentAmount.value = '20';
    updatePaymentCalc();
}

function closeGuestLoginModal() {
    const guestModal = document.getElementById('guestLoginModal'); if (guestModal) guestModal.classList.remove('active');
    const guestName = document.getElementById('guestName'); if (guestName) guestName.value = '';
    const paymentAmount = document.getElementById('paymentAmount'); if (paymentAmount) paymentAmount.value = '20';
}

// Calculate payment and time
function updatePaymentCalc() {
    const amountEl = document.getElementById('paymentAmount');
    const amount = amountEl ? parseFloat(amountEl.value) || 0 : 0;
    const hours = amount / PESOS_PER_HOUR;
    const totalMinutes = Math.floor(hours * 60);
    const displayHours = Math.floor(hours);
    const displayMins = totalMinutes % 60;

    const paymentTotal = document.getElementById('paymentTotal'); if (paymentTotal) paymentTotal.textContent = `₱${amount}`;
    const sessionTime = document.getElementById('sessionTime'); if (sessionTime) sessionTime.textContent = `${displayHours}h ${displayMins}m`;
    const calcTime = document.getElementById('calcTime'); if (calcTime) calcTime.textContent = `${displayHours}h ${displayMins}m`;
}

// wire the input after DOM is ready
document.addEventListener('DOMContentLoaded', function(){
    const paymentInput = document.getElementById('paymentAmount');
    if (paymentInput) paymentInput.addEventListener('input', updatePaymentCalc);

    // Process guest login
    window.processGuestLogin = function(event) {
        event.preventDefault();
        if (!selectedPC) return;

        const guestNameEl = document.getElementById('guestName');
        const paymentEl = document.getElementById('paymentAmount');
        const errorEl = document.getElementById('guestError');
        const guestName = guestNameEl ? guestNameEl.value.trim() : '';
        const paymentAmount = paymentEl ? parseFloat(paymentEl.value) || 0 : 0;

        if (!guestName) {
            if (errorEl) { errorEl.textContent = 'Guest name is required.'; errorEl.style.display = 'block'; }
            return;
        }
        if (paymentAmount < 20) {
            if (errorEl) { errorEl.textContent = 'Payment must be at least 20 pesos (1 hour).'; errorEl.style.display = 'block'; }
            return;
        }
        if (errorEl) errorEl.style.display = 'none';

        // Update PC with guest session
        selectedPC.currentUser = guestName;
        selectedPC.timePaid = Math.floor((paymentAmount / PESOS_PER_HOUR) * 60);
        selectedPC.timeUsed = 0;
        selectedPC.status = 'active';
        selectedPC.isPoweredOn = true;

        closeGuestLoginModal();
        selectPC(selectedPC.id);
    };

    // Modal overlay click to close
    const guestModal = document.getElementById('guestLoginModal');
    if (guestModal) {
        guestModal.addEventListener('click', function(e){ if (e.target === this) closeGuestLoginModal(); });
    }

    // Initialize UI
    loadStationsFromDB().then(() => renderPCs()).catch(() => {
        initializePCs();
        renderPCs();
    });
});

// Toggle PC power function exposed globally
window.togglePCPower = function() {
    if (!selectedPC) return;
    fetch('/Home/ToggleStationPower', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ stationId: selectedPC.id })
    })
    .then(r => r.json())
    .then(result => {
        if (result.success) {
            selectedPC.isPoweredOn = result.powered;
            if (!result.powered) {
                selectedPC.status = 'offline';
                selectedPC.currentUser = null;
                selectedPC.timePaid = 0;
                selectedPC.timeUsed = 0;
            } else if (!selectedPC.currentUser) {
                selectedPC.status = 'available';
            }
            selectPC(selectedPC.id);
        }
    })
    .catch(err => console.error('Power toggle error:', err));
};

// Update statistics
function updateStats() {
    const total = pcList.length;
    const active = pcList.filter(p => p.status === 'active').length;
    const offline = pcList.filter(p => p.status === 'offline').length;
    const available = pcList.filter(p => p.status === 'available').length;

    const infoTotal = document.getElementById('infoTotal'); if (infoTotal) infoTotal.textContent = total;
    const infoActive = document.getElementById('infoActive'); if (infoActive) infoActive.textContent = active;
    const infoOffline = document.getElementById('infoOffline'); if (infoOffline) infoOffline.textContent = offline;
    const infoAvailable = document.getElementById('infoAvailable'); if (infoAvailable) infoAvailable.textContent = available;

    const pct = total > 0 ? Math.round((active / total) * 100) : 0;
    const occFill = document.querySelector('.occ-fill'); if (occFill) occFill.style.width = pct + '%';
    const occSub = document.querySelector('.occ-sub'); if (occSub) occSub.textContent = `${pct}% occupied`;
}

// Refresh: reload all stations from DB
window.toggleAllStations = function() { 
    loadStationsFromDB().then(() => renderPCs());
};

// Admin action: mark selected PC unavailable (hardware/problem)
window.markPCUnavailable = function() {
    if (!selectedPC) return;
    fetch('/Home/MarkStationUnavailable', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ stationId: selectedPC.id })
    })
    .then(r => r.json())
    .then(result => {
        if (result.success) {
            selectedPC.unavailable = true;
            selectedPC.isPoweredOn = false;
            selectedPC.currentUser = null;
            selectedPC.status = 'unavailable';
            selectPC(selectedPC.id);
        }
    })
    .catch(err => console.error('Mark unavailable error:', err));
}

// Admin action: remove selected station permanently
window.removeStation = function() {
    if (!selectedPC) return;
    const confirmed = confirm(`Remove PC ${selectedPC.stationNumber}? This cannot be undone.`);
    if (!confirmed) return;

    fetch('/Home/RemoveStation', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ stationId: selectedPC.id })
    })
    .then(r => r.json())
    .then(result => {
        if (result.success) {
            selectedPC = null;
            const managementPanel = document.getElementById('pcManagementPanel'); if (managementPanel) managementPanel.style.display = 'none';
            const emptyPanel = document.getElementById('emptyPanel'); if (emptyPanel) emptyPanel.style.display = 'block';
            loadStationsFromDB().then(() => renderPCs());
        } else {
            console.error('Remove station failed:', result.message);
        }
    })
    .catch(err => console.error('Remove station error:', err));
}

// Admin action: assign existing player to selected PC (simple assign)
window.assignPlayerToPC = function(playerName, minutesPaid) {
    if (!selectedPC) return;
    // For now, use playerName as-is (you can later integrate with player picker UI)
    selectedPC.currentUser = playerName;
    selectedPC.timePaid = minutesPaid || 60;
    selectedPC.timeUsed = 0;
    selectedPC.status = 'active';
    selectedPC.isPoweredOn = true;
    selectPC(selectedPC.id);
}

window.addStation = function() {
    fetch('/Home/AddStation', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' }
    })
    .then(r => r.json())
    .then(result => {
        if (result.success) {
            loadStationsFromDB().then(() => renderPCs());
        } else {
            console.error('Add station failed:', result.message);
        }
    })
    .catch(err => {
        console.error('Add station error:', err);
    });
}

