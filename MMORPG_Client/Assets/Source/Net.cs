using System;
using System.Collections.Generic;
using WebSocketSharp;

public static class Net
{
    public static bool isConnectedToLobby;
    public static string myLobbyID;
    public static string myGameID;
    public static string myElo;
    public static int myMatchID;
    public static WebSocket clientSocketLobby;
    public static WebSocket clientSocketGame;

    public static List<NetPackett> receiving = new List<NetPackett>();

    // Lobby
    public static void joinServer()
    {
        isConnectedToLobby = false;
        clientSocketLobby = new WebSocket("ws://localhost:8080/Lobby");
        clientSocketLobby.OnOpen += ClientSocketLobby_OnOpen;
        clientSocketLobby.OnMessage += ClientSocketLobby_OnMessage;
        clientSocketLobby.OnClose += ClientSocketLobby_OnClose;
        clientSocketLobby.Connect();
    }

    private static void ClientSocketLobby_OnClose(object sender, CloseEventArgs e)
    {
        clientSocketLobby.Send(Serializator.serialize(new NetPackett { messageType = MessageType.ExitGame }));
    }

    private static void ClientSocketLobby_OnMessage(object sender, MessageEventArgs e)
    {
        if(e.IsBinary)
        {
            // serialize and add to receiving list
            NetPackett packet = new NetPackett();
            packet = Serializator.DeserializeNetPackett(e.RawData);
            receiving.Add(packet);
        }
    }

    private static void ClientSocketLobby_OnOpen(object sender, EventArgs e)
    {
        //isConnectedToLobby = true;
        Net.isConnectedToLobby = true;
    }

    public static void sendLobbyPacket(NetPackett packet)
    {
        clientSocketLobby.Send(Serializator.serialize(packet));
    }

    // Game
    public static void joinGame()
    {
        clientSocketGame = new WebSocket("ws://localhost:8080/Game");
        clientSocketGame.OnMessage += ClientSocketGame_OnMessage;
        clientSocketGame.OnClose += ClientSocketGame_OnClose;
        clientSocketGame.Connect();
    }

    private static void ClientSocketGame_OnClose(object sender, CloseEventArgs e)
    {
        clientSocketGame.Send(Serializator.serialize(new NetPackett { messageType = MessageType.ExitGame }));
    }

    private static void ClientSocketGame_OnMessage(object sender, MessageEventArgs e)
    {
        if (e.IsBinary)
        {
            NetPackett packet = new NetPackett();
            packet = Serializator.DeserializeNetPackett(e.RawData);
            receiving.Add(packet);
        }
    }

    public static void sendGamePacket(NetPackett packet)
    {
        clientSocketGame.Send(Serializator.serialize(packet));  
    }

    //API for polling
    public static List<NetPackett> doUpdate()
    {
        List<NetPackett> output = new List<NetPackett>(receiving);
        receiving.Clear();

        return output;
    }

    public static void destroy()
    {
        if (clientSocketLobby != null && clientSocketLobby.IsAlive)
        {
            clientSocketLobby.Close();
        }
        if (clientSocketGame != null && clientSocketGame.IsAlive)
        {
            clientSocketGame.Close();
        }
    }
}