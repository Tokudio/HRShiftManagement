# HRShiftManagement

A Human Resource and Shift Management System for supermarkets.

## Overview

HRShiftManagement is a software project designed to manage employees, work shifts, and related human resource operations in a supermarket environment.

The project uses a layered backend architecture and a separate frontend application.

## Technology Stack

### Frontend
- Angular
- TypeScript
- SCSS

### Backend
- ASP.NET Core Web API
- C#
- Entity Framework Core
- JWT Authentication

### Database
- Microsoft SQL Server

### Testing
- xUnit
- Automated testing for backend components

## Architecture

The backend follows Clean Architecture principles.

```text
HRShiftManagement/
├── src/
│   ├── Backend/
│   │   ├── HRShiftManagement.API/
│   │   ├── HRShiftManagement.Application/
│   │   ├── HRShiftManagement.Domain/
│   │   └── HRShiftManagement.Infrastructure/
│   └── Frontend/
│       └── hr-shift-management-ui/
├── database/
│   ├── Design/
│   ├── Scripts/
│   └── SeedData/
├── docs/
│   ├── API/
│   ├── Architecture/
│   ├── Database/
│   ├── Deployment/
│   ├── Requirements/
│   └── Testing/
├── tests/
│   ├── HRShiftManagement.API.Tests/
│   ├── HRShiftManagement.Application.Tests/
│   ├── HRShiftManagement.Domain.Tests/
│   └── HRShiftManagement.Infrastructure.Tests/
├── HRShiftManagement.sln
├── .gitignore
├── README.md
└── LICENSE
```

## Getting Started

### Prerequisites

- .NET SDK compatible with the backend target framework
- Node.js and npm
- Microsoft SQL Server
- Angular CLI (optional, as it can also be invoked through `npx`)

### Backend

From the repository root, build the solution:

```powershell
dotnet restore
dotnet build
dotnet test
```

### Frontend

Navigate to the Angular project:

```powershell
cd src/Frontend/hr-shift-management-ui
npm install
npm start
```

The development server is available at `http://localhost:4200` by default.

## Project Status

The initial solution structure and Angular frontend have been created. Backend features, database integration, authentication, and automated tests will be developed incrementally.

## License

See the [LICENSE](LICENSE) file.