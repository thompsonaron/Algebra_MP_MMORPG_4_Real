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
        // used as ID for matches
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
				Console.WriteLine("New player joined: " + ID);
                // Adding new player to the list and sending it's lobby ID back 
				Player newPlayer = new Player();
				newPlayer.lobbyId = ID;
                players.Add(newPlayer);
                Sessions.SendTo(Serializator.serialize(new NetPackett() { data = Serializator.serialize(ID), messageType = MessageType.SendingLobbyID }), ID);
                //Send(("#msg:" + ID);
                base.OnOpen();
            }

            protected override void OnClose(CloseEventArgs e)
            {
                // removing player from list on close
                // foreach will cause errors due to changes
                for (int i = 0; i < players.Count; i++)
                {
                    if (players[i].lobbyId.Equals(ID)) { players.RemoveAt(i); }
                }
                base.OnClose(e);
            }

            protected override void OnError(ErrorEventArgs e)
            {
                Console.WriteLine("Lobby error: " + e.Message);
                Console.WriteLine("Lobby error: " + e.Exception);
                base.OnError(e);
            }

            protected override void OnMessage(MessageEventArgs e)
            {
                if (e.IsBinary)
                {
                    NetPackett packett = new NetPackett();
                    packett = Serializator.DeserializeNetPackett(e.RawData);
                    // receiving ELO and starting matchmaking
                    if (packett.messageType == MessageType.SendingElo)
                    {
                        int elo = int.Parse(Serializator.DeserializeString(packett.data));
                        StartMatchmaking(elo, ID);
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

            protected override void OnError(ErrorEventArgs e)
            {
                Console.WriteLine("GameBehavior error: " + e.Message);
                Console.WriteLine("GameBehavior error: " + e.Exception);
                base.OnError(e);
            }

            protected override void OnClose(CloseEventArgs e)
            {
                // removing player from list on close
                foreach (var match in matches)
                {
                    // foreach will cause errors due to changes
                    for (int i = 0; i < match.matchPlayers.Count; i++)
                    {
                        if (match.matchPlayers[i] .gameId.Equals(ID)) { match.matchPlayers.RemoveAt(i); }
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
                      //  Console.WriteLine("Game: " + sortedData[0] + " " + sortedData[1]);

                        Match match = getMatch(int.Parse(sortedData[1]));
                        foreach (var player in match.matchPlayers)
                        {
                            if (player.lobbyId == sortedData[0])
                            {
                                player.gameId = ID;
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
                        foreach (var match in matches)
                        {
                            if (match.matchID == pos.matchID)
                            {
                                foreach (var player in match.matchPlayers)
                                {
                                    if (!ID.Equals(player.gameId))
                                    {
                                        Sessions.SendTo(Serializator.serialize(netPackett), player.gameId);
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
}