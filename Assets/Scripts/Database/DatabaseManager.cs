using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Threading.Tasks;
using MySqlConnector;
using UnityEngine;

public struct LeaderboardEntry
{
    public string Username;
    public int FinishTimeMs;
    public DateTime AchievedAt;

    /// <summary>Formats as m:ss.mmm for display.</summary>
    public string FormattedTime => TimeSpan.FromMilliseconds(FinishTimeMs).ToString(@"m\:ss\.fff");
}

public struct PlayerSession
{
    public int PlayerId;
    public string Username;
    public bool IsAdmin;
}

/// <summary>
/// All MariaDB access for Last Chance. Every query is parameterized (no string-built SQL).
/// Call from MonoBehaviours with await, e.g.
///   var top = await DatabaseManager.Instance.GetLeaderboardAsync();
/// </summary>
public class DatabaseManager
{
    static DatabaseManager _instance;
    public static DatabaseManager Instance => _instance ??= new DatabaseManager(DbConfig.Load());

    readonly string _connectionString;

    DatabaseManager(DbConfig cfg)
    {
        _connectionString = new MySqlConnectionStringBuilder
        {
            Server = cfg.server,
            Port = cfg.port,
            Database = cfg.database,
            UserID = cfg.user,
            Password = cfg.password,
            ConnectionTimeout = 5,
        }.ConnectionString;
    }

    async Task<MySqlConnection> OpenAsync()
    {
        var conn = new MySqlConnection(_connectionString);
        await conn.OpenAsync();
        return conn;
    }

    /// <summary>Quick connectivity check for a startup screen or debug button.</summary>
    public async Task<bool> TestConnectionAsync()
    {
        try
        {
            await using var conn = await OpenAsync();
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"[DB] Connection failed: {e.Message}");
            return false;
        }
    }

    // ---------- Accounts (data side of registration / login / admin gate) ----------

    /// <summary>Returns false if the username is already taken.</summary>
    public async Task<bool> RegisterPlayerAsync(string username, string password)
    {
        await using var conn = await OpenAsync();
        await using var cmd = new MySqlCommand(
            "INSERT INTO players (username, password_hash) VALUES (@u, @h)", conn);
        cmd.Parameters.AddWithValue("@u", username.Trim());
        cmd.Parameters.AddWithValue("@h", PasswordHasher.Hash(password));
        try
        {
            await cmd.ExecuteNonQueryAsync();
            return true;
        }
        catch (MySqlException e) when (e.ErrorCode == MySqlErrorCode.DuplicateKeyEntry)
        {
            return false;
        }
    }

    /// <summary>Returns the session on success, or null on bad username/password.</summary>
    public async Task<PlayerSession?> LoginAsync(string username, string password)
    {
        await using var conn = await OpenAsync();
        await using var cmd = new MySqlCommand(
            "SELECT player_id, username, password_hash, is_admin FROM players WHERE username = @u", conn);
        cmd.Parameters.AddWithValue("@u", username.Trim());

        await using var reader = await cmd.ExecuteReaderAsync();
        if (!await reader.ReadAsync()) return null;
        if (!PasswordHasher.Verify(password, reader.GetString("password_hash"))) return null;

        return new PlayerSession
        {
            PlayerId = reader.GetInt32("player_id"),
            Username = reader.GetString("username"),
            IsAdmin = reader.GetBoolean("is_admin"),
        };
    }

    // ---------- Runs & leaderboard ----------

    /// <summary>Records a completed escape. achieved_at is set by the server clock.</summary>
    public async Task SubmitRunAsync(int playerId, int finishTimeMs)
    {
        await using var conn = await OpenAsync();
        await using var cmd = new MySqlCommand(
            "INSERT INTO runs (player_id, finish_time_ms) VALUES (@p, @t)", conn);
        cmd.Parameters.AddWithValue("@p", playerId);
        cmd.Parameters.AddWithValue("@t", finishTimeMs);
        await cmd.ExecuteNonQueryAsync();
    }

    /// <summary>Top 8, one entry per player (personal bests).</summary>
    public async Task<List<LeaderboardEntry>> GetLeaderboardAsync()
    {
        var results = new List<LeaderboardEntry>();
        await using var conn = await OpenAsync();
        await using var cmd = new MySqlCommand(
            "SELECT username, finish_time_ms, achieved_at FROM leaderboard_top8", conn);
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            results.Add(new LeaderboardEntry
            {
                Username = reader.GetString("username"),
                FinishTimeMs = reader.GetInt32("finish_time_ms"),
                AchievedAt = reader.GetDateTime("achieved_at"),
            });
        }
        return results;
    }

    /// <summary>A player's best time, or null if they've never finished.</summary>
    public async Task<int?> GetPersonalBestAsync(int playerId)
    {
        await using var conn = await OpenAsync();
        await using var cmd = new MySqlCommand(
            "SELECT MIN(finish_time_ms) FROM runs WHERE player_id = @p", conn);
        cmd.Parameters.AddWithValue("@p", playerId);
        object result = await cmd.ExecuteScalarAsync();
        return result is DBNull or null ? null : Convert.ToInt32(result);
    }
}

/// <summary>PBKDF2-SHA256 password hashing. Stored as "iterations.salt.hash" (base64).</summary>
static class PasswordHasher
{
    const int Iterations = 100_000;
    const int SaltBytes = 16;
    const int HashBytes = 32;

    public static string Hash(string password)
    {
        byte[] salt = new byte[SaltBytes];
        using (var rng = RandomNumberGenerator.Create()) rng.GetBytes(salt);
        byte[] hash = Derive(password, salt, Iterations);
        return $"{Iterations}.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
    }

    public static bool Verify(string password, string stored)
    {
        string[] parts = stored.Split('.');
        if (parts.Length != 3 || !int.TryParse(parts[0], out int iters)) return false;
        byte[] salt = Convert.FromBase64String(parts[1]);
        byte[] expected = Convert.FromBase64String(parts[2]);
        byte[] actual = Derive(password, salt, iters);

        // Constant-time compare
        int diff = expected.Length ^ actual.Length;
        for (int i = 0; i < Math.Min(expected.Length, actual.Length); i++) diff |= expected[i] ^ actual[i];
        return diff == 0;
    }

    static byte[] Derive(string password, byte[] salt, int iterations)
    {
        using var kdf = new Rfc2898DeriveBytes(password, salt, iterations, HashAlgorithmName.SHA256);
        return kdf.GetBytes(HashBytes);
    }
}
