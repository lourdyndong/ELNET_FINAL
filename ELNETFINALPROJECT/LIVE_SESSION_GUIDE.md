# Live Session Panel - Player Dashboard

## Overview
The Live Session panel displays real-time information about an active gaming session on the Player Dashboard. It shows game name, PC number, elapsed time, remaining time, and hourly rate.

## Features

### Dynamic Rate Calculation
- **PC 1-10**: ₱25 per hour (Premium PCs)
- **PC 11-40**: ₱20 per hour (Standard PCs)

### Display Information
- **Game**: Current game being played
- **Station**: PC number (formatted as "PC 01", "PC 02", etc.)
- **Elapsed**: Time spent in the session (HH:MM format)
- **Rate**: Hourly charge rate
- **Remaining**: Time left in the paid session (amber colored)

### Real-Time Updates
- Session timer updates every second
- Automatically counts down remaining time
- Ends session when time reaches 0

## JavaScript API

### Functions

#### `initializeSession(stationNumber, paidMinutes, gameName)`
Starts a new gaming session.

**Parameters:**
- `stationNumber` (number): PC number from 1-40
- `paidMinutes` (number): Total minutes paid for
- `gameName` (string, optional): Game name (default: "Counter-Strike 2")

**Example:**
```javascript
// Start session on PC 5 with 2 hours (120 minutes) paid time
initializeSession(5, 120, 'Counter-Strike 2');

// Start session on PC 15 with 1.5 hours (90 minutes)
initializeSession(15, 90, 'VALORANT');
```

#### `endSession()`
Stops the current session and hides the live session panel.

**Example:**
```javascript
// End the active session
endSession();
```

#### `showLiveSession()`
Shows the live session panel (visible when session is active).

#### `hideLiveSession()`
Hides the live session panel.

#### `updateSessionDisplay()`
Updates all session display values (called automatically every second).

#### `startSessionTimer()`
Starts the session update timer (called automatically by initializeSession).

#### `stopSessionTimer()`
Stops the session update timer (called automatically when session ends).

## CSS Classes

### `.live-session-card`
Main container for the live session panel
- Grid layout with 2 columns
- Green accent border
- Semi-transparent green background

### `.live-session-header`
Header section with "LIVE SESSION" text and animated play icon

### `.session-item`
Individual session information row with label and value

### `.session-label`
Label text (gray, uppercase)

### `.session-value`
Value display with monospace font and green color
- `.session-value.game`: White text, larger font
- `.session-value.station`: Extra large font
- `.session-value.remaining`: Amber/orange color for countdown

## HTML Structure

```html
<div class="live-session-card" id="liveSessionCard" style="display:none;">
    <div class="live-session-header">
        <i class="bi bi-play-circle-fill"></i> LIVE SESSION
    </div>
    <!-- Left column: Game and Station -->
    <div>
        <div class="session-item">
            <span class="session-label">Game</span>
            <span class="session-value game" id="sessionGame">—</span>
        </div>
        <div class="session-item">
            <span class="session-label">Station</span>
            <span class="session-value station" id="sessionStation">—</span>
        </div>
    </div>
    <!-- Right column: Elapsed and Rate -->
    <div>
        <div class="session-item">
            <span class="session-label">Elapsed</span>
            <span class="session-value" id="sessionElapsed">0h 00m</span>
        </div>
        <div class="session-item">
            <span class="session-label">Rate</span>
            <span class="session-value" id="sessionRate">₱25 / hr</span>
        </div>
    </div>
    <!-- Full width: Remaining Time -->
    <div class="session-item" style="grid-column: 1 / -1;">
        <span class="session-label">Remaining</span>
        <span class="session-value remaining" id="sessionRemaining">0h 00m</span>
    </div>
</div>
```

## Usage Example

### Complete Session Flow

```javascript
// 1. Initialize a session when player starts gaming
// Player books PC 7 for 3 hours (180 minutes) to play VALORANT
initializeSession(7, 180, 'VALORANT');

// Live Session Panel displays:
// Game: VALORANT
// Station: PC 07
// Elapsed: 0h 00m
// Rate: ₱25 / hr (PC 7 is premium)
// Remaining: 3h 00m

// 2. After 45 minutes, display shows:
// Elapsed: 0h 45m
// Remaining: 2h 15m

// 3. When time runs out or player logs off
endSession();
// Live Session Panel hides
```

### Integration with Server

To integrate with your backend:

```javascript
// Fetch active session from server
async function startPlayerSession() {
    try {
        const response = await fetch('/Player/GetActiveSession');
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
        console.error('Error fetching session:', error);
    }
}

// Call on page load
document.addEventListener('DOMContentLoaded', startPlayerSession);
```

## Styling Details

### Colors
- Header text: `var(--green)` (#00ff88)
- Card border: `rgba(0,255,136,0.25)`
- Card background: `rgba(0,255,136,0.06)`
- Label text: `rgba(255,255,255,0.4)`
- Value text: `var(--green)`
- Remaining time: `var(--amber)` (#ffaa00)

### Animation
- Play icon has a pulsing glow effect
- Animation: `pulse-glow 2s infinite`
- Opacity transitions from 1 to 0.5

## State Management

### Session Object
```javascript
let currentSession = {
    isActive: false,      // Is session currently active
    stationNumber: null,  // PC number (1-40)
    gameName: '',         // Game name
    startTime: null,      // Date object when session started
    remainingTime: 0,     // Minutes remaining
    rate: 25              // ₱ per hour
};
```

## Time Formatting

Times are displayed in **HH:MM** format:
- Hours: 0-23
- Minutes: 00-59 (zero-padded)

Examples:
- 0h 00m - just started
- 0h 45m - 45 minutes elapsed
- 1h 30m - 1 hour 30 minutes
- 23h 59m - almost 24 hours

## Error Handling

The system automatically:
- Ends session when remaining time reaches 0
- Handles missing DOM elements gracefully
- Clears intervals on session end to prevent memory leaks
- Logs errors to console

## Performance Notes

- Timer updates every 1 second (reasonable balance between accuracy and performance)
- Uses `setInterval` with proper cleanup via `clearInterval`
- Minimal DOM updates (only changed values are updated)
- No unnecessary calculations during display updates

## Browser Compatibility

Works on all modern browsers supporting:
- ES6 arrow functions
- Array methods (.padStart())
- setInterval/clearInterval
- CSS Grid Layout
