using OfficeOpenXml;
using System.Globalization;

namespace PowerAnalysis.Tests.Helpers;

/// <summary>
/// Helper class for creating test Excel files
/// </summary>
public static class ExcelTestHelper
{
    static ExcelTestHelper()
    {
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
    }

    /// <summary>
    /// Create a valid test Excel file with sample data
    /// </summary>
    public static string CreateValidExcelFile(
        string fileName,
        DateTime startDate,
        int daysCount,
        string sheetName = "負載交叉表")
    {
        var filePath = Path.Combine(Path.GetTempPath(), fileName);

        using var package = new ExcelPackage();
        var worksheet = package.Workbook.Worksheets.Add(sheetName);

        // Header row - "Time" in A1
        worksheet.Cells[1, 1].Value = "Time";

        // Date headers (starting from B1)
        for (int day = 0; day < daysCount; day++)
        {
            var date = startDate.AddDays(day);
            worksheet.Cells[1, day + 2].Value = date.ToString("yyyy/MM/dd");
        }

        // Time column and data
        var row = 2;
        for (int hour = 0; hour < 24; hour++)
        {
            for (int minute = 0; minute < 60; minute += 30)
            {
                // Time column
                worksheet.Cells[row, 1].Value = $"{hour:D2}:{minute:D2}";

                // Data columns
                for (int day = 0; day < daysCount; day++)
                {
                    var random = new Random(row * 100 + day);
                    var value = 100 + random.NextDouble() * 400;
                    worksheet.Cells[row, day + 2].Value = Math.Round(value, 2);
                }

                row++;
            }
        }

        package.SaveAs(new FileInfo(filePath));
        return filePath;
    }

    /// <summary>
    /// Create an Excel file with invalid format (missing "Time" header)
    /// </summary>
    public static string CreateInvalidExcelFile(string fileName, string sheetName = "負載交叉表")
    {
        var filePath = Path.Combine(Path.GetTempPath(), fileName);

        using var package = new ExcelPackage();
        var worksheet = package.Workbook.Worksheets.Add(sheetName);

        // Invalid header - missing "Time"
        worksheet.Cells[1, 1].Value = "InvalidHeader";
        worksheet.Cells[1, 2].Value = "2024/01/01";

        package.SaveAs(new FileInfo(filePath));
        return filePath;
    }

    /// <summary>
    /// Create an Excel file with invalid date formats
    /// </summary>
    public static string CreateExcelFileWithInvalidDates(
        string fileName,
        string sheetName = "負載交叉表")
    {
        var filePath = Path.Combine(Path.GetTempPath(), fileName);

        using var package = new ExcelPackage();
        var worksheet = package.Workbook.Worksheets.Add(sheetName);

        worksheet.Cells[1, 1].Value = "Time";
        worksheet.Cells[1, 2].Value = "InvalidDate";
        worksheet.Cells[1, 3].Value = "2024-13-45"; // Invalid date

        worksheet.Cells[2, 1].Value = "00:00";
        worksheet.Cells[2, 2].Value = 100.5;

        package.SaveAs(new FileInfo(filePath));
        return filePath;
    }

    /// <summary>
    /// Create an Excel file with invalid time formats
    /// </summary>
    public static string CreateExcelFileWithInvalidTimes(
        string fileName,
        string sheetName = "負載交叉表")
    {
        var filePath = Path.Combine(Path.GetTempPath(), fileName);

        using var package = new ExcelPackage();
        var worksheet = package.Workbook.Worksheets.Add(sheetName);

        worksheet.Cells[1, 1].Value = "Time";
        worksheet.Cells[1, 2].Value = "2024/01/01";

        worksheet.Cells[2, 1].Value = "25:99"; // Invalid time
        worksheet.Cells[2, 2].Value = 100.5;

        worksheet.Cells[3, 1].Value = "InvalidTime";
        worksheet.Cells[3, 2].Value = 200.5;

        package.SaveAs(new FileInfo(filePath));
        return filePath;
    }

    /// <summary>
    /// Create an Excel file with invalid load values
    /// </summary>
    public static string CreateExcelFileWithInvalidValues(
        string fileName,
        string sheetName = "負載交叉表")
    {
        var filePath = Path.Combine(Path.GetTempPath(), fileName);

        using var package = new ExcelPackage();
        var worksheet = package.Workbook.Worksheets.Add(sheetName);

        worksheet.Cells[1, 1].Value = "Time";
        worksheet.Cells[1, 2].Value = "2024/01/01";

        worksheet.Cells[2, 1].Value = "00:00";
        worksheet.Cells[2, 2].Value = "NotANumber";

        worksheet.Cells[3, 1].Value = "00:30";
        worksheet.Cells[3, 2].Value = "ABC";

        package.SaveAs(new FileInfo(filePath));
        return filePath;
    }

    /// <summary>
    /// Create an Excel file with empty data
    /// </summary>
    public static string CreateEmptyExcelFile(string fileName, string sheetName = "負載交叉表")
    {
        var filePath = Path.Combine(Path.GetTempPath(), fileName);

        using var package = new ExcelPackage();
        var worksheet = package.Workbook.Worksheets.Add(sheetName);

        worksheet.Cells[1, 1].Value = "Time";
        worksheet.Cells[1, 2].Value = "2024/01/01";

        // No data rows

        package.SaveAs(new FileInfo(filePath));
        return filePath;
    }

    /// <summary>
    /// Create an Excel file with mixed valid and invalid data
    /// </summary>
    public static string CreateMixedValidityExcelFile(
        string fileName,
        string sheetName = "負載交叉表")
    {
        var filePath = Path.Combine(Path.GetTempPath(), fileName);

        using var package = new ExcelPackage();
        var worksheet = package.Workbook.Worksheets.Add(sheetName);

        worksheet.Cells[1, 1].Value = "Time";
        worksheet.Cells[1, 2].Value = "2024/01/01";
        worksheet.Cells[1, 3].Value = "2024/01/02";

        // Valid row
        worksheet.Cells[2, 1].Value = "00:00";
        worksheet.Cells[2, 2].Value = 100.5;
        worksheet.Cells[2, 3].Value = 105.3;

        // Invalid time
        worksheet.Cells[3, 1].Value = "InvalidTime";
        worksheet.Cells[3, 2].Value = 200.5;

        // Valid row
        worksheet.Cells[4, 1].Value = "01:00";
        worksheet.Cells[4, 2].Value = 150.5;
        worksheet.Cells[4, 3].Value = "NotANumber"; // Invalid value

        // Valid row
        worksheet.Cells[5, 1].Value = "01:30";
        worksheet.Cells[5, 2].Value = 175.5;
        worksheet.Cells[5, 3].Value = 180.2;

        package.SaveAs(new FileInfo(filePath));
        return filePath;
    }

    /// <summary>
    /// Clean up test Excel files
    /// </summary>
    public static void CleanupTestFile(string filePath)
    {
        if (File.Exists(filePath))
        {
            try
            {
                File.Delete(filePath);
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }
}
