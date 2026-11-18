using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

//Tools > Last Chance > Build Login Scene makes the login scene, hooks up LoginPanel and puts it first in the build
//safe to run again, it asks before replacing the old one
public static class LoginSceneBuilder
{
    const string LoginScenePath = "Assets/Scenes/Login.unity";
    const string GameScenePath = "Assets/Scenes/SampleScene.unity";

    [MenuItem("Tools/Last Chance/Build Login Scene")]
    static void Build()
    {
        //TMP needs its essentials to be imported once before any of this works
        if (Resources.Load<TMP_Settings>("TMP Settings") == null)
        {
            EditorApplication.ExecuteMenuItem("Window/TextMeshPro/Import TMP Essential Resources");
            EditorUtility.DisplayDialog("TextMeshPro needed",
                "Click \"Import TMP Essentials\" in the window that opened (or use Window > TextMeshPro > " +
                "Import TMP Essential Resources). When it finishes, run Tools > Last Chance > Build Login Scene again.",
                "OK");
            return;
        }

        if (File.Exists(LoginScenePath) && !EditorUtility.DisplayDialog("Replace Login scene?",
                $"{LoginScenePath} already exists. Replace it?", "Replace", "Cancel"))
            return;

        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        var cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
        cameraObject.tag = "MainCamera";
        cameraObject.transform.position = new Vector3(0, 0, -10);
        var camera = cameraObject.GetComponent<Camera>();
        camera.orthographic = true;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.08f, 0.09f, 0.11f);

        //eventsystem for whichever input system the project is using
#if ENABLE_INPUT_SYSTEM
        new GameObject("EventSystem", typeof(EventSystem), typeof(UnityEngine.InputSystem.UI.InputSystemUIInputModule));
#else
        new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
#endif

        var canvasObject = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        //main panel in the middle of the screen
        var panel = new GameObject("LoginPanel", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup));
        panel.transform.SetParent(canvasObject.transform, false);
        var panelRect = (RectTransform)panel.transform;
        panelRect.anchorMin = panelRect.anchorMax = panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(640, 600);
        panel.GetComponent<Image>().color = new Color(0.14f, 0.15f, 0.19f, 0.97f);
        var layout = panel.GetComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(48, 48, 40, 40);
        layout.spacing = 20;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        var res = new TMP_DefaultControls.Resources
        {
            standard = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd"),
            background = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd"),
            inputField = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/InputFieldBackground.psd"),
            knob = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd"),
            checkmark = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Checkmark.psd"),
            dropdown = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/DropdownArrow.psd"),
            mask = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UIMask.psd"),
        };

        var title = MakeText(panel.transform, "Title", "LAST CHANCE", 64, 90);
        title.fontStyle = FontStyles.Bold;
        MakeText(panel.transform, "Subtitle", "Log in or create an account", 28, 40).color = new Color(0.75f, 0.78f, 0.85f);

        var username = MakeInput(panel.transform, res, "UsernameInput", "Username", false);
        var password = MakeInput(panel.transform, res, "PasswordInput", "Password", true);

        var buttonRow = new GameObject("Buttons", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
        buttonRow.transform.SetParent(panel.transform, false);
        buttonRow.GetComponent<LayoutElement>().preferredHeight = 70;
        var rowLayout = buttonRow.GetComponent<HorizontalLayoutGroup>();
        rowLayout.spacing = 20;
        rowLayout.childControlWidth = rowLayout.childControlHeight = true;
        rowLayout.childForceExpandWidth = rowLayout.childForceExpandHeight = true;
        var loginButton = MakeButton(buttonRow.transform, res, "LoginButton", "Log In", new Color(0.30f, 0.45f, 0.75f));
        var registerButton = MakeButton(buttonRow.transform, res, "RegisterButton", "Register", new Color(0.26f, 0.29f, 0.35f));

        var status = MakeText(panel.transform, "StatusText", "", 26, 70);
        status.color = new Color(1f, 0.75f, 0.45f);
        status.textWrappingMode = TextWrappingModes.Normal;

        //hook up the LoginPanel fields
        var loginPanel = panel.AddComponent<LoginPanel>();
        var so = new SerializedObject(loginPanel);
        so.FindProperty("usernameInput").objectReferenceValue = username;
        so.FindProperty("passwordInput").objectReferenceValue = password;
        so.FindProperty("loginButton").objectReferenceValue = loginButton;
        so.FindProperty("registerButton").objectReferenceValue = registerButton;
        so.FindProperty("statusText").objectReferenceValue = status;
        so.FindProperty("sceneAfterLogin").stringValue = Path.GetFileNameWithoutExtension(GameScenePath);
        so.ApplyModifiedPropertiesWithoutUndo();

        Directory.CreateDirectory(Path.GetDirectoryName(LoginScenePath));
        if (!EditorSceneManager.SaveScene(scene, LoginScenePath))
        {
            EditorUtility.DisplayDialog("Login scene", "Couldn't save " + LoginScenePath, "OK");
            return;
        }

        //build order is login, then the game, then whatever else was there
        var others = EditorBuildSettings.scenes
            .Where(s => s.path != LoginScenePath && s.path != GameScenePath);
        var scenes = new List<EditorBuildSettingsScene> { new EditorBuildSettingsScene(LoginScenePath, true) };
        if (File.Exists(GameScenePath)) scenes.Add(new EditorBuildSettingsScene(GameScenePath, true));
        scenes.AddRange(others);
        EditorBuildSettings.scenes = scenes.ToArray();

        Debug.Log($"[LoginSceneBuilder] Built {LoginScenePath} and set it as the first scene in the build.");
        EditorUtility.DisplayDialog("Login scene ready",
            "Login.unity is built and is now first in Build Settings. Press Play to try it.", "OK");
    }

    static TextMeshProUGUI MakeText(Transform parent, string name, string text, float size, float height)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));
        go.transform.SetParent(parent, false);
        go.GetComponent<LayoutElement>().preferredHeight = height;
        var t = go.GetComponent<TextMeshProUGUI>();
        t.text = text;
        t.fontSize = size;
        t.alignment = TextAlignmentOptions.Center;
        t.color = Color.white;
        return t;
    }

    static TMP_InputField MakeInput(Transform parent, TMP_DefaultControls.Resources res, string name,
                                    string placeholder, bool isPassword)
    {
        var go = TMP_DefaultControls.CreateInputField(res);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.AddComponent<LayoutElement>().preferredHeight = 70;

        var input = go.GetComponent<TMP_InputField>();
        input.lineType = TMP_InputField.LineType.SingleLine;
        input.characterLimit = isPassword ? 128 : 32;
        input.contentType = isPassword ? TMP_InputField.ContentType.Password : TMP_InputField.ContentType.Standard;
        input.pointSize = 30;
        if (input.placeholder is TMP_Text ph) ph.text = placeholder;
        if (input.textComponent != null) input.textComponent.fontSize = 30;
        return input;
    }

    static Button MakeButton(Transform parent, TMP_DefaultControls.Resources res, string name, string label, Color color)
    {
        var go = TMP_DefaultControls.CreateButton(res);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.GetComponent<Image>().color = color;
        var text = go.GetComponentInChildren<TMP_Text>();
        text.text = label;
        text.fontSize = 30;
        text.color = Color.white;
        return go.GetComponent<Button>();
    }
}
