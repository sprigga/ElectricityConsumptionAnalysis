using Microsoft.EntityFrameworkCore;
using PowerAnalysis.Data;
using PowerAnalysis.Repositories;
using PowerAnalysis.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// 添加 API Controllers 支援
builder.Services.AddControllers();

// 配置資料庫 (使用 SQLite 作為示例，您可以改為 SQL Server)
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    // 使用 SQLite (開發環境)
    options.UseSqlite(
        builder.Configuration.GetConnectionString("DefaultConnection")
        ?? "Data Source=PowerAnalysis.db");

    // 如果要使用 SQL Server，請取消註解以下程式碼並註解上面的 SQLite
    // options.UseSqlServer(
    //     builder.Configuration.GetConnectionString("DefaultConnection")
    //     ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found."));
});

// 註冊 Repository
builder.Services.AddScoped<ILoadReadingRepository, LoadReadingRepository>();

// 訑冊服務
builder.Services.AddScoped<ILoadReadingImportService, LoadReadingImportService>();

// 添加日誌
builder.Services.AddLogging();

var app = builder.Build();

// 自動執行資料庫遷移 (Migration)
// 在應用程式啟動時自動將資料庫結構更新到最新版本
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        // 執行未套用的遷移
        context.Database.Migrate();
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogInformation("資料庫遷移已成功執行");
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "執行資料庫遷移時發生錯誤");
        throw; // 如果遷移失敗，應用程式不應該啟動
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=LoadReadingChart}/{id?}");

app.Run();
