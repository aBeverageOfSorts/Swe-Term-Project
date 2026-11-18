/// <summary>
/// Who is logged in right now. Static, so it survives scene loads.
/// Set by LoginPanel; read by RunSubmitter (and later the admin gate).
/// </summary>
public static class GameSession
{
    public static PlayerSession? Current { get; private set; }
    public static bool IsLoggedIn => Current.HasValue;
    public static bool IsAdmin => Current.HasValue && Current.Value.IsAdmin;

    public static void SignIn(PlayerSession session) => Current = session;
    public static void SignOut() => Current = null;
}
