using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DeadMosquito.AndroidGoodies;
using UnityEngine.SceneManagement;
using UnityEngine.Networking;
using UnityEngine.Networking.Types;
using UnityEngine.Networking.Match;

public class devSett : MonoBehaviour {

    public string output;
    //public Text playertextbox;

    string status;
    string wifiInfo;
    public string state;
    public float t;
    public float check;
    public bool isActive = true;
    public bool isServer;
    public InputField playerData;
    public GameObject engine;
    public float duration;

	// Use this for initialization
	void Start () {
        state = "";
        playerData.text = ("Статус:");
    }

    //[SyncVar(hook = "OnMyData")]
    //public string sunk2;

    void Update () {
        t += Time.deltaTime;
        if (isActive)
        {
            duration = engine.GetComponent<engineClient>().duration;
            if (t > 3)
            {
                check += 1;
                if (AGNetwork.IsInternetAvailable())
                {
                    if (AGNetwork.IsWifiEnabled())
                    {
                        if (AGNetwork.IsWifiConnected())
                        {
                            status = "Wi-Fi";
                            wifiInfo = (" Speed: " + AGNetwork.GetWifiConnectionInfo().LinkSpeed + " Signal: " + AGNetwork.GetWifiSignalLevel() + "/100");
                        }
                        else
                        {
                            if (AGNetwork.IsMobileConnected())
                            {
                                status = "4G";
                                wifiInfo = (" Speed: ...");
                            }
                            else
                            {
                                status = "---";
                            }
                        }
                    }
                    //connection.text = ("Тип соединения: " + status + wifiInfo);
                }
                else
                {
                    status = "Wi-Fi";
                    //connection.text = ("Соединение не установлено.");
                }
                if (!isServer)
                {
                    output = ("Батарея: " + AGBattery.GetBatteryChargeLevel() + " | " + status + " | " + state + check.ToString() + " " + duration);
                    playerData.text = output;
                }

                if(check > 9)
                {
                    check = 0;
                }
                t = 0;
            }
        }
	}

}
