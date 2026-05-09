# Live Session Panel - Visual Guide

## Live Session Card Layout

```
┌─────────────────────────────────────────────────────────────────┐
│ ▶ LIVE SESSION                                                  │
├──────────────────────────┬──────────────────────────────────────┤
│                          │                                      │
│  Game                    │  Elapsed              Rate            │
│  Counter-Strike 2        │  0h 45m               ₱25 / hr        │
│                          │                                      │
│  Station                 │  Remaining                           │
│  PC 05                   │  2h 15m                              │
│                          │                                      │
└──────────────────────────┴──────────────────────────────────────┘
```

## Color Scheme

```
Header Background:    rgba(0,255,136,0.06)  [Light Green]
Border:              rgba(0,255,136,0.25)  [Medium Green]

Text:
  - Labels:          rgba(255,255,255,0.4) [Gray]
  - Values:          #00ff88               [Bright Green]
  - Game:            #ffffff               [White]
  - Remaining:       #ffaa00               [Amber]

Animation:
  - Play Icon:       Pulse glow effect     [2s infinite]
```

## Component Hierarchy

```
Live Session Card (live-session-card)
├── Header (live-session-header)
│   ├── Icon: bi-play-circle-fill [animated]
│   └── Text: "LIVE SESSION"
├── Column 1 (Left)
│   ├── Game Row (session-item)
│   │   ├── Label: "Game"
│   │   └── Value: Counter-Strike 2 (session-value.game)
│   └── Station Row (session-item)
│       ├── Label: "Station"
│       └── Value: PC 05 (session-value.station)
├── Column 2 (Right)
│   ├── Elapsed Row (session-item)
│   │   ├── Label: "Elapsed"
│   │   └── Value: 0h 45m (session-value)
│   └── Rate Row (session-item)
│       ├── Label: "Rate"
│       └── Value: ₱25 / hr (session-value)
└── Remaining Row (Full Width, session-item)
    ├── Label: "Remaining"
    └── Value: 2h 15m (session-value.remaining) [Amber]
```

## State Diagram

```
                    ┌─────────────┐
                    │   Inactive  │
                    └──────┬──────┘
                           │
                    initializeSession()
                           │
                           ▼
                    ┌─────────────┐
                    │   Active    │◄────── Updates every 1s
        ┌──────────►│   Session   │
        │           └──────┬──────┘
        │                  │
        └──────────────────┘
             Timer runs
             Elapsed++
             Remaining--
                           │
                    Time reaches 0
                    OR endSession()
                           │
                           ▼
                    ┌─────────────┐
                    │   Ended     │
                    └──────┬──────┘
                           │
                    hideLiveSession()
                           │
                           ▼
                    Panel disappears
```

## Data Flow

```
User Action
    │
    ├─► initializeSession(stationNumber, paidMinutes, gameName)
    │   │
    │   ├─► Calculate rate based on PC number
    │   ├─► Set startTime = now()
    │   ├─► Show live-session-card element
    │   └─► Start session timer (1s interval)
    │
    ├─► [Timer Tick Every 1 Second]
    │   │
    │   ├─► Calculate elapsed = now() - startTime
    │   ├─► Calculate remaining = paidMinutes - elapsed
    │   ├─► Update DOM: sessionElapsed
    │   ├─► Update DOM: sessionRemaining
    │   └─► Check if remaining <= 0
    │
    ├─► User ends session or time expires
    │   │
    │   ├─► endSession()
    │   ├─► Clear timer interval
    │   └─► Hide live-session-card element
    │
    └─► Done
```

## Rate Calculation Logic

```
PC Number Input
    │
    ├─ Between 1 and 10? ─► Yes ─► Rate = ₱25/hr (Premium)
    │                              Display: ₱25 / hr
    │
    └─ Between 11 and 40? ─► Yes ─► Rate = ₱20/hr (Standard)
                                    Display: ₱20 / hr
```

## Time Update Example

```
Session Start: PC 5 (Premium, ₱25/hr) with 120 minutes paid

Second 0:
  - Elapsed: 0h 00m
  - Remaining: 2h 00m

Second 45:
  - Elapsed: 0h 00m (still < 1 minute)
  - Remaining: 1h 59m (45 seconds passed)

Second 60:
  - Elapsed: 0h 01m (1 minute passed)
  - Remaining: 1h 59m

Second 3600:
  - Elapsed: 1h 00m (1 hour passed)
  - Remaining: 1h 00m

Second 7200:
  - Elapsed: 2h 00m (2 hours passed)
  - Remaining: 0h 00m
  - ► endSession() triggered automatically
  - ► Panel hidden
  - ► Timer stopped
```

## CSS Grid Layout

```
┌──────────────────────────────────────┐
│ Header (grid-column: 1 / -1)         │  Spans full width
├──────────────┬──────────────────────┤
│ Column 1     │ Column 2             │  Each 1fr width
│ (auto)       │ (auto)               │
│              │                      │
│ • Game       │ • Elapsed            │  Flex column in each
│ • Station    │ • Rate               │
│              │                      │
├──────────────────────────────────────┤
│ Remaining (grid-column: 1 / -1)      │  Spans full width
└──────────────────────────────────────┘

Gap between elements: 20px (horizontal)
Padding: 20px (all sides)
```

## Example JavaScript Flow

```javascript
// 1. Initialize
initializeSession(5, 120, 'Counter-Strike 2');

// Session object becomes:
// {
//   isActive: true,
//   stationNumber: 5,
//   gameName: 'Counter-Strike 2',
//   startTime: Date (now),
//   remainingTime: 120,
//   rate: 25
// }

// 2. Every second, updateSessionDisplay() runs:
// - Calculates elapsed from startTime
// - Calculates remaining from elapsed
// - Formats as HH:MM
// - Updates DOM elements

// 3. After 120 minutes:
endSession();
// {
//   isActive: false,
//   stationNumber: null,
//   ... (reset)
// }
```

## HTML Generated

```html
<div class="live-session-card" id="liveSessionCard" style="display:none;">
    <div class="live-session-header">
        <i class="bi bi-play-circle-fill"></i>
        LIVE SESSION
    </div>
    
    <!-- Column 1 -->
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
    
    <!-- Column 2 -->
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
    
    <!-- Full Width -->
    <div class="session-item" style="grid-column: 1 / -1;">
        <span class="session-label">Remaining</span>
        <span class="session-value remaining" id="sessionRemaining">0h 00m</span>
    </div>
</div>
```

## DOM ID Reference

| ID | Purpose | Updated | Initial |
|---|---|---|---|
| `liveSessionCard` | Container | Display only | hidden |
| `sessionGame` | Game name | Every 1s | — |
| `sessionStation` | PC number | Once on init | — |
| `sessionElapsed` | Time elapsed | Every 1s | 0h 00m |
| `sessionRemaining` | Time left | Every 1s | 0h 00m |
| `sessionRate` | Hourly rate | Once on init | ₱25 / hr |

## File Structure

```
ELNETFINALPROJECT/
├── Views/
│   ├── Player/
│   │   └── Dashboard.cshtml          [Modified - Added panel + CSS]
│   └── Shared/
│       └── _Header.cshtml            [New - Shared header]
├── wwwroot/
│   └── js/
│       └── live-session.js           [New - Session management]
├── LIVE_SESSION_GUIDE.md             [New - Complete docs]
├── LIVE_SESSION_TEST.md              [New - Testing guide]
└── LIVE_SESSION_IMPLEMENTATION.md    [New - Implementation summary]
```

---

**Next Steps to Integrate:**
1. Create GameSessions database model
2. Create backend API endpoints for session management
3. Call `initializeSession()` when player starts gaming
4. Call `endSession()` when player logs off or time expires
5. Persist session data to database for history/billing
