using UnityEngine;

public class Pistol : MonoBehaviour
{
    private Inventory inventory;
    private bool isPickedUp = false;

    private void Start()
    {
        inventory = FindObjectOfType<Inventory>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") && !isPickedUp)
        {
            if(inventory == null) return;
            isPickedUp = inventory.AddItem(gameObject);
            if(isPickedUp){
            Debug.Log("Picked up " + gameObject.name);
        }}
    }

}