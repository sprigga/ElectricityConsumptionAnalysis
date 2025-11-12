# Power Analysis Application

## Overview

The Power Analysis Application is a web-based system designed to import, store, analyze and visualize electrical load readings. The application allows users to import electricity consumption data from Excel files, store it in a time-series format, and provides API endpoints for data retrieval and analysis.

## Purpose

This application is specifically designed for:
- Importing electrical load data from Excel spreadsheets (ElectricityConsumptionDifferenceTable.xlsx format)
- Storing time-series load readings in a database
- Providing API endpoints for load analysis and visualization
- Supporting chart-based data visualization for power consumption trends

## Architecture

### Technology Stack
- **Framework**: ASP.NET Core 8.0
- **Language**: C#
- **Database**: SQLite (default) or SQL Server
- **ORM**: Entity Framework Core
- **Excel Processing**: EPPlus library
- **Frontend**: MVC Views with charting capabilities

### Project Structure

```
PowerAnalysis/
├── Controllers/           # API and MVC controllers
│   ├── HomeController.cs
│   └── LoadReadingController.cs
├── Data/                 # Database context and migrations
│   ├── ApplicationDbContext.cs
│   ├── Migrations/
│   └── reference/        # Sample Excel files
├── Models/               # Data models
│   ├── LoadReading.cs
│   └── ErrorViewModel.cs
├── Repositories/         # Data access layer
│   ├── ILoadReadingRepository.cs
│   └── LoadReadingRepository.cs
├── Services/             # Business logic
│   ├── ILoadReadingImportService.cs
│   └── LoadReadingImportService.cs
├── appsettings.json      # Configuration files
├── Program.cs            # Application startup
├── Dockerfile           # Container deployment
└── ...
```

### Key Components

#### Models
- `LoadReading`: Core entity representing electrical load data with timestamp, load value, and metadata

#### Data Access Layer
- `ApplicationDbContext`: Entity Framework context using SQLite
- `ILoadReadingRepository` / `LoadReadingRepository`: Interface and implementation for database operations

#### Business Logic
- `ILoadReadingImportService` / `LoadReadingImportService`: Handles Excel file parsing and data import with validation

#### API Endpoints
- `GET /api/loadreading` - Retrieve all load readings
- `GET /api/loadreading/range` - Get load readings by date range
- `GET /api/loadreading/aggregated` - Get aggregated data for charts
- `GET /api/loadreading/count` - Get total record count
- `GET /api/loadreading/daterange` - Get date range of available data
- `POST /api/loadreading/import` - Import from default Excel file
- `POST /api/loadreading/import/custom` - Import from custom Excel file
- `POST /api/loadreading/validate` - Validate Excel file format
- `DELETE /api/loadreading/range` - Delete records by date range

#### MVC Views
- `HomeController`: Provides web interface with LoadReadingChart view for visualization

## Features

### Data Import
- Import electrical load data from Excel files in "ElectricityConsumptionDifferenceTable.xlsx" format
- Time-based data parsing with validation
- Support for different Excel sheet names and data sources
- Duplicate detection and handling

### Data Management
- Time-series storage of load readings
- Efficient querying with database indexing
- Date range operations
- Data aggregation for different time periods (hourly, daily, weekly)

### API Capabilities
- RESTful API endpoints for data access
- Date range filtering
- Aggregated data for charting
- Import and validation endpoints

## Database Schema

The application uses a single table `LoadReadings` with the following structure:

- **Id**: Primary key (integer)
- **Timestamp**: DateTime (with unique constraint)
- **LoadValue**: Decimal (18,3 precision)
- **DataSource**: String (max 100 chars)
- **ImportedAt**: DateTime (defaults to UTC now)
- **Remarks**: String (max 500 chars)

Database indexes are optimized for:
- Unique timestamp constraint
- Timestamp-based queries
- Data source filtering
- Combined timestamp and data source queries

## Configuration

### Database
The application supports both SQLite and SQL Server:
- Default: SQLite with "PowerAnalysis.db" file
- Configurable via "DefaultConnection" connection string

### Excel Import Format
Expected Excel format includes:
- First row: "Time" header followed by dates
- First column: Time values in "HH:mm" format
- Data cells: Load values corresponding to time and date

## Deployment

### Docker Deployment
The application includes a multi-stage Dockerfile:

```Dockerfile
# Uses .NET 8.0 SDK for build
# Multi-stage build process
# .NET 8.0 ASP.NET runtime in final stage
# Exposes ports 80 and 443
# Sets production environment
```

### Environment Variables
- `ASPNETCORE_URLS`: Default is http://+:80
- `ASPNETCORE_ENVIRONMENT`: Default is Production

### Build and Run
```bash
# Build the Docker image
docker build -t poweranalysis .

# Run the container
docker run -p 80:80 poweranalysis
```

## Development

### Prerequisites
- .NET 8.0 SDK
- Visual Studio or Visual Studio Code
- SQLite or SQL Server

### Running Locally
```bash
# Clone the repository
git clone <repository-url>

# Navigate to the project directory
cd PowerAnalysis

# Restore dependencies
dotnet restore

# Run the application
dotnet run
```

### Testing
The project includes a test import functionality:
- Console-based import testing via TestImport.cs
- API testing script (test-import-api.sh) for endpoint validation

## API Testing

A shell script is provided for API testing:
```bash
# Run the application first
dotnet run

# In another terminal, execute the test script
./test-import-api.sh
```

## Data Files

- Sample Excel file: `Data/reference/ElectricityConsumptionDifferenceTable.xlsx`
- SQLite database file: `PowerAnalysis.db`

## Security Considerations

- Connection strings can be configured via environment variables
- Input validation for Excel files
- Parameter validation for all API endpoints
- Proper error handling without exposing sensitive information

## Logging

- Comprehensive logging throughout the application
- Structured logging for import operations
- Error logging with context information
- Performance timing for import operations