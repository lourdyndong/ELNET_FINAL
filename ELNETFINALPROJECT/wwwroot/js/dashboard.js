// Dashboard - Player Panel JavaScript
// Manages UI interactions and payment processing

let topUpAmount = 0;

/**
 * Open the top-up modal and emit event for payment-control.js
 */
function openTopUp() {
    const modal = document.getElementById('topUpModal');
    if (modal) {
        modal.classList.add('open');
        // Emit event so payment-control.js can reinitialize
        window.dispatchEvent(new Event('topUpModalOpened'));
    }
}

/**
 * Close the top-up modal
 */
function closeTopUp() {
    const modal = document.getElementById('topUpModal');
    if (modal) {
        modal.classList.remove('open');
    }
}

/**
 * Select a preset amount
 * @param {number} amount - The preset amount value
 * @param {HTMLElement} el - The clicked element (passed via onclick)
 */
function selectAmount(amount, el) {
    topUpAmount = amount;
    
    // Update UI
    const presets = document.querySelectorAll('.amount-preset');
    presets.forEach(preset => {
        preset.classList.remove('selected');
    });
    
    if (el) el.classList.add('selected');
    
    // Clear custom amount
    const customInput = document.getElementById('customAmount');
    if (customInput) {
        customInput.value = '';
    }
    
    updateSummary();
}

/**
 * Handle custom amount input
 */
function onCustomAmount(value) {
    topUpAmount = parseFloat(value) || 0;
    
    // Deselect all presets
    const presets = document.querySelectorAll('.amount-preset');
    presets.forEach(preset => {
        preset.classList.remove('selected');
    });
    
    updateSummary();
}

/**
 * Clear preset selections
 */
function clearPresets() {
    const presets = document.querySelectorAll('.amount-preset');
    presets.forEach(preset => {
        preset.classList.remove('selected');
    });
}

/**
 * Update the summary display
 */
function updateSummary() {
    // Format amount
    const formattedAmount = '₱' + topUpAmount.toFixed(2);
    const summaryTopup = document.getElementById('summaryTopup');
    if (summaryTopup) {
        summaryTopup.textContent = '+' + formattedAmount;
    }
}

/**
 * Execute payment - This is called by executePaymentWithValidation from payment-control.js
 * Only card payments reach this function (validation happens in payment-control.js)
 */
function executePayment() {
    if (topUpAmount <= 0) {
        alert('Please enter a valid amount');
        return;
    }
    
    // Show processing
    const executeBtn = document.querySelector('.execute-btn');
    const originalText = executeBtn.innerHTML;
    executeBtn.disabled = true;
    executeBtn.innerHTML = 'PROCESSING <i class="bi bi-hourglass-split"></i>';
    
    // Send top-up request to backend
    fetch('/Player/TopUpBalance', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/x-www-form-urlencoded'
        },
        body: 'amount=' + encodeURIComponent(topUpAmount)
    })
    .then(response => response.json())
    .then(data => {
        if (data.success) {
            // Show success
            alert(`✅ Payment successful!\n\nTop-up amount: ₱${topUpAmount.toFixed(2)}\nNew balance: ₱${data.newBalance.toFixed(2)}`);
            
            // Update UI
            document.getElementById('balanceDisplay').textContent = '₱' + data.newBalance.toFixed(2);
            
            // Reset and close modal
            topUpAmount = 0;
            closeTopUp();
            
            // Refresh profile and dashboard data from backend
            if (typeof getProfileData === 'function') {
                getProfileData();
            }
            if (typeof loadDashboardData === 'function') {
                loadDashboardData();
            }
        } else {
            alert(`❌ Payment failed: ${data.message}`);
        }
    })
    .catch(error => {
        console.error('Error:', error);
        alert('❌ Error processing payment: ' + error.message);
    })
    .finally(() => {
        // Restore button
        executeBtn.disabled = false;
        executeBtn.innerHTML = originalText;
    });
}

/**
 * Load and display profile data
 */
function getProfileData() {
    fetch('/Player/GetProfileData')
        .then(response => response.json())
        .then(data => {
            if (data.success) {
                // Update UI with real profile data
                document.getElementById('profileUsername').textContent = data.displayName;
                document.getElementById('profileEmail').textContent = data.email;
                document.getElementById('profileDisplayName').value = data.displayName;
                document.getElementById('balanceDisplay').textContent = '₱' + data.balance.toFixed(2);
                document.getElementById('sidebarUsername').textContent = data.displayName;
                
                // Update profile photo
                const profilePhoto = document.getElementById('profilePhoto');
                const sidebarAvatar = document.getElementById('sidebarAvatar');
                
                if (data.profilePicture) {
                    const img = document.createElement('img');
                    img.src = data.profilePicture;
                    profilePhoto.innerHTML = '';
                    profilePhoto.appendChild(img);
                    
                    const sidebarImg = document.createElement('img');
                    sidebarImg.src = data.profilePicture;
                    sidebarAvatar.innerHTML = '';
                    sidebarAvatar.appendChild(sidebarImg);
                } else {
                    // Show initials
                    const initials = (data.displayName.charAt(0) + data.displayName.charAt(1)).toUpperCase();
                    profilePhoto.innerHTML = '<span>' + initials + '</span>';
                    sidebarAvatar.innerHTML = '<span>' + initials + '</span>';
                    document.getElementById('profilePhotoInitials').textContent = initials;
                    document.getElementById('sidebarAvatarInitials').textContent = initials;
                }
                
                // Reload dashboard data to get updated stats
                if (typeof loadDashboardData === 'function') {
                    loadDashboardData();
                }
            }
        })
        .catch(error => console.error('Error loading profile:', error));
}

/**
 * Handle profile picture upload
 */
function handleProfilePictureUpload(input) {
    if (input.files && input.files[0]) {
        const formData = new FormData();
        formData.append('profilePicture', input.files[0]);
        
        fetch('/Player/UpdateProfile', {
            method: 'POST',
            body: formData
        })
        .then(response => response.json())
        .then(data => {
            if (data.success) {
                alert('✅ Profile picture updated successfully');
                getProfileData();
                input.value = ''; // Reset file input
            } else {
                alert('❌ Error: ' + data.message);
            }
        })
        .catch(error => {
            console.error('Error:', error);
            alert('❌ Error uploading profile picture');
        });
    }
}

/**
 * Update credentials (password)
 */
function updateCredentials() {
    const currentPassword = document.getElementById('currentPassword').value;
    const newPassword = document.getElementById('newPassword').value;
    const confirmPassword = document.getElementById('confirmPassword').value;
    
    if (!currentPassword || !newPassword || !confirmPassword) {
        alert('❌ All fields are required');
        return;
    }
    
    fetch('/Player/UpdateCredentials', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/x-www-form-urlencoded'
        },
        body: `currentPassword=${encodeURIComponent(currentPassword)}&newPassword=${encodeURIComponent(newPassword)}&confirmPassword=${encodeURIComponent(confirmPassword)}`
    })
    .then(response => response.json())
    .then(data => {
        if (data.success) {
            alert('✅ ' + data.message);
            document.getElementById('currentPassword').value = '';
            document.getElementById('newPassword').value = '';
            document.getElementById('confirmPassword').value = '';
        } else {
            alert('❌ Error: ' + data.message);
        }
    })
    .catch(error => {
        console.error('Error:', error);
        alert('❌ Error updating credentials');
    });
}

/**
 * Initialize on page load
 */
document.addEventListener('DOMContentLoaded', function() {
    getProfileData();
});
