using System;
using System.IO;
using UnityEngine;

/// <summary>
/// Connection settings loaded from StreamingAssets/dbconfig.json.
/// That file is gitignored; dbconfig.example.json is the committed template.
/// </summary>
[Serializable]
public class DbConfig
{
    public string server = "localhost";
    public uint port = 3306;
    public string database = "last_chance";
    public string user = "lastchance_game";
    public string password = "";

    const string FileName = "dbconfig.json";

    public static DbConfig Load()
    {
        string path = Path.Combine(Application.streamingAssetsPath, FileName);
        if (!File.Exists(path))
        {
            throw new FileNotFoundException(
                $"Missing {FileName}. Copy dbconfig.example.json to {FileName} in " +
                "Assets/StreamingAssets and fill in your credentials.", path);
        }
        return JsonUtility.FromJson<DbConfig>(File.ReadAllText(path));
    }
}
