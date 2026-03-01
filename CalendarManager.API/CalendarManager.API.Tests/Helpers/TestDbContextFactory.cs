using CalendarManager.API.Data;
using Microsoft.EntityFrameworkCore;

namespace CalendarManager.API.Tests.Helpers;

public static class TestDbContextFactory
{
    private static int _databaseCounter = 0;

    public static AppDbContext CreateInMemoryDbContext(string? databaseName = null)
    {
        databaseName ??= $"TestDb_{Interlocked.Increment(ref _databaseCounter)}_{Guid.NewGuid():N}";

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName)
            .Options;

        var context = new AppDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    public static async Task<AppDbContext> CreateInMemoryDbContextAsync(string? databaseName = null)
    {
        var context = CreateInMemoryDbContext(databaseName);
        await context.Database.EnsureCreatedAsync();
        return context;
    }

    public static void Destroy(AppDbContext context)
    {
        context.Database.EnsureDeleted();
        context.Dispose();
    }

    public static async Task DestroyAsync(AppDbContext context)
    {
        await context.Database.EnsureDeletedAsync();
        await context.DisposeAsync();
    }
}
