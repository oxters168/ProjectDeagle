using UnityEngine;
using System.Collections.Generic;

namespace ProjectDeagle
{
    public class PlayerResource
    {
        //internal Entity entity;
        public PlayerInfo playerInfo { get; internal set; }
        public string model { get; internal set; }

        #region Vectors
        public Vector3 position { get; internal set; } //cslocaldata.m_vecOrigin (Right and Forward Axis), cslocaldata.m_vecOrigin[2] (Up Axis)
        public Vector3 velocity { get; internal set; } //localdata.m_vecVelocity[0], localdata.m_vecVelocity[2], localdata.m_vecVelocity[1]
        public Vector2 viewDirection { get; internal set; } //m_angEyeAngles[1], m_angEyeAngles[0]
        #endregion

        #region Integers
        public int playerState { get; internal set; } //m_iPlayerState --

        public int activeWeapon { get; internal set; } //m_hActiveWeapon --
        public int lastWeapon { get; internal set; } //localdata.m_hLastWeapon --

        public int startMoney { get; internal set; } //m_iStartAccount --
        public int money { get; internal set; } //m_iAccount --
        public int health { get; internal set; } //m_iHealth --
        public int armor { get; internal set; } //m_ArmorValue --

        public int teamNum { get; internal set; } //m_iTeamNum
        public int pendingTeamNum { get; internal set; } //m_iPendingTeamNum --

        public int shotsFired { get; internal set; } //cslocaldata.m_iShotsFired --
        public int throwGrenadeCounter { get; internal set; } //m_iThrowGrenadeCounter --
        public int roundKills { get; internal set; } //m_iNumRoundKills --
        public int roundHeadshots { get; internal set; } //m_iNumRoundKillsHeadshots --
        public int lastKillerIndex { get; internal set; } //m_nLastKillerIndex --
        public int lastConcurrentKilled { get; internal set; } //m_nLastConcurrentKilled --

        public int currentEquipmentValue { get; internal set; } //m_unCurrentEquipmentValue --
        public int freezeTimeEndEquipmentValue { get; internal set; } //m_unFreezetimeEndEquipmentValue --
        public int roundStartEquipmentValue { get; internal set; } //m_unRoundStartEquipmentValue --

        public int bonusChallenge { get; internal set; } //m_iBonusChallenge --
        public int bonusProgress { get; internal set; } //m_iBonusProgress --

        public int modelIndex { get; internal set; } //m_nModelIndex --
        public int controlledBotEntityIndex { get; internal set; } //m_iControlledBotEntIndex --
        public int observerTarget { get; internal set; } //m_hObserverTarget --
        public int zoomOwner { get; internal set; } //m_hZoomOwner --

        public int ping { get; internal set; } //m_iPing.num (CCSPlayerResource)
        public int kills { get; internal set; } //m_iKills.num (CCSPlayerResource)
        public int assists { get; internal set; } //m_iAssists.num (CCSPlayerResource)
        public int deaths { get; internal set; } //m_iDeaths.num (CCSPlayerResource)
        public int mvps { get; internal set; } //m_iMVPs.num (CCSPlayerResource)
        public int score { get; internal set; } //m_iScore.num (CCSPlayerResource)
        public int controlledPlayer { get; internal set; } //m_iControlledPlayer.num (CCSPlayerResource)
        public int controlledByPlayer { get; internal set; } //m_iControlledByPlayer.num (CCSPlayerResource)
        public int team { get; internal set; } //m_iTeam.num (CCSPlayerResource)

        public int competitiveRanking { get; internal set; } //m_iCompetitiveRanking.num (CCSPlayerResource)
        public int competitiveWins { get; internal set; } //m_iCompetitiveWins.num (CCSPlayerResource)
        public int competitiveTeammateColor { get; internal set; } //m_iCompTeammateColor.num (CCSPlayerResource)
        #endregion

        #region Booleans
        public bool isConnected { get; internal set; } //m_bConnected.num (CCSPlayerResource)

        public bool isWalking { get; internal set; } //m_bIsWalking --
        public bool isDucking { get; internal set; } //localdata.m_Local.m_bDucking --
        public bool ducked { get; internal set; } //localdata.m_Local.m_bDucked --
        public bool inDuckJump { get; internal set; } //localdata.m_Local.m_bInDuckJump --
        public bool isDefusing { get; internal set; } //m_bIsDefusing --
        public bool isGrabbingHostage { get; internal set; } //m_bIsGrabbingHostage --
        public bool isRescuing { get; internal set; } //m_bIsRescuing --

        public bool hasDefuseKit { get; internal set; } //m_bHasDefuser --
        public bool hasHelmet { get; internal set; } //m_bHasHelmet --
        public bool hasKevlar { get; internal set; } //m_bHasHeavyArmor --

        public bool isDead { get; internal set; } //pl.deadflag --
        public bool killedByTaser { get; internal set; } //m_bKilledByTaser --
        public bool isRespawningForDeathmatchBonus { get; internal set; } //m_bIsRespawningForDMBonus --
        public bool hasMovedSinceSpawn { get; internal set; } //m_bHasMovedSinceSpawn --

        public bool inBombZone { get; internal set; } //m_bInBombZone --
        public bool inBuyZone { get; internal set; } //m_bInBuyZone --
        public bool inNoDefuseArea { get; internal set; } //m_bInNoDefuseArea --
        public bool inHostageRescueZone { get; internal set; } //m_bInHostageRescueZone --

        public bool isLookingAtWeapon { get; internal set; } //m_bIsLookingAtWeapon --
        public bool isHoldingLookAtWeapon { get; internal set; } //m_bIsHoldingLookAtWeapon --
        public bool isScoped { get; internal set; } //m_bIsScoped --
        public bool resumeZoom { get; internal set; } //m_bResumeZoom --

        public bool isControllingBot { get; internal set; } //m_bIsControllingBot --
        public bool hasControlledBotThisRound { get; internal set; } //m_bHasControlledBotThisRound --
        public bool canControlObservedBot { get; internal set; } //m_bCanControlObservedBot --

        public bool isCurrentGunGameLeader { get; internal set; } //m_isCurrentGunGameLeader --
        public bool isCurrentGunGameTeamLeader { get; internal set; } //m_isCurrentGunGameTeamLeader --
        #endregion

        #region Strings
        public string clanName { get; internal set; } //m_szClan.num (CCSPlayerResource)
        public string lastPlaceName { get; internal set; } //m_szLastPlaceName --
        public string armsModel { get; internal set; } //m_szArmsModel --
        #endregion

        #region Collections
        internal Dictionary<int, int> _weapons; //m_hMyWeapons.num --
        public Dictionary<int, int> weapons { get { return _weapons != null ? new Dictionary<int, int>(_weapons) : null; } }
        internal Dictionary<int, int> _ammo; //m_iAmmo.num --
        public Dictionary<int, int> ammo { get { return _ammo != null ? new Dictionary<int, int>(_ammo) : null; } }
        internal List<int> _weaponPurchasesThisRound; //cslocaldata.m_iWeaponPurchasesThisRound.num --
        public List<int> weaponPurchasesThisRound { get { return _weaponPurchasesThisRound != null ? new List<int>(_weaponPurchasesThisRound) : null; } }
        internal Dictionary<int, bool> _playersDominatedByMe; //cslocaldata.m_PlayerDominated.num --
        public Dictionary<int, bool> playersDominatedByMe { get { return _playersDominatedByMe != null ? new Dictionary<int, bool>(_playersDominatedByMe) : null; } }
        internal Dictionary<int, bool> _playersDominatingMe; //cslocaldata.m_bPlayerDominatingMe.num --
        public Dictionary<int, bool> playersDominatingMe { get { return _playersDominatingMe != null ? new Dictionary<int, bool>(_playersDominatingMe) : null; } }
        #endregion

        internal PlayerResource() { }
        internal PlayerResource(PlayerResource other)
        {
            playerInfo = other.playerInfo;
            model = other.model;

            #region Vectors
            position = other.position;
            velocity = other.velocity;
            viewDirection = other.viewDirection;
            #endregion

            #region Integers
            playerState = other.playerState;

            activeWeapon = other.activeWeapon;
            lastWeapon = other.lastWeapon;

            startMoney = other.startMoney;
            money = other.money;
            health = other.health;
            armor = other.armor;

            teamNum = other.teamNum;
            pendingTeamNum = other.pendingTeamNum;

            shotsFired = other.shotsFired;
            throwGrenadeCounter = other.throwGrenadeCounter;
            roundKills = other.roundKills;
            roundHeadshots = other.roundHeadshots;
            lastKillerIndex = other.lastKillerIndex;
            lastConcurrentKilled = other.lastConcurrentKilled;

            currentEquipmentValue = other.currentEquipmentValue;
            freezeTimeEndEquipmentValue = other.freezeTimeEndEquipmentValue;
            roundStartEquipmentValue = other.roundStartEquipmentValue;

            bonusChallenge = other.bonusChallenge;
            bonusProgress = other.bonusProgress;

            modelIndex = other.modelIndex;
            controlledBotEntityIndex = other.controlledBotEntityIndex;
            observerTarget = other.observerTarget;
            zoomOwner = other.zoomOwner;
            #endregion

            #region Booleans
            isWalking = other.isWalking;
            isDucking = other.isDucking;
            ducked = other.ducked;
            inDuckJump = other.inDuckJump;
            isDefusing = other.isDefusing;
            isGrabbingHostage = other.isGrabbingHostage;
            isRescuing = other.isRescuing;

            hasDefuseKit = other.hasDefuseKit;
            hasHelmet = other.hasHelmet;
            hasKevlar = other.hasKevlar;

            isDead = other.isDead;
            killedByTaser = other.killedByTaser;
            isRespawningForDeathmatchBonus = other.isRespawningForDeathmatchBonus;
            hasMovedSinceSpawn = other.hasMovedSinceSpawn;

            inBombZone = other.inBombZone;
            inBuyZone = other.inBuyZone;
            inNoDefuseArea = other.inNoDefuseArea;
            inHostageRescueZone = other.inHostageRescueZone;

            isLookingAtWeapon = other.isLookingAtWeapon;
            isHoldingLookAtWeapon = other.isHoldingLookAtWeapon;
            isScoped = other.isScoped;
            resumeZoom = other.resumeZoom;

            isControllingBot = other.isControllingBot;
            hasControlledBotThisRound = other.hasControlledBotThisRound;
            canControlObservedBot = other.canControlObservedBot;

            isCurrentGunGameLeader = other.isCurrentGunGameLeader;
            isCurrentGunGameTeamLeader = other.isCurrentGunGameTeamLeader;
            #endregion

            #region Strings
            lastPlaceName = other.lastPlaceName;
            armsModel = other.armsModel;
            #endregion

            #region Collections
            _weapons = other.weapons;
            _ammo = other.ammo;
            _weaponPurchasesThisRound = other.weaponPurchasesThisRound;
            _playersDominatedByMe = other.playersDominatedByMe;
            _playersDominatingMe = other.playersDominatingMe;
            #endregion

            CopyResourceValues(other);
        }

        internal void CopyResourceValues(PlayerResource other)
        {
            isConnected = other.isConnected;
            kills = other.kills;
            deaths = other.deaths;
            assists = other.assists;
            score = other.score;
            mvps = other.mvps;
            ping = other.ping;
            controlledPlayer = other.controlledPlayer;
            controlledByPlayer = other.controlledByPlayer;
            team = other.team;

            clanName = other.clanName;
            competitiveRanking = other.competitiveRanking;
            competitiveWins = other.competitiveWins;
            competitiveTeammateColor = other.competitiveTeammateColor;
        }
    }
}