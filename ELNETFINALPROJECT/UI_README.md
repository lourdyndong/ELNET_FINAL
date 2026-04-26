# Smart Sit-In / Computer Lab Management System - Frontend UI

A modern, professional ASP.NET Core MVC dashboard UI for managing computer lab sit-in requests.

## 📁 Project Structure

```
ELNETFINALPROJECT/
├── Views/
│   ├── Shared/
│   │   ├── _StudentLayout.cshtml       # Main student layout
│   │   ├── _AdminLayout.cshtml         # Main admin layout
│   │   ├── _StudentSidebar.cshtml      # Student navigation sidebar
│   │   ├── _AdminSidebar.cshtml        # Admin navigation sidebar
│   │   ├── _StudentNavbar.cshtml       # Student top navbar
│   │   └── _AdminNavbar.cshtml         # Admin top navbar
│   ├── Student/
│   │   ├── Dashboard.cshtml            # Student dashboard
│   │   ├── RequestSitIn.cshtml         # Request form
│   │   ├── MyRequests.cshtml           # View student requests
│   │   ├── Notifications.cshtml        # Notifications page
│   │   └── Announcements.cshtml        # Announcements page
│   └── Admin/
│       ├── Dashboard.cshtml            # Admin dashboard
│       ├── ManageRequests.cshtml       # Request management
│       ├── ManageAnnouncements.cshtml  # Announcement management
│       ├── Labs.cshtml                 # Lab room management
│       ├── Users.cshtml                # User management
│       └── Reports.cshtml              # Reports generation
└── wwwroot/
    └── css/
        └── dashboard.css               # Custom styling
```

## 🎨 Design Features

### Layout
- **Fixed Left Sidebar** (250px): Blue gradient background with navigation menu
- **Top Navbar** (70px): Search bar, notifications, user profile
- **Main Content Area**: Flexible grid-based layout

### Color Scheme
- **Primary Blue**: #4a7fd7
- **Secondary Blue**: #357abd
- **Success Green**: #22c55e
- **Warning Yellow**: #eab308
- **Danger Red**: #ef4444
- **Light Gray**: #f8f9fa

### Components
- **Summary Cards**: Display key metrics
- **Data Tables**: Sortable/filterable request tables
- **Form Controls**: Input fields, dropdowns, date/time pickers
- **Status Badges**: Color-coded status indicators
- **Action Buttons**: Approve, reject, edit, delete actions
- **Notification List**: Icon + message + timestamp display
- **Announcement Cards**: Title + description + date display

## 📱 Responsive Design

- **Desktop (1200px+)**: Full layout with 2-column grids
- **Tablet (768px-1199px)**: Collapsed sidebars, single-column content
- **Mobile (< 768px)**: Minimal sidebar icons, full-width content

## 🔧 Technologies Used

- **ASP.NET Core MVC** (Razor Views)
- **Bootstrap 5** (Grid system, components)
- **Bootstrap Icons** (SVG icons)
- **Custom CSS** (Flexbox, Grid layouts)
- **JavaScript** (Client-side interactions)

## 📄 Page Descriptions

### Student Pages

**Dashboard**
- Welcome message with user name
- Summary cards (Total, Approved, Pending requests)
- Recent notifications list
- Quick action buttons
- Available labs status

**Request Sit-In**
- Form to submit new sit-in requests
- Room selection dropdown
- Date and time pickers
- Purpose text area
- Form validation

**My Requests**
- Table of all student requests
- Status indicators (Approved, Pending, Rejected)
- View/Cancel actions

**Notifications**
- Chronological list of notifications
- Color-coded notification types
- Timestamp for each notification
- Mark as read functionality

**Announcements**
- List of lab announcements
- Title, description, and posting date
- Icon indicators

### Admin Pages

**Dashboard**
- Summary cards (Total, Pending, Approved requests)
- Quick pending requests table with approve/reject actions

**Manage Requests**
- Full table of all student requests
- Status filter dropdown
- Approve/Reject buttons with confirmations
- Student name, room, date, time columns

**Manage Announcements**
- Create new announcement form
- List of all announcements
- Edit/Delete actions for each announcement

**Labs Management**
- Table of all lab rooms
- Room number, capacity, equipment, status
- Add new lab button
- Edit/Delete actions

**Users Management**
- Table of all users (students and admins)
- User role badge
- Active/Inactive status
- Add new user form
- Role filter dropdown
- Edit/Delete actions

**Reports**
- Report generation form (type, date range, format)
- List of recent generated reports
- Download/Delete actions

## 🚀 Getting Started

1. **Set your default layout** in `_ViewStart.cshtml`:
   ```csharp
   @{
       Layout = "_StudentLayout"; // or "_AdminLayout"
   }
   ```

2. **Create controllers** for Student and Admin:
   ```csharp
   public class StudentController : Controller
   {
       public IActionResult Dashboard() => View();
       public IActionResult RequestSitIn() => View();
       // ... other actions
   }
   ```

3. **Connect to backend** by replacing placeholder onclick handlers with form submissions

4. **Customize colors** by modifying CSS variables in `dashboard.css`:
   ```css
   :root {
       --primary-blue: #4a7fd7;
       // ... other colors
   }
   ```

## 📝 Notes

- All forms are frontend-only (no backend logic)
- JavaScript is used for basic interactions (dropdowns, form validation)
- Icons use Bootstrap Icons (CDN link in layout files)
- The design is fully responsive and mobile-friendly
- All styles are in `dashboard.css` for easy customization
- Table actions are placeholder functions - connect to backend endpoints

## 🎯 Next Steps

1. Create backend models and database
2. Implement controller actions with data access
3. Add form submission handlers
4. Implement authentication and authorization
5. Add data validation
6. Connect to real lab management database

---

**Enjoy your Smart Sit-In System!** 🎓
