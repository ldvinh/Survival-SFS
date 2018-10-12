using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System;
using System.Collections;
using System.Collections.Generic;
using Sfs2X;
using Sfs2X.Logging;
using Sfs2X.Util;
using Sfs2X.Core;
using Sfs2X.Entities;
using Sfs2X.Requests;

public class Connector : MonoBehaviour {

    [Tooltip("IP address or domain name of the SmartFoxServer 2X instance")]
    public string Host = "127.0.0.1";
    public int TcpPort = 9933;
    public int WSPort = 8080;
    public string Zone = "Zone1";
    public string Room = "ROOMZ";
    public InputField m_UserName;
    public Button m_Connect;
    private SmartFox sfs;
    //ConfigData cfg;
    void Start () {
        
    }
	
	void Update () {
        if (sfs != null)
            sfs.ProcessEvents();
    }

    public void onConnectClicked()
    {
        EnableConnect(false);
        // Set connection parameters
        ConfigData cfg = new ConfigData();
        cfg.Host = Host;
        cfg.Port = TcpPort;
        cfg.Zone = Zone;
        cfg.UseBlueBox = false;
        sfs = new SmartFox();
        sfs.ThreadSafeMode = true;

        sfs.AddEventListener(SFSEvent.CONNECTION, OnConnection);
        sfs.AddEventListener(SFSEvent.CONNECTION_LOST, OnConnectionLost);
        sfs.AddEventListener(SFSEvent.LOGIN, OnLogin);
        sfs.AddEventListener(SFSEvent.LOGIN_ERROR, OnLoginError);
        sfs.AddEventListener(SFSEvent.ROOM_JOIN, OnRoomJoin);
        sfs.AddEventListener(SFSEvent.ROOM_JOIN_ERROR, OnRoomJoinError);

        // Connect to SFS2X
        sfs.Connect(cfg);
    }
    private void EnableConnect(bool enable)
    {
        m_UserName.interactable = enable;
        m_Connect.interactable = enable;
    }
    private void reset()
    {
        // Remove SFS2X listeners
        sfs.RemoveAllEventListeners();

        // Enable interface
        EnableConnect(true);
    }
    private void OnConnection(BaseEvent evt)
    {
        if ((bool)evt.Params["success"])
        {
            // Save reference to the SmartFox instance in a static field, to share it among different scenes
            SmartFoxConnection.Connection = sfs;
            Debug.Log("SFS2X API version: " + sfs.Version);
            Debug.Log("Connection mode is: " + sfs.ConnectionMode);
            // Login
            sfs.Send(new Sfs2X.Requests.LoginRequest(m_UserName.text));
        }
        else
        {
            reset();
            Debug.LogError("connect Fail");
        }
    }
    private void OnConnectionLost(BaseEvent evt)
    {
        reset();
        string reason = (string)evt.Params["reason"];
        if (reason != ClientDisconnectionReason.MANUAL)
        {
            // Show error message
            Debug.LogError("Connection was lost; reason is: " + reason);
        }
    }
    private void OnLogin(BaseEvent evt)
    {
        Debug.Log("OnLogin success");
        string roomName = Room;
        
        // We either create the Game Room or join it if it exists already
        if (sfs.RoomManager.ContainsRoom(roomName))
        {
            sfs.Send(new JoinRoomRequest(roomName));
        }
        else
        {
            RoomSettings settings = new RoomSettings(roomName);
            settings.MaxUsers = 40;
            sfs.Send(new CreateRoomRequest(settings, true));
        }
    }

    private void OnLoginError(BaseEvent evt)
    {
        // Disconnect
        sfs.Disconnect();
        // Remove SFS2X listeners and re-enable interface
        reset();
        // Show error message
        Debug.LogError("Login failed: " + (string)evt.Params["errorMessage"]);
    }

    private void OnRoomJoin(BaseEvent evt)
    {
        Debug.Log("OnRoomJoin success");
        reset();        
        SceneManager.LoadScene("GameScene");
    }

    private void OnRoomJoinError(BaseEvent evt)
    {
        // Show error message
        Debug.LogError("Room join failed: " + (string)evt.Params["errorMessage"]);
    }

}
