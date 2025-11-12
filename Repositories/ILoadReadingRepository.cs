using PowerAnalysis.Models;

namespace PowerAnalysis.Repositories;

/// <summary>
/// 負載讀數 Repository 介面
/// </summary>
public interface ILoadReadingRepository
{
    /// <summary>
    /// 取得所有負載讀數
    /// </summary>
    Task<IEnumerable<LoadReading>> GetAllAsync();

    /// <summary>
    /// 根據 ID 取得負載讀數
    /// </summary>
    Task<LoadReading?> GetByIdAsync(int id);

    /// <summary>
    /// 根據時間範圍取得負載讀數
    /// </summary>
    Task<IEnumerable<LoadReading>> GetByDateRangeAsync(DateTime startDate, DateTime endDate);

    /// <summary>
    /// 根據數據來源取得負載讀數
    /// </summary>
    Task<IEnumerable<LoadReading>> GetByDataSourceAsync(string dataSource);

    /// <summary>
    /// 檢查指定時間戳記是否已存在
    /// </summary>
    Task<bool> ExistsAsync(DateTime timestamp);

    /// <summary>
    /// 新增單筆負載讀數
    /// </summary>
    Task AddAsync(LoadReading entity);

    /// <summary>
    /// 批量新增負載讀數
    /// </summary>
    Task AddRangeAsync(IEnumerable<LoadReading> entities);

    /// <summary>
    /// 更新負載讀數
    /// </summary>
    void Update(LoadReading entity);

    /// <summary>
    /// 刪除負載讀數
    /// </summary>
    Task DeleteAsync(int id);

    /// <summary>
    /// 刪除指定時間範圍的負載讀數
    /// </summary>
    Task DeleteByDateRangeAsync(DateTime startDate, DateTime endDate);

    /// <summary>
    /// 取得記錄總數
    /// </summary>
    Task<int> CountAsync();

    /// <summary>
    /// 取得資料庫中的最小和最大日期
    /// </summary>
    Task<(DateTime? MinDate, DateTime? MaxDate)> GetDateRangeAsync();

    /// <summary>
    /// 儲存變更
    /// </summary>
    Task<int> SaveChangesAsync();
}
