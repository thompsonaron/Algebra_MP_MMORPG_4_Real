using System;
using System.Collections.Generic;
using System.IO;

public static class Serializator
{
    // NETPACKETT
    // custom modified to read Message Type
    public static byte[] serialize(NetPackett netpackett)
    {
        var s = new MemoryStream();
        var bW = new BinaryWriter(s);
        bW.Write((int)netpackett.messageType);
        bW.Write(netpackett.data.Length);
        foreach (var item in netpackett.data)
        {
            bW.Write(item);
        }
        return s.ToArray();
    }

    // custom modified to read Message Type
    public static NetPackett DeserializeNetPackett(byte[] b)
    {
        var s = new MemoryStream(b);
        var bR = new BinaryReader(s);
        var obj = new NetPackett();
        obj.messageType = (MessageType)bR.ReadInt32();
        int dataArraySize = bR.ReadInt32();
        obj.data = new Byte[dataArraySize];
        for (int i = 0; i < dataArraySize; i++)
        {
            obj.data[i] = bR.ReadByte();
        }
        return obj;
    }

    // custom modified to read Message Type
    private static NetPackett DeserializeNetPackett(ref byte[] b, ref MemoryStream s, ref BinaryReader bR)
    {
        var obj = new NetPackett();
        obj.messageType = (MessageType)bR.ReadInt32();
        int dataArraySize = bR.ReadInt32();
        obj.data = new Byte[dataArraySize];
        for (int i = 0; i < dataArraySize; i++)
        {
            obj.data[i] = bR.ReadByte();
        }
        return obj;
    }


    // PLAYER
    public static byte[] serialize(Player player)
    {
        var s = new MemoryStream();
        var bW = new BinaryWriter(s);
        bW.Write(player.lobbyId);
        bW.Write(player.gameId);
        bW.Write(player.elo);
        bW.Write(player.isMatchmaking);
        return s.ToArray();
    }

    public static Player DeserializePlayer(byte[] b)
    {
        var s = new MemoryStream(b);
        var bR = new BinaryReader(s);
        var obj = new Player();
        obj.lobbyId = bR.ReadString();
        obj.gameId = bR.ReadString();
        obj.elo = bR.ReadInt32();
        obj.isMatchmaking = bR.ReadBoolean();
        return obj;
    }

    private static Player DeserializePlayer(ref byte[] b, ref MemoryStream s, ref BinaryReader bR)
    {
        var obj = new Player();
        obj.lobbyId = bR.ReadString();
        obj.gameId = bR.ReadString();
        obj.elo = bR.ReadInt32();
        obj.isMatchmaking = bR.ReadBoolean();
        return obj;
    }

    // MATCH
    public static byte[] serialize(Match match)
    {
        var s = new MemoryStream();
        var bW = new BinaryWriter(s);
        bW.Write(match.matchPlayers.Count);
        foreach (var item in match.matchPlayers)
        {
            bW.Write(serialize(item));
        }
        bW.Write(match.matchID);
        return s.ToArray();
    }

    public static Match DeserializeMatch(byte[] b)
    {
        var s = new MemoryStream(b);
        var bR = new BinaryReader(s);
        var obj = new Match();
        obj.matchPlayers = new List<Player>();
        int matchPlayersListSize = bR.ReadInt32();
        for (int i = 0; i < matchPlayersListSize; i++)
        {
            obj.matchPlayers.Add(DeserializePlayer(ref b, ref s, ref bR));
        }
        obj.matchID = bR.ReadInt32();
        return obj;
    }

    private static Match DeserializeMatch(ref byte[] b, ref MemoryStream s, ref BinaryReader bR)
    {
        var obj = new Match();
        obj.matchPlayers = new List<Player>();
        int matchPlayersListSize = bR.ReadInt32();
        for (int i = 0; i < matchPlayersListSize; i++)
        {
            obj.matchPlayers.Add(DeserializePlayer(ref b, ref s, ref bR));
        }
        obj.matchID = bR.ReadInt32();
        return obj;
    }

    // PLAYERPOSITION
    public static byte[] serialize(PlayerPosition playerposition)
    {
        var s = new MemoryStream();
        var bW = new BinaryWriter(s);
        bW.Write(playerposition.posX);
        bW.Write(playerposition.posY);
        bW.Write(playerposition.posZ);
        bW.Write(playerposition.playerIndex);
        bW.Write(playerposition.matchID);
        return s.ToArray();
    }

    public static PlayerPosition DeserializePlayerPosition(byte[] b)
    {
        var s = new MemoryStream(b);
        var bR = new BinaryReader(s);
        var obj = new PlayerPosition();
        obj.posX = bR.ReadSingle();
        obj.posY = bR.ReadSingle();
        obj.posZ = bR.ReadSingle();
        obj.playerIndex = bR.ReadInt32();
        obj.matchID = bR.ReadInt32();
        return obj;
    }

    private static PlayerPosition DeserializePlayerPosition(ref byte[] b, ref MemoryStream s, ref BinaryReader bR)
    {
        var obj = new PlayerPosition();
        obj.posX = bR.ReadSingle();
        obj.posY = bR.ReadSingle();
        obj.posZ = bR.ReadSingle();
        obj.playerIndex = bR.ReadInt32();
        obj.matchID = bR.ReadInt32();
        return obj;
    }

    // STRING
    public static byte[] serialize(string stringdata)
    {
        var s = new MemoryStream();
        var bW = new BinaryWriter(s);
        bW.Write(stringdata);
        return s.ToArray();
    }

    public static string DeserializeString(byte[] b)
    {
        var s = new MemoryStream(b);
        var bR = new BinaryReader(s);
        return bR.ReadString();
    }
}