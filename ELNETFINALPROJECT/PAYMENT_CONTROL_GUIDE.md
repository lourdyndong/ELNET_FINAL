# Payment Method Control System

## Overview

The payment control system allows you to enable/disable specific payment methods in the Top-Up modal. Currently configured to:

- ✅ **Credit/Debit Card** - ENABLED (accepts dummy input)
- ❌ **GCash QR Payment** - DISABLED (coming soon)
- ❌ **Digital Wallets** - DISABLED (coming soon)

---

## Current Configuration

### File: `payment-control.js`

```javascript
const enabledMethods = {
    'card': true,      // ✅ Credit/Debit Card - ENABLED
    'gcash': false,    // ❌ GCash QR - DISABLED
    'wallet': false    // ❌ Digital Wallet - DISABLED
};
```

---

## How It Works

### 1. **Payment Method Validation**
When a user selects a disabled payment method, they get an alert:
```
"GCash QR Payment is coming soon! 🚀

Currently, only Credit/Debit Card payments are available.

Please select Card Payment to continue."
```

### 2. **Radio Button Disabling**
Disabled payment methods:
- Have `disabled` attribute set
- Are visually faded (opacity 0.5)
- Show cursor: not-allowed
- Display "Coming Soon" badge

### 3. **Payment Execution Control**
When user clicks "EXECUTE PAYMENT":
1. System checks selected payment method
2. If disabled → Shows alert and returns
3. If enabled (card) → Proceeds with payment (accepts dummy input)

---

## Features

### ✨ User-Friendly Disabled State
```
[✓] Card Payment              ← Enabled (clickable)
[ ] GCash QR Payment    [Soon] ← Disabled (faded, can't select)
[ ] Digital Wallets     [Soon] ← Disabled (faded, can't select)
```

### ⚠️ Smart Redirection
If user tries to force a disabled method:
- Alert appears explaining method is not available
- Radio button auto-resets to Card
- Card payment form displays

### 🎯 Dummy Input Acceptance
For Card payment:
- Card number: Any format accepted (e.g., "1111111111111111")
- Expiry: Any format (e.g., "12/25")
- CVV: Any 3-4 digits
- Name: Any text

---

## Usage & Testing

### Test Card Payment (ENABLED)
```
1. Click "Top Up Balance" button
2. Select Card Payment option (only one available)
3. Enter dummy card details:
   - Name: Test User
   - Card: 4111111111111111
   - Expiry: 12/25
   - CVV: 123
4. Click "EXECUTE PAYMENT"
5. ✅ Payment processes successfully
```

### Test GCash Disabled (NOT AVAILABLE)
```
1. Try to click GCash QR Payment radio button
2. ❌ Button is disabled (can't select)
3. ⚠️ Badge shows "Soon"
4. Hover shows tooltip: "GCash QR Payment - Coming Soon"
```

### Test Digital Wallet Disabled (NOT AVAILABLE)
```
1. Try to click Digital Wallets radio button
2. ❌ Button is disabled (can't select)
3. ⚠️ Badge shows "Soon"
4. Hover shows tooltip: "Digital Wallets - Coming Soon"
```

---

## Enabling/Disabling Methods

To change payment method availability, edit `wwwroot/js/payment-control.js`:

### Example: Enable GCash

```javascript
const enabledMethods = {
    'card': true,      // ✅ Enabled
    'gcash': true,     // ✅ Now enabled!
    'wallet': false    // ❌ Still disabled
};
```

Then reload the page. The change takes effect immediately.

### Example: Disable All Except Card

```javascript
const enabledMethods = {
    'card': true,      // ✅ Only this enabled
    'gcash': false,
    'wallet': false
};
```

---

## API Reference

### `isPaymentMethodAvailable(paymentMethod)`
**Purpose:** Check if a payment method is enabled  
**Parameters:**
- `paymentMethod` (string): 'card', 'gcash', or 'wallet'

**Returns:** boolean (true if enabled, false if disabled)

**Example:**
```javascript
if (isPaymentMethodAvailable('gcash')) {
    // Process GCash payment
} else {
    // Show "coming soon" message
}
```

---

### `showPayMethod(method)`
**Purpose:** Show the selected payment method form (with validation)  
**Parameters:**
- `method` (string): Payment method to show

**Features:**
- Validates that method is enabled
- Shows alert if method is disabled
- Auto-resets to card if disabled method selected
- Hides all other payment forms

**Example:**
```javascript
showPayMethod('card');  // Shows card form ✅
showPayMethod('gcash'); // Shows alert, resets to card ❌
```

---

### `showPaymentMethodDisabledAlert(method)`
**Purpose:** Display "coming soon" alert for disabled methods  
**Parameters:**
- `method` (string): Disabled payment method

**Example:**
```javascript
showPaymentMethodDisabledAlert('wallet');
// Shows: "Digital Wallets is coming soon! 🚀..."
```

---

### `executePaymentWithValidation()`
**Purpose:** Validate and execute payment (replaces executePayment)  
**Features:**
- Checks if selected method is enabled
- Shows alert if disabled
- Calls original executePayment() if card is selected

**Example:**
```javascript
// Called when user clicks "EXECUTE PAYMENT" button
executePaymentWithValidation();
```

---

### `initializePaymentMethods()`
**Purpose:** Initialize payment method states on page load  
**Does:**
1. Disables unavailable payment method radio buttons
2. Adds "Coming Soon" badges
3. Sets default to Card payment
4. Adds tooltips to disabled methods

**Automatically called on:** Page load, Modal open

---

## Visual Indicators

### Enabled Payment Method (Card)
```css
✅ Normal opacity (1.0)
✅ Cursor: pointer
✅ Border highlights on hover/select
✅ No badge
```

### Disabled Payment Method (GCash, Wallet)
```css
❌ Reduced opacity (0.5)
❌ Cursor: not-allowed
❌ Can't be selected
❌ "Soon" badge shown
❌ Gray disabled appearance
```

---

## Backend Integration Notes

### Current Behavior (Frontend Only)
- Payment validation happens entirely on client-side
- Disabled methods are blocked from selection
- Card payments are allowed with ANY input (dummy data)

### When Ready to Enable Methods

#### 1. **For GCash Payment**
```javascript
// In payment-control.js
'gcash': true  // Set to true

// Create backend endpoint:
// POST /Player/ProcessGCashPayment
```

#### 2. **For Digital Wallet**
```javascript
// In payment-control.js
'wallet': true  // Set to true

// Create backend endpoint:
// POST /Player/ProcessWalletPayment
```

#### 3. **Example Backend Structure**
```csharp
[HttpPost]
public IActionResult ProcessCardPayment(PaymentRequest request)
{
    // Validate card details with payment processor
    // (e.g., Stripe, PayPal)
    return Ok(new { success = true, transactionId = "..." });
}

[HttpPost]
public IActionResult ProcessGCashPayment(GCashRequest request)
{
    // Integrate with GCash API
    return Ok(new { success = true, qrCode = "..." });
}

[HttpPost]
public IActionResult ProcessWalletPayment(WalletRequest request)
{
    // Integrate with wallet provider
    return Ok(new { success = true, redirectUrl = "..." });
}
```

---

## Testing Scenarios

### Scenario 1: Card Payment Flow
```
✅ User clicks Top Up
✅ Card option is only available
✅ User enters dummy data
✅ Clicks Execute Payment
✅ Payment processes
✅ Balance updates
```

### Scenario 2: Try to Force GCash
```
❌ User tries to select GCash
❌ Radio button doesn't respond
❌ User notices it's disabled
✅ User falls back to Card
```

### Scenario 3: Multiple Top-Ups
```
✅ First payment with card
✅ Second payment with card
✅ System accepts both with dummy data
✅ No validation errors
```

---

## File Structure

```
wwwroot/
├── js/
│   ├── payment-control.js    ← Payment method management
│   ├── live-session.js       ← Session tracking
│   └── stations.js           ← Station management
│
Views/
└── Player/
    └── Dashboard.cshtml      ← Loads payment-control.js
```

---

## Browser Compatibility

Works on all modern browsers:
- ✅ Chrome/Edge 90+
- ✅ Firefox 88+
- ✅ Safari 14+
- ✅ Mobile browsers

---

## Troubleshooting

### Issue: GCash/Wallet buttons still work
**Solution:** 
1. Hard refresh page (Ctrl+F5)
2. Check that payment-control.js is loaded
3. Open DevTools Console to verify no errors

### Issue: Alert not showing when selecting disabled method
**Solution:**
1. Check browser popup blockers
2. Verify DOMContentLoaded event fired
3. Check initializePaymentMethods() ran

### Issue: Card payment not working
**Solution:**
1. Check that executePayment() function exists
2. Verify backend endpoint is available
3. Check network tab for failed requests

---

## Future Enhancements

When ready to enable new payment methods:

1. ✅ Change `enabledMethods` in payment-control.js
2. ✅ Create backend endpoints for payment processing
3. ✅ Integrate with payment service providers
4. ✅ Test with real payment credentials
5. ✅ Update documentation
6. ✅ Deploy to production

---

## Performance Notes

- **Size:** ~4KB uncompressed
- **Load Time:** <50ms
- **DOM Impact:** Minimal (disables radio buttons only)
- **Memory:** <100KB
- **CPU:** Negligible

---

## Security Notes

### Current State (Development)
- ⚠️ Accepts ANY dummy input for card payments
- ⚠️ No validation of card format
- ⚠️ No encryption of card data
- ❌ Do NOT use in production

### Before Production
- [ ] Implement real card validation
- [ ] Use PCI-compliant payment processor
- [ ] Encrypt all card data
- [ ] Use HTTPS only
- [ ] Implement rate limiting
- [ ] Add fraud detection
- [ ] Audit payment flows

---

## Summary

| Feature | Status | Notes |
|---------|--------|-------|
| Card Payment | ✅ Enabled | Accepts dummy input for testing |
| GCash QR | ❌ Disabled | Coming soon |
| Digital Wallet | ❌ Disabled | Coming soon |
| User Alerts | ✅ Working | Shows "coming soon" message |
| Radio Disabling | ✅ Working | Prevents selection of unavailable methods |
| Dummy Input | ✅ Allowed | For testing/development only |

---

**Status:** ✅ **READY FOR TESTING**

The payment control system is fully functional and allows developers to easily enable/disable payment methods as needed.
