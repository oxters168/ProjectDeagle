using UnityEngine;
using UnityHelpers;
using System.Linq;

public class ChatController : MonoBehaviour
{
    public static readonly Color ONLINE_COLOR = Color.green;
    public static readonly Color AWAY_COLOR = Color.yellow;
    public static readonly Color BUSY_COLOR = Color.red;
    public static readonly Color SNOOZE_COLOR = Color.blue;
    public static readonly Color INVISIBLE_COLOR = Color.gray;
    public static readonly Color OFFLINE_COLOR = Color.clear;

    public static ChatController chatInScene;

    public RectTransform notLoggedInPanel;
    public RectTransform loggedInPanel;
    public float bottomMissingSize = 40;

    public ChatHeadController chatView;

    //private SteamUserData loggedInUser;

    //public ListViewController friendsList;
    public CustomListController friendsList;

    private void Awake()
    {
        chatInScene = this;
    }
    private void Start()
    {
        SteamController.steamInScene.onUserStateChanged += SteamInScene_onUserStateChanged;
    }
    private void Update()
    {
        UpdateCurrentUserData();
    }
    private void OnEnable()
    {
        UpdateFriendList();
    }

    private void SteamInScene_onUserStateChanged(SteamUserData user)
    {
        TaskManagerController.RunAction(UpdateFriendList);
    }

    public void OpenChat(uint friendId)
    {
        Doozy.Engine.GameEventMessage.SendEvent("GotoChatView");
        chatView.SetUser(friendId);
    }
    private void UpdateCurrentUserData()
    {
        bool loggedIn = SteamController.steamInScene.IsLoggedIn;
        notLoggedInPanel.gameObject.SetActive(!loggedIn);
        loggedInPanel.gameObject.SetActive(loggedIn);

        //if (loggedIn && (loggedInUser == null || loggedInUser.GetIdUInt32() != SteamController.steamInScene.userAccountId))
        //    loggedInUser = SteamController.steamInScene.GetFriendWithAccountId(SteamController.steamInScene.userAccountId);
    }
    private void UpdateFriendList()
    {
        if (SteamController.steamInScene)
        {
            SteamUserData[] friends = SteamController.steamInScene.GetFriends();
            friends = friends.OrderBy(friend => friend.GetState() != SteamKit2.EPersonaState.Offline ? (int)friend.GetState() : int.MaxValue).ToArray();

            float scrollPosition = friendsList.GetCurrentVerticalScrollValue();

            friendsList.ClearItems();
            friendsList.AddToList(friends);

            friendsList.SetCurrentVerticalScrollValue(scrollPosition);
        }
    }

    public static Color GetStateColor(SteamKit2.EPersonaState state)
    {
        switch (state)
        {
            case SteamKit2.EPersonaState.Online:
                return ONLINE_COLOR;
            case SteamKit2.EPersonaState.Away:
                return AWAY_COLOR;
            case SteamKit2.EPersonaState.Busy:
                return BUSY_COLOR;
            case SteamKit2.EPersonaState.Snooze:
                return SNOOZE_COLOR;
            case SteamKit2.EPersonaState.Invisible:
                return INVISIBLE_COLOR;
            case SteamKit2.EPersonaState.Offline:
                return OFFLINE_COLOR;
            default:
                return Color.white;
        }
    }
}
