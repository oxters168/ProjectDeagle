using UnityEngine;
using TMPro;

public class FriendListItemController : ListItemController
{
    public AccountAvatarController accountAvatar;
    public TextMeshProUGUI displayName;

    [Space(10)]

    private uint currentId;
    private Sprite currentAvatar;

    private void Update()
    {
        if (item != null && item is SteamUserData)
        {
            var steamUser = (SteamUserData)item;

            if (currentId != steamUser.GetIdUInt32() || currentAvatar == null)
            {
                currentAvatar = steamUser.GetAvatarAsSprite();
                currentId = steamUser.GetIdUInt32();
            }

            accountAvatar.accountImage.sprite = currentAvatar;
            displayName.text = steamUser.GetDisplayName();

            accountAvatar.personaStateImage.color = ChatController.GetStateColor(steamUser.GetState());
        }
    }

    public void InitiateChat()
    {
        if (item != null && item is SteamUserData)
        {
            var steamUser = (SteamUserData)item;
            Debug.Log("Initiating chat with " + steamUser.GetDisplayName());
            ChatController.chatInScene.OpenChat(steamUser.GetIdUInt32());
        }
    }
}
