Airport Ticket Booking System
==============================

A .NET console application that lets **passengers** search & book flights and lets a **manager** review bookings and import flights in bulk.  
Data is stored on the **file system** (CSV).

Objective
---------
Build a maintainable console app where passengers can book/modify/cancel tickets and managers can filter bookings and import flight data with validation.

Features
--------
### For Passengers
- **Search Flights** by: Price, Departure Country, Destination Country, Departure Date, Departure Airport, Arrival Airport, Class.  
  (Only flights with available seats are returned.)
- **Book a Flight** and choose a **class** (Economy/Business/First) with class‑specific pricing.
- **Manage Bookings**
  - View personal bookings
  - Cancel a booking (refund + seat restored)
  - Modify a booking (change class, handles price deltas and seat reallocation)
- **Balance**
  - View current balance
  - Deposit funds

### For Managers
- **Filter Bookings** by passenger **first/last name**, flight **number**,
  and other flight route/date/class/price filters.
- **Batch Flight Upload** from **CSV** (see `Seed/Imports/FlightsFromManager.csv`).
- **Model‑level Validation** on import (required fields, ranges, etc.).
- **Dynamic Validation Hints**: when import errors occur, the app prints the allowed constraints for each field (pulled from model attributes).

Tech
----
- .NET 8 (C#)
- Storage: CSV files
- Clean architecture:
  - **Domain**: Entities, Enums, Interfaces, Services
  - **Data**: CSV Repositories
  - **API**: Console UI (menus, prompts, renderers)
  - **Validation**: attribute‑driven, with a doc‑style constraint printer

Project Structure
-----------------
```
/AirportTicketBookingSystem
  └── ATBS
      ├── API                         # UI layer
      │   ├── ConsoleUI               # Menus, prompts (input), and renderers (output)
      │   └── Validation              # Utilities that print validation constraints
      ├── Data                        # Data layer
      │   └── Repositories            # CSV-backed repositories (CRUD over CSV files)
      ├── Domain                      # Domain/Business layer
      │   ├── Attributes              # Custom attributes
      │   ├── Entities                # Domain models (Flight, Passenger, Booking, FlightClass)
      │   ├── Enums                   # Enumerations (TravelClass, BookingStatus)
      │   ├── Interfaces              # Contracts for repos/services
      │   └── Services                # Application logic
      ├── Seed                        # CSV-based "database" used by the app at startup
      │   └── Imports                 # CSVs used **only** when Manager uploads flights
      ├── Utils                       # Helper Classes
      └── Program.cs                  # Program entrypoint
```

Getting Started
---------------
1) **Clone**
```
git clone https://github.com/Mohammad-Mans/airport-ticket-booking-system.git
cd AirportTicketBookingSystem
```
2) **Run**
```
dotnet run --project ATBS
```
> Requires .NET 8 SDK (or later).

Data & Import Notes
-------------------
- The **Seed** folder acts like a simple CSV **database** for the app. Repositories read from these files at startup.
- The **Seed/Imports** folder is **not** part of the initial database; it holds CSVs that the **Manager** can import via the “Upload Flights from CSV” option.
- The Manager menu’s upload reads the file path configured in `Utils/FilePaths.cs`. Point it to any CSV you want to import.

How to Use
----------
- **Passenger Menu**
  1. Search flights using any combination of filters.
  2. Pick a flight, choose a class, confirm booking.
  3. View bookings, you can **cancel** or **modify class** (upgrade/downgrade handles seat and balance).
  4. Check **balance** and **deposit** funds when needed.

- **Manager Menu**
  1. Filter bookings by passenger name and/or flight number, with optional route/date/class/price filters.
  2. Upload flights from CSV; if there are errors, you’ll see a list of row‑level issues **plus** a generated
     cheat‑sheet of validation constraints for each field.

Design Highlights
-----------------
- **Repositories** are file‑backed and focused on persistence only.
- **Services** encapsulate business rules.
- **Console UI** is de‑duplicated via `ConsolePrompts` (input) and `ConsoleRenderer` (output).

Sample Menu Flow
----------------
```
=== Main Menu ===
1) Passenger
2) Manager
0) Exit

=== Passenger Menu ===
1) Search Flights
2) Book a Flight
3) View My Bookings
4) Cancel Booking
5) Modify Booking
6) View/Deposit Balance
7) Logout

=== Manager Menu ===
1) Filter Bookings
2) Upload Flights from CSV
0) Back
```

:stars: Acknowledgment
------------
Special thanks to [**Foothill Technology Solutions**](https://www.foothillsolutions.com/) for the opportunity to work on this project during my internship. The experience and knowledge gained have been invaluable.
