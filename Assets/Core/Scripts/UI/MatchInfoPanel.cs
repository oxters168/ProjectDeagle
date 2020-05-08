using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityHelpers;

public class MatchInfoPanel : MonoBehaviour
{
    private readonly string MATCH_STATS_GEN = "MatchStatsMaker";

    public static MatchInfo match { get; private set; }

    public PlayerStatsItemController playerStatsItemPrefab;
    private ObjectPool<PlayerStatsItemController> playerStatsItemsPool;
    public RectTransform playerStatsHolder;
    [Space(10)]
    public RoundStatItemController roundStatItemPrefab;
    private ObjectPool<RoundStatItemController> roundStatItemsPool;
    public RectTransform roundStatsHolder;
    public float maxRoundStatWidth = 32;

    [Space(10)]
    public GameObject noMatchPanel, viewingMatchPanel;
    public TMPro.TextMeshProUGUI downloadStatusText;
    public GameObject downloadBarBack;
    public Image downloadBar;
    public TwoPartButton watch3DButton, watch2DButton;
    public Button shareButton;

    private float canDownloadCheckGap = 30;
    //private string status;

    Dictionary<SteamKit2.SteamID, string> userNames = new Dictionary<SteamKit2.SteamID, string>();

    private void Awake()
    {
        playerStatsItemsPool = new ObjectPool<PlayerStatsItemController>(playerStatsItemPrefab, 5, false, true, playerStatsHolder, false);
        roundStatItemsPool = new ObjectPool<RoundStatItemController>(roundStatItemPrefab, 5, false, true, roundStatsHolder, false);
    }
    private void Update()
    {
        noMatchPanel.SetActive(match == null);
        viewingMatchPanel.SetActive(match != null);
        if (match != null)
        {
            RefreshLoadingBar();

            bool isLoading = match.IsLoading;
            watch3DButton.buttonText.text = isLoading ? "Cancel" : "Watch 3D";
            watch2DButton.gameObject.SetActive(!isLoading);

            bool taskMakerBusy = !isLoading && TaskMaker.IsBusy();
            watch3DButton.button.interactable = !taskMakerBusy;
            watch2DButton.button.interactable = !taskMakerBusy;
        }
    }

    public void Watch(bool twoDee)
    {
        if (match != null)
        {
            if (match.IsLoading)
            {
                match.CancelChainedTask();
            }
            else
            {
                match.LoadMatch(twoDee);
            }
        }
    }
    private void RefreshLoadingBar()
    {
        MapData currentMap = match.GetMap();

        float percent = 0;
        if (match.IsDownloading)
            percent = Mathf.Clamp01(match.downloadProgress);
        else
        {
            if (match.IsGeneratingInfo)
                percent = match.infoProgress;
            else if (currentMap.IsDownloading2D || currentMap.IsDownloading3D || currentMap.IsDownloadingDependencies)
                percent = DepotDownloader.ContentDownloader.DownloadPercent;
            else
                percent = currentMap.GetPercentBuilt();
        }

        downloadBarBack.SetActive(match.IsLoading);
        downloadBar.fillAmount = percent;
        downloadStatusText.text = match.GetStatus();
    }
    public void SetMatch(MatchInfo _match)
    {
        match = _match;
        DisplayPlayerStats(match);
        DisplayRoundStats(match);
        SetShareButtonInteractability();
    }

    private void SetShareButtonInteractability()
    {
        shareButton.interactable = false;
        if (match != null)
        {
            if (Time.time - match.lastCanDownloadCheckTime > canDownloadCheckGap)
            {
                match.StartCanDownloadCheck((canDownload) =>
                {
                    TaskManagerController.RunAction(() =>
                    {
                        shareButton.interactable = canDownload;
                    });
                });
            }
            else
                shareButton.interactable = match.lastCanDownload;
        }
    }
    public void NativeShare()
    {
        var nativeShare = new NativeShare();
        nativeShare.SetText("Check out this match!\n\nCopy the link below and paste it in your browser or directly in the GO:View app to watch it:\n\n" + match.GetSharecodeUrl());
        nativeShare.Share();
    }
    private void DisplayPlayerStats(MatchInfo match)
    {
        playerStatsItemsPool.ReturnAll();

        var lastRoundStats = match?.GetLastRoundStats();
        if (lastRoundStats != null)
        {
            List<uint> accountIds = lastRoundStats.reservation.account_ids;
            for (int accountIndex = 0; accountIndex < accountIds.Count; accountIndex++)
            {
                PlayerStatsItemController playerStatsItem = playerStatsItemsPool.Get();
                playerStatsItem.transform.SetAsLastSibling();

                string name = "Unknown";
                if (match.extraMatchStats != null)
                {
                    int extraAccountIndex = match.extraMatchStats.accountIds.IndexOf(accountIds[accountIndex]);
                    name = match.extraMatchStats.playerNames[extraAccountIndex];
                }
                else
                {
                    var steamId = new SteamKit2.SteamID(accountIds[accountIndex], SteamKit2.EUniverse.Public, SteamKit2.EAccountType.Individual);
                    name = steamId.AccountID.ToString();

                    var friend = SteamController.steamInScene.GetFriendWithAccountId(steamId);
                    if (friend != null)
                        name = friend.GetDisplayName();
                    else
                    {
                        if (userNames.ContainsKey(steamId))
                            name = userNames[steamId];
                        else
                        {
                            if (SteamController.steamInScene.IsLoggedIn)
                                SteamController.steamInScene.RequestProfileInfo(steamId, (profileInfo) =>
                                {
                                    TaskManagerController.RunAction(() =>
                                    {
                                        //Debug.Log(profileInfo.Result);
                                        if (!string.IsNullOrEmpty(profileInfo.RealName))
                                        {
                                            userNames[steamId] = profileInfo.RealName;
                                            playerStatsItem.nameText.text = profileInfo.RealName;
                                        }
                                    });
                                });
                        }
                    }
                }

                playerStatsItem.nameText.text = name;
                playerStatsItem.killsCountText.text = lastRoundStats.kills[accountIndex].ToString();
                playerStatsItem.assistsCountText.text = lastRoundStats.assists[accountIndex].ToString();
                playerStatsItem.deathsCountText.text = lastRoundStats.deaths[accountIndex].ToString();
                playerStatsItem.scoreText.text = lastRoundStats.scores[accountIndex].ToString();
            }
        }
    }
    private void DisplayRoundStats(MatchInfo match)
    {
        roundStatItemsPool.ReturnAll();
        if (match != null && match.extraMatchStats != null && match.extraMatchStats.roundWinner != null)
        {
            float roundStatWidth = Mathf.Min(maxRoundStatWidth, roundStatsHolder.rect.size.x / match.extraMatchStats.roundWinner.Count), roundStatHeight = roundStatsHolder.rect.size.y;
            foreach (int roundWinner in match.extraMatchStats.roundWinner)
            {
                var roundStatItem = roundStatItemsPool.Get();
                roundStatItem.transform.SetAsLastSibling();
                roundStatItem.SelfRectTransform.sizeDelta = new Vector2(roundStatWidth, roundStatHeight);

                roundStatItem.SetWinner((DemoInfo.Team)roundWinner);
            }
        }
    }
}
