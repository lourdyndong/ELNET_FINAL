// Live Session Management for Player Dashboard

let currentSession = {
    isActive: false,
    stationNumber: null,
    startTime: null,
    remainingTime: 0, // in minutes
    rate: 25 // per hour
};

let sessionUpdateInterval = null;

/**
 * Initialize a live gaming session
 * @param {number} stationNumber - PC number (1-40)
 * @param {number} paidMinutes - Total minutes paid for
 * @param {string} gameName - Name of the game being played
 */
function initializeSession(stationNumber, paidMinutes) {
    currentSession.isActive = true;
    currentSession.stationNumber = stationNumber;
    currentSession.startTime = new Date();
    currentSession.remainingTime = paidMinutes;
    
    // Calculate rate based on PC number
    // PC 1-10: ₱25/hr, PC 11-40: ₱20/hr
    currentSession.rate = (stationNumber >= 1 && stationNumber <= 10) ? 25 : 20;
    
    showLiveSession();
    startSessionTimer();
}

function showLiveSession() {
    const card = document.getElementById('liveSessionCard');
    if (card) {
        card.style.display = 'grid';
    }
    updateSessionDisplay();
}

function hideLiveSession() {
    const card = document.getElementById('liveSessionCard');
    if (card) {
        card.style.display = 'none';
    }
    stopSessionTimer();
}

function startSessionTimer() {
    if (sessionUpdateInterval) {
        clearInterval(sessionUpdateInterval);
    }
    
    // Update every second
    sessionUpdateInterval = setInterval(() => {
        if (currentSession.isActive) {
            updateSessionDisplay();
            
            // Check if time is up
            if (currentSession.remainingTime <= 0) {
                endSession();
            }
        }
    }, 1000);
}

function stopSessionTimer() {
    if (sessionUpdateInterval) {
        clearInterval(sessionUpdateInterval);
        sessionUpdateInterval = null;
    }
}

function updateSessionDisplay() {
    if (!currentSession.isActive) return;
    
    // Calculate elapsed time
    const now = new Date();
    const elapsedMs = now - currentSession.startTime;
    const elapsedMinutes = Math.floor(elapsedMs / 60000);
    const elapsedSeconds = Math.floor((elapsedMs % 60000) / 1000);
    const elapsedHours = Math.floor(elapsedMinutes / 60);
    const elapsedMins = elapsedMinutes % 60;
    
    // Calculate remaining time
    const remainingMinutes = Math.max(0, currentSession.remainingTime - elapsedMinutes);
    const remainingSeconds = Math.max(0, 60 - elapsedSeconds);
    const remainingHours = Math.floor(remainingMinutes / 60);
    const remainingMins = remainingMinutes % 60;
    
    // Format time display
    const elapsedDisplay = `${elapsedHours}h ${String(elapsedMins).padStart(2, '0')}m`;
    const remainingDisplay = `${remainingHours}h ${String(remainingMins).padStart(2, '0')}m`;
    
    // Update DOM elements
    updateElement('sessionStation', `PC ${String(currentSession.stationNumber).padStart(2, '0')}`);
    updateElement('sessionElapsed', elapsedDisplay);
    updateElement('sessionRemaining', remainingDisplay);
    updateElement('sessionRate', `₱${currentSession.rate} / hr`);
}

function updateElement(id, value) {
    const element = document.getElementById(id);
    if (element) {
        element.textContent = value;
    }
}

function endSession() {
    currentSession.isActive = false;
    stopSessionTimer();
    hideLiveSession();
    // Could trigger end session effects here
    console.log('Session ended');
}

/**
 * Format time in HH:MM format
 */
function formatTime(hours, minutes) {
    return `${hours}h ${String(minutes).padStart(2, '0')}m`;
}

// Example: Start a session when page loads (for testing)
// Uncomment to test
// document.addEventListener('DOMContentLoaded', function() {
//     // Simulate starting a session on PC 5 with 2 hours (120 minutes) paid time
//     initializeSession(5, 120, 'Counter-Strike 2');
// });
