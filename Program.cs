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
