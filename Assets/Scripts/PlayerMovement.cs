using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
   [SerializeField] private float moveSpeed = 5f;
   
   private Rigidbody2D rb;
    private PauseScript pausesystem;
    private Vector2 moveInput;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        pausesystem = FindFirstObjectByType<PauseScript>();
    }
    // Update is called once per frame
    void Update()
    {
        //get movement direction
        float moveX = Input.GetAxisRaw("Horizontal");
        float moveY = Input.GetAxisRaw("Vertical");
        moveInput = new Vector2(moveX, moveY).normalized;
        
    }

    void FixedUpdate()
    {
        // No inputs if the game is paused ( I don't think this is needed but
        // if we ever rely on physics then we MUST disable inputs before pausing the game.
        
        //if (pausesystem.GetIsPaused()) { return; }
        
        //Apply movement evenly regardless of framerate
        rb.linearVelocity = moveInput * moveSpeed;
    }
}
