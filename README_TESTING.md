# Testing Guide for PowerAnalysis

## Overview

This project now includes a comprehensive test suite with over 250+ automated tests covering:
- Service Layer (Excel import, parsing, validation)
- Repository Layer (database operations)
- API Controllers (all endpoints)
- Model Validation (entity constraints)

## Test Structure

```
PowerAnalysis.Tests/
├── Controllers/
│   └── LoadReadingControllerTests.cs      # API endpoint tests
├── Services/
│   └── LoadReadingImportServiceTests.cs   # Import service tests
├── Repositories/
│   └── LoadReadingRepositoryTests.cs      # Data access tests
├── Models/
│   └── LoadReadingTests.cs                # Model validation tests
├── Fixtures/
│   └── DatabaseFixture.cs                 # Test database setup
├── Helpers/
│   ├── TestDataGenerator.cs               # Generate test data
│   ├── ExcelTestHelper.cs                 # Create test Excel files
│   └── LoggerHelper.cs                    # Mock logger utilities
└── TestBase.cs                            # Base class for all tests
```

## Running Tests

### Run All Tests

```bash
dotnet test
```

### Run Tests with Detailed Output

```bash
dotnet test --verbosity detailed
```

### Run Tests for a Specific Project

```bash
dotnet test PowerAnalysis.Tests/PowerAnalysis.Tests.csproj
```

### Run Tests by Category/Filter

```bash
# Run only Service tests
dotnet test --filter "FullyQualifiedName~LoadReadingImportServiceTests"

# Run only Repository tests
dotnet test --filter "FullyQualifiedName~LoadReadingRepositoryTests"

# Run only Controller tests
dotnet test --filter "FullyQualifiedName~LoadReadingControllerTests"
```

## Code Coverage

### Generate Coverage Report Locally

```bash
# Run tests with coverage collection
dotnet test \
  --collect:"XPlat Code Coverage" \
  --results-directory ./coverage \
  --settings PowerAnalysis.Tests/coverlet.runsettings

# Install ReportGenerator (if not already installed)
dotnet tool install -g dotnet-reportgenerator-globaltool

# Generate HTML report
reportgenerator \
  -reports:"coverage/**/coverage.cobertura.xml" \
  -targetdir:"coveragereport" \
  -reporttypes:Html

# Open report in browser
open coveragereport/index.html  # macOS
xdg-open coveragereport/index.html  # Linux
start coveragereport/index.html  # Windows
```

### View Coverage in VS Code

1. Install the "Coverage Gutters" extension
2. Run tests with coverage
3. Click "Watch" in the status bar to display coverage in the editor

## Test Categories

### 1. Service Layer Tests (53 tests)

**LoadReadingImportServiceTests.cs**

Tests for Excel import functionality:
- Valid file import (happy path)
- File not found handling
- Invalid worksheet names
- Empty worksheets
- Invalid date/time/value formats
- Mixed valid/invalid data
- Large file imports
- Custom data sources
- Timestamp handling
- Performance metrics
- Excel format validation

**Key Test Methods:**
- `ImportFromExcelAsync_ValidFile_ImportsSuccessfully()`
- `ImportFromExcelAsync_InvalidDateFormats_SkipsInvalidRows()`
- `ValidateExcelFormat_ValidFile_ReturnsTrue()`

### 2. Repository Layer Tests (80 tests)

**LoadReadingRepositoryTests.cs**

Tests for database operations:
- CRUD operations (Create, Read, Update, Delete)
- Query operations (GetAll, GetByDateRange, GetByDataSource)
- Date range filtering
- Existence checks
- Batch operations
- Performance tests
- Edge cases (empty database, boundary values)

**Key Test Methods:**
- `GetByDateRangeAsync_ReturnsRecordsWithinRange()`
- `AddRangeAsync_LargeBatch_PerformsWell()`
- `DeleteByDateRangeAsync_DeletesAllInRange()`

### 3. Controller Layer Tests (60+ tests)

**LoadReadingControllerTests.cs**

Tests for API endpoints:
- GET /api/loadreading (all records)
- GET /api/loadreading/range (date range filter)
- GET /api/loadreading/aggregated (hourly/daily/weekly)
- GET /api/loadreading/count
- GET /api/loadreading/daterange
- POST /api/loadreading/import
- POST /api/loadreading/import/custom
- POST /api/loadreading/validate
- DELETE /api/loadreading/range

**Key Test Methods:**
- `GetAggregatedData_ReportMode_ReturnsRawData()`
- `GetAggregatedData_OneDayOrLess_ReturnsHourlyAverages()`
- `ImportFromDefaultExcel_Success_ReturnsOkWithResult()`

### 4. Model Validation Tests (40+ tests)

**LoadReadingTests.cs**

Tests for entity model:
- Property validation
- Data type constraints
- String length limits
- Decimal precision
- Database constraints (unique timestamp, auto-increment ID)
- CRUD operations at entity level
- Edge cases (min/max values, empty strings)

**Key Test Methods:**
- `LoadReading_Timestamp_MustBeUnique()`
- `LoadReading_LoadValue_Handles18_3Precision()`
- `LoadReading_DataSource_AcceptsLongStrings()`

## Test Data Generation

### Using TestDataGenerator

```csharp
// Generate a single LoadReading
var reading = TestDataGenerator.GenerateLoadReading(
    timestamp: new DateTime(2024, 1, 1),
    loadValue: 123.45m,
    dataSource: "Test Source");

// Generate multiple readings
var readings = TestDataGenerator.GenerateLoadReadings(
    count: 100,
    startDate: new DateTime(2024, 1, 1),
    interval: TimeSpan.FromMinutes(30));

// Generate readings for a date range
var readings = TestDataGenerator.GenerateLoadReadingsForDateRange(
    new DateTime(2024, 1, 1),
    new DateTime(2024, 1, 31));
```

### Using ExcelTestHelper

```csharp
// Create a valid test Excel file
var filePath = ExcelTestHelper.CreateValidExcelFile(
    "test.xlsx",
    startDate: new DateTime(2024, 1, 1),
    daysCount: 30);

// Create an Excel file with invalid data
var invalidFile = ExcelTestHelper.CreateExcelFileWithInvalidDates("invalid.xlsx");

// Clean up test files
ExcelTestHelper.CleanupTestFile(filePath);
```

## CI/CD Integration

Tests run automatically on:
- Push to `main`, `develop`, or `claude/**` branches
- Pull requests to `main` or `develop`

### GitHub Actions Workflow

The `.github/workflows/test-and-coverage.yml` workflow:
1. Restores dependencies
2. Builds the project
3. Runs all tests with coverage
4. Generates HTML coverage report
5. Uploads coverage artifacts
6. Comments on PRs with coverage results
7. Checks coverage threshold (70% minimum)

### Viewing Coverage in CI/CD

1. Navigate to the Actions tab in GitHub
2. Click on the workflow run
3. Download the "coverage-report" artifact
4. Extract and open `index.html`

## Best Practices

### Writing New Tests

1. **Follow AAA Pattern**: Arrange, Act, Assert
   ```csharp
   [Fact]
   public async Task TestMethod_Scenario_ExpectedBehavior()
   {
       // Arrange
       var input = ...;

       // Act
       var result = await _service.DoSomething(input);

       // Assert
       result.Should().Be(expected);
   }
   ```

2. **Use FluentAssertions** for readable assertions
   ```csharp
   result.Should().NotBeNull();
   result.Count.Should().BeGreaterThan(0);
   result.Should().AllSatisfy(r => r.IsValid.Should().BeTrue());
   ```

3. **Use Theory for parameterized tests**
   ```csharp
   [Theory]
   [InlineData(1, 10)]
   [InlineData(7, 168)]
   [InlineData(30, 720)]
   public async Task Test_WithDifferentInputs(int days, int expectedHours)
   {
       // Test implementation
   }
   ```

4. **Mock external dependencies**
   ```csharp
   var mockRepo = new Mock<ILoadReadingRepository>();
   mockRepo.Setup(r => r.GetAllAsync())
       .ReturnsAsync(testData);
   ```

5. **Test both happy path and error cases**
   ```csharp
   [Fact]
   public async Task HappyPath_Test() { }

   [Fact]
   public async Task ErrorCase_Test() { }
   ```

### Naming Conventions

- Test class: `{ClassName}Tests`
- Test method: `{MethodName}_{Scenario}_{ExpectedBehavior}`

Examples:
- `GetAllAsync_EmptyDatabase_ReturnsEmptyCollection`
- `ImportFromExcelAsync_InvalidFile_ReturnsFailure`

### Test Organization

- Group related tests using `#region` directives
- One test class per production class
- Keep tests focused and independent
- Clean up resources in Dispose() methods

## Coverage Goals

| Component | Target Coverage | Current Status |
|-----------|----------------|----------------|
| Services | 90%+ | ✅ Implemented |
| Repositories | 85%+ | ✅ Implemented |
| Controllers | 80%+ | ✅ Implemented |
| Models | 75%+ | ✅ Implemented |
| **Overall** | **80%+** | 🎯 Target |

## Troubleshooting

### Tests Fail Locally But Pass in CI

- Check .NET SDK version matches CI (8.0)
- Ensure all dependencies are restored
- Clear bin/obj folders and rebuild

### Coverage Report Not Generating

- Verify coverlet.collector package is installed
- Check runsettings file path is correct
- Ensure reportgenerator tool is installed

### In-Memory Database Issues

- Each test gets a fresh database context
- Unique constraint violations may behave differently
- Use `CreateFreshContext()` for isolated tests

## Additional Resources

- [xUnit Documentation](https://xunit.net/)
- [FluentAssertions Documentation](https://fluentassertions.com/)
- [Moq Documentation](https://github.com/moq/moq4)
- [Coverlet Documentation](https://github.com/coverlet-coverage/coverlet)
- [ReportGenerator Documentation](https://github.com/danielpalme/ReportGenerator)

## Test Metrics Summary

```
Total Tests: 233+
├── Service Tests: 53
├── Repository Tests: 80
├── Controller Tests: 60
└── Model Tests: 40

Test Frameworks:
├── xUnit 2.9.2
├── Moq 4.20.72
├── FluentAssertions 6.12.1
├── Bogus 35.6.1 (test data generation)
└── Coverlet 6.0.2 (code coverage)

Coverage Configuration:
├── Format: Cobertura, OpenCover, JSON
├── Excludes: Migrations, Tests, obj folder
├── Threshold: 70% (warning)
└── Reports: HTML, Badges, Markdown
```

## Running Specific Test Scenarios

### Test Excel Import

```bash
dotnet test --filter "FullyQualifiedName~ImportFromExcelAsync"
```

### Test Date Range Queries

```bash
dotnet test --filter "FullyQualifiedName~DateRange"
```

### Test Aggregation Logic

```bash
dotnet test --filter "FullyQualifiedName~Aggregated"
```

### Performance Tests Only

```bash
dotnet test --filter "FullyQualifiedName~Performance"
```

## Contributing

When adding new features:
1. Write tests first (TDD approach)
2. Ensure all existing tests pass
3. Maintain or improve code coverage
4. Update this documentation if needed
5. Run coverage report before committing

---

**Last Updated:** 2025-11-14
**Test Coverage Target:** 80%+
**Total Test Count:** 233+
