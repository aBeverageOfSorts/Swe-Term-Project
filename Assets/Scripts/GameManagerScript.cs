using UnityEngine;

public class GameManagerScript : MonoBehaviour
{
    // Boolean to control other scripts related to the state of the game.
    // Once this is set to false, all other game related scripts should be disabled.
    // The above statement also includes pausing the game.
    public bool active = true;

    // An integer shall represent the state of the game. Each vaule will correspond to one of the following states:
    /*
        * 0 = The game is not active or the game is paused.
        * 1 = The game is active or "playing".
        * 2 = The game has been won.
        * 3 = The game has been lost.
     */
    // Due to the nature of how games work, the game should only really be active while gameState is equal to 1.
    public int gameState = 0;

    // A boolean representing whether the player has been caught by the police. This is used for reading only.
    public bool death = false;

    // A boolean representing whether the player has won the game. Read only.
    public bool win = false;

    // A boolean representing whether the game has been paused. Read only.
    public bool paused = false;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // Don't need to do anything here since the variables above are all initialized.
    }

    // Update is called once per frame
    void Update()
    {
        if (win)
        {
            gameState = 2;
            active = false;
        }
        else if (death)
        {
            gameState = 3;
            active = false;
        }
        else if (paused)
        {
            gameState = 0;
            // Since the Pause script already controls time, no need to modify the active variable here.
        }
        else
        {
            gameState = 1;
            active = true;
        }
    }
}
