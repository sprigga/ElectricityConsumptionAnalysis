using Microsoft.EntityFrameworkCore;
using PowerAnalysis.Data;
using PowerAnalysis.Tests.Fixtures;

namespace PowerAnalysis.Tests;

/// <summary>
/// Base class for all tests, providing common functionality
/// </summary>
public abstract class TestBase : IDisposable
{
    protected ApplicationDbContext DbContext { get; private set; }
    private bool _disposed = false;

    protected TestBase()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        DbContext = new ApplicationDbContext(options);
        DbContext.Database.EnsureCreated();
    }

    protected ApplicationDbContext CreateFreshContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var context = new ApplicationDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                DbContext?.Database.EnsureDeleted();
                DbContext?.Dispose();
            }
            _disposed = true;
        }
    }
}
