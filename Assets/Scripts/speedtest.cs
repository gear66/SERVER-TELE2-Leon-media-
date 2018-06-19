using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class speedtest : MonoBehaviour {

    public string dur;
    public float speed;
    public float deltaSpeed = 0;
    public float t;
    public float tt;
    public float ttt;
    public float rotSpeed;
    public Text textbox;
    public GameObject textboxobj;
    public GameObject textboxobjL;
    public bool isactive = false;
    public bool isReal = false;
    public string log;
    public string url;
    public GameObject engine;
    public int localflag = 0;

    public float speedMin;
    public float speedMax;

    float s; // начальное положение стрелки по оси Z
    Quaternion rot;
    Quaternion rotst;
    public GameObject arrowL; // стрелка спидометра
    public GameObject arrow;

    //public enum ProjectMode { Project3D = 0, Project2D = 1 };
    //public ProjectMode projectMode = ProjectMode.Project3D;

    //public Transform target; // объект с которого берем скорость
    //public float velocity; // текущая реальная скорость объекта
    private float speeed;

    // Use this for initialization

    void Start () {
        //arrow.localRotation = Quaternion.Euler(0, 0, _start);
        //speedMin = PlayerPrefs.GetFloat("SpeedMin", 20f);
        //speedMax = PlayerPrefs.GetFloat("SpeedMax", 25f);
        rotst = arrow.transform.localRotation;
        rotSpeed = 0.5f;
    }
	
	// Update is called once per frame
	void Update () {
        s = arrow.transform.localRotation.z;
        rot = Quaternion.Euler(0, 0, -deltaSpeed * (360f / 45f) + 120f);
        arrow.transform.localRotation = rot;
        arrowL.transform.localRotation = rot;
        if (isactive)
        {
            ttt += Time.deltaTime;
            tt += Time.deltaTime;
            t += Time.deltaTime;
            if (isReal)
            {
                if (t > 1)
                {
                    StartCoroutine(SpeedTestt());
                    //StartCoroutine(CallWebPage());
                    UnityEngine.Debug.Log("T started");
                    t = 0;
                }
            }
            else
            {
                deltaSpeed = Mathf.Lerp(deltaSpeed, speed, rotSpeed * Time.deltaTime);
                if (t > 0.5f & localflag ==0)
                {
                    speed = UnityEngine.Random.Range(speedMin, speedMax);
                    log = (deltaSpeed.ToString("N0") + " Мб/с");
                    textboxobj.GetComponent<TextMesh>().text = log;
                    textboxobjL.GetComponent<TextMesh>().text = log;
                    t = 0;
                }
            }
            if (tt > 10 & localflag == 0)
            {
                speed = 0;
                rotSpeed = 1;
                engine.GetComponent<engineClient>().flag = 2;
                localflag = 1;
            }
            if (tt > 15 & localflag == 1)
            {
                textboxobj.GetComponent<TextMesh>().text = (speedMax - 2).ToString() + " Мб/с";
                textboxobjL.GetComponent<TextMesh>().text = (speedMax - 2).ToString() + " Мб/с";
                textboxobj.transform.localScale = new Vector3(0.01f, 0.01f);
                textboxobjL.transform.localScale = new Vector3(0.01f, 0.01f);
                //engine.GetComponent<engineClient>().flag = 2;
                localflag = 2;
                t = 0;
            }
            if (tt > 40 & localflag == 2)
            {
                Stap();
                tt = 0;
                localflag = 0;
            }
        }
    }

    public IEnumerator SpeedTestt()
    {
        yield return new WaitForSeconds(1f);
        UnityEngine.Debug.Log("started");

        Console.WriteLine("Downloading file....");

        var watch = new Stopwatch();

        byte[] data;
        using (var client = new System.Net.WebClient())
        {
            watch.Start();
            data = client.DownloadData("http://dl.google.com/googletalk/googletalk-setup.exe?t=" + DateTime.Now.Ticks);
            watch.Stop();
        }

        var speed = data.LongLength / watch.Elapsed.TotalSeconds / 100000f; // instead of [Seconds] property

        log = (speed.ToString("N0") + " Мб/с");
        //log = ("Speed: " + speed.ToString("N0") + "Мб/с" + Environment.NewLine);
        textboxobj.GetComponent<TextMesh>().text = log;
        textboxobjL.GetComponent<TextMesh>().text = log;
        //textbox.text = log;
    }

    public void Sstart()
    {
        if (localflag == 0)
        {
            textboxobj.transform.localScale = new Vector3(0.0079f, 0.0073f);
            textboxobjL.transform.localScale = new Vector3(0.0079f, 0.0073f);
            rotSpeed = 0.5f;
            isactive = true;
        }
    }
    public void Stap()
    {
        rotSpeed = 0.5f;
        isactive = false;
    }
    public void GoNull()
    {

    }

    IEnumerator CallWebPage()
    {
        DateTime dt1 = DateTime.Now;
        WWW www = new WWW("https://static.pexels.com/photos/20974/pexels-photo.jpg");
        yield return www;
        DateTime dt2 = DateTime.Now;
        UnityEngine.Debug.Log(Math.Round((www.bytes.Length / 1024) / (dt2 - dt1).TotalSeconds, 2));
    }
    //    Console.WriteLine("Downloading file....");

    //var watch = new Stopwatch();

    //    byte[] data;
    //using (var client = new System.Net.WebClient())
    //{
    //    watch.Start();
    //    data = client.DownloadData("http://dl.google.com/googletalk/googletalk-setup.exe?t=" + DateTime.Now.Ticks);
    //    watch.Stop();
    //}

    //var speed = data.LongLength / watch.Elapsed.TotalSeconds; // instead of [Seconds] property

    //Console.WriteLine("Download duration: {0}", watch.Elapsed);
    //Console.WriteLine("File size: {0}", data.Length.ToString("N0"));
    //Console.WriteLine("Speed: {0} bps ", speed.ToString("N0"));

    //Console.WriteLine("Press any key to continue...");
    //Console.ReadLine();
}

//using UnityEngine;
//using System.Collections;

//public class Speedometer : MonoBehaviour
//{

//    public float _start; // начальное положение стрелки по оси Z

//    public float maxSpeed; // максимальная скорость на спидометре

//    public RectTransform arrow; // стрелка спидометра

//    public enum ProjectMode { Project3D = 0, Project2D = 1 };
//    public ProjectMode projectMode = ProjectMode.Project3D;

//    public Transform target; // объект с которого берем скорость

//    public float velocity; // текущая реальная скорость объекта

//    private Rigidbody _3D;
//    private Rigidbody2D _2D;
//    private float speed;

//    void Start()
//    {
//        arrow.localRotation = Quaternion.Euler(0, 0, _start);
//        if (projectMode == ProjectMode.Project3D) _3D = target.GetComponent<Rigidbody>();
//        else _2D = target.GetComponent<Rigidbody2D>();
//    }

//    void Update()
//    {
//        if (projectMode == ProjectMode.Project3D) velocity = _3D.velocity.magnitude; else velocity = _2D.velocity.magnitude;
//        if (velocity > maxSpeed) velocity = maxSpeed;
//        speed = _start - velocity;
//        arrow.localRotation = Quaternion.Euler(0, 0, speed);
//    }
//}
