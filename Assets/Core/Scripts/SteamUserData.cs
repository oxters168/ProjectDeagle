using SteamKit2;
using System;
using UnityEngine;
using UnityHelpers;

[Serializable]
public class SteamUserData
{
    [SerializeField]
    private uint idUInt32;
    [SerializeField]
    private ulong idUInt64;
    [SerializeField]
    private string displayName = "";
    [SerializeField]
    private Texture2D avatar;
    private Sprite avatarAsSprite;
    [SerializeField]
    private uint appId;
    [SerializeField]
    private EPersonaState state;
    [SerializeField]
    private string clanTag = "";
    [SerializeField]
    private uint clanRank;
    [SerializeField]
    private uint onlineSessionInstances;
    [SerializeField]
    private uint publishedSessionId;
    [SerializeField]
    private DateTime lastLogOn;
    [SerializeField]
    private DateTime lastLogOff;
    [SerializeField]
    private EFriendRelationship relationship;

    public SteamUserData(uint _idUInt32, ulong _idUInt64)
    {
        idUInt32 = _idUInt32;
        idUInt64 = _idUInt64;
    }

    public uint GetIdUInt32()
    {
        return idUInt32;
    }
    public ulong GetIdUInt64()
    {
        return idUInt64;
    }
    public void SetDisplayName(string name)
    {
        displayName = name;
    }
    public string GetDisplayName()
    {
        return displayName;
    }
    public void SetAvatar(byte[] data)
    {
        TaskManagerController.RunAction(() =>
        {
            if (data != null && data.Length > 0)
            {
                if (!avatar)
                    avatar = new Texture2D(1, 1);

                avatar.LoadImage(data);
                avatar.Apply();
                avatarAsSprite = avatar.ToSprite();
            }
        });
    }
    public Texture2D GetAvatar()
    {
        return avatar;
    }
    public Sprite GetAvatarAsSprite()
    {
        return avatarAsSprite;
    }
    public void SetAppId(uint _appId)
    {
        appId = _appId;
    }
    public uint GetAppId()
    {
        return appId;
    }
    public void SetState(EPersonaState _state)
    {
        state = _state;
    }
    public EPersonaState GetState()
    {
        return state;
    }
    public void SetClanTag(string tag)
    {
        clanTag = tag;
    }
    public string GetClanTag()
    {
        return clanTag;
    }
    public void SetClanRank(uint rank)
    {
        clanRank = rank;
    }
    public uint GetClanRank()
    {
        return clanRank;
    }
    public void SetOnlineSessionInstances(uint instances)
    {
        onlineSessionInstances = instances;
    }
    public uint GetOnlineSessionInstances()
    {
        return onlineSessionInstances;
    }
    public void SetPublishedSessionId(uint id)
    {
        publishedSessionId = id;
    }
    public uint GetPublishedSessionId()
    {
        return publishedSessionId;
    }
    public void SetLastLogOn(DateTime time)
    {
        lastLogOn = time;
    }
    public DateTime GetLastLogOn()
    {
        return lastLogOn;
    }
    public void SetLastLogOff(DateTime time)
    {
        lastLogOff = time;
    }
    public DateTime GetLastLogOff()
    {
        return lastLogOff;
    }
    public void SetRelationship(EFriendRelationship _relationship)
    {
        relationship = _relationship;
    }
    public EFriendRelationship GetRelationship()
    {
        return relationship;
    }
}
