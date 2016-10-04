using UnityEngine;
using System;
using System.IO;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Linq;

public class DemoParser
{
    public DemoHeader demoHeader;
    Stream stream;

    public static uint prints = 0;

    public string locationToParse;
    public int port;
    private Socket gotvSocket;
    private IPEndPoint gotvEndPoint;

    const int MAX_EDICT_BITS = 11;
    const int INDEX_MASK = ((1 << MAX_EDICT_BITS) - 1);
    const int MAX_ENTITIES = (1 << MAX_EDICT_BITS);
    const int MAX_PLAYERS = 64;
    const int MAX_WEAPONS = 64;

    public DataTables dataTables; //Holds the ServerClasses

    public Dictionary<ServerClass, EquipmentElement> equipmentMapping = new Dictionary<ServerClass, EquipmentElement>();
    public Dictionary<int, EventDescriptor> gameEventDescriptors;

    public Dictionary<EventDescriptor, Dictionary<string, object>> uniqueEvents = new Dictionary<EventDescriptor, Dictionary<string, object>>(); //Debug

    public Entity[] entities = new Entity[MAX_ENTITIES];

    public List<StringTable> stringTables = new List<StringTable>();

    public PlayerInfo[] playerInfo = new PlayerInfo[MAX_PLAYERS];
    public Dictionary<int, byte[]> instanceBaselines = new Dictionary<int, byte[]>();
    public List<string> modelPrecache = new List<string>();

    public Dictionary<int, object[]> preprocessedBaselines = new Dictionary<int, object[]>();

    public DemoParser(Stream stream)
    {
        this.stream = stream;
    }

    public void ParseHeader()
    {
        demoHeader.header = DataParser.ReadNullTerminatedString(stream);
        demoHeader.demoProtocol = DataParser.ReadInt(stream);
        demoHeader.networkProtocol = DataParser.ReadInt(stream);
        demoHeader.serverName = System.Text.Encoding.Default.GetString(DataParser.ReadBytes(stream, 260));
        demoHeader.clientName = System.Text.Encoding.Default.GetString(DataParser.ReadBytes(stream, 260));
        demoHeader.mapName = System.Text.Encoding.Default.GetString(DataParser.ReadBytes(stream, 260));
        demoHeader.gameDirectory = System.Text.Encoding.Default.GetString(DataParser.ReadBytes(stream, 260));
        demoHeader.playbackTime = DataParser.ReadFloat(stream);
        demoHeader.ticks = DataParser.ReadInt(stream);
        demoHeader.frames = DataParser.ReadInt(stream);
        demoHeader.signOnLength = DataParser.ReadInt(stream);

        #region Debug Info
        Debug.Log("Header: " + demoHeader.header);
        Debug.Log("DemoProtocol: " + demoHeader.demoProtocol);
        Debug.Log("NetworkProtocol: " + demoHeader.networkProtocol);
        Debug.Log("ServerName: " + demoHeader.serverName);
        Debug.Log("ClientName: " + demoHeader.clientName);
        Debug.Log("MapName: " + demoHeader.mapName);
        Debug.Log("GameDirectory: " + demoHeader.gameDirectory);
        Debug.Log("PlaybackTime: " + demoHeader.playbackTime);
        Debug.Log("Ticks: " + demoHeader.ticks);
        Debug.Log("Frames: " + demoHeader.frames);
        Debug.Log("SignOnLength: " + demoHeader.signOnLength);
        #endregion
    }
    public void ParseToEnd()
    {
        while(stream.Position < stream.Length)
        {
            ParseTick();
        }

        #region Debug Unique Events
        for(int i = 0; i < uniqueEvents.Count; i++)
        {
            string debugString = uniqueEvents.Keys.ElementAt(i).name + "\n";
            for(int j = 0; j < uniqueEvents.Values.ElementAt(i).Count; j++)
            {
                debugString += "\n" + uniqueEvents.Values.ElementAt(i).Keys.ElementAt(j) + ": " + uniqueEvents.Values.ElementAt(i).Values.ElementAt(j);
            }
            Debug.Log(debugString);
        }
        #endregion
    }
    private void ParseTick()
    {
        Tick tick = new Tick(this);
        tick.command = (DemoCommand)DataParser.ReadByte(stream);
        tick.tickNumber = DataParser.ReadInt(stream);
        tick.playerSlot = DataParser.ReadByte(stream);
        byte[] data = null;
        //Debug.Log("Number: " + tick.tickNumber + "\nCommand: " + tick.command);

        if (tick.command == DemoCommand.Synctick)
        {
            return;
        }
        else if (tick.command == DemoCommand.Stop)
        {
            return;
        }
        else if (tick.command == DemoCommand.UserCommand)
        {
            DataParser.ReadInt(stream);
        }
        else if (tick.command == DemoCommand.Signon || tick.command == DemoCommand.Packet)
        {
            DataParser.ReadInt(stream);
            new Vector3(DataParser.ReadFloat(stream), DataParser.ReadFloat(stream), DataParser.ReadFloat(stream));
            new Vector3(DataParser.ReadFloat(stream), DataParser.ReadFloat(stream), DataParser.ReadFloat(stream));
            new Vector3(DataParser.ReadFloat(stream), DataParser.ReadFloat(stream), DataParser.ReadFloat(stream));
            new Vector3(DataParser.ReadFloat(stream), DataParser.ReadFloat(stream), DataParser.ReadFloat(stream));
            new Vector3(DataParser.ReadFloat(stream), DataParser.ReadFloat(stream), DataParser.ReadFloat(stream));
            new Vector3(DataParser.ReadFloat(stream), DataParser.ReadFloat(stream), DataParser.ReadFloat(stream));

            DataParser.ReadInt(stream);
            new Vector3(DataParser.ReadFloat(stream), DataParser.ReadFloat(stream), DataParser.ReadFloat(stream));
            new Vector3(DataParser.ReadFloat(stream), DataParser.ReadFloat(stream), DataParser.ReadFloat(stream));
            new Vector3(DataParser.ReadFloat(stream), DataParser.ReadFloat(stream), DataParser.ReadFloat(stream));
            new Vector3(DataParser.ReadFloat(stream), DataParser.ReadFloat(stream), DataParser.ReadFloat(stream));
            new Vector3(DataParser.ReadFloat(stream), DataParser.ReadFloat(stream), DataParser.ReadFloat(stream));
            new Vector3(DataParser.ReadFloat(stream), DataParser.ReadFloat(stream), DataParser.ReadFloat(stream));

            DataParser.ReadInt(stream);
            DataParser.ReadInt(stream);
        }

        tick.size = (uint)DataParser.ReadInt(stream); //sizeof
        if (tick.size > 0) data = DataParser.ReadBytes(stream, tick.size);

        if (data != null) tick.ParseTickData(data);
    }

    #region Network Stuff
    public void Connect()
    {
        if (port > -1)
        {
            if (gotvSocket == null)
            {
                gotvSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                gotvSocket.ReceiveTimeout = 500;
                gotvSocket.SendTimeout = 500;
                gotvEndPoint = new IPEndPoint(IPAddress.Parse(locationToParse), port);
                gotvSocket.Connect(gotvEndPoint);

                byte[] clientMessage;
                byte[] gotvReply;

                short header = -1;
                short protocolVersion = -1;
                int challengeNum = -1;

                try
                {
                    clientMessage = GetBytesUTF16("\xff\xff\xff\xff" + "qconnect" + "0x00000000" + "\x00");
                    gotvSocket.Send(clientMessage);
                }
                catch (Exception e) { Debug.Log(e.Message); }

                try
                {
                    gotvReply = new byte[59];
                    gotvSocket.Receive(gotvReply);

                    header = gotvReply[4];
                    challengeNum = BitConverter.ToInt32(gotvReply, 5);
                    protocolVersion = BitConverter.ToInt16(gotvReply, 42);
                    //Debug.Log("Connect Reply: " + GetStringUTF16(gotvReply));
                    Debug.Log("Header: " + header + " Challenge Number: " + challengeNum + " Protocol Version: " + protocolVersion);
                }
                catch (Exception e) { Debug.Log(e.Message); }

                if (challengeNum > -1)
                {
                    try
                    {
                        clientMessage = GetBytesUTF16(GenerateChallengePacket(challengeNum, protocolVersion));
                        gotvSocket.Send(clientMessage);
                    }
                    catch (Exception e) { Debug.Log(e.Message); }
                }
            }
        }
    }
    private void ReadNetworkPacket()
    {
        if (port > -1)
        {
            if (gotvSocket != null)
            {
                if (gotvSocket.Available > 0)
                {
                    try
                    {
                        byte[] received = new byte[2048];
                        int receivedBytes = gotvSocket.Receive(received);
                        byte[] packetData = new byte[receivedBytes];
                        Array.ConstrainedCopy(received, 0, packetData, 0, receivedBytes);
                        Debug.Log("Packet Length: " + receivedBytes);
                        Debug.Log("Plain Text: " + GetStringUTF16(packetData));
                        Debug.Log("Numbers: " + ConvertToBunchNumbers(packetData));

                        ReadNetMessage(packetData);

                        //packetData = ProtoBuf.ProtoReader.DirectReadBytes(new System.IO.MemoryStream(packetData), packetData.Length);
                        //System.IO.MemoryStream decompressed = new System.IO.MemoryStream();
                        //BZip2.Decompress(new System.IO.MemoryStream(packetData), decompressed, true);
                        //packetData = new byte[((int)decompressed.Length)];
                        //decompressed.Read(packetData, 0, packetData.Length);
                        //Debug.Log("After ProtoBuf: " + GetStringUTF16(packetData));
                    }
                    catch (Exception e) { Debug.Log(e.Message); }
                }
            }
        }
    }
    private void CloseConnection()
    {
        try
        {
            //if (gotvStream != null) gotvStream.Close();
            //if (gotvClient != null) gotvClient.Close();
            if (gotvSocket != null) gotvSocket.Close();
            //gotvStream = null;
            //gotvClient = null;
            gotvSocket = null;
        }
        catch (Exception e) { Debug.Log(e.Message); }
    }

    private static string GenerateChallengePacket(int challengeNum, short protocolVersion)
    {
        string packetData = "";

        string userID = "169555980";
        string username = "The Ox";

        //First packet constant
        string pc1 = "\xff\xff\xff\xff" + "k" + GetStringUTF16(BitConverter.GetBytes(protocolVersion)) + "\x00\x00\x03\x00\x00\x00";
        //First match variable
        //string pmv1 = "\xab\x92\xd8\x08\x00" + "125F1946E3661C0AECAAFE564979F08277889B6254FEFBD63D0DEA5309EBB5D1";
        string pmv1 = GetStringUTF16(BitConverter.GetBytes(challengeNum)) + "\x00" + "125F1946E3661C0AECAAFE564979F08277889B6254FEFBD63D0DEA5309EBB5D1";
        //Second packet constant
        string pc2 = "\x00\x01\x10\xaa\x01\x0a\xa7\x01\x0a\x0d\x12\x09" + userID + "\x18\x01\x0a\x05\x12\x01" + "1" + "\x18\x03\x0a\x05\x12\x01" + "0" + "\x18\x04\x0a\x05\x12\x01" + "0" + "\x18\x05\x0a\x0a\x12\x06" + username + "\x18\x06\x0a\x05\x12\x01" + "2" + "\x18\x07\x0a\x05\x12\x01" + "1" + "\x18\x08\x0a\x06\x12\x02" + "64" + "\x18\x09\x0a\x06\x12\x02" + "$0" + "\x18\x0a\x0a\x05\x12\x01" + "0" + "\x18\x0b\x0a\x05\x12\x01" + "1" + "\x18\x0c\x0a\x05\x12\x01" + "3" + "\x18\x0d\x0a\x06\x12\x02" + "64" + "\x18\x0e\x0a\x08\x12\x04" + "1200" + "\x18\x0f\x0a\x09\x12\x05" + "80000" + "\x18\x10\x0a\x05\x12\x01" + "1" + "\x18\x11\x0a\x05\x12\x01" + "1" + "\x18\x12\x0a\x09\x12\x05" + "0.031" + "\x18\x13\x0a\x05\x12\x01" + "0" + "\x18\x14\x0a\x05\x12\x01" + "4" + "\x18\x15\x00\x00\x00\x00\x00\x00\x00\x00\x02\x00\x00\x00\x00\xe4\x01\x18" + "p6" + "\x14\x02\x00" + " " + "\x02" + "(" + "\x00\x00\x00";
        //First packet variable
        string pv1 = "\x8a" + "3" + "\xec\x97" + "F" + "\xb3\x1d\xaa\x18";
        //string pv1 = "\x0c\x65\x4c\x98\x54\xb6\x6d\xed\x18";
        //Third packet constant
        string pc3 = "p6" + "\x14\x02\x00" + " " + "\x02";
        //Second packet variable
        string pv2 = "\xc2\xab\xc1";
        //string pv2 = "\x04\x81\xc9";
        //Fourth packet constant
        string pc4 = "\xac" + "0" + "\x00\x00\x00\x02\x00\x00\x00\x04\x00\x00\x00\x04\x86" + "H" + "\xb3\x01\x00\x00\x00";
        //Third packet variable
        string pv3 = "\xfa\x13" + "c" + "\x04\x1a";
        //string pv3 = "\xb4\x19\xab\x01\x04";
        //Fifth packet constant
        string pc5 = "\x00\x00\x00" + "d" + "\x01\x00\x00" + "d" + "\x00\x00\x00\x08\x00\x00\x00\x18" + "p6" + "\x14\x02\x00" + " " + "\x02\xb4\x05\x00\x00\x04\x86" + "H" + "\xb3\xd9\x00" + "P" + "\x81\x01\x00\x00\x00" + "\\|" + "\xbc\xac" + "\\" + "\xdb\xf3\xac\x02\x00\x1a\xa6\x01\x00\x00\x00\x00\x00\x1e" + "O" + "\xef\xdd\x83\x8a\x1d\xe4\xc1\xf4\xb3" + "<" + "\xf2\x1e\x98\xfe\xf5" + "Xv" + "\x00\x80\xcc\x9d\xb1\x02\xa7" + "h6" + "\xb8\xb6\x8f" + "x" + "\xb4" + "i" + "\xe2\xda\x01" + "):`" + "\x91\xe2\x90\xe3\xdd\x0f" + "k" + "\xf4\xd2\xcc" + "T./" + "\xa6\x87\xc1\xc2\xd5" + "T+" + "\x9d\x00\xd0\xd1\xd4" + "BP" + "\x1a" + "Q0iE" + "\xc0" + "YQ" + "\x15" + "#" + "\x02\xdd\xd6\x15" + "6/f" + "\xe2\xeb\xe6\xc6\x8b\xfa\xbd\xac\x0c" + "2" + "\xb4\x16\xeb" + "Nt" + "\xdf\x96\x7f\x82" + "\\" + "\x81" + "_E" + "\xca\x9e\xf4" + "`" + "\xc6\x85" + "E" + "\xc4\xa8\x16" + ")" + "\xa0\x0a" + "H" + "\xc1\x06\xb3\xdd\x89\xf9\x85\x01";

        packetData = pc1 + pmv1 + pc2 + pv1 + pc3 + pv2 + pc4 + pv3 + pc5;
        return packetData;
    }
    public static byte[] GetBytesUTF16(string input)
    {
        List<byte> encoded = new List<byte>();
        byte[] originalEnc = System.Text.Encoding.Unicode.GetBytes(input);
        for (int i = 0; i < originalEnc.Length; i += 2)
        {
            encoded.Add(originalEnc[i]);
        }
        return encoded.ToArray();
    }
    public static string GetStringUTF16(byte[] input)
    {
        string decoded = "";
        for (int i = 0; i < input.Length; i += 1)
        {
            decoded += System.Convert.ToChar(input[i]);
        }
        return decoded;
    }
    public static string ConvertToBunchNumbers(byte[] input)
    {
        string decoded = "";
        for (int i = 0; i < input.Length; i += 1)
        {
            decoded += ((short)input[i]) + " ";
        }
        return decoded;
    }
    public static void ReadNetMessage(byte[] packetData)
    {
        int messageType = BitConverter.ToInt32(packetData, 0);
        if (messageType > 0 && messageType < 8)
        {
            Debug.Log(((NET_Messages)messageType));
        }
        else if (messageType > 7)
        {
            Debug.Log(((SVC_Messages)messageType));
        }
        //CNETMsg_Tick tick;
        //tick = CNETMsg_Tick.Parser.ParseFrom(packetData);
        //Debug.Log(tick.ToString());
    }
    #endregion
}

public enum NET_Messages
{
    net_NOP = 0,
    net_Disconnect = 1,
    net_File = 2,
    net_SplitScreenUser = 3,
    net_Tick = 4,
    net_StringCmd = 5,
    net_SetConVar = 6,
    net_SignonState = 7
}

public enum SVC_Messages
{
    svc_ServerInfo = 8,
    svc_SendTable = 9,
    svc_ClassInfo = 10,
    svc_SetPause = 11,
    svc_CreateStringTable = 12,
    svc_UpdateStringTable = 13,
    svc_VoiceInit = 14,
    svc_VoiceData = 15,
    svc_Print = 16,
    svc_Sounds = 17,
    svc_SetView = 18,
    svc_FixAngle = 19,
    svc_CrosshairAngle = 20,
    svc_BSPDecal = 21,
    svc_SplitScreen = 22,
    svc_UserMessage = 23,
    svc_EntityMessage = 24,
    svc_GameEvent = 25,
    svc_PacketEntities = 26,
    svc_TempEntities = 27,
    svc_Prefetch = 28,
    svc_Menu = 29,
    svc_GameEventList = 30,
    svc_GetCvarValue = 31,
    svc_PaintmapData = 33,
    svc_CmdKeyValues = 34,
    svc_EncryptedData = 35
}