using UnityEngine;

public class CameraView : MonoBehaviour
{
    public Transform player; // The target the camera will follow

    void LateUpdate()
    {
        // Check if the player is assigned
        if (player != null)
        {
            // Set the camera's position to the player's position
            transform.position = new Vector3(player.position.x, player.position.y, -10f); // Keep the camera at a fixed distance on the Z-axis
        }
    }
}
