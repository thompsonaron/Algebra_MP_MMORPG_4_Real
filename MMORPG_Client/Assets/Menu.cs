using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class Menu : MonoBehaviour
{
    public InputField inputElo;
    public Button btnJoinServer;
    public Button btnJoinGame;

    public void joinServer()
    {
        Net.joinServer();
        btnJoinGame.gameObject.SetActive(false);
        StartCoroutine(JoinedServerRoutine());
    }

    public void startMatchmaking()
    {
        Net.myElo = inputElo.text;
        Net.joinMatchmaking();
    }

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        var packets = Net.doUpdate();
        foreach (var packet in packets)
        {
            if (packet.messageType == MessageType.StartTheGame)
            {
                Net.myMatchID = int.Parse(Serializator.DeserializeString(packet.data));
                Net.joinGame();
                Debug.Log("JOIN GAME");
                SceneManager.LoadScene("Game");
            }
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
}
