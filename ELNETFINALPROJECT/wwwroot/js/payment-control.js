// Payment Processing Control
// This file controls which payment methods are enabled/disabled

/**
 * Check if payment method is available
 * @param {string} paymentMethod - 'card', 'gcash', or 'wallet'
 * @returns {boolean}
 */
function isPaymentMethodAvailable(paymentMethod) {
    const enabledMethods = {
        'card': true,      // ✅ Credit/Debit Card - ENABLED
        'gcash': false,    // ❌ GCash QR - DISABLED (coming soon)
        'wallet': false    // ❌ Digital Wallet - DISABLED (coming soon)
    };
    
    return enabledMethods[paymentMethod] || false;
}

/**
 * Override the original showPayMethod to restrict disabled payment methods
 */
function showPayMethod(method) {
    // Check if payment method is available
    if (!isPaymentMethodAvailable(method)) {
        showPaymentMethodDisabledAlert(method);
        // Reset to card (default)
        document.querySelector('input[name="payMethod"][value="card"]').checked = true;
        showPayMethodDefault('card');
        return;
    }
    
    // Original functionality
    const cardFields = document.getElementById('cardFields');
    const gcashFields = document.getElementById('gcashFields');
    const walletFields = document.getElementById('walletFields');
    
    // Hide all payment method forms
    if (cardFields) cardFields.style.display = 'none';
    if (gcashFields) gcashFields.style.display = 'none';
    if (walletFields) walletFields.style.display = 'none';
    
    // Show selected method
    if (method === 'card' && cardFields) {
        cardFields.style.display = 'block';
    } else if (method === 'gcash' && gcashFields) {
        gcashFields.style.display = 'block';
    } else if (method === 'wallet' && walletFields) {
        walletFields.style.display = 'block';
    }
}

/**
 * Show default card payment form
 */
function showPayMethodDefault(method) {
    const cardFields = document.getElementById('cardFields');
    const gcashFields = document.getElementById('gcashFields');
    const walletFields = document.getElementById('walletFields');
    
    if (cardFields) cardFields.style.display = 'block';
    if (gcashFields) gcashFields.style.display = 'none';
    if (walletFields) walletFields.style.display = 'none';
}

/**
 * Show alert that payment method is not yet available
 */
function showPaymentMethodDisabledAlert(method) {
    const methodNames = {
        'gcash': 'GCash QR Payment',
        'wallet': 'Digital Wallets'
    };
    
    const methodName = methodNames[method] || method;
    
    // Show alert (use setTimeout to ensure it works after radio click)
    setTimeout(() => {
        alert(`${methodName} is coming soon! 🚀\n\nCurrently, only Credit/Debit Card payments are available.\n\nPlease select Card Payment to continue.`);
    }, 100);
}

/**
 * Override executePayment to validate payment method
 */
function executePaymentWithValidation() {
    // Get selected payment method
    const paymentMethod = document.querySelector('input[name="payMethod"]:checked')?.value;
    
    if (!paymentMethod) {
        alert('Please select a payment method');
        return;
    }
    
    // Check if method is available
    if (!isPaymentMethodAvailable(paymentMethod)) {
        showPaymentMethodDisabledAlert(paymentMethod);
        return;
    }
    
    // If card payment, proceed with normal execution
    if (paymentMethod === 'card') {
        executePayment();
    } else {
        alert(`${paymentMethod} payment is not yet available.`);
    }
}

/**
 * Disable payment method radio buttons that aren't available
 * Call this on page load
 */
function initializePaymentMethods() {
    const paymentOptions = document.querySelectorAll('input[name="payMethod"]');
    
    paymentOptions.forEach(option => {
        const method = option.value;
        const label = option.closest('.pay-label');
        
        if (!isPaymentMethodAvailable(method)) {
            // Disable the radio button
            option.disabled = true;
            
            // Add visual indicator
            if (label) {
                label.style.opacity = '0.5';
                label.style.cursor = 'not-allowed';
                
                // Add tooltip/hint
                label.title = `${method === 'gcash' ? 'GCash QR Payment' : 'Digital Wallets'} - Coming Soon`;
                
                // Add "Coming Soon" badge if there's space
                if (!label.querySelector('.coming-soon-badge')) {
                    const badge = document.createElement('span');
                    badge.className = 'coming-soon-badge';
                    badge.textContent = 'Soon';
                    badge.style.cssText = `
                        position: absolute;
                        top: 0;
                        right: 0;
                        background: rgba(255,170,0,0.2);
                        color: #ffaa00;
                        padding: 2px 6px;
                        border-radius: 3px;
                        font-size: 8px;
                        font-weight: 700;
                        letter-spacing: 0.5px;
                        text-transform: uppercase;
                    `;
                    label.style.position = 'relative';
                    label.appendChild(badge);
                }
            }
        }
    });
    
    // Set default to card payment if it's the only option
    const cardRadio = document.querySelector('input[name="payMethod"][value="card"]');
    if (cardRadio && !cardRadio.disabled) {
        cardRadio.checked = true;
        showPayMethodDefault('card');
    }
}

/**
 * Update payment button to use validation
 */
function updatePaymentButton() {
    const executeBtn = document.querySelector('.execute-btn');
    if (executeBtn) {
        // Store original onclick
        const originalOnClick = executeBtn.getAttribute('onclick');
        // Replace with our validation function
        executeBtn.setAttribute('onclick', 'executePaymentWithValidation()');
    }
}

// Initialize on page load
document.addEventListener('DOMContentLoaded', function() {
    initializePaymentMethods();
    updatePaymentButton();
});

// Also run on modal open (add to openTopUp function)
window.addEventListener('topUpModalOpened', function() {
    initializePaymentMethods();
    updatePaymentButton();
});
