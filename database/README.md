# Database

This directory contains database-related files for the HRShiftManagement project.

## Structure

- **Design/**: Database design documents (ERD, schema diagrams)
- **Scripts/**: SQL utility scripts (indexes, stored procedures, views)
- **SeedData/**: Seed data SQL scripts or JSON files

## Database Management

Database migrations are managed via Entity Framework Core in the `HRShiftManagement.Infrastructure` project.

### Common Commands

```bash
# Add a new migration
dotnet ef migrations add <MigrationName> --project src/Backend/HRShiftManagement.Infrastructure --startup-project src/Backend/HRShiftManagement.API

# Update database
dotnet ef database update --project src/Backend/HRShiftManagement.Infrastructure --startup-project src/Backend/HRShiftManagement.API

# Remove last migration
dotnet ef migrations remove --project src/Backend/HRShiftManagement.Infrastructure --startup-project src/Backend/HRShiftManagement.API
```

## Connection String

The connection string is configured in `appsettings.json`. For sensitive credentials, use User Secrets or environment variables.

Database name: **HRShiftManagementDb**
