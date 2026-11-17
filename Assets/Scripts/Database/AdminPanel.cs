using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MySqlConnector;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

//admin screen, press F1 as an admin to open/close it
//sets itself up when the game starts and makes its own ui so nothing to add to a scene
//runs tab lets you delete runs, players tab lets you make/remove admins or delete accounts
//game pauses while its open
public class AdminPanel : MonoBehaviour
{
    const string ToggleKeyName = "F1";
    const float ConfirmWindowSeconds = 3f;

    static AdminPanel _instance;

    enum Tab { Runs, Players }
    Tab _tab = Tab.Runs;

    GameObject _window;
    GameObject _badge;
    TMP_Text _title;
    TMP_Text _status;
    RectTransform _listContent;
    Button _runsTab, _playersTab, _refreshButton;
    bool _busy;
    float _timeScaleBeforeOpen = 1f;

    //for the click twice to delete thing
    Button _pendingConfirm;
    float _pendingUntil;

    static readonly Color Dim = new Color(0f, 0f, 0f, 0.65f);
    static readonly Color WindowColor = new Color(0.11f, 0.12f, 0.15f, 0.98f);
    static readonly Color RowColor = new Color(1f, 1f, 1f, 0.04f);
    static readonly Color ButtonColor = new Color(0.24f, 0.27f, 0.33f);
    static readonly Color TabActiveColor = new Color(0.30f, 0.45f, 0.75f);
    static readonly Color DangerColor = new Color(0.65f, 0.20f, 0.20f);

    public static bool IsOpen => _instance != null && _instance._window != null && _instance._window.activeSelf;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        if (_instance != null) return;
        var go = new GameObject("[AdminPanel]");
        DontDestroyOnLoad(go);
        _instance = go.AddComponent<AdminPanel>();
    }

    void Update()
    {
        bool admin = GameSession.IsAdmin;

        //close it if they logged out or arent an admin
        if (!admin && IsOpen) Close();
        if (_badge != null) _badge.SetActive(admin && !IsOpen);
        if (admin && _badge == null) BuildUi();

        //Sure? goes back to Delete if they dont click again in time
        if (_pendingConfirm != null && Time.unscaledTime >= _pendingUntil) ResetPendingConfirm();

        if (admin && TogglePressed())
        {
            if (IsOpen) Close(); else Open();
        }
    }

    static bool TogglePressed()
    {
#if ENABLE_INPUT_SYSTEM
        var kb = UnityEngine.InputSystem.Keyboard.current;
        return kb != null && kb.f1Key.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.F1);
#endif
    }

    //open and close

    void Open()
    {
        BuildUi();
        EnsureEventSystem();
        _timeScaleBeforeOpen = Time.timeScale;
        Time.timeScale = 0f;
        _window.SetActive(true);
        _title.text = $"Admin  -  signed in as {GameSession.Current.Value.Username}";
        Refresh();
    }

    void Close()
    {
        if (_window == null || !_window.activeSelf) return;
        _window.SetActive(false);
        Time.timeScale = _timeScaleBeforeOpen;
        _pendingConfirm = null;
    }

    void OnDestroy()
    {
        if (IsOpen) Time.timeScale = _timeScaleBeforeOpen;
    }

    //buttons need an eventsystem, only add one if the scene doesnt have one
    static void EnsureEventSystem()
    {
        if (EventSystem.current != null || FindAnyObjectByType<EventSystem>() != null) return;
#if ENABLE_INPUT_SYSTEM
        new GameObject("EventSystem", typeof(EventSystem),
            typeof(UnityEngine.InputSystem.UI.InputSystemUIInputModule));
#else
        new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
#endif
    }

    //loading data

    void Refresh() => Reload(null);

    //reloads the tab, shows doneMessage instead of the count if theres one
    async void Reload(string doneMessage)
    {
        if (_busy || !GameSession.IsAdmin) return;
        _busy = true;
        SetInteractable(false);
        ResetPendingConfirm();
        ShowStatus("Loading...");
        ClearList();
        int me = GameSession.Current.Value.PlayerId;
        try
        {
            if (_tab == Tab.Runs)
            {
                List<AdminRunRow> runs = await DatabaseManager.Instance.AdminGetRunsAsync(me);
                if (this == null) return;
                ShowStatus(doneMessage ?? (runs.Count == 0 ? "No runs yet." : $"{runs.Count} run(s), fastest first."));
                AddRunHeader();
                for (int i = 0; i < runs.Count; i++) AddRunRow(i + 1, runs[i]);
            }
            else
            {
                List<AdminPlayerRow> players = await DatabaseManager.Instance.AdminGetPlayersAsync(me);
                if (this == null) return;
                ShowStatus(doneMessage ?? $"{players.Count} account(s).");
                AddPlayerHeader();
                foreach (var p in players) AddPlayerRow(p, me);
            }
        }
        catch (Exception e)
        {
            HandleError(e);
        }
        finally
        {
            if (this != null)
            {
                _busy = false;
                SetInteractable(true);
            }
        }
    }

    //does the admin action then reloads the list
    async void RunAction(Func<int, Task<bool>> action, string doneMessage)
    {
        if (_busy || !GameSession.IsAdmin) return;
        _busy = true;
        SetInteractable(false);
        ShowStatus("Working...");
        string message = null;
        try
        {
            bool changed = await action(GameSession.Current.Value.PlayerId);
            message = changed ? doneMessage : "Nothing changed (it may already be gone).";
        }
        catch (Exception e)
        {
            HandleError(e);
        }
        if (this == null) return;
        _busy = false;
        SetInteractable(true);
        if (message != null) Reload(message);
    }

    void HandleError(Exception e)
    {
        if (this == null) return;
        switch (e)
        {
            case NotAdminException:
                ShowStatus("Your account is no longer an admin. Log out and back in.");
                break;
            case InvalidOperationException:
                ShowStatus(e.Message);
                break;
            case MySqlException sql when sql.ErrorCode == MySqlErrorCode.TableAccessDenied
                                      || sql.ErrorCode == MySqlErrorCode.ColumnAccessDenied:
                ShowStatus("The game's database user is missing admin permissions. " +
                           "Run Database/admin_upgrade.sql as root.");
                Debug.LogError($"[Admin] {e}");
                break;
            default:
                ShowStatus("Can't reach the server. Is MariaDB running?");
                Debug.LogError($"[Admin] {e}");
                break;
        }
    }

    //rows

    void AddRunHeader() =>
        AddRow(true, ("#", 70, 0), ("Player", 0, 1), ("Time", 200, 0), ("Date", 260, 0), ("", 170, 0));

    void AddRunRow(int rank, AdminRunRow run)
    {
        var row = AddRow(false, ($"{rank}", 70, 0), (run.Username, 0, 1), (run.FormattedTime, 200, 0),
                         (run.AchievedAt.ToString("MM/dd/yy h:mm tt"), 260, 0));
        int runId = run.RunId;
        string label = $"{run.Username}'s {run.FormattedTime}";
        AddConfirmButton(row, "Delete", 170, () => RunAction(
            me => DatabaseManager.Instance.AdminDeleteRunAsync(me, runId), $"Deleted {label}."));
    }

    void AddPlayerHeader() =>
        AddRow(true, ("Player", 0, 1), ("Role", 140, 0), ("Runs", 100, 0), ("Joined", 170, 0),
               ("", 230, 0), ("", 170, 0));

    void AddPlayerRow(AdminPlayerRow p, int me)
    {
        bool isMe = p.PlayerId == me;
        var row = AddRow(false, (isMe ? $"{p.Username} (you)" : p.Username, 0, 1),
                         (p.IsAdmin ? "Admin" : "Player", 140, 0), ($"{p.RunCount}", 100, 0),
                         (p.CreatedAt.ToString("MM/dd/yy"), 170, 0));

        int id = p.PlayerId;
        string name = p.Username;
        if (isMe)
        {
            AddSpacer(row, 230);
            AddSpacer(row, 170);
            return;
        }

        bool makeAdmin = !p.IsAdmin;
        AddButton(row, makeAdmin ? "Make admin" : "Remove admin", 230, ButtonColor, () => RunAction(
            m => DatabaseManager.Instance.AdminSetAdminAsync(m, id, makeAdmin),
            makeAdmin ? $"{name} is now an admin." : $"{name} is no longer an admin."));
        AddConfirmButton(row, "Delete", 170, () => RunAction(
            m => DatabaseManager.Instance.AdminDeletePlayerAsync(m, id),
            $"Deleted {name} and their runs."));
    }

    //building the ui

    void BuildUi()
    {
        if (_window != null) return;

        var canvasGo = new GameObject("AdminCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGo.transform.SetParent(transform, false);
        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;   //on top of the games ui
        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        //little ADMIN label in the corner
        var badgeText = NewText(canvasGo.transform, $"ADMIN  [{ToggleKeyName}]", 22, TextAlignmentOptions.TopRight);
        badgeText.color = new Color(1f, 0.85f, 0.3f, 0.85f);
        badgeText.raycastTarget = false;
        var badgeRect = badgeText.rectTransform;
        badgeRect.anchorMin = badgeRect.anchorMax = badgeRect.pivot = new Vector2(1, 1);
        badgeRect.anchoredPosition = new Vector2(-20, -14);
        badgeRect.sizeDelta = new Vector2(300, 40);
        _badge = badgeText.gameObject;

        //dark background so you cant click the game behind it
        var dim = NewRect("AdminPanel", canvasGo.transform);
        Stretch(dim);
        dim.gameObject.AddComponent<Image>().color = Dim;
        _window = dim.gameObject;

        var window = NewRect("Window", dim);
        window.anchorMin = window.anchorMax = window.pivot = new Vector2(0.5f, 0.5f);
        window.sizeDelta = new Vector2(1400, 860);
        window.gameObject.AddComponent<Image>().color = WindowColor;
        var layout = window.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(32, 32, 24, 28);
        layout.spacing = 14;
        layout.childControlWidth = layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        //title and close button
        var header = NewHorizontal(window, 56, 12);
        _title = NewText(header, "Admin", 34, TextAlignmentOptions.MidlineLeft);
        _title.fontStyle = FontStyles.Bold;
        Size(_title.gameObject, 0, 1);
        AddButton(header, $"Close  [{ToggleKeyName}]", 220, ButtonColor, Close);

        //tabs and refresh
        var tabs = NewHorizontal(window, 52, 12);
        _runsTab = AddButton(tabs, "Runs", 200, ButtonColor, () => SwitchTab(Tab.Runs));
        _playersTab = AddButton(tabs, "Players", 200, ButtonColor, () => SwitchTab(Tab.Players));
        AddSpacer(tabs, 0, 1);
        _refreshButton = AddButton(tabs, "Refresh", 180, ButtonColor, () => Refresh());

        _status = NewText(window, "", 24, TextAlignmentOptions.MidlineLeft);
        _status.color = new Color(0.8f, 0.85f, 0.9f);
        Size(_status.gameObject, 0, 0, 34);

        //scrolling list
        var scrollRect = NewRect("List", window);
        Size(scrollRect.gameObject, 0, 1, 0, 1);
        scrollRect.gameObject.AddComponent<Image>().color = new Color(0, 0, 0, 0.25f);
        var scroll = scrollRect.gameObject.AddComponent<ScrollRect>();
        scroll.horizontal = false;
        scroll.scrollSensitivity = 40;
        scroll.movementType = ScrollRect.MovementType.Clamped;

        var viewport = NewRect("Viewport", scrollRect);
        Stretch(viewport);
        viewport.gameObject.AddComponent<RectMask2D>();
        //invisible image so scrolling works on empty space too
        viewport.gameObject.AddComponent<Image>().color = Color.clear;

        _listContent = NewRect("Content", viewport);
        _listContent.anchorMin = new Vector2(0, 1);
        _listContent.anchorMax = new Vector2(1, 1);
        _listContent.pivot = new Vector2(0.5f, 1);
        _listContent.sizeDelta = Vector2.zero;
        var listLayout = _listContent.gameObject.AddComponent<VerticalLayoutGroup>();
        listLayout.padding = new RectOffset(10, 10, 10, 10);
        listLayout.spacing = 6;
        listLayout.childControlWidth = listLayout.childControlHeight = true;
        listLayout.childForceExpandWidth = true;
        listLayout.childForceExpandHeight = false;
        _listContent.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scroll.viewport = viewport;
        scroll.content = _listContent;

        _window.SetActive(false);
        UpdateTabColors();
    }

    void SwitchTab(Tab tab)
    {
        if (_busy) return;
        _tab = tab;
        UpdateTabColors();
        Refresh();
    }

    void UpdateTabColors()
    {
        _runsTab.image.color = _tab == Tab.Runs ? TabActiveColor : ButtonColor;
        _playersTab.image.color = _tab == Tab.Players ? TabActiveColor : ButtonColor;
    }

    void SetInteractable(bool on)
    {
        if (_listContent == null) return;
        foreach (var b in _listContent.GetComponentsInChildren<Button>()) b.interactable = on;
        _runsTab.interactable = _playersTab.interactable = _refreshButton.interactable = on;
    }

    void ShowStatus(string message)
    {
        if (_status != null) _status.text = message;
    }

    void ClearList()
    {
        for (int i = _listContent.childCount - 1; i >= 0; i--)
            Destroy(_listContent.GetChild(i).gameObject);
    }

    //each cell is (text, width or 0, flex)
    RectTransform AddRow(bool isHeader, params (string text, float width, float flex)[] cells)
    {
        var row = NewHorizontal(_listContent, isHeader ? 40 : 52, 10);
        var hl = row.GetComponent<HorizontalLayoutGroup>();
        hl.padding = new RectOffset(14, 8, 4, 4);
        if (!isHeader) row.gameObject.AddComponent<Image>().color = RowColor;

        foreach (var (text, width, flex) in cells)
        {
            var t = NewText(row, text, isHeader ? 22 : 26, TextAlignmentOptions.MidlineLeft);
            t.textWrappingMode = TextWrappingModes.NoWrap;
            t.overflowMode = TextOverflowModes.Ellipsis;
            if (isHeader)
            {
                t.fontStyle = FontStyles.Bold;
                t.color = new Color(0.7f, 0.75f, 0.82f);
            }
            Size(t.gameObject, width, flex);
        }
        return row;
    }

    Button AddButton(Transform parent, string label, float width, Color color, Action onClick)
    {
        var rect = NewRect(label + "Button", parent);
        var image = rect.gameObject.AddComponent<Image>();
        image.color = color;
        var button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(() => onClick());
        Size(rect.gameObject, width, 0);

        var text = NewText(rect, label, 24, TextAlignmentOptions.Center);
        Stretch(text.rectTransform);
        text.raycastTarget = false;
        return button;
    }

    //red button you have to click twice
    void AddConfirmButton(Transform parent, string label, float width, Action onConfirmed)
    {
        Button button = null;
        button = AddButton(parent, label, width, DangerColor, () =>
        {
            if (_pendingConfirm == button)
            {
                _pendingConfirm = null;
                onConfirmed();
                return;
            }
            ResetPendingConfirm();
            _pendingConfirm = button;
            _pendingUntil = Time.unscaledTime + ConfirmWindowSeconds;
            button.GetComponentInChildren<TMP_Text>().text = "Sure?";
        });
    }

    void ResetPendingConfirm()
    {
        if (_pendingConfirm != null)
        {
            var t = _pendingConfirm.GetComponentInChildren<TMP_Text>();
            if (t != null) t.text = "Delete";
        }
        _pendingConfirm = null;
    }

    static void AddSpacer(Transform parent, float width, float flex = 0)
    {
        var rect = NewRect("Spacer", parent);
        Size(rect.gameObject, width, flex);
    }

    static RectTransform NewHorizontal(Transform parent, float height, float spacing)
    {
        var rect = NewRect("Row", parent);
        var hl = rect.gameObject.AddComponent<HorizontalLayoutGroup>();
        hl.spacing = spacing;
        hl.childAlignment = TextAnchor.MiddleLeft;
        hl.childControlWidth = hl.childControlHeight = true;
        hl.childForceExpandWidth = false;
        hl.childForceExpandHeight = true;
        Size(rect.gameObject, 0, 0, height);
        return rect;
    }

    static TextMeshProUGUI NewText(Transform parent, string text, float size, TextAlignmentOptions align)
    {
        var rect = NewRect("Text", parent);
        var t = rect.gameObject.AddComponent<TextMeshProUGUI>();
        t.text = text;
        t.fontSize = size;
        t.alignment = align;
        t.color = Color.white;
        return t;
    }

    static RectTransform NewRect(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return (RectTransform)go.transform;
    }

    static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
    }

    //sets width/height, 0 means no fixed size
    static void Size(GameObject go, float width, float flexWidth, float height = 0, float flexHeight = 0)
    {
        if (!go.TryGetComponent(out LayoutElement le)) le = go.AddComponent<LayoutElement>();
        if (width > 0) { le.preferredWidth = width; le.minWidth = width; }
        le.flexibleWidth = flexWidth;
        if (height > 0) { le.preferredHeight = height; le.minHeight = height; }
        le.flexibleHeight = flexHeight;
    }
}
