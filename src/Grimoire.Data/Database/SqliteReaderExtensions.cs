using Microsoft.Data.Sqlite;

namespace Grimoire.Data.Database;

/// <summary>
/// Convenience extension methods for SqliteDataReader to allow column-name-based access.
/// </summary>
internal static class SqliteReaderExtensions
{
    public static string GetString(this SqliteDataReader reader, string columnName) =>
        reader.GetString(reader.GetOrdinal(columnName));

    public static int GetInt32(this SqliteDataReader reader, string columnName) =>
        reader.GetInt32(reader.GetOrdinal(columnName));

    public static long GetInt64(this SqliteDataReader reader, string columnName) =>
        reader.GetInt64(reader.GetOrdinal(columnName));

    public static double GetDouble(this SqliteDataReader reader, string columnName) =>
        reader.GetDouble(reader.GetOrdinal(columnName));

    public static float GetFloat(this SqliteDataReader reader, string columnName) =>
        reader.GetFloat(reader.GetOrdinal(columnName));

    public static bool GetBoolean(this SqliteDataReader reader, string columnName) =>
        reader.GetBoolean(reader.GetOrdinal(columnName));

    public static bool IsDBNull(this SqliteDataReader reader, string columnName) =>
        reader.IsDBNull(reader.GetOrdinal(columnName));
}
