using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Networking.Match;
using UnityEngine.Networking.Types;
using UnityEngine.Networking.Match;
using UnityEngine.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using ExitGames;
using ExitGames.Client;
using ExitGames.Client.Photon;

public class GameMatchMaker : Photon.PunBehaviour
{

    public GameObject gameNetworkPrefab;
    [NonSerialized]
    public GameNetwork gameNetwork;
    public Camera camera;
    public Canvas canvasConnect;
    public Canvas canvasPlay;
    public Canvas canvasSettings;
    public Button startButton;
    public Button joinButton;
    public Text joinButtonText;
    public Button startLocalButton;
    public Button settingsButton;
    public InputField roomIdField;
    public string storedMatchName = "";

    public int joinAttempts = 0;

    public float preferenceHealth = 100.0f;
    public float preferenceStamina = 100.0f;
    public float preferenceStaminaConsume = 30.0f;
    public float preferenceStaminaRegeneration = 10.0f;
    public float preferenceMinDamage = 5.0f;
    public float preferenceMaxDamage = 8.0f;
    public float preferenceCritChance = 0.15f;
    public float preferenceCritMultiplier = 1.5f;
    public float preferenceInjureChance = 0.1f;
    public float preferenceAbilityEvadeChance = 0.2f;
    public float preferenceAbilityCritChance = 0.15f;
    public float preferenceAbilityStunDuration = 5.0f;
    public float preferenceAbilityShieldDuration = 5.0f;
    public float preferenceAbilityShieldMultiplier = 0.5f;
    public float preferenceInjureArmEffect = 0.5f;
    public float preferenceInjureLegEffect = 0.5f;
    public float preferenceStrafeSpeed = 0.5f;


    public InputField preferenceFieldHealth;
    public InputField preferenceFieldStamina;
    public InputField preferenceFieldStaminaConsume;
    public InputField preferenceFieldStaminaRegeneration;
    public InputField preferenceFieldMinDamage;
    public InputField preferenceFieldMaxDamage;
    public InputField preferenceFieldCritChance;
    public InputField preferenceFieldCritMultiplier;
    public InputField preferenceFieldInjureChance;
    public InputField preferenceFieldAbilityEvadeChance;
    public InputField preferenceFieldAbilityCritChance;
    public InputField preferenceFieldAbilityStunDuration;
    public InputField preferenceFieldAbilityShieldDuration;
    public InputField preferenceFieldAbilityShieldMultiplier;
    public InputField preferenceFieldInjureArmEffect;
    public InputField preferenceFieldInjureLegEffect;
    public InputField preferenceFieldStrafeSpeed;


    private Dictionary<int, string> langNotices = new Dictionary<int, string>();
    private RoomInfo selectedRoom = null;
    private TypedLobby lobby;

    public override void OnConnectedToMaster()
    {
        Debug.Log("CONNECTED!");
        lobby = new TypedLobby();
        lobby.Type = LobbyType.Default;
        lobby.Name = "Battle";
        PhotonNetwork.JoinLobby(lobby);
    }

    public override void OnJoinedLobby()
    {
        base.OnJoinedLobby();
        int i;
        Debug.Log("OnJoinedLobby: " + PhotonNetwork.networkingPeer.lobby.Name + " [" + PhotonNetwork.networkingPeer.lobby.Type + "] (" + PhotonNetwork.networkingPeer.insideLobby + ")");
        joinButton.interactable = true;
    }

    public override void OnReceivedRoomListUpdate()
    {
        base.OnReceivedRoomListUpdate();
        int i;
        RoomInfo[] rooms = PhotonNetwork.GetRoomList();
        Debug.Log("Rooms: " + rooms.Length);
        for (i = 0; i < rooms.Length; i++)
        {
            if (selectedRoom == null && rooms[i].open && rooms[i].playerCount == 1)
            {
                selectedRoom = rooms[i];
            }
            Debug.Log("Room [" + rooms[i].name + "] players: " + rooms[i].playerCount);
        }
        if (selectedRoom != null)
        {
            joinButtonText.text = "Войти в бой #" + selectedRoom.name;
            roomIdField.text = selectedRoom.name;
        }
    }

    public override void OnJoinedRoom()
    {
        base.OnJoinedRoom();
        int i;
        Debug.Log("OnJoinedRoom: " + PhotonNetwork.networkingPeer.CurrentRoom.name + " (" + PhotonNetwork.networkingPeer.CurrentRoom.playerCount + ")");
        canvasConnect.enabled = false;
    }

    public override void OnCreatedRoom()
    {
        base.OnCreatedRoom();
        Debug.Log("OnCreatedRoom: " + PhotonNetwork.networkingPeer.CurrentRoom.name + " (" + PhotonNetwork.networkingPeer.CurrentRoom.playerCount + ")");
    }

    public override void OnPhotonCreateRoomFailed(object[] codeAndMsg)
    {
        base.OnPhotonCreateRoomFailed(codeAndMsg);
        startButton.interactable = true;
        startLocalButton.interactable = true;
    }

    public override void OnPhotonJoinRoomFailed(object[] codeAndMsg)
    {
        base.OnPhotonCreateRoomFailed(codeAndMsg);
        joinButtonText.text = "Создать бой";
        startButton.interactable = true;
        startLocalButton.interactable = true;
    }

    void Start()
    {
        Application.runInBackground = true;
        //NetManager.singleton.StartMatchMaker();
        canvasConnect.enabled = true;
        canvasPlay.enabled = false;
        canvasSettings.enabled = false;
        roomIdField.text = "" + UnityEngine.Random.Range(1, 9);
        settingsButton.onClick.AddListener(delegate(){
            if(canvasSettings.enabled)
            {
                canvasSettings.enabled = false;
            }
            else
            {
                canvasSettings.enabled = true;
            }
        });
        startButton.onClick.AddListener(delegate() {
            PhotonNetwork.logLevel = PhotonLogLevel.ErrorsOnly;
            //PhotonNetwork.logLevel = PhotonLogLevel.Full;
            PhotonNetwork.OnEventCall += OnEvent;
            Debug.Log("Connecting to Photon Server start #1");
            if (PhotonNetwork.ConnectUsingSettings("1.0"))
            {
                Debug.Log("Connecting to Photon Server process... #1");
                UpdatePreferences();
                gameNetwork = GameObject.Instantiate(gameNetworkPrefab).GetComponent<GameNetwork>();
                gameNetwork.camera = camera;
                gameNetwork.gameMatchMaker = this;
                gameNetwork.isServer = false;
                gameNetwork.isLocal = false;
            }
            else
            {
                Debug.LogError("Connection to Photon Server failed");
                startButton.interactable = true;
                startLocalButton.interactable = true;
                DestroyImmediate(gameNetwork);
            }
            startButton.interactable = false;
            startLocalButton.interactable = false;
            //CreateInternetMatch(roomIdField.text);
        });
        joinButton.onClick.AddListener(delegate () {
            Debug.Log("Joining room");
            if (selectedRoom == null)
            {
                RoomOptions roomOptions = new RoomOptions();
                roomOptions.IsOpen = true;
                roomOptions.IsVisible = true;
                roomOptions.MaxPlayers = 2;
                if (PhotonNetwork.CreateRoom(roomIdField.text, roomOptions, lobby))
                {
                    Debug.Log("Room creating!");
                }
                else
                {
                    Debug.LogError("Can't create room");
                }
            }
            else
            {
                if (PhotonNetwork.JoinRoom(selectedRoom.name))
                {
                    Debug.Log("Room joining!");
                }
                else
                {
                    Debug.LogError("Can't join room");
                }
            }
            //FindInternetMatch(roomIdField.text);
            //canvasConnect.enabled = false;
            joinButton.interactable = false;
        });
        joinButton.interactable = false;
        startLocalButton.onClick.AddListener(delegate () {
            UpdatePreferences();
            gameNetwork = GameObject.Instantiate(gameNetworkPrefab).GetComponent<GameNetwork>();
            gameNetwork.camera = camera;
            gameNetwork.gameMatchMaker = this;
            gameNetwork.isLocal = true;
        });
        //((NetManager)NetManager.singleton).ServerConnect += OnServerConnect;




        langNotices.Add(0, "");
        langNotices.Add(1, "К");
        langNotices.Add(2, "Щ");
        langNotices.Add(3, "ЩИТ");
        langNotices.Add(4, "УКЛОНЕНИЕ");
        langNotices.Add(5, "ОГЛУШЕН");
        langNotices.Add(6, "РУКА");
        langNotices.Add(7, "НОГА");
        langNotices.Add(8, "ОГЛУШЕНИЕ");


    }

    void OnEvent(byte eventCode, object content, int senderId)
    {
        BaseObjectMessage baseObjectMessage;
        PlayerObject playerObject = null;
        PlayerController playerController = null;
        //Debug.Log("RECEIVE EVENT[" + eventCode + "] from [" + senderId + "]");
        switch (eventCode)
        {
            case 1:
                baseObjectMessage = new BaseObjectMessage();
                baseObjectMessage.Unpack((byte[])content);
                gameNetwork.ClientInit();
                gameNetwork.playerId = baseObjectMessage.id;
                playerObject = (PlayerObject)gameNetwork.location.GetObject(gameNetwork.playerId);
                if (playerObject != null)
                {
                    camera.transform.position = new Vector3(camera.transform.position.x, camera.transform.position.y, playerObject.position.z * 10.0f);
                    if (gameNetwork.playerId == 1)
                    {
                        camera.transform.eulerAngles = new Vector3(camera.transform.eulerAngles.x, 180.0f, camera.transform.eulerAngles.z);
                    }
                }
                playerObject = (PlayerObject)gameNetwork.location.GetObject(gameNetwork.playerId == 1 ? 0 : 1);
                if (playerObject != null)
                {
                    playerController = (Instantiate(gameNetwork.bodyPrefabs[0])).GetComponent<PlayerController>();
                    playerController.gameNetwork = gameNetwork;
                    playerController.obj = playerObject;
                    playerObject.visualObject = playerController;
                    playerController.transform.position = playerObject.position * 10.0f;
                    playerController.transform.localScale *= 10.0f;
                }
                canvasPlay.enabled = true;
                PhotonNetwork.networkingPeer.OpCustom((byte)1, new Dictionary<byte, object> { { 100, "CLIENT_JOINED" } }, true);
                break;
            case 2:
                SpawnObjectMessage spawnObjectMessage = new SpawnObjectMessage();
                spawnObjectMessage.Unpack((byte[])content);
                Debug.Log(Time.fixedTime + " Spawn." + spawnObjectMessage.objectType + " [" + spawnObjectMessage.id + "]");
                gameNetwork.RpcSpawnObject(spawnObjectMessage.id, spawnObjectMessage.objectType, spawnObjectMessage.newPosition, spawnObjectMessage.newFloat, spawnObjectMessage.visualId);
                break;
            case 3:
                DestroyObjectMessage destroyObjectMessage = new DestroyObjectMessage();
                destroyObjectMessage.Unpack((byte[])content);
                Debug.Log(Time.fixedTime + " Destroy [" + destroyObjectMessage.id + "]");
                gameNetwork.RpcDestroyObject(destroyObjectMessage.id);
                break;
            case 4:
                MoveObjectMessage moveObjectMessage = new MoveObjectMessage();
                moveObjectMessage.Unpack((byte[])content);
                gameNetwork.RpcMoveObject(moveObjectMessage.id, moveObjectMessage.newPosition, moveObjectMessage.newFloat, moveObjectMessage.timestamp);
                break;
            case 5:
                UpdatePlayerMessage updatePlayerMessage = new UpdatePlayerMessage();
                updatePlayerMessage.Unpack((byte[])content);
                //Debug.Log("Player[" + updatePlayerMessage.id + "] health: " + updatePlayerMessage.newHealth + " ; stamina: " + updatePlayerMessage.newStamina);
                gameNetwork.RpcUpdatePlayer(updatePlayerMessage.id, updatePlayerMessage.newHealth, updatePlayerMessage.newStamina);
                break;
            case 6:
                gameNetwork.RpcRearmMissile();
                break;
            case 7:
                baseObjectMessage = new BaseObjectMessage();
                baseObjectMessage.Unpack((byte[])content);
                gameNetwork.RpcFlashPlayer(baseObjectMessage.id);
                break;
            case 8:
                baseObjectMessage = new BaseObjectMessage();
                baseObjectMessage.Unpack((byte[])content);
                gameNetwork.RpcGameOver(baseObjectMessage.id);
                break;
            case 9:
                SetAbilityMessage setAbilityMessage = new SetAbilityMessage();
                setAbilityMessage.Unpack((byte[])content);
                gameNetwork.RpcSetAbility(setAbilityMessage.active, setAbilityMessage.id);
                break;
            case 10:
                NoticeMessage noticeMessage = new NoticeMessage();
                noticeMessage.Unpack((byte[])content);
                string noticeText = "";
                if (noticeMessage.color == 1)
                {
                    noticeText += "-";
                }
                else
                {
                    noticeText += "+";
                }
                if (noticeMessage.prefixMessage != -1)
                {
                    noticeText += " " + langNotices[noticeMessage.prefixMessage];
                }
                if (noticeMessage.numericValue != 0)
                {
                    noticeText += " " + noticeMessage.numericValue;
                }
                if (noticeMessage.suffixMessage != -1)
                {
                    noticeText += " " + langNotices[noticeMessage.suffixMessage];
                }
                gameNetwork.RpcShowNotice(noticeMessage.id, noticeText, noticeMessage.offset, noticeMessage.color, noticeMessage.floating);
                break;
            case 11:
                baseObjectMessage = new BaseObjectMessage();
                baseObjectMessage.Unpack((byte[])content);
                gameNetwork.RpcFlashPassiveAbility(baseObjectMessage.id);
                break;
            case 12:
                baseObjectMessage = new BaseObjectMessage();
                baseObjectMessage.Unpack((byte[])content);
                gameNetwork.RpcFlashObstruction(baseObjectMessage.id);
                break;
        }
    }

    void OnGUI()
    {
        if(Input.GetKeyDown(KeyCode.Escape))
        {
            Application.Quit();
        }
    }

    private void OnServerConnect(object sender, NetManager.NetworkConnectionEventArgs e)
    {
        if (e.conn.address != "localServer" && e.conn.address != "localClient")
        {
            gameNetwork = GameObject.Instantiate(gameNetworkPrefab).GetComponent<GameNetwork>();
            gameNetwork.camera = camera;
            gameNetwork.gameMatchMaker = this;
            NetworkServer.Spawn(gameNetwork.gameObject);
        }
        //Debug.Log("Client connected: " + e.conn.address);
    }

    public void CreateInternetMatch(string matchName)
    {
        UpdatePreferences();
        //Debug.Log("Create internet match");
        //NetManager.singleton.matchMaker.CreateMatch(matchName, 4, true, "", "", "", 0, 0, OnInternetMatchCreate);
    }

    private void OnInternetMatchCreate(bool success, string extendedInfo, MatchInfo hostInfo)
    {
        if (hostInfo != null)
        {
            //Debug.Log("Create match succeeded");
            if(gameNetwork != null)
            {
                NetManager.singleton.StopHost();
                NetManager.singleton.StopServer();
                NetManager.singleton.StopMatchMaker();
                NetManager.singleton.StartMatchMaker();
            }
            //NetworkServer.Listen(hostInfo, NetManager.singleton.networkPort);
            //NetManager.singleton.StartHost(hostInfo);
            canvasPlay.enabled = true;
        }
        else
        {
            //Debug.LogError("Create match failed");
        }
    }

    public void FindInternetMatch(string matchName)
    {
        if (gameNetwork != null)
        {
            //NetManager.singleton.StopMatchMaker();
            //NetManager.singleton.StartMatchMaker();
        }
        storedMatchName = matchName;
        //NetManager.singleton.matchMaker.ListMatches(0, 20, matchName, true, 0, 0, OnInternetMatchList);
    }

    private void OnInternetMatchList(bool success, string extendedInfo, List<MatchInfoSnapshot> matchList)
    {
        if (matchList != null)
        {
            if (matchList.Count != 0)
            {
                //NetManager.singleton.matchMaker.JoinMatch(matchList[matchList.Count - 1].networkId, "", "", "", 0, 0, OnJoinInternetMatch);
            }
            else
            {
                joinAttempts++;
                //Debug.Log("No matches in requested room! Attempt: " + joinAttempts);
                if (joinAttempts < 10)
                {
                    FindInternetMatch(storedMatchName);
                }
                else
                {
                    joinAttempts = 0;
                    //Debug.Log("Failed 10 join attempts");
                }
            }
        }
        else
        {
            //Debug.LogError("Couldn't connect to match maker");
        }
    }

    //this method is called when your request to join a match is returned
    private void OnJoinInternetMatch(bool success, string extendedInfo, MatchInfo matchInfo)
    {
        //NetManager.singleton.OnServerConnect()
        if (matchInfo != null)
        {
            //Debug.Log("Able to join a match");
            //NetManager.singleton.StartClient(matchInfo);
            canvasPlay.enabled = true;
        }
        else
        {
            //Debug.LogError("Join match failed");
        }
    }

    public void UpdatePreferences()
    {
        preferenceHealth = float.Parse(preferenceFieldHealth.text);
        preferenceStamina = float.Parse(preferenceFieldStamina.text);
        preferenceStaminaConsume = float.Parse(preferenceFieldStaminaConsume.text);
        preferenceStaminaRegeneration = float.Parse(preferenceFieldStaminaRegeneration.text);
        preferenceMinDamage = float.Parse(preferenceFieldMinDamage.text);
        preferenceMaxDamage = float.Parse(preferenceFieldMaxDamage.text);
        preferenceCritChance = float.Parse(preferenceFieldCritChance.text) * 0.01f;
        preferenceCritMultiplier = float.Parse(preferenceFieldCritMultiplier.text);
        preferenceInjureChance = float.Parse(preferenceFieldInjureChance.text) * 0.01f;
        preferenceAbilityEvadeChance = float.Parse(preferenceFieldAbilityEvadeChance.text) * 0.01f;
        preferenceAbilityCritChance = float.Parse(preferenceFieldAbilityCritChance.text) * 0.01f;
        preferenceAbilityStunDuration = float.Parse(preferenceFieldAbilityStunDuration.text);
        preferenceAbilityShieldDuration = float.Parse(preferenceFieldAbilityShieldDuration.text);
        preferenceAbilityShieldMultiplier = float.Parse(preferenceFieldAbilityShieldMultiplier.text) * 0.01f;
        preferenceInjureArmEffect = float.Parse(preferenceFieldInjureArmEffect.text) * 0.01f;
        preferenceInjureLegEffect = float.Parse(preferenceFieldInjureLegEffect.text) * 0.01f;
        preferenceStrafeSpeed = float.Parse(preferenceFieldStrafeSpeed.text);
    }

}