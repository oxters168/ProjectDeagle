using System.IO;
using System.Linq;
using System.Collections.Generic;

using UnityEngine;
using UnityHelpers;

public class MatchesPanel : MonoBehaviour
{
    public static MatchesPanel matchesInScene;
    public MatchInfoPanel matchInfoPanel;
    //public ListViewController matchesList;
    public CustomListController matchesList;

    //private float lastRecentMatchesRequest;
    //private SteamKit2.EPersonaState userLastState;
    //private float recentMatchesRequestCooldown = 5;
    //private bool blockRefresh;

    public TMPro.TextMeshProUGUI emptyListLabel;
    public string loggedInEmptyListMessage, notLoggedInEmptyListMessage;

    private void Awake()
    {
        matchesInScene = this;
        matchesList.onItemSelected += MatchesList_onItemSelected;
    }
    private void Start()
    {
        SteamController.steamInScene.onRecentGamesReceived += SteamInScene_onRecentGamesReceived;
    }
    private void OnEnable()
    {
        RepopulateList();
        //CheckForNewMatches();
    }
    private void OnDisable()
    {
        matchInfoPanel.SetMatch(null);
    }
    private void Update()
    {
        UpdateMatchUI();
    }

    private void UpdateMatchUI()
    {
        emptyListLabel.text = SteamController.steamInScene.IsLoggedIn ? loggedInEmptyListMessage : notLoggedInEmptyListMessage;
    }
    private void MatchesList_onItemSelected(object item)
    {
        if (item != null)
        {
            MatchInfo selectedMatch = (MatchInfo)item;
            matchInfoPanel.SetMatch(selectedMatch);
        }
    }

    public void RepopulateList()
    {
        //CheckForNewMatches();
        GetOfflineDemos();
        SteamController.steamInScene.RequestRecentMatches();
    }
    //public void CheckForNewMatches()
    //{
    //    GetOfflineDemos();
    //    RequestRecentMatches();
    //}
    private void SteamInScene_onRecentGamesReceived(SteamKit2.GC.CSGO.Internal.CMsgGCCStrike15_v2_MatchList matchList)
    {
        SteamController.LogToConsole("\nReceived " + matchList.matches.Count() + " match(es)");
        TaskManagerController.RunAction(() =>
        {
            foreach (var match in matchList.matches)
                MatchInfo.FindOrCreateMatch(match);
            RefreshList();
        });
    }

    //private void RequestRecentMatches()
    //{

    //}
    /*public void SendRequestFullGame(MatchSignature matchSignature)
    {
        //var matchSig = MatchInfo.DecodeShareCode("CSGO-WMw4F-5EYMy-7Hk79-JhRrm-tNTWJ");
        //RequestFullGameInfo(matchSig.matchId, matchSig.outcomeId, matchSig.token);
        if (SteamController.steamInScene != null && SteamController.steamInScene.IsLoggedIn && !SteamController.steamInScene.steam3.isAnon)
        {
            SteamController.LogToConsole("\nRequesting full game info");
            //blockRefresh = true;

            var account = SteamController.steamInScene.GetFriendWithAccountId(SteamController.steamInScene.userAccountId);
            userLastState = account != null ? account.GetState() : (SteamKit2.EPersonaState)SettingsController.personaState;

            SteamController.steamInScene.SetPersonaState(SteamKit2.EPersonaState.Invisible);
            SteamController.steamInScene.PlayCStrike();
            StartCoroutine(CommonRoutines.WaitToDoAction(() =>
            {
                SteamController.steamInScene.RequestFullGameInfo(matchSignature.matchId, matchSignature.outcomeId, matchSignature.token);
                //lastRecentMatchesRequest = Time.time;
                //blockRefresh = false;
            }, 30, () => SteamController.steamInScene.playingApp == 730));
        }
    }
    public void RequestRecentMatches()
    {
        if (SteamController.steamInScene != null && SteamController.steamInScene.IsLoggedIn && !SteamController.steamInScene.steam3.isAnon && !blockRefresh && Time.time - lastRecentMatchesRequest > recentMatchesRequestCooldown)
        {
            SteamController.LogToConsole("\nRequesting recent matches");
            blockRefresh = true;

            var account = SteamController.steamInScene.GetFriendWithAccountId(SteamController.steamInScene.userAccountId);
            userLastState = account != null ? account.GetState() : (SteamKit2.EPersonaState)SettingsController.personaState;

            SteamController.steamInScene.SetPersonaState(SteamKit2.EPersonaState.Invisible);
            SteamController.steamInScene.PlayCStrike();
            StartCoroutine(CommonRoutines.WaitToDoAction(() =>
            {
                SteamController.steamInScene.RequestRecentGamesPlayed(SteamController.steamInScene.userAccountId);
                lastRecentMatchesRequest = Time.time;
                blockRefresh = false;
            }, 30, () => { return SteamController.steamInScene.playingApp == 730; }, () => { lastRecentMatchesRequest = Time.time; blockRefresh = false; }));
        }
    }*/
    private void GetOfflineDemos()
    {
        SteamController.LogToConsole("\nFinding matches on device");
        IEnumerable<string> demoFiles = null;
        if (Directory.Exists(SettingsController.matchesLocation))
            demoFiles = Directory.EnumerateFiles(SettingsController.matchesLocation, "*.dem");
        else
            SteamController.LogToConsole("Matches directory does not exist");

        if (demoFiles != null)
        {
            SteamController.LogToConsole("Found " + demoFiles.Count() + " match(es) on device");
            foreach (string file in demoFiles)
                TaskManagerController.RunAction(() =>
                {
                    MatchInfo.FindOrCreateMatch(Path.GetFileNameWithoutExtension(file));
                    //AddAvailableMatch(match);
                });
            RefreshList();
        }
        else
            SteamController.LogToConsole("Did not find any match(es) on device");
    }
    private void RefreshList()
    {
        matchesList.ClearItems();
        foreach (var match in MatchInfo.GetCachedMatches())
            matchesList.AddToList(match);
    }
    /*public void AddAvailableMatch(MatchInfo match)
    {
        MatchInfo[] matchesInList = matchesList.GetItems().Cast<MatchInfo>().ToArray();
        if (matchesInList.Where(availableMatch => availableMatch.Equals(match)).Count() <= 0)
        {
            SteamController.LogToConsole("Adding match to list");
            matchesList.AddToList(match);
        }
        else
            SteamController.LogToConsole("Match already in list");
    }*/
}
