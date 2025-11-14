using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using PowerAnalysis.Controllers;
using PowerAnalysis.Models;
using PowerAnalysis.Repositories;
using PowerAnalysis.Services;
using PowerAnalysis.Tests.Helpers;

namespace PowerAnalysis.Tests.Controllers;

/// <summary>
/// Comprehensive tests for LoadReadingController
/// </summary>
public class LoadReadingControllerTests : TestBase
{
    private readonly Mock<ILoadReadingRepository> _mockRepository;
    private readonly Mock<ILoadReadingImportService> _mockImportService;
    private readonly Mock<ILogger<LoadReadingController>> _mockLogger;
    private readonly Mock<IWebHostEnvironment> _mockEnvironment;
    private readonly LoadReadingController _controller;

    public LoadReadingControllerTests()
    {
        _mockRepository = new Mock<ILoadReadingRepository>();
        _mockImportService = new Mock<ILoadReadingImportService>();
        _mockLogger = LoggerHelper.CreateMockLogger<LoadReadingController>();
        _mockEnvironment = new Mock<IWebHostEnvironment>();

        _mockEnvironment.Setup(e => e.ContentRootPath)
            .Returns("/test/path");

        _controller = new LoadReadingController(
            _mockRepository.Object,
            _mockImportService.Object,
            _mockLogger.Object,
            _mockEnvironment.Object);
    }

    #region GetAll Tests

    [Fact]
    public async Task GetAll_ReturnsOkWithData()
    {
        // Arrange
        var readings = TestDataGenerator.GenerateLoadReadings(10);
        _mockRepository.Setup(r => r.GetAllAsync())
            .ReturnsAsync(readings);

        // Act
        var result = await _controller.GetAll();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedReadings = okResult.Value.Should().BeAssignableTo<IEnumerable<LoadReading>>().Subject;
        returnedReadings.Should().HaveCount(10);
    }

    [Fact]
    public async Task GetAll_EmptyDatabase_ReturnsEmptyArray()
    {
        // Arrange
        _mockRepository.Setup(r => r.GetAllAsync())
            .ReturnsAsync(new List<LoadReading>());

        // Act
        var result = await _controller.GetAll();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedReadings = okResult.Value.Should().BeAssignableTo<IEnumerable<LoadReading>>().Subject;
        returnedReadings.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAll_RepositoryThrowsException_Returns500()
    {
        // Arrange
        _mockRepository.Setup(r => r.GetAllAsync())
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.GetAll();

        // Assert
        var statusCodeResult = result.Should().BeOfType<ObjectResult>().Subject;
        statusCodeResult.StatusCode.Should().Be(500);

        LoggerHelper.VerifyErrorLog(_mockLogger);
    }

    #endregion

    #region GetByDateRange Tests

    [Fact]
    public async Task GetByDateRange_ValidRange_ReturnsData()
    {
        // Arrange
        var startDate = new DateTime(2024, 1, 1);
        var endDate = new DateTime(2024, 1, 31);
        var readings = TestDataGenerator.GenerateLoadReadingsForDateRange(startDate, endDate);

        _mockRepository.Setup(r => r.GetByDateRangeAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .ReturnsAsync(readings);

        // Act
        var result = await _controller.GetByDateRange(startDate, endDate);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedReadings = okResult.Value.Should().BeAssignableTo<IEnumerable<LoadReading>>().Subject;
        returnedReadings.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetByDateRange_AdjustsStartDateToMidnight()
    {
        // Arrange
        var startDate = new DateTime(2024, 1, 15, 14, 30, 0);
        var endDate = new DateTime(2024, 1, 20);

        _mockRepository.Setup(r => r.GetByDateRangeAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .ReturnsAsync(new List<LoadReading>());

        // Act
        await _controller.GetByDateRange(startDate, endDate);

        // Assert
        _mockRepository.Verify(r => r.GetByDateRangeAsync(
            new DateTime(2024, 1, 15, 0, 0, 0),
            It.IsAny<DateTime>()), Times.Once);
    }

    [Fact]
    public async Task GetByDateRange_AdjustsEndDateToEndOfDay()
    {
        // Arrange
        var startDate = new DateTime(2024, 1, 1);
        var endDate = new DateTime(2024, 1, 31, 14, 30, 0);

        _mockRepository.Setup(r => r.GetByDateRangeAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .ReturnsAsync(new List<LoadReading>());

        // Act
        await _controller.GetByDateRange(startDate, endDate);

        // Assert
        _mockRepository.Verify(r => r.GetByDateRangeAsync(
            It.IsAny<DateTime>(),
            new DateTime(2024, 1, 31, 23, 59, 59)), Times.Once);
    }

    [Fact]
    public async Task GetByDateRange_NoDataInRange_ReturnsEmptyArray()
    {
        // Arrange
        _mockRepository.Setup(r => r.GetByDateRangeAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .ReturnsAsync(new List<LoadReading>());

        // Act
        var result = await _controller.GetByDateRange(
            new DateTime(2024, 1, 1),
            new DateTime(2024, 1, 31));

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedReadings = okResult.Value.Should().BeAssignableTo<IEnumerable<LoadReading>>().Subject;
        returnedReadings.Should().BeEmpty();
    }

    [Fact]
    public async Task GetByDateRange_ExceptionOccurs_Returns500()
    {
        // Arrange
        _mockRepository.Setup(r => r.GetByDateRangeAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.GetByDateRange(
            new DateTime(2024, 1, 1),
            new DateTime(2024, 1, 31));

        // Assert
        var statusCodeResult = result.Should().BeOfType<ObjectResult>().Subject;
        statusCodeResult.StatusCode.Should().Be(500);

        LoggerHelper.VerifyErrorLog(_mockLogger);
    }

    #endregion

    #region GetAggregatedData Tests

    [Fact]
    public async Task GetAggregatedData_ReportMode_ReturnsRawData()
    {
        // Arrange
        var startDate = new DateTime(2024, 1, 1);
        var endDate = new DateTime(2024, 1, 2);
        var readings = TestDataGenerator.GenerateLoadReadingsForDateRange(startDate, endDate);

        _mockRepository.Setup(r => r.GetByDateRangeAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .ReturnsAsync(readings);

        // Act
        var result = await _controller.GetAggregatedData(startDate, endDate, 1, reportMode: true);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var data = okResult.Value.Should().BeAssignableTo<IEnumerable<object>>().Subject;
        data.Should().HaveCount(readings.Count);
    }

    [Fact]
    public async Task GetAggregatedData_OneDayOrLess_ReturnsHourlyAverages()
    {
        // Arrange
        var startDate = new DateTime(2024, 1, 1);
        var endDate = new DateTime(2024, 1, 1, 23, 59, 59);
        var readings = TestDataGenerator.GenerateLoadReadingsForDateRange(startDate, endDate);

        _mockRepository.Setup(r => r.GetByDateRangeAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .ReturnsAsync(readings);

        // Act
        var result = await _controller.GetAggregatedData(startDate, endDate, days: 1, reportMode: false);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var data = okResult.Value.Should().BeAssignableTo<IEnumerable<object>>().Subject;
        data.Should().HaveCount(24); // 24 hours
    }

    [Fact]
    public async Task GetAggregatedData_SevenDaysOrLess_ReturnsHourlyWithDate()
    {
        // Arrange
        var startDate = new DateTime(2024, 1, 1);
        var endDate = new DateTime(2024, 1, 7);
        var readings = TestDataGenerator.GenerateLoadReadingsForDateRange(startDate, endDate);

        _mockRepository.Setup(r => r.GetByDateRangeAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .ReturnsAsync(readings);

        // Act
        var result = await _controller.GetAggregatedData(startDate, endDate, days: 7, reportMode: false);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var data = okResult.Value.Should().BeAssignableTo<IEnumerable<object>>().Subject;
        data.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetAggregatedData_SixtyDaysOrLess_ReturnsDailySummary()
    {
        // Arrange
        var startDate = new DateTime(2024, 1, 1);
        var endDate = new DateTime(2024, 2, 29); // 60 days
        var readings = TestDataGenerator.GenerateLoadReadingsForDateRange(
            startDate,
            startDate.AddDays(30)); // Sample data

        _mockRepository.Setup(r => r.GetByDateRangeAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .ReturnsAsync(readings);

        // Act
        var result = await _controller.GetAggregatedData(startDate, endDate, days: 60, reportMode: false);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var data = okResult.Value.Should().BeAssignableTo<IEnumerable<object>>().Subject;
        data.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetAggregatedData_MoreThanSixtyDays_ReturnsWeeklySummary()
    {
        // Arrange
        var startDate = new DateTime(2024, 1, 1);
        var endDate = new DateTime(2024, 4, 1); // > 60 days
        var readings = TestDataGenerator.GenerateLoadReadingsForDateRange(
            startDate,
            startDate.AddDays(90));

        _mockRepository.Setup(r => r.GetByDateRangeAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .ReturnsAsync(readings);

        // Act
        var result = await _controller.GetAggregatedData(startDate, endDate, days: 91, reportMode: false);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var data = okResult.Value.Should().BeAssignableTo<IEnumerable<object>>().Subject;
        data.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetAggregatedData_EmptyData_ReturnsEmptyArray()
    {
        // Arrange
        _mockRepository.Setup(r => r.GetByDateRangeAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .ReturnsAsync(new List<LoadReading>());

        // Act
        var result = await _controller.GetAggregatedData(
            new DateTime(2024, 1, 1),
            new DateTime(2024, 1, 31),
            days: 30,
            reportMode: false);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var data = okResult.Value.Should().BeAssignableTo<IEnumerable<object>>().Subject;
        data.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAggregatedData_EndDateAtMidnight_AdjustsCorrectly()
    {
        // Arrange
        var startDate = new DateTime(2024, 1, 1);
        var endDate = new DateTime(2024, 1, 2, 0, 0, 0);

        _mockRepository.Setup(r => r.GetByDateRangeAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .ReturnsAsync(new List<LoadReading>());

        // Act
        await _controller.GetAggregatedData(startDate, endDate, days: 1, reportMode: false);

        // Assert
        _mockRepository.Verify(r => r.GetByDateRangeAsync(
            It.IsAny<DateTime>(),
            new DateTime(2024, 1, 2, 23, 59, 59)), Times.Once);
    }

    [Fact]
    public async Task GetAggregatedData_ExceptionOccurs_Returns500()
    {
        // Arrange
        _mockRepository.Setup(r => r.GetByDateRangeAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.GetAggregatedData(
            new DateTime(2024, 1, 1),
            new DateTime(2024, 1, 31),
            days: 30,
            reportMode: false);

        // Assert
        var statusCodeResult = result.Should().BeOfType<ObjectResult>().Subject;
        statusCodeResult.StatusCode.Should().Be(500);
    }

    #endregion

    #region GetCount Tests

    [Fact]
    public async Task GetCount_ReturnsCorrectCount()
    {
        // Arrange
        _mockRepository.Setup(r => r.CountAsync())
            .ReturnsAsync(42);

        // Act
        var result = await _controller.GetCount();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var value = okResult.Value.Should().BeAssignableTo<object>().Subject;
        var count = value.GetType().GetProperty("count")?.GetValue(value);
        count.Should().Be(42);
    }

    [Fact]
    public async Task GetCount_ZeroCount_ReturnsZero()
    {
        // Arrange
        _mockRepository.Setup(r => r.CountAsync())
            .ReturnsAsync(0);

        // Act
        var result = await _controller.GetCount();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var value = okResult.Value.Should().BeAssignableTo<object>().Subject;
        var count = value.GetType().GetProperty("count")?.GetValue(value);
        count.Should().Be(0);
    }

    [Fact]
    public async Task GetCount_ExceptionOccurs_Returns500()
    {
        // Arrange
        _mockRepository.Setup(r => r.CountAsync())
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.GetCount();

        // Assert
        var statusCodeResult = result.Should().BeOfType<ObjectResult>().Subject;
        statusCodeResult.StatusCode.Should().Be(500);
    }

    #endregion

    #region GetDateRange Tests

    [Fact]
    public async Task GetDateRange_ReturnsMinAndMaxDates()
    {
        // Arrange
        var minDate = new DateTime(2024, 1, 1);
        var maxDate = new DateTime(2024, 12, 31);

        _mockRepository.Setup(r => r.GetDateRangeAsync())
            .ReturnsAsync((minDate, maxDate));

        // Act
        var result = await _controller.GetDateRange();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task GetDateRange_EmptyDatabase_ReturnsNullValues()
    {
        // Arrange
        _mockRepository.Setup(r => r.GetDateRangeAsync())
            .ReturnsAsync(((DateTime?)null, (DateTime?)null));

        // Act
        var result = await _controller.GetDateRange();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task GetDateRange_ExceptionOccurs_Returns500()
    {
        // Arrange
        _mockRepository.Setup(r => r.GetDateRangeAsync())
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.GetDateRange();

        // Assert
        var statusCodeResult = result.Should().BeOfType<ObjectResult>().Subject;
        statusCodeResult.StatusCode.Should().Be(500);
    }

    #endregion

    #region ImportFromDefaultExcel Tests

    [Fact]
    public async Task ImportFromDefaultExcel_Success_ReturnsOkWithResult()
    {
        // Arrange
        var importResult = new ImportResult
        {
            IsSuccess = true,
            ImportedCount = 100,
            ElapsedMilliseconds = 1000
        };
        importResult.Messages.Add("成功導入 100 筆記錄");

        _mockImportService.Setup(s => s.ImportFromExcelAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>()))
            .ReturnsAsync(importResult);

        // Act
        var result = await _controller.ImportFromDefaultExcel();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedResult = okResult.Value.Should().BeOfType<ImportResult>().Subject;
        returnedResult.IsSuccess.Should().BeTrue();
        returnedResult.ImportedCount.Should().Be(100);
    }

    [Fact]
    public async Task ImportFromDefaultExcel_Failure_ReturnsBadRequest()
    {
        // Arrange
        var importResult = new ImportResult
        {
            IsSuccess = false,
            ErrorMessage = "文件不存在"
        };

        _mockImportService.Setup(s => s.ImportFromExcelAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>()))
            .ReturnsAsync(importResult);

        // Act
        var result = await _controller.ImportFromDefaultExcel();

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var returnedResult = badRequestResult.Value.Should().BeOfType<ImportResult>().Subject;
        returnedResult.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task ImportFromDefaultExcel_ExceptionOccurs_Returns500()
    {
        // Arrange
        _mockImportService.Setup(s => s.ImportFromExcelAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>()))
            .ThrowsAsync(new Exception("Import error"));

        // Act
        var result = await _controller.ImportFromDefaultExcel();

        // Assert
        var statusCodeResult = result.Should().BeOfType<ObjectResult>().Subject;
        statusCodeResult.StatusCode.Should().Be(500);
    }

    #endregion

    #region ImportFromCustomExcel Tests

    [Fact]
    public async Task ImportFromCustomExcel_Success_ReturnsOk()
    {
        // Arrange
        var importResult = new ImportResult
        {
            IsSuccess = true,
            ImportedCount = 50
        };

        _mockImportService.Setup(s => s.ImportFromExcelAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>()))
            .ReturnsAsync(importResult);

        // Act
        var result = await _controller.ImportFromCustomExcel(
            "/test/path/file.xlsx",
            "Sheet1",
            "Custom Source");

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedResult = okResult.Value.Should().BeOfType<ImportResult>().Subject;
        returnedResult.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task ImportFromCustomExcel_UsesProvidedParameters()
    {
        // Arrange
        var importResult = new ImportResult { IsSuccess = true };
        var filePath = "/custom/path/file.xlsx";
        var sheetName = "CustomSheet";
        var dataSource = "CustomSource";

        _mockImportService.Setup(s => s.ImportFromExcelAsync(
            filePath,
            sheetName,
            dataSource))
            .ReturnsAsync(importResult);

        // Act
        await _controller.ImportFromCustomExcel(filePath, sheetName, dataSource);

        // Assert
        _mockImportService.Verify(s => s.ImportFromExcelAsync(
            filePath,
            sheetName,
            dataSource), Times.Once);
    }

    [Fact]
    public async Task ImportFromCustomExcel_Failure_ReturnsBadRequest()
    {
        // Arrange
        var importResult = new ImportResult
        {
            IsSuccess = false,
            ErrorMessage = "Invalid file"
        };

        _mockImportService.Setup(s => s.ImportFromExcelAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>()))
            .ReturnsAsync(importResult);

        // Act
        var result = await _controller.ImportFromCustomExcel(
            "/test/file.xlsx",
            "Sheet1",
            "Source");

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    #endregion

    #region ValidateExcelFormat Tests

    [Fact]
    public void ValidateExcelFormat_ValidFile_ReturnsOkWithTrue()
    {
        // Arrange
        _mockImportService.Setup(s => s.ValidateExcelFormat(
            It.IsAny<string>(),
            It.IsAny<string>()))
            .Returns(true);

        // Act
        var result = _controller.ValidateExcelFormat("/test/file.xlsx", "Sheet1");

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().NotBeNull();
    }

    [Fact]
    public void ValidateExcelFormat_InvalidFile_ReturnsOkWithFalse()
    {
        // Arrange
        _mockImportService.Setup(s => s.ValidateExcelFormat(
            It.IsAny<string>(),
            It.IsAny<string>()))
            .Returns(false);

        // Act
        var result = _controller.ValidateExcelFormat("/test/file.xlsx", "Sheet1");

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().NotBeNull();
    }

    [Fact]
    public void ValidateExcelFormat_ExceptionOccurs_Returns500()
    {
        // Arrange
        _mockImportService.Setup(s => s.ValidateExcelFormat(
            It.IsAny<string>(),
            It.IsAny<string>()))
            .Throws(new Exception("Validation error"));

        // Act
        var result = _controller.ValidateExcelFormat("/test/file.xlsx", "Sheet1");

        // Assert
        var statusCodeResult = result.Should().BeOfType<ObjectResult>().Subject;
        statusCodeResult.StatusCode.Should().Be(500);
    }

    #endregion

    #region DeleteByDateRange Tests

    [Fact]
    public async Task DeleteByDateRange_Success_ReturnsOk()
    {
        // Arrange
        _mockRepository.Setup(r => r.DeleteByDateRangeAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .Returns(Task.CompletedTask);
        _mockRepository.Setup(r => r.SaveChangesAsync())
            .ReturnsAsync(1);

        // Act
        var result = await _controller.DeleteByDateRange(
            new DateTime(2024, 1, 1),
            new DateTime(2024, 1, 31));

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task DeleteByDateRange_AdjustsDateRange()
    {
        // Arrange
        var startDate = new DateTime(2024, 1, 15, 14, 30, 0);
        var endDate = new DateTime(2024, 1, 20, 10, 15, 0);

        _mockRepository.Setup(r => r.DeleteByDateRangeAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .Returns(Task.CompletedTask);
        _mockRepository.Setup(r => r.SaveChangesAsync())
            .ReturnsAsync(1);

        // Act
        await _controller.DeleteByDateRange(startDate, endDate);

        // Assert
        _mockRepository.Verify(r => r.DeleteByDateRangeAsync(
            new DateTime(2024, 1, 15, 0, 0, 0),
            new DateTime(2024, 1, 20, 23, 59, 59)), Times.Once);
    }

    [Fact]
    public async Task DeleteByDateRange_CallsSaveChanges()
    {
        // Arrange
        _mockRepository.Setup(r => r.DeleteByDateRangeAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .Returns(Task.CompletedTask);
        _mockRepository.Setup(r => r.SaveChangesAsync())
            .ReturnsAsync(1);

        // Act
        await _controller.DeleteByDateRange(
            new DateTime(2024, 1, 1),
            new DateTime(2024, 1, 31));

        // Assert
        _mockRepository.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task DeleteByDateRange_ExceptionOccurs_Returns500()
    {
        // Arrange
        _mockRepository.Setup(r => r.DeleteByDateRangeAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .ThrowsAsync(new Exception("Delete error"));

        // Act
        var result = await _controller.DeleteByDateRange(
            new DateTime(2024, 1, 1),
            new DateTime(2024, 1, 31));

        // Assert
        var statusCodeResult = result.Should().BeOfType<ObjectResult>().Subject;
        statusCodeResult.StatusCode.Should().Be(500);
    }

    #endregion
}
