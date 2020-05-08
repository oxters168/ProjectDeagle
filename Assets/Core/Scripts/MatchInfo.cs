using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Numerics;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using SteamKit2.GC.CSGO.Internal;
using UnityEngine;
using UnityHelpers;

public class MatchInfo
{
    private const string DICTIONARY = "ABCDEFGHJKLMNOPQRSTUVWXYZabcdefhijkmnopqrstuvwxyz23456789";
    private const string SHARECODE_PATTERN = "CSGO(-[\\w]{5}){5}"; //CSGO(-[\w]{5}){5}
    private static Regex shareCodeReg = new Regex(SHARECODE_PATTERN);

    private static List<MatchInfo> cachedMatches = new List<MatchInfo>();

    public CDataGCCStrike15_v2_MatchInfo matchInfoData;
    public ExtraMatchStats extraMatchStats;
    public string fileName { get; private set; }
    public float downloadProgress { get; private set; }
    public float infoProgress { get; private set; }
    public bool availableOffline { get { return File.Exists(GetMatchFilePath()); } }
    private string mapName;

    private ChainedTask matchChainedTask;
    private TaskWrapper downloadTask, generateInfoTask;
    public bool IsDownloading { get { return TaskMaker.IsMainTask(downloadTask); } }
    public bool IsGeneratingInfo { get { return TaskMaker.IsMainTask(generateInfoTask); } }
    public bool IsLoading { get { return TaskMaker.HasChainedTask(matchChainedTask); } }

    private bool setToTwoDee;

    private TaskWrapper canDownloadCheckTask;
    public float lastCanDownloadCheckTime = float.MinValue;
    public bool lastCanDownload { get; private set; }

    private MatchInfo(string _fileName)
    {
        SetFileName(_fileName);
    }
    private MatchInfo(CDataGCCStrike15_v2_MatchInfo _matchInfoData)
    {
        matchInfoData = _matchInfoData;
        fileName = GenerateName(_matchInfoData);
    }

    public static bool CheckShareCode(string sharecode)
    {
        return shareCodeReg.IsMatch(sharecode);
    }
    public static string EncodeShareCode(MatchSignature signature)
    {
        var bytes = new byte[19];
        Array.Copy(BitConverter.GetBytes(signature.matchId), 0, bytes, 1, 8);
        Array.Copy(BitConverter.GetBytes(signature.outcomeId), 0, bytes, 9, 8);
        Array.Copy(BitConverter.GetBytes((ushort)(signature.token & ((1 << 16) - 1))), 0, bytes, 17, 2);

        string output = "";
        var aVeryLargeNumber = new BigInteger(bytes.Reverse().ToArray());
        for (int i = 0; i < 25; i++)
        {
            int currentIndex = (int)(aVeryLargeNumber % DICTIONARY.Length);
            output += DICTIONARY[currentIndex];
            aVeryLargeNumber /= DICTIONARY.Length;
        }

        output = "CSGO-" + output.Substring(0, 5) + "-" + output.Substring(5, 5) + "-" + output.Substring(10, 5) + "-" + output.Substring(15, 5) + "-" + output.Substring(20, 5);
        return output;
    }
    public static MatchSignature DecodeShareCode(string sharecode)
    {
        MatchSignature decodedSignature = new MatchSignature();

        if (CheckShareCode(sharecode))
        {
            sharecode = sharecode.Replace("CSGO-", "");
            sharecode = sharecode.Replace("-", "");
            var aVeryLargeNumber = BigInteger.Zero;
            for (int i = sharecode.Length - 1; i >= 0; i--)
                aVeryLargeNumber = aVeryLargeNumber * DICTIONARY.Length + DICTIONARY.IndexOf(sharecode[i]);

            var bytes = aVeryLargeNumber.ToByteArray();
            // sometimes the number isn't unsigned, add a 00 byte at the end (the array is reversed later) of the array to make sure it is
            if (bytes.Length == 18)
                bytes = bytes.Concat(new byte[] { 0 }).ToArray();
            bytes = bytes.Reverse().ToArray();

            byte[] longContainer = new byte[8];
            Array.Copy(bytes, 1, longContainer, 0, 8);
            decodedSignature.matchId = BitConverter.ToUInt64(longContainer, 0);
            Array.Copy(bytes, 9, longContainer, 0, 8);
            decodedSignature.outcomeId = BitConverter.ToUInt64(longContainer, 0);
            Array.Copy(bytes, 17, longContainer, 0, 2);
            longContainer[2] = 0;
            longContainer[3] = 0;
            decodedSignature.token = BitConverter.ToUInt32(longContainer, 0);
        }
        else
            Debug.LogError("MatchInfo: Error decoding sharecode, unknown format");

        return decodedSignature;
    }

    public void StartCanDownloadCheck(Action<bool> onCheckCompleted)
    {
        if (!TaskManagerController.HasTask(canDownloadCheckTask))
        {
            lastCanDownloadCheckTime = Time.time;

            canDownloadCheckTask = TaskManagerController.RunActionAsync(async (ct) =>
            {
                bool canDownload = await CanDownload();
                onCheckCompleted?.Invoke(canDownload);
            });
        }
        else
            Debug.LogWarning("MatchInfo: A download check is already in progress");
    }
    private async Task<bool> CanDownload()
    {
        bool canDownload = true;
        string downloadUrl = GetDownloadUrl();

        if (!string.IsNullOrEmpty(downloadUrl))
        {
            WebRequest request = WebRequest.Create(new Uri(downloadUrl));
            request.Method = "HEAD";

            try
            {
                using (WebResponse response = await request.GetResponseAsync())
                {
                    canDownload = response.ContentLength > 0;
                    Debug.Log("MatchInfo: " + response.ContentLength + " " + response.ContentType);
                }
            }
            catch (Exception e)
            {
                canDownload = false;
                Debug.LogError("MatchInfo: " + e.ToString());
            }
        }
        else
        {
            canDownload = false;
            Debug.Log("MatchInfo: Could not get download url");
        }

        lastCanDownload = canDownload;
        return canDownload;
    }
    public string GetSharecodeUrl()
    {
        string startCode = "steam://rungame/730/76561202255233023/+csgo_download_match%20";
        return startCode + GetSharecode();
    }
    public string GetSharecode()
    {
        var lastRoundStats = GetLastRoundStats();
        ulong matchId = 0;
        ulong reservationId = 0;
        uint tvPort = 0;
        if (matchInfoData != null && lastRoundStats != null)
        {
            matchId = matchInfoData.matchid;
            reservationId = lastRoundStats.reservationid;
            tvPort = matchInfoData.watchablematchinfo.tv_port;
        }
        return EncodeShareCode(new MatchSignature(matchId, reservationId, tvPort));
    }
    public static string ExtractSharecode(string url)
    {
        return shareCodeReg.Match(url).Value;
    }

    public static MatchInfo[] GetCachedMatches()
    {
        return cachedMatches.ToArray();
    }
    public static MatchInfo FindOrCreateMatch(string fileName)
    {
        MatchInfo requestedMatch = cachedMatches.FirstOrDefault(match => match.fileName.Equals(fileName, StringComparison.OrdinalIgnoreCase));
        if (requestedMatch == null)
        {
            requestedMatch = new MatchInfo(fileName);
            cachedMatches.Add(requestedMatch);
        }

        return requestedMatch;
    }
    public static MatchInfo FindOrCreateMatch(CDataGCCStrike15_v2_MatchInfo matchDeets)
    {
        string currentExpectedName = GenerateName(matchDeets);
        MatchInfo requestedMatch = cachedMatches.FirstOrDefault(match => (match.matchInfoData == null && match.fileName.Equals(currentExpectedName, StringComparison.OrdinalIgnoreCase)) || (match.matchInfoData != null && currentExpectedName.Equals(GenerateName(match.matchInfoData), StringComparison.OrdinalIgnoreCase)));
        if (requestedMatch == null)
        {
            requestedMatch = new MatchInfo(matchDeets);
            cachedMatches.Add(requestedMatch);
        }

        return requestedMatch;
    }

    public string GetMatchFilePath()
    {
        return Path.Combine(SettingsController.matchesLocation, fileName) + ".dem";
    }
    public string GetInfoFilePath()
    {
        return Path.Combine(SettingsController.matchesLocation, fileName) + ".dem.info";
    }
    public string GetExtraStatsFilePath()
    {
        return Path.Combine(SettingsController.matchesLocation, fileName) + ".dem.extrainfo";
    }
    private void SetFileName(string _fileName)
    {
        fileName = _fileName;

        string infoFilePath = GetInfoFilePath();
        if (File.Exists(infoFilePath))
        {
            matchInfoData = ReadInfoFile(infoFilePath);
        }

        string extraStatsFilePath = GetExtraStatsFilePath();
        if (File.Exists(extraStatsFilePath))
        {
            extraMatchStats = ReadExtraStatsFile(extraStatsFilePath);
        }
    }
    public static string GenerateName(CDataGCCStrike15_v2_MatchInfo matchDeets)
    {
        string generatedName = null;
        if (matchDeets != null)
            generatedName = "match730_00" + matchDeets.roundstatsall[matchDeets.roundstatsall.Count - 1].reservationid + "_" + matchDeets.watchablematchinfo.tv_port + "_" + matchDeets.watchablematchinfo.server_ip;
        return generatedName;
    }

    public MapData GetMap()
    {
        string mapName = GetMapName();
        Debug.Assert(!string.IsNullOrEmpty(mapName), "MatchInfo: Map name is null or empty");
        MapData map = null;
        if (!string.IsNullOrEmpty(mapName))
        {
            map = MapData.FindOrCreateMap(mapName);
        }
        return map;
    }
    public bool IsMapAvailable()
    {
        MapData map = GetMap();
        Debug.Assert(map != null, "MatchInfo: Map is null");
        bool available = false;
        if (map != null)
        {
            available = map.IsMapAvailable();
        }
        return available;
    }
    public bool IsMapLoaded()
    {
        MapData map = GetMap();
        Debug.Assert(map != null, "MatchInfo: Map is null");
        bool loaded = false;
        if (map != null)
        {
            loaded = map.IsBuilt;
        }
        return loaded;
    }

    public string GetStatus()
    {
        string status;
        if (matchChainedTask != null && matchChainedTask.cancelled)
            status = "Cancelling";
        else if (IsDownloading)
            status = "Downloading Match";
        else if (IsGeneratingInfo)
            status = "Generating Match Info";
        else
        {
            var map = GetMap();
            status = setToTwoDee ? map.GetStatus2D() : map.GetStatus3D();
        }

        return status;
    }
    public void CancelChainedTask()
    {
        matchChainedTask.Cancel();
    }
    public void LoadMatch(bool twoDee)
    {
        setToTwoDee = twoDee;
        if (!availableOffline)
            matchChainedTask = TaskMaker.DownloadMatch(this, twoDee, !twoDee, true, true);
        else
            matchChainedTask = TaskMaker.GenerateMatchInfo(this, twoDee, !twoDee, true, true);
    }
    public TaskWrapper GetDownloadTask(Action<bool> onMatchDownloaded = null)
    {
        if (downloadTask == null)
        {
            string downloadUrl = GetDownloadUrl();
            Debug.Assert(!string.IsNullOrEmpty(downloadUrl), "MatchInfo: Could not get download url");
            //if (!string.IsNullOrEmpty(downloadUrl))
            //{
                downloadTask = SteamController.GenerateDownloadTask(downloadUrl, true, GetMatchFilePath(), (success, data) =>
                {
                    if (success)
                    {
                        if (matchInfoData != null)
                            SaveMatchInfoTo(matchInfoData, GetInfoFilePath());
                    }
                    onMatchDownloaded?.Invoke(success);
                }, (progress) =>
                {
                    downloadProgress = progress / 100f;
                },
                (exception) =>
                {
                    TaskManagerController.RunAction(() =>
                    {
                        SteamController.ShowErrorPopup("Download Error", "Could not retrieve the match: " + exception.ToString());
                    });
                });
            //}
        }
        return downloadTask;
    }
    public TaskWrapper GetMakeMatchInfoTask()
    {
        if (generateInfoTask == null)
        {
            generateInfoTask = TaskManagerController.CreateTask((cts) =>
            {
                GenerateMatchInfo(cts);
            });
        }
        return generateInfoTask;
    }
    public void GenerateMatchInfo(System.Threading.CancellationToken cancelToken)
    {
        //if (!availableOffline)
        //    throw new FileNotFoundException("Missing DEM file");

        if (availableOffline)
        {
            if (matchInfoData == null || extraMatchStats == null || extraMatchStats.version < ExtraMatchStats.STATS_VERSION)
            {
                CDataGCCStrike15_v2_MatchInfo analyzedInfo;
                ExtraMatchStats analyzedStats;
                CreateMatchInfo(this, out analyzedInfo, out analyzedStats, cancelToken);

                if (!cancelToken.IsCancellationRequested)
                {
                    if (analyzedInfo != null && matchInfoData == null)
                    {
                        matchInfoData = analyzedInfo;
                        SaveMatchInfoTo(matchInfoData, GetInfoFilePath());
                    }
                    if (analyzedStats != null && (extraMatchStats == null || extraMatchStats.version < ExtraMatchStats.STATS_VERSION))
                    {
                        extraMatchStats = analyzedStats;
                        SaveExtraStatsTo(extraMatchStats, GetExtraStatsFilePath());
                    }
                }
            }
        }
        else
            Debug.LogError("MatchInfo: DEM file not found");
    }
    public static void CreateMatchInfo(MatchInfo match, out CDataGCCStrike15_v2_MatchInfo matchInfo, out ExtraMatchStats extraStats, System.Threading.CancellationToken cancelToken)
    {
        matchInfo = null;
        extraStats = null;
        if (match.availableOffline)
        {
            CDataGCCStrike15_v2_MatchInfo tempMatchInfo = new CDataGCCStrike15_v2_MatchInfo();
            ExtraMatchStats tempExtraStats = new ExtraMatchStats();

            #region File Name Data
            ulong reservationId = 0;
            bool reservationIdSpecified = false;

            string[] fileNameSplits = match.fileName.Split('_');
            if (match.fileName.IndexOf("match730_") == 0 && fileNameSplits.Length == 4)
            {
                try
                {
                    reservationId = Convert.ToUInt64(fileNameSplits[1]);
                    reservationIdSpecified = true;

                    WatchableMatchInfo watchablematchinfo = new WatchableMatchInfo();
                    watchablematchinfo.tv_port = Convert.ToUInt32(fileNameSplits[2]);
                    watchablematchinfo.server_ip = Convert.ToUInt32(fileNameSplits[3]);
                    tempMatchInfo.watchablematchinfo = watchablematchinfo;
                }
                catch (Exception)
                { }
            }
            #endregion

            using (FileStream fileStream = File.Open(match.GetMatchFilePath(), FileMode.Open, FileAccess.Read))
            using (DemoInfo.DemoParser dp = new DemoInfo.DemoParser(fileStream))
            {
                #region Data Analysis
                #region Match Info Variables
                int matchStartTick = 0;

                CMsgGCCStrike15_v2_MatchmakingServerRoundStats currentRoundStats = null;
                Dictionary<uint, int> totalAssists = new Dictionary<uint, int>();
                Dictionary<uint, int> totalDeaths = new Dictionary<uint, int>();
                Dictionary<uint, int> totalEnemyHeadshots = new Dictionary<uint, int>();
                Dictionary<uint, int> totalEnemyKills = new Dictionary<uint, int>();
                Dictionary<uint, int> totalKills = new Dictionary<uint, int>();
                Dictionary<uint, int> totalMvps = new Dictionary<uint, int>();
                Dictionary<uint, int> totalScores = new Dictionary<uint, int>();
                List<List<DemoInfo.Player>> playerTeams = new List<List<DemoInfo.Player>>();

                //List<uint> accountIds = new List<uint>();
                int[] totalTeamScore = new int[2];
                #endregion

                Action<uint> AddPlayerToCurrentRound = (accountId) =>
                {
                    currentRoundStats.reservation.account_ids.Add(accountId);
                    currentRoundStats.assists.Add(0);
                    currentRoundStats.deaths.Add(0);
                    currentRoundStats.enemy_headshots.Add(0);
                    currentRoundStats.enemy_kills.Add(0);
                    currentRoundStats.kills.Add(0);
                    currentRoundStats.mvps.Add(0);
                    currentRoundStats.scores.Add(0);
                };
                Func<uint, int> GetPlayerIndex = (accountId) =>
                {
                    int playerIndex = currentRoundStats.reservation.account_ids.IndexOf(accountId);
                    if (playerIndex < 0)
                    {
                        AddPlayerToCurrentRound(accountId);
                        playerIndex = currentRoundStats.reservation.account_ids.Count - 1;
                    }
                    return playerIndex;
                };

                EventHandler<DemoInfo.MatchStartedEventArgs> matchStartedHandler = (obj, msea) =>
                {
                    matchStartTick = dp.CurrentTick;

                    foreach (var player in dp.PlayerInformations)
                    {
                        if (player != null && player.SteamID > 0)
                        {
                            uint accountId = new SteamKit2.SteamID((ulong)player.SteamID).AccountID;

                            #region Extra Stats Data
                            tempExtraStats.accountIds.Add(accountId);
                            tempExtraStats.playerNames.Add(player.Name);
                            #endregion

                            var teamToAddTo = playerTeams.Find((team) => team.Exists((teamPlayer) => teamPlayer.Team == player.Team));
                            if (teamToAddTo == null)
                            {
                                teamToAddTo = new List<DemoInfo.Player>();
                                playerTeams.Add(teamToAddTo);
                            }
                            teamToAddTo.Add(player);
                        }
                    }
                };
                EventHandler<DemoInfo.RoundStartedEventArgs> roundStartedHandler = (obj, rsea) =>
                {
                    #region Match Info Data
                    currentRoundStats = new CMsgGCCStrike15_v2_MatchmakingServerRoundStats();
                    tempMatchInfo.roundstatsall.Add(currentRoundStats);

                    currentRoundStats.team_scores.AddRange(totalTeamScore);

                    CMsgGCCStrike15_v2_MatchmakingGC2ServerReserve reservation = new CMsgGCCStrike15_v2_MatchmakingGC2ServerReserve();
                    currentRoundStats.reservation = reservation;
                    foreach (var player in dp.PlayerInformations)
                        if (player != null && player.SteamID > 0)
                            AddPlayerToCurrentRound(new SteamKit2.SteamID((ulong)player.SteamID).AccountID);
                    #endregion
                    #region Extra Stats Data
                    tempExtraStats.roundStartTicks.Add(dp.CurrentTick);
                    #endregion
                };
                EventHandler<DemoInfo.PlayerKilledEventArgs> playerKilledHandler = (obj, pkea) =>
                {
                    if (currentRoundStats != null)
                    {
                        if (pkea.Victim?.SteamID > 0)
                        {
                            uint victimAccountId = new SteamKit2.SteamID((ulong)pkea.Victim.SteamID).AccountID;
                            int victimIndex = GetPlayerIndex(victimAccountId);
                            UnityEngine.Debug.Assert(victimIndex > -1, "How do we not have this player yet?? @tick " + dp.CurrentTick + " index: " + victimIndex + " accountId: " + victimAccountId + " name " + pkea.Victim.Name);
                            if (victimIndex > -1)
                            {
                                if (!totalDeaths.ContainsKey(victimAccountId))
                                    totalDeaths[victimAccountId] = 0;
                                currentRoundStats.deaths[victimIndex] = ++totalDeaths[victimAccountId];
                            }
                        }
                        if (pkea.Killer?.SteamID > 0)
                        {
                            uint killerAccountId = new SteamKit2.SteamID((ulong)pkea.Killer.SteamID).AccountID;
                            int killerIndex = GetPlayerIndex(killerAccountId);
                            UnityEngine.Debug.Assert(killerIndex > -1, "How do we not have this player yet?? @tick " + dp.CurrentTick + " index: " + killerIndex + " accountId: " + killerAccountId + " name " + pkea.Killer.Name);
                            if (killerIndex > -1)
                            {
                                if (!totalKills.ContainsKey(killerAccountId))
                                    totalKills[killerAccountId] = 0;
                                currentRoundStats.kills[killerIndex] = ++totalKills[killerAccountId];

                                bool enemyKill = pkea.Victim.TeamID != pkea.Killer.TeamID;
                                if (!totalEnemyKills.ContainsKey(killerAccountId))
                                    totalEnemyKills[killerAccountId] = 0;
                                currentRoundStats.enemy_kills[killerIndex] += enemyKill ? ++totalEnemyKills[killerAccountId] : 0;
                                if (!totalEnemyHeadshots.ContainsKey(killerAccountId))
                                    totalEnemyHeadshots[killerAccountId] = 0;
                                currentRoundStats.enemy_headshots[killerIndex] += enemyKill && pkea.Headshot ? ++totalEnemyHeadshots[killerAccountId] : 0;
                            }
                        }
                        if (pkea.Assister?.SteamID > 0)
                        {
                            uint assisterAccountId = new SteamKit2.SteamID((ulong)pkea.Assister.SteamID).AccountID;
                            int assisterIndex = GetPlayerIndex(assisterAccountId);
                            UnityEngine.Debug.Assert(assisterIndex > -1, "How do we not have this player yet?? @tick " + dp.CurrentTick + " index: " + assisterIndex + " accountId: " + assisterAccountId + " name " + pkea.Assister.Name);
                            if (assisterIndex > -1)
                            {
                                if (!totalAssists.ContainsKey(assisterAccountId))
                                    totalAssists[assisterAccountId] = 0;
                                currentRoundStats.assists[assisterIndex] = ++totalAssists[assisterAccountId];
                            }
                        }
                    }
                };
                EventHandler<DemoInfo.RoundMVPEventArgs> roundMVPHandler = (obj, rmea) =>
                {
                    if (rmea.Player?.SteamID > 0)
                    {
                        uint playerAccountId = new SteamKit2.SteamID((ulong)rmea.Player.SteamID).AccountID;
                        if (!totalMvps.ContainsKey(playerAccountId))
                            totalMvps[playerAccountId] = 0;

                        int playerIndex = GetPlayerIndex(playerAccountId);
                        UnityEngine.Debug.Assert(playerIndex > -1, "How do we not have this player yet?? @tick " + dp.CurrentTick + " index: " + playerIndex + " accountId: " + playerAccountId + " name " + rmea.Player.Name);
                        if (playerIndex > -1 && playerIndex < currentRoundStats.mvps.Count)
                            currentRoundStats.mvps[playerIndex] = ++totalMvps[playerAccountId];
                    }
                };
                EventHandler<DemoInfo.RoundEndedEventArgs> roundEndedHandler = (obj, reea) =>
                {
                    #region Match Info Data
                    Debug.Assert(currentRoundStats != null, "How can you end a round without starting it!? @tick " + dp.CurrentTick);
                    if (currentRoundStats != null)
                    {
                        if (reea.Winner != DemoInfo.Team.Spectate)
                        {
                            int teamIndex = playerTeams.FindIndex((team) => team[0].Team == reea.Winner);
                            if (teamIndex > -1 && teamIndex < totalTeamScore.Length)
                                currentRoundStats.team_scores[teamIndex] = ++totalTeamScore[teamIndex];
                        }
                        currentRoundStats.match_duration = (int)((dp.CurrentTick - matchStartTick) * dp.TickTime);

                        foreach (var player in dp.PlayerInformations)
                        {
                            if (player != null && player.SteamID > 0)
                            {
                                uint playerAccountId = new SteamKit2.SteamID((ulong)player.SteamID).AccountID;
                                int playerIndex = GetPlayerIndex(playerAccountId);
                                Debug.Assert(playerIndex > -1, "How do we not have this player yet?? @tick " + dp.CurrentTick + " index: " + playerIndex + " accountId: " + playerAccountId + " name " + player.Name);
                                currentRoundStats.scores[playerIndex] = player.AdditionaInformations.Score;
                            }
                        }
                    }
                    #endregion
                    #region Extra Stats Data
                    tempExtraStats.roundEndTicks.Add(dp.CurrentTick);
                    tempExtraStats.roundWinner.Add((int)reea.Winner);
                    #endregion
                };
                #endregion

                dp.MatchStarted += matchStartedHandler;
                dp.RoundStart += roundStartedHandler;
                dp.PlayerKilled += playerKilledHandler;
                dp.RoundMVP += roundMVPHandler;
                dp.RoundEnd += roundEndedHandler;

                dp.ParseHeader();
                while (dp.ParseNextTick() && !cancelToken.IsCancellationRequested)
                {
                    match.infoProgress = dp.CurrentTick / (float)dp.Header.PlaybackFrames;
                }

                dp.MatchStarted -= matchStartedHandler;
                dp.RoundStart -= roundStartedHandler;
                dp.PlayerKilled -= playerKilledHandler;
                dp.RoundMVP -= roundMVPHandler;
                dp.RoundEnd -= roundEndedHandler;

                #region Last round stats
                if (reservationIdSpecified)
                    currentRoundStats.reservationid = reservationId;
                currentRoundStats.reservation.game_type = (uint)(GameType)Enum.Parse(typeof(GameType), dp.Map);

                if (totalTeamScore[0] != totalTeamScore[1])
                {
                    var winningTeam = (DemoInfo.Team)currentRoundStats.round_result;
                    currentRoundStats.match_result = (winningTeam == DemoInfo.Team.Terrorist ? 1 : 2); //1 is CT, 2 is T. I do the switching because of team switching at half
                }
                else
                    currentRoundStats.match_result = 0;
                #endregion
            }

            if (cancelToken.IsCancellationRequested)
            {
                tempMatchInfo = null;
                tempExtraStats = null;
            }
            matchInfo = tempMatchInfo;
            extraStats = tempExtraStats;
        }
    }

    public CMsgGCCStrike15_v2_MatchmakingServerRoundStats GetLastRoundStats()
    {
        if (matchInfoData != null && matchInfoData.roundstatsall != null && matchInfoData.roundstatsall.Count > 0)
            return matchInfoData.roundstatsall[matchInfoData.roundstatsall.Count - 1];
        else
            return null;
    }

    public string GetDownloadUrl()
    {
        string downloadUrl = null;
        var lastRoundStats = GetLastRoundStats();
        if (lastRoundStats != null && !string.IsNullOrEmpty(lastRoundStats.map))
        {
            downloadUrl = lastRoundStats.map;
        }
        return downloadUrl;
    }

    public string GetMapName()
    {
        if (string.IsNullOrEmpty(mapName))
        {
            if (availableOffline)
            {
                using (FileStream fs = new FileStream(GetMatchFilePath(), FileMode.Open, FileAccess.Read))
                using (DemoInfo.DemoParser dp = new DemoInfo.DemoParser(fs))
                {
                    dp.ParseHeader();
                    mapName = dp.Map;
                }
            }
            else
            {
                var lastRound = GetLastRoundStats();
                if (lastRound != null)
                    mapName = ((GameType)lastRound.reservation.game_type).ToString();
            }
        }
        return mapName;
    }

    public static void SaveMatchInfoTo(CDataGCCStrike15_v2_MatchInfo matchInfoData, string filePath)
    {
        using (FileStream fs = File.Open(filePath, FileMode.OpenOrCreate, FileAccess.Write))
        using (Stream serializedStream = SerializeInfo(matchInfoData))
        {
            serializedStream.CopyTo(fs);
        }
    }
    public static void SaveExtraStatsTo(ExtraMatchStats extraInfo, string filePath)
    {
        using (FileStream fs = File.Open(filePath, FileMode.OpenOrCreate, FileAccess.Write))
        using (StreamWriter sw = new StreamWriter(fs))
        {
            sw.Write(UnityEngine.JsonUtility.ToJson(extraInfo));
        }
    }
    public static ExtraMatchStats ReadExtraStatsFile(string location)
    {
        ExtraMatchStats extraStats;
        using (FileStream extraStatsFileStream = new FileStream(location, FileMode.Open))
        using (StreamReader streamReader = new StreamReader(extraStatsFileStream))
        {
            extraStats = UnityEngine.JsonUtility.FromJson<ExtraMatchStats>(streamReader.ReadToEnd());
        }
        return extraStats;
    }
    public static CDataGCCStrike15_v2_MatchInfo ReadInfoFile(string location)
    {
        CDataGCCStrike15_v2_MatchInfo matchInfoData;

        using (FileStream infoFileStream = new FileStream(location, FileMode.Open))
        {
            ProtoBuf.Meta.TypeModel model = (ProtoBuf.Meta.TypeModel)Activator.CreateInstance(Type.GetType("MyProtoModel, MyProtoModel"));
            matchInfoData = (CDataGCCStrike15_v2_MatchInfo)model.Deserialize(infoFileStream, null, typeof(CDataGCCStrike15_v2_MatchInfo));
            //matchInfoData = Serializer.Deserialize<CDataGCCStrike15_v2_MatchInfo>(infoFileStream);
        }

        return matchInfoData;
    }
    public static Stream SerializeInfo(CDataGCCStrike15_v2_MatchInfo matchInfo)
    {
        MemoryStream memoryStream = new MemoryStream();
        ProtoBuf.Meta.TypeModel model = (ProtoBuf.Meta.TypeModel)Activator.CreateInstance(Type.GetType("MyProtoModel, MyProtoModel"));
        model.Serialize(memoryStream, matchInfo);
        //Serializer.Serialize(memoryStream, matchInfo);
        memoryStream.Position = 0;
        return memoryStream;
    }

    public override bool Equals(object obj)
    {
        if (obj == null || !(obj is MatchInfo))
            return false;

        MatchInfo otherMatch = (MatchInfo)obj;
        var firstName = GenerateName(matchInfoData);
        var secondName = GenerateName(otherMatch.matchInfoData);
        return firstName != null && secondName != null && firstName.Equals(secondName, StringComparison.OrdinalIgnoreCase);
    }
    public override int GetHashCode()
    {
        return -1223983128 + EqualityComparer<string>.Default.GetHashCode(fileName);
    }
}

public struct MatchSignature
{
    public ulong matchId;
    public ulong outcomeId;
    public uint token;

    public MatchSignature(ulong _matchId, ulong _reservationId, uint _tvPort)
    {
        matchId = _matchId;
        outcomeId = _reservationId;
        token = _tvPort;
    }
    public override string ToString()
    {
        return matchId + " " + outcomeId + " " + token;
    }
}
public enum GameType { de_train = 1032, de_dust2 = 520, de_inferno = 4104, de_nuke = 8200, de_vertigo = 16392, cs_office = 65544, de_mirage = 32776, de_cache = 1048584, de_zoo = 33554440, cs_agency = 134217736, de_overpass = 268435464, de_workout = 67108872, }