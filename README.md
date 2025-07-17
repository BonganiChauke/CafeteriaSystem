# Cafeteria Credit & Ordering System

## Overview
Web application built with ASP.NET Core for managing a cafeteria system with:
- Admin portal for restaurant/menu/order management
- Employee portal for deposits and ordering
- SQL Server database with LocalDB
- ASP.NET Core Identity for authentication

## Features

### Admin Features
| Feature | Path |
|---------|------|
| Manage restaurants | `/Restaurants/Index` |
| Add menu items | `/Restaurants/CreateMenuItem` |
| View menu items | `/Restaurants/MenuItems` |
| Manage all orders | `/Orders/ManageOrders` |
| Manage employees | `/Employees/Index` |
| Register admins | `/Account/AdminRegister` |

### Employee Features
| Feature | Path |
|---------|------|
| Deposit funds | `/Employees/Deposit` |
| View history | `/Employees/DepositHistory` |
| View orders | `/Orders/MyOrders` |
| Place orders | `/Orders/Create` |

### Default Credentials
| Role | Email | Password |
|------|-------|----------|
| Admin | `admin@cafeteria.com` | `Admin123!` |
| Employee | `employee@cafeteria.com` | `Employee123!` |

## Setup Guide

### 1. Prerequisites
- [.NET SDK 8.0+](https://dotnet.microsoft.com/download)
- SQL Server LocalDB (comes with Visual Studio)
- [SSMS](https://aka.ms/ssmsfullsetup)
- Git

### 2. Database Configuration
**Connection String** (`appsettings.json`):
```json
"ConnectionStrings": {
  "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=CafeteriaSystem;Trusted_Connection=True;MultipleActiveResultSets=true"
}
```

# Install EF Core
dotnet tool install --global dotnet-ef --version 8.0.*

# Apply migrations
dotnet ef migrations add InitialCreate
dotnet ef database update

# Run application
dotnet run

-- Verify restaurants
SELECT * FROM Restaurants;

-- Check employee balance
SELECT Balance FROM Employees 
WHERE UserId = (SELECT Id FROM AspNetUsers WHERE Email = 'employee@cafeteria.com');
