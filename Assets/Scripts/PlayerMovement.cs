using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
   [SerializeField] private float moveSpeed = 5f;
   
   private Rigidbody2D rb;
   private Vector2 moveInput;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
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
        //Apply movement evenly regardless of framerate
        rb.linearVelocity = moveInput * moveSpeed;
    }
}
