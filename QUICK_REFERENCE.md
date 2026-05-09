# Player Dashboard - Quick Reference Guide

## Database Initialization

The database is automatically created on first run using `EnsureCreated()` in `Program.cs`. The SQLite database file is located at `accounts.db`.

### Account Fields
```sql
CREATE TABLE Accounts (
    Id INTEGER PRIMARY KEY,
    Email TEXT NOT NULL,
    Username TEXT NOT NULL,
    Password TEXT NOT NULL,
    Balance DECIMAL NOT NULL,
    Role TEXT NOT NULL,
    Status TEXT NOT NULL DEFAULT 'Offline',
    RegisteredDate DATETIME NOT NULL,
    ProfilePicture BLOB,
    DisplayName TEXT,
    LastLogin DATETIME,
    IsVerified INTEGER NOT NULL DEFAULT 0
);
```

## API Usage Examples

### 1. Get Profile Data
```javascript
fetch('/Player/GetProfileData')
    .then(r => r.json())
    .then(data => console.log(data));
```

### 2. Update Profile Picture
```javascript
const formData = new FormData();
formData.append('profilePicture', fileInput.files[0]);

fetch('/Player/UpdateProfile', {
    method: 'POST',
    body: formData
})
.then(r => r.json())
.then(data => console.log(data));
```

### 3. Update Password
```javascript
fetch('/Player/UpdateCredentials', {
    method: 'POST',
    headers: {'Content-Type': 'application/x-www-form-urlencoded'},
    body: new URLSearchParams({
        'currentPassword': currentPass,
        'newPassword': newPass,
        'confirmPassword': confirmPass
    })
})
.then(r => r.json())
.then(data => console.log(data));
```

### 4. Top-Up Balance
```javascript
fetch('/Player/TopUpBalance', {
    method: 'POST',
    headers: {'Content-Type': 'application/x-www-form-urlencoded'},
    body: new URLSearchParams({
        'amount': 100
    })
})
.then(r => r.json())
.then(data => console.log(data));
```

## Session Variables

After successful login, the following session variables are set:
- `UserId` (int): Player ID for database queries
- `UserEmail` (string): Player email
- `UserName` (string): Player username
- `Role` (string): "Player"

## Error Handling

All endpoints return JSON with this format:
```json
{
    "success": true/false,
    "message": "Success or error message",
    "newBalance": 1500 // (for TopUpBalance only)
}
```

## Security Notes

1. **Passwords are hashed** using SHA256 before storage
2. **Sessions are server-side** validated
3. **File uploads are validated** for size and type
4. **All inputs are sanitized** by Entity Framework

## Development Workflow

1. Make code changes
2. Test functionality
3. Commit with `feat: description` prefix
4. Push to remote repository

Example:
```bash
git add .
git commit -m "feat: add email verification"
git push origin rasil
```

## Database Maintenance

### View All Players
```csharp
var players = _context.Accounts
    .Where(a => a.Role == "Player")
    .ToList();
```

### Update Player Balance (Admin)
```csharp
var player = _context.Accounts.Find(userId);
player.Balance = 1000;
_context.SaveChanges();
```

### Reset Player Password (Admin)
```csharp
var player = _context.Accounts.Find(userId);
player.Password = PasswordHelper.Hash("newPassword123");
_context.SaveChanges();
```

## Troubleshooting

### Profile Picture Not Loading
- Check file size (max 5MB)
- Verify file type (JPEG, PNG, GIF, WebP)
- Check browser console for errors

### Password Change Fails
- Verify current password is correct
- Ensure new password is at least 6 characters
- Check that new passwords match

### Top-Up Not Processing
- Verify amount is greater than 0
- Check user session is valid
- Review server logs for errors

### Database Not Updating
- Ensure `_context.SaveChanges()` is called
- Check database connection string in `Program.cs`
- Verify user has database write permissions
