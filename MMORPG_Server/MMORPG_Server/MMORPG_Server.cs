using System;
using System.Collections.Generic;
using WebSocketSharp;
using WebSocketSharp.Server;

namespace MMORPG_Server
{
    class MMORPG_Server
    {
        static List<Match> matches = new List<Match>();
        // used as ID for matches
        static int counter = 0;
        readonly static int minimumMatchmakingRange = 100;

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
                Console.WriteLine("Player entering matchmaking");
                Player current = new Player();
                // finding player through lobby ID, enabling his isMatchmaking and setting elo
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
                    //(Math.Abs(elo - player.elo) calculates "distance"
                    if ((player.lobbyId != playerID) && (Math.Abs(elo - player.elo) <= minimumMatchmakingRange) && player.isMatchmaking)
                    {
                        match.matchPlayers.Add(player);
                        // if 3 players are in a match, add it to a list and give it id -> Send match id to those players and let them know they can play
                        if (match.matchPlayers.Count == 3)
                        {
                            Console.WriteLine("Found a match");
                            match.matchID = counter++;
                            matches.Add(match);
                            foreach (var p in match.matchPlayers)
                            {
                                NetPackett packett = new NetPackett()
                                {
                                    data = Serializator.serialize(match.matchID.ToString()),
                                    messageType = MessageType.StartTheGame
                                };
                                Sessions.SendTo(Serializator.serialize(packett), p.lobbyId);
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

                        Match match = getMatch(int.Parse(sortedData[1]));
                        // connecting lobbyID and gameID to all players in a match
                        foreach (var player in match.matchPlayers)
                        {
                            if (player.lobbyId == sortedData[0])
                            {
                                player.gameId = ID;
                            }
                        }

                        // checking if gameID is set to all players - as in: are they all loaded into a game
                        bool allLoaded = true;
                        foreach (var player in match.matchPlayers)
                        {
                            if (player.gameId.IsNullOrEmpty())
                            {
                                allLoaded = false;
                            }
                        }
                        // if All Loaded - send them match data and allow Player spawning
                        if (allLoaded)
                        {
                            Console.WriteLine("All players loaded");
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
                        // 1. in all matches finding my match through id
                        // 2. when I find the match - sending to my new position to all players except myself
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