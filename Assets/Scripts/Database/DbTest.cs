using UnityEngine;

public class DbTest : MonoBehaviour
{
    async void Start()
    {
        bool ok = await DatabaseManager.Instance.TestConnectionAsync();
        Debug.Log(ok ? "DB connected" : "DB failed, check Console for the error");

        if (ok)
        {
            var top = await DatabaseManager.Instance.GetLeaderboardAsync();
            Debug.Log($"Leaderboard has {top.Count} entries");
        }
    }
}