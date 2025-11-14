using PowerAnalysis.Models;
using PowerAnalysis.Repositories;
using PowerAnalysis.Tests.Helpers;

namespace PowerAnalysis.Tests.Repositories;

/// <summary>
/// Comprehensive tests for LoadReadingRepository
/// </summary>
public class LoadReadingRepositoryTests : TestBase
{
    private readonly ILoadReadingRepository _repository;

    public LoadReadingRepositoryTests()
    {
        _repository = new LoadReadingRepository(DbContext);
    }

    #region GetAllAsync Tests

    [Fact]
    public async Task GetAllAsync_EmptyDatabase_ReturnsEmptyCollection()
    {
        // Act
        var result = await _repository.GetAllAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllAsync_WithData_ReturnsAllRecords()
    {
        // Arrange
        var readings = TestDataGenerator.GenerateLoadReadings(10);
        await _repository.AddRangeAsync(readings);
        await _repository.SaveChangesAsync();

        // Act
        var result = await _repository.GetAllAsync();

        // Assert
        result.Should().HaveCount(10);
    }

    [Fact]
    public async Task GetAllAsync_OrdersByTimestamp()
    {
        // Arrange
        var readings = new List<LoadReading>
        {
            TestDataGenerator.GenerateLoadReading(new DateTime(2024, 1, 3)),
            TestDataGenerator.GenerateLoadReading(new DateTime(2024, 1, 1)),
            TestDataGenerator.GenerateLoadReading(new DateTime(2024, 1, 2))
        };
        await _repository.AddRangeAsync(readings);
        await _repository.SaveChangesAsync();

        // Act
        var result = (await _repository.GetAllAsync()).ToList();

        // Assert
        result.Should().BeInAscendingOrder(r => r.Timestamp);
        result[0].Timestamp.Should().Be(new DateTime(2024, 1, 1));
        result[1].Timestamp.Should().Be(new DateTime(2024, 1, 2));
        result[2].Timestamp.Should().Be(new DateTime(2024, 1, 3));
    }

    [Fact]
    public async Task GetAllAsync_LargeDataset_PerformsWell()
    {
        // Arrange
        var readings = TestDataGenerator.GenerateLoadReadings(1000);
        await _repository.AddRangeAsync(readings);
        await _repository.SaveChangesAsync();

        // Act
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var result = await _repository.GetAllAsync();
        stopwatch.Stop();

        // Assert
        result.Should().HaveCount(1000);
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(2000); // Should complete in less than 2 seconds
    }

    [Fact]
    public async Task GetAllAsync_IncludesAllProperties()
    {
        // Arrange
        var reading = TestDataGenerator.GenerateLoadReading(
            timestamp: new DateTime(2024, 1, 1, 12, 30, 0),
            loadValue: 123.45m,
            dataSource: "Test Source",
            remarks: "Test Remarks");
        await _repository.AddAsync(reading);
        await _repository.SaveChangesAsync();

        // Act
        var result = (await _repository.GetAllAsync()).First();

        // Assert
        result.Timestamp.Should().Be(new DateTime(2024, 1, 1, 12, 30, 0));
        result.LoadValue.Should().Be(123.45m);
        result.DataSource.Should().Be("Test Source");
        result.Remarks.Should().Be("Test Remarks");
    }

    #endregion

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_ValidId_ReturnsEntity()
    {
        // Arrange
        var reading = TestDataGenerator.GenerateLoadReading();
        await _repository.AddAsync(reading);
        await _repository.SaveChangesAsync();

        // Act
        var result = await _repository.GetByIdAsync(reading.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(reading.Id);
        result.Timestamp.Should().Be(reading.Timestamp);
    }

    [Fact]
    public async Task GetByIdAsync_InvalidId_ReturnsNull()
    {
        // Act
        var result = await _repository.GetByIdAsync(99999);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsync_DeletedEntity_ReturnsNull()
    {
        // Arrange
        var reading = TestDataGenerator.GenerateLoadReading();
        await _repository.AddAsync(reading);
        await _repository.SaveChangesAsync();
        var id = reading.Id;

        await _repository.DeleteAsync(id);
        await _repository.SaveChangesAsync();

        // Act
        var result = await _repository.GetByIdAsync(id);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsCorrectEntity()
    {
        // Arrange
        var readings = TestDataGenerator.GenerateLoadReadings(5);
        await _repository.AddRangeAsync(readings);
        await _repository.SaveChangesAsync();

        var targetReading = readings[2];

        // Act
        var result = await _repository.GetByIdAsync(targetReading.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(targetReading.Id);
        result.Timestamp.Should().Be(targetReading.Timestamp);
        result.LoadValue.Should().Be(targetReading.LoadValue);
    }

    #endregion

    #region GetByDateRangeAsync Tests

    [Fact]
    public async Task GetByDateRangeAsync_ReturnsRecordsWithinRange()
    {
        // Arrange
        var startDate = new DateTime(2024, 1, 1);
        var readings = new List<LoadReading>
        {
            TestDataGenerator.GenerateLoadReading(new DateTime(2023, 12, 31)),
            TestDataGenerator.GenerateLoadReading(new DateTime(2024, 1, 1)),
            TestDataGenerator.GenerateLoadReading(new DateTime(2024, 1, 5)),
            TestDataGenerator.GenerateLoadReading(new DateTime(2024, 1, 10)),
            TestDataGenerator.GenerateLoadReading(new DateTime(2024, 1, 15))
        };
        await _repository.AddRangeAsync(readings);
        await _repository.SaveChangesAsync();

        // Act
        var result = await _repository.GetByDateRangeAsync(
            new DateTime(2024, 1, 1),
            new DateTime(2024, 1, 10));

        // Assert
        result.Should().HaveCount(3);
        result.Should().AllSatisfy(r =>
        {
            r.Timestamp.Should().BeOnOrAfter(new DateTime(2024, 1, 1));
            r.Timestamp.Should().BeOnOrBefore(new DateTime(2024, 1, 10));
        });
    }

    [Fact]
    public async Task GetByDateRangeAsync_OrdersByTimestamp()
    {
        // Arrange
        var readings = new List<LoadReading>
        {
            TestDataGenerator.GenerateLoadReading(new DateTime(2024, 1, 10)),
            TestDataGenerator.GenerateLoadReading(new DateTime(2024, 1, 2)),
            TestDataGenerator.GenerateLoadReading(new DateTime(2024, 1, 5))
        };
        await _repository.AddRangeAsync(readings);
        await _repository.SaveChangesAsync();

        // Act
        var result = (await _repository.GetByDateRangeAsync(
            new DateTime(2024, 1, 1),
            new DateTime(2024, 1, 15))).ToList();

        // Assert
        result.Should().BeInAscendingOrder(r => r.Timestamp);
    }

    [Fact]
    public async Task GetByDateRangeAsync_EmptyRange_ReturnsEmpty()
    {
        // Arrange
        var readings = TestDataGenerator.GenerateLoadReadings(10,
            new DateTime(2024, 1, 1));
        await _repository.AddRangeAsync(readings);
        await _repository.SaveChangesAsync();

        // Act
        var result = await _repository.GetByDateRangeAsync(
            new DateTime(2025, 1, 1),
            new DateTime(2025, 1, 31));

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetByDateRangeAsync_SingleRecordMatch_ReturnsSingleRecord()
    {
        // Arrange
        var targetDate = new DateTime(2024, 1, 15, 12, 0, 0);
        var readings = new List<LoadReading>
        {
            TestDataGenerator.GenerateLoadReading(new DateTime(2024, 1, 1)),
            TestDataGenerator.GenerateLoadReading(targetDate),
            TestDataGenerator.GenerateLoadReading(new DateTime(2024, 2, 1))
        };
        await _repository.AddRangeAsync(readings);
        await _repository.SaveChangesAsync();

        // Act
        var result = await _repository.GetByDateRangeAsync(
            targetDate,
            targetDate);

        // Assert
        result.Should().HaveCount(1);
        result.First().Timestamp.Should().Be(targetDate);
    }

    [Fact]
    public async Task GetByDateRangeAsync_IncludesStartDateBoundary()
    {
        // Arrange
        var startDate = new DateTime(2024, 1, 1, 0, 0, 0);
        var reading = TestDataGenerator.GenerateLoadReading(startDate);
        await _repository.AddAsync(reading);
        await _repository.SaveChangesAsync();

        // Act
        var result = await _repository.GetByDateRangeAsync(
            startDate,
            new DateTime(2024, 1, 31));

        // Assert
        result.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetByDateRangeAsync_IncludesEndDateBoundary()
    {
        // Arrange
        var endDate = new DateTime(2024, 1, 31, 23, 59, 59);
        var reading = TestDataGenerator.GenerateLoadReading(endDate);
        await _repository.AddAsync(reading);
        await _repository.SaveChangesAsync();

        // Act
        var result = await _repository.GetByDateRangeAsync(
            new DateTime(2024, 1, 1),
            endDate);

        // Assert
        result.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetByDateRangeAsync_LargeDataset_PerformsWell()
    {
        // Arrange
        var startDate = new DateTime(2024, 1, 1);
        var readings = TestDataGenerator.GenerateLoadReadingsForDateRange(
            startDate,
            startDate.AddDays(30));
        await _repository.AddRangeAsync(readings);
        await _repository.SaveChangesAsync();

        // Act
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var result = await _repository.GetByDateRangeAsync(
            startDate,
            startDate.AddDays(7));
        stopwatch.Stop();

        // Assert
        result.Should().NotBeEmpty();
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(1000);
    }

    #endregion

    #region GetByDataSourceAsync Tests

    [Fact]
    public async Task GetByDataSourceAsync_ReturnsMatchingRecords()
    {
        // Arrange
        var dataSource1 = "Source 1";
        var dataSource2 = "Source 2";
        var readings = new List<LoadReading>
        {
            TestDataGenerator.GenerateLoadReading(dataSource: dataSource1),
            TestDataGenerator.GenerateLoadReading(dataSource: dataSource1),
            TestDataGenerator.GenerateLoadReading(dataSource: dataSource2)
        };
        await _repository.AddRangeAsync(readings);
        await _repository.SaveChangesAsync();

        // Act
        var result = await _repository.GetByDataSourceAsync(dataSource1);

        // Assert
        result.Should().HaveCount(2);
        result.Should().AllSatisfy(r => r.DataSource.Should().Be(dataSource1));
    }

    [Fact]
    public async Task GetByDataSourceAsync_OrdersByTimestamp()
    {
        // Arrange
        var dataSource = "Test Source";
        var readings = new List<LoadReading>
        {
            TestDataGenerator.GenerateLoadReading(new DateTime(2024, 1, 3), dataSource: dataSource),
            TestDataGenerator.GenerateLoadReading(new DateTime(2024, 1, 1), dataSource: dataSource),
            TestDataGenerator.GenerateLoadReading(new DateTime(2024, 1, 2), dataSource: dataSource)
        };
        await _repository.AddRangeAsync(readings);
        await _repository.SaveChangesAsync();

        // Act
        var result = await _repository.GetByDataSourceAsync(dataSource);

        // Assert
        result.Should().BeInAscendingOrder(r => r.Timestamp);
    }

    [Fact]
    public async Task GetByDataSourceAsync_NoMatches_ReturnsEmpty()
    {
        // Arrange
        var readings = TestDataGenerator.GenerateLoadReadings(5, dataSource: "Source 1");
        await _repository.AddRangeAsync(readings);
        await _repository.SaveChangesAsync();

        // Act
        var result = await _repository.GetByDataSourceAsync("NonExistent");

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetByDataSourceAsync_CaseSensitive_VerifiesBehavior()
    {
        // Arrange
        var readings = new List<LoadReading>
        {
            TestDataGenerator.GenerateLoadReading(dataSource: "TestSource"),
            TestDataGenerator.GenerateLoadReading(dataSource: "testsource")
        };
        await _repository.AddRangeAsync(readings);
        await _repository.SaveChangesAsync();

        // Act
        var result1 = await _repository.GetByDataSourceAsync("TestSource");
        var result2 = await _repository.GetByDataSourceAsync("testsource");

        // Assert
        result1.Should().HaveCount(1);
        result2.Should().HaveCount(1);
    }

    #endregion

    #region ExistsAsync Tests

    [Fact]
    public async Task ExistsAsync_ExistingTimestamp_ReturnsTrue()
    {
        // Arrange
        var timestamp = new DateTime(2024, 1, 15, 12, 30, 0);
        var reading = TestDataGenerator.GenerateLoadReading(timestamp);
        await _repository.AddAsync(reading);
        await _repository.SaveChangesAsync();

        // Act
        var result = await _repository.ExistsAsync(timestamp);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task ExistsAsync_NonExistingTimestamp_ReturnsFalse()
    {
        // Act
        var result = await _repository.ExistsAsync(new DateTime(2024, 1, 1));

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task ExistsAsync_MillisecondPrecision_Works()
    {
        // Arrange
        var timestamp = new DateTime(2024, 1, 1, 12, 30, 45, 123);
        var reading = TestDataGenerator.GenerateLoadReading(timestamp);
        await _repository.AddAsync(reading);
        await _repository.SaveChangesAsync();

        // Act
        var result = await _repository.ExistsAsync(timestamp);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task ExistsAsync_LargeDataset_PerformsWell()
    {
        // Arrange
        var readings = TestDataGenerator.GenerateLoadReadings(10000);
        await _repository.AddRangeAsync(readings);
        await _repository.SaveChangesAsync();

        var targetTimestamp = readings[5000].Timestamp;

        // Act
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var result = await _repository.ExistsAsync(targetTimestamp);
        stopwatch.Stop();

        // Assert
        result.Should().BeTrue();
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(100);
    }

    #endregion

    #region AddAsync Tests

    [Fact]
    public async Task AddAsync_ValidEntity_AddsSuccessfully()
    {
        // Arrange
        var reading = TestDataGenerator.GenerateLoadReading();

        // Act
        await _repository.AddAsync(reading);
        await _repository.SaveChangesAsync();

        // Assert
        var count = await _repository.CountAsync();
        count.Should().Be(1);
        reading.Id.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task AddAsync_GeneratesId()
    {
        // Arrange
        var reading = TestDataGenerator.GenerateLoadReading();
        reading.Id = 0;

        // Act
        await _repository.AddAsync(reading);
        await _repository.SaveChangesAsync();

        // Assert
        reading.Id.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task AddAsync_AllPropertiesPersisted()
    {
        // Arrange
        var reading = TestDataGenerator.GenerateLoadReading(
            timestamp: new DateTime(2024, 1, 1, 12, 30, 0),
            loadValue: 123.456m,
            dataSource: "Test Source",
            remarks: "Test Remarks");

        // Act
        await _repository.AddAsync(reading);
        await _repository.SaveChangesAsync();

        var retrieved = await _repository.GetByIdAsync(reading.Id);

        // Assert
        retrieved.Should().NotBeNull();
        retrieved!.Timestamp.Should().Be(new DateTime(2024, 1, 1, 12, 30, 0));
        retrieved.LoadValue.Should().Be(123.456m);
        retrieved.DataSource.Should().Be("Test Source");
        retrieved.Remarks.Should().Be("Test Remarks");
    }

    #endregion

    #region AddRangeAsync Tests

    [Fact]
    public async Task AddRangeAsync_MultipleEntities_AddsAll()
    {
        // Arrange
        var readings = TestDataGenerator.GenerateLoadReadings(10);

        // Act
        await _repository.AddRangeAsync(readings);
        await _repository.SaveChangesAsync();

        // Assert
        var count = await _repository.CountAsync();
        count.Should().Be(10);
    }

    [Fact]
    public async Task AddRangeAsync_EmptyCollection_NoError()
    {
        // Arrange
        var readings = new List<LoadReading>();

        // Act
        await _repository.AddRangeAsync(readings);
        await _repository.SaveChangesAsync();

        // Assert
        var count = await _repository.CountAsync();
        count.Should().Be(0);
    }

    [Fact]
    public async Task AddRangeAsync_LargeBatch_PerformsWell()
    {
        // Arrange
        var readings = TestDataGenerator.GenerateLoadReadings(1000);

        // Act
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        await _repository.AddRangeAsync(readings);
        await _repository.SaveChangesAsync();
        stopwatch.Stop();

        // Assert
        var count = await _repository.CountAsync();
        count.Should().Be(1000);
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(5000);
    }

    #endregion

    #region Update Tests

    [Fact]
    public async Task Update_ExistingEntity_UpdatesSuccessfully()
    {
        // Arrange
        var reading = TestDataGenerator.GenerateLoadReading(loadValue: 100m);
        await _repository.AddAsync(reading);
        await _repository.SaveChangesAsync();

        // Act
        reading.LoadValue = 200m;
        reading.Remarks = "Updated";
        _repository.Update(reading);
        await _repository.SaveChangesAsync();

        // Assert
        var updated = await _repository.GetByIdAsync(reading.Id);
        updated!.LoadValue.Should().Be(200m);
        updated.Remarks.Should().Be("Updated");
    }

    [Fact]
    public async Task Update_AllPropertiesUpdated()
    {
        // Arrange
        var reading = TestDataGenerator.GenerateLoadReading();
        await _repository.AddAsync(reading);
        await _repository.SaveChangesAsync();

        // Act
        reading.LoadValue = 999.99m;
        reading.DataSource = "New Source";
        reading.Remarks = "New Remarks";
        _repository.Update(reading);
        await _repository.SaveChangesAsync();

        // Assert
        var updated = await _repository.GetByIdAsync(reading.Id);
        updated!.LoadValue.Should().Be(999.99m);
        updated.DataSource.Should().Be("New Source");
        updated.Remarks.Should().Be("New Remarks");
    }

    #endregion

    #region DeleteAsync Tests

    [Fact]
    public async Task DeleteAsync_ExistingEntity_DeletesSuccessfully()
    {
        // Arrange
        var reading = TestDataGenerator.GenerateLoadReading();
        await _repository.AddAsync(reading);
        await _repository.SaveChangesAsync();
        var id = reading.Id;

        // Act
        await _repository.DeleteAsync(id);
        await _repository.SaveChangesAsync();

        // Assert
        var deleted = await _repository.GetByIdAsync(id);
        deleted.Should().BeNull();

        var count = await _repository.CountAsync();
        count.Should().Be(0);
    }

    [Fact]
    public async Task DeleteAsync_NonExistentId_NoError()
    {
        // Act
        await _repository.DeleteAsync(99999);
        await _repository.SaveChangesAsync();

        // Assert - Should not throw exception
        var count = await _repository.CountAsync();
        count.Should().Be(0);
    }

    [Fact]
    public async Task DeleteAsync_MultipleDeletes_Works()
    {
        // Arrange
        var readings = TestDataGenerator.GenerateLoadReadings(5);
        await _repository.AddRangeAsync(readings);
        await _repository.SaveChangesAsync();

        // Act
        await _repository.DeleteAsync(readings[0].Id);
        await _repository.DeleteAsync(readings[2].Id);
        await _repository.DeleteAsync(readings[4].Id);
        await _repository.SaveChangesAsync();

        // Assert
        var count = await _repository.CountAsync();
        count.Should().Be(2);
    }

    #endregion

    #region DeleteByDateRangeAsync Tests

    [Fact]
    public async Task DeleteByDateRangeAsync_DeletesAllInRange()
    {
        // Arrange
        var readings = new List<LoadReading>
        {
            TestDataGenerator.GenerateLoadReading(new DateTime(2024, 1, 1)),
            TestDataGenerator.GenerateLoadReading(new DateTime(2024, 1, 5)),
            TestDataGenerator.GenerateLoadReading(new DateTime(2024, 1, 10)),
            TestDataGenerator.GenerateLoadReading(new DateTime(2024, 1, 15)),
            TestDataGenerator.GenerateLoadReading(new DateTime(2024, 1, 20))
        };
        await _repository.AddRangeAsync(readings);
        await _repository.SaveChangesAsync();

        // Act
        await _repository.DeleteByDateRangeAsync(
            new DateTime(2024, 1, 5),
            new DateTime(2024, 1, 15));
        await _repository.SaveChangesAsync();

        // Assert
        var remaining = await _repository.GetAllAsync();
        remaining.Should().HaveCount(2);
        remaining.Should().Contain(r => r.Timestamp == new DateTime(2024, 1, 1));
        remaining.Should().Contain(r => r.Timestamp == new DateTime(2024, 1, 20));
    }

    [Fact]
    public async Task DeleteByDateRangeAsync_EmptyRange_NoError()
    {
        // Arrange
        var readings = TestDataGenerator.GenerateLoadReadings(5);
        await _repository.AddRangeAsync(readings);
        await _repository.SaveChangesAsync();

        // Act
        await _repository.DeleteByDateRangeAsync(
            new DateTime(2025, 1, 1),
            new DateTime(2025, 1, 31));
        await _repository.SaveChangesAsync();

        // Assert
        var count = await _repository.CountAsync();
        count.Should().Be(5);
    }

    [Fact]
    public async Task DeleteByDateRangeAsync_BoundaryRecordsDeleted()
    {
        // Arrange
        var startDate = new DateTime(2024, 1, 1);
        var endDate = new DateTime(2024, 1, 31);
        var readings = new List<LoadReading>
        {
            TestDataGenerator.GenerateLoadReading(startDate),
            TestDataGenerator.GenerateLoadReading(endDate),
            TestDataGenerator.GenerateLoadReading(new DateTime(2024, 2, 1))
        };
        await _repository.AddRangeAsync(readings);
        await _repository.SaveChangesAsync();

        // Act
        await _repository.DeleteByDateRangeAsync(startDate, endDate);
        await _repository.SaveChangesAsync();

        // Assert
        var remaining = await _repository.GetAllAsync();
        remaining.Should().HaveCount(1);
        remaining.First().Timestamp.Should().Be(new DateTime(2024, 2, 1));
    }

    [Fact]
    public async Task DeleteByDateRangeAsync_PreservesRecordsOutsideRange()
    {
        // Arrange
        var readings = TestDataGenerator.GenerateLoadReadingsForDateRange(
            new DateTime(2024, 1, 1),
            new DateTime(2024, 1, 31));
        await _repository.AddRangeAsync(readings);
        await _repository.SaveChangesAsync();

        var initialCount = await _repository.CountAsync();

        // Act
        await _repository.DeleteByDateRangeAsync(
            new DateTime(2024, 1, 10),
            new DateTime(2024, 1, 15));
        await _repository.SaveChangesAsync();

        // Assert
        var finalCount = await _repository.CountAsync();
        finalCount.Should().BeLessThan(initialCount);

        var remaining = await _repository.GetAllAsync();
        remaining.Should().AllSatisfy(r =>
        {
            var inDeletedRange = r.Timestamp >= new DateTime(2024, 1, 10) &&
                                r.Timestamp <= new DateTime(2024, 1, 15);
            inDeletedRange.Should().BeFalse();
        });
    }

    #endregion

    #region CountAsync Tests

    [Fact]
    public async Task CountAsync_EmptyDatabase_ReturnsZero()
    {
        // Act
        var count = await _repository.CountAsync();

        // Assert
        count.Should().Be(0);
    }

    [Fact]
    public async Task CountAsync_WithData_ReturnsCorrectCount()
    {
        // Arrange
        var readings = TestDataGenerator.GenerateLoadReadings(42);
        await _repository.AddRangeAsync(readings);
        await _repository.SaveChangesAsync();

        // Act
        var count = await _repository.CountAsync();

        // Assert
        count.Should().Be(42);
    }

    [Fact]
    public async Task CountAsync_AfterAdd_CountIncreases()
    {
        // Arrange
        var initialCount = await _repository.CountAsync();

        // Act
        var reading = TestDataGenerator.GenerateLoadReading();
        await _repository.AddAsync(reading);
        await _repository.SaveChangesAsync();

        // Assert
        var newCount = await _repository.CountAsync();
        newCount.Should().Be(initialCount + 1);
    }

    [Fact]
    public async Task CountAsync_AfterDelete_CountDecreases()
    {
        // Arrange
        var readings = TestDataGenerator.GenerateLoadReadings(5);
        await _repository.AddRangeAsync(readings);
        await _repository.SaveChangesAsync();

        // Act
        await _repository.DeleteAsync(readings[0].Id);
        await _repository.SaveChangesAsync();

        // Assert
        var count = await _repository.CountAsync();
        count.Should().Be(4);
    }

    [Fact]
    public async Task CountAsync_LargeCount_PerformsWell()
    {
        // Arrange
        var readings = TestDataGenerator.GenerateLoadReadings(10000);
        await _repository.AddRangeAsync(readings);
        await _repository.SaveChangesAsync();

        // Act
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var count = await _repository.CountAsync();
        stopwatch.Stop();

        // Assert
        count.Should().Be(10000);
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(100);
    }

    #endregion

    #region GetDateRangeAsync Tests

    [Fact]
    public async Task GetDateRangeAsync_EmptyDatabase_ReturnsNullValues()
    {
        // Act
        var (minDate, maxDate) = await _repository.GetDateRangeAsync();

        // Assert
        minDate.Should().BeNull();
        maxDate.Should().BeNull();
    }

    [Fact]
    public async Task GetDateRangeAsync_WithData_ReturnsCorrectRange()
    {
        // Arrange
        var readings = new List<LoadReading>
        {
            TestDataGenerator.GenerateLoadReading(new DateTime(2024, 1, 15)),
            TestDataGenerator.GenerateLoadReading(new DateTime(2024, 1, 1)),
            TestDataGenerator.GenerateLoadReading(new DateTime(2024, 1, 31))
        };
        await _repository.AddRangeAsync(readings);
        await _repository.SaveChangesAsync();

        // Act
        var (minDate, maxDate) = await _repository.GetDateRangeAsync();

        // Assert
        minDate.Should().Be(new DateTime(2024, 1, 1));
        maxDate.Should().Be(new DateTime(2024, 1, 31));
    }

    [Fact]
    public async Task GetDateRangeAsync_SingleRecord_MinEqualsMax()
    {
        // Arrange
        var timestamp = new DateTime(2024, 1, 15, 12, 30, 0);
        var reading = TestDataGenerator.GenerateLoadReading(timestamp);
        await _repository.AddAsync(reading);
        await _repository.SaveChangesAsync();

        // Act
        var (minDate, maxDate) = await _repository.GetDateRangeAsync();

        // Assert
        minDate.Should().Be(timestamp);
        maxDate.Should().Be(timestamp);
    }

    [Fact]
    public async Task GetDateRangeAsync_AfterInsert_RangeUpdates()
    {
        // Arrange
        var reading1 = TestDataGenerator.GenerateLoadReading(new DateTime(2024, 1, 15));
        await _repository.AddAsync(reading1);
        await _repository.SaveChangesAsync();

        // Act & Assert - First check
        var (min1, max1) = await _repository.GetDateRangeAsync();
        min1.Should().Be(new DateTime(2024, 1, 15));
        max1.Should().Be(new DateTime(2024, 1, 15));

        // Add new min
        var reading2 = TestDataGenerator.GenerateLoadReading(new DateTime(2024, 1, 1));
        await _repository.AddAsync(reading2);
        await _repository.SaveChangesAsync();

        var (min2, max2) = await _repository.GetDateRangeAsync();
        min2.Should().Be(new DateTime(2024, 1, 1));
        max2.Should().Be(new DateTime(2024, 1, 15));

        // Add new max
        var reading3 = TestDataGenerator.GenerateLoadReading(new DateTime(2024, 1, 31));
        await _repository.AddAsync(reading3);
        await _repository.SaveChangesAsync();

        var (min3, max3) = await _repository.GetDateRangeAsync();
        min3.Should().Be(new DateTime(2024, 1, 1));
        max3.Should().Be(new DateTime(2024, 1, 31));
    }

    [Fact]
    public async Task GetDateRangeAsync_LargeDataset_PerformsWell()
    {
        // Arrange
        var readings = TestDataGenerator.GenerateLoadReadings(10000);
        await _repository.AddRangeAsync(readings);
        await _repository.SaveChangesAsync();

        // Act
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var (minDate, maxDate) = await _repository.GetDateRangeAsync();
        stopwatch.Stop();

        // Assert
        minDate.Should().NotBeNull();
        maxDate.Should().NotBeNull();
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(100);
    }

    #endregion

    #region SaveChangesAsync Tests

    [Fact]
    public async Task SaveChangesAsync_ReturnsNumberOfChanges()
    {
        // Arrange
        var readings = TestDataGenerator.GenerateLoadReadings(3);
        await _repository.AddRangeAsync(readings);

        // Act
        var changesCount = await _repository.SaveChangesAsync();

        // Assert
        changesCount.Should().Be(3);
    }

    [Fact]
    public async Task SaveChangesAsync_NoChanges_ReturnsZero()
    {
        // Act
        var changesCount = await _repository.SaveChangesAsync();

        // Assert
        changesCount.Should().Be(0);
    }

    [Fact]
    public async Task SaveChangesAsync_MultipleChanges_ReturnsCorrectCount()
    {
        // Arrange
        var readings = TestDataGenerator.GenerateLoadReadings(5);
        await _repository.AddRangeAsync(readings);
        await _repository.SaveChangesAsync();

        readings[0].LoadValue = 999m;
        readings[1].LoadValue = 888m;
        _repository.Update(readings[0]);
        _repository.Update(readings[1]);
        await _repository.DeleteAsync(readings[2].Id);

        // Act
        var changesCount = await _repository.SaveChangesAsync();

        // Assert
        changesCount.Should().Be(3); // 2 updates + 1 delete
    }

    #endregion
}
