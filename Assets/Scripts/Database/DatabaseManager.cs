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

    //shows the time as m:ss.mmm
    public string FormattedTime => TimeSpan.FromMilliseconds(FinishTimeMs).ToString(@"m\:ss\.fff");
}

public struct PlayerSession
{
    public int PlayerId;
    public string Username;
    public bool IsAdmin;
}

//one account for the admin screen
public struct AdminPlayerRow
{
    public int PlayerId;
    public string Username;
    public bool IsAdmin;
    public DateTime CreatedAt;
    public int RunCount;
}

//one run for the admin screen
public struct AdminRunRow
{
    public int RunId;
    public string Username;
    public int FinishTimeMs;
    public DateTime AchievedAt;

    public string FormattedTime => TimeSpan.FromMilliseconds(FinishTimeMs).ToString(@"m\:ss\.fff");
}

//thrown when someone who isnt an admin tries to do admin stuff
public class NotAdminException : Exception
{
    public NotAdminException() : base("This account is not an administrator.") { }
}

//all the database stuff for the game, every query uses parameters so no sql injection
//use it with await, ex: var top = await DatabaseManager.Instance.GetLeaderboardAsync();
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

    //quick check that we can connect
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

    //accounts

    //false if the username is taken
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

    //null if the username or password is wrong
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

    //runs and leaderboard

    //saves a finished run, the db sets the date
    public async Task SubmitRunAsync(int playerId, int finishTimeMs)
    {
        await using var conn = await OpenAsync();
        await using var cmd = new MySqlCommand(
            "INSERT INTO runs (player_id, finish_time_ms) VALUES (@p, @t)", conn);
        cmd.Parameters.AddWithValue("@p", playerId);
        cmd.Parameters.AddWithValue("@t", finishTimeMs);
        await cmd.ExecuteNonQueryAsync();
    }

    //top 8, only each players best time
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

    //players best time, null if they never finished
    public async Task<int?> GetPersonalBestAsync(int playerId)
    {
        await using var conn = await OpenAsync();
        await using var cmd = new MySqlCommand(
            "SELECT MIN(finish_time_ms) FROM runs WHERE player_id = @p", conn);
        cmd.Parameters.AddWithValue("@p", playerId);
        object result = await cmd.ExecuteScalarAsync();
        return result is DBNull or null ? null : Convert.ToInt32(result);
    }

    //admin stuff
    //every admin method checks is_admin in the db first so someone who got demoted cant keep using it

    async Task RequireAdminAsync(MySqlConnection conn, int adminId)
    {
        await using var cmd = new MySqlCommand(
            "SELECT is_admin FROM players WHERE player_id = @a", conn);
        cmd.Parameters.AddWithValue("@a", adminId);
        object result = await cmd.ExecuteScalarAsync();
        if (result is DBNull or null || !Convert.ToBoolean(result)) throw new NotAdminException();
    }

    //checks the db directly instead of the saved session
    public async Task<bool> IsAdminAsync(int playerId)
    {
        await using var conn = await OpenAsync();
        try { await RequireAdminAsync(conn, playerId); return true; }
        catch (NotAdminException) { return false; }
    }

    //all accounts, admins first then a-z
    public async Task<List<AdminPlayerRow>> AdminGetPlayersAsync(int adminId)
    {
        await using var conn = await OpenAsync();
        await RequireAdminAsync(conn, adminId);

        await using var cmd = new MySqlCommand(
            @"SELECT p.player_id, p.username, p.is_admin, p.created_at, COUNT(r.run_id) AS run_count
              FROM players p LEFT JOIN runs r ON r.player_id = p.player_id
              GROUP BY p.player_id, p.username, p.is_admin, p.created_at
              ORDER BY p.is_admin DESC, p.username", conn);
        var rows = new List<AdminPlayerRow>();
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            rows.Add(new AdminPlayerRow
            {
                PlayerId = reader.GetInt32("player_id"),
                Username = reader.GetString("username"),
                IsAdmin = reader.GetBoolean("is_admin"),
                CreatedAt = reader.GetDateTime("created_at"),
                RunCount = reader.GetInt32("run_count"),
            });
        }
        return rows;
    }

    //all runs, fastest first
    public async Task<List<AdminRunRow>> AdminGetRunsAsync(int adminId, int limit = 100)
    {
        await using var conn = await OpenAsync();
        await RequireAdminAsync(conn, adminId);

        await using var cmd = new MySqlCommand(
            @"SELECT r.run_id, p.username, r.finish_time_ms, r.achieved_at
              FROM runs r JOIN players p ON p.player_id = r.player_id
              ORDER BY r.finish_time_ms, r.achieved_at
              LIMIT @lim", conn);
        cmd.Parameters.AddWithValue("@lim", limit);
        var rows = new List<AdminRunRow>();
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            rows.Add(new AdminRunRow
            {
                RunId = reader.GetInt32("run_id"),
                Username = reader.GetString("username"),
                FinishTimeMs = reader.GetInt32("finish_time_ms"),
                AchievedAt = reader.GetDateTime("achieved_at"),
            });
        }
        return rows;
    }

    //deletes a run (like a cheated time), false if it was already gone
    public async Task<bool> AdminDeleteRunAsync(int adminId, int runId)
    {
        await using var conn = await OpenAsync();
        await RequireAdminAsync(conn, adminId);

        await using var cmd = new MySqlCommand("DELETE FROM runs WHERE run_id = @r", conn);
        cmd.Parameters.AddWithValue("@r", runId);
        return await cmd.ExecuteNonQueryAsync() > 0;
    }

    //make or remove an admin, cant change your own so nobody locks themselves out
    public async Task<bool> AdminSetAdminAsync(int adminId, int targetPlayerId, bool makeAdmin)
    {
        if (adminId == targetPlayerId)
            throw new InvalidOperationException("You can't change your own admin status.");

        await using var conn = await OpenAsync();
        await RequireAdminAsync(conn, adminId);

        await using var cmd = new MySqlCommand(
            "UPDATE players SET is_admin = @v WHERE player_id = @p", conn);
        cmd.Parameters.AddWithValue("@v", makeAdmin);
        cmd.Parameters.AddWithValue("@p", targetPlayerId);
        return await cmd.ExecuteNonQueryAsync() > 0;
    }

    //deletes the account and all its runs, cant delete yourself
    public async Task<bool> AdminDeletePlayerAsync(int adminId, int targetPlayerId)
    {
        if (adminId == targetPlayerId)
            throw new InvalidOperationException("You can't delete your own account.");

        await using var conn = await OpenAsync();
        await RequireAdminAsync(conn, adminId);

        await using var cmd = new MySqlCommand("DELETE FROM players WHERE player_id = @p", conn);
        cmd.Parameters.AddWithValue("@p", targetPlayerId);
        return await cmd.ExecuteNonQueryAsync() > 0;
    }
}

//password hashing (PBKDF2), saved as iterations.salt.hash
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

        //compare every byte so timing doesnt give anything away
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
