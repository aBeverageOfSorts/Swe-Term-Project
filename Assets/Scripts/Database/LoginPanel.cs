using System;
using System.IO;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

//login and register screen, Tools > Last Chance > Build Login Scene sets all this up
public class LoginPanel : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] TMP_InputField usernameInput;
    [SerializeField] TMP_InputField passwordInput;   //content type has to be Password
    [SerializeField] Button loginButton;
    [SerializeField] Button registerButton;
    [SerializeField] TMP_Text statusText;

    [Header("Flow")]
    [Tooltip("Exact name of the scene to load after logging in. Must be in the build's scene list.")]
    [SerializeField] string sceneAfterLogin = "SampleScene";

    const int MinPasswordLength = 6;
    static readonly Regex UsernameRule = new Regex("^[A-Za-z0-9_]{3,32}$");

    bool _busy;

    void Awake()
    {
        loginButton.onClick.AddListener(OnLoginClicked);
        registerButton.onClick.AddListener(OnRegisterClicked);
        statusText.text = "";

        //stop tab from typing into the box, Update handles tab instead
        usernameInput.onValidateInput += (text, index, c) => c == '\t' ? '\0' : c;
        passwordInput.onValidateInput += (text, index, c) => c == '\t' ? '\0' : c;

        //enter on username goes to password, enter on password logs in
        usernameInput.onSubmit.AddListener(_ => Focus(passwordInput));
        passwordInput.onSubmit.AddListener(_ => OnLoginClicked());
    }

    void Start() => Focus(usernameInput);

    void Update()
    {
        if (!TabPressed()) return;
        Focus(usernameInput.isFocused ? passwordInput : usernameInput);
    }

    static bool TabPressed()
    {
#if ENABLE_INPUT_SYSTEM
        var kb = UnityEngine.InputSystem.Keyboard.current;
        return kb != null && kb.tabKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.Tab);
#endif
    }

    static void Focus(TMP_InputField field)
    {
        field.Select();
        field.ActivateInputField();
    }

    async void OnLoginClicked()
    {
        if (_busy) return;
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
            if (this == null) return;
            if (session == null)
            {
                ShowStatus("Wrong username or password.");
                return;
            }
            EnterGame(session.Value);
        }
        catch (Exception e)
        {
            ShowError("Login", e);
        }
        finally
        {
            //panel might be gone if the scene already changed
            if (this != null) SetBusy(false);
        }
    }

    async void OnRegisterClicked()
    {
        if (_busy) return;
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
            if (this == null) return;
            if (!created)
            {
                ShowStatus("That username is taken.");
                return;
            }
            //log them in right after they register
            PlayerSession? session = await DatabaseManager.Instance.LoginAsync(username, password);
            if (this != null && session != null) EnterGame(session.Value);
        }
        catch (Exception e)
        {
            ShowError("Register", e);
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

    void ShowError(string action, Exception e)
    {
        Debug.LogError($"[{action}] {e}");
        if (this == null) return;
        ShowStatus(e is FileNotFoundException
            ? "Missing dbconfig.json in Assets/StreamingAssets (copy dbconfig.example.json)."
            : "Can't reach the server. Is MariaDB running?");
    }

    void SetBusy(bool busy, string message = null)
    {
        _busy = busy;
        loginButton.interactable = !busy;
        registerButton.interactable = !busy;
        if (message != null) statusText.text = message;
    }

    void ShowStatus(string message)
    {
        if (statusText != null) statusText.text = message;
    }
}
