using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Solicitudes.Core.Data;

namespace Solicitudes.Tests.Support;

public sealed class TestDatabase : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<SolicitudesDbContext> _options;

    public TestDatabase()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        _options = new DbContextOptionsBuilder<SolicitudesDbContext>()
            .UseSqlite(_connection)
            .Options;

        using var context = CreateContext();
        context.Database.EnsureCreated();
    }

    public SolicitudesDbContext CreateContext()
    {
        return new SolicitudesDbContext(_options);
    }

    public void Dispose()
    {
        _connection.Dispose();
    }
}