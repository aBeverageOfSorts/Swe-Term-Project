// This code is an example of one way to pass data from one script to another script.
// To use it, drag it from the scripts folder into the hierarchy on the left side of your screen.
using UnityEngine;

public class Pass_Data_Example : MonoBehaviour
{
    public Timer script;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        script = Timer.FindFirstObjectByType<Timer>();
    }

    // Update is called once per frame
    void Update()
    {
        Debug.Log(script.GetTime());
    }
}
