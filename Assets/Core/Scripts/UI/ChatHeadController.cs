using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityHelpers;

public class ChatHeadController : MonoBehaviour
{
    public ChatMessageController chatMessagePrefab;
    public RectTransform chatMessagesHolder;
    private ObjectPool<ChatMessageController> _chatMessagePool;
    private ObjectPool<ChatMessageController> ChatMessagePool { get { if (_chatMessagePool == null) _chatMessagePool = new ObjectPool<ChatMessageController>(chatMessagePrefab, 5, false, true, chatMessagesHolder, false); return _chatMessagePool; } }

    public AccountAvatarController avatar;
    public TextMeshProUGUI nameText;
    public TMP_InputField messageField;

    public GameObject isTypingGlyph;
    private float lastTypingSent;
    private float lastTypingReceived;
    private float sendTypingFrequency = 10f;
    public bool friendIsTyping { get { return Time.time - lastTypingReceived <= sendTypingFrequency; } }

    private SteamUserData user;

    private void Start()
    {
        SteamController.steamInScene.onFriendChatReceived += SteamInScene_onFriendChatReceived;
        SteamController.steamInScene.onChatHistoryReceived += SteamInScene_onChatHistoryReceived;
    }
    private void Update()
    {
        UpdateChatWindowUserInfo();
        UpdateChatWindow();
    }
    private void OnDisable()
    {
        ClearUser();
    }

    private void SteamInScene_onFriendChatReceived(uint fromId, SteamKit2.EChatEntryType entryType, string message)
    {
        if (user != null && user.GetIdUInt32() == fromId)
        {
            if (entryType == SteamKit2.EChatEntryType.ChatMsg)
            {
                lastTypingReceived = float.MinValue;
                PutMessage(message, true);
            }
            else if (entryType == SteamKit2.EChatEntryType.Typing)
                lastTypingReceived = Time.time;
        }
    }
    private void SteamInScene_onChatHistoryReceived(uint chatPartnerId, IEnumerable<SteamKit2.SteamFriends.FriendMsgHistoryCallback.FriendMessage> messages)
    {
        if (user != null && user.GetIdUInt32() == chatPartnerId)
        {
            SteamKit2.SteamFriends.FriendMsgHistoryCallback.FriendMessage[] orderedMessages = messages.ToList().OrderBy(message => message.Timestamp).ToArray();
            for (int i = 0; i < orderedMessages.Length; i++)
            {
                PutMessage(orderedMessages[i].Message, orderedMessages[i].SteamID.AccountID != SteamController.steamInScene.userAccountId);
            }
        }
    }

    private void UpdateChatWindowUserInfo()
    {
        if (user != null)
        {
            avatar.accountImage.sprite = user.GetAvatarAsSprite();
            nameText.text = user.GetDisplayName();
            avatar.personaStateImage.color = ChatController.GetStateColor(user.GetState());
        }
    }
    private void UpdateChatWindow()
    {
        isTypingGlyph.SetActive(friendIsTyping);
    }

    public void SetUser(uint userId)
    {
        if (user == null || user.GetIdUInt32() != userId)
        {
            user = SteamController.steamInScene.GetFriendWithAccountId(userId);
            SteamController.steamInScene.RequestFriendChatHistory(user.GetIdUInt64());
        }
    }
    public void ClearUser()
    {
        user = null;
        avatar.accountImage.sprite = null;
        ChatMessagePool.ReturnAll();
    }
    public uint GetUserId()
    {
        return user.GetIdUInt32();
    }

    public void Typing()
    {
        if (Time.time - lastTypingSent > sendTypingFrequency)
        {
            lastTypingSent = Time.time;
            SteamController.steamInScene.SendTypingTo(user.GetIdUInt64());
        }
    }
    public void Send()
    {
        string message = messageField.text;
        if (message.Length > 0)
        {
            SteamController.steamInScene.SendChatMessageTo(user.GetIdUInt64(), message);
            PutMessage(message, false);
            messageField.text = "";
        }
    }
    public void PutMessage(string text, bool received)
    {
        ChatMessageController chatMessage = ChatMessagePool.Get();
        chatMessage.showOnLeft = received;
        chatMessage.transform.SetAsLastSibling();
        chatMessage.SetMessage(text);

        //LayoutRebuilder.MarkLayoutForRebuild(chatMessagesHolder);
        LayoutRebuilder.ForceRebuildLayoutImmediate(chatMessagesHolder);
    }
}
