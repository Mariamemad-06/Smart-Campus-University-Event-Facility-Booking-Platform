# Smart Campus — University Event & Facility Booking Platform

A web-services-based web application designed to help university students and faculty members discover campus events, book study rooms and labs, register for workshops, and track their reservations. Built with a modular, service-oriented architecture where all administrative and user operations are exposed as reusable web services and consumed by a unified front-end application.

**Academic Context:** Faculty of Computer and Information Sciences, Ain Shams University & University of East London

---

## Table of Contents
- [System Architecture](#system-architecture)
- [Core Features](#core-features)
  - [User Services](#user-services)
  - [Admin Services](#admin-services)
  - [Bonus Features](#bonus-features)
- [Technology Stack](#technology-stack)
- [Getting Started](#getting-started)
- [Quality Attribute Evaluation](#quality-attribute-evaluation)
- [Team & Contributors](#team--contributors)

---

## System Architecture

The platform follows a decoupled **Service-Oriented / Web API Architecture**:
1. **Reusable Core Web Services (Backend):** Independent service modules exposing RESTful endpoints for business logic and data persistence.
   - **Auth Service:** Handles user registration, login, and role-based access control (`Student`, `Faculty`, `Admin`).
   - **Facility Catalog Service:** Manages campus rooms, labs, equipment, and availability statuses[cite: 1].
   - **Booking Engine Service:** Processes room reservations, workshop registrations, conflict detection, and cancellations[cite: 1].
   - **Notification Service:** Dispatches email and dashboard alerts for booking confirmations, reminders, and schedule updates[cite: 1].
2. **Application Composition (Frontend):** A single MVC web application that consumes the Admin and User web services to provide an interactive UI and calendar synchronization[cite: 1].
3. **Database Layer:** Relational data storage powered by **Microsoft SQL Server**[cite: 1].

---

## Core Features

### User Services
* **Authentication & Profiles:** Register and log in using Name, University ID / Email, Password, and Role (`Student` or `Faculty`)[cite: 1].
* **Search & Discovery:** Search and filter campus facilities and events by keyword, date, category, or building location[cite: 1].
* **Room & Event Booking:** Reserve study spaces/labs or register for campus workshops[cite: 1].
* **Reservation Management:** View active reservations, synchronize schedules with a calendar view, and cancel upcoming bookings[cite: 1].
* **Automated Notifications:** Receive email or in-app dashboard alerts for booking confirmations, reminders, and schedule changes[cite: 1].

### Admin Services
* **Facility & Equipment Management:** Add, edit, delete, and view campus rooms, labs, and equipment (including title, building/location, capacity, type, and `available`/`maintenance` status)[cite: 1].
* **Event & Workshop Management:** Add, edit, delete, and view campus events, schedules, categories, and speaker details[cite: 1].

### Bonus Features
* **Campus Assistant Chatbot:** Interactive assistant to handle room availability inquiries and event FAQs[cite: 1].
* **Modern Responsive UI:** Clean, accessible user interface tailored for desktop and mobile devices[cite: 1].

---

## Technology Stack

* **Backend / Web Services:** ASP.NET Core Web API / C#[cite: 1]
* **Frontend / Composition Layer:** ASP.NET Core MVC, HTML5, CSS3, JavaScript[cite: 1]
* **Database:** Microsoft SQL Server[cite: 1]
* **Documentation & Testing:** Swagger / OpenAPI, Postman

---

## Getting Started

### Prerequisites
* [.NET SDK](https://dotnet.microsoft.com/download) (v8.0 or later)
* [Microsoft SQL Server](https://www.microsoft.com/en-us/sql-server) & SQL Server Management Studio (SSMS)
* Visual Studio 2022 or VS Code

### Installation & Setup
1. **Clone the repository:**
   ```bash
   git clone [https://github.com/](https://github.com/)<your-username>/smart-campus-booking.git
   cd smart-campus-booking
