using UnityEngine;
using UnityHelpers;

using System;
using System.IO;
using System.Net;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

using SteamKit2;
using SteamKit2.GC; // brings in the GC related classes
using SteamKit2.Internal; // brings in our protobuf client messages
using SteamKit2.GC.CSGO.Internal; // brings in csgo specific protobuf messages

using DepotDownloader;

using Doozy.Engine.UI;
using SteamWebAPI2.Utilities;

public class SteamController : MonoBehaviour, IDebugListener
{
    public static SteamController steamInScene { get; private set; }
    public const string SENTRY_FILE_NAME = "sentry.bin";
    private const uint APPID = 730; //csgo's appid
    private const uint DSID = 740; //csgo's ds appid
    public string webApiKey;

    private SteamClient steamClient;
    private SteamUser steamUser;
    private SteamApps steamApps;
    private SteamFriends steamFriends;
    private uint requestsMade;
    private SteamWebInterfaceFactory webInterfaceFactory;
    private SteamWebAPI2.Interfaces.SteamUser steamUserWeb;

    public bool playingBlocked { get; private set; }
    public uint playingApp { get; private set; }

    private List<string> debugLogMessages = new List<string>();
    private int lastDebugMessageCount;
    public bool suppressDebugLog;

    private SteamGameCoordinator gameCoordinator;
    public bool suppressLog;

    public enum AuthType { None, Gaurd, TwoFA, }

    public event UserPersonaStateChangeHandler onUserStateChanged;
    public delegate void UserPersonaStateChangeHandler(SteamUserData user);

    public event ChatMessageCallback onFriendChatReceived;
    public delegate void ChatMessageCallback(uint fromId, EChatEntryType entryType, string message);
    public event ChatHistoryCallback onChatHistoryReceived;
    public delegate void ChatHistoryCallback(uint chatPartnerId, IEnumerable<SteamFriends.FriendMsgHistoryCallback.FriendMessage> messages);

    public event MatchesReceivedHandler onRecentGamesReceived;
    public delegate void MatchesReceivedHandler(CMsgGCCStrike15_v2_MatchList matchList);

    public bool isManifestDownloading { get; private set; }
    public uint userAccountId { get { return steamUser.SteamID.AccountID; } }
    public bool IsLoggedIn { get { return steamUser.SteamID != null; } }
    private List<SteamUserData> friendData = new List<SteamUserData>();

    public Steam3Session steam3;
    private Dictionary<ulong, ProtoManifest> csgoManifests = new Dictionary<ulong, ProtoManifest>();

    public Sprite loggingInIcon, anonIcon;
    public AccountAvatarController[] accountAvatars;

    private Coroutine waitForLoginToGetMatchInfoRoutine;
    private UIPopup loadingPopup;

    private void Awake()
    {
        steamInScene = this;
        #if UNITY_ANDROID || UNITY_IOS
        ImaginationOverflow.UniversalDeepLinking.DeepLinkManager.Instance.LinkActivated += Instance_LinkActivated;
        #endif

        //InitSteamWeb();
        InitSteamClient();
        SetupSteamSession();
    }
    private void OnEnable()
    {
        steam3.onLogonFailed += Steam3_onLogonFailed;
        ContentDownloader.onManifestReceived += ContentDownloader_onManifestReceived;
    }
    private void OnDisable()
    {
        steam3.onLogonFailed -= Steam3_onLogonFailed;
        ContentDownloader.onManifestReceived -= ContentDownloader_onManifestReceived;
    }
    private void Update()
    {
        UpdateAccountButton();
        ConsoleDebugLogMessages();
    }
    private void OnDestroy()
    {
        suppressLog = true;
        steam3.Disconnect(true);
    }

    #if UNITY_ANDROID || UNITY_IOS
    private void Instance_LinkActivated(ImaginationOverflow.UniversalDeepLinking.LinkActivation s)
    {
        LogToConsole("\nReceived deep link");
        SendRequestFullGame(s.RawQueryString);
    }
    #endif
    private void Steam3_onLogonFailed(EResult reason)
    {
        LogOff();
        if (reason != EResult.AccountLoginDeniedNeedTwoFactor && reason != EResult.AccountLogonDenied)
            TaskManagerController.RunAction(() => { ShowErrorPopup("Login Error", reason.ToString()); });
    }

    private void InitSteamWeb()
    {
        webInterfaceFactory = new SteamWebInterfaceFactory(webApiKey);
        steamUserWeb = webInterfaceFactory.CreateSteamWebInterface<SteamWebAPI2.Interfaces.SteamUser>(new System.Net.Http.HttpClient());
    }
    private void SetupSteamSession()
    {
        ConfigStore.LoadFromFile(Path.Combine(SettingsController.LogonLoc(), "DepotDownloader.config"));
        string username = SettingsController.user;
        string loginKey = null;

        if (!string.IsNullOrEmpty(username))
            ConfigStore.TheConfig.LoginKeys.TryGetValue(username, out loginKey);

        if (!string.IsNullOrEmpty(loginKey))
        {
            steam3 = new Steam3Session(steamClient, CallbackManagerController.manager,
                new SteamUser.LogOnDetails()
                {
                    Username = username,
                    ShouldRememberPassword = true,
                    LoginKey = loginKey,
                });
        }
        else
        {
            steam3 = new Steam3Session(steamClient, CallbackManagerController.manager);
            //TaskManagerController.RunActionAsync((ct) => { return steam3.LoginAsAnon(); });
        }
    }

    public SteamUserData GetFriendWithAccountId(SteamID id)
    {
        SteamUserData user = null;
        Debug.Assert(friendData != null);
        if (friendData != null)
            user = friendData.FirstOrDefault(friend =>
            {
                Debug.Assert(friend != null);
                Debug.Assert(id != null);
                if (friend != null && id != null)
                    return friend.GetIdUInt32() == id.AccountID;
                return false;
            });
        return user;
    }
    public string GetFriendDisplayName(SteamID id)
    {
        SteamUserData friend = GetFriendWithAccountId(id);
        return friend != null ? friend.GetDisplayName() : "Not on friends list";
    }
    public SteamUserData[] GetFriends()
    {
        SteamUserData[] friends = new SteamUserData[0];
        if (friendData.Count > 0)
            friends = friendData.Where(steamUser => steamUser.GetRelationship() == EFriendRelationship.Friend).ToArray();

        return friends;
    }
    private void ClearFriendData()
    {
        friendData.Clear();
    }
    public uint GetCurrentAppId()
    {
        return steam3.isAnon ? DSID : APPID;
    }

#region UI
    private void UpdateAccountButton()
    {
        foreach (AccountAvatarController accountAvatar in accountAvatars)
        {
            if (IsLoggedIn && !steam3.isAnon)
            {
                SteamUserData user = GetFriendWithAccountId(userAccountId);
                if (user != null)
                {
                    Sprite accountSprite = user.GetAvatarAsSprite();
                    if (accountSprite != null)
                    {
                        accountAvatar.accountImage.color = Color.white;
                        accountAvatar.accountImage.sprite = accountSprite;
                    }
                    accountAvatar.personaStateImage.color = ChatController.GetStateColor(user.GetState());
                }
            }
            else if (IsLoggedIn && steam3.isAnon)
            {
                accountAvatar.accountImage.color = Color.black;
                accountAvatar.accountImage.sprite = anonIcon;
            }
            else
            {
                accountAvatar.accountImage.color = Color.black;
                if (steam3.isLoggingIn || steam3.bConnecting)
                    accountAvatar.accountImage.sprite = loggingInIcon;
                else
                    accountAvatar.accountImage.sprite = SettingsController.settingsInScene.defaultAccountIcon;

                accountAvatar.personaStateImage.color = Color.clear;
            }
        }
    }
    public void AccountButtonPressed()
    {
        if (IsLoggedIn)
        {
            ShowPersonaPopup();
        }
        else if (!steam3.isLoggingIn && !steam3.bConnecting)
        {
            ShowLoginPopup();
        }
    }
    public static void ShowErrorPopup(string titleString, string messageString)
    {
        UIPopup errorPopup = UIPopupManager.ShowPopup("ErrorPopup", true, false);
        errorPopup.Data.SetLabelsTexts(titleString, messageString);
    }
    public static void ShowPromptPopup(string titleString, string messageString, Action<bool> onResponseReceived, string submitButtonText = "Submit", string cancelButtonText = "Cancel")
    {
        //TaskManagerController.RunAction(() =>
        //{
            UIPopup promptPopup = UIPopupManager.ShowPopup("PromptPopup", true, false);
            promptPopup.Data.SetLabelsTexts(titleString, messageString);
            promptPopup.Data.SetButtonsLabels(submitButtonText, cancelButtonText);
            promptPopup.Data.SetButtonsCallbacks(() => { promptPopup.Hide(); onResponseReceived?.Invoke(true); }, () => { promptPopup.Hide(); onResponseReceived?.Invoke(false); });
        //});
    }
    public static void ShowPersonaPopup()
    {
        var personaPopup = UIPopupManager.ShowPopup("PersonaPopup", true, false);

        for (int i = 0; i < 5; i++)
            personaPopup.Data.Buttons[i].Interactable = !steamInScene.steam3.isAnon;

        personaPopup.Data.SetButtonsCallbacks(
            () => { steamInScene.SetPersonaState(EPersonaState.Online); personaPopup.Hide(); },
            () => { steamInScene.SetPersonaState(EPersonaState.Away); personaPopup.Hide(); },
            () => { steamInScene.SetPersonaState(EPersonaState.Busy); personaPopup.Hide(); },
            () => { steamInScene.SetPersonaState(EPersonaState.Snooze); personaPopup.Hide(); },
            () => { steamInScene.SetPersonaState(EPersonaState.Invisible); personaPopup.Hide(); },
            () => { steamInScene.LogOff(); personaPopup.Hide(); });
    }
    public static void ShowLoginPopup()
    {
        var loginPopup = UIPopupManager.ShowPopup("LoginPopup", true, false);
        loginPopup.Data.SetButtonsCallbacks(
            () =>
            {
                steamInScene.LoginAsAnon();
                loginPopup.Hide();
            },
            () =>
            {
                var popupItems = loginPopup.GetComponent<LoginPopupItemsContainer>();
                steamInScene.Login(popupItems.userField.text, popupItems.passField.text, popupItems.rememberMe.isOn);
                loginPopup.Hide();
            });
    }
    public void ShowSharecodePopup()
    {
        if (IsLoggedIn && !steam3.isAnon)
        {
            var sharecodePopup = UIPopupManager.ShowPopup("SharecodePopup", true, false);
            sharecodePopup.Data.SetButtonsCallbacks(
                () =>
                {
                    var sharecodePopupItemsContainer = sharecodePopup.GetComponent<SharecodePopupItemsContainer>();
                    string sharecode = sharecodePopupItemsContainer.sharecodeField.text;
                    SendRequestFullGame(sharecode);
                    sharecodePopup.Hide();
                });
        }
        else
            ShowErrorPopup("Not Logged In", "You need to be logged in to Steam due to the way sharecodes work.");
    }
    public void ShowLoadingPopup(string loadingText, Action cancelAction)
    {
        loadingPopup = UIPopupManager.ShowPopup("LoadingPopup", true, false);
        loadingPopup.Data.SetLabelsTexts(loadingText);
        loadingPopup.Data.SetButtonsCallbacks(() =>
        {
            HideLoadingPopup();
            cancelAction?.Invoke();
        });
    }
    public void HideLoadingPopup()
    {
        loadingPopup?.Hide();
        loadingPopup = null;
    }

    private void ConsoleDebugLogMessages()
    {
        if (!suppressDebugLog)
            for (int i = lastDebugMessageCount; i < debugLogMessages.Count; i++)
                LogToConsole(debugLogMessages[i]);

        lastDebugMessageCount = debugLogMessages.Count;
    }
    //IDebugListener implemented function
    public void WriteLine(string category, string msg)
    {
        debugLogMessages.Add("\n[IDebug] - " + category + ": " + msg);
    }
#endregion
#region Steam Client Functions
    private void InitSteamClient()
    {
        // create our steamclient instance
        steamClient = new SteamClient();
        // create the callback manager which will route callbacks to function calls
        CallbackManagerController.manager = new CallbackManager(steamClient);
        // get the steamuser handler, which is used for logging on after successfully connecting
        steamUser = steamClient.GetHandler<SteamUser>();
        // get the steam friends handler, which is used for interacting with friends on the network after logging on
        steamFriends = steamClient.GetHandler<SteamFriends>();
        steamApps = steamClient.GetHandler<SteamApps>();

        gameCoordinator = steamClient.GetHandler<SteamGameCoordinator>();

        // register a few callbacks we're interested in
        // these are registered upon creation to a callback manager, which will then route the callbacks
        // to the functions specified
        CallbackManagerController.manager.Subscribe<SteamClient.ConnectedCallback>(OnConnected);
        CallbackManagerController.manager.Subscribe<SteamClient.DisconnectedCallback>(OnDisconnected);

        CallbackManagerController.manager.Subscribe<SteamUser.LoggedOnCallback>(OnLoggedOn);
        CallbackManagerController.manager.Subscribe<SteamUser.LoggedOffCallback>(OnLoggedOff);
        //manager.Subscribe<SteamUser.LoginKeyCallback>(OnLoginKeyReceived);

        // this callback is triggered when the steam servers wish for the client to store the sentry file
        //manager.Subscribe<SteamUser.UpdateMachineAuthCallback>(OnMachineAuth);

        // we use the following callbacks for friends related activities
        CallbackManagerController.manager.Subscribe<SteamUser.AccountInfoCallback>(OnAccountInfo);
        CallbackManagerController.manager.Subscribe<SteamFriends.FriendsListCallback>(OnFriendsList);
        CallbackManagerController.manager.Subscribe<SteamFriends.PersonaStateCallback>(OnPersonaState);
        CallbackManagerController.manager.Subscribe<SteamFriends.FriendAddedCallback>(OnFriendAdded);

        CallbackManagerController.manager.Subscribe<SteamFriends.ChatMsgCallback>(OnChatMessageReceived);
        CallbackManagerController.manager.Subscribe<SteamFriends.ChatInviteCallback>(OnInviteReceived);
        CallbackManagerController.manager.Subscribe<SteamFriends.ChatEnterCallback>(OnChatEntered);
        CallbackManagerController.manager.Subscribe<SteamFriends.ChatMemberInfoCallback>(OnChatMemberInfoReceived);
        CallbackManagerController.manager.Subscribe<SteamFriends.ChatActionResultCallback>(OnChatActionResultReceived);
        CallbackManagerController.manager.Subscribe<SteamFriends.FriendMsgHistoryCallback>(OnFriendMessageHistoryReceived);
        CallbackManagerController.manager.Subscribe<SteamFriends.FriendMsgCallback>(OnFriendMessageReceived);
        CallbackManagerController.manager.Subscribe<SteamFriends.FriendMsgEchoCallback>(OnFriendMessageEchoReceived);
        CallbackManagerController.manager.Subscribe<SteamFriends.ClientPlayingSessionStateCallback>(OnPlayingSessionStateReceived);
        CallbackManagerController.manager.Subscribe<SteamFriends.ProfileInfoCallback>(OnProfileInfoReceived);

        CallbackManagerController.manager.Subscribe<SteamGameCoordinator.MessageCallback>(OnGCMessage);

        //CallbackManagerController.manager.Subscribe<SteamApps.PICSProductInfoCallback>(ProductInfoResponse);
        //CallbackManagerController.manager.Subscribe<SteamApps.AppOwnershipTicketCallback>(AppOwnershipResponse);
        //CallbackManagerController.manager.Subscribe<SteamApps.DepotKeyCallback>(DepotDecryptionKeyResponse);
        //CallbackManagerController.manager.Subscribe<SteamApps.CDNAuthTokenCallback>(CDNAuthTokenResponse);
        CallbackManagerController.manager.Subscribe<SteamApps.FreeLicenseCallback>(FreeLicenseResponse);

        DebugLog.AddListener(this);
        DebugLog.Enabled = true;
    }
    private void OnConnected(SteamClient.ConnectedCallback callback)
    {
        LogToConsole("\nConnected to Steam");
    }
    private void OnDisconnected(SteamClient.DisconnectedCallback callback)
    {
        // after recieving an AccountLogonDenied, we'll be disconnected from steam
        // so after we read an authcode from the user, we need to reconnect to begin the logon flow again
        LogToConsole("\nDisconnected from Steam");
    }
#endregion
#region Steam User Functions
    public void Login(string username, string password, bool rememberMe)
    {
        if (username.Length > 0 && password.Length > 0)
        {
            TaskManagerController.RunActionAsync((ct) =>
            {
                return steamInScene.steam3.LoginAsAsync(
                new SteamUser.LogOnDetails()
                {
                    Username = username,
                    Password = password,
                    ShouldRememberPassword = rememberMe,
                });
            });
        }
    }
    public void LoginAsAnon()
    {
        TaskManagerController.RunActionAsync((ct) => { return steam3.LoginAsAnon(); });
    }
    public void LogOff()
    {
        SettingsController.user = "";
        SettingsController.personaState = (int)EPersonaState.Invisible;
        TaskManagerController.RunAction(SettingsController.SaveSettings);

        if (IsLoggedIn)
            steam3.Disconnect(true);
    }

    private TaskWrapper manifestDownloadTask;
    public TaskWrapper GetDownloadManifestTask(Action onManifestDownloaded = null)
    {
        if (manifestDownloadTask == null)
        {
            manifestDownloadTask = GenerateDownloadGOManifestTask(null, onManifestDownloaded);
        }
        return manifestDownloadTask;
    }
    private void OnLoggedOn(SteamUser.LoggedOnCallback callback)
    {
        if (callback.Result == EResult.OK)
        {
            if (steam3.logonDetails != null && steam3.logonDetails.ShouldRememberPassword)
            {
                TaskManagerController.RunAction(() => { SettingsController.RememberUser(steam3.logonDetails.Username); });
            }
            TaskMaker.DownloadManifest(() =>
            {
                TaskManagerController.RunAction(() =>
                {
                    MapsPanel.mapsPanelInScene.RepopulateList();
                    MatchesPanel.matchesInScene.RepopulateList();
                });
            });
        }
        //GrabGOManifestAsync(() => { TaskManagerController.RunAction(MapsPanel.mapsPanelInScene.PopulateList); });
    }
    private void OnLoggedOff(SteamUser.LoggedOffCallback callback)
    {
        LogToConsole("\nLogged off of Steam: " + callback.Result);
    }

    private void OnAccountInfo(SteamUser.AccountInfoCallback callback)
    {
        LogToConsole("\nReceived account info");

        // before being able to interact with friends, you must wait for the account info callback
        // this callback is posted shortly after a successful logon

        // at this point, we can go online on friends, so lets do that
        steamFriends.SetPersonaState((EPersonaState)SettingsController.personaState);
    }
    public void SetPersonaState(int state)
    {
        state = Mathf.Clamp(state, 0, 8);
        SetPersonaState((EPersonaState)state);
    }
    public void SetPersonaState(EPersonaState state)
    {
        SettingsController.personaState = (int)state;
        TaskManagerController.RunAction(SettingsController.SaveSettings);
        steamFriends.SetPersonaState(state);
    }
#endregion
#region CSGO Download
    public ProtoManifest.FileData GetFileInManifest(string filePath)
    {
        ProtoManifest.FileData fileWithName = null;
        foreach (var manifest in csgoManifests)
        {
            fileWithName = manifest.Value.Files.FirstOrDefault(file => Util.UriEquals(file.FileName, filePath.Replace("/", "\\")));
            if (fileWithName != null)
                break;
        }
        return fileWithName;
    }

    public const string CONTENT_DOWNLOAD_TASK_NAME = "DepotDownloaderSteamTask", VPK_DOWNLOAD_TASK_NAME = "VPKDownloadTask";
    public List<ProtoManifest.FileData> GetFilesInManifestWithExtension(string extension)
    {
        List<ProtoManifest.FileData> files = new List<ProtoManifest.FileData>();
        foreach(var manifest in csgoManifests)
            files.AddRange(manifest.Value.Files.Where(file => Path.GetExtension(file.FileName).Equals(extension, StringComparison.OrdinalIgnoreCase)));
        return files;
    }
    public bool ManifestHasPreview(string mapName)
    {
        bool contains = false;
        foreach (var manifest in csgoManifests)
        {
            contains = manifest.Value.Files.Where(file => Util.UriEquals(file.FileName, "csgo\\maps\\" + mapName + ".jpg")).Count() > 0;
            if (contains)
                break;
        }
        return contains;
    }
    public TaskWrapper DownloadFromSteam(string[] files, Action onDownloadStart = null, Action onDownloadComplete = null, string customTaskName = null)
    {
        string taskName = CONTENT_DOWNLOAD_TASK_NAME;
        if (!string.IsNullOrEmpty(customTaskName))
            taskName = customTaskName;

        return TaskManagerController.CreateTask(taskName, (cancelToken) =>
        {
            Task downloadTask = null;
            //if (IsLoggedIn)
            //{
                onDownloadStart?.Invoke();
                downloadTask = ContentDownloader.DownloadApp(steam3, SettingsController.gameLocation, GetCurrentAppId(), cancelToken, false, onDownloadComplete, files);
            //}
            //else
            //{
            //    TaskManagerController.RunAction(() => { ShowErrorPopup("Download Error", "You must be logged in to complete this action"); });
            //}

            return downloadTask;
        });
    }
    public TaskWrapper GenerateMapPreviewDownloadTask(string mapName, Action onPreviewStartDownload = null, Action onPreviewReceived = null)
    {
        return DownloadFromSteam(new string[] { "csgo\\maps\\" + mapName + ".jpg" }, onPreviewStartDownload, onPreviewReceived, "Preview_Download_" + mapName);
    }
    public TaskWrapper GenerateDownloadMapTask(string mapName, Action onMapStartDownload = null, Action onMapReceived = null)
    {
        string[] toBeDownloaded;
        string vpkDirPakPath = "csgo\\pak01_dir.vpk";
        string mapPath = "csgo\\maps\\" + mapName + ".bsp";
        toBeDownloaded = new string[] { mapPath, vpkDirPakPath };

        return DownloadFromSteam(toBeDownloaded, onMapStartDownload, onMapReceived);
    }
    public TaskWrapper GenerateDownloadVPKTask(MapData map, Action onVPKStartDownload = null, Action onVPKReceived = null)
    {
        return TaskManagerController.CreateTask(VPK_DOWNLOAD_TASK_NAME, (cancelToken) =>
        {
            Task downloadTask = null;
            //if (IsLoggedIn)
            //{
                string vpkDirPakPath = "csgo\\pak01_dir.vpk";
                string[] vpkNames = map.dependenciesList;
                Debug.Assert(vpkNames != null && vpkNames.Length > 0, "No dependencies found for " + map.mapName);
                int vpkNamesCount = 0;
                if (vpkNames != null)
                    vpkNamesCount = vpkNames.Length;

                string[] toBeDownloaded = new string[vpkNamesCount + 1];
                for (int i = 0; i < vpkNamesCount; i++)
                    toBeDownloaded[i] = "csgo\\" + vpkNames[i] + ".vpk";
                toBeDownloaded[toBeDownloaded.Length - 1] = vpkDirPakPath;

                onVPKStartDownload?.Invoke();
                downloadTask = ContentDownloader.DownloadApp(steam3, SettingsController.gameLocation, GetCurrentAppId(), cancelToken, false, onVPKReceived, toBeDownloaded);
            //}
            //else
            //{
            //    TaskManagerController.RunAction(() => { ShowErrorPopup("Download Error", "You must be logged in to complete this action"); });
            //}

            return downloadTask;
        });
    }
    public TaskWrapper GenerateMapOverviewDownloadTask(string mapName, Action onOverviewStartDownload = null, Action onOverviewReceived = null)
    {
        string overviewDownLoc = "csgo\\resource\\overviews\\";
        string[] overviewFiles = new string[]
        {
                overviewDownLoc + mapName + ".txt",
                overviewDownLoc + OverviewData.GetOverviewTextureFileName(mapName, OverviewData.RadarLevel.main),
                overviewDownLoc + OverviewData.GetOverviewTextureFileName(mapName, OverviewData.RadarLevel.lower),
                overviewDownLoc + OverviewData.GetOverviewTextureFileName(mapName, OverviewData.RadarLevel.higher)
        };
        return DownloadFromSteam(overviewFiles, onOverviewStartDownload, onOverviewReceived);
    }
    private TaskWrapper GenerateDownloadGOManifestTask(Action onManifestStartDownload = null, Action onManifestDownloaded = null)
    {
        TaskWrapper tw = null;
        //if (IsLoggedIn)
        //{
            tw = TaskManagerController.CreateTask(CONTENT_DOWNLOAD_TASK_NAME, (cancelToken) =>
            {
                isManifestDownloading = true;
                onManifestStartDownload?.Invoke();
                return ContentDownloader.DownloadApp(steam3, SettingsController.gameLocation, GetCurrentAppId(), cancelToken, true, () => { isManifestDownloading = false; onManifestDownloaded?.Invoke(); });
            });
        //}
        //else
        //{
        //    TaskManagerController.RunAction(() => { ShowErrorPopup("Download Error", "Unable to download manifest, user not logged in"); });
        //}
        return tw;
    }
    private void ContentDownloader_onManifestReceived(uint appId, uint depotId, string depotName, ProtoManifest manifest)
    {
        if (appId == APPID || appId == DSID)
        {
            csgoManifests[manifest.ID] = manifest;
            //ulong depotTotalByteSize = 0;
            //int filesPrinted = 0;
            //foreach (var file in manifest.Files)
            //{
            //    depotTotalByteSize += file.TotalSize;
            //if (filesPrinted++ < 10)
            //    LogToConsole("File in manifest " + filesPrinted + ": " + file.FileName);
            //}

            LogToConsole("Received depot manifest (" + depotId + " " + depotName + ") containing " + manifest.Files.Count + " item(s)");
            //LogToConsole("Received depot manifest (" + depotId + " " + depotName + ") containing " + manifest.Files.Count + " item(s) with a total size of " + depotTotalByteSize + " byte(s)");
        }
    }
#endregion
#region Web API
    public void GetRecentMatches()
    {
        //steamWebUser.GetPlayerSummaryAsync(123);
    }
#endregion
#region Steam Friends Functions
    public void SendTypingTo(SteamID friendId)
    {
        LogToConsole("\nSending typing to " + GetFriendDisplayName(friendId) + "...");
        steamFriends.SendChatMessage(friendId, EChatEntryType.Typing, "");
    }
    public void SendChatMessageTo(SteamID friendId, string message)
    {
        LogToConsole("\nSending chat message to " + GetFriendDisplayName(friendId) + "...");
        steamFriends.SendChatMessage(friendId, EChatEntryType.ChatMsg, message);
    }
    public void RequestFriendChatHistory(SteamID friendId)
    {
        LogToConsole("\nRequesting chat history with " + GetFriendDisplayName(friendId) + "...");
        steamFriends.RequestMessageHistory(friendId);
    }
    public void RequestOfflineChatMessages()
    {
        LogToConsole("\nRequesting offline messages...");
        steamFriends.RequestOfflineMessages();
    }

    public void RequestProfileInfo(SteamID steamId, Action<SteamFriends.ProfileInfoCallback> profileInfoActions)
    {
        TaskManagerController.RunActionAsync(async (cts) => { var profileInfo = await steamFriends.RequestProfileInfo(steamId); profileInfoActions?.Invoke(profileInfo); });
        //steamFriends.RequestProfileInfo(steamId);
    }
    private void OnProfileInfoReceived(SteamFriends.ProfileInfoCallback callback)
    {
        LogToConsole("\n---Profile Info Received---" +
        "\nSteamId: " + callback.SteamID +
        "\nRealName: " + callback.RealName);
    }
    private void OnFriendAdded(SteamFriends.FriendAddedCallback callback)
    {
        // someone accepted our friend request, or we accepted one
        LogToConsole("\n" + callback.PersonaName + " is now a friend");
    }
    private void OnPersonaState(SteamFriends.PersonaStateCallback callback)
    {
        TaskManagerController.RunAction(() =>
        {
            // this callback is received when the persona state (friend information) of a friend changes

            // for this sample we'll simply display the names of the friends
            LogToConsole("\n" + callback.Name + "(" + callback.FriendID.AccountID + ", " + callback.FriendID.ConvertToUInt64() + ") is now " + callback.State + ". Game(" + callback.GameName + ", " + callback.GameID + ", " + callback.GameAppID + ") StateFlags(" + callback.StateFlags + ") StatusFlags(" + callback.StatusFlags + ")");

            if (callback.FriendID.IsIndividualAccount)
            {
                SteamUserData currentUser = GetFriendWithAccountId(callback.FriendID);
                if (currentUser == null)
                {
                    currentUser = new SteamUserData(callback.FriendID.AccountID, callback.FriendID.ConvertToUInt64());
                    friendData.Add(currentUser);
                }
                Download(GetAvatarURL(callback.AvatarHash), currentUser.SetAvatar);
                currentUser.SetDisplayName(callback.Name);
                currentUser.SetAppId(callback.GameAppID);
                currentUser.SetState(callback.State);
                currentUser.SetClanRank(callback.ClanRank);
                currentUser.SetClanTag(callback.ClanTag);
                currentUser.SetLastLogOn(callback.LastLogOn);
                currentUser.SetLastLogOff(callback.LastLogOff);
                currentUser.SetOnlineSessionInstances(callback.OnlineSessionInstances);
                currentUser.SetPublishedSessionId(callback.PublishedSessionID);
                currentUser.SetRelationship(steamFriends.GetFriendRelationship(callback.FriendID));

                onUserStateChanged?.Invoke(currentUser);
            }
        });
    }
    private void OnPlayingSessionStateReceived(SteamFriends.ClientPlayingSessionStateCallback callback)
    {
        playingBlocked = callback.PlayingBlocked;
        playingApp = callback.PlayingApp;

        LogToConsole("\nPlaying session state received" +
            "\nPlaying Blocked: " + playingBlocked +
            "\nPlaying App: " + playingApp);
    }
    private void OnFriendsList(SteamFriends.FriendsListCallback callback)
    {
        // at this point, the client has received it's friends list

        int friendCount = steamFriends.GetFriendCount();
        LogToConsole("\nFound " + friendCount + " friends");
    }

    private void OnInviteReceived(SteamFriends.ChatInviteCallback callback)
    {
        LogToConsole("\n----Game Invite Received----" +
            "\nInviterName: " + GetFriendDisplayName(callback.PatronID.AccountID) +
            "\nInviterId: " + callback.PatronID.AccountID +
            "\nChatFriendId: " + callback.FriendChatID.AccountID +
            "\nGameAppId: " + callback.GameID.AppID);
    }
    private void OnChatMessageReceived(SteamFriends.ChatMsgCallback callback)
    {
        LogToConsole("\n----Chat Msg Received----" +
            "\nChatterName: " + GetFriendDisplayName(callback.ChatterID.AccountID) +
            "\nChatterId: " + callback.ChatterID.AccountID +
            "\nChatroomId: " + callback.ChatRoomID.AccountID +
            "\nChatEntryType: " + callback.ChatMsgType +
            "\nMessage: " + callback.Message);
    }
    private void OnChatEntered(SteamFriends.ChatEnterCallback callback)
    {
        LogToConsole("\n----Chat Entered----" +
            "\nChatFlags: " + callback.ChatFlags +
            "\nChatroomId: " + callback.ChatID.AccountID + 
            "\nChatroomOwnerName: " + GetFriendDisplayName(callback.OwnerID.AccountID) +
            "\nChatroomOwnerId: " + callback.OwnerID.AccountID);
    }
    private void OnChatMemberInfoReceived(SteamFriends.ChatMemberInfoCallback callback)
    {
        LogToConsole("\n----Chat Member Info Received----");
    }
    private void OnChatActionResultReceived(SteamFriends.ChatActionResultCallback callback)
    {
        LogToConsole("\n----Chat Action Result Received----");
    }
    private void OnFriendMessageHistoryReceived(SteamFriends.FriendMsgHistoryCallback callback)
    {
        LogToConsole("\n----Friend Msg History Received----" +
            "\nResult: " + callback.Result +
            "\nChatPartnerName: " + GetFriendDisplayName(callback.SteamID.AccountID) +
            "\nChatPartnerId: " + callback.SteamID.AccountID +
            "\nMessages: " + (callback.Messages != null ? callback.Messages.Count.ToString() : "0"));

        if (callback.Result != EResult.Fail)
            onChatHistoryReceived?.Invoke(callback.SteamID.AccountID, callback.Messages);
    }
    private void OnFriendMessageReceived(SteamFriends.FriendMsgCallback callback)
    {
        LogToConsole("\n----Friend Msg Received----" +
            "\nSenderName: " + GetFriendDisplayName(callback.Sender.AccountID) +
            "\nSenderId: " + callback.Sender.AccountID +
            "\nFromLimitedAccount: " + callback.FromLimitedAccount +
            "\nChatEntryType: " + callback.EntryType +
            "\nMessage: " + callback.Message);

        onFriendChatReceived?.Invoke(callback.Sender.AccountID, callback.EntryType, callback.Message);
    }
    private void OnFriendMessageEchoReceived(SteamFriends.FriendMsgEchoCallback callback)
    {
        LogToConsole("\n----Friend Msg Echo Received----" +
            "\nRecipientName: " + GetFriendDisplayName(callback.Recipient.AccountID) +
            "\nRecipientId: " + callback.Recipient.AccountID +
            "\nFromLimitedAccount: " + callback.FromLimitedAccount +
            "\nChatEntryType: " + callback.EntryType +
            "\nMessage: " + callback.Message);
    }
#endregion
#region GameCoordinator Functions
    public void PlayCStrike()
    {
        LogToConsole("\nStarting CS:GO");
        if (!playingBlocked)
        {
            // now we need to inform the steam server that we're playing csgo (in order to receive GC messages)

            // steamkit doesn't expose the "play game" message through any handler, so we'll just send the message manually
            ClientMsgProtobuf<CMsgClientGamesPlayed> playGame = new ClientMsgProtobuf<CMsgClientGamesPlayed>(EMsg.ClientGamesPlayedWithDataBlob);

            playGame.Body.games_played.Add(new CMsgClientGamesPlayed.GamePlayed
            {
                game_id = APPID, // or game_id = APPID,
            });
            //playGame.Body.client_os_type = 16;

            // send it off
            // notice here we're sending this message directly using the SteamClient
            steamClient.Send(playGame);

            // delay a little to give steam some time to establish a GC connection to us
            StartCoroutine(CommonRoutines.WaitToDoAction((success) => { SendGCGreeting(); }));
        }
        else
            LogToConsole("Already in " + playingApp + ". Please quit " + playingApp + " before trying again.");
    }
    public void QuitGame()
    {
        if (playingApp > 0)
        {
            ClientMsgProtobuf<CMsgClientGamesPlayed> endGame = new ClientMsgProtobuf<CMsgClientGamesPlayed>(EMsg.ClientGamesPlayedWithDataBlob);

            steamClient.Send(endGame);
        }
    }
    private void SendGCGreeting()
    {
        // inform the dota GC that we want a session
        ClientGCMsgProtobuf<CMsgClientHello> clientHello = new ClientGCMsgProtobuf<CMsgClientHello>((uint)EGCBaseClientMsg.k_EMsgGCClientHello);
        clientHello.Body.partner_accountid = steamUser.SteamID.AccountID;
        clientHello.Body.partner_accountidSpecified = true;
        clientHello.Body.version = 900;
        //clientHello.Body.engine = ESourceEngine.k_ESE_Source2;
        gameCoordinator.Send(clientHello, APPID);
    }
    // called when a gamecoordinator (GC) message arrives
    // these kinds of messages are designed to be game-specific
    // in this case, we'll be handling csgo's GC messages
    private void OnGCMessage(SteamGameCoordinator.MessageCallback callback)
    {
        LogToConsole("\nGC message received: " + callback.EMsg);

        // setup our dispatch table for messages
        // this makes the code cleaner and easier to maintain
        Dictionary<uint, Action<IPacketGCMsg>> messageMap = new Dictionary<uint, Action<IPacketGCMsg>>
        {
            { (uint)EGCBaseClientMsg.k_EMsgGCClientWelcome, OnClientWelcome },
            { (uint)ECsgoGCMsg.k_EMsgGCCStrike15_v2_MatchList, OnMatchListReceived },
            { (uint)ECsgoGCMsg.k_EMsgGCCStrike15_v2_PlayersProfile, OnPlayerProfileReceived },
            { (uint)ECsgoGCMsg.k_EMsgGCCStrike15_v2_MatchmakingGC2ClientHello, OnGCHello },
            { (uint)ECsgoGCMsg.k_EMsgGCCStrike15_v2_ClientGCRankUpdate, OnRankUpdate },
            { (uint)ECsgoGCMsg.k_EMsgGCCStrike15_v2_WatchInfoUsers, OnWatchInfoReceived },
            { (uint)ECsgoGCMsg.k_EMsgGCCStrike15_v2_GCToClientSteamdatagramTicket, ClientSteamdatagramTicket },
            { (uint)ECsgoGCMsg.k_EMsgGCCStrike15_v2_MatchmakingGC2ClientReserve, MatchMakingGC2ClientReserve },
            { (uint)ECsgoGCMsg.k_EMsgGCCStrike15_v2_MatchmakingGC2ServerReserve, MatchMakingGC2ServerReserve },
            { (uint)ECsgoGCMsg.k_EMsgGCCStrike15_v2_MatchmakingServerReservationResponse, MatchmakingServerReservationResponse },
            { (uint)ECsgoGCMsg.k_EMsgGCCStrike15_v2_GC2ServerReservationUpdate, ServerReservationUpdate },
            { (uint)ECsgoGCMsg.k_EMsgGCCStrike15_v2_MatchListRequestFullGameInfo, FullGameInfoReceived },
        };

        Action<IPacketGCMsg> func;
        if (!messageMap.TryGetValue(callback.EMsg, out func))
        {
            DebugViewController.Log("Unknown message");
            // this will happen when we recieve some GC messages that we're not handling
            // this is okay because we're handling every essential message, and the rest can be ignored
            return;
        }

        func(callback.Message);
    }
    // this message arrives when the GC welcomes a client
    // this happens after telling steam that we launched csgo (with the ClientGamesPlayed message)
    // this can also happen after the GC has restarted (due to a crash or new version)
    private void OnClientWelcome(IPacketGCMsg packetMsg)
    {
        // in order to get at the contents of the message, we need to create a ClientGCMsgProtobuf from the packet message we recieve
        // note here the difference between ClientGCMsgProtobuf and the ClientMsgProtobuf used when sending ClientGamesPlayed
        // this message is used for the GC, while the other is used for general steam messages
        ClientGCMsgProtobuf<CMsgClientWelcome> msg = new ClientGCMsgProtobuf<CMsgClientWelcome>(packetMsg);

        LogToConsole("\nGC sends welcome. Version(" + (msg.Body.versionSpecified ? "specified" : "unspecified") + "): " + msg.Body.version);

        // at this point, the GC is now ready to accept messages from us
    }
    private void OnGCHello(IPacketGCMsg packetMsg)
    {
        LogToConsole("\nReceived hello from GC");

        ClientGCMsgProtobuf<CMsgGCCStrike15_v2_MatchmakingGC2ClientHello> msg = new ClientGCMsgProtobuf<CMsgGCCStrike15_v2_MatchmakingGC2ClientHello>(packetMsg);
    }
    private void OnRankUpdate(IPacketGCMsg packetMsg)
    {
        LogToConsole("\nReceived rank update");

        ClientGCMsgProtobuf<CMsgGCCStrike15_v2_ClientGCRankUpdate> msg = new ClientGCMsgProtobuf<CMsgGCCStrike15_v2_ClientGCRankUpdate>(packetMsg);
    }

    private float lastRecentMatchesRequest;
    private EPersonaState userLastState;
    private float recentMatchesRequestCooldown = 5;
    private bool blockRefresh;
    public void SendRequestFullGame(string sharecode)
    {
        Debug.Log("Deciphering " + sharecode);
        sharecode = MatchInfo.ExtractSharecode(sharecode);
        LogToConsole("\nGetting match from sharecode " + sharecode);

        //if (IsLoggedIn && !steam3.isAnon)
        //{
            if (MatchInfo.CheckShareCode(sharecode))
            {
                var matchSignature = MatchInfo.DecodeShareCode(sharecode);
                SendRequestFullGame(matchSignature);
            }
            else
                ShowErrorPopup("Bad Format", "The given sharecode format is incorrect.");
        //}
        //else
        //    ShowErrorPopup("Not Logged In", "You need to be logged in to Steam due to the way sharecodes work.");
    }
    public void SendRequestFullGame(MatchSignature matchSignature)
    {
        //var matchSig = MatchInfo.DecodeShareCode("CSGO-WMw4F-5EYMy-7Hk79-JhRrm-tNTWJ");
        //RequestFullGameInfo(matchSig.matchId, matchSig.outcomeId, matchSig.token);
        if (waitForLoginToGetMatchInfoRoutine == null)
        {
            waitForLoginToGetMatchInfoRoutine = StartCoroutine(CommonRoutines.WaitToDoAction((success) =>
            {
                waitForLoginToGetMatchInfoRoutine = null;
                HideLoadingPopup();
                if (IsLoggedIn && !steam3.isAnon)
                {
                    LogToConsole("\nRequesting full game info");
                    //blockRefresh = true;

                    var account = GetFriendWithAccountId(userAccountId);
                    userLastState = account != null ? account.GetState() : (EPersonaState)SettingsController.personaState;

                    SetPersonaState(EPersonaState.Invisible);
                    PlayCStrike();
                    StartCoroutine(CommonRoutines.WaitToDoAction((innerSuccess) =>
                    {
                        if (innerSuccess)
                            RequestFullGameInfo(matchSignature.matchId, matchSignature.outcomeId, matchSignature.token);
                        //lastRecentMatchesRequest = Time.time;
                        //blockRefresh = false;
                    }, 30, () => playingApp == 730));
                }
                else
                    ShowErrorPopup("Not Logged In", "You need to be logged in to Steam due to the way sharecodes work.");
            }, -1, () => { return (!steam3.isLoggingIn && IsLoggedIn && !isManifestDownloading) || (!steam3.isLoggingIn && !IsLoggedIn); }));

            if (steam3.isLoggingIn || isManifestDownloading)
                ShowLoadingPopup("Waiting for log in", () =>
                {
                    StopCoroutine(waitForLoginToGetMatchInfoRoutine);
                    waitForLoginToGetMatchInfoRoutine = null;
                });
        }
        else
            Debug.LogError("Already waiting to retrieve match");
    }
    public void RequestRecentMatches()
    {
        if (IsLoggedIn && !steam3.isAnon && !blockRefresh && Time.time - lastRecentMatchesRequest > recentMatchesRequestCooldown)
        {
            LogToConsole("\nRequesting recent matches");
            blockRefresh = true;

            var account = GetFriendWithAccountId(userAccountId);
            userLastState = account != null ? account.GetState() : (EPersonaState)SettingsController.personaState;

            SetPersonaState(EPersonaState.Invisible);
            PlayCStrike();
            StartCoroutine(CommonRoutines.WaitToDoAction((success) =>
            {
                if (success)
                    RequestRecentGamesPlayed(userAccountId);

                lastRecentMatchesRequest = Time.time;
                blockRefresh = false;
            }, 30, () => { return playingApp == 730; }));
        }
    }

    //public void SendRequestFullGame()
    //{
    //    var matchSig = MatchInfo.DecodeShareCode("CSGO-WMw4F-5EYMy-7Hk79-JhRrm-tNTWJ");
    //    RequestFullGameInfo(matchSig.matchId, matchSig.outcomeId, matchSig.token);
    //    var addToLibrary = new ClientGCMsgProtobuf<CMsgClientPurchaseWithMachineID>(1);
    //CMsgClientRequestFreeLicense
    //CMsgClientSiteLicenseCheckout
    //}
    public void RequestFullGameInfo(ulong matchId, ulong outcomeId, uint token)
    {
        LogToConsole("\nRequesting full game info...");

        ClientGCMsgProtobuf<CMsgGCCStrike15_v2_MatchListRequestFullGameInfo> requestFullGame = new ClientGCMsgProtobuf<CMsgGCCStrike15_v2_MatchListRequestFullGameInfo>((uint)ECsgoGCMsg.k_EMsgGCCStrike15_v2_MatchListRequestFullGameInfo);
        requestFullGame.Body.matchid = matchId;
        //requestFullGame.Body.matchidSpecified = matchIdSpec;
        requestFullGame.Body.outcomeid = outcomeId;
        //requestFullGame.Body.outcomeidSpecified = outcomeIdSpec;
        requestFullGame.Body.token = token;
        //requestFullGame.Body.tokenSpecified = tokenSpec;
        gameCoordinator.Send(requestFullGame, APPID);
    }
    public void RequestRecentGamesPlayed(uint accountId)
    {
        LogToConsole("\nRequesting recent games played...");

        ClientGCMsgProtobuf<CMsgGCCStrike15_v2_MatchListRequestRecentUserGames> requestRecentGames = new ClientGCMsgProtobuf<CMsgGCCStrike15_v2_MatchListRequestRecentUserGames>((uint)ECsgoGCMsg.k_EMsgGCCStrike15_v2_MatchListRequestRecentUserGames);
        requestRecentGames.Body.accountid = accountId;

        gameCoordinator.Send(requestRecentGames, APPID);
    }
    public void RequestPlayerProfile(uint accountId)
    {
        LogToConsole("\nRequesting player profile...");

        ClientGCMsgProtobuf<CMsgGCCStrike15_v2_ClientRequestPlayersProfile> playerProfile = new ClientGCMsgProtobuf<CMsgGCCStrike15_v2_ClientRequestPlayersProfile>((uint)ECsgoGCMsg.k_EMsgGCCStrike15_v2_ClientRequestPlayersProfile);
        playerProfile.Body.account_id = accountId;
        playerProfile.Body.account_idSpecified = true;

        gameCoordinator.Send(playerProfile, APPID);
    }
    public void RequestJoinFriendData(uint friendAccountId)
    {
        LogToConsole("\nRequesting join friend data...");

        ClientGCMsgProtobuf<CMsgGCCStrike15_v2_ClientRequestJoinFriendData> friendWatchDataRequest = new ClientGCMsgProtobuf<CMsgGCCStrike15_v2_ClientRequestJoinFriendData>((uint)ECsgoGCMsg.k_EMsgGCCStrike15_v2_ClientRequestWatchInfoFriends2);
        friendWatchDataRequest.Body.account_id = friendAccountId;
        friendWatchDataRequest.Body.account_idSpecified = true;
        gameCoordinator.Send(friendWatchDataRequest, APPID);
    }
    public void RequestJoinServerData(uint accountId, bool accountIdSpec, ulong serverId, bool serverIdSpec, uint serverIp, bool serverIpSpec, uint serverPort, bool serverPortSpec)
    {
        LogToConsole("\nRequesting join server data...");

        ClientGCMsgProtobuf<CMsgGCCStrike15_v2_ClientRequestJoinServerData> serverJoinDataRequest = new ClientGCMsgProtobuf<CMsgGCCStrike15_v2_ClientRequestJoinServerData>((uint)ECsgoGCMsg.k_EMsgGCCStrike15_v2_ClientRequestJoinServerData);
        serverJoinDataRequest.Body.account_id = accountId;
        serverJoinDataRequest.Body.account_idSpecified = accountIdSpec;
        serverJoinDataRequest.Body.serverid = serverId;
        serverJoinDataRequest.Body.serveridSpecified = serverIdSpec;
        serverJoinDataRequest.Body.server_ip = serverIp;
        serverJoinDataRequest.Body.server_ipSpecified = serverIpSpec;
        serverJoinDataRequest.Body.server_port = serverPort;
        serverJoinDataRequest.Body.server_portSpecified = serverPortSpec;
        gameCoordinator.Send(serverJoinDataRequest, APPID);
    }
    public void RequestLiveGames()
    {
        LogToConsole("\nRequesting live games...");

        ClientGCMsgProtobuf<CMsgGCCStrike15_v2_MatchListRequestCurrentLiveGames> requestLiveGames = new ClientGCMsgProtobuf<CMsgGCCStrike15_v2_MatchListRequestCurrentLiveGames>((uint)ECsgoGCMsg.k_EMsgGCCStrike15_v2_MatchListRequestCurrentLiveGames);
        gameCoordinator.Send(requestLiveGames, APPID);
    }
    public uint RequestWatchInfo(ulong matchId, ulong serverId)
    {
        LogToConsole("\nRequesting watch info...");

        uint requestId = ++requestsMade;

        ClientGCMsgProtobuf<CMsgGCCStrike15_v2_ClientRequestWatchInfoFriends> requestWatchInfo = new ClientGCMsgProtobuf<CMsgGCCStrike15_v2_ClientRequestWatchInfoFriends>((uint)ECsgoGCMsg.k_EMsgGCCStrike15_v2_ClientRequestWatchInfoFriends2);
        requestWatchInfo.Body.request_id = requestId;
        requestWatchInfo.Body.request_idSpecified = true;
        requestWatchInfo.Body.serverid = serverId;
        requestWatchInfo.Body.serveridSpecified = true;
        requestWatchInfo.Body.matchid = matchId;
        requestWatchInfo.Body.matchidSpecified = true;
        //requestWatchInfo.Body.data_center_pings
        gameCoordinator.Send(requestWatchInfo, APPID);

        return requestId;
    }
    private void OnWatchInfoReceived(IPacketGCMsg packetMsg)
    {
        ClientGCMsgProtobuf<CMsgGCCStrike15_v2_WatchInfoUsers> msg = new ClientGCMsgProtobuf<CMsgGCCStrike15_v2_WatchInfoUsers>(packetMsg);
        List<uint> accountIds = msg.Body.account_ids;
        List<WatchableMatchInfo> watchableMatchInfos = msg.Body.watchable_match_infos;
        LogToConsole(
            "\nWatch info received:" +
            "\nAcountIdCount(" + accountIds.Count +
            ")\nExtendedTime(" + (msg.Body.extended_timeoutSpecified ? msg.Body.extended_timeout.ToString() : "?") +
            ")\nRequestId(" + (msg.Body.request_idSpecified ? msg.Body.request_id.ToString() : "?") +
            ")\nMatchInfoCount(" + watchableMatchInfos.Count + ")");
        for (int i = 0; i < watchableMatchInfos.Count; i++)
        {
            WatchableMatchInfo watchable = watchableMatchInfos[i];
            LogToConsole("WatchInfo(" + i + "):\n" + WatchableMatchInfoToString(watchable));
        }
    }
    private void FullGameInfoReceived(IPacketGCMsg packetMsg)
    {
        var msg = new ClientGCMsgProtobuf<CMsgGCCStrike15_v2_MatchListRequestFullGameInfo>(packetMsg);
        LogToConsole("\nFull game info received");
    }
    private void ClientSteamdatagramTicket(IPacketGCMsg packetMsg)
    {
        ClientGCMsgProtobuf<CMsgGCToClientSteamDatagramTicket> msg = new ClientGCMsgProtobuf<CMsgGCToClientSteamDatagramTicket>(packetMsg);
        LogToConsole(
            "\nSteam Datagram Received:" +
            "\nAppId(" + (msg.Body.legacy_app_idSpecified ? msg.Body.legacy_app_id.ToString() : "?") +
            ")\nAuthPubIP(" + (msg.Body.legacy_authorized_public_ipSpecified ? msg.Body.legacy_authorized_public_ip.ToString() : "?") +
            ")\nAuthSteamID(" + (msg.Body.legacy_authorized_steam_idSpecified ? msg.Body.legacy_authorized_steam_id.ToString() : "?") +
            ")\nGameServerNetID(" + (msg.Body.legacy_gameserver_net_idSpecified ? msg.Body.legacy_gameserver_net_id.ToString() : "?") +
            ")\nGameServerSteamID(" + (msg.Body.legacy_gameserver_steam_idSpecified ? msg.Body.legacy_gameserver_steam_id.ToString() : "?") +
            ")\nSignature(" + (msg.Body.legacy_signatureSpecified ? msg.Body.legacy_signature.ToString() : "?") +
            ")\nSerializedTicket(" + (msg.Body.serialized_ticketSpecified ? msg.Body.serialized_ticket.ToString() : "?") +
            ")");
    }
    private void OnMatchListReceived(IPacketGCMsg packetMsg)
    {
        QuitGame();
        SetPersonaState(userLastState);

        ClientGCMsgProtobuf<CMsgGCCStrike15_v2_MatchList> msg = new ClientGCMsgProtobuf<CMsgGCCStrike15_v2_MatchList>(packetMsg);

        LogToConsole("\nReceived match list" +
            "\nMatches found: " + msg.Body.matches.Count +
            "\nServer Time: " + msg.Body.servertime +
            "\nStreams: " + (msg.Body.streams != null ? msg.Body.streams.Count.ToString() : "nil") +
            "\nTournament Info: " + (msg.Body.tournamentinfo != null ? "available" : "nil"));

        //if (msg.Body.msgrequestid == (uint)ECsgoGCMsg.k_EMsgGCCStrike15_v2_MatchListRequestRecentUserGames)
        onRecentGamesReceived?.Invoke(msg.Body);
    }
    private void OnPlayerProfileReceived(IPacketGCMsg packetMsg)
    {
        LogToConsole("\nReceived player profile:");

        ClientGCMsgProtobuf<CMsgGCCStrike15_v2_PlayersProfile> msg = new ClientGCMsgProtobuf<CMsgGCCStrike15_v2_PlayersProfile>(packetMsg);
        foreach (CMsgGCCStrike15_v2_MatchmakingGC2ClientHello playerProfile in msg.Body.account_profiles)
            LogToConsole(
                "Level(" + playerProfile.player_level + 
                ")\nWins(" + (playerProfile.ranking != null ? playerProfile.ranking.wins.ToString() : "?") +
                ")\nRankId(" + (playerProfile.ranking != null ? playerProfile.ranking.rank_id.ToString() : "?") +
                ")\nXP(" + playerProfile.player_cur_xp +
                ")");
    }
    private void MatchMakingGC2ClientReserve(IPacketGCMsg packetMsg)
    {
        LogToConsole("\nReceived MMClientReserve");

        ClientGCMsgProtobuf<CMsgGCCStrike15_v2_MatchmakingGC2ClientReserve> msg = new ClientGCMsgProtobuf<CMsgGCCStrike15_v2_MatchmakingGC2ClientReserve>(packetMsg);
        LogToConsole(
            "Map(" + (msg.Body.mapSpecified ? msg.Body.map : "") +
            ") GameType(" + (msg.Body.reservation.game_typeSpecified ? msg.Body.reservation.game_type.ToString() : "") +
            ") ReservationId(" + (msg.Body.reservationidSpecified ? msg.Body.reservationid.ToString() : "") + ")");
    }
    private void MatchMakingGC2ServerReserve(IPacketGCMsg packetMsg)
    {
        LogToConsole("\nReceived MMServerReserve");

        ClientGCMsgProtobuf<CMsgGCCStrike15_v2_MatchmakingGC2ServerReserve> msg = new ClientGCMsgProtobuf<CMsgGCCStrike15_v2_MatchmakingGC2ServerReserve>(packetMsg);
        LogToConsole(
            "GameType(" + (msg.Body.game_typeSpecified ? msg.Body.game_type.ToString() : "") + ")");
    }
    private void MatchmakingServerReservationResponse(IPacketGCMsg packetMsg)
    {
        LogToConsole("\nReceived MMServerReservationResponse");

        ClientGCMsgProtobuf<CMsgGCCStrike15_v2_MatchmakingServerReservationResponse> msg = new ClientGCMsgProtobuf<CMsgGCCStrike15_v2_MatchmakingServerReservationResponse>(packetMsg);
        LogToConsole(
            "Map(" + (msg.Body.mapSpecified ? msg.Body.map : "") +
            ") GameType(" + (msg.Body.reservation.game_typeSpecified ? msg.Body.reservation.game_type.ToString() : "") +
            ") ReservationId(" + (msg.Body.reservationidSpecified ? msg.Body.reservationid.ToString() : "") + ")");
    }
    private void ServerReservationUpdate(IPacketGCMsg packetMsg)
    {
        LogToConsole("\nReceived ServerReservationUpdate");

        ClientGCMsgProtobuf<CMsgGCCStrike15_v2_GC2ServerReservationUpdate> msg = new ClientGCMsgProtobuf<CMsgGCCStrike15_v2_GC2ServerReservationUpdate>(packetMsg);
        LogToConsole(
            "ExternalViewersSteam(" + (msg.Body.viewers_external_steamSpecified ? msg.Body.viewers_external_steam.ToString() : "") +
            ") ViewersExternalTotal(" + (msg.Body.viewers_external_totalSpecified ? msg.Body.viewers_external_total.ToString() : "") + ")");
    }
#endregion
#region Steam Apps Functions
    /*public void RequestProductInfo()
    {
        LogToConsole("\nRequesting product info");
        steamApps.PICSGetProductInfo(APPID, null, false);
    }
    private void ProductInfoResponse(SteamApps.PICSProductInfoCallback callback)
    {
        LogToConsole("\n---Received Product Info" + (callback.Apps != null && callback.Apps.ContainsKey(APPID) ?
            "\nAppId: " + callback.Apps[APPID].ID +
            "\nChangeNum: " + callback.Apps[APPID].ChangeNumber +
            "\nMissingToken: " + callback.Apps[APPID].MissingToken +
            "\nShaBytes: " + (callback.Apps[APPID].SHAHash != null ? callback.Apps[APPID].SHAHash.Length.ToString() : "nil") +
            "\nOnlyPublic: " + callback.Apps[APPID].OnlyPublic +
            "\nUseHttp: " + callback.Apps[APPID].UseHttp : ""));
    }
    public void GetAppOwnershipTicket()
    {
        LogToConsole("\nGetting ownership ticket");
        steamApps.GetAppOwnershipTicket(APPID);
    }
    private void AppOwnershipResponse(SteamApps.AppOwnershipTicketCallback callback)
    {
        LogToConsole("\n---Received Ownership Ticket" +
            "\nAppId: " + callback.AppID +
            "\nResult: " + callback.Result +
            "\nTicketBytes: " + (callback.Ticket != null ? callback.Ticket.Length.ToString() : "nil"));
    }
    public void GetDepotDecryptionkey()
    {
        LogToConsole("\nGetting depot decryption key");
        steamApps.GetDepotDecryptionKey(APPID + 1, APPID);
    }
    private void DepotDecryptionKeyResponse(SteamApps.DepotKeyCallback callback)
    {
        LogToConsole("\n---Received Depot Decryption Key" +
            "\nDepotId: " + callback.DepotID +
            "\nDepotKeyBytes: " + (callback.DepotKey != null ? callback.DepotKey.Length.ToString() : "nil"));
    }
    public void GetCDNAuthToken()
    {
        LogToConsole("\nGetting CDN auth token");
        steamApps.GetCDNAuthToken(APPID, APPID + 1, "steampipe.steamcontent.com");
    }
    private void CDNAuthTokenResponse(SteamApps.CDNAuthTokenCallback callback)
    {
        LogToConsole("\n---Received CDN Auth Token" +
            "\nResult: " + callback.Result +
            "\nToken: " + callback.Token +
            "\nExpireTime: " + callback.Expiration);
    }*/
    private void FreeLicenseResponse(SteamApps.FreeLicenseCallback callback)
    {
        LogToConsole("\nFree License Result: " + callback.Result);
    }
#endregion
#region Helper Functions
    public static TaskWrapper GenerateDownloadTask(string url, bool decompress = false, string saveTo = null, Action<bool, byte[]> dataAction = null, Action<int> progressAction = null, Action<Exception> errorAction = null)
    {
        return TaskManagerController.CreateTask(async (cts) =>
        {
            cts.ThrowIfCancellationRequested();
            DownloadProgressChangedEventHandler downloadProgressChangedAction = (sender, downloadProgressChangedEventArgs) =>
            {
                progressAction?.Invoke(downloadProgressChangedEventArgs.ProgressPercentage);
            };
            using (WebClient client = new WebClient())
            {
                client.DownloadProgressChanged += downloadProgressChangedAction;
                try
                {
                    byte[] data = await client.DownloadDataTaskAsync(url);
                    if (decompress)
                        data = await Unzip(data);
                    dataAction?.Invoke(true, data);
                    if (!string.IsNullOrEmpty(saveTo))
                        File.WriteAllBytes(saveTo, data);
                }
                catch (Exception e)
                {
                    LogToConsole("SteamControllerDownload: " + e.ToString());
                    errorAction?.Invoke(e);
                    dataAction?.Invoke(false, null);
                }
                finally
                {
                    client.DownloadProgressChanged -= downloadProgressChangedAction;
                }
            }
        });
    }
    public static async Task<byte[]> Unzip(byte[] data)
    {
        byte[] decompressedData;
        using (var contentStream = new MemoryStream(data))
        using (var bzstream = new Ionic.BZip2.BZip2InputStream(contentStream))
        using (var decompressedStream = new MemoryStream())
        {
            await bzstream.CopyToAsync(decompressedStream);
            decompressedData = decompressedStream.ToArray();
        }

        return decompressedData;
    }
    public static void Download(string url, Action<byte[]> dataAction, float timeout = 30, Action<int> progressAction = null)
    {
        byte[] data = null;
        bool isFinished = false;
        float lastChange = Time.time;
        using (WebClient client = new WebClient())
        {
            DownloadDataCompletedEventHandler downloadCompleteAction = null;
            DownloadProgressChangedEventHandler downloadProgressChangedAction = (sender, downloadProgressChangedEventArgs) => { lastChange = Time.time; progressAction?.Invoke(downloadProgressChangedEventArgs.ProgressPercentage); };
            downloadCompleteAction = (sender, downloadDataCompletedEventArgs) =>
            {
                client.DownloadDataCompleted -= downloadCompleteAction;
                client.DownloadProgressChanged -= downloadProgressChangedAction;

                data = downloadDataCompletedEventArgs.Result;
                isFinished = true;
            };

            client.DownloadDataCompleted += downloadCompleteAction;
            client.DownloadProgressChanged += downloadProgressChangedAction;
            client.DownloadDataAsync(new Uri(url));
        }

        steamInScene.StartCoroutine(CommonRoutines.WaitToDoAction((success) =>
        {
            if (success)
                dataAction(data);
        }, -1, () =>
        {
            return isFinished || (Time.time - lastChange > timeout);
        }));
    }
    public static void Download(string url, string filepath, float timeout = 30, Action<int> progressAction = null)
    {
        float lastChange = Time.time;
        using (WebClient client = new WebClient())
        {
            client.DownloadProgressChanged += (sender, downloadProgressChangedEventArgs) => { progressAction?.Invoke(downloadProgressChangedEventArgs.ProgressPercentage); };
            client.DownloadFileAsync(new Uri(url), filepath);
        }
    }

    private static string GetAvatarURL(byte[] avatarHashBytes)
    {
        string avatarHash = null;

        string avatarURL;
        string steamAvatarBaseURL = "https://steamcdn-a.akamaihd.net/steamcommunity/public/images/avatars/";

#region Getting hash as string
        if (avatarHashBytes != null && avatarHashBytes.Length > 0 && avatarHashBytes.Any(singleByte => singleByte != 0))
        {
            avatarHash = BitConverter.ToString(avatarHashBytes).Replace("-", "").ToLowerInvariant();

            if (string.IsNullOrEmpty(avatarHash) || avatarHash.All(singleChar => singleChar == '0'))
            {
                avatarHash = null;
            }
        }
#endregion

#region Making URL
        if (avatarHash != null)
        {
            string folder = avatarHash.Substring(0, 2);
            avatarURL = steamAvatarBaseURL + folder + "/" + avatarHash + ".jpg";
        }
        else
        {
            avatarURL = steamAvatarBaseURL + "fe/fef49e7fa7e1997310d705b2a6158ff8dc1cdfeb.jpg";
        }
#endregion

        return avatarURL;
    }

    // this is a utility function to transform a uint emsg into a string that can be used to display the name
    private static string GetEMsgDisplayString(uint eMsg)
    {
        Type[] eMsgEnums =
        {
                typeof(EGCBaseClientMsg),
                typeof(EGCBaseMsg),
                typeof(EGCItemMsg),
                typeof(ESOMsg),
                typeof(EGCSystemMsg),
            };

        foreach (Type enumType in eMsgEnums)
        {
            if (Enum.IsDefined(enumType, (int)eMsg))
                return Enum.GetName(enumType, (int)eMsg);

        }

        return eMsg.ToString();
    }

    private static string GetSentryFileLoc()
    {
        return Path.Combine(Application.persistentDataPath, SENTRY_FILE_NAME);
    }

    public static IPAddress ConvertToIPAddress(ulong address)
    {
        return IPAddress.Parse(ulong.Parse(address.ToString()).ToString());
    }
    public static void LogToConsole(string log)
    {
        TaskManagerController.RunAction(() =>
        {
            //try
            //{
                //Debug.Log(log);
                if (!steamInScene.suppressLog)
                    DebugViewController.Log(log);
            //}
            //catch (Exception e)
            //{
                //Debug.LogWarning("Could not log: " + e);
            //}
        });
    }
    private static string WatchableMatchInfoToString(WatchableMatchInfo watchInfo)
    {
        return "DecryptDataKey(" + (watchInfo.cl_decryptdata_keySpecified ? watchInfo.cl_decryptdata_key.ToString() : "?") +
                ")\nDecryptDataKeyPub(" + (watchInfo.cl_decryptdata_key_pubSpecified ? watchInfo.cl_decryptdata_key_pub.ToString() : "?") +
                ")\nGameMapGroup(" + (watchInfo.game_mapgroupSpecified ? watchInfo.game_mapgroup : "?") +
                ")\nGameMap(" + (watchInfo.game_mapSpecified ? watchInfo.game_map : "?") +
                ")\nGameType(" + (watchInfo.game_typeSpecified ? watchInfo.game_type.ToString() : "?") +
                ")\nMatchId(" + (watchInfo.match_idSpecified ? watchInfo.match_id.ToString() : "?") +
                ")\nReservationId(" + (watchInfo.reservation_idSpecified ? watchInfo.reservation_id.ToString() : "?") +
                ")\nServerId(" + (watchInfo.server_idSpecified ? watchInfo.server_id.ToString() : "?") +
                ")\nServerIP(" + (watchInfo.server_ipSpecified ? "uint[" + watchInfo.server_ip + "], int[" + ((int)watchInfo.server_ip) + "]" : "?") +
                ")\nTVPort(" + (watchInfo.tv_portSpecified ? watchInfo.tv_port.ToString() : "?") +
                ")\nTVSpectators(" + (watchInfo.tv_spectatorsSpecified ? watchInfo.tv_spectators.ToString() : "?") +
                ")\nTVTime(" + (watchInfo.tv_timeSpecified ? watchInfo.tv_time.ToString() : "?") +
                ")\nTVWatchPassword(" + (watchInfo.tv_watch_passwordSpecified ? ArrayToString(watchInfo.tv_watch_password) : "?") +
                ")";
    }
    public static string MatchInfoToString(CDataGCCStrike15_v2_MatchInfo matchInfoData)
    {
        string matchAsString = "";
        matchAsString += "MatchId: " + matchInfoData.matchid;
        matchAsString += "\nMatchTime: " + matchInfoData.matchtime;

        matchAsString += "\n\n---Watchable Match Info Start---";
        if (matchInfoData.watchablematchinfo != null)
        {
            matchAsString += "\n\tServerIP: " + matchInfoData.watchablematchinfo.server_ip;
            matchAsString += "\n\tTvPort: " + matchInfoData.watchablematchinfo.tv_port;
            matchAsString += "\n\tTvSpectators: " + matchInfoData.watchablematchinfo.tv_spectators;
            matchAsString += "\n\tTvTime: " + matchInfoData.watchablematchinfo.tv_time;
            matchAsString += "\n\tTvWatchPassword: " + matchInfoData.watchablematchinfo.tv_watch_password;
            matchAsString += "\n\tClDecryptDataKey: " + matchInfoData.watchablematchinfo.cl_decryptdata_key;
            matchAsString += "\n\tClDecryptDataKeyPublic: " + matchInfoData.watchablematchinfo.cl_decryptdata_key_pub;
            matchAsString += "\n\tGameType: " + matchInfoData.watchablematchinfo.game_type;
            matchAsString += "\n\tGameMapGroup: " + matchInfoData.watchablematchinfo.game_mapgroup;
            matchAsString += "\n\tGameMap: " + matchInfoData.watchablematchinfo.game_map;
            matchAsString += "\n\tServerId: " + matchInfoData.watchablematchinfo.server_id;
            matchAsString += "\n\tMatchId: " + matchInfoData.watchablematchinfo.match_id;
            matchAsString += "\n\tReservationId: " + matchInfoData.watchablematchinfo.reservation_id;
        }
        else
            matchAsString += "\n\tnil";
        matchAsString += "\n---Watchable Match Info End---";

        matchAsString += "\n\n---Round Stats Legacy Start---";
        if (matchInfoData.roundstats_legacy != null)
        {

        }
        else
            matchAsString += "\n\tnil";
        matchAsString += "\n---Round Stats Legacy End";

        matchAsString += "\n\n---Round Stats All Start---";
        if (matchInfoData.roundstatsall != null)
        {
            for (int i = 0; i < matchInfoData.roundstatsall.Count; i++)
            {
                matchAsString += "\n\tRound " + i;
                matchAsString += "\n\t\tReservationId: " + matchInfoData.roundstatsall[i].reservationid;

                matchAsString += "\n\n\t\tAccountIds(" + (matchInfoData.roundstatsall[i].reservation.account_ids != null ? matchInfoData.roundstatsall[i].reservation.account_ids.Count.ToString() : "nil") + ")";
                if (matchInfoData.roundstatsall[i].reservation.account_ids != null)
                    for (int j = 0; j < matchInfoData.roundstatsall[i].reservation.account_ids.Count; j++)
                        matchAsString += "\n\t\t\t[" + j + "]: " + matchInfoData.roundstatsall[i].reservation.account_ids[j];

                matchAsString += "\n\n\t\tGameType: " + matchInfoData.roundstatsall[i].reservation.game_type;
                matchAsString += "\n\t\tMatchId: " + matchInfoData.roundstatsall[i].reservation.match_id;
                matchAsString += "\n\t\tServerVersion: " + matchInfoData.roundstatsall[i].reservation.server_version;
                matchAsString += "\n\t\tRankings: " + (matchInfoData.roundstatsall[i].reservation.rankings != null ? "available" : "nil");
                matchAsString += "\n\t\tEncryptionKey: " + matchInfoData.roundstatsall[i].reservation.encryption_key;
                matchAsString += "\n\t\tEncryptionKeyPublic: " + matchInfoData.roundstatsall[i].reservation.encryption_key_pub;
                matchAsString += "\n\t\tPartyIds: " + (matchInfoData.roundstatsall[i].reservation.party_ids != null ? "available" : "nil");
                matchAsString += "\n\t\tWhitelist: " + (matchInfoData.roundstatsall[i].reservation.whitelist != null ? "available" : "nil");
                matchAsString += "\n\t\tTvMasterSteamId: " + matchInfoData.roundstatsall[i].reservation.tv_master_steamid;
                matchAsString += "\n\t\tTournamentEvent: " + (matchInfoData.roundstatsall[i].reservation.tournament_event != null ? "available" : "nil");
                matchAsString += "\n\t\tTournamentTeams: " + (matchInfoData.roundstatsall[i].reservation.tournament_teams != null ? "available" : "nil");
                matchAsString += "\n\t\tTournamentCastersAccountIds: " + (matchInfoData.roundstatsall[i].reservation.tournament_casters_account_ids != null ? "available" : "nil");
                matchAsString += "\n\t\tTvRelaySteamId: " + matchInfoData.roundstatsall[i].reservation.tv_relay_steamid;
                matchAsString += "\n\t\tPreMatchData: " + (matchInfoData.roundstatsall[i].reservation.pre_match_data != null ? "available" : "nil");
                matchAsString += "\n\t\tRtime32EventStart: " + matchInfoData.roundstatsall[i].reservation.rtime32_event_start;

                matchAsString += "\n\n\t\tMap: " + matchInfoData.roundstatsall[i].map;
                matchAsString += "\n\t\tRound: " + matchInfoData.roundstatsall[i].round;

                matchAsString += "\n\n\t\tKills(" + (matchInfoData.roundstatsall[i].kills != null ? matchInfoData.roundstatsall[i].kills.Count.ToString() : "nil") + ")";
                if (matchInfoData.roundstatsall[i].kills != null)
                    for (int j = 0; j < matchInfoData.roundstatsall[i].kills.Count; j++)
                        matchAsString += "\n\t\t\t[" + j + "]: " + matchInfoData.roundstatsall[i].kills[j];

                matchAsString += "\n\n\t\tAssists(" + (matchInfoData.roundstatsall[i].assists != null ? matchInfoData.roundstatsall[i].assists.Count.ToString() : "nil") + ")";
                if (matchInfoData.roundstatsall[i].assists != null)
                    for (int j = 0; j < matchInfoData.roundstatsall[i].assists.Count; j++)
                        matchAsString += "\n\t\t\t[" + j + "]: " + matchInfoData.roundstatsall[i].assists[j];

                matchAsString += "\n\n\t\tDeaths(" + (matchInfoData.roundstatsall[i].deaths != null ? matchInfoData.roundstatsall[i].deaths.Count.ToString() : "nil") + ")";
                if (matchInfoData.roundstatsall[i].deaths != null)
                    for (int j = 0; j < matchInfoData.roundstatsall[i].deaths.Count; j++)
                        matchAsString += "\n\t\t\t[" + j + "]: " + matchInfoData.roundstatsall[i].deaths[j];

                matchAsString += "\n\n\t\tScores(" + (matchInfoData.roundstatsall[i].scores != null ? matchInfoData.roundstatsall[i].scores.Count.ToString() : "nil") + ")";
                for (int j = 0; j < matchInfoData.roundstatsall[i].scores.Count; j++)
                    matchAsString += "\n\t\t\t[" + j + "]: " + matchInfoData.roundstatsall[i].scores[j];

                matchAsString += "\n\n\t\tPings: " + (matchInfoData.roundstatsall[i].pings != null ? "available" : "nil");
                matchAsString += "\n\t\tRoundResult: " + matchInfoData.roundstatsall[i].round_result;
                matchAsString += "\n\t\tMatchResult: " + matchInfoData.roundstatsall[i].match_result;

                matchAsString += "\n\n\t\tTeam Scores(" + (matchInfoData.roundstatsall[i].team_scores != null ? matchInfoData.roundstatsall[i].team_scores.Count.ToString() : "nil") + ")";
                if (matchInfoData.roundstatsall[i].team_scores != null)
                    for (int j = 0; j < matchInfoData.roundstatsall[i].team_scores.Count; j++)
                        matchAsString += "\n\t\t\t[" + j + "]: " + matchInfoData.roundstatsall[i].team_scores[j];

                matchAsString += "\n\n\t\tConfirm: " + (matchInfoData.roundstatsall[i].confirm != null ? "available" : "nil");
                matchAsString += "\n\t\tReservationStage: " + matchInfoData.roundstatsall[i].reservation_stage;
                matchAsString += "\n\t\tMatchDuration: " + matchInfoData.roundstatsall[i].match_duration;

                matchAsString += "\n\n\t\tEnemy Kills(" + (matchInfoData.roundstatsall[i].enemy_kills != null ? matchInfoData.roundstatsall[i].enemy_kills.Count.ToString() : "nil") + ")";
                if (matchInfoData.roundstatsall[i].enemy_kills != null)
                    for (int j = 0; j < matchInfoData.roundstatsall[i].enemy_kills.Count; j++)
                        matchAsString += "\n\t\t\t[" + j + "]: " + matchInfoData.roundstatsall[i].enemy_kills[j];

                matchAsString += "\n\n\t\tEnemy Headshots(" + (matchInfoData.roundstatsall[i].enemy_headshots != null ? matchInfoData.roundstatsall[i].enemy_headshots.Count.ToString() : "nil") + ")";
                if (matchInfoData.roundstatsall[i].enemy_headshots != null)
                    for (int j = 0; j < matchInfoData.roundstatsall[i].enemy_headshots.Count; j++)
                        matchAsString += "\n\t\t\t[" + j + "]: " + matchInfoData.roundstatsall[i].enemy_headshots[j];

                matchAsString += "\n\n\t\tEnemy 3ks(" + (matchInfoData.roundstatsall[i].enemy_3ks != null ? matchInfoData.roundstatsall[i].enemy_3ks.Count.ToString() : "nil") + ")";
                if (matchInfoData.roundstatsall[i].enemy_3ks != null)
                    for (int j = 0; j < matchInfoData.roundstatsall[i].enemy_3ks.Count; j++)
                        matchAsString += "\n\t\t\t[" + j + "]: " + matchInfoData.roundstatsall[i].enemy_3ks[j];

                matchAsString += "\n\n\t\tEnemy 4ks(" + (matchInfoData.roundstatsall[i].enemy_4ks != null ? matchInfoData.roundstatsall[i].enemy_4ks.Count.ToString() : "nil") + ")";
                if (matchInfoData.roundstatsall[i].enemy_4ks != null)
                    for (int j = 0; j < matchInfoData.roundstatsall[i].enemy_4ks.Count; j++)
                        matchAsString += "\n\t\t\t[" + j + "]: " + matchInfoData.roundstatsall[i].enemy_4ks[j];

                matchAsString += "\n\n\t\tEnemy 5ks(" + (matchInfoData.roundstatsall[i].enemy_5ks != null ? matchInfoData.roundstatsall[i].enemy_5ks.Count.ToString() : "nil") + ")";
                if (matchInfoData.roundstatsall[i].enemy_5ks != null)
                    for (int j = 0; j < matchInfoData.roundstatsall[i].enemy_5ks.Count; j++)
                        matchAsString += "\n\t\t\t[" + j + "]: " + matchInfoData.roundstatsall[i].enemy_5ks[j];

                matchAsString += "\n\n\t\tmvps(" + (matchInfoData.roundstatsall[i].mvps != null ? matchInfoData.roundstatsall[i].mvps.Count.ToString() : "nil") + ")";
                if (matchInfoData.roundstatsall[i].mvps != null)
                    for (int j = 0; j < matchInfoData.roundstatsall[i].mvps.Count; j++)
                        matchAsString += "\n\t\t\t[" + j + "]: " + matchInfoData.roundstatsall[i].mvps[j];

                matchAsString += "\n\n\t\tSpectatorsCount: " + matchInfoData.roundstatsall[i].spectators_count;
                matchAsString += "\n\t\tSpectatorsCountTv: " + matchInfoData.roundstatsall[i].spectators_count_tv;
                matchAsString += "\n\t\tSpectatorsCountLnk: " + matchInfoData.roundstatsall[i].spectators_count_lnk;

                matchAsString += "\n\n\t\tEnemy Kills Agg(" + (matchInfoData.roundstatsall[i].enemy_kills_agg != null ? matchInfoData.roundstatsall[i].enemy_kills_agg.Count.ToString() : "nil") + ")";
                if (matchInfoData.roundstatsall[i].enemy_kills_agg != null)
                    for (int j = 0; j < matchInfoData.roundstatsall[i].enemy_kills_agg.Count; j++)
                        matchAsString += "\n\t\t\t[" + j + "]: " + matchInfoData.roundstatsall[i].enemy_kills_agg[j];

                matchAsString += "\n\n\t\tDropInfo: " + (matchInfoData.roundstatsall[i].drop_info != null ? "available" : "nil");
            }
        }
        else
            matchAsString += "\n\tnil";
        matchAsString += "\n---Round Stats All End---";

        matchAsString += "\nRoundCount: " + (matchInfoData.roundstatsall != null ? matchInfoData.roundstatsall.Count.ToString() : "nil");
        matchAsString += "\nLink: " + (matchInfoData.roundstatsall != null ? matchInfoData.roundstatsall[matchInfoData.roundstatsall.Count - 1].map : "nil");
        matchAsString += "\nMapId: " + (matchInfoData.roundstatsall != null ? matchInfoData.roundstatsall[matchInfoData.roundstatsall.Count - 1].reservation.game_type.ToString() : "nil");

        return matchAsString;
    }

    public static string ArrayToString(Array array)
    {
        string output = "{ ";
        for (int i = 0; i < array.Length; i++)
        {
            object value = array.GetValue(i);
            output += value.ToString();
            if (i < array.Length - 1)
                output += ", ";
        }
        output += " }";
        return output;
    }

    public static IPacketMsg GetPacketMsg(byte[] data)
    {
        if (data.Length < sizeof(uint))
        {
            Debug.Log("PacketMsg too small to contain a message, was only " + data.Length + " bytes. Message: 0x" + BitConverter.ToString(data).Replace("-", string.Empty));
            return null;
        }

        uint rawEMsg = BitConverter.ToUInt32(data, 0);
        EMsg eMsg = MsgUtil.GetMsg(rawEMsg);

        switch (eMsg)
        {
            // certain message types are always MsgHdr
            case EMsg.ChannelEncryptRequest:
            case EMsg.ChannelEncryptResponse:
            case EMsg.ChannelEncryptResult:
                return new PacketMsg(eMsg, data);
        }

        try
        {
            if (MsgUtil.IsProtoBuf(rawEMsg))
            {
                // if the emsg is flagged, we're a proto message
                return new PacketClientMsgProtobuf(eMsg, data);
            }
            else
            {
                // otherwise we're a struct message
                return new PacketClientMsg(eMsg, data);
            }
        }
        catch (Exception ex)
        {
            Debug.Log("Exception deserializing emsg " + eMsg + " (" + MsgUtil.IsProtoBuf(rawEMsg) + ").\n" + ex.ToString());
            return null;
        }
    }
#endregion
}