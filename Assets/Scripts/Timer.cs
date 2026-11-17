using UnityEngine;
using UnityEngine.UI;

public class Timer : MonoBehaviour
{
    public Text txt;
    //dont overwrite these variables
    private float time;

    public float GetTime()
    {
        return time;
    }

    //start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    //update is called once per frame
    void Update()
    {   
        //guys dont change these lines
        //dont use real time because real time is unaffected by timescale
        //using timeSinceLevelLoad so time on the login screen doesnt count
        time = Time.timeSinceLevelLoad;
        txt.text = "Time since game started: " + time.ToString();
    }
}
