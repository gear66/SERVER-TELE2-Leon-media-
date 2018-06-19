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

        //Client numPlayers from NetworkManager is always 0, so we count (throught connect/destroy in LobbyPlayer) the number
        //of players, so that even client know how many player there is.
        [HideInInspector]
        public int _playerNumber = 0;

        //used to disconnect a client properly when exiting the matchmaker
        [HideInInspector]
        public bool _isMatchmaking = false;

        protected bool _disconnectServer = false;

        protected ulong _currentMatchID;

        protected LobbyHook _lobbyHooks;

        private bool addNewPlayer = false;
        private Payload tempPayload;

        private bool refreshPlayer = false;
        private Payload refreshPayload;

        private bool toggleVideoPlayer = false;
        private Payload togglePayload;

        public static Dictionary<string, RectTransform> screens;
        public static Dictionary<string, Action<Payload>> serverCommands;
        public static Dictionary<string, Action<Payload>> playerCommands;
        public static Dictionary<string, Action<Payload>> adminCommands;
        public static Dictionary<string, Dictionary<string, Action<Payload>>> commandsSet;

        public float t;
        public float tt;
        public float ttt;
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
            speedIndicator.fillAmount = Mathf.Lerp(speedIndicator.fillAmount, fillLim, 0.03f);

            if (addNewPlayer)
            {
                adminCommands["addPlayer"](tempPayload);
                addNewPlayer = false;
            }

            if (toggleVideoPlayer)
            {
                LobbyPlayerList._instance.TogglePlayerVideo(togglePayload.target, togglePayload.onlineVideo);
            }

            if (refreshPlayer)
            {
                playerCommands["refresh"](refreshPayload);
                refreshPlayer = false;
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
                user.userName = "1";
                user.userType = "Admin";

                Payload newPayload = new Payload();
                newPayload.user = user;
                newPayload.speedTest = deltaSpeed;

                UnityEngine.Debug.Log("deltaSpeed sent in payload");
                UnityEngine.Debug.Log(newPayload.speedTest);

                serverCommands["broadCastSpeedTest"](newPayload);

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
            _lobbyHooks = GetComponent<Prototype.NetworkLobby.LobbyHook>();
            currentPanel = mainMenuPanel;

            adminCommands = new Dictionary<string, Action<Payload>> {
                { "addPlayer", (payload) => {
                        LobbyPlayerList._instance.AddPlayer(GetPlayer(payload));
                    }
                },
                { "playerConnected", (payload) => {
                        tempPayload = payload;
                        addNewPlayer = true;
                    }
                },
                { "toggleOnlineVideo", (payload) => {
                        
                        togglePayload = payload;
                        toggleVideoPlayer = true;
                    }
                },
                { "startDemo", (payload) => {
                        User newAdmin = new User();
                        newAdmin.userName = "1";
                        newAdmin.userType = "Admin";

                        payload.user = newAdmin;

                        Command command = new Command();
                        command.command = "startDemo";
                        command.setType = "playerCommands";

                        Message message = new Message();
                        message.payload = payload;
                        message.command = command;
                        message.target = payload.target;

                        string json = JsonConvert.SerializeObject(message);
                        ws.Send(json);
                    }
                }
            };

            playerCommands = new Dictionary<string, Action<Payload>> {
                { "refresh", (payload) => {
                        LobbyPlayerList._instance.UpdatePlayer(payload.user, payload.stateData);
                    }
                },
                { "refreshData", (payload) => {
                        refreshPayload = payload;
                        refreshPlayer = true;
                    }
                }
            };

            serverCommands = new Dictionary<string, Action<Payload>> {
                { "createLobby", (payload) => {
                        User user = new User();
                        user.userName = "1";
                        user.userType = "Admin";

                        Payload newPayload = new Payload();
                        newPayload.user = user;

                        Command command = new Command();
                        command.setType = "serverCommands";
                        command.command = "createLobby";

                        Message message = new Message();
                        message.payload = payload;
                        message.command = command;

                        string json = JsonConvert.SerializeObject(message);
                        ws.Send(json);
                    }
                },
                { "toggleOnlineVideo", (payload) => {
                        Command command = new Command();
                        command.setType = "serverCommands";
                        command.command = "toggleOnlineVideo";

                        Message message = new Message();
                        message.payload = payload;
                        message.command = command;
                    message.target = payload.target;

                    UnityEngine.Debug.Log("Toggle targety on " + payload.target + " - Toggle video is " + payload.onlineVideo);

                        string json = JsonConvert.SerializeObject(message);
                        ws.Send(json);
                    }
                },
                { "broadCastSpeedTest", (payload) => {
                        Command command = new Command();
                        command.setType = "serverCommands";
                        command.command = "broadCastSpeedTest";

                        Message message = new Message();
                        message.payload = payload;
                        message.command = command;

                        string json = JsonConvert.SerializeObject(message);
                        ws.Send(json);
                    }
                }
            };

            screens = new Dictionary<string, RectTransform> {
                { "mainMenu", s_Singleton.mainMenuPanel},
                { "lobby", s_Singleton.lobbyPanel }
            };

            commandsSet = new Dictionary<string, Dictionary<string, Action<Payload>>> {
                { "playerCommands", playerCommands },
                { "adminCommands", adminCommands },
                { "serverCommands", serverCommands }
            };

            backButton.gameObject.SetActive(false);
            GetComponent<Canvas>().enabled = true;

            //ws = new WebSocket("ws://localhost:8999");
            ws = new WebSocket("ws://cinematele2.herokuapp.com/");


            User admin = new User();
            admin.userType = "Admin";
            admin.userName = "1";

            Payload initPayload = new Payload();
            initPayload.user = admin;

            Command newCommand = new Command();
            newCommand.setType = "serverCommands";
            newCommand.command = "reg";

            Message newMessage = new Message();
            newMessage.command = newCommand;
            newMessage.payload = initPayload;

            string jsonAdmin = JsonConvert.SerializeObject(newMessage);
            ws.OnMessage += (sender, e) => {
                Message message = JsonConvert.DeserializeObject<Message>(e.Data);
                Command command = message.command;

                Dictionary<string, Action<Payload>> currentCommandSet = commandsSet[command.setType];

                currentCommandSet[command.command](message.payload);
            };

            ws.Connect();
            ws.Send(jsonAdmin);

            DontDestroyOnLoad(gameObject);

            SetServerInfo("Offline", "None");
        }

        public LobbyPlayer GetPlayer(Payload payload)
        {
            User user = payload.user;
            GameObject obj = Instantiate(lobbyPlayerPrefab.gameObject) as GameObject;

            LobbyPlayer newPlayer = obj.GetComponent<LobbyPlayer>();

            newPlayer.playerName = user.userName;
            newPlayer.nameInput.text = user.userName;
            newPlayer.toggleState = user.toggleState;

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
            user.userName = "1";
            user.userType = "Admin";
            UnityEngine.Debug.Log("ToggleOnlineVideo called");
            Payload newPayload = new Payload();
            newPayload.user = user;
            newPayload.onlineVideo = !toggleState;
            newPayload.target = name;
            UnityEngine.Debug.Log(newPayload.target);
            toggleState = !toggleState;
            serverCommands["toggleOnlineVideo"](newPayload);
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
            ChangeTo(lobbyPanel);
            backDelegate = StopHostClbk;
            SetServerInfo("Hosting", networkAddress);

            User user = new User();
            user.userName = "1";
            user.userType = "Admin";

            Payload payload = new Payload();
            payload.user = user;

            serverCommands["createLobby"](payload);
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
            backDelegate();
            topPanel.isInGame = false;
        }

        // ----------------- Server management

        public void AddLocalPlayer()
        {
            User user = new User();
            user.userName = "1";
            user.userType = "player";

            Payload payload = new Payload();
            payload.user = user;
            adminCommands["playerConnected"](payload);
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