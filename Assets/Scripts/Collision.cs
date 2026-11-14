using UnityEngine;



// This script just passes information about what collided with this object, upwards for the parent.
public class Collision : MonoBehaviour
{

    public Patrol patrol_script;

    
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        patrol_script = GetComponentInParent<Patrol>();

    }

    // Update is called once per frame
    void Update()
    {

    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if(collision.transform.name == "Player")
        {
            patrol_script.coll_state = true;
            //Debug.Log("Player has entered the sight cone.");
        }
    }
    private void OnTriggerExit2D(Collider2D collision)
    {
        if(collision.transform.name == "Player")
        {
            patrol_script.coll_state = false;
            //Debug.Log("Player has exited the sight cone.");
        }
        
    }
}
