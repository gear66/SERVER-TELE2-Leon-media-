using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public class engineClient : MonoBehaviour {

    public InputField ipData;
    public bool is4g = true;
    public bool isInCinema = false;
    public bool isWatchEnded = false;
    public bool isReal;
    bool isAudioPlayed = false;
    public int flag = 0;
    public int set;
    public float duration;
    public float t = 5;

    public GameObject cinemaEnv;
    public GameObject[] videoss;
    public GameObject[] videossOnline;
    public GameObject vidPrefab;
    public GameObject gazee;
    public GameObject pointer;
    public GameObject browBlock;
    //public GameObject rig;
    public GameObject player;
    public GameObject splash;
    //public Camera main;
    public GameObject buttons;

    public MeshRenderer leftScrMat;
    public MeshRenderer leftScrMatL;
    public MeshRenderer prerollMat;
    public Material g3;
    public Material g4;
    public Material g33;
    public Material g44;

    public AudioSource audioSrc;
    public AudioClip hello3g;
    public AudioClip hello4g;
    AudioClip hellog;
    public AudioClip watch3g;
    public AudioClip watch4g;
    AudioClip watchg;
    public AudioClip end3g;
    public AudioClip end4g;
    AudioClip endg;
    public GameObject browserColl2;

    // Use this for initialization
    void Start () {
        flag = 0;
        ipData.text = PlayerPrefs.GetString("IP", "192.168.");
        audioSrc.clip = null;
        hellog = null;
        watchg = null;
        endg = null;
        set = PlayerPrefs.GetInt("set", 3);
        if (set == 3)
        {
            ImageSet3g();
        }
        if (set == 4)
        {
            ImageSet4g();
        }
    }
	
	// Update is called once per frame
	void Update () {
        if (isInCinema)
        {
            t += Time.deltaTime;
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                StartDemo();
                if (t < 2)
                {
                    flag = 5;
                    buttons.SetActive(true);
                    audioSrc.Stop();
                }
                t = 0;
            }
            if (Input.GetKeyUp(KeyCode.A))
            {
                flag = 5;
                buttons.SetActive(true);
            }
            if (flag == 1)
            {
                if (audioSrc.time > audioSrc.clip.length - 10f)
                {
                    Debug.Log("speedtest coll deleted");
                    browserColl2.SetActive(false);
                }
                if (audioSrc.time > audioSrc.clip.length - 0.01f)
                {
                    Debug.Log("First audio ended");
                    audioSrc.Stop();
                    //audioSrc.clip.
                    //flag = 2;
                }
            }
            if (flag == 2)
            {
                //if (audioSrc.time > audioSrc.clip.length - 0.01f)
                //{
                    Debug.Log("Second audio started");
                    audioSrc.clip = watchg;
                    audioSrc.Play();
                    flag = 3;
                buttons.SetActive(true);
                //}
            }
            if (flag == 3)
            {
                if (audioSrc.time > audioSrc.clip.length - 0.01f)
                {
                    Debug.Log("Second audio ended");
                    audioSrc.Stop();
                    browBlock.SetActive(false);
                    flag = 4;
                }
            }
            if (flag == 4)
            {
                if (isWatchEnded)
                {
                    Debug.Log("End audio started");
                    audioSrc.clip = endg;
                    audioSrc.Play();
                    browBlock.SetActive(true);
                    browserColl2.SetActive(true);
                    flag = 0;
                }
            }
            if (flag == 5)
            {
                browBlock.SetActive(false);
                browserColl2.SetActive(false);
                flag = 0;
            }
        }
        else
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                GoCinema();
            }
        }
    }

    public void SaveIP()
    {
        PlayerPrefs.SetString("IP", ipData.text);
    }

    public void GoWatch(int i)
    {
        buttons.SetActive(false);
        browserColl2.SetActive(false);
        isInCinema = false;
        gazee.SetActive(false);
        vidPrefab.SetActive(true);
        cinemaEnv.SetActive(false);
        splash.SetActive(true);
        if (isReal)
        {
            videossOnline[i - 1].SetActive(true);
            Debug.Log(i + " selected, play started");
        }
        else
        {
            videoss[i - 1].SetActive(true);
            //videoss[i - 1].GetComponent<VideoPlayer>().Play();
            //videoss[i - 1].GetComponent<MediaPlayerCtrl>().Play();
        }
    }

    public void GoCinema()
    {
        splash.SetActive(false);
        foreach (GameObject i in videoss)
        {
            i.SetActive(false);
        }
        foreach (GameObject i in videossOnline)
        {
            i.SetActive(false);
        }
        vidPrefab.SetActive(false);
        cinemaEnv.SetActive(true);
        //rig.SetActive(false);
        //player.SetActive(true);
        isInCinema = true;
        browBlock.SetActive(true);
        browserColl2.SetActive(true);
        flag = 4;
        gazee.SetActive(true);
        //main.enabled = true;
    }

    public void SetRot(GameObject ga)
    {
        ga.transform.position = pointer.transform.position;
        ga.transform.rotation = pointer.transform.rotation;
    }

    public void StartDemo()
    {
        browBlock.SetActive(true);
        audioSrc.clip = hellog;
        audioSrc.Play();
        isAudioPlayed = true;
        flag = 1;
        Debug.Log("Demo started");
    }

    public void ImageSet3g()
    {
        is4g = false;
        PlayerPrefs.SetInt("set", 3);
        leftScrMat.material = g3;
        leftScrMatL.material = g3;
        prerollMat.material = g33;
        hellog = hello3g;
        watchg = watch3g;
        endg = end3g;
    }

    public void ImageSet4g()
    {
        is4g = true;
        PlayerPrefs.SetInt("set", 4);
        leftScrMat.material = g4;
        leftScrMatL.material = g4;
        prerollMat.material = g44;
        hellog = hello4g;
        watchg = watch4g;
        endg = end4g;
    }

    public void WatchEnded()
    {
        isWatchEnded = true;
    }
}
