using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WebSocketSharp;
using WebSocketSharp.Server;

namespace MMORPG_Server
{
    class MMORPG_Server
    {
        static List<Match> matches = new List<Match>();
        static int counter = 0;
        static void Main(string[] args)
        {
			WebSocketServer server = new WebSocketServer(8080);
			server.AddWebSocketService<LobbyBehavior>("/Lobby");	//localhost:8080/Lobby
			server.AddWebSocketService<GameBehavior>("/Game");  //localhost:8080/Game
			server.Start();
            while (true) { }
        }

		class LobbyBehavior : WebSocketBehavior
        {
			static List<Player> players = new List<Player>();

            protected override void OnOpen()
            {
				Console.WriteLine("Joined: " + ID);
				Player p = new Player();
				p.lobbyId = ID;
                players.Add(p);
                Sessions.SendTo(Serializator.serialize(new NetPackett() { data = Serializator.serialize(ID), messageType = MessageType.SendingLobbyID }), ID);
                //Send(("#msg:" + ID);
                base.OnOpen();
            }

            protected override void OnClose(CloseEventArgs e)
            {
                foreach (var player in players)
                {
                    if (player.lobbyId.Equals(ID))
                    {
                        players.Remove(player);
                    }
                }
                base.OnClose(e);
            }

            protected override void OnMessage(MessageEventArgs e)
            {
                if (e.IsText)
                {
                    string[] data = e.Data.Split(':');
                    if (data[0].Equals("#elo"))
                    {
                        string playerLobbyID = data[1];
                        int elo = int.Parse(data[2]);
                        StartMatchmaking(elo, playerLobbyID);
                    }
                }
                base.OnMessage(e);
            }

            public void StartMatchmaking(int elo, string playerID)
            {
                Console.WriteLine("MATCHMAKING");
                Player current = new Player();
                foreach (var player in players)
                {
                    if (player.lobbyId == playerID)
                    {
                        player.elo = elo;
                        player.isMatchmaking = true;
                        current = player;
                    }
                }

                //if (players.Count < 3)
                //{
                //    return;
                //}

                Match match = new Match();
                match.matchPlayers.Add(current);
                foreach (var player in players)
                {
                    Console.WriteLine(Math.Abs(elo - player.elo));
                    //(Math.Abs(elo - player.elo) calculates "distance"
                    if ((player.lobbyId != playerID) && (Math.Abs(elo - player.elo) <= 100) && player.isMatchmaking)
                    {
                        Console.WriteLine("Found a match");
                        match.matchPlayers.Add(player);
                        if (match.matchPlayers.Count == 3)
                        {
                            match.matchID = counter++;
                            matches.Add(match);
                            foreach (var p in match.matchPlayers)
                            {
                                Sessions.SendTo(Serializator.serialize(new NetPackett() { data = Serializator.serialize(match.matchID.ToString()), messageType = MessageType.StartTheGame }), p.lobbyId);
                                Console.WriteLine(p.lobbyId);
                                //foreach (var item in players)
                                //{
                                //    if (item.lobbyId == p.lobbyId)
                                //    {
                                //        players.Remove(item);
                                //    }
                                //}
                            }
                        }
                    }
                }
            }
        }

		class GameBehavior : WebSocketBehavior
        {
            protected override void OnOpen()
            {
                base.OnOpen();
            }

            protected override void OnClose(CloseEventArgs e)
            {
                foreach (var match in matches)
                {
                    foreach (var player in match.matchPlayers)
                    {
                        if (player.gameId.Equals(ID))
                        {
                            match.matchPlayers.Remove(player);
                        }
                    }
                }
                base.OnClose(e);
            }

            protected override void OnMessage(MessageEventArgs e)
            {
                if (e.IsBinary)
                {
                    NetPackett packett = new NetPackett();
                    packett = Serializator.DeserializeNetPackett(e.RawData);

                    if (packett.messageType == MessageType.GameLoaded)
                    {
                        string data = Serializator.DeserializeString(packett.data);
                        string[] sortedData = data.Split(':');
                        Console.WriteLine("Game: " + sortedData[0] + " " + sortedData[1]);

                        Match match = getMatch(int.Parse(sortedData[1]));
                        foreach (var player in match.matchPlayers)
                        {
                            if (player.lobbyId == sortedData[0])
                            {
                                player.gameId = ID;
                            }
                            else
                            {
                                Console.WriteLine(">>" + player.gameId + "<<");
                            }
                        }

                        bool allLoaded = true;
                        foreach (var player in match.matchPlayers)
                        {
                            if (player.gameId.IsNullOrEmpty())
                            {
                                allLoaded = false;
                            }
                        }
                        if (allLoaded)
                        {
                            Console.WriteLine("allLoaded");

                            foreach (var player in match.matchPlayers)
                            {
                                Sessions.SendTo(Serializator.serialize(new NetPackett() { data = Serializator.serialize(match), messageType = MessageType.SpawnPlayer }), player.gameId);
                            }
                            
                        }
                    }
                    else if (packett.messageType == MessageType.ClientMoved)
                    {
                        PlayerPosition pos = new PlayerPosition();
                        pos = Serializator.DeserializePlayerPosition(packett.data);
                        NetPackett netPackett = new NetPackett() { messageType = MessageType.OtherPlayerMoved, data = packett.data };
                        Console.WriteLine("Whotindex " + pos.matchID);
                        foreach (var match in matches)
                        {
                            if (match.matchID == pos.matchID)
                            {
                                foreach (var player in match.matchPlayers)
                                {
                                    if (!ID.Equals(player.gameId))
                                    {
                                        Console.WriteLine("Oponent ID:" + player.gameId);
                                        Sessions.SendTo(Serializator.serialize(netPackett), player.gameId);
                                    }
                                    else
                                    {
                                        Console.WriteLine("Me      ID:" + player.gameId);

                                    }
                                }
                            }
                        }
                    }
                }
                base.OnMessage(e);
            }

            public Match getMatch(int matchID)
            {
                foreach (var match in matches)
                {
                    if (match.matchID == matchID)
                    {
                        return match;
                    }
                }
                return null;
            }
        }
    }

	public enum MessageType
	{
		PlayerJoin,             //client - server	0
		StartTheGame,           //server - client	1
		GameLoaded,             //client - server	2
		SpawnPlayer,            //server - client	3
		ClientMoved,            //clinet - server	4
		OtherPlayerMoved,       //server - client	5
		SpawnFireball,          //client - server	6
		ClientSpawnedFireball,  //server - client	7
		ExitGame,               //server - client	8
        SendingLobbyID          //server - client   9
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
}
