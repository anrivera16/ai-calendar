using CalendarManager.API.Data;
using Microsoft.EntityFrameworkCore;

namespace CalendarManager.API.Tests.Helpers;

public static class TestDbContextFactory
{
    private static int _databaseCounter = 0;

    public static AppDbContext Create()
    {
        var dbName = $"TestDb_{Interlocked.Increment(ref _databaseCounter)}_{Guid.NewGuid()}";
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;

        var context = new AppDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    public static void Destroy(AppDbContext context)
    {
        context.Database.EnsureDeleted();
        context.Dispose();
    }
}
