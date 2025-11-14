using Bogus;
using PowerAnalysis.Models;

namespace PowerAnalysis.Tests.Helpers;

/// <summary>
/// Test data generator using Bogus library
/// </summary>
public static class TestDataGenerator
{
    /// <summary>
    /// Generate a single LoadReading with optional overrides
    /// </summary>
    public static LoadReading GenerateLoadReading(
        DateTime? timestamp = null,
        decimal? loadValue = null,
        string? dataSource = null,
        string? remarks = null)
    {
        return new LoadReading
        {
            Timestamp = timestamp ?? DateTime.UtcNow.AddHours(-new Random().Next(1, 1000)),
            LoadValue = loadValue ?? (decimal)(new Random().NextDouble() * 1000),
            DataSource = dataSource ?? "Test Data Source",
            ImportedAt = DateTime.UtcNow,
            Remarks = remarks
        };
    }

    /// <summary>
    /// Generate multiple LoadReadings with sequential timestamps
    /// </summary>
    public static List<LoadReading> GenerateLoadReadings(
        int count,
        DateTime? startDate = null,
        TimeSpan? interval = null,
        string? dataSource = null)
    {
        var start = startDate ?? DateTime.UtcNow.Date;
        var timeInterval = interval ?? TimeSpan.FromMinutes(30);
        var readings = new List<LoadReading>();

        var faker = new Faker();

        for (int i = 0; i < count; i++)
        {
            readings.Add(new LoadReading
            {
                Timestamp = start.Add(timeInterval * i),
                LoadValue = (decimal)faker.Random.Double(50, 500),
                DataSource = dataSource ?? "Test Data Source",
                ImportedAt = DateTime.UtcNow,
                Remarks = i % 10 == 0 ? faker.Lorem.Sentence() : null
            });
        }

        return readings;
    }

    /// <summary>
    /// Generate LoadReadings for a specific date range with 30-minute intervals
    /// </summary>
    public static List<LoadReading> GenerateLoadReadingsForDateRange(
        DateTime startDate,
        DateTime endDate,
        string? dataSource = null)
    {
        var readings = new List<LoadReading>();
        var current = startDate;
        var faker = new Faker();

        while (current <= endDate)
        {
            readings.Add(new LoadReading
            {
                Timestamp = current,
                LoadValue = (decimal)faker.Random.Double(50, 500),
                DataSource = dataSource ?? "Test Data Source",
                ImportedAt = DateTime.UtcNow
            });
            current = current.AddMinutes(30);
        }

        return readings;
    }

    /// <summary>
    /// Generate LoadReadings with specific values for testing aggregation
    /// </summary>
    public static List<LoadReading> GenerateLoadReadingsWithKnownValues(
        DateTime startDate,
        int daysCount,
        decimal baseValue = 100m)
    {
        var readings = new List<LoadReading>();
        var current = startDate;

        for (int day = 0; day < daysCount; day++)
        {
            for (int hour = 0; hour < 24; hour++)
            {
                for (int halfHour = 0; halfHour < 2; halfHour++)
                {
                    readings.Add(new LoadReading
                    {
                        Timestamp = current,
                        LoadValue = baseValue + day * 10 + hour,
                        DataSource = "Test Known Values",
                        ImportedAt = DateTime.UtcNow
                    });
                    current = current.AddMinutes(30);
                }
            }
        }

        return readings;
    }

    /// <summary>
    /// Generate a Faker instance for LoadReading
    /// </summary>
    public static Faker<LoadReading> GetLoadReadingFaker(string? dataSource = null)
    {
        return new Faker<LoadReading>()
            .RuleFor(lr => lr.Timestamp, f => f.Date.Between(
                DateTime.UtcNow.AddMonths(-6),
                DateTime.UtcNow))
            .RuleFor(lr => lr.LoadValue, f => (decimal)f.Random.Double(10, 1000))
            .RuleFor(lr => lr.DataSource, f => dataSource ?? f.Company.CompanyName())
            .RuleFor(lr => lr.ImportedAt, f => DateTime.UtcNow)
            .RuleFor(lr => lr.Remarks, f => f.Random.Bool(0.2f) ? f.Lorem.Sentence() : null);
    }
}
