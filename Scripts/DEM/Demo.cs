using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using DemoInfo;
using System;
//using System.Linq;

public class Demo
{
    public static Dictionary<string, Demo> loadedDemos = new Dictionary<string, Demo>();
    public string locationToParse { get; private set; }
    public int port { get; private set; }
    private Socket gotvSocket;
    private IPEndPoint gotvEndPoint;
    //private UdpClient gotvClient;
    //private NetworkStream gotvStream;
    public Dictionary<Player, DemoEntity> players = new Dictionary<Player, DemoEntity>();
    //public Dictionary<Player, WeaponInfo> heldWeapon = new Dictionary<Player, WeaponInfo>();
    public List<GameTick> demoTicks = new List<GameTick>();
    public DemoParser demoParser;
    public DemoHeader demoHeader;
    public BSPMap demoMap;
    public Dictionary<Player, CSGOPlayer> playerObjects = new Dictionary<Player, CSGOPlayer>();
    public bool play;
    public int totalTicks = 0;
    public int seekIndex = 0;
    public float playSpeed = 0.05f;
    private float previousTime = 0f;
    private bool switchedTarget;
    public bool alreadyParsed = false;

    public Demo(string location, bool online)
    {
        if (online)
        {
            if(location.Length > 0)
            {
                if (location.IndexOf(":") > -1) try { port = System.Convert.ToInt32(location.Substring(location.IndexOf(":") + 1)); } catch(System.Exception) {}
                else port = 27024;
                locationToParse = location.Split(':')[0];
            }
        }
        else
        {
            port = -1;
            locationToParse = location;

            if (!loadedDemos.ContainsKey(location)) loadedDemos.Add(location, this);
            else alreadyParsed = true;
            //mapsLocation = mL;
        }
    }

    /*public static byte[] HexStringToByteArray(string hex)
    {
        return Enumerable.Range(0, hex.Length)
                         .Where(x => x % 2 == 0)
                         .Select(x => System.Convert.ToByte(hex.Substring(x, 2), 16))
                         .ToArray();
    }*/
    /*public static byte[] HexStringToByteArray(string hexString)
    {
        int numChars = hexString.Length;
        byte[] bytes = new byte[numChars / 2];
        for (int i = 0; i < numChars; i += 2)
        {
            bytes[i / 2] = Convert.ToByte(hexString.Substring(i, 2), 16);
        }
        return bytes;
    }*/
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
            decoded += ((short) input[i]) + " ";
        }
        return decoded;
    }

    public void ParseReplay()
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
                    //initialConnect = HexStringToByteArray("ffffffff" + StringToHexString("qconnect") + StringToHexString("0x00000000") + "00");
                    //initialConnect = System.Text.Encoding.ASCII.GetBytes("");
                    gotvSocket.Send(clientMessage);
                }
                catch (Exception e) { Debug.Log(e.Message); }

                try
                {
                    gotvReply = new byte[59];
                    gotvSocket.Receive(gotvReply);

                    header = (short) gotvReply[4];
                    challengeNum = BitConverter.ToInt32(gotvReply, 5);
                    protocolVersion = BitConverter.ToInt16(gotvReply, 42);
                    //Debug.Log("Connect Reply: " + GetStringUTF16(gotvReply));
                    Debug.Log("Header: " + header + " Challenge Number: " + challengeNum + " Protocol Version: " + protocolVersion);

                    //Debug.Log("Before BZip2: " + System.Text.Encoding.Default.GetString(gotvReply));
                    //System.IO.MemoryStream decompressed = new System.IO.MemoryStream();
                    //BZip2.Decompress(new NetworkStream(gotvSocket), decompressed, true);
                    //gotvReply = new byte[((int)decompressed.Length)];
                    //decompressed.Read(gotvReply, 0, gotvReply.Length);
                    //Debug.Log("After BZip2: " + System.Text.Encoding.Default.GetString(gotvReply));
                }
                catch (Exception e) { Debug.Log(e.Message); }

                if (challengeNum > -1)
                {
                    try
                    {
                        //clientMessage = HexStringToByteArray("ffffffff6bc734000003000000cdf23a000037423436324445443235454644424531414633323436343445423143463832463834433437453836414344344242323437464231353042453131363746353330000110aa010aa7010a0d120931363935353539383018010a0512013118030a0512013018040a0512013018050a0a1206546865204f7818060a0512013218070a0512013118080a061202363418090a0612022430180a0a05120130180b0a05120131180c0a05120133180d0a0612023634180e0a08120431323030180f0a091205383030303018100a0512013118110a0512013118120a091205302e30333118130a0512013018140a05120134181500000000000000000200000000e4011870361402002002280000005a027168f64ee8a51970361402002002fa95c3ac300000000200000004000000048648b30100000008b74e00040000006401000064000000080000001870361402002002b4050000048648b3d9005081010000005c7cbcac5cdbf3ac02001aa60100000000001e4fefdd838a1de4c1f4b33cf21e98fef558760080cc9db102a76836b8b68f78b469e2da01293a6091e290e3dd0f6bf4d2cc542e2fa687c1c2d5542b9d00d0d1d442501a51306945c05951152302ddd615362f66e2ebe6c68bfabdac0c32b416eb4e74df967f825c815f45ca9ef460c68545c4a81629a00a48c106b3dd89f98501");
                        clientMessage = GetBytesUTF16(GenerateChallengePacket(challengeNum, protocolVersion));
                        gotvSocket.Send(clientMessage);
                    }
                    catch (Exception e) { Debug.Log(e.Message); }
                }
            }
        }
        else if (!alreadyParsed)
        {
            System.IO.FileStream replayFile = new System.IO.FileStream(locationToParse, System.IO.FileMode.Open);
            demoParser = new DemoInfo.DemoParser(replayFile);
            demoParser.TickDone += demoParser_TickDone;
            demoParser.HeaderParsed += demoParser_HeaderParsed;
            demoParser.ParseHeader();
            demoParser.ParseToEnd();
            replayFile.Close();

            if (BSPMap.loadedMaps.ContainsKey(demoParser.Map)) demoMap = BSPMap.loadedMaps[demoParser.Map];
            else { demoMap = new BSPMap(demoParser.Map); demoMap.MakeMap(); }

            demoParser.Dispose();
        }
    }

    void demoParser_HeaderParsed(object sender, HeaderParsedEventArgs e)
    {
        demoHeader = demoParser.Header;
    }

    void demoParser_TickDone(object sender, DemoInfo.TickDoneEventArgs e)
    {
        totalTicks++;
        CreateTick();
        //Debug.Log("Tick Passed");
    }
    /*void demoParser_RoundEnd(object sender, DemoInfo.RoundEndedEventArgs e)
    {
        //Debug.Log("Round Ended");
    }
    void demoParser_RoundStart(object sender, DemoInfo.RoundStartedEventArgs e)
    {
        //Debug.Log("Round Started");
    }
    void demoParser_LastRoundHalf(object sender, DemoInfo.LastRoundHalfEventArgs e)
    {
        //Debug.Log("Half Time");
    }
    void demoParser_MatchStarted(object sender, DemoInfo.MatchStartedEventArgs e)
    {
        //Debug.Log("Match Started");
    }
    void demoParser_PlayerKilled(object sender, DemoInfo.PlayerKilledEventArgs e)
    {
        //DemoParser parsedParser = ((DemoParser)sender);
        string victim = "Unknown", killer = "Unknown";
        try { victim = e.Victim.Name; }
        catch (System.Exception) { }
        try { killer = e.Killer.Name; }
        catch (System.Exception) { }
        //Debug.Log("Player Killed: " + victim + " By: " + killer + " Using: " + e.Weapon.Weapon);
    }
    void demoParser_PlayerHurt(object sender, DemoInfo.PlayerHurtEventArgs e)
    {
        //DemoParser parsedParser = ((DemoParser)sender);
        //Debug.Log("Player Hurt: " + e.Player.Name + " By: " + e.Attacker.Name + " Using: " + e.Weapon.Weapon + " Current Health: " + e.Health);
    }
    void demoParser_PlayerBind(object sender, DemoInfo.PlayerBindEventArgs e)
    {
        DemoParser parsedParser = ((DemoParser)sender);
        //Debug.Log("Player Connected: " + e.Player.Name + " Num Players: " + parsedParser.Players.Count);
    }
    void demoParser_HeaderParsed(object sender, DemoInfo.HeaderParsedEventArgs e)
    {
        DemoInfo.DemoParser parsedParser = ((DemoInfo.DemoParser)sender);
        //Debug.Log("Map: " + parsedParser.Map);
    }*/

    private void CreateTick()
    {
        GameTick tick = new GameTick();
        foreach (KeyValuePair<int, Player> entry in demoParser.Players)
        {
            WeaponInfo weaponInfo = null;
            if (entry.Value.ActiveWeapon != null) weaponInfo = new WeaponInfo(entry.Value.ActiveWeaponID, entry.Value.ActiveWeapon);

            EntityInfo entityInfo = new EntityInfo(entry.Value.Name, entry.Value.AdditionaInformations.Clantag, weaponInfo, entry.Value.EntityID, entry.Value.SteamID, entry.Value.Position, entry.Value.ViewDirectionX, entry.Value.ViewDirectionY, entry.Value.Velocity, entry.Value.HP, entry.Value.AdditionaInformations.Kills, entry.Value.TeamID, entry.Value.IsAlive, entry.Value.IsDucking);
            tick.AddPlayer(entry.Value);
            if (!players.ContainsKey(entry.Value))
            {
                players.Add(entry.Value, new DemoEntity(entry.Value, entityInfo));
            }
            else
            {
                players[entry.Value].AddTickInfo(entityInfo);
            }
        }
        tick.ctID = demoParser.ctID;
        tick.tID = demoParser.tID;
        demoTicks.Add(tick);
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

    public void Stream()
    {
        if (port > -1)
        {
            if (gotvSocket != null)
            {
                if (gotvSocket.Available > 0)
                {
                    //Debug.Log("Availability: " + gotvSocket.Available);
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
                    catch (System.Exception e) { Debug.Log(e.Message); }
                }
            }
        }

        if (play)
        {
            if (port > -1)
            {
                //set tick to last received
            }

            if (demoTicks != null && demoTicks.Count > 0)
            {
                if (playerObjects.Count < players.Count)
                {
                    List<Player> missingKeys = new List<Player>();
                    foreach (KeyValuePair<Player, DemoEntity> entry in players)
                    {
                        if (!playerObjects.ContainsKey(entry.Key)) missingKeys.Add(entry.Key);
                    }

                    foreach (Player key in missingKeys)
                    {
                        GameObject entryObject = new GameObject();
                        CSGOPlayer entryPlayer = entryObject.AddComponent<CSGOPlayer>();
                        entryPlayer.replay = this;
                        entryPlayer.playerInfo = players[key];
                        playerObjects.Add(key, entryPlayer);
                    }
                }

                if (seekIndex < 0 || seekIndex >= demoTicks.Count) seekIndex = 0;
                if (port > 0) { demoParser.ParseNextTick(); seekIndex = demoTicks.Count - 1; }
                if (Time.time - previousTime >= playSpeed)
                {
                    seekIndex += (int)((Time.time - previousTime) / playSpeed);
                    previousTime = Time.time;
                }
            }
        }
        else previousTime = Time.time;

        /*if ((Input.GetMouseButton(0) || Input.GetMouseButton(1)) && !switchedTarget)
        {
            CameraControl theCam = Camera.main.GetComponent<CameraControl>();

            if (playerObjects != null && playerObjects.Count > 0)
            {
                int currentIndex = -1;
                if (theCam.target != null)
                {
                    for (int i = 0; i < playerObjects.Count; i++)
                    {
                        if (playerObjects[i].entityID == theCam.target.gameObject.GetComponent<CSGOPlayer>().entityID) { currentIndex = i; break; }
                    }
                    //playerObjects.IndexOf(theCam.target.gameObject.GetComponent<CSGOPlayer>());
                }
                //Debug.Log("Previous Index: " + currentIndex);
                if (Input.GetMouseButton(0)) currentIndex++;
                else if (Input.GetMouseButton(1)) currentIndex--;
                if (currentIndex > playerObjects.Count - 1) currentIndex = 0;
                if (currentIndex < 0) currentIndex = playerObjects.Count - 1;

                theCam.target = playerObjects[currentIndex].transform;
                switchedTarget = true;
            }
            else
            {
                theCam.target = null;
            }
        }
        else if (!Input.GetMouseButton(0) && !Input.GetMouseButton(1)) switchedTarget = false;*/
    }

    public void Stop()
    {
        play = false;
        seekIndex = 0;

        List<Player> keysToRemove = new List<Player>();
        foreach (KeyValuePair<Player, CSGOPlayer> entity in playerObjects)
        {
            GameObject toBeRemoved = playerObjects[entity.Key].gameObject;
            keysToRemove.Add(entity.Key);
            //playerObjects.Remove(entity.Key);
            GameObject.DestroyImmediate(toBeRemoved);
        }
        foreach(Player key in keysToRemove) playerObjects.Remove(key);

        if (port > -1)
        {
            CloseConnection();
        }
    }
    public void SelfDestruct()
    {
        demoParser.Dispose();
        
        if(port < 0) loadedDemos.Remove(locationToParse);
        demoMap.SetVisibility(false);
        Stop();
    }
}

struct MapMaterial
{
    string materialTexturePath;
    Material material;
}
