using System;
using System.Text;
using TMPro;
using UnityEngine;

/// <summary>
/// Shows the top 8 in one TextMeshPro text. Put it on a leaderboard panel; it
/// refreshes every time the panel is shown. Call Refresh() to reload manually.
/// </summary>
public class LeaderboardDisplay : MonoBehaviour
{
    [SerializeField] TMP_Text leaderboardText;
    const int Slots = 8;

    void OnEnable() => Refresh();

    public async void Refresh()
    {
        leaderboardText.text = "Loading...";
        try
        {
            var entries = await DatabaseManager.Instance.GetLeaderboardAsync();
            if (this == null) return;   // panel closed while loading

            var sb = new StringBuilder();
            for (int i = 0; i < Slots; i++)
            {
                if (i < entries.Count)
                {
                    var e = entries[i];
                    sb.AppendLine($"{i + 1}. {e.Username,-16} {e.FormattedTime}   {e.AchievedAt:MM/dd/yy}");
                }
                else
                {
                    sb.AppendLine($"{i + 1}. ---");
                }
            }
            leaderboardText.text = sb.ToString();
        }
        catch (Exception e)
        {
            Debug.LogError($"[Leaderboard] {e}");
            if (this != null) leaderboardText.text = "Leaderboard unavailable.";
        }
    }
}
