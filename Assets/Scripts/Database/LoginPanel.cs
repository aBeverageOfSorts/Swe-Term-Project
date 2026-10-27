using System;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Login / register screen. Put this on the panel in the Login scene and drag the
/// UI elements into the slots in the Inspector.
/// </summary>
public class LoginPanel : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] TMP_InputField usernameInput;
    [SerializeField] TMP_InputField passwordInput;   // set Content Type = Password in the Inspector
    [SerializeField] Button loginButton;
    [SerializeField] Button registerButton;
    [SerializeField] TMP_Text statusText;

    [Header("Flow")]
    [Tooltip("Exact name of the scene to load after logging in. Must be in the build's scene list.")]
    [SerializeField] string sceneAfterLogin = "SampleScene";

    const int MinPasswordLength = 6;
    static readonly Regex UsernameRule = new Regex("^[A-Za-z0-9_]{3,32}$");

    void Awake()
    {
        loginButton.onClick.AddListener(OnLoginClicked);
        registerButton.onClick.AddListener(OnRegisterClicked);
        statusText.text = "";
    }

    async void OnLoginClicked()
    {
        string username = usernameInput.text.Trim();
        string password = passwordInput.text;
        if (username.Length == 0 || password.Length == 0)
        {
            ShowStatus("Enter a username and password.");
            return;
        }

        SetBusy(true, "Logging in...");
        try
        {
            PlayerSession? session = await DatabaseManager.Instance.LoginAsync(username, password);
            if (session == null)
            {
                ShowStatus("Wrong username or password.");
                return;
            }
            EnterGame(session.Value);
        }
        catch (Exception e)
        {
            Debug.LogError($"[Login] {e}");
            ShowStatus("Can't reach the server. Is MariaDB running?");
        }
        finally
        {
            if (this != null) SetBusy(false);   // panel may be gone if the scene changed
        }
    }

    async void OnRegisterClicked()
    {
        string username = usernameInput.text.Trim();
        string password = passwordInput.text;
        if (!UsernameRule.IsMatch(username))
        {
            ShowStatus("Username: 3-32 letters, numbers or _");
            return;
        }
        if (password.Length < MinPasswordLength)
        {
            ShowStatus($"Password must be at least {MinPasswordLength} characters.");
            return;
        }

        SetBusy(true, "Creating account...");
        try
        {
            bool created = await DatabaseManager.Instance.RegisterPlayerAsync(username, password);
            if (!created)
            {
                ShowStatus("That username is taken.");
                return;
            }
            // Log straight in after registering
            PlayerSession? session = await DatabaseManager.Instance.LoginAsync(username, password);
            if (session != null) EnterGame(session.Value);
        }
        catch (Exception e)
        {
            Debug.LogError($"[Register] {e}");
            ShowStatus("Can't reach the server. Is MariaDB running?");
        }
        finally
        {
            if (this != null) SetBusy(false);
        }
    }

    void EnterGame(PlayerSession session)
    {
        GameSession.SignIn(session);
        Debug.Log($"[Login] Signed in as {session.Username} (admin: {session.IsAdmin})");
        SceneManager.LoadScene(sceneAfterLogin);
    }

    void SetBusy(bool busy, string message = null)
    {
        loginButton.interactable = !busy;
        registerButton.interactable = !busy;
        if (message != null) statusText.text = message;
    }

    void ShowStatus(string message)
    {
        if (statusText != null) statusText.text = message;
    }
}
