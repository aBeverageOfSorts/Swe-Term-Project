using UnityEngine;
using UnityEngine.UI;

public class Timer : MonoBehaviour
{
    public Text txt;
    // Please do not attempt to overwrite this variable.
    private float time;

    public float GetTime()
    {
        return time;
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        // Do not use real time because real time is unaffected by timescale.
        time = Time.time;
        txt.text = "Time since game started: " + time.ToString();
    }
}
