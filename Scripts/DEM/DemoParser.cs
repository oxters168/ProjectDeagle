using UnityEngine;
using System;
using System.IO;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
//using System.Linq;

namespace ProjectDeagle
{
    public class DemoParser : UnityThreadJob
    {
        public string demoLocation { get; internal set; }
        private FileStream stream;

        public DemoHeader demoHeader { get; internal set; }
        private List<Tick> _ticks;

        internal const int INDEX_MASK = 2047;

        internal DataTables dataTables; //Holds the ServerClasses

        internal Dictionary<ServerClass, EquipmentElement> equipmentMapping;
        internal Dictionary<int, EventDescriptor> gameEventDescriptors;

        internal Dictionary<int, Entity> entities;

        internal List<StringTable> stringTables; //Carry userinfo, instancebaselines, and modelprecache

        internal int ccsplayerCount = 0;
        internal Dictionary<int, PlayerInfo> _playerInfo;
        internal Dictionary<int, byte[]> instanceBaselines; //Default ServerClass instance values as raw byte arrays
        internal List<string> modelPrecache;

        #region Thread Safety
        private object ticksLock = new object();
        public Tick[] ticks
        {
            get
            {
                lock(ticksLock)
                {
                    return _ticks.ToArray();
                }
            }
        }

        private object playerInfoLock = new object();
        public Dictionary<int, PlayerInfo> playerInfo
        {
            get
            {
                lock(playerInfoLock)
                {
                    return new Dictionary<int, PlayerInfo>(_playerInfo);
                }
            }
        }
        #endregion

        #region Debug Stuff
        internal static List<string> uniqueStringTableEntries = new List<string>();
        internal static uint prints = 0, valuesRead = 0; //Debug
        //public static bool playerResourceReceived = false; //Debug
        //public static string example = ""; //Debug
        internal static Dictionary<ServerClass, Entity> uniqueEntities = new Dictionary<ServerClass, Entity>(); //Debug
        //public Dictionary<EventDescriptor, Dictionary<string, object>> uniqueEvents = new Dictionary<EventDescriptor, Dictionary<string, object>>(); //For Debug purposes
        #endregion

        public DemoParser(string demoLocation)
        {
            this.demoLocation = demoLocation;

            _ticks = new List<Tick>();
            equipmentMapping = new Dictionary<ServerClass, EquipmentElement>();
            entities = new Dictionary<int, Entity>();
            stringTables = new List<StringTable>();
            _playerInfo = new Dictionary<int, PlayerInfo>();
            instanceBaselines = new Dictionary<int, byte[]>();
            modelPrecache = new List<string>();
            //preprocessedBaselines = new Dictionary<int, object[]>();
        }

        protected override void ThreadFunction()
        {
            //using (stream = new FileStream(demoLocation, FileMode.Open))
            //{
                ParseHeader();
                ParseToEnd();
            //}
        }
        public void ParseHeader()
        {
            DemoHeader header = new DemoHeader();
            using (stream = new FileStream(demoLocation, FileMode.Open))
            {
                header.header = DataParser.ReadNullTerminatedString(stream);
                header.demoProtocol = DataParser.ReadInt(stream);
                header.networkProtocol = DataParser.ReadInt(stream);
                header.serverName = System.Text.Encoding.Default.GetString(DataParser.ReadBytes(stream, 260)).Replace("\0", "");
                header.clientName = System.Text.Encoding.Default.GetString(DataParser.ReadBytes(stream, 260)).Replace("\0", "");
                header.mapName = System.Text.Encoding.Default.GetString(DataParser.ReadBytes(stream, 260)).Replace("\0", "");
                header.gameDirectory = System.Text.Encoding.Default.GetString(DataParser.ReadBytes(stream, 260)).Replace("\0", "");
                header.playbackTime = DataParser.ReadFloat(stream);
                header.ticks = DataParser.ReadInt(stream);
                header.frames = DataParser.ReadInt(stream);
                header.signOnLength = DataParser.ReadInt(stream);
            }

            demoHeader = header;

            #region Debug Info
            //Debug.Log("Header: " + demoHeader.header);
            //Debug.Log("DemoProtocol: " + demoHeader.demoProtocol);
            //Debug.Log("NetworkProtocol: " + demoHeader.networkProtocol);
            //Debug.Log("ServerName: " + demoHeader.serverName);
            //Debug.Log("ClientName: " + demoHeader.clientName);
            //Debug.Log("MapName: " + demoHeader.mapName);
            //Debug.Log("GameDirectory: " + demoHeader.gameDirectory);
            //Debug.Log("PlaybackTime: " + demoHeader.playbackTime);
            //Debug.Log("Ticks: " + demoHeader.ticks);
            //Debug.Log("Frames: " + demoHeader.frames);
            //Debug.Log("SignOnLength: " + demoHeader.signOnLength);
            #endregion
        }
        private void ParseToEnd()
        {
            using (stream = new FileStream(demoLocation, FileMode.Open))
            {
                while (stream.Position < stream.Length)
                {
                    if (ProgramInterface.isQuitting) break;
                    ParseTick();
                }
            }

            #region Debug Unique StringTables
            string stringTableDebugString = "StringTables\n\n";
            foreach(string entry in uniqueStringTableEntries)
            {
                stringTableDebugString += entry + "\n";
            }
            Debug.Log(stringTableDebugString);
            #endregion
            #region Debug Unique Events
            //for(int i = 0; i < uniqueEvents.Count; i++)
            //{
            //    string debugString = uniqueEvents.Keys.ElementAt(i).name + "\n";
            //    for(int j = 0; j < uniqueEvents.Values.ElementAt(i).Count; j++)
            //    {
            //        debugString += "\n" + uniqueEvents.Values.ElementAt(i).Keys.ElementAt(j) + ": " + uniqueEvents.Values.ElementAt(i).Values.ElementAt(j);
            //    }
            //    Debug.Log(debugString);
            //}
            #endregion
            #region Debug Unique Entities
            foreach(KeyValuePair<ServerClass, Entity> entity in uniqueEntities)
            {
                string eventsDebugString = entity.Value.id + ": ";
                //foreach (ServerClass baseClass in entity.Value.serverClass.baseClasses) eventsDebugString += baseClass.name + ".";
                eventsDebugString += entity.Value.serverClass.name + "\n\n";
                foreach (KeyValuePair<string, PropertyEntry> propertyEntry in entity.Value.properties)
                    if (!(propertyEntry.Key.IndexOf("0") > -1 && propertyEntry.Key.IndexOf("0") + 2 < propertyEntry.Key.Length && char.IsDigit(propertyEntry.Key[propertyEntry.Key.IndexOf("0") + 1]) && char.IsDigit(propertyEntry.Key[propertyEntry.Key.IndexOf("0") + 2])) || propertyEntry.Key.IndexOf("000") > -1)
                        eventsDebugString += propertyEntry.Key + "(" + propertyEntry.Value.value + ")\n";
                Debug.Log(eventsDebugString);
            }
            #endregion
            #region Debug Mapped Equipment
            string mappedEquipmentDebugString = "Mapped Equipment\n\n";
            foreach(KeyValuePair<ServerClass, EquipmentElement> weapon in equipmentMapping)
            {
                mappedEquipmentDebugString += weapon.Key.name + "(" + weapon.Value + ")\n";
            }
            Debug.Log(mappedEquipmentDebugString);
            #endregion
            #region Debug PacketEntities Teams
            for(int i = 3110; i < 3120; i++)
            {
                Debug.Log("Teams(" + i + ")\n\n");
                if(i < _ticks.Count)
                {
                    foreach(KeyValuePair<int, TeamResource> team in _ticks[i]._teams)
                    {
                        string teamDebugString = "EntityID(" + team.Key + ") TeamName(" + team.Value.teamName + ")\n\n";
                        teamDebugString += "clanName(" + team.Value.clanName + ")\n";
                        teamDebugString += "flagImage(" + team.Value.flagImage + ")\n";
                        //teamDebugString += "logoImage(" + team.Value.logoImage + ")\n";
                        teamDebugString += "matchStat(" + team.Value.matchStat + ")\n";
                        teamDebugString += "teamNum(" + team.Value.teamNum + ")\n";
                        teamDebugString += "totalScore(" + team.Value.totalScore + ")\n";
                        teamDebugString += "firstHalfScore(" + team.Value.firstHalfScore + ")\n";
                        teamDebugString += "secondHalfScore(" + team.Value.secondHalfScore + ")\n";
                        teamDebugString += "clanID(" + team.Value.clanID + ")\n";
                        teamDebugString += "surrendered(" + team.Value.surrendered + ")\n";
                        teamDebugString += "player_array(";
                        for(int j = 0; j < team.Value.player_array.Length; j++)
                        {
                            teamDebugString += j + "(" + team.Value.player_array[j] + "), ";
                        }
                        teamDebugString += ")\n";
                        Debug.Log(teamDebugString);
                    }
                }
            }
            #endregion
            #region Debug PacketEntites Players
            for(int i = 3110; i < 3120; i++)
            {
                Debug.Log("Players(" + i + ")\n\n");
                if (i < _ticks.Count)
                {
                    foreach (KeyValuePair<int, PlayerResource> player in _ticks[i]._players)
                    {
                        string playerDebugString = "EntityID(" + player.Key + ")";
                        if (player.Value.playerInfo != null) playerDebugString += " Name(" + player.Value.playerInfo.name + ")";
                        playerDebugString += "\n\n";
                        playerDebugString += "Position(" + player.Value.position + ")\n";
                        playerDebugString += "Money(" + player.Value.money + ")\n";
                        playerDebugString += "Health(" + player.Value.health + ")\n";
                        playerDebugString += "Armor(" + player.Value.armor + ")\n";
                        playerDebugString += "HasHelmet(" + player.Value.hasHelmet + ")\n";
                        playerDebugString += "HasKevlar(" + player.Value.hasKevlar + ")\n";
                        playerDebugString += "HasDefuseKit(" + player.Value.hasDefuseKit + ")\n";
                        playerDebugString += "IsDead(" + player.Value.isDead + ")\n";
                        playerDebugString += "ModelIndex(" + player.Value.modelIndex + ")\n";
                        playerDebugString += "ModelPrecache(" + modelPrecache[player.Value.modelIndex] + ")\n";
                        playerDebugString += "PlayerState(" + player.Value.playerState + ")\n";
                        playerDebugString += "LastPlace(" + player.Value.lastPlaceName + ")\n";
                        playerDebugString += "isConnected(" + player.Value.isConnected + ")\n";
                        playerDebugString += "Kills(" + player.Value.kills + ")\n";
                        playerDebugString += "Deaths(" + player.Value.deaths + ")\n";
                        playerDebugString += "Assists(" + player.Value.assists + ")\n";
                        playerDebugString += "Score(" + player.Value.score + ")\n";
                        playerDebugString += "MVPs(" + player.Value.mvps + ")\n";
                        playerDebugString += "Ping(" + player.Value.ping + ")\n";
                        playerDebugString += "ControlledPlayer(" + player.Value.controlledPlayer + ")\n";
                        playerDebugString += "ControlledByPlayer(" + player.Value.controlledByPlayer + ")\n";
                        playerDebugString += "TeamNum(" + player.Value.teamNum + ")\n";
                        playerDebugString += "Team(" + player.Value.team + ")\n";
                        playerDebugString += "ClanName(" + player.Value.clanName + ")\n";
                        playerDebugString += "CompRank(" + player.Value.competitiveRanking + ")\n";
                        playerDebugString += "CompWins(" + player.Value.competitiveWins + ")\n";
                        playerDebugString += "CompColor(" + player.Value.competitiveTeammateColor + ")\n";
                        playerDebugString += "ObserverTarget(" + player.Value.observerTarget + ")\n";
                        playerDebugString += "ZoomOwner(" + player.Value.zoomOwner + ")\n";
                        playerDebugString += "ActiveWeapon(" + player.Value.activeWeapon + ")\n";
                        playerDebugString += "LastWeapon(" + player.Value.lastWeapon + ")\n";
                        playerDebugString += "Weapons(";
                        foreach (KeyValuePair<int, int> weapon in player.Value._weapons)
                            playerDebugString += weapon.Key + "(" + weapon.Value + "), ";
                        playerDebugString += ")\n";
                        playerDebugString += "Ammo(";
                        foreach (KeyValuePair<int, int> ammo in player.Value._ammo)
                            playerDebugString += ammo.Key + "(" + ammo.Value + "), ";
                        playerDebugString += ")\n";
                        Debug.Log(playerDebugString);
                    }
                }
            }
            #endregion
            #region Debug PacketEntities Weapons
            for (int i = 3110; i < 3120; i++)
            {
                Debug.Log("Weapons(" + i + ")\n\n");
                foreach (KeyValuePair<int, WeaponResource> weapon in _ticks[i]._weapons)
                {
                    string weaponDebugString = "EntityID(" + weapon.Key + ") Element(" + weapon.Value.equipmentElement + ")";
                    weaponDebugString += "\n\n";
                    weaponDebugString += "position(" + weapon.Value.position + ")\n";
                    weaponDebugString += "rotation(" + weapon.Value.rotation + ")\n";
                    weaponDebugString += "hOwner(" + weapon.Value.owner + ")\n";
                    weaponDebugString += "hPrevOwner(" + weapon.Value.previousOwner + ")\n";
                    weaponDebugString += "customName(" + weapon.Value.customName + ")\n";
                    weaponDebugString += "wear(" + weapon.Value.wear + ")\n";
                    weaponDebugString += "skin(" + weapon.Value.skin + ")\n";
                    weaponDebugString += "paintKit(" + weapon.Value.paintKit + ")\n";
                    weaponDebugString += "seed(" + weapon.Value.seed + ")\n";
                    weaponDebugString += "stattrak(" + weapon.Value.stattrak + ")\n";
                    weaponDebugString += "clip1(" + weapon.Value.clip1 + ")\n";
                    weaponDebugString += "primaryAmmoReserve(" + weapon.Value.primaryAmmoReserve + ")\n";
                    //weaponDebugString += "primaryAmmoReserve(" + GetTick(i).players[weapon.Value.owner]._ammo[weapon.Value.primaryAmmoType] + ")\n";
                    weaponDebugString += "primaryAmmoType(" + weapon.Value.primaryAmmoType + ")\n";
                    weaponDebugString += "muzzleFlashParity(" + weapon.Value.muzzleFlashParity + ")\n";
                    weaponDebugString += "viewModelIndex(" + weapon.Value.viewModelIndex + ")\n";
                    weaponDebugString += "viewModelPrecache(" + modelPrecache[weapon.Value.viewModelIndex] + ")\n";
                    weaponDebugString += "worldModelIndex(" + weapon.Value.worldModelIndex + ")\n";
                    weaponDebugString += "worldModelPrecache(" + modelPrecache[weapon.Value.worldModelIndex] + ")\n";
                    weaponDebugString += "zoomLevel(" + weapon.Value.zoomLevel + ")\n";
                    weaponDebugString += "state(" + weapon.Value.state + ")\n";
                    weaponDebugString += "burstMode(" + weapon.Value.burstMode + ")\n";
                    weaponDebugString += "silenced(" + weapon.Value.silenced + ")\n";
                    Debug.Log(weaponDebugString);
                }
            }
            #endregion
            #region Debug PlayerInfo
            string playerInfoDebugString = "Player Info\n\n";
            //playerInfoDebugString += "Received(" + playerResourceReceived + ") Example(" + example + ") TotalPlayersInResources(" + prints + ")\n\n";
            foreach(KeyValuePair<int, PlayerInfo> player in playerInfo)
            {
                playerInfoDebugString += "ID(" + player.Key + ") Name(" + player.Value.name + ")\n";
                playerInfoDebugString += "version(" + player.Value.version + ")\n";
                playerInfoDebugString += "xuid(" + player.Value.xuid + ")\n";
                playerInfoDebugString += "userID(" + player.Value.userID + ")\n";
                playerInfoDebugString += "guid(" + player.Value.guid + ")\n";
                playerInfoDebugString += "friendsID(" + player.Value.friendsID + ")\n";
                playerInfoDebugString += "friendsName(" + player.Value.friendsName + ")\n";
                playerInfoDebugString += "isFakePlayer(" + player.Value.isFakePlayer + ")\n";
                playerInfoDebugString += "isHLTV(" + player.Value.isHLTV + ")\n";

                playerInfoDebugString += "customFiles0(" + player.Value.customFiles0 + ")\n";
                playerInfoDebugString += "customFiles1(" + player.Value.customFiles1 + ")\n";
                playerInfoDebugString += "customFiles2(" + player.Value.customFiles2 + ")\n";
                playerInfoDebugString += "customFiles3(" + player.Value.customFiles3 + ")\n";

                playerInfoDebugString += "filesDownloaded(" + player.Value.filesDownloaded + ")\n\n";
            }
            Debug.Log(playerInfoDebugString);
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

            tick.byteSize = (uint)DataParser.ReadInt(stream); //sizeof
            if (tick.byteSize > 0) data = DataParser.ReadBytes(stream, tick.byteSize);

            if (data != null) tick.ParseTickData(data);

            AddTick(tick);
        }

        #region Tick Functions
        internal void AddTick(Tick tick)
        {
            lock (ticksLock)
            {
                _ticks.Add(tick);
            }
        }
        internal Tick GetTick(int index)
        {
            lock(ticksLock)
            {
                return _ticks[index];
            }
        }
        public int TicksParsed()
        {
            lock(ticksLock)
            {
                return _ticks.Count;
            }
        }
        #endregion

        #region PlayerInfo Functions
        public void AddPlayerInfo(int key, PlayerInfo player)
        {
            lock (playerInfoLock)
            {
                _playerInfo[key] = player;
            }
        }
        public bool HasPlayerInfo(int key)
        {
            lock (playerInfoLock)
            {
                return _playerInfo.ContainsKey(key);
            }
        }
        public PlayerInfo GetPlayerInfo(int key)
        {
            lock (playerInfoLock)
            {
                return _playerInfo[key];
            }
        }
        #endregion

        #region PlayerResource Functions
        public PlayerResource GetPlayerResource(int tickIndex, int playerKey)
        {
            PlayerResource player = null;
            lock (ticksLock)
            {
                Tick tick = null;
                if (tickIndex > -1 && tickIndex < _ticks.Count)
                    tick = _ticks[tickIndex];
                if (tick != null && tick._players.ContainsKey(playerKey))
                    player = tick._players[playerKey];
            }
            return player;
        }
        #endregion

        #region Network Stuff
        public string locationToParse;
        public int port;
        private Socket gotvSocket;
        private IPEndPoint gotvEndPoint;

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

    internal enum NET_Messages
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

    internal enum SVC_Messages
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
}