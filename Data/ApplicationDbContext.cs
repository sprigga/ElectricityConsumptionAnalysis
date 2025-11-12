using Microsoft.EntityFrameworkCore;
using PowerAnalysis.Models;

namespace PowerAnalysis.Data;

/// <summary>
/// 應用程式資料庫上下文
/// </summary>
public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    /// <summary>
    /// 負載讀數資料集
    /// </summary>
    public DbSet<LoadReading> LoadReadings { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // 配置 LoadReading 實體
        modelBuilder.Entity<LoadReading>(entity =>
        {
            // 唯一約束：同一時間戳記只能有一筆記錄
            entity.HasIndex(e => e.Timestamp)
                  .IsUnique()
                  .HasDatabaseName("IX_LoadReading_Timestamp_Unique");

            // 建立索引以提升查詢效能
            entity.HasIndex(e => e.Timestamp)
                  .HasDatabaseName("IX_LoadReading_Timestamp");

            // 數據來源索引
            entity.HasIndex(e => e.DataSource)
                  .HasDatabaseName("IX_LoadReading_DataSource");

            // 組合索引：時間範圍查詢優化
            entity.HasIndex(e => new { e.Timestamp, e.DataSource })
                  .HasDatabaseName("IX_LoadReading_Timestamp_DataSource");

            // 設定表格名稱
            entity.ToTable("LoadReadings");
        });
    }
}
