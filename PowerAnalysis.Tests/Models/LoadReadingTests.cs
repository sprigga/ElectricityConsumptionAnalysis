using PowerAnalysis.Models;
using PowerAnalysis.Tests.Helpers;

namespace PowerAnalysis.Tests.Models;

/// <summary>
/// Tests for LoadReading model and validation
/// </summary>
public class LoadReadingTests : TestBase
{
    #region Entity Creation Tests

    [Fact]
    public void LoadReading_CanBeCreated()
    {
        // Arrange & Act
        var reading = new LoadReading
        {
            Timestamp = DateTime.UtcNow,
            LoadValue = 123.45m,
            DataSource = "Test Source",
            ImportedAt = DateTime.UtcNow
        };

        // Assert
        reading.Should().NotBeNull();
        reading.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        reading.LoadValue.Should().Be(123.45m);
        reading.DataSource.Should().Be("Test Source");
    }

    [Fact]
    public void LoadReading_AllPropertiesCanBeSet()
    {
        // Arrange
        var timestamp = new DateTime(2024, 1, 15, 12, 30, 0);
        var importedAt = DateTime.UtcNow;

        // Act
        var reading = new LoadReading
        {
            Id = 1,
            Timestamp = timestamp,
            LoadValue = 456.78m,
            DataSource = "Test Data Source",
            ImportedAt = importedAt,
            Remarks = "Test remarks"
        };

        // Assert
        reading.Id.Should().Be(1);
        reading.Timestamp.Should().Be(timestamp);
        reading.LoadValue.Should().Be(456.78m);
        reading.DataSource.Should().Be("Test Data Source");
        reading.ImportedAt.Should().Be(importedAt);
        reading.Remarks.Should().Be("Test remarks");
    }

    [Fact]
    public void LoadReading_RemarksCanBeNull()
    {
        // Arrange & Act
        var reading = new LoadReading
        {
            Timestamp = DateTime.UtcNow,
            LoadValue = 100m,
            DataSource = "Test",
            ImportedAt = DateTime.UtcNow,
            Remarks = null
        };

        // Assert
        reading.Remarks.Should().BeNull();
    }

    #endregion

    #region Property Validation Tests

    [Fact]
    public async Task LoadReading_Timestamp_IsRequired()
    {
        // Arrange
        var reading = TestDataGenerator.GenerateLoadReading();
        await DbContext.LoadReadings.AddAsync(reading);
        await DbContext.SaveChangesAsync();

        // Assert
        var saved = await DbContext.LoadReadings.FindAsync(reading.Id);
        saved.Should().NotBeNull();
        saved!.Timestamp.Should().NotBe(default(DateTime));
    }

    [Fact]
    public async Task LoadReading_LoadValue_IsRequired()
    {
        // Arrange
        var reading = TestDataGenerator.GenerateLoadReading(loadValue: 0m);
        await DbContext.LoadReadings.AddAsync(reading);
        await DbContext.SaveChangesAsync();

        // Assert
        var saved = await DbContext.LoadReadings.FindAsync(reading.Id);
        saved.Should().NotBeNull();
        saved!.LoadValue.Should().Be(0m);
    }

    [Fact]
    public async Task LoadReading_LoadValue_AcceptsDecimals()
    {
        // Arrange
        var reading = TestDataGenerator.GenerateLoadReading(loadValue: 123.456m);
        await DbContext.LoadReadings.AddAsync(reading);
        await DbContext.SaveChangesAsync();

        // Assert
        var saved = await DbContext.LoadReadings.FindAsync(reading.Id);
        saved!.LoadValue.Should().Be(123.456m);
    }

    [Fact]
    public async Task LoadReading_LoadValue_AcceptsNegativeValues()
    {
        // Arrange
        var reading = TestDataGenerator.GenerateLoadReading(loadValue: -50.5m);
        await DbContext.LoadReadings.AddAsync(reading);
        await DbContext.SaveChangesAsync();

        // Assert
        var saved = await DbContext.LoadReadings.FindAsync(reading.Id);
        saved!.LoadValue.Should().Be(-50.5m);
    }

    [Fact]
    public async Task LoadReading_LoadValue_AcceptsZero()
    {
        // Arrange
        var reading = TestDataGenerator.GenerateLoadReading(loadValue: 0m);
        await DbContext.LoadReadings.AddAsync(reading);
        await DbContext.SaveChangesAsync();

        // Assert
        var saved = await DbContext.LoadReadings.FindAsync(reading.Id);
        saved!.LoadValue.Should().Be(0m);
    }

    [Fact]
    public async Task LoadReading_LoadValue_AcceptsLargeValues()
    {
        // Arrange
        var reading = TestDataGenerator.GenerateLoadReading(loadValue: 999999999.999m);
        await DbContext.LoadReadings.AddAsync(reading);
        await DbContext.SaveChangesAsync();

        // Assert
        var saved = await DbContext.LoadReadings.FindAsync(reading.Id);
        saved!.LoadValue.Should().Be(999999999.999m);
    }

    [Fact]
    public async Task LoadReading_DataSource_CanBeSet()
    {
        // Arrange
        var reading = TestDataGenerator.GenerateLoadReading(dataSource: "Custom Source");
        await DbContext.LoadReadings.AddAsync(reading);
        await DbContext.SaveChangesAsync();

        // Assert
        var saved = await DbContext.LoadReadings.FindAsync(reading.Id);
        saved!.DataSource.Should().Be("Custom Source");
    }

    [Fact]
    public async Task LoadReading_DataSource_AcceptsLongStrings()
    {
        // Arrange
        var longSource = new string('A', 100); // Max length is 100
        var reading = TestDataGenerator.GenerateLoadReading(dataSource: longSource);
        await DbContext.LoadReadings.AddAsync(reading);
        await DbContext.SaveChangesAsync();

        // Assert
        var saved = await DbContext.LoadReadings.FindAsync(reading.Id);
        saved!.DataSource.Should().Be(longSource);
        saved.DataSource.Length.Should().Be(100);
    }

    [Fact]
    public async Task LoadReading_Remarks_AcceptsLongText()
    {
        // Arrange
        var longRemarks = new string('B', 500); // Max length is 500
        var reading = TestDataGenerator.GenerateLoadReading(remarks: longRemarks);
        await DbContext.LoadReadings.AddAsync(reading);
        await DbContext.SaveChangesAsync();

        // Assert
        var saved = await DbContext.LoadReadings.FindAsync(reading.Id);
        saved!.Remarks.Should().Be(longRemarks);
        saved.Remarks!.Length.Should().Be(500);
    }

    [Fact]
    public async Task LoadReading_ImportedAt_IsSet()
    {
        // Arrange
        var importedAt = DateTime.UtcNow;
        var reading = TestDataGenerator.GenerateLoadReading();
        reading.ImportedAt = importedAt;

        await DbContext.LoadReadings.AddAsync(reading);
        await DbContext.SaveChangesAsync();

        // Assert
        var saved = await DbContext.LoadReadings.FindAsync(reading.Id);
        saved!.ImportedAt.Should().BeCloseTo(importedAt, TimeSpan.FromSeconds(1));
    }

    #endregion

    #region Database Constraint Tests

    [Fact]
    public async Task LoadReading_Timestamp_MustBeUnique()
    {
        // Arrange
        var timestamp = new DateTime(2024, 1, 15, 12, 30, 0);
        var reading1 = TestDataGenerator.GenerateLoadReading(timestamp);
        var reading2 = TestDataGenerator.GenerateLoadReading(timestamp);

        await DbContext.LoadReadings.AddAsync(reading1);
        await DbContext.SaveChangesAsync();

        // Act
        await DbContext.LoadReadings.AddAsync(reading2);
        var saveAction = async () => await DbContext.SaveChangesAsync();

        // Assert
        // In-memory database may not enforce unique constraint the same way as SQL
        // This test verifies the behavior in the in-memory context
        // In a real SQL database, this would throw a DbUpdateException
        await saveAction.Should().ThrowAsync<Exception>();
    }

    [Fact]
    public async Task LoadReading_Id_AutoIncrements()
    {
        // Arrange
        var reading1 = TestDataGenerator.GenerateLoadReading();
        var reading2 = TestDataGenerator.GenerateLoadReading();

        // Act
        await DbContext.LoadReadings.AddAsync(reading1);
        await DbContext.SaveChangesAsync();

        await DbContext.LoadReadings.AddAsync(reading2);
        await DbContext.SaveChangesAsync();

        // Assert
        reading1.Id.Should().BeGreaterThan(0);
        reading2.Id.Should().BeGreaterThan(0);
        reading2.Id.Should().BeGreaterThan(reading1.Id);
    }

    [Fact]
    public async Task LoadReading_CanBeQueriedById()
    {
        // Arrange
        var reading = TestDataGenerator.GenerateLoadReading();
        await DbContext.LoadReadings.AddAsync(reading);
        await DbContext.SaveChangesAsync();

        // Act
        var found = await DbContext.LoadReadings.FindAsync(reading.Id);

        // Assert
        found.Should().NotBeNull();
        found!.Id.Should().Be(reading.Id);
        found.Timestamp.Should().Be(reading.Timestamp);
    }

    [Fact]
    public async Task LoadReading_CanBeUpdated()
    {
        // Arrange
        var reading = TestDataGenerator.GenerateLoadReading(loadValue: 100m);
        await DbContext.LoadReadings.AddAsync(reading);
        await DbContext.SaveChangesAsync();

        // Act
        reading.LoadValue = 200m;
        reading.Remarks = "Updated";
        DbContext.LoadReadings.Update(reading);
        await DbContext.SaveChangesAsync();

        // Assert
        var updated = await DbContext.LoadReadings.FindAsync(reading.Id);
        updated!.LoadValue.Should().Be(200m);
        updated.Remarks.Should().Be("Updated");
    }

    [Fact]
    public async Task LoadReading_CanBeDeleted()
    {
        // Arrange
        var reading = TestDataGenerator.GenerateLoadReading();
        await DbContext.LoadReadings.AddAsync(reading);
        await DbContext.SaveChangesAsync();
        var id = reading.Id;

        // Act
        DbContext.LoadReadings.Remove(reading);
        await DbContext.SaveChangesAsync();

        // Assert
        var deleted = await DbContext.LoadReadings.FindAsync(id);
        deleted.Should().BeNull();
    }

    #endregion

    #region Edge Case Tests

    [Fact]
    public async Task LoadReading_Timestamp_AcceptsMinValue()
    {
        // Arrange
        var reading = TestDataGenerator.GenerateLoadReading(DateTime.MinValue);
        await DbContext.LoadReadings.AddAsync(reading);
        await DbContext.SaveChangesAsync();

        // Assert
        var saved = await DbContext.LoadReadings.FindAsync(reading.Id);
        saved!.Timestamp.Should().Be(DateTime.MinValue);
    }

    [Fact]
    public async Task LoadReading_Timestamp_AcceptsMaxValue()
    {
        // Arrange
        var reading = TestDataGenerator.GenerateLoadReading(DateTime.MaxValue);
        await DbContext.LoadReadings.AddAsync(reading);
        await DbContext.SaveChangesAsync();

        // Assert
        var saved = await DbContext.LoadReadings.FindAsync(reading.Id);
        saved!.Timestamp.Should().Be(DateTime.MaxValue);
    }

    [Fact]
    public async Task LoadReading_Timestamp_PreservesMilliseconds()
    {
        // Arrange
        var timestamp = new DateTime(2024, 1, 15, 12, 30, 45, 123);
        var reading = TestDataGenerator.GenerateLoadReading(timestamp);
        await DbContext.LoadReadings.AddAsync(reading);
        await DbContext.SaveChangesAsync();

        // Assert
        var saved = await DbContext.LoadReadings.FindAsync(reading.Id);
        saved!.Timestamp.Should().Be(timestamp);
        saved.Timestamp.Millisecond.Should().Be(123);
    }

    [Fact]
    public async Task LoadReading_DataSource_AcceptsEmptyString()
    {
        // Arrange
        var reading = TestDataGenerator.GenerateLoadReading(dataSource: "");
        await DbContext.LoadReadings.AddAsync(reading);
        await DbContext.SaveChangesAsync();

        // Assert
        var saved = await DbContext.LoadReadings.FindAsync(reading.Id);
        saved!.DataSource.Should().BeEmpty();
    }

    [Fact]
    public async Task LoadReading_Remarks_AcceptsEmptyString()
    {
        // Arrange
        var reading = TestDataGenerator.GenerateLoadReading(remarks: "");
        await DbContext.LoadReadings.AddAsync(reading);
        await DbContext.SaveChangesAsync();

        // Assert
        var saved = await DbContext.LoadReadings.FindAsync(reading.Id);
        saved!.Remarks.Should().BeEmpty();
    }

    [Fact]
    public async Task LoadReading_MultipleRecords_CanHaveSameDataSource()
    {
        // Arrange
        var dataSource = "Same Source";
        var reading1 = TestDataGenerator.GenerateLoadReading(dataSource: dataSource);
        var reading2 = TestDataGenerator.GenerateLoadReading(dataSource: dataSource);

        await DbContext.LoadReadings.AddAsync(reading1);
        await DbContext.LoadReadings.AddAsync(reading2);
        await DbContext.SaveChangesAsync();

        // Assert
        var readings = DbContext.LoadReadings.Where(r => r.DataSource == dataSource).ToList();
        readings.Should().HaveCount(2);
    }

    [Fact]
    public async Task LoadReading_MultipleRecords_CanHaveSameLoadValue()
    {
        // Arrange
        var loadValue = 123.45m;
        var reading1 = TestDataGenerator.GenerateLoadReading(loadValue: loadValue);
        var reading2 = TestDataGenerator.GenerateLoadReading(loadValue: loadValue);

        await DbContext.LoadReadings.AddAsync(reading1);
        await DbContext.LoadReadings.AddAsync(reading2);
        await DbContext.SaveChangesAsync();

        // Assert
        var readings = DbContext.LoadReadings.Where(r => r.LoadValue == loadValue).ToList();
        readings.Should().HaveCount(2);
    }

    #endregion

    #region Precision Tests

    [Theory]
    [InlineData(0.001)]
    [InlineData(0.123)]
    [InlineData(1.234)]
    [InlineData(12.345)]
    [InlineData(123.456)]
    public async Task LoadReading_LoadValue_PreservesDecimalPrecision(double value)
    {
        // Arrange
        var decimalValue = (decimal)value;
        var reading = TestDataGenerator.GenerateLoadReading(loadValue: decimalValue);
        await DbContext.LoadReadings.AddAsync(reading);
        await DbContext.SaveChangesAsync();

        // Assert
        var saved = await DbContext.LoadReadings.FindAsync(reading.Id);
        saved!.LoadValue.Should().Be(decimalValue);
    }

    [Fact]
    public async Task LoadReading_LoadValue_Handles18_3Precision()
    {
        // Arrange - 18 total digits, 3 after decimal
        var value = 999999999999999.999m;
        var reading = TestDataGenerator.GenerateLoadReading(loadValue: value);
        await DbContext.LoadReadings.AddAsync(reading);
        await DbContext.SaveChangesAsync();

        // Assert
        var saved = await DbContext.LoadReadings.FindAsync(reading.Id);
        saved!.LoadValue.Should().Be(value);
    }

    #endregion
}
