using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

//put this on a button (like in the pause menu)
// logs the player out and goes back to login
[RequireComponent(typeof(Button))]
public class LogoutButton : MonoBehaviour
{
    [SerializeField] string loginScene = "Login";

    void Awake() => GetComponent<Button>().onClick.AddListener(LogOut);

    void LogOut()
    {
        GameSession.SignOut();
        //pause menu sets this to 0 so reset it before going back
        Time.timeScale = 1f;
        SceneManager.LoadScene(loginScene);
    }
}
