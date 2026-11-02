// Inspiried by: https://gamedev.tv/articles/how-to-basic-pause-system-in-unity
using UnityEngine;

public class PauseScript : MonoBehaviour
{
    public bool isPaused = false;
    public GameObject PauseMenu;
    public bool GetIsPaused() { return isPaused; }
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        PauseMenu.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            isPaused = !isPaused;
            Time.timeScale = isPaused ? 0 : 1;
            PauseMenu.SetActive(isPaused);
        }
    }
}
