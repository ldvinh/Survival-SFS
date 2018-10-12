using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using Sfs2X;
using Sfs2X.Core;
using Sfs2X.Entities;
using Sfs2X.Entities.Data;
using Sfs2X.Entities.Variables;
using Sfs2X.Requests;
public class GameManager : MonoBehaviour {
    //----------------------------------------------------------
    // Public properties
    //----------------------------------------------------------
    public GameObject[] playerModels;

    public Camera PlayerCamera;
    //----------------------------------------------------------
    // Private properties
    //----------------------------------------------------------
    private SmartFox sfs;
    private GameObject localPlayer;
    private PlayerController localPlayerController;
    private Dictionary<SFSUser, GameObject> remotePlayers = new Dictionary<SFSUser, GameObject>();
    void Start()
    {
        if (!SmartFoxConnection.IsInitialized)
        {
            SceneManager.LoadScene("ConnectScene");
            return;
        }
        Debug.LogWarning("Start");
        sfs = SmartFoxConnection.Connection;
        // Register callback delegates
        sfs.AddEventListener(SFSEvent.OBJECT_MESSAGE, OnObjectMessage);
        sfs.AddEventListener(SFSEvent.CONNECTION_LOST, OnConnectionLost);
        sfs.AddEventListener(SFSEvent.USER_VARIABLES_UPDATE, OnUserVariableUpdate);
        sfs.AddEventListener(SFSEvent.USER_EXIT_ROOM, OnUserExitRoom);
        sfs.AddEventListener(SFSEvent.USER_ENTER_ROOM, OnUserEnterRoom);
        // Get random avatar and color and spawn player
        int numModel = 0;
        SpawnLocalPlayer(numModel);

    }
    void FixedUpdate()
    {
        if (sfs != null)
        {
            sfs.ProcessEvents();

            // If we spawned a local player, send position if movement is dirty
            if (localPlayer != null && localPlayerController != null && localPlayerController.MovementDirty)
            {
                List<UserVariable> userVariables = new List<UserVariable>();
                userVariables.Add(new SFSUserVariable("x", (double)localPlayer.transform.position.x));
                userVariables.Add(new SFSUserVariable("y", (double)localPlayer.transform.position.y));
                userVariables.Add(new SFSUserVariable("z", (double)localPlayer.transform.position.z));
                userVariables.Add(new SFSUserVariable("rot", (double)localPlayer.transform.rotation.eulerAngles.y));
                sfs.Send(new SetUserVariablesRequest(userVariables));
                localPlayerController.MovementDirty = false;
            }
        }
    }

    void OnApplicationQuit()
    {
        RemoveLocalPlayer();
    }
    //----------------------------------------------------------
    // SmartFoxServer event listeners
    //----------------------------------------------------------
    public void OnUserExitRoom(BaseEvent evt)
    {
        // Someone left - lets make certain they are removed if they didn't nicely send a remove command
        SFSUser user = (SFSUser)evt.Params["user"];
        RemoveRemotePlayer(user);
    }
    public void OnUserEnterRoom(BaseEvent evt)
    {
        Debug.LogWarning("OnUserEnterRoom");
        // User joined - and we might be standing still (not sending position updates); so let's send him our position
        if (localPlayer != null)
        {
            List<UserVariable> userVariables = new List<UserVariable>();
            userVariables.Add(new SFSUserVariable("x", (double)localPlayer.transform.position.x));
            userVariables.Add(new SFSUserVariable("y", (double)localPlayer.transform.position.y));
            userVariables.Add(new SFSUserVariable("z", (double)localPlayer.transform.position.z));
            userVariables.Add(new SFSUserVariable("rot", (double)localPlayer.transform.rotation.eulerAngles.y));
            sfs.Send(new SetUserVariablesRequest(userVariables));
        }
    }
    public void OnConnectionLost(BaseEvent evt)
    {
        Debug.LogWarning("OnConnectionLost");
        sfs.RemoveAllEventListeners();
        SceneManager.LoadScene("Connection");
    }
    public void OnObjectMessage(BaseEvent evt)
    {
        Debug.LogWarning("OnObjectMessage");
        ISFSObject dataObj = (SFSObject)evt.Params["message"];
        SFSUser sender = (SFSUser)evt.Params["sender"];

        if (dataObj.ContainsKey("cmd"))
        {
            switch (dataObj.GetUtfString("cmd"))
            {
                case "rm":
                    Debug.Log("Removing player unit " + sender.Id);
                    RemoveRemotePlayer(sender);
                    break;
            }
        }
    }
    public void OnUserVariableUpdate(BaseEvent evt)
    {
        //Debug.LogWarning("OnUserVariableUpdate");
        List<string> changedVars = (List<string>)evt.Params["changedVars"];
        SFSUser user = (SFSUser)evt.Params["user"];
        if (user == sfs.MySelf) return;
        if (!remotePlayers.ContainsKey(user))
        {
            // New client just started transmitting - lets create remote player
            Vector3 pos = new Vector3(0, 1, 0);
            if (user.ContainsVariable("x") && user.ContainsVariable("y") && user.ContainsVariable("z"))
            {
                pos.x = (float)user.GetVariable("x").GetDoubleValue();
                pos.y = (float)user.GetVariable("y").GetDoubleValue();
                pos.z = (float)user.GetVariable("z").GetDoubleValue();
            }
            float rotAngle = 0;
            if (user.ContainsVariable("rot"))
            {
                rotAngle = (float)user.GetVariable("rot").GetDoubleValue();
            }
            int numModel = 0;
            if (user.ContainsVariable("model"))
            {
                numModel = user.GetVariable("model").GetIntValue();
            }
            SpawnRemotePlayer(user, numModel, pos, Quaternion.Euler(0, rotAngle, 0));
        }
        // Check if the remote user changed his position or rotation
        if (changedVars.Contains("x") && changedVars.Contains("y") && changedVars.Contains("z") && changedVars.Contains("rot"))
        {
            // Move the character to a new position...
            remotePlayers[user].GetComponent<SimpleRemoteInterpolation>().SetTransform(
                new Vector3((float)user.GetVariable("x").GetDoubleValue(), (float)user.GetVariable("y").GetDoubleValue(), (float)user.GetVariable("z").GetDoubleValue()),
                Quaternion.Euler(0, (float)user.GetVariable("rot").GetDoubleValue(), 0),
                true);
        }
    }
    //----------------------------------------------------------
    // Public interface methods for UI
    //----------------------------------------------------------
    public void Disconnect()
    {
        sfs.Disconnect();
    }
    private void SpawnLocalPlayer(int numModel)
    {
        Debug.LogWarning("SpawnLocalPlayer");
        Vector3 pos;
        Quaternion rot;
        
        // See if there already exists a model - if so, take its pos+rot before destroying it
        if (localPlayer != null)
        {
            pos = localPlayer.transform.position;
            rot = localPlayer.transform.rotation;
            //Camera.main.transform.parent = null;            
            Destroy(localPlayer);
        }
        else
        {
            pos = new Vector3(0, 1, 0);
            rot = Quaternion.identity;
        }

        // Lets spawn our local player model
        localPlayer = GameObject.Instantiate(playerModels[numModel]) as GameObject;
        localPlayer.transform.position = pos;
        localPlayer.transform.rotation = rot;

        

        // Since this is the local player, lets add a controller and fix the camera
        localPlayer.AddComponent<PlayerController>();
        localPlayerController = localPlayer.GetComponent<PlayerController>();        
        //Camera.main.transform.parent = localPlayer.transform;

        // Lets set the model and material choice and tell the others about it
        List<UserVariable> userVariables = new List<UserVariable>();
        userVariables.Add(new SFSUserVariable("model", numModel));        
        sfs.Send(new SetUserVariablesRequest(userVariables));
    }
    private void SpawnRemotePlayer(SFSUser user, int numModel, Vector3 pos, Quaternion rot)
    {
        Debug.LogWarning("SpawnRemotePlayer");
        // See if there already exists a model so we can destroy it first
        if (remotePlayers.ContainsKey(user) && remotePlayers[user] != null)
        {
            Destroy(remotePlayers[user]);
            remotePlayers.Remove(user);
        }

        // Lets spawn our remote player model
        GameObject remotePlayer = GameObject.Instantiate(playerModels[numModel]) as GameObject;
        remotePlayer.AddComponent<SimpleRemoteInterpolation>();
        remotePlayer.GetComponent<SimpleRemoteInterpolation>().SetTransform(pos, rot, false);

        // Lets track the dude
        remotePlayers.Add(user, remotePlayer);
    }

    private void RemoveLocalPlayer()
    {
        // Someone dropped off the grid. Lets remove him
        SFSObject obj = new SFSObject();
        obj.PutUtfString("cmd", "rm");
        sfs.Send(new ObjectMessageRequest(obj, sfs.LastJoinedRoom));
    }

    private void RemoveRemotePlayer(SFSUser user)
    {
        if (user == sfs.MySelf) return;
        if (remotePlayers.ContainsKey(user))
        {
            Destroy(remotePlayers[user]);
            remotePlayers.Remove(user);
        }
    }

}
