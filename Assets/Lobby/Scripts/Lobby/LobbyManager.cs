using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.Networking;
using UnityEngine.Networking.Types;
using UnityEngine.Networking.Match;
using System.Collections;
using WebSocketSharp;
using Newtonsoft.Json;
using System.Collections.Generic;
using System;
using System.Diagnostics;

namespace Prototype.NetworkLobby
{
    public class LobbyManager : NetworkLobbyManager
    {
        static short MsgKicked = MsgType.Highest + 1;

        static public LobbyManager s_Singleton;

        static public WebSocket ws;


        [Header("Unity UI Lobby")]
        [Tooltip("Time in second between all players ready & match start")]
        public float prematchCountdown = 5.0f;

        [Space]
        [Header("UI Reference")]
        public LobbyTopPanel topPanel;

        public RectTransform mainMenuPanel;
        public RectTransform lobbyPanel;

        public LobbyInfoPanel infoPanel;
        public LobbyCountdownPanel countdownPanel;
        public GameObject addPlayerButton;

        protected RectTransform currentPanel;

        public Button backButton;

        public Text statusInfo;
        public Text hostInfo;

        public InputField lobbyName;
        public Text duration;
        public Text lobbyNumber;

        //Client numPlayers from NetworkManager is always 0, so we count (throught connect/destroy in LobbyPlayer) the number
        //of players, so that even client know how many player there is.
        [HideInInspector]
        public int _playerNumber = 0;

        //used to disconnect a client properly when exiting the matchmaker
        [HideInInspector]
        public bool _isMatchmaking = false;

        protected bool _disconnectServer = false;

        private bool onClose = false;

        protected ulong _currentMatchID;

        protected LobbyHook _lobbyHooks;

        private bool addNewPlayer = false;
        private Payload joinLobbyPayload;

        private bool refreshPlayer = false;
        private Payload refreshPayload;

        private bool refreshDuration = false;
        private Payload refreshDurationPayload;

        private bool toggleVideoPlayer = false;
        private Payload togglePayload;

        private bool onUserDisconnect = false;
        private Payload userDisconnectPayload;

        private bool disconnected = false;
        private bool manualDisconnected = true;

        public bool onConnect = false;
        public bool reconnecting = false;
        public bool connected = false;

        public static Dictionary<string, RectTransform> screens;
        public static Dictionary<string, Action<Message>> responses;
        public static Dictionary<string, Action<Payload>> requests;

        private bool lobbyCreated = false;

        public bool repeatConnect = false;

        public float t;
        public float tt;
        public float ttt;
        public float t5s;

        public float gg = 0;
        float fillLim;
        public bool speedTest;
        public float deltaSpeed;
        public Text textSpeed;
        public Image speedIndicator;

        void Update()
        {
            t += Time.deltaTime;
            tt += Time.deltaTime;
            ttt += Time.deltaTime;
            t5s += Time.deltaTime;
            speedIndicator.fillAmount = Mathf.Lerp(speedIndicator.fillAmount, fillLim, 0.03f);

            if (disconnected)
            {
                ChangeTo(mainMenuPanel);
                ws.Close();
                topPanel.isInGame = false;
                disconnected = false;
                manualDisconnected = true;
            }

            if (onUserDisconnect)
            {
                onUserDisconnect = false;
                LobbyPlayerList._instance.RemovePlayer(userDisconnectPayload.user.userName);
            }

            if (onClose)
            {
                if (!repeatConnect && connected)
                {
                    UnityEngine.Debug.Log("closing connection");
                    if (LobbyPlayerList._instance != null)
                    {
                        LobbyPlayerList._instance.ClearPlayres();
                    }
                    ChangeTo(mainMenuPanel);
                    infoPanel.Display("Вы были отключены, создайте новый сервер!", "ОК", null);

                    if (!manualDisconnected)
                    {
                        infoPanel.Display("Производятся попытки переподключения", "ОК", null);
                        reconnecting = true;
                    }
                }
                onClose = false;
            }

            if (reconnecting)
            {
                if (t5s > 5)
                {
                    UnityEngine.Debug.Log("InitConn");
                    ws.ConnectAsync();
                    t5s = 0;
                }
            }

            if (onConnect)
            {
                onConnect = false;
                reconnecting = false;
                repeatConnect = false;
                manualDisconnected = false;

                infoPanel.gameObject.SetActive(false);

                User user = new User();
                user.userName = lobbyName.text;
                user.userType = "Admin";

                Payload payload = new Payload();
                payload.user = user;

                Message regMessage = new Message();
                regMessage.payload = payload;
                regMessage.command = "reg";

                string json = JsonConvert.SerializeObject(regMessage);
                ws.Send(json);

                requests["createLobby"](payload);
            }

            if (lobbyCreated)
            {
                StartLobby();
                lobbyCreated = false;
                connected = true;
            }

            if (addNewPlayer)
            {
                LobbyPlayerList._instance.AddPlayer(GetPlayer(joinLobbyPayload));
                addNewPlayer = false;

            }

            if (toggleVideoPlayer)
            {
                UnityEngine.Debug.Log("toggleVideoPlayer oniine video: " + togglePayload.onlineVideo);
                LobbyPlayerList._instance.TogglePlayerVideo(togglePayload.user.userName, togglePayload.onlineVideo);
                toggleVideoPlayer = false;
            }

            if (refreshPlayer)
            {
                LobbyPlayerList._instance.UpdatePlayer(refreshPayload.user, refreshPayload.stateData);
                refreshPlayer = false;
            }

            if (refreshDuration)
            {
                LobbyPlayerList._instance.SetPlayerDuration(refreshDurationPayload.target, refreshDurationPayload.duration);
                refreshDuration = false;
            }

            if (speedTest && gg <= 6 && tt>1f)
            {
                fillLim = gg / 6;
                UnityEngine.Debug.Log("Calculating speed...");
                StartCoroutine(SpeedTestt());
                tt = 0;
            }

            if (gg >= 6 && tt > 1f)
            {
                speedIndicator.color = Color.green;
                //D0FFCC
                speedIndicator.color = new Color(0.79f, 1f, 0.79f);

                User user = new User();
                user.userName = lobbyName.text;
                user.userType = "Admin";

                Payload payload = new Payload();
                payload.user = user;
                payload.speedTest = deltaSpeed;

                UnityEngine.Debug.Log("deltaSpeed sent in payload");
                UnityEngine.Debug.Log(payload.speedTest);

                requests["broadCastSpeedTest"](payload);

                deltaSpeed = 0;
                speedTest = false;
                tt = 0;
                gg = 0;
            }
        }

        public void StartSpeedTest()
        {
            speedIndicator.fillAmount = 0;
            speedIndicator.color = Color.white;
            speedTest = true;
        }

        void Start()
        {
            s_Singleton = this;
            _lobbyHooks = GetComponent<LobbyHook>();
            currentPanel = mainMenuPanel;

            requests = new Dictionary<string, Action<Payload>> {
                { "joinLobby", (payload) => {
                        joinLobbyPayload = payload;
                        addNewPlayer = true;
                    }
                },
                { "disconnect", (payload) => {
                        Message message = new Message();
                        message.payload = payload;
                        message.command = "disconnect";

                        string json = JsonConvert.SerializeObject(message);
                        ws.Send(json);
                    }
                },
                { "userDisconnect", (payload) => {
                        UnityEngine.Debug.Log("Calling user diconnect");
                        userDisconnectPayload = payload;
                        onUserDisconnect = true;
                    }
                },
                { "joinLobbyConfirm", (payload) => {
                        joinLobbyPayload = payload;
                        addNewPlayer = true;
                    }
                },
                { "refreshData", (payload) => {
                        refreshPayload = payload;
                        refreshPlayer = true;
                    }
                },
                { "duration", (payload) => {
                    refreshDuration = true;
                    refreshDurationPayload = payload;
                    }
                },
                { "startDemo", (payload) => {
                        User newAdmin = new User();
                        newAdmin.userName = lobbyName.text;
                        newAdmin.userType = "Admin";

                        payload.user = newAdmin;

                        Message message = new Message();
                        message.payload = payload;
                        message.command = "startDemo";

                        string json = JsonConvert.SerializeObject(message);
                        ws.Send(json);
                    }
                },
                { "createLobby", (payload) => {
                        Message message = new Message();
                        message.payload = payload;
                        message.command = "createLobby";

                        string json = JsonConvert.SerializeObject(message);
                        ws.Send(json);
                    }
                },
                { "toggleOnlineVideo", (payload) => {
                        Message message = new Message();
                        message.payload = payload;
                        message.command = "toggleOnlineVideo";

                        UnityEngine.Debug.Log("Toggle targety on " + payload.target + " - Toggle video is " + payload.onlineVideo);

                        string json = JsonConvert.SerializeObject(message);
                        ws.Send(json);
                    }
                },
                { "toggleOnlineVideoConfirm", (payload) => {
                        UnityEngine.Debug.Log("toggleOnlineVideoConfirm: " + payload.onlineVideo);
                        togglePayload = payload;
                        toggleVideoPlayer = true;
                    }
                },
                { "broadCastSpeedTest", (payload) => {
                        Message message = new Message();
                        message.payload = payload;
                        message.command = "broadCastSpeedTest";

                        string json = JsonConvert.SerializeObject(message);
                        ws.Send(json);
                    }
                }
            };

            responses = new Dictionary<string, Action<Message>> {
                { "createLobby", (message) => {
                        if (message.success) {
                            lobbyCreated = true;
                        }
                    }
                },
                { "disconnect", (message) => {
                        if (message.success) {
                            disconnected = true;
                        }
                    }
                },
                { "toggleOnlineVideo", (message) => {
                        if (message.success) {
                            //togglePayload.onlineVideo = !togglePayload.onlineVideo;
                            //toggleVideoPlayer = true;
                        }
                    }
                },
                { "connect", (message) => {
                        onConnect = true;
                     }

                }
            };

            screens = new Dictionary<string, RectTransform> {
                { "mainMenu", s_Singleton.mainMenuPanel},
                { "lobby", s_Singleton.lobbyPanel }
            };

            backButton.gameObject.SetActive(false);
            GetComponent<Canvas>().enabled = true;

            DontDestroyOnLoad(gameObject);

            SetServerInfo("Offline", "None");
        }

        public void BackButton()
        {
            ChangeTo(mainMenuPanel);
            ws.Close();
        }

        public LobbyPlayer GetPlayer(Payload payload)
        {
            User user = payload.user;
            GameObject obj = Instantiate(lobbyPlayerPrefab.gameObject) as GameObject;

            LobbyPlayer newPlayer = obj.GetComponent<LobbyPlayer>();

            newPlayer.playerName = user.userName;
            newPlayer.nameInput.text = user.userName;
            //newPlayer.toggleState = user.toggleState;
            newPlayer.toggleState = true;

            return newPlayer;
        }

        public IEnumerator SpeedTestt()
        {
            yield return new WaitForSeconds(1f);
            UnityEngine.Debug.Log("started");
            //textSpeed.text = deltaSpeed.ToString();
            Console.WriteLine("Downloading file....");

            var watch = new Stopwatch();

            byte[] data;
            using (var client = new System.Net.WebClient())
            {
                watch.Start();
                data = client.DownloadData("http://dl.google.com/googletalk/googletalk-setup.exe?t=" + DateTime.Now.Ticks);
                watch.Stop();
            }

            deltaSpeed = (float)(data.LongLength / watch.Elapsed.TotalSeconds / 100000f / 6f); // instead of [Seconds] property
            UnityEngine.Debug.Log("deltaSpeed in speedtest");
            UnityEngine.Debug.Log(deltaSpeed);
            textSpeed.text = deltaSpeed.ToString("0.00") + " Mb/s";
            gg++;
        }

        public void ToggleOnlineVideo(string name, bool toggleState)
        {
            User user = new User();
            user.userName = lobbyName.text;
            user.userType = "Admin";
            UnityEngine.Debug.Log("ToggleOnlineVideo called");
            Payload newPayload = new Payload();
            newPayload.user = user;
            newPayload.onlineVideo = toggleState;
            newPayload.target = name;
            UnityEngine.Debug.Log(newPayload.target);
            //toggleState = !toggleState;
            requests["toggleOnlineVideo"](newPayload);
        }

        public override void OnLobbyClientSceneChanged(NetworkConnection conn)
        {
            if (SceneManager.GetSceneAt(0).name == lobbyScene)
            {
                if (topPanel.isInGame)
                {
                    ChangeTo(lobbyPanel);
                    if (_isMatchmaking)
                    {
                        if (conn.playerControllers[0].unetView.isServer)
                        {
                            backDelegate = StopHostClbk;
                        }
                        else
                        {
                            backDelegate = StopClientClbk;
                        }
                    }
                    else
                    {
                        if (conn.playerControllers[0].unetView.isClient)
                        {
                            backDelegate = StopHostClbk;
                        }
                        else
                        {
                            backDelegate = StopClientClbk;
                        }
                    }
                }
                else
                {
                    ChangeTo(mainMenuPanel);
                }

                topPanel.ToggleVisibility(true);
                topPanel.isInGame = false;
            }
            else
            {
                ChangeTo(null);

                Destroy(GameObject.Find("MainMenuUI(Clone)"));

                //backDelegate = StopGameClbk;
                topPanel.isInGame = true;
                topPanel.ToggleVisibility(false);
            }
        }

        public void HostStart()
        {
            User user = new User();
            user.userName = lobbyName.text;
            user.userType = "Admin";
            lobbyNumber.text = user.userName.ToString();
            int n;
            if (user.userName == "" || !int.TryParse(user.userName, out n))
            {
                infoPanel.Display("Введите числовое значение для номера сервера", "Вернуться", () =>
                {
                    ChangeTo(mainMenuPanel);
                });
                return;
            }

            UnityEngine.Debug.Log(user.userName);

            Payload payload = new Payload();
            payload.user = user;

            InitConnection();
        }

        private void StartLobby()
        {
            ChangeTo(lobbyPanel);
            SetServerInfo("Hosting", lobbyName.text);
        }

        private void InitConnection()
        {
            UnityEngine.Debug.Log("InitConn");
            if (ws != null)
            {
                repeatConnect = true;
                ws.Close();
            }
            //ws = new WebSocket("ws://localhost:8999");
            //ws = new WebSocket("ws://cinematele2.herokuapp.com/");
            //ws = new WebSocket("ws://130.255.43.125:8999");
            ws = new WebSocket("ws://91.201.53.171:8999");

            ws.OnMessage += (sender, e) => {
                UnityEngine.Debug.Log(e.Data);
                Message message = JsonConvert.DeserializeObject<Message>(e.Data);
                UnityEngine.Debug.Log("command: " + message.command);


                if (message.isRequest)
                {
                    requests[message.command](message.payload);
                }
                else
                {
                    responses[message.command](message);
                }
            };

            ws.OnClose += (sender, e) =>
            {
                onClose = true;
            };

            infoPanel.Display("Идет подключение...", "Отменить", null);
            reconnecting = true;
            repeatConnect = true;
        }

        public void ChangeTo(RectTransform newPanel)
        {
            if (currentPanel != null)
            {
                currentPanel.gameObject.SetActive(false);
            }

            if (newPanel != null)
            {
                newPanel.gameObject.SetActive(true);
            }

            currentPanel = newPanel;

            if (currentPanel != mainMenuPanel)
            {
                backButton.gameObject.SetActive(true);
            }
            else
            {
                backButton.gameObject.SetActive(false);
                SetServerInfo("Offline", "None");
                _isMatchmaking = false;
            }
        }

        public void DisplayIsConnecting()
        {
            var _this = this;
            infoPanel.Display("Connecting...", "Cancel", () => { _this.backDelegate(); });
        }

        public void SetServerInfo(string status, string host)
        {
            statusInfo.text = status;
            hostInfo.text = host;
        }


        public delegate void BackButtonDelegate();
        public BackButtonDelegate backDelegate;
        public void GoBackButton()
        {
            User user = new User();
            user.userName = lobbyName.text;
            user.userType = "Admin";

            Payload payload = new Payload();
            payload.user = user;

            requests["disconnect"](payload);
        }

        // ----------------- Server management

        public void AddLocalPlayer()
        {
            User user = new User();
            user.userName = "1";
            user.userType = "player";

            Payload payload = new Payload();
            payload.user = user;
            requests["playerConnected"](payload);
            //TryToAddPlayer();
        }

        public void RemovePlayer(LobbyPlayer player)
        {
            player.RemovePlayer();
        }

        public void SimpleBackClbk()
        {
            ChangeTo(mainMenuPanel);
        }

        public void StopHostClbk()
        {
            if (_isMatchmaking)
            {
                matchMaker.DestroyMatch((NetworkID)_currentMatchID, 0, OnDestroyMatch);
                _disconnectServer = true;
            }
            else
            {
                StopHost();
            }


            ChangeTo(mainMenuPanel);
        }

        public void StopClientClbk()
        {
            StopClient();

            if (_isMatchmaking)
            {
                StopMatchMaker();
            }

            ChangeTo(mainMenuPanel);
        }

        public void StopServerClbk()
        {
            StopServer();
            ChangeTo(mainMenuPanel);
        }

        class KickMsg : MessageBase { }
        public void KickPlayer(NetworkConnection conn)
        {
            conn.Send(MsgKicked, new KickMsg());
        }


        public void KickedMessageHandler(NetworkMessage netMsg)
        {
            infoPanel.Display("Kicked by Server", "Close", null);
            netMsg.conn.Disconnect();
        }

        //===================

        public override void OnStartHost()
        {
            base.OnStartHost();

            ChangeTo(lobbyPanel);
            backDelegate = StopHostClbk;
            SetServerInfo("Hosting", networkAddress);
        }

        public override void OnMatchCreate(bool success, string extendedInfo, MatchInfo matchInfo)
        {
            base.OnMatchCreate(success, extendedInfo, matchInfo);
            _currentMatchID = (System.UInt64)matchInfo.networkId;
        }

        public override void OnDestroyMatch(bool success, string extendedInfo)
        {
            base.OnDestroyMatch(success, extendedInfo);
            if (_disconnectServer)
            {
                StopMatchMaker();
                StopHost();
            }
        }

        //allow to handle the (+) button to add/remove player
        public void OnPlayersNumberModified(int count)
        {
            _playerNumber += count;

            int localPlayerCount = 0;
            foreach (PlayerController p in ClientScene.localPlayers)
                localPlayerCount += (p == null || p.playerControllerId == -1) ? 0 : 1;

            addPlayerButton.SetActive(localPlayerCount < maxPlayersPerConnection && _playerNumber < maxPlayers);
        }

        // ----------------- Server callbacks ------------------

        //we want to disable the button JOIN if we don't have enough player
        //But OnLobbyClientConnect isn't called on hosting player. So we override the lobbyPlayer creation
        public override GameObject OnLobbyServerCreateLobbyPlayer(NetworkConnection conn, short playerControllerId)
        {
            GameObject obj = Instantiate(lobbyPlayerPrefab.gameObject) as GameObject;

            LobbyPlayer newPlayer = obj.GetComponent<LobbyPlayer>();
            newPlayer.ToggleJoinButton(numPlayers + 1 >= minPlayers);


            for (int i = 0; i < lobbySlots.Length; ++i)
            {
                LobbyPlayer p = lobbySlots[i] as LobbyPlayer;

                if (p != null)
                {
                    p.RpcUpdateRemoveButton();
                    p.ToggleJoinButton(numPlayers + 1 >= minPlayers);
                }
            }

            return obj;
        }

        public override void OnLobbyServerPlayerRemoved(NetworkConnection conn, short playerControllerId)
        {
            for (int i = 0; i < lobbySlots.Length; ++i)
            {
                LobbyPlayer p = lobbySlots[i] as LobbyPlayer;

                if (p != null)
                {
                    p.RpcUpdateRemoveButton();
                    p.ToggleJoinButton(numPlayers + 1 >= minPlayers);
                }
            }
        }

        public override void OnLobbyServerDisconnect(NetworkConnection conn)
        {
            for (int i = 0; i < lobbySlots.Length; ++i)
            {
                LobbyPlayer p = lobbySlots[i] as LobbyPlayer;

                if (p != null)
                {
                    p.RpcUpdateRemoveButton();
                    p.ToggleJoinButton(numPlayers >= minPlayers);
                }
            }

        }

        public override bool OnLobbyServerSceneLoadedForPlayer(GameObject lobbyPlayer, GameObject gamePlayer)
        {
            //This hook allows you to apply state data from the lobby-player to the game-player
            //just subclass "LobbyHook" and add it to the lobby object.

            if (_lobbyHooks)
                _lobbyHooks.OnLobbyServerSceneLoadedForPlayer(this, lobbyPlayer, gamePlayer);

            return true;
        }

        // --- Countdown management

        public override void OnLobbyServerPlayersReady()
        {
            bool allready = true;
            for (int i = 0; i < lobbySlots.Length; ++i)
            {
                if (lobbySlots[i] != null)
                    allready &= lobbySlots[i].readyToBegin;
            }

            if (allready)
                StartCoroutine(ServerCountdownCoroutine());
        }

        public IEnumerator ServerCountdownCoroutine()
        {
            float remainingTime = prematchCountdown;
            int floorTime = Mathf.FloorToInt(remainingTime);

            while (remainingTime > 0)
            {
                yield return null;

                remainingTime -= Time.deltaTime;
                int newFloorTime = Mathf.FloorToInt(remainingTime);

                if (newFloorTime != floorTime)
                {//to avoid flooding the network of message, we only send a notice to client when the number of plain seconds change.
                    floorTime = newFloorTime;

                    for (int i = 0; i < lobbySlots.Length; ++i)
                    {
                        if (lobbySlots[i] != null)
                        {//there is maxPlayer slots, so some could be == null, need to test it before accessing!
                            (lobbySlots[i] as LobbyPlayer).RpcUpdateCountdown(floorTime);
                        }
                    }
                }
            }

            for (int i = 0; i < lobbySlots.Length; ++i)
            {
                if (lobbySlots[i] != null)
                {
                    (lobbySlots[i] as LobbyPlayer).RpcUpdateCountdown(0);
                }
            }

            ServerChangeScene(playScene);
        }

        // ----------------- Client callbacks ------------------

        public override void OnClientConnect(NetworkConnection conn)
        {
            base.OnClientConnect(conn);

            infoPanel.gameObject.SetActive(false);

            conn.RegisterHandler(MsgKicked, KickedMessageHandler);

            if (!NetworkServer.active)
            {//only to do on pure client (not self hosting client)
                ChangeTo(lobbyPanel);
                backDelegate = StopClientClbk;
                SetServerInfo("Client", networkAddress);
            }
        }


        public override void OnClientDisconnect(NetworkConnection conn)
        {
            base.OnClientDisconnect(conn);
            ChangeTo(mainMenuPanel);
        }

        public override void OnClientError(NetworkConnection conn, int errorCode)
        {
            ChangeTo(mainMenuPanel);
            infoPanel.Display("Cient error : " + (errorCode == 6 ? "timeout" : errorCode.ToString()), "Close", null);
        }
    }
}