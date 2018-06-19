using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class numpad : MonoBehaviour
{

    public InputField inp;
    public InputField min;
    public InputField max;
    public GameObject speedoMeter;
    public string txt;

    // Use this for initialization
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

    public void PresNum(string num)
    {
        inp.text += num;
    }

    public void Cleaar()
    {
        inp.text = "";
    }

    public void PlusMin()
    {
        speedoMeter.GetComponent<speedtest>().speedMin++;
        min.text = speedoMeter.GetComponent<speedtest>().speedMin.ToString();
    }
    public void PlusMax()
    {
        speedoMeter.GetComponent<speedtest>().speedMax++;
        max.text = speedoMeter.GetComponent<speedtest>().speedMax.ToString();
    }

    public void MinusMin()
    {
        speedoMeter.GetComponent<speedtest>().speedMin--;
        min.text = speedoMeter.GetComponent<speedtest>().speedMin.ToString();
    }
    public void MinusMax()
    {
        speedoMeter.GetComponent<speedtest>().speedMax--;
        max.text = speedoMeter.GetComponent<speedtest>().speedMax.ToString();
    }
}
