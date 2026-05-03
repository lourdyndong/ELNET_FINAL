// Stations page JS extracted from Stations.cshtml
const PESOS_PER_HOUR = 20;
let pcList = [];
let selectedPC = null;

// Initialize 40 PCs
function initializePCs() {
    pcList = [];
    for (let i = 1; i <= 40; i++) {
        pcList.push({
            id: i,
            status: Math.random() > 0.7 ? 'offline' : 'available',
            currentUser: null,
            timeUsed: 0,
            timePaid: 0,
            isPoweredOn: Math.random() > 0.7 ? false : true
        });
    }
}

// Render PC grid
function renderPCs() {
    const grid = document.getElementById('stationsGrid');
    if (!grid) return;
    grid.innerHTML = pcList.map(pc => {
        const dotClass = pc.status === 'active' ? 'dot-green' : pc.status === 'in-use' ? 'dot-amber' : pc.status === 'offline' ? 'dot-red' : 'dot-gray';
        const statusLabel = pc.status === 'active' ? 'Active' : pc.status === 'in-use' ? 'In Use' : pc.status === 'offline' ? 'Offline' : 'Available';
        const isSelected = selectedPC && selectedPC.id === pc.id;
        return `
        <div class="station-card ${pc.status === 'offline' ? 'offline' : ''} ${isSelected ? 'selected' : ''}" onclick="selectPC(${pc.id})">
            <div class="station-top">
                <span class="station-num">PC ${pc.id}</span>
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

    const pcTitle = document.getElementById('pcTitle'); if (pcTitle) pcTitle.textContent = `PC ${selectedPC.id} Management`;
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
    const modalPCTitle = document.getElementById('modalPCTitle'); if (modalPCTitle) modalPCTitle.textContent = `PC ${selectedPC.id} — Guest Login`;
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
        selectedPC.status = 'in-use';
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
    initializePCs();
    renderPCs();
});

// Toggle PC power function exposed globally
window.togglePCPower = function() {
    if (!selectedPC) return;
    selectedPC.isPoweredOn = !selectedPC.isPoweredOn;
    if (!selectedPC.isPoweredOn) {
        selectedPC.status = 'offline';
    } else if (!selectedPC.currentUser) {
        selectedPC.status = 'available';
    }
    selectPC(selectedPC.id);
};

// Update statistics
function updateStats() {
    const total = pcList.length;
    const active = pcList.filter(p => p.status === 'active').length;
    const inUse = pcList.filter(p => p.status === 'in-use').length;
    const offline = pcList.filter(p => p.status === 'offline').length;
    const available = pcList.filter(p => p.status === 'available').length;

    const infoTotal = document.getElementById('infoTotal'); if (infoTotal) infoTotal.textContent = total;
    const infoActive = document.getElementById('infoActive'); if (infoActive) infoActive.textContent = active;
    const infoInUse = document.getElementById('infoInUse'); if (infoInUse) infoInUse.textContent = inUse;
    const infoOffline = document.getElementById('infoOffline'); if (infoOffline) infoOffline.textContent = offline;
    const infoAvailable = document.getElementById('infoAvailable'); if (infoAvailable) infoAvailable.textContent = available;

    const pct = total > 0 ? Math.round(((active + inUse) / total) * 100) : 0;
    const occFill = document.querySelector('.occ-fill'); if (occFill) occFill.style.width = pct + '%';
    const occSub = document.querySelector('.occ-sub'); if (occSub) occSub.textContent = `${pct}% occupied`;
}

// Toggle all stations (simple refresh stub)
window.toggleAllStations = function() { initializePCs(); renderPCs(); };
