using System;
using System.Collections.Generic;

namespace MMORPG_Server
{
    public enum MessageType
    {
        PlayerJoin,             //client - server	0
        StartTheGame,           //server - client	1
        GameLoaded,             //client - server	2
        SpawnPlayer,            //server - client	3
        ClientMoved,            //clinet - server	4
        OtherPlayerMoved,       //server - client	5
        ExitGame,               //server - client	6
        SendingLobbyID,         //server - client   7
        SendingElo              //client - server   8
    }
    [Serializable]
    public class Player
    {
        public string lobbyId;
        public string gameId;
        public int elo;
        public bool isMatchmaking;
    }

    [Serializable]
    public class Match
    {
        public List<Player> matchPlayers = new List<Player>();
        public int matchID;
    }

    [Serializable]
    public class PlayerPosition
    {
        public float posX;
        public float posY;
        public float posZ;
        public int playerIndex;
        public int matchID;
    }

    [Serializable]
    public class NetPackett
    {
        public MessageType messageType;
        public byte[] data;
    }
}