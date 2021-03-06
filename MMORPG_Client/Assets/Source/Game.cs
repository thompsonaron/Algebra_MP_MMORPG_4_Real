﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Game : MonoBehaviour
{
    public GameObject playerTemplate;
    public GameObject opponentsTemplate;
    public GameObject[] spawnPoints;
    private GameObject[] players = new GameObject[3];
    private Match match = new Match();
    private int myPlayerIndex = -1;
    public int myMovementSpeed = 7;

    public Text info;

    // Start is called before the first frame update
    void Start()
    {
        Net.sendGamePacket(new NetPackett() { messageType = MessageType.GameLoaded, data = Serializator.serialize(Net.myLobbyID + ":" + Net.myMatchID) });    //request
    }
    public float speed = 10f;
    // Update is called once per frame
    void Update()
    {
        var packages = Net.doUpdate();
        foreach (var package in packages)
        {
            if (package.messageType == MessageType.SpawnPlayer)  //response
            {
                Match match = new Match();
                match = Serializator.DeserializeMatch(package.data);
                this.match = match;
                Net.myMatchID = match.matchID;
                for (int i = 0; i < this.match.matchPlayers.Count; i++)
                {
                    // if it is me, instantiate player color and set myGameID
                    if (this.match.matchPlayers[i].lobbyId == Net.myLobbyID)
                    {
                        players[i] = Instantiate(playerTemplate, spawnPoints[i].transform.position, Quaternion.identity);
                        Net.myGameID = this.match.matchPlayers[i].gameId;
                        myPlayerIndex = i;
                        info.text = "My index: " + i + "ID: " + this.match.matchPlayers[i].gameId;
                    }
                    // else it is others
                    else
                    {
                        players[i] = Instantiate(opponentsTemplate, spawnPoints[i].transform.position, Quaternion.identity);
                    }
                }
            }
            else if (package.messageType == MessageType.OtherPlayerMoved)
            {

                float step = speed * Time.deltaTime;
                PlayerPosition playerPosition = new PlayerPosition();
                playerPosition = Serializator.DeserializePlayerPosition(package.data);

                // Movement in steps
                players[playerPosition.playerIndex].transform.position = Vector3.MoveTowards(players[playerPosition.playerIndex].transform.position, new Vector3(playerPosition.posX, playerPosition.posY, playerPosition.posZ), step);
                
                // Instant movement
                //players[playerPosition.playerIndex].transform.position = new Vector3(playerPosition.posX, playerPosition.posY, playerPosition.posZ);
            }
            else if (package.messageType == MessageType.ExitGame)
            {
                #if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
                #else
				Application.Quit();
                #endif
            }
        }

        Moving();
    }

    private void Moving()
    {
        if (myPlayerIndex != -1)
        {
            var myPlayer = players[myPlayerIndex].transform;
            var direction = Vector3.zero;
            if (Input.GetKey(KeyCode.W))
            {
                direction += Vector3.forward;
            }
            if (Input.GetKey(KeyCode.A))
            {
                direction += Vector3.left;
            }
            if (Input.GetKey(KeyCode.S))
            {
                direction += Vector3.back;
            }
            if (Input.GetKey(KeyCode.D))
            {
                direction += Vector3.right;
            }

            direction.Normalize();
            if (direction != Vector3.zero)
            {
                var position = myPlayer.position + direction * myMovementSpeed * Time.deltaTime;
                //Vector3 relativePos = position - myPlayer.position;
                myPlayer.position = position;
                NetPackett packett = new NetPackett
                {
                    messageType = MessageType.ClientMoved,
                    data = Serializator.serialize(new PlayerPosition() { playerIndex = myPlayerIndex, matchID = Net.myMatchID, posX = myPlayer.position.x, posY = myPlayer.position.y, posZ = myPlayer.position.z })
                };
                Net.sendGamePacket(packett);
            }
        }
    }

    public void OnApplicationQuit()
    {
        Net.destroy();
    }
}
