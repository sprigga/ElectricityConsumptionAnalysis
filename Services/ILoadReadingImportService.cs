namespace PowerAnalysis.Services;

/// <summary>
/// 負載讀數導入服務介面
/// </summary>
public interface ILoadReadingImportService
{
    /// <summary>
    /// 從 Excel 文件導入負載交叉表數據
    /// </summary>
    /// <param name="filePath">Excel 文件路徑</param>
    /// <param name="sheetName">工作表名稱</param>
    /// <param name="dataSource">數據來源標識</param>
    /// <returns>導入結果</returns>
    Task<ImportResult> ImportFromExcelAsync(string filePath, string sheetName = "負載交叉表", string dataSource = "負載交叉表");

    /// <summary>
    /// 驗證 Excel 文件格式
    /// </summary>
    bool ValidateExcelFormat(string filePath, string sheetName);
}

/// <summary>
/// 導入結果
/// </summary>
public class ImportResult
{
    /// <summary>
    /// 是否成功
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// 導入的記錄數
    /// </summary>
    public int ImportedCount { get; set; }

    /// <summary>
    /// 跳過的記錄數（重複或無效）
    /// </summary>
    public int SkippedCount { get; set; }

    /// <summary>
    /// 錯誤訊息
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// 詳細訊息列表
    /// </summary>
    public List<string> Messages { get; set; } = new();

    /// <summary>
    /// 處理耗時（毫秒）
    /// </summary>
    public long ElapsedMilliseconds { get; set; }
}
