
using HRShiftManagement.Infrastructure.Persistence.Context;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace HRShiftManagement.Infrastructure.Tests.Fixtures;

public sealed class TestDatabaseFixture : IAsyncDisposable
{
    private readonly SqliteConnection _connection;

    public ApplicationDbContext Context { get; }

    public TestDatabaseFixture()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(_connection)
            .Options;

        Context = new ApplicationDbContext(options);

        // Database initialization will be performed
        // through InitializeAsync().
    }

    public async Task InitializeAsync()
    {
        await Context.Database.EnsureCreatedAsync();
    }

    public async ValueTask DisposeAsync()
    {
        await Context.DisposeAsync();
        await _connection.DisposeAsync();
    }
}
