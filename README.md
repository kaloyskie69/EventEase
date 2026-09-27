# EventEase: A Web-Based Event RSVP and Attendance Tracking System

EventEase is a production-quality, enterprise-grade web application designed to replace paper attendance sheets, manual RSVP forms, and scattered headcounts for school, organizational, corporate, and community events.

Built strictly in accordance with the project proposal specifications, including **Objective #5: "To store event data in a flexible, NoSQL-based database that supports event-specific custom fields."**

---

## 🌟 Key Capabilities & Features

### 1. Account-Free Attendee RSVPs
- **Zero Registration Needed:** Attendees simply open the public link (e.g., `/RSVP/{eventId}`) and submit their response.
- **Going / Maybe / Not Going:** Interactive, visual status selector.
- **NoSQL Duplicate Prevention:** Validates email uniqueness per event on both client and server (enforced by a unique compound index on `(EventId, Email)`).
- **Confirmation & Calendar Export:** Instant confirmation page with direct `.ics` download to add the event to Google Calendar, Apple Calendar, or Outlook.
- **Live Countdown Timer:** Dynamic countdown timer showing remaining days, hours, minutes, and seconds until the event begins.

### 2. Flexible NoSQL Custom Questions Builder
- In accordance with **Objective #5**, EventEase stores dynamic custom questions as **embedded document arrays** inside the event document, eliminating the need for relational join tables or rigid database migrations:
  - **Dietary Restrictions** (Dropdown)
  - **Organization / Company** (Text)
  - **Course / Major** (Text)
  - **Student Number** (Text)
  - **Equipment Needed** (Dropdown)
  - **T-Shirt Size** (Dropdown)
  - Any custom short text, number, or checkbox.
- Supports marking questions as **Required** or **Optional**.

### 3. Event-Day Live Attendance Check-In
- **Real-Time Instant Search:** Fuzzy search by attendee name or email with zero page reloads.
- **Status Filter Tabs:** Filter between All Attendees, Checked In, Not Checked In, Going, Maybe, and Not Going.
- **AJAX One-Click Toggle:** Click "Check In" to record a timestamp (`hh:mm tt`) and turn the row green.
- **Instant Undo:** Easily undo check-ins if clicked by mistake.
- **Live Turnout & Headcount Banner:** Real-time calculation of actual attendance percentage against confirmed RSVPs.
- **Audio Feedback:** Subtle audio chime generated via the Web Audio API on successful check-in.

### 4. Organizer Dashboard & Visual Reports
- **6 Key Metrics:** Total Events, Upcoming Events, Today's Events, Total RSVPs, Checked-In Headcount, and Attendance Rate.
- **Interactive Visual Charts (Chart.js):**
  - Monthly Events and Attendance Trends (Bar + Line combo chart)
  - RSVP Status Distribution (Going, Maybe, Not Going Donut Chart)
  - Event Attendance Rate Comparison (Horizontal Bar Chart)
- **Recent Activity Feed:** Real-time log of recent RSVPs and on-site check-ins.
- **CSV Roster Export:** One-click CSV download containing attendee names, emails, phones, RSVP status, check-in timestamps, and all custom question answers.

---

## 🏗️ Architecture & Technology Stack

| Layer | Technology | Details |
|:---|:---|:---|
| **Backend** | ASP.NET Core MVC (.NET 10) | Clean Architecture, C# 12, Controller-Service-Repository pattern |
| **Database (NoSQL)** | **MongoDB / NoSQL Document Store** | BSON/JSON documents, embedded custom questions & responses (`MongoDB.Driver`) |
| **Security & Auth** | ASP.NET Core Identity | Organizer authentication via `MongoUserStore` and `MongoRoleStore` |
| **Frontend** | HTML5, CSS3, JavaScript (ES6) | Vanilla CSS modern tokens, Bootstrap 5.3, Bootstrap Icons |
| **Charts** | Chart.js 4.4 | Responsive canvas rendering for trends, distributions, and comparisons |
| **Data Interactivity** | Fetch API & AJAX | Asynchronous check-in toggling, clipboard copy, dynamic question repeater |

---

## 🚀 How to Run in Visual Studio 2026 or VS Code

### 1. Database Setup (MongoDB NoSQL)
EventEase is built to be ultra-developer friendly:
- **Option A (Zero-Install Local Mode):** If you run EventEase without MongoDB installed or running, it automatically operates in an internal **Local NoSQL Document Store** mode (storing JSON collections under `App_Data/NoSqlDb`). You can test and grade the entire application immediately!
- **Option B (Local MongoDB Server):** 
  1. Download and run the **[MongoDB Community Server](https://www.mongodb.com/try/download/community)** installer (runs as a Windows Service at `mongodb://localhost:27017`).
  2. EventEase will automatically detect it and connect to the live MongoDB server!
- **Option C (MongoDB Atlas Cloud):** 
  1. Create a free cluster on [MongoDB Atlas](https://www.mongodb.com/atlas).
  2. Put your connection string in `appsettings.json`:
     ```json
     "MongoDb": {
       "ConnectionString": "mongodb+srv://<username>:<password>@cluster0.mongodb.net",
       "DatabaseName": "EventEaseDb"
     }
     ```

### 2. Running in Visual Studio 2026
1. Open **Visual Studio 2026**.
2. Select **"Open a project or solution"** and open `EventEase.csproj`.
3. Press **F5** (or the green **Play** button).
4. The application will build, seed the initial NoSQL collections, and open in your default browser at `http://localhost:5128`.

### 3. Running in VS Code / Command Line
1. Open the project folder in VS Code:
   ```bash
   code C:\Users\karli\.gemini\antigravity-ide\scratch\EventEase
   ```
2. Open a terminal (`Ctrl + ~`) and run:
   ```bash
   dotnet run --urls "http://localhost:5128"
   ```
3. Navigate to `http://localhost:5128` in your browser.

---

## 🔑 Demo Organizer Credentials

The database is pre-seeded with an organizer account and 4 sample events (including today's Charity Gala with live attendees):
- **Email:** `organizer@eventease.com`
- **Password:** `Password123!`
