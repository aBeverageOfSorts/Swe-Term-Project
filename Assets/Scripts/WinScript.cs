using UnityEngine;

// This script just sets the win boolean in GameManagerScript to true, once the player touches this, indicating that the game is won.
public class WinScript : MonoBehaviour
{
    private GameManagerScript gm;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        gm = GameObject.FindAnyObjectByType<GameManagerScript>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        Debug.Log(collision.transform.name);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        Debug.Log(collision.transform.name);
        if(collision.transform.name == "Player" && gm.active)
        {
            gm.win = true;
        }
    }
}
