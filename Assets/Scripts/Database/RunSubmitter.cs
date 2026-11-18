using System;
using System.Threading.Tasks;
using UnityEngine;

public struct SubmitResult
{
    public bool Saved;            // false if not logged in or the DB was unreachable
    public bool IsPersonalBest;
    public int TimeMs;
}

/// <summary>
/// Call this once when the player escapes:
///     SubmitResult r = await RunSubmitter.SubmitAsync(elapsedSeconds);
/// </summary>
public static class RunSubmitter
{
    public static async Task<SubmitResult> SubmitAsync(float elapsedSeconds)
    {
        int timeMs = Mathf.RoundToInt(elapsedSeconds * 1000f);
        var result = new SubmitResult { TimeMs = timeMs };

        if (!GameSession.IsLoggedIn)
        {
            Debug.LogWarning("[Run] Not logged in, time not saved.");
            return result;
        }
        if (timeMs <= 0)
        {
            Debug.LogWarning($"[Run] Ignoring invalid time {elapsedSeconds}s.");
            return result;
        }

        try
        {
            int playerId = GameSession.Current.Value.PlayerId;
            int? previousBest = await DatabaseManager.Instance.GetPersonalBestAsync(playerId);
            await DatabaseManager.Instance.SubmitRunAsync(playerId, timeMs);

            result.Saved = true;
            result.IsPersonalBest = previousBest == null || timeMs < previousBest.Value;
            Debug.Log($"[Run] Saved {timeMs} ms (personal best: {result.IsPersonalBest})");
        }
        catch (Exception e)
        {
            Debug.LogError($"[Run] Couldn't save time: {e}");
        }
        return result;
    }
}
