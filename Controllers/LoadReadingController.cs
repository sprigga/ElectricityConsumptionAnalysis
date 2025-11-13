using Microsoft.AspNetCore.Mvc;
using PowerAnalysis.Repositories;
using PowerAnalysis.Services;

namespace PowerAnalysis.Controllers;

/// <summary>
/// 負載讀數 API Controller
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class LoadReadingController : ControllerBase
{
    private readonly ILoadReadingRepository _repository;
    private readonly ILoadReadingImportService _importService;
    private readonly ILogger<LoadReadingController> _logger;
    private readonly IWebHostEnvironment _environment;

    public LoadReadingController(
        ILoadReadingRepository repository,
        ILoadReadingImportService importService,
        ILogger<LoadReadingController> logger,
        IWebHostEnvironment environment)
    {
        _repository = repository;
        _importService = importService;
        _logger = logger;
        _environment = environment;
    }

    /// <summary>
    /// 取得所有負載讀數
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        try
        {
            var loadReadings = await _repository.GetAllAsync();
            return Ok(loadReadings);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "取得負載讀數時發生錯誤");
            return StatusCode(500, new { error = "內部伺服器錯誤", message = ex.Message });
        }
    }

    /// <summary>
    /// 根據日期範圍取得負載讀數
    /// </summary>
    [HttpGet("range")]
    public async Task<IActionResult> GetByDateRange(
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate)
    {
        try
        {
            // 確保開始日期從當天 00:00:00 開始
            var adjustedStartDate = startDate.Date;
            // 確保結束日期包含整天
            var adjustedEndDate = endDate.Date.AddDays(1).AddSeconds(-1);

            var loadReadings = await _repository.GetByDateRangeAsync(adjustedStartDate, adjustedEndDate);
            return Ok(loadReadings);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "取得日期範圍負載讀數時發生錯誤");
            return StatusCode(500, new { error = "內部伺服器錯誤", message = ex.Message });
        }
    }

    /// <summary>
    /// 根據日期範圍取得聚合的負載讀數（用於圖表顯示）
    /// </summary>
    /// <param name="reportMode">是否為報表模式（true: 以30分鐘間隔顯示, false: 按照天數邏輯顯示）</param>
    [HttpGet("aggregated")]
    public async Task<IActionResult> GetAggregatedData(
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate,
        [FromQuery] int days,
        [FromQuery] bool reportMode = false)
    {
        try
        {
            // 原有：強制調整為整天範圍
            // var adjustedStartDate = startDate.Date;
            // var adjustedEndDate = endDate.Date.AddDays(1).AddSeconds(-1);

            // 修改：支援自訂時間，如果有指定時間則使用，否則預設為整天
            // 前端會傳送包含時間的 DateTime，所以直接使用傳入的值
            var adjustedStartDate = startDate;

            // 修正：如果結束時間是 00:00:00，調整為前一天的 23:59:59
            // 這樣可以確保包含整天的資料，而不是只查到 00:00 這個時間點
            var adjustedEndDate = endDate.TimeOfDay == TimeSpan.Zero && endDate > startDate
                ? endDate.AddDays(1).AddSeconds(-1)
                : endDate;

            var loadReadings = await _repository.GetByDateRangeAsync(adjustedStartDate, adjustedEndDate);
            var dataList = loadReadings.ToList();

            if (dataList.Count == 0)
            {
                return Ok(new List<object>());
            }

            List<object> result;

            // 新增：報表模式 - 返回所有原始資料（每30分鐘一筆）
            if (reportMode)
            {
                result = dataList
                    .OrderBy(r => r.Timestamp)
                    .Select(r => new
                    {
                        id = r.Id,
                        timestamp = r.Timestamp,
                        label = r.Timestamp.ToString("yyyy-MM-dd HH:mm:ss"),
                        value = r.LoadValue,
                        dataSource = r.DataSource,
                        importedAt = r.ImportedAt
                    })
                    .Cast<object>()
                    .ToList();
            }
            // 原有：圖表顯示邏輯 - 一週內：按小時顯示（包含日期和小時）
            else if (days <= 7)
            {
                result = dataList
                    .GroupBy(r => new
                    {
                        Date = r.Timestamp.Date,
                        Hour = r.Timestamp.Hour
                    })
                    .OrderBy(g => g.Key.Date)
                    .ThenBy(g => g.Key.Hour)
                    .Select(g => new
                    {
                        // 當天數為1時只顯示小時，多天時顯示日期+小時
                        label = days <= 1
                            ? $"{g.Key.Hour:D2}:00"
                            : $"{g.Key.Date:MM/dd} {g.Key.Hour:D2}:00",
                        value = g.Average(r => r.LoadValue)
                    })
                    .Cast<object>()
                    .ToList();
            }
            else if (days <= 60)
            {
                // 修改：一週到兩個月：按日期顯示（每天平均和總和）
                // 原有邏輯：只返回平均值
                // result = dataList
                //     .GroupBy(r => r.Timestamp.Date)
                //     .OrderBy(g => g.Key)
                //     .Select(g => new
                //     {
                //         label = g.Key.ToString("MM/dd"),
                //         value = g.Average(r => r.LoadValue)
                //     })
                //     .Cast<object>()
                //     .ToList();

                // 新邏輯：返回平均值和總和
                result = dataList
                    .GroupBy(r => r.Timestamp.Date)
                    .OrderBy(g => g.Key)
                    .Select(g => new
                    {
                        label = g.Key.ToString("MM/dd"),
                        average = g.Average(r => r.LoadValue),  // 每日平均值
                        total = g.Sum(r => r.LoadValue),        // 每日總和
                        count = g.Count()                        // 資料點數量（供參考）
                    })
                    .Cast<object>()
                    .ToList();
            }
            else
            {
                // 修改：超過兩個月：按週顯示（每週平均和總和）
                // 原有邏輯：只返回平均值
                // result = dataList
                //     .GroupBy(r =>
                //     {
                //         // 計算週數（從 startDate 開始計算）
                //         var weekNumber = (int)Math.Floor((r.Timestamp.Date - startDate.Date).TotalDays / 7);
                //         var weekStart = startDate.Date.AddDays(weekNumber * 7);
                //         return new { WeekNumber = weekNumber + 1, WeekStart = weekStart };
                //     })
                //     .OrderBy(g => g.Key.WeekNumber)
                //     .Select(g => new
                //     {
                //         label = $"第 {g.Key.WeekNumber} 週",
                //         value = g.Average(r => r.LoadValue)
                //     })
                //     .Cast<object>()
                //     .ToList();

                // 新邏輯：返回平均值和總和
                result = dataList
                    .GroupBy(r =>
                    {
                        // 計算週數（從 startDate 開始計算）
                        var weekNumber = (int)Math.Floor((r.Timestamp.Date - startDate.Date).TotalDays / 7);
                        var weekStart = startDate.Date.AddDays(weekNumber * 7);
                        return new { WeekNumber = weekNumber + 1, WeekStart = weekStart };
                    })
                    .OrderBy(g => g.Key.WeekNumber)
                    .Select(g => new
                    {
                        label = $"第 {g.Key.WeekNumber} 週",
                        average = g.Average(r => r.LoadValue),  // 每週平均值
                        total = g.Sum(r => r.LoadValue),        // 每週總和
                        count = g.Count()                        // 資料點數量（供參考）
                    })
                    .Cast<object>()
                    .ToList();
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "取得聚合負載讀數時發生錯誤");
            return StatusCode(500, new { error = "內部伺服器錯誤", message = ex.Message });
        }
    }

    /// <summary>
    /// 取得記錄總數
    /// </summary>
    [HttpGet("count")]
    public async Task<IActionResult> GetCount()
    {
        try
        {
            var count = await _repository.CountAsync();
            return Ok(new { count });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "取得記錄總數時發生錯誤");
            return StatusCode(500, new { error = "內部伺服器錯誤", message = ex.Message });
        }
    }

    /// <summary>
    /// 取得資料庫中的日期範圍
    /// </summary>
    [HttpGet("daterange")]
    public async Task<IActionResult> GetDateRange()
    {
        try
        {
            var (minDate, maxDate) = await _repository.GetDateRangeAsync();
            return Ok(new { minDate, maxDate });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "取得日期範圍時發生錯誤");
            return StatusCode(500, new { error = "內部伺服器錯誤", message = ex.Message });
        }
    }

    /// <summary>
    /// 從預設 Excel 文件導入負載交叉表數據
    /// </summary>
    [HttpPost("import")]
    public async Task<IActionResult> ImportFromDefaultExcel()
    {
        try
        {
            // 預設文件路徑
            var filePath = Path.Combine(
                _environment.ContentRootPath,
                "Data",
                "reference",
                "ElectricityConsumptionDifferenceTable.xlsx"
            );

            _logger.LogInformation("開始從預設文件導入: {FilePath}", filePath);

            var result = await _importService.ImportFromExcelAsync(filePath);

            if (result.IsSuccess)
            {
                return Ok(result);
            }
            else
            {
                return BadRequest(result);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "導入數據時發生錯誤");
            return StatusCode(500, new { error = "導入失敗", message = ex.Message });
        }
    }

    /// <summary>
    /// 從指定 Excel 文件導入數據
    /// </summary>
    [HttpPost("import/custom")]
    public async Task<IActionResult> ImportFromCustomExcel(
        [FromQuery] string filePath,
        [FromQuery] string sheetName = "負載交叉表",
        [FromQuery] string dataSource = "負載交叉表")
    {
        try
        {
            _logger.LogInformation("開始從自訂文件導入: {FilePath}", filePath);

            var result = await _importService.ImportFromExcelAsync(filePath, sheetName, dataSource);

            if (result.IsSuccess)
            {
                return Ok(result);
            }
            else
            {
                return BadRequest(result);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "導入數據時發生錯誤");
            return StatusCode(500, new { error = "導入失敗", message = ex.Message });
        }
    }

    /// <summary>
    /// 驗證 Excel 文件格式
    /// </summary>
    [HttpPost("validate")]
    public IActionResult ValidateExcelFormat(
        [FromQuery] string filePath,
        [FromQuery] string sheetName = "負載交叉表")
    {
        try
        {
            var isValid = _importService.ValidateExcelFormat(filePath, sheetName);
            return Ok(new { isValid });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "驗證 Excel 格式時發生錯誤");
            return StatusCode(500, new { error = "驗證失敗", message = ex.Message });
        }
    }

    /// <summary>
    /// 刪除指定日期範圍的數據
    /// </summary>
    [HttpDelete("range")]
    public async Task<IActionResult> DeleteByDateRange(
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate)
    {
        try
        {
            // 確保開始日期從當天 00:00:00 開始
            var adjustedStartDate = startDate.Date;
            // 確保結束日期包含整天
            var adjustedEndDate = endDate.Date.AddDays(1).AddSeconds(-1);

            await _repository.DeleteByDateRangeAsync(adjustedStartDate, adjustedEndDate);
            await _repository.SaveChangesAsync();
            return Ok(new { message = "刪除成功" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "刪除數據時發生錯誤");
            return StatusCode(500, new { error = "刪除失敗", message = ex.Message });
        }
    }
}
