using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class Menu : MonoBehaviour
{
    public InputField inputElo;
    public Button btnJoinServer;
    public Button btnJoinGame;
    public Text textMatchmaking;

    public void joinServer()
    {
        btnJoinServer.gameObject.SetActive(false);
        // connecting to lobby
        Net.joinServer();
        // enabling matchmaking input once connected
        StartCoroutine(JoinedServerRoutine());
    }

    public void startMatchmaking()
    {
        // disabling elo input and joining matchmaking
        Net.myElo = inputElo.text;
        btnJoinGame.gameObject.SetActive(false);
        inputElo.gameObject.SetActive(false);
        textMatchmaking.gameObject.SetActive(true);


        NetPackett netPackett = new NetPackett()
        {
            data = Serializator.serialize(Net.myElo),
            messageType = MessageType.SendingElo
        };

        Net.sendLobbyPacket(netPackett);
    }

    void Update()
    {
        var packets = Net.doUpdate();
        foreach (var packet in packets)
        {
            // matchmaking was success - receiving match ID and starting a Game
            if (packet.messageType == MessageType.StartTheGame)
            {
                Net.myMatchID = int.Parse(Serializator.DeserializeString(packet.data));
                Net.joinGame();
                SceneManager.LoadScene("Game");
            }
            // receiving response from server and my lobby ID, notifying that I have connected
            else if (packet.messageType == MessageType.SendingLobbyID)
            {
                Net.myLobbyID = Serializator.DeserializeString(packet.data);
                Net.isConnectedToLobby = true;
            }
        }
    }

    public IEnumerator JoinedServerRoutine()
    {
        yield return new WaitWhile(()=> (Net.isConnectedToLobby == false));
        
        btnJoinGame.gameObject.SetActive(true);
        inputElo.gameObject.SetActive(true);
    }

    public void OnApplicationQuit()
    {
        Net.destroy();
    }
}