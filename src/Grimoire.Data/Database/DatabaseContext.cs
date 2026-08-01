using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;

namespace Grimoire.Data.Database;

/// <summary>
/// Manages the SQLite connection with WAL mode for crash safety.
/// </summary>
public sealed class DatabaseContext : IDisposable
{
    private readonly string _connectionString;
    private readonly ILogger<DatabaseContext>? _logger;
    private SqliteConnection? _connection;

    public DatabaseContext(string dbPath, ILogger<DatabaseContext>? logger = null)
    {
        _logger = logger;
        _connectionString = $"Data Source={dbPath}";
    }

    /// <summary>Opens (or re-opens) the database connection with WAL mode.</summary>
    public async Task<SqliteConnection> OpenAsync()
    {
        if (_connection is { State: System.Data.ConnectionState.Open })
            return _connection;

        _connection = new SqliteConnection(_connectionString);
        await _connection.OpenAsync();

        // Enable WAL mode for crash safety and concurrent read performance
        var walCmd = _connection.CreateCommand();
        walCmd.CommandText = "PRAGMA journal_mode=WAL; PRAGMA synchronous=NORMAL; PRAGMA foreign_keys=ON;";
        await walCmd.ExecuteNonQueryAsync();

        _logger?.LogInformation("SQLite database opened with WAL mode.");
        return _connection;
    }

    /// <summary>Ensures the connection is open and returns it.</summary>
    public SqliteConnection GetConnection()
    {
        if (_connection is not { State: System.Data.ConnectionState.Open })
            throw new InvalidOperationException("Database not open. Call OpenAsync first.");
        return _connection;
    }

    public void Dispose()
    {
        _connection?.Close();
        _connection?.Dispose();
    }
}
