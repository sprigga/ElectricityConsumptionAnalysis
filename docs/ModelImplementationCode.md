# 模型类实现代码

## 1. LoadCategory.cs
```csharp
using System.ComponentModel.DataAnnotations;

namespace PowerAnalysis.Models
{
    public class LoadCategory
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;
        
        [StringLength(500)]
        public string? Description { get; set; }
        
        // 导航属性：从此负载类别开始的交叉条目
        public virtual ICollection<LoadCrossEntry> LoadCrossEntriesFrom { get; set; } = new List<LoadCrossEntry>();
        
        // 导航属性：到此负载类别的交叉条目
        public virtual ICollection<LoadCrossEntry> LoadCrossEntriesTo { get; set; } = new List<LoadCrossEntry>();
    }
}
```

## 2. LoadCrossEntry.cs
```csharp
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PowerAnalysis.Models
{
    public class LoadCrossEntry
    {
        [Key]
        public int Id { get; set; }
        
        // 起始负载类别ID (外键)
        [Required]
        public int FromLoadCategoryId { get; set; }
        
        // 目标负载类别ID (外键)
        [Required]
        public int ToLoadCategoryId { get; set; }
        
        // 负载差异值
        [Column(TypeName = "decimal(18,6)")]
        public decimal DifferenceValue { get; set; }
        
        // 时间周期ID (外键) - 可选
        public int? TimePeriodId { get; set; }
        
        // 创建日期
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        
        // 更新日期
        public DateTime UpdatedDate { get; set; } = DateTime.UtcNow;
        
        // 导航属性：起始负载类别
        [ForeignKey("FromLoadCategoryId")]
        public virtual LoadCategory? FromLoadCategory { get; set; }
        
        // 导航属性：目标负载类别
        [ForeignKey("ToLoadCategoryId")]
        public virtual LoadCategory? ToLoadCategory { get; set; }
        
        // 导航属性：时间周期
        [ForeignKey("TimePeriodId")]
        public virtual TimePeriod? TimePeriod { get; set; }
    }
}
```

## 3. TimePeriod.cs
```csharp
using System.ComponentModel.DataAnnotations;

namespace PowerAnalysis.Models
{
    public class TimePeriod
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        [StringLength(100)]
        public string PeriodName { get; set; } = string.Empty;
        
        [StringLength(500)]
        public string? Description { get; set; }
        
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        
        // 导航属性：此时间周期内的交叉条目
        public virtual ICollection<LoadCrossEntry> LoadCrossEntries { get; set; } = new List<LoadCrossEntry>();
    }
}
```

## 4. 数据库上下文配置

### ApplicationDbContext.cs
```csharp
using Microsoft.EntityFrameworkCore;
using PowerAnalysis.Models;

namespace PowerAnalysis.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<LoadCategory> LoadCategories { get; set; }
        public DbSet<LoadCrossEntry> LoadCrossEntries { get; set; }
        public DbSet<TimePeriod> TimePeriods { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // 配置 LoadCrossEntry 实体关系
            modelBuilder.Entity<LoadCrossEntry>()
                .HasOne(lce => lce.FromLoadCategory)
                .WithMany(lc => lc.LoadCrossEntriesFrom)
                .HasForeignKey(lce => lce.FromLoadCategoryId)
                .OnDelete(DeleteBehavior.Restrict); // 防止级联删除

            modelBuilder.Entity<LoadCrossEntry>()
                .HasOne(lce => lce.ToLoadCategory)
                .WithMany(lc => lc.LoadCrossEntriesTo)
                .HasForeignKey(lce => lce.ToLoadCategoryId)
                .OnDelete(DeleteBehavior.Restrict); // 防止级联删除

            modelBuilder.Entity<LoadCrossEntry>()
                .HasOne(lce => lce.TimePeriod)
                .WithMany(tp => tp.LoadCrossEntries)
                .HasForeignKey(lce => lce.TimePeriodId)
                .OnDelete(DeleteBehavior.SetNull); // 删除时间周期时设置为NULL

            // 配置唯一约束：确保相同起始/目标/时间周期的组合唯一
            modelBuilder.Entity<LoadCrossEntry>()
                .HasIndex(lce => new { lce.FromLoadCategoryId, lce.ToLoadCategoryId, lce.TimePeriodId })
                .IsUnique()
                .HasDatabaseName("IX_LoadCrossEntry_FromToTimePeriod");

            base.OnModelCreating(modelBuilder);
        }
    }
}
```

## 5. Repository 接口定义

### ILoadCategoryRepository.cs
```csharp
using PowerAnalysis.Models;

namespace PowerAnalysis.Repositories
{
    public interface ILoadCategoryRepository
    {
        Task<IEnumerable<LoadCategory>> GetAllAsync();
        Task<LoadCategory?> GetByIdAsync(int id);
        Task<LoadCategory?> FindByNameAsync(string name);
        Task AddAsync(LoadCategory entity);
        void Update(LoadCategory entity);
        Task DeleteAsync(int id);
    }
}
```

### ILoadCrossEntryRepository.cs
```csharp
using PowerAnalysis.Models;

namespace PowerAnalysis.Repositories
{
    public interface ILoadCrossEntryRepository
    {
        Task<IEnumerable<LoadCrossEntry>> GetAllAsync();
        Task<LoadCrossEntry?> GetByIdAsync(int id);
        Task<IEnumerable<LoadCrossEntry>> GetByLoadCategoryIdsAsync(int fromId, int toId);
        Task<IEnumerable<LoadCrossEntry>> GetByTimePeriodAsync(int timePeriodId);
        Task AddAsync(LoadCrossEntry entity);
        void Update(LoadCrossEntry entity);
        Task DeleteAsync(int id);
    }
}
```

### ITimePeriodRepository.cs
```csharp
using PowerAnalysis.Models;

namespace PowerAnalysis.Repositories
{
    public interface ITimePeriodRepository
    {
        Task<IEnumerable<TimePeriod>> GetAllAsync();
        Task<TimePeriod?> GetByIdAsync(int id);
        Task<TimePeriod?> FindByNameAsync(string name);
        Task AddAsync(TimePeriod entity);
        void Update(TimePeriod entity);
        Task DeleteAsync(int id);
    }
}
```

## 6. Repository 实现

### LoadCategoryRepository.cs
```csharp
using Microsoft.EntityFrameworkCore;
using PowerAnalysis.Data;
using PowerAnalysis.Models;

namespace PowerAnalysis.Repositories
{
    public class LoadCategoryRepository : ILoadCategoryRepository
    {
        private readonly ApplicationDbContext _context;

        public LoadCategoryRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<LoadCategory>> GetAllAsync()
        {
            return await _context.LoadCategories.ToListAsync();
        }

        public async Task<LoadCategory?> GetByIdAsync(int id)
        {
            return await _context.LoadCategories.FindAsync(id);
        }

        public async Task<LoadCategory?> FindByNameAsync(string name)
        {
            return await _context.LoadCategories.FirstOrDefaultAsync(lc => lc.Name == name);
        }

        public async Task AddAsync(LoadCategory entity)
        {
            await _context.LoadCategories.AddAsync(entity);
        }

        public void Update(LoadCategory entity)
        {
            _context.LoadCategories.Update(entity);
        }

        public async Task DeleteAsync(int id)
        {
            var entity = await _context.LoadCategories.FindAsync(id);
            if (entity != null)
            {
                _context.LoadCategories.Remove(entity);
            }
        }
    }
}
```

这些是规范化的模型实现代码，遵循了数据库规范化原则，避免了数据冗余，确保了数据完整性。