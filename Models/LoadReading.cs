using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PowerAnalysis.Models;

/// <summary>
/// 負載讀數模型 - 儲存時間序列的負載數據
/// </summary>
public class LoadReading
{
    /// <summary>
    /// 主鍵
    /// </summary>
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// 時間戳記 (精確到分鐘)
    /// </summary>
    [Required]
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// 負載值 (單位: MW 或 kW，依數據源而定)
    /// </summary>
    [Required]
    [Column(TypeName = "decimal(18,3)")]
    public decimal LoadValue { get; set; }

    /// <summary>
    /// 數據來源 (選填)
    /// </summary>
    [StringLength(100)]
    public string? DataSource { get; set; }

    /// <summary>
    /// 數據導入時間
    /// </summary>
    [Required]
    public DateTime ImportedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 備註
    /// </summary>
    [StringLength(500)]
    public string? Remarks { get; set; }
}
