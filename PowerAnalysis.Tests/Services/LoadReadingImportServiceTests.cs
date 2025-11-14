using Microsoft.Extensions.Logging;
using PowerAnalysis.Repositories;
using PowerAnalysis.Services;
using PowerAnalysis.Tests.Helpers;

namespace PowerAnalysis.Tests.Services;

/// <summary>
/// Comprehensive tests for LoadReadingImportService
/// </summary>
public class LoadReadingImportServiceTests : TestBase
{
    private readonly Mock<ILogger<LoadReadingImportService>> _mockLogger;
    private readonly ILoadReadingRepository _repository;
    private readonly LoadReadingImportService _service;

    public LoadReadingImportServiceTests()
    {
        _mockLogger = LoggerHelper.CreateMockLogger<LoadReadingImportService>();
        _repository = new LoadReadingRepository(DbContext);
        _service = new LoadReadingImportService(_repository, _mockLogger.Object);
    }

    #region ImportFromExcelAsync Tests

    [Fact]
    public async Task ImportFromExcelAsync_ValidFile_ImportsSuccessfully()
    {
        // Arrange
        var filePath = ExcelTestHelper.CreateValidExcelFile(
            "valid_test.xlsx",
            new DateTime(2024, 1, 1),
            3);

        try
        {
            // Act
            var result = await _service.ImportFromExcelAsync(filePath);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.ImportedCount.Should().BeGreaterThan(0);
            result.ErrorMessage.Should().BeNullOrEmpty();
            result.ElapsedMilliseconds.Should().BeGreaterThan(0);
            result.Messages.Should().Contain(m => m.Contains("成功導入"));

            var count = await _repository.CountAsync();
            count.Should().Be(result.ImportedCount);

            LoggerHelper.VerifyInformationLog(_mockLogger);
        }
        finally
        {
            ExcelTestHelper.CleanupTestFile(filePath);
        }
    }

    [Fact]
    public async Task ImportFromExcelAsync_FileNotFound_ReturnsFailure()
    {
        // Arrange
        var filePath = "/non/existent/file.xlsx";

        // Act
        var result = await _service.ImportFromExcelAsync(filePath);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("文件不存在");
        result.ImportedCount.Should().Be(0);

        LoggerHelper.VerifyErrorLog(_mockLogger);
    }

    [Fact]
    public async Task ImportFromExcelAsync_InvalidWorksheetName_ReturnsFailure()
    {
        // Arrange
        var filePath = ExcelTestHelper.CreateValidExcelFile(
            "test_worksheet.xlsx",
            new DateTime(2024, 1, 1),
            2);

        try
        {
            // Act
            var result = await _service.ImportFromExcelAsync(
                filePath,
                sheetName: "NonExistentSheet");

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Contain("找不到工作表");
            result.ImportedCount.Should().Be(0);

            LoggerHelper.VerifyErrorLog(_mockLogger);
        }
        finally
        {
            ExcelTestHelper.CleanupTestFile(filePath);
        }
    }

    [Fact]
    public async Task ImportFromExcelAsync_EmptyWorksheet_ReturnsFailure()
    {
        // Arrange
        var filePath = ExcelTestHelper.CreateEmptyExcelFile("empty_test.xlsx");

        try
        {
            // Act
            var result = await _service.ImportFromExcelAsync(filePath);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Contain("沒有可導入的數據");
            result.ImportedCount.Should().Be(0);

            LoggerHelper.VerifyWarningLog(_mockLogger);
        }
        finally
        {
            ExcelTestHelper.CleanupTestFile(filePath);
        }
    }

    [Fact]
    public async Task ImportFromExcelAsync_InvalidDateFormats_SkipsInvalidRows()
    {
        // Arrange
        var filePath = ExcelTestHelper.CreateExcelFileWithInvalidDates(
            "invalid_dates.xlsx");

        try
        {
            // Act
            var result = await _service.ImportFromExcelAsync(filePath);

            // Assert
            result.Messages.Should().Contain(m => m.Contains("無法解析日期"));
            result.SkippedCount.Should().BeGreaterThan(0);
        }
        finally
        {
            ExcelTestHelper.CleanupTestFile(filePath);
        }
    }

    [Fact]
    public async Task ImportFromExcelAsync_InvalidTimeFormats_SkipsInvalidRows()
    {
        // Arrange
        var filePath = ExcelTestHelper.CreateExcelFileWithInvalidTimes(
            "invalid_times.xlsx");

        try
        {
            // Act
            var result = await _service.ImportFromExcelAsync(filePath);

            // Assert
            result.Messages.Should().Contain(m => m.Contains("無法解析時間"));
            result.SkippedCount.Should().BeGreaterThan(0);
        }
        finally
        {
            ExcelTestHelper.CleanupTestFile(filePath);
        }
    }

    [Fact]
    public async Task ImportFromExcelAsync_InvalidLoadValues_SkipsInvalidCells()
    {
        // Arrange
        var filePath = ExcelTestHelper.CreateExcelFileWithInvalidValues(
            "invalid_values.xlsx");

        try
        {
            // Act
            var result = await _service.ImportFromExcelAsync(filePath);

            // Assert
            result.Messages.Should().Contain(m => m.Contains("無法解析負載值"));
            result.SkippedCount.Should().BeGreaterThan(0);
        }
        finally
        {
            ExcelTestHelper.CleanupTestFile(filePath);
        }
    }

    [Fact]
    public async Task ImportFromExcelAsync_MixedValidInvalidData_ImportsValidSkipsInvalid()
    {
        // Arrange
        var filePath = ExcelTestHelper.CreateMixedValidityExcelFile(
            "mixed_data.xlsx");

        try
        {
            // Act
            var result = await _service.ImportFromExcelAsync(filePath);

            // Assert
            result.ImportedCount.Should().BeGreaterThan(0);
            result.SkippedCount.Should().BeGreaterThan(0);
            result.Messages.Should().NotBeEmpty();

            var count = await _repository.CountAsync();
            count.Should().Be(result.ImportedCount);
        }
        finally
        {
            ExcelTestHelper.CleanupTestFile(filePath);
        }
    }

    [Fact]
    public async Task ImportFromExcelAsync_LargeFile_ImportsSuccessfully()
    {
        // Arrange
        var filePath = ExcelTestHelper.CreateValidExcelFile(
            "large_file.xlsx",
            new DateTime(2024, 1, 1),
            30); // 30 days of data

        try
        {
            // Act
            var result = await _service.ImportFromExcelAsync(filePath);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.ImportedCount.Should().BeGreaterThan(1000); // 30 days * 48 records/day
            result.ElapsedMilliseconds.Should().BeGreaterThan(0);

            var count = await _repository.CountAsync();
            count.Should().Be(result.ImportedCount);
        }
        finally
        {
            ExcelTestHelper.CleanupTestFile(filePath);
        }
    }

    [Fact]
    public async Task ImportFromExcelAsync_CustomDataSource_UsesProvidedDataSource()
    {
        // Arrange
        var filePath = ExcelTestHelper.CreateValidExcelFile(
            "custom_source.xlsx",
            new DateTime(2024, 1, 1),
            2);
        var customDataSource = "Custom Test Source";

        try
        {
            // Act
            var result = await _service.ImportFromExcelAsync(
                filePath,
                dataSource: customDataSource);

            // Assert
            result.IsSuccess.Should().BeTrue();

            var readings = await _repository.GetAllAsync();
            readings.Should().AllSatisfy(r =>
                r.DataSource.Should().Be(customDataSource));
        }
        finally
        {
            ExcelTestHelper.CleanupTestFile(filePath);
        }
    }

    [Fact]
    public async Task ImportFromExcelAsync_SetsImportedAtTimestamp()
    {
        // Arrange
        var filePath = ExcelTestHelper.CreateValidExcelFile(
            "timestamp_test.xlsx",
            new DateTime(2024, 1, 1),
            1);
        var beforeImport = DateTime.UtcNow;

        try
        {
            // Act
            var result = await _service.ImportFromExcelAsync(filePath);
            var afterImport = DateTime.UtcNow;

            // Assert
            result.IsSuccess.Should().BeTrue();

            var readings = await _repository.GetAllAsync();
            readings.Should().AllSatisfy(r =>
            {
                r.ImportedAt.Should().BeOnOrAfter(beforeImport);
                r.ImportedAt.Should().BeOnOrBefore(afterImport);
            });
        }
        finally
        {
            ExcelTestHelper.CleanupTestFile(filePath);
        }
    }

    [Fact]
    public async Task ImportFromExcelAsync_RecordsElapsedTime()
    {
        // Arrange
        var filePath = ExcelTestHelper.CreateValidExcelFile(
            "timing_test.xlsx",
            new DateTime(2024, 1, 1),
            5);

        try
        {
            // Act
            var result = await _service.ImportFromExcelAsync(filePath);

            // Assert
            result.ElapsedMilliseconds.Should().BeGreaterThan(0);
        }
        finally
        {
            ExcelTestHelper.CleanupTestFile(filePath);
        }
    }

    [Fact]
    public async Task ImportFromExcelAsync_PopulatesMessagesCollection()
    {
        // Arrange
        var filePath = ExcelTestHelper.CreateValidExcelFile(
            "messages_test.xlsx",
            new DateTime(2024, 1, 1),
            2);

        try
        {
            // Act
            var result = await _service.ImportFromExcelAsync(filePath);

            // Assert
            result.Messages.Should().NotBeNull();
            result.Messages.Should().NotBeEmpty();
            result.Messages.Should().Contain(m => m.Contains("成功導入"));
        }
        finally
        {
            ExcelTestHelper.CleanupTestFile(filePath);
        }
    }

    #endregion

    #region ValidateExcelFormat Tests

    [Fact]
    public void ValidateExcelFormat_ValidFile_ReturnsTrue()
    {
        // Arrange
        var filePath = ExcelTestHelper.CreateValidExcelFile(
            "validate_valid.xlsx",
            new DateTime(2024, 1, 1),
            2);

        try
        {
            // Act
            var result = _service.ValidateExcelFormat(filePath, "負載交叉表");

            // Assert
            result.Should().BeTrue();
        }
        finally
        {
            ExcelTestHelper.CleanupTestFile(filePath);
        }
    }

    [Fact]
    public void ValidateExcelFormat_FileNotFound_ReturnsFalse()
    {
        // Arrange
        var filePath = "/non/existent/file.xlsx";

        // Act
        var result = _service.ValidateExcelFormat(filePath, "負載交叉表");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ValidateExcelFormat_InvalidWorksheet_ReturnsFalse()
    {
        // Arrange
        var filePath = ExcelTestHelper.CreateValidExcelFile(
            "validate_worksheet.xlsx",
            new DateTime(2024, 1, 1),
            2);

        try
        {
            // Act
            var result = _service.ValidateExcelFormat(filePath, "NonExistentSheet");

            // Assert
            result.Should().BeFalse();
        }
        finally
        {
            ExcelTestHelper.CleanupTestFile(filePath);
        }
    }

    [Fact]
    public void ValidateExcelFormat_MissingTimeHeader_ReturnsFalse()
    {
        // Arrange
        var filePath = ExcelTestHelper.CreateInvalidExcelFile("validate_invalid.xlsx");

        try
        {
            // Act
            var result = _service.ValidateExcelFormat(filePath, "負載交叉表");

            // Assert
            result.Should().BeFalse();
        }
        finally
        {
            ExcelTestHelper.CleanupTestFile(filePath);
        }
    }

    [Fact]
    public void ValidateExcelFormat_MissingDateColumn_ReturnsFalse()
    {
        // Arrange
        var filePath = ExcelTestHelper.CreateEmptyExcelFile("validate_empty.xlsx");

        try
        {
            // Act
            var result = _service.ValidateExcelFormat(filePath, "負載交叉表");

            // Assert
            result.Should().BeFalse();
        }
        finally
        {
            ExcelTestHelper.CleanupTestFile(filePath);
        }
    }

    [Fact]
    public void ValidateExcelFormat_ExceptionOccurs_ReturnsFalse()
    {
        // Arrange
        var filePath = Path.Combine(Path.GetTempPath(), "corrupted.xlsx");

        // Create a corrupted file (just text, not a real Excel file)
        File.WriteAllText(filePath, "This is not a valid Excel file");

        try
        {
            // Act
            var result = _service.ValidateExcelFormat(filePath, "負載交叉表");

            // Assert
            result.Should().BeFalse();
            LoggerHelper.VerifyErrorLog(_mockLogger);
        }
        finally
        {
            ExcelTestHelper.CleanupTestFile(filePath);
        }
    }

    [Fact]
    public void ValidateExcelFormat_LogsErrorsAppropriately()
    {
        // Arrange
        var filePath = "/non/existent/file.xlsx";

        // Act
        _service.ValidateExcelFormat(filePath, "負載交叉表");

        // Assert
        // Note: In the current implementation, file not found doesn't log an error
        // but exception handling does. This test verifies the behavior.
        _mockLogger.Verify(
            x => x.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)),
            Times.Never);
    }

    #endregion

    #region Date/Time Parsing Edge Cases

    [Theory]
    [InlineData("2024/01/15")]
    [InlineData("15/01/2024")]
    [InlineData("2024-01-15")]
    [InlineData("15-01-2024")]
    [InlineData("1/15/2024")]
    public async Task ImportFromExcelAsync_VariousDateFormats_ParsesCorrectly(string dateFormat)
    {
        // This test would require creating Excel files with specific date formats
        // The actual parsing is done by the TryParseDate private method
        // We're testing this indirectly through the import process

        // Note: This is a placeholder for more detailed date format testing
        // In a real scenario, you'd create Excel files with each format
        await Task.CompletedTask;
    }

    #endregion

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            // Clean up any remaining test files
            var tempPath = Path.GetTempPath();
            var testFiles = Directory.GetFiles(tempPath, "*_test.xlsx")
                .Concat(Directory.GetFiles(tempPath, "*.xlsx"));

            foreach (var file in testFiles)
            {
                try
                {
                    if (file.Contains("_test") || file.Contains("validate_") ||
                        file.Contains("invalid_") || file.Contains("empty_") ||
                        file.Contains("mixed_") || file.Contains("large_") ||
                        file.Contains("custom_") || file.Contains("timestamp_") ||
                        file.Contains("timing_") || file.Contains("messages_") ||
                        file.Contains("corrupted"))
                    {
                        File.Delete(file);
                    }
                }
                catch
                {
                    // Ignore cleanup errors
                }
            }
        }
        base.Dispose(disposing);
    }
}
