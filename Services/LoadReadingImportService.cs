using System.Diagnostics;
using System.Globalization;
using OfficeOpenXml;
using PowerAnalysis.Models;
using PowerAnalysis.Repositories;

namespace PowerAnalysis.Services;

/// <summary>
/// 負載讀數導入服務實作
/// </summary>
public class LoadReadingImportService : ILoadReadingImportService
{
    private readonly ILoadReadingRepository _repository;
    private readonly ILogger<LoadReadingImportService> _logger;

    public LoadReadingImportService(
        ILoadReadingRepository repository,
        ILogger<LoadReadingImportService> logger)
    {
        _repository = repository;
        _logger = logger;

        // 設定 EPPlus 授權 (非商業用途)
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
    }

    public async Task<ImportResult> ImportFromExcelAsync(
        string filePath,
        string sheetName = "負載交叉表",
        string dataSource = "負載交叉表")
    {
        var result = new ImportResult();
        var stopwatch = Stopwatch.StartNew();

        try
        {
            _logger.LogInformation("開始導入 Excel 文件: {FilePath}, 工作表: {SheetName}", filePath, sheetName);

            // 驗證文件是否存在
            if (!File.Exists(filePath))
            {
                result.IsSuccess = false;
                result.ErrorMessage = $"文件不存在: {filePath}";
                _logger.LogError(result.ErrorMessage);
                return result;
            }

            // 讀取 Excel 文件
            using var package = new ExcelPackage(new FileInfo(filePath));
            var worksheet = package.Workbook.Worksheets[sheetName];

            if (worksheet == null)
            {
                result.IsSuccess = false;
                result.ErrorMessage = $"找不到工作表: {sheetName}";
                _logger.LogError(result.ErrorMessage);
                return result;
            }

            // 解析 Excel 數據
            var loadReadings = ParseExcelData(worksheet, dataSource, result);

            if (loadReadings.Count == 0)
            {
                result.IsSuccess = false;
                result.ErrorMessage = "沒有可導入的數據";
                _logger.LogWarning(result.ErrorMessage);
                return result;
            }

            // 批量導入數據
            await _repository.AddRangeAsync(loadReadings);
            await _repository.SaveChangesAsync();

            result.IsSuccess = true;
            result.ImportedCount = loadReadings.Count;
            result.Messages.Add($"成功導入 {result.ImportedCount} 筆記錄");

            _logger.LogInformation("成功導入 {Count} 筆記錄", result.ImportedCount);
        }
        catch (Exception ex)
        {
            result.IsSuccess = false;
            result.ErrorMessage = $"導入失敗: {ex.Message}";
            _logger.LogError(ex, "導入 Excel 時發生錯誤");
        }
        finally
        {
            stopwatch.Stop();
            result.ElapsedMilliseconds = stopwatch.ElapsedMilliseconds;
        }

        return result;
    }

    public bool ValidateExcelFormat(string filePath, string sheetName)
    {
        try
        {
            if (!File.Exists(filePath))
            {
                return false;
            }

            using var package = new ExcelPackage(new FileInfo(filePath));
            var worksheet = package.Workbook.Worksheets[sheetName];

            if (worksheet == null)
            {
                return false;
            }

            // 檢查基本格式
            // 第一列應該是 "Time"，第一行應該包含日期
            var firstCell = worksheet.Cells[1, 1].Value?.ToString();
            if (firstCell != "Time")
            {
                return false;
            }

            // 檢查第二列是否包含日期
            var firstDate = worksheet.Cells[1, 2].Value;
            if (firstDate == null)
            {
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "驗證 Excel 格式時發生錯誤");
            return false;
        }
    }

    /// <summary>
    /// 解析 Excel 數據
    /// </summary>
    private List<LoadReading> ParseExcelData(ExcelWorksheet worksheet, string dataSource, ImportResult result)
    {
        var loadReadings = new List<LoadReading>();

        try
        {
            // 取得工作表的維度
            var rowCount = worksheet.Dimension.Rows;
            var colCount = worksheet.Dimension.Columns;

            _logger.LogInformation("工作表維度: {Rows} 行 x {Cols} 列", rowCount, colCount);

            // 解析日期列（第一行，從第 2 列開始）
            var dates = new List<DateTime>();
            for (int col = 2; col <= colCount; col++)
            {
                var cellValue = worksheet.Cells[1, col].Value;
                if (cellValue != null && TryParseDate(cellValue.ToString(), out DateTime date))
                {
                    dates.Add(date);
                }
                else
                {
                    result.Messages.Add($"警告: 無法解析日期 (列 {col}): {cellValue}");
                }
            }

            _logger.LogInformation("成功解析 {Count} 個日期", dates.Count);

            // 解析時間和負載值（從第 2 行開始）
            for (int row = 2; row <= rowCount; row++)
            {
                // 取得時間（第一列）
                var timeValue = worksheet.Cells[row, 1].Value?.ToString();
                if (string.IsNullOrWhiteSpace(timeValue))
                {
                    continue;
                }

                if (!TryParseTime(timeValue, out TimeSpan time))
                {
                    result.Messages.Add($"警告: 無法解析時間 (行 {row}): {timeValue}");
                    result.SkippedCount++;
                    continue;
                }

                // 遍歷每個日期列
                for (int col = 0; col < dates.Count && (col + 2) <= colCount; col++)
                {
                    var loadValue = worksheet.Cells[row, col + 2].Value;
                    if (loadValue == null)
                    {
                        continue;
                    }

                    if (TryParseDecimal(loadValue.ToString(), out decimal load))
                    {
                        var timestamp = dates[col].Add(time);

                        loadReadings.Add(new LoadReading
                        {
                            Timestamp = timestamp,
                            LoadValue = load,
                            DataSource = dataSource,
                            ImportedAt = DateTime.UtcNow
                        });
                    }
                    else
                    {
                        result.Messages.Add($"警告: 無法解析負載值 (行 {row}, 列 {col + 2}): {loadValue}");
                        result.SkippedCount++;
                    }
                }
            }

            _logger.LogInformation("成功解析 {Count} 筆負載讀數", loadReadings.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "解析 Excel 數據時發生錯誤");
            throw;
        }

        return loadReadings;
    }

    /// <summary>
    /// 嘗試解析日期
    /// </summary>
    private bool TryParseDate(string? dateString, out DateTime date)
    {
        date = DateTime.MinValue;

        if (string.IsNullOrWhiteSpace(dateString))
        {
            return false;
        }

        // 嘗試多種日期格式
        string[] formats = {
            "dd/MM/yyyy",
            "d/M/yyyy",
            "yyyy/MM/dd",
            "yyyy-MM-dd",
            "dd-MM-yyyy",
            "M/d/yyyy"
        };

        return DateTime.TryParseExact(
            dateString,
            formats,
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out date);
    }

    /// <summary>
    /// 嘗試解析時間
    /// </summary>
    private bool TryParseTime(string? timeString, out TimeSpan time)
    {
        time = TimeSpan.Zero;

        if (string.IsNullOrWhiteSpace(timeString))
        {
            return false;
        }

        // 嘗試解析 "HH:mm" 格式
        return TimeSpan.TryParseExact(
            timeString,
            @"hh\:mm",
            CultureInfo.InvariantCulture,
            out time);
    }

    /// <summary>
    /// 嘗試解析十進制數字
    /// </summary>
    private bool TryParseDecimal(string? valueString, out decimal value)
    {
        value = 0m;

        if (string.IsNullOrWhiteSpace(valueString))
        {
            return false;
        }

        return decimal.TryParse(
            valueString,
            NumberStyles.Any,
            CultureInfo.InvariantCulture,
            out value);
    }
}
