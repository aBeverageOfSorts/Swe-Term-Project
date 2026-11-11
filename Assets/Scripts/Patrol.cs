// Code inspired by: https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Vector2.MoveTowards.html
// By: Samy Katibi

/* Instructions:
 * Put this script inside an object (a.k.a. Transform).
 * Create 2 or more other transforms to serve as waypoints to travel between.
 * Put all transforms inside the array thing that shows up inside the Unity Inspector, under the script header.
 * Customize the stats if you wish.
 * Then run the script.
 * NOTE: This script does NOT yet check for collision.
 */

// TODO: Code and implement police officer vision and collision = gameover.
using UnityEngine;

public class Patrol : MonoBehaviour
{
    [Header("Waypoints")]
    // Array that holds all the waypoints to travel to (in descending order).
    public Transform[] waypoints;

    [Header("Stats")]
    // Float that is used to calculate the speed at which the officer will travel.
    public float speed = 15.0f;

    // Integer describing how far away the sprite must approach the waypoint before it has been considered as reached.
    public float tolerance = 3.0f;

    // This is a boolean that controls whether patrol behavior should happen. Controlled by GameManagerScript. Read only.
    public bool active = true;

    // This variable is used to detect if the player is touching the sightline. Please do not modify it except to point to the sight object.
    public GameObject sight;

    // This variable shall control the state of the officer.
    /*
        * 0 = Calm state, the officer is patrolling. 
        * 1 = Alert state, the officer is chasing.
     */
    private int state = 0;

    // Integer describing which waypoint in the array that the officer will try to move towards.
    private int dest = 0;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // Don't really need to do anything here.
    }

    // Update is called once per frame
    void Update()
    {
        if (!active)
        {
            return;
        }

        if (false)
        {
            state = 1;
        }
        
        // If the number of waypoints is empty.
        if(waypoints.Length == 0)
        {
            // I have no where to go.
            // Delete this if part IF the officer should be stationary instead of patrolling.
            Debug.Log("I don't know where to go!");
            Debug.Log(transform.name);
            return;
        }
        
        float step = speed * Time.deltaTime;

        // Take a step towards the destination.
        transform.position = Vector2.MoveTowards(transform.position, waypoints[dest].position, step);


        // If we are close to the destination, set a new destination.
        if(Vector2.Distance(transform.position, waypoints[dest].position) < tolerance)
        {
            dest = (dest + 1) % waypoints.Length;
        }
    }
}
