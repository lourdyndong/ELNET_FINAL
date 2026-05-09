# Live Session Quick Test Guide

## How to Test the Live Session Panel

### Method 1: Browser Console

1. Open Player Dashboard
2. Press `F12` to open Developer Tools
3. Go to the **Console** tab
4. Run one of these commands:

```javascript
// Test premium PC (PC 1-10) - ₱25/hr
// Start session on PC 5 with 2 hours (120 minutes)
initializeSession(5, 120, 'Counter-Strike 2');

// Test standard PC (PC 11-40) - ₱20/hr  
// Start session on PC 25 with 1.5 hours (90 minutes)
initializeSession(25, 90, 'VALORANT');

// Test with 5 minutes for quick testing
initializeSession(1, 5, 'DOTA 2');
```

### What to Expect

1. **Live Session Card appears** with green border and header
2. **Values update**:
   - Game: Shows the game name you specified
   - Station: Shows formatted PC number (e.g., "PC 05")
   - Elapsed: Starts at 0h 00m and counts up
   - Remaining: Counts down from the paid time
   - Rate: Shows ₱25/hr for PC 1-10, ₱20/hr for PC 11-40

3. **Real-time updates**: All values update every second

### Testing Different Scenarios

#### Test 1: Premium PC Rate (₱25/hr)
```javascript
initializeSession(1, 60, 'Counter-Strike 2');
```
Expected Rate: ₱25 / hr

#### Test 2: Standard PC Rate (₱20/hr)
```javascript
initializeSession(40, 60, 'DOTA 2');
```
Expected Rate: ₱20 / hr

#### Test 3: Time Countdown
```javascript
// 5 minute session for quick visual testing
initializeSession(10, 5, 'Valorant');
```
Watch the "Remaining" time count down from 5h 00m

#### Test 4: End Session
```javascript
// After testing, hide the panel
endSession();
```
Expected: Live Session card disappears

### Verification Checklist

- [ ] Live Session card displays when initialized
- [ ] Game name shows correctly
- [ ] Station number formatted as "PC XX" (zero-padded)
- [ ] Rate shows ₱25/hr for PC 1-10
- [ ] Rate shows ₱20/hr for PC 11-40
- [ ] Elapsed time increments every second
- [ ] Remaining time decrements every second
- [ ] Time format is correct (HH:MM)
- [ ] Remaining time is amber/orange colored
- [ ] Panel hides when endSession() is called
- [ ] No console errors appear

### Browser DevTools Testing

1. Open Network tab to verify no errors
2. Open Console to check for JavaScript errors
3. Open Elements tab to inspect the card structure
4. Verify CSS is applied correctly

### Integration Testing

Once the panel works, integrate with your backend:

```javascript
// In Player Dashboard after successful login
document.addEventListener('DOMContentLoaded', async () => {
    // Check if player has active session
    const userId = new URLSearchParams(window.location.search).get('userId');
    if (userId) {
        try {
            const response = await fetch(`/Player/GetActiveSession?userId=${userId}`);
            if (response.ok) {
                const session = await response.json();
                if (session.isActive) {
                    initializeSession(
                        session.stationNumber,
                        session.remainingMinutes,
                        session.gameName
                    );
                }
            }
        } catch (error) {
            console.error('Error loading session:', error);
        }
    }
});
```

### Debugging Tips

If the panel doesn't appear:
1. Check console for errors: `Uncaught ReferenceError`
2. Verify script loaded: Check in Network tab for `live-session.js`
3. Check that `liveSessionCard` element exists in DOM
4. Ensure JavaScript is enabled in browser

If times aren't updating:
1. Verify timer interval started
2. Check browser console for any errors
3. Verify system clock is correct

### Performance Monitoring

Check browser performance:
1. Open DevTools → Performance tab
2. Start recording
3. Initialize session
4. Record for 10 seconds
5. Stop recording
6. Look for smooth 60fps updates

Expected: Minimal impact on performance, mostly idle

---

**Need Help?**
- Check LIVE_SESSION_GUIDE.md for detailed documentation
- Review live-session.js source code for implementation details
- Check the HTML structure in Player Dashboard view
