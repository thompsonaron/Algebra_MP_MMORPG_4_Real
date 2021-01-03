using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public static class Serializator
{
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
    public static byte[] serialize(Pllayyer pllayyer)
    {
        var s = new MemoryStream();
        var bW = new BinaryWriter(s);
        bW.Write(pllayyer.pos);
        bW.Write(pllayyer.ID);
        return s.ToArray();
    }
    public static NetPackett DeserializeNetPackett(byte[] b)
    {
        var s = new MemoryStream(b);
        var bR = new BinaryReader(s);
        var obj = new NetPackett();
        //obj.messageType = DeserializeMessageType(ref b, ref s, ref bR);
        obj.messageType = (MessageType)bR.ReadInt32();
        int dataArraySize = bR.ReadInt32();
        obj.data = new Byte[dataArraySize];
        for (int i = 0; i < dataArraySize; i++)
        {
            obj.data[i] = bR.ReadByte();
        }
        return obj;
    }
    public static Pllayyer DeserializePllayyer(byte[] b)
    {
        var s = new MemoryStream(b);
        var bR = new BinaryReader(s);
        var obj = new Pllayyer();
        obj.pos = bR.ReadSingle();
        obj.ID = bR.ReadString();
        return obj;
    }

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
    private static Pllayyer DeserializePllayyer(ref byte[] b, ref MemoryStream s, ref BinaryReader bR)
    {
        var obj = new Pllayyer();
        obj.pos = bR.ReadSingle();
        obj.ID = bR.ReadString();
        return obj;
    }

    public static byte[] serialize(StringData stringdata)
    {
        var s = new MemoryStream();
        var bW = new BinaryWriter(s);
        bW.Write(stringdata.data);
        return s.ToArray();
    }

    private static StringData DeserializeStringData(ref byte[] b, ref MemoryStream s, ref BinaryReader bR)
    {
        var obj = new StringData();
        obj.data = bR.ReadString();
        return obj;
    }

    private static Player DeserializePlayer(ref byte[] b, ref MemoryStream s, ref BinaryReader bR)
    {
        var obj = new Player();
        obj.lobbyId = bR.ReadString();
        obj.gameId = bR.ReadString();
        obj.elo = bR.ReadInt32();
        return obj;
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

    private static string DeserializeString(ref byte[] b, ref MemoryStream s, ref BinaryReader bR)
    {
        return bR.ReadString();
    }

}



[Serializable]
public class NetPackett
{
    public MessageType messageType;
    public byte[] data;
}

[Serializable]
public class Pllayyer
{
    public float pos;
    public string ID;
}

[Serializable]
public class StringData
{
    public string data;
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
