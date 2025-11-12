using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PowerAnalysis.Data;
using PowerAnalysis.Repositories;
using PowerAnalysis.Services;

namespace PowerAnalysis;

/// <summary>
/// 測試 Excel 導入功能的控制台程式
/// </summary>
public class TestImport
{
    // Note: This method can be called from a separate test runner
    public static async Task RunTestAsync()
    {
        Console.WriteLine("=== 負載交叉表導入測試程式 ===\n");

        // 建立日誌工廠
        using var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Information);
        });

        // 設定資料庫連線
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite("Data Source=PowerAnalysis.db")
            .LogTo(Console.WriteLine, LogLevel.Information)
            .Options;

        // 建立 DbContext
        using var context = new ApplicationDbContext(options);

        // 建立 Repository
        var repository = new LoadReadingRepository(context);

        // 建立導入服務
        var logger = loggerFactory.CreateLogger<LoadReadingImportService>();
        var importService = new LoadReadingImportService(repository, logger);

        // Excel 文件路徑
        var filePath = Path.Combine(
            Directory.GetCurrentDirectory(),
            "Data",
            "reference",
            "ElectricityConsumptionDifferenceTable.xlsx"
        );

        Console.WriteLine($"Excel 文件路徑: {filePath}");
        Console.WriteLine($"文件存在: {File.Exists(filePath)}\n");

        if (!File.Exists(filePath))
        {
            Console.WriteLine("❌ 錯誤: Excel 文件不存在！");
            return;
        }

        // 顯示目前資料庫記錄數
        var currentCount = await repository.CountAsync();
        Console.WriteLine($"資料庫目前記錄數: {currentCount}\n");

        // 詢問是否要清空資料庫
        if (currentCount > 0)
        {
            Console.Write("是否要清空現有資料？(y/n): ");
            var response = Console.ReadLine();
            if (response?.ToLower() == "y")
            {
                var firstDate = DateTime.MinValue;
                var lastDate = DateTime.MaxValue;
                await repository.DeleteByDateRangeAsync(firstDate, lastDate);
                await repository.SaveChangesAsync();
                Console.WriteLine("✓ 已清空資料\n");
            }
        }

        // 執行導入
        Console.WriteLine("開始導入 Excel 數據...\n");
        var result = await importService.ImportFromExcelAsync(
            filePath,
            sheetName: "負載交叉表",
            dataSource: "負載交叉表"
        );

        // 顯示結果
        Console.WriteLine("\n=== 導入結果 ===");
        Console.WriteLine($"成功: {(result.IsSuccess ? "✓" : "✗")}");
        Console.WriteLine($"導入記錄數: {result.ImportedCount}");
        Console.WriteLine($"跳過記錄數: {result.SkippedCount}");
        Console.WriteLine($"處理時間: {result.ElapsedMilliseconds} ms");

        if (!string.IsNullOrEmpty(result.ErrorMessage))
        {
            Console.WriteLine($"錯誤訊息: {result.ErrorMessage}");
        }

        if (result.Messages.Count > 0)
        {
            Console.WriteLine("\n詳細訊息:");
            foreach (var message in result.Messages.Take(10))
            {
                Console.WriteLine($"  - {message}");
            }
            if (result.Messages.Count > 10)
            {
                Console.WriteLine($"  ... 還有 {result.Messages.Count - 10} 條訊息");
            }
        }

        // 查詢並顯示部分導入的數據
        if (result.IsSuccess && result.ImportedCount > 0)
        {
            Console.WriteLine("\n=== 資料庫中的前 5 筆記錄 ===");
            var samples = await context.LoadReadings
                .OrderBy(lr => lr.Timestamp)
                .Take(5)
                .ToListAsync();

            foreach (var sample in samples)
            {
                Console.WriteLine($"  {sample.Timestamp:yyyy-MM-dd HH:mm} | 負載: {sample.LoadValue:F3} | 來源: {sample.DataSource}");
            }

            // 統計資訊
            var totalRecords = await repository.CountAsync();
            var minDate = await context.LoadReadings.MinAsync(lr => lr.Timestamp);
            var maxDate = await context.LoadReadings.MaxAsync(lr => lr.Timestamp);
            var avgLoad = await context.LoadReadings.AverageAsync(lr => lr.LoadValue);

            Console.WriteLine("\n=== 統計資訊 ===");
            Console.WriteLine($"總記錄數: {totalRecords}");
            Console.WriteLine($"日期範圍: {minDate:yyyy-MM-dd HH:mm} ~ {maxDate:yyyy-MM-dd HH:mm}");
            Console.WriteLine($"平均負載: {avgLoad:F3}");
        }

        Console.WriteLine("\n測試完成！");
    }
}