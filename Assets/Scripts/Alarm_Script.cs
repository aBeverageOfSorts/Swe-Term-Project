using System.Collections;
using System.Threading;
using UnityEngine;

// This script triggers all the officers in the game to lock onto the player for a given amount of time.
public class Alarm_Script : MonoBehaviour
{
    
    // This variable controls how long each officer shall be in the alert state and try to chase the player (in seconds).
    public int attention = 5;

    // This variable is what the officer's attention span will be set to after the attention variable above time has passed.

    public float old_attention = 0.5F;

    // Array that holds every officer inside the game, assuming we do not create any more at runtime.
    private GameObject[] officer_arr;

    // Triggering the alarm should depend on whether the game is active or not.
    private GameManagerScript gm;

    // Private float used to record when the alarm triggered.
    private float tm = 0;

    // Debounce variable.
    private bool bounce = false;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        officer_arr = GameObject.FindGameObjectsWithTag("Officer");
        gm = GameObject.FindAnyObjectByType<GameManagerScript>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (bounce)
        {
            return;
        }
        if(collision.transform.name == "Player" && gm.active)
        {
            StartCoroutine(Alarm());
        }
    }
        
    IEnumerator Alarm() { 
        Debug.Log("Triggering alarm.");
        tm = Time.time;   
        bounce = true;
        foreach (var item in officer_arr)
        {
            Debug.Log(item.name);
            Patrol p = item.GetComponent<Patrol>();
            p.attentionSpan = attention;
            p.state = 1;
        }
        // Wait until the attention time has passed.
        // But I need to divide this by 4 for it to work. Not sure why.
        yield return new WaitForSeconds(attention / 4);
        
        // Returning to normal state.
        foreach (var item in officer_arr)
        {
            Patrol p = item.GetComponent<Patrol>();
            p.attentionSpan = old_attention;
        }
        bounce = false;
    }
}
