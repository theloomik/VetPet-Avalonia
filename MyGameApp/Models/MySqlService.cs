using System;
using System.Threading.Tasks;
using MySqlConnector;

namespace MyGameApp.Models;

public class MySqlService
{
    private readonly string _connectionString;

    public MySqlService(string connectionString)
    {
        _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
    }

    public async Task<bool> TestConnectionAsync()
    {
        try
        {
            await using var conn = new MySqlConnection(_connectionString);
            await conn.OpenAsync();
            await conn.CloseAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<string?> GetVersionAsync()
    {
        try
        {
            await using var conn = new MySqlConnection(_connectionString);
            await conn.OpenAsync();
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT VERSION()";
            var version = (await cmd.ExecuteScalarAsync())?.ToString();
            return version;
        }
        catch
        {
            return null;
        }
    }
}
