using UnityEngine;
using DemoInfo;
using System.IO;
using System.Linq;
using UnityHelpers;
using UnityEngine.UI;
using Doozy.Engine.UI;
using TMPro;
using System.Collections.Generic;

public class MatchPlayer : MonoBehaviour
{
    private readonly string MATCH_SEEK_ACTION_NAME = "MatchSeek";

    public string exitMatchPlayerGameEvent = "ExitMatchPlayer";

    private bool threeDView;

    public TouchGesturesHandler touchGestures;
    public Sprite playSprite, pauseSprite;
    public Image playbackButtonImage;
    public UIView playbackControlsView;
    public Slider seekBar;
    public Image currentSeekImage;
    public TextMeshProUGUI currentRoundText, totalRoundsText;
    public TextMeshProUGUI currentTimeText, totalTimeText;
    public TextMeshProUGUI team1ScoreText, team2ScoreText;
    public TextMeshProUGUI winText;
    public float winTextShowTime = 5;
    private float winTextLastShown;

    public float controlsHideTime = 5;
    private float controlsToggledTime;

    public int seekAmount = 10;

    private static MatchPlayer matchPlayerInScene;
    private MatchInfo currentMatch;
    private MapData matchMap;
    private OverviewData matchOverview;

    private DemoParser demoParser;

    public Color counterTerroristColor, terroristColor, spectatorColor;

    public static bool Play { get { return matchPlayerInScene.play; } set { matchPlayerInScene.play = value; } }
    public bool play;
    public int tickIndex;
    public float secondsPerTick;
    private float lastTickPlayed;

    public bool isDone { get { return demoParser == null || demoParser.CurrentTick >= demoParser.Header.PlaybackFrames; } }

    public float defaultOverviewRotation = 180, characterRotationXOffset = -90, characterZoomScale = 0.02f;

    [Space(10)]
    public SizableItem overviewObject;
    public OverviewCharacter ocPrefab;
    private ObjectPool<OverviewCharacter> mapBlips;

    [Space(10)]
    public MatchCharacter mcPrefab;
    private ObjectPool<MatchCharacter> charactersPool;

    private Dictionary<int, MatchCharacter> characterModels = new Dictionary<int, MatchCharacter>();
    private Dictionary<int, OverviewCharacter> twoDeeCharacters = new Dictionary<int, OverviewCharacter>();

    //key is entity id
    private Dictionary<int, TempPlayerInfo> playerTempValues = new Dictionary<int, TempPlayerInfo>();

    private bool isSeeking;
    private bool isBarSeeking;

    private Vector3 prevMousePos;

    private void Awake()
    {
        matchPlayerInScene = this;
        mapBlips = new ObjectPool<OverviewCharacter>(ocPrefab, 10, false, true, overviewObject.transform, false);
        charactersPool = new ObjectPool<MatchCharacter>(mcPrefab, 10, false);
    }
    private void OnEnable()
    {
        touchGestures.onUp += TouchGestures_onUp;
        Doozy.Engine.Message.AddListener<Doozy.Engine.GameEventMessage>(OnGameEventReceived);
    }
    private void OnDisable()
    {
        touchGestures.onUp -= TouchGestures_onUp;
        Doozy.Engine.Message.RemoveListener<Doozy.Engine.GameEventMessage>(OnGameEventReceived);
        CancelSeek();
        DisposeDemoParser();
    }
    private void Update()
    {
        Vector3 currentMousePos = Input.mousePosition;

        if (demoParser != null && !Input.GetMouseButton(0) && (currentMousePos - prevMousePos).sqrMagnitude > 0)
            ShowControls();

        prevMousePos = currentMousePos;
    }
    private void FixedUpdate()
    {
        if (demoParser != null)
        {
            UpdateSeekBar();
            UpdateRoundsText();

            if (demoParser.CurrentTick != tickIndex)
                Goto(tickIndex);

            if (play && !isSeeking && !isBarSeeking && !isDone && Time.time >= lastTickPlayed + secondsPerTick)
            {
                lastTickPlayed = Time.time;
                demoParser.ParseNextTick();
                tickIndex = demoParser.CurrentTick;
            }

            UpdateOverviewBlips();
            if (threeDView)
                UpdateCharacters();
            ResetTempValues();
        }

        if (Time.time - winTextLastShown > winTextShowTime)
            winText.gameObject.SetActive(false);

        if ((playbackControlsView.IsShowing || playbackControlsView.IsVisible) && !isBarSeeking && play && Time.time - controlsToggledTime > controlsHideTime)
            HideControls();
        playbackButtonImage.sprite = (play && !isDone) ? pauseSprite : playSprite;
    }

    private void OnGameEventReceived(Doozy.Engine.GameEventMessage message)
    {
        if (message.EventName.Equals(exitMatchPlayerGameEvent))
            CloseCurrentMatch();
    }

    private void ResetTempValues()
    {
        foreach(var tempInfo in playerTempValues)
            tempInfo.Value.ResetValues();
    }

    public List<MatchCharacter> GetPlayers()
    {
        return characterModels.Where(pair => pair.Value != null).Select(pair => pair.Value).ToList();
    }
    private void TouchGestures_onUp(Vector2 position, Vector2 delta)
    {
        if (demoParser != null)
        {
            if (playbackControlsView.IsVisible || playbackControlsView.IsShowing)
                HideControls();
            else
                ShowControls();
            //playbackControlsView.Toggle();
        }
    }
    public void TogglePause()
    {
        play = !play;
        controlsToggledTime = Time.time;
    }
    public void SeekToNextRound()
    {
        int roundStartTickIndex = currentMatch.extraMatchStats.roundStartTicks.FindIndex((roundIndex) => tickIndex < roundIndex);
        if (roundStartTickIndex > -1)
            Goto(currentMatch.extraMatchStats.roundStartTicks[roundStartTickIndex]);
        controlsToggledTime = Time.time;
    }
    public void SeekToPrevRound()
    {
        int roundStartIndex = currentMatch.extraMatchStats.roundStartTicks.FindLastIndex((roundIndex) => tickIndex > roundIndex);
        if (roundStartIndex > -1)
            Goto(currentMatch.extraMatchStats.roundStartTicks[roundStartIndex]);
        controlsToggledTime = Time.time;
    }
    public void SeekForward()
    {
        int ticksToSeek = SecondsToTicks(seekAmount);
        Goto(tickIndex + ticksToSeek);
        controlsToggledTime = Time.time;
    }
    public void SeekBackward()
    {
        int ticksToSeek = SecondsToTicks(seekAmount);
        Goto(tickIndex - ticksToSeek);
        controlsToggledTime = Time.time;
    }
    public void SeekbarDrag()
    {
        isBarSeeking = true;
        Goto(Mathf.FloorToInt(seekBar.value * demoParser.Header.PlaybackFrames));
        controlsToggledTime = Time.time;
    }
    public void SeekbarEndDrag()
    {
        isBarSeeking = false;
        Goto(Mathf.FloorToInt(seekBar.value * demoParser.Header.PlaybackFrames));
        controlsToggledTime = Time.time;
    }

    private void ShowWinText(string text)
    {
        winText.text = text;
        winText.gameObject.SetActive(true);
        winTextLastShown = Time.time;
    }
    private void CancelSeek()
    {
        if (TaskManagerController.HasTask(MATCH_SEEK_ACTION_NAME))
            TaskManagerController.CancelTask(MATCH_SEEK_ACTION_NAME);
    }
    private void Goto(int index)
    {
        tickIndex = Mathf.Clamp(index, 0, demoParser.Header.PlaybackFrames);

        if (!isSeeking)
        {
            isSeeking = true;
            TaskManagerController.RunActionAsync(MATCH_SEEK_ACTION_NAME, (cancelToken) => { CancellableGoto(cancelToken); isSeeking = false; });
        }
    }
    private void CancellableGoto(System.Threading.CancellationToken cancellationToken)
    {
        //index = Mathf.Clamp(index, 0, demoParser.Header.PlaybackFrames);

        RemoveListeners(demoParser);
        if (demoParser.CurrentTick > tickIndex)
            demoParser.GotoStart();
        while (demoParser.CurrentTick != tickIndex && !cancellationToken.IsCancellationRequested)
        {
            demoParser.ParseNextTick();
            if (demoParser.CurrentTick > tickIndex)
                demoParser.GotoStart();
        }
        AddListeners(demoParser);
    }
    private void UpdateSeekBar()
    {
        float seekPercent = (float)tickIndex / demoParser.Header.PlaybackFrames;
        seekBar.value = seekPercent;
        float currentSeekPercent = (float)demoParser.CurrentTick / demoParser.Header.PlaybackFrames;
        currentSeekImage.fillAmount = currentSeekPercent;

        int currentTimeInSeconds = Mathf.FloorToInt(demoParser.Header.PlaybackTime * seekPercent);
        int seconds;
        int minutes;
        int hours;
        SecondsToHMS(currentTimeInSeconds, out seconds, out minutes, out hours);
        currentTimeText.text = string.Format("{0}:{1:00}:{2:00}", hours, minutes, seconds);
        SecondsToHMS(Mathf.FloorToInt(demoParser.Header.PlaybackTime), out seconds, out minutes, out hours);
        totalTimeText.text = string.Format("{0}:{1:00}:{2:00}", hours, minutes, seconds);
    }
    private void UpdateRoundsText()
    {
        totalRoundsText.text = "/" + currentMatch.extraMatchStats.roundStartTicks.Count;
        int currentRoundIndex = currentMatch.extraMatchStats.roundStartTicks.FindLastIndex((roundTick) => tickIndex >= roundTick);
        currentRoundIndex = currentRoundIndex > -1 ? currentRoundIndex + 1 : 0;
        currentRoundText.text = currentRoundIndex.ToString();

        team1ScoreText.text = demoParser.CTScore.ToString();
        team2ScoreText.text = demoParser.TScore.ToString();
    }
    public static void SecondsToHMS(int totalSeconds, out int seconds, out int minutes, out int hours)
    {
        seconds = totalSeconds % 60;
        minutes = totalSeconds / 60 % 60;
        hours = totalSeconds / 60 / 60;
    }
    private void UpdateOverviewBlips()
    {
        //mapBlips.ReturnAll();

        if (matchOverview != null)
        {
            List<int> keysToRemove = new List<int>();
            foreach (var characterBlip in twoDeeCharacters)
            {
                if (!demoParser.PlayingParticipants.Any(player => player.EntityID == characterBlip.Key))
                {
                    if (characterBlip.Value != null)
                        keysToRemove.Add(characterBlip.Key);
                }
            }
            foreach (int key in keysToRemove)
            {
                mapBlips.Return(twoDeeCharacters[key]);
                twoDeeCharacters[key] = null;
            }

            float overviewRotation = defaultOverviewRotation;
            if (matchOverview.values.ContainsKey(OverviewData.Values.rotate))
                overviewRotation += matchOverview.values[OverviewData.Values.rotate];

            foreach (Player player in demoParser.PlayingParticipants)
            {
                OverviewCharacter characterBlip = null;
                if (!twoDeeCharacters.ContainsKey(player.EntityID))
                    twoDeeCharacters[player.EntityID] = null;

                characterBlip = twoDeeCharacters[player.EntityID];

                if (player.IsAlive)
                {

                    if (characterBlip == null)
                        characterBlip = mapBlips.Get();
                    twoDeeCharacters[player.EntityID] = characterBlip;

                    Vector2 unitPosition = ConvertToOverviewPosition(player.Position, matchOverview);
                    Vector3 position = new Vector2(unitPosition.x * overviewObject.SelfRectTransform.sizeDelta.x, unitPosition.y * overviewObject.SelfRectTransform.sizeDelta.y);
                    position = Quaternion.Euler(0, 0, overviewRotation) * position;
                    //position += Vector3.forward * player.EntityID;
                    float lookDirection = player.ViewDirectionX + characterRotationXOffset;

                    //OverviewCharacter characterBlip = mapBlips.Get(null, true, position, true);
                    characterBlip.transform.localPosition = position;
                    characterBlip.SetSizeOffset(overviewObject.zoomedAmount * characterZoomScale);
                    characterBlip.SetColor(GetIsHurt(player.EntityID) ? Color.red : (player.Team == Team.CounterTerrorist ? counterTerroristColor : (player.Team == Team.Terrorist ? terroristColor : spectatorColor)));
                    characterBlip.SetRotation(lookDirection);
                    characterBlip.SetName(threeDView ? "" : player.Name);

                    if (!threeDView)
                    {
                        characterBlip.SetGunShotVisibility(GetIsFiring(player.EntityID));
                        if (player.ActiveWeapon != null)
                            characterBlip.SetWeapon(player.ActiveWeapon.Weapon);
                    }
                }
                else
                {
                    if (twoDeeCharacters[player.EntityID] != null)
                    {
                        mapBlips.Return(twoDeeCharacters[player.EntityID]);
                        twoDeeCharacters[player.EntityID] = null;
                    }
                }
            }
        }
    }
    private void UpdateCharacters()
    {
        //charactersPool.ReturnAll();

        #region Return models that are not being used anymore (possibly disconnected or bot or something)
        List<int> keysToRemove = new List<int>();
        foreach (var characterModel in characterModels)
        {
            if (!demoParser.PlayingParticipants.Any(player => player.EntityID == characterModel.Key))
            {
                if (characterModel.Value != null)
                    keysToRemove.Add(characterModel.Key);
            }
        }
        foreach (int key in keysToRemove)
        {
            charactersPool.Return(characterModels[key]);
            characterModels[key] = null;
        }
        #endregion
        //for (int i = 0; i < demoParser.PlayingParticipants.Count(); i++)
        foreach (var player in demoParser.PlayingParticipants)
        {
            //var player = demoParser.PlayingParticipants.ElementAt(i); //I removed the foreach since I got an error that the enumerable was modified during the loop

            MatchCharacter character = null;
            if (!characterModels.ContainsKey(player.EntityID))
                characterModels[player.EntityID] = null;

            character = characterModels[player.EntityID];

            if (player.IsAlive)
            {
                if (character == null)
                    character = charactersPool.Get();
                characterModels[player.EntityID] = character;

                character.entityId = player.EntityID;

                Vector3 worldPosition = new Vector3(player.Position.X, player.Position.Z, player.Position.Y);
                Vector2 worldDirection = new Vector2(player.ViewDirectionY, -(player.ViewDirectionX + characterRotationXOffset));

                character.SetPosition(worldPosition);
                character.SetColor(GetIsHurt(player.EntityID) ? Color.red : (player.Team == Team.CounterTerrorist ? counterTerroristColor : (player.Team == Team.Terrorist ? terroristColor : spectatorColor)));
                character.SetRotation(worldDirection);
                character.SetCrouching(player.IsDucking);
                character.SetVelocity(new Vector3(player.Velocity.X, player.Velocity.Z, player.Velocity.Y));
                character.SetName(player.Name);

                character.SetGunShotVisibility(GetIsFiring(player.EntityID));
                if (player.ActiveWeapon != null)
                    character.SetWeapon(player.ActiveWeapon.Weapon);

                character.SetAnimationPlaying(play);
            }
            else
            {
                if (characterModels[player.EntityID] != null)
                {
                    charactersPool.Return(characterModels[player.EntityID]);
                    characterModels[player.EntityID] = null;
                }
            }
        }
    }
    private void CheckEntityHasTemp(int entityId)
    {
        if (!playerTempValues.ContainsKey(entityId))
            playerTempValues[entityId] = new TempPlayerInfo(entityId);
    }
    private bool GetIsFiring(int entityId)
    {
        bool isFiring = false;
        if (playerTempValues.ContainsKey(entityId))
            isFiring = playerTempValues[entityId].isFiring;
        return isFiring;
    }
    private void SetIsFiring(int entityId)
    {
        CheckEntityHasTemp(entityId);
        playerTempValues[entityId].isFiring = true;
    }
    private bool GetIsHurt(int entityId)
    {
        bool isHurt = false;
        if (playerTempValues.ContainsKey(entityId))
            isHurt = playerTempValues[entityId].isHurt;
        return isHurt;
    }
    private void SetIsHurt(int entityId)
    {
        CheckEntityHasTemp(entityId);
        playerTempValues[entityId].isHurt = true;
    }
    public static Vector2 ConvertToOverviewPosition(Vector characterPosition, OverviewData overview)
    {
        float overviewScale = overview.values[OverviewData.Values.scale];
        Vector2 topLeftMapCornerPosition = new Vector2(overview.values[OverviewData.Values.pos_x], overview.values[OverviewData.Values.pos_y]);

        Vector2 unitPosition = ((topLeftMapCornerPosition - new Vector2(characterPosition.X, characterPosition.Y)) / overviewScale / 1024) - new Vector2(-0.5f, 0.5f);
        return unitPosition;
    }
    public static void OpenMatch(MatchInfo match, bool twoDee)
    {
        matchPlayerInScene.CloseCurrentMatch();
        matchPlayerInScene.SetCurrentMatch(match, !twoDee);

        Screen.sleepTimeout = SleepTimeout.NeverSleep;

        //Doozy.Engine.GameEventMessage.SendEvent("GotoOverview");
        //Doozy.Engine.GameEventMessage.SendEvent("GotoExploreMap");

        ShowControls();
    }
    public static void ShowControls()
    {
        matchPlayerInScene.controlsToggledTime = Time.time;
        matchPlayerInScene.playbackControlsView.Show();
        #if UNITY_ANDROID
        //Screen.fullScreen = false;
        #endif
    }
    public static void HideControls()
    {
        matchPlayerInScene.playbackControlsView.Hide();
        #if UNITY_ANDROID
        //Screen.fullScreen = true;
        #endif
    }
    private void SetCurrentMatch(MatchInfo match, bool threeD = false)
    {
        currentMatch = match;
        using (FileStream fs = new FileStream(match.GetMatchFilePath(), FileMode.Open, FileAccess.Read))
            demoParser = new DemoParser(fs);
        demoParser.ParseHeader();
        secondsPerTick = matchPlayerInScene.demoParser.TickTime;

        //demoParser.ParseToEnd(false, false, false);
        //demoParser.GotoTick(0);
        tickIndex = 0;

        AddListeners(demoParser);

        matchMap = MapData.FindOrCreateMap(demoParser.Header.MapName);
        MapLoaderController.mapLoaderInScene.SetCurrentMap(matchPlayerInScene.matchMap);
        if (matchMap != null && matchMap.IsOverviewAvailable())
            matchOverview = matchMap.GetOverviewData();

        threeDView = threeD;
    }
    public void CloseCurrentMatch()
    {
        Screen.sleepTimeout = SleepTimeout.SystemSetting;
        CancelSeek();
        DisposeDemoParser();
        currentMatch = null;
        matchMap = null;
        matchOverview = null;
        winText.gameObject.SetActive(false);
        playerTempValues.Clear();
        HideControls();
        //playbackControlsView.Hide();
        mapBlips.ReturnAll();
        charactersPool.ReturnAll();
        characterModels.Clear();

        #if UNITY_ANDROID
        //Screen.fullScreen = false;
        #endif
    }
    private void DisposeDemoParser()
    {
        if (demoParser != null)
        {
            RemoveListeners(demoParser);
            demoParser.Dispose();
            demoParser = null;
        }
    }

    public static int SecondsToTicks(float seconds)
    {
        return Mathf.RoundToInt(seconds / matchPlayerInScene.secondsPerTick);
    }
    private void AddListeners(DemoParser currentDemoParser)
    {
        currentDemoParser.PlayerBind += DemoParser_PlayerBind;
        currentDemoParser.WeaponFired += CurrentDemoParser_WeaponFired;
        currentDemoParser.PlayerHurt += CurrentDemoParser_PlayerHurt;
        currentDemoParser.PlayerKilled += CurrentDemoParser_PlayerKilled;
        currentDemoParser.PlayerDisconnect += CurrentDemoParser_PlayerDisconnect;
        currentDemoParser.PlayerTeam += CurrentDemoParser_PlayerTeam;
        currentDemoParser.BotTakeOver += CurrentDemoParser_BotTakeOver;

        currentDemoParser.MatchStarted += CurrentDemoParser_MatchStarted;
        currentDemoParser.RoundAnnounceMatchStarted += CurrentDemoParser_RoundAnnounceMatchStarted;
        currentDemoParser.RoundStart += CurrentDemoParser_RoundStart;
        currentDemoParser.RoundEnd += CurrentDemoParser_RoundEnd;
        currentDemoParser.RoundOfficiallyEnd += CurrentDemoParser_RoundOfficiallyEnd;
        currentDemoParser.LastRoundHalf += CurrentDemoParser_LastRoundHalf;
        currentDemoParser.FreezetimeEnded += CurrentDemoParser_FreezetimeEnded;
        currentDemoParser.RoundMVP += CurrentDemoParser_RoundMVP;
        currentDemoParser.RoundFinal += CurrentDemoParser_RoundFinal;

        currentDemoParser.BombBeginDefuse += CurrentDemoParser_BombBeginDefuse;
        currentDemoParser.BombBeginPlant += CurrentDemoParser_BombBeginPlant;
        currentDemoParser.BombAbortDefuse += CurrentDemoParser_BombAbortDefuse;
        currentDemoParser.BombAbortPlant += CurrentDemoParser_BombAbortPlant;
        currentDemoParser.BombDefused += CurrentDemoParser_BombDefused;
        currentDemoParser.BombPlanted += CurrentDemoParser_BombPlanted;
        currentDemoParser.BombExploded += CurrentDemoParser_BombExploded;

        currentDemoParser.Blind += CurrentDemoParser_Blind;
        currentDemoParser.DecoyNadeEnded += CurrentDemoParser_DecoyNadeEnded;
        currentDemoParser.DecoyNadeStarted += CurrentDemoParser_DecoyNadeStarted;
        currentDemoParser.ExplosiveNadeExploded += CurrentDemoParser_ExplosiveNadeExploded;
        currentDemoParser.FireNadeEnded += CurrentDemoParser_FireNadeEnded;
        currentDemoParser.FireNadeStarted += CurrentDemoParser_FireNadeStarted;
        currentDemoParser.FireNadeWithOwnerStarted += CurrentDemoParser_FireNadeWithOwnerStarted;
        currentDemoParser.FlashNadeExploded += CurrentDemoParser_FlashNadeExploded;
        currentDemoParser.NadeReachedTarget += CurrentDemoParser_NadeReachedTarget;
        currentDemoParser.SmokeNadeEnded += CurrentDemoParser_SmokeNadeEnded;
        currentDemoParser.SmokeNadeStarted += CurrentDemoParser_SmokeNadeStarted;
    }
    private void RemoveListeners(DemoParser currentDemoParser)
    {
        currentDemoParser.PlayerBind -= DemoParser_PlayerBind;
        currentDemoParser.WeaponFired -= CurrentDemoParser_WeaponFired;
        currentDemoParser.PlayerHurt -= CurrentDemoParser_PlayerHurt;
        currentDemoParser.PlayerKilled -= CurrentDemoParser_PlayerKilled;
        currentDemoParser.PlayerDisconnect -= CurrentDemoParser_PlayerDisconnect;
        currentDemoParser.PlayerTeam -= CurrentDemoParser_PlayerTeam;
        currentDemoParser.BotTakeOver -= CurrentDemoParser_BotTakeOver;

        currentDemoParser.MatchStarted -= CurrentDemoParser_MatchStarted;
        currentDemoParser.RoundAnnounceMatchStarted -= CurrentDemoParser_RoundAnnounceMatchStarted;
        currentDemoParser.RoundStart -= CurrentDemoParser_RoundStart;
        currentDemoParser.RoundEnd -= CurrentDemoParser_RoundEnd;
        currentDemoParser.RoundOfficiallyEnd -= CurrentDemoParser_RoundOfficiallyEnd;
        currentDemoParser.LastRoundHalf -= CurrentDemoParser_LastRoundHalf;
        currentDemoParser.FreezetimeEnded -= CurrentDemoParser_FreezetimeEnded;
        currentDemoParser.RoundMVP -= CurrentDemoParser_RoundMVP;
        currentDemoParser.RoundFinal -= CurrentDemoParser_RoundFinal;

        currentDemoParser.BombBeginDefuse -= CurrentDemoParser_BombBeginDefuse;
        currentDemoParser.BombBeginPlant -= CurrentDemoParser_BombBeginPlant;
        currentDemoParser.BombAbortDefuse -= CurrentDemoParser_BombAbortDefuse;
        currentDemoParser.BombAbortPlant -= CurrentDemoParser_BombAbortPlant;
        currentDemoParser.BombDefused -= CurrentDemoParser_BombDefused;
        currentDemoParser.BombPlanted -= CurrentDemoParser_BombPlanted;
        currentDemoParser.BombExploded -= CurrentDemoParser_BombExploded;

        currentDemoParser.Blind -= CurrentDemoParser_Blind;
        currentDemoParser.DecoyNadeEnded -= CurrentDemoParser_DecoyNadeEnded;
        currentDemoParser.DecoyNadeStarted -= CurrentDemoParser_DecoyNadeStarted;
        currentDemoParser.ExplosiveNadeExploded -= CurrentDemoParser_ExplosiveNadeExploded;
        currentDemoParser.FireNadeEnded -= CurrentDemoParser_FireNadeEnded;
        currentDemoParser.FireNadeStarted -= CurrentDemoParser_FireNadeStarted;
        currentDemoParser.FireNadeWithOwnerStarted -= CurrentDemoParser_FireNadeWithOwnerStarted;
        currentDemoParser.FlashNadeExploded -= CurrentDemoParser_FlashNadeExploded;
        currentDemoParser.NadeReachedTarget -= CurrentDemoParser_NadeReachedTarget;
        currentDemoParser.SmokeNadeEnded -= CurrentDemoParser_SmokeNadeEnded;
        currentDemoParser.SmokeNadeStarted -= CurrentDemoParser_SmokeNadeStarted;
    }

    #region Player Events
    private void DemoParser_PlayerBind(object sender, PlayerBindEventArgs e)
    {
        SteamController.LogToConsole((e.Player != null ? e.Player.Name : "Nobody") + " connected");
    }
    private void CurrentDemoParser_WeaponFired(object sender, WeaponFiredEventArgs e)
    {
        SteamController.LogToConsole((e.Shooter != null ? e.Shooter.Name : "Nobody") + " fired " + (e.Weapon != null ? e.Weapon.OriginalString : "nothing"));
        if (e.Shooter != null)
            SetIsFiring(e.Shooter.EntityID);
    }
    private void CurrentDemoParser_PlayerHurt(object sender, PlayerHurtEventArgs e)
    {
        SteamController.LogToConsole((e.Player != null ? e.Player.Name : "Nobody") + " was hurt by " + (e.Attacker != null ? e.Attacker.Name : "nobody") + " using " + (e.Weapon != null ? e.Weapon.OriginalString : "nothing") + " and took " + e.HealthDamage + " hp damage and " + e.ArmorDamage + " armor damage");
        if (e.Player != null)
            SetIsHurt(e.Player.EntityID);
    }
    private void CurrentDemoParser_PlayerKilled(object sender, PlayerKilledEventArgs e)
    {
        SteamController.LogToConsole((e.Killer != null ? e.Killer.Name : "Nobody") + (e.Assister != null ? " and " + e.Assister.Name : "") + " killed " + (e.Victim != null ? e.Victim.Name : "nobody"));
    }
    private void CurrentDemoParser_PlayerDisconnect(object sender, PlayerDisconnectEventArgs e)
    {
        SteamController.LogToConsole((e.Player != null ? e.Player.Name : "Nobody") + " disconnected");
    }
    private void CurrentDemoParser_PlayerTeam(object sender, PlayerTeamEventArgs e)
    {
        SteamController.LogToConsole((e.IsBot ? "Bot" : (e.Swapped != null ? e.Swapped.Name : "Nobody")) + " left the " + e.OldTeam + " and joined the " + e.NewTeam + " forces");
    }
    private void CurrentDemoParser_BotTakeOver(object sender, BotTakeOverEventArgs e)
    {
        SteamController.LogToConsole((e.Taker != null ? e.Taker.Name : "Nobody") + " took over a bot");
    }
    #endregion
    #region Round Events
    private void CurrentDemoParser_MatchStarted(object sender, MatchStartedEventArgs e)
    {
        SteamController.LogToConsole("Match started");
    }
    private void CurrentDemoParser_RoundAnnounceMatchStarted(object sender, RoundAnnounceMatchStartedEventArgs e)
    {
        SteamController.LogToConsole("Match start announced");
    }
    private void CurrentDemoParser_RoundStart(object sender, RoundStartedEventArgs e)
    {
        SteamController.LogToConsole(e.Objective + " round started. " + e.TimeLimit + " second(s) to round end");
    }
    private void CurrentDemoParser_RoundEnd(object sender, RoundEndedEventArgs e)
    {
        SteamController.LogToConsole("Round ended. " + e.Winner + " forces won the round. " + e.Reason + ". " + e.Message + ".");

        if (e.Winner != Team.Spectate)
        {
            string teamName = e.Winner == Team.CounterTerrorist ? "Counter-Terrorists" : "Terrorists";
            ShowWinText(teamName + "\n" + "Win");
        }
    }
    private void CurrentDemoParser_RoundOfficiallyEnd(object sender, RoundOfficiallyEndedEventArgs e)
    {
        SteamController.LogToConsole("Official round end");
    }
    private void CurrentDemoParser_LastRoundHalf(object sender, LastRoundHalfEventArgs e)
    {
        SteamController.LogToConsole("Last round of the half");
    }
    private void CurrentDemoParser_FreezetimeEnded(object sender, FreezetimeEndedEventArgs e)
    {
        SteamController.LogToConsole("Freeze time ended");
    }
    private void CurrentDemoParser_RoundMVP(object sender, RoundMVPEventArgs e)
    {
        SteamController.LogToConsole((e.Player != null ? e.Player.Name : "Nobody") + " is the MVP of the round. " + e.Reason + ".");
    }
    private void CurrentDemoParser_RoundFinal(object sender, RoundFinalEventArgs e)
    {
        SteamController.LogToConsole("Last round of the match");
    }
    #endregion
    #region Bomb Events
    private void CurrentDemoParser_BombExploded(object sender, BombEventArgs e)
    {
        SteamController.LogToConsole((e.Player != null ? e.Player.Name : "Nobody") + " blew up the bomb at " + e.Site + " site");
    }
    private void CurrentDemoParser_BombPlanted(object sender, BombEventArgs e)
    {
        SteamController.LogToConsole((e.Player != null ? e.Player.Name : "Nobody") + " planted the bomb at " + e.Site + " site");
    }
    private void CurrentDemoParser_BombDefused(object sender, BombEventArgs e)
    {
        SteamController.LogToConsole((e.Player != null ? e.Player.Name : "Nobody") + " defused the bomb at " + e.Site + " site");
    }
    private void CurrentDemoParser_BombAbortPlant(object sender, BombEventArgs e)
    {
        SteamController.LogToConsole((e.Player != null ? e.Player.Name : "Nobody") + " stopped planting the bomb at " + e.Site + " site");
    }
    private void CurrentDemoParser_BombAbortDefuse(object sender, BombDefuseEventArgs e)
    {
        SteamController.LogToConsole((e.Player != null ? e.Player.Name : "Nobody") + " stopped defusing the bomb" + (e.HasKit ? " with a kit" : ""));
    }
    private void CurrentDemoParser_BombBeginPlant(object sender, BombEventArgs e)
    {
        SteamController.LogToConsole((e.Player != null ? e.Player.Name : "Nobody") + " started planting the bomb at " + e.Site + " site");
    }
    private void CurrentDemoParser_BombBeginDefuse(object sender, BombDefuseEventArgs e)
    {
        SteamController.LogToConsole((e.Player != null ? e.Player.Name : "Nobody") + " started defusing the bomb" + (e.HasKit ? " with a kit" : ""));
    }
    #endregion
    #region Nade Events
    private void CurrentDemoParser_SmokeNadeStarted(object sender, SmokeEventArgs e)
    {
        SteamController.LogToConsole(e.NadeType + " that was thrown by " + (e.ThrownBy != null ? e.ThrownBy.Name : "nobody") + " started at " + e.Position);
    }
    private void CurrentDemoParser_SmokeNadeEnded(object sender, SmokeEventArgs e)
    {
        SteamController.LogToConsole(e.NadeType + " that was thrown by " + (e.ThrownBy != null ? e.ThrownBy.Name : "nobody") + " ended at " + e.Position);
    }
    private void CurrentDemoParser_NadeReachedTarget(object sender, NadeEventArgs e)
    {
        SteamController.LogToConsole(e.NadeType + " that was thrown by " + (e.ThrownBy != null ? e.ThrownBy.Name : "nobody") + " reached it's target at " + e.Position);
    }
    private void CurrentDemoParser_FlashNadeExploded(object sender, FlashEventArgs e)
    {
        string playersEffected = "";
        if (e.FlashedPlayers != null)
            foreach (var player in e.FlashedPlayers)
                playersEffected += " " + (player != null ? player.Name : "nobody");
        SteamController.LogToConsole(e.NadeType + " that was thrown by " + (e.ThrownBy != null ? e.ThrownBy.Name : "nobody") + " exploded at " + e.Position + " and affected" + playersEffected);
    }
    private void CurrentDemoParser_FireNadeWithOwnerStarted(object sender, FireEventArgs e)
    {
        SteamController.LogToConsole(e.NadeType + " that was thrown by " + (e.ThrownBy != null ? e.ThrownBy.Name : "nobody") + " started at " + e.Position);
    }
    private void CurrentDemoParser_FireNadeStarted(object sender, FireEventArgs e)
    {
        SteamController.LogToConsole(e.NadeType + " that was thrown by " + (e.ThrownBy != null ? e.ThrownBy.Name : "nobody") + " fake started at " + e.Position);
    }
    private void CurrentDemoParser_FireNadeEnded(object sender, FireEventArgs e)
    {
        SteamController.LogToConsole(e.NadeType + " that was thrown by " + (e.ThrownBy != null ? e.ThrownBy.Name : "nobody") + " ended at " + e.Position);
    }
    private void CurrentDemoParser_ExplosiveNadeExploded(object sender, GrenadeEventArgs e)
    {
        SteamController.LogToConsole(e.NadeType + " that was thrown by " + (e.ThrownBy != null ? e.ThrownBy.Name : "nobody") + " exploded at " + e.Position);
    }
    private void CurrentDemoParser_DecoyNadeStarted(object sender, DecoyEventArgs e)
    {
        SteamController.LogToConsole(e.NadeType + " that was thrown by " + (e.ThrownBy != null ? e.ThrownBy.Name : "nobody") + " started at " + e.Position);
    }
    private void CurrentDemoParser_DecoyNadeEnded(object sender, DecoyEventArgs e)
    {
        SteamController.LogToConsole(e.NadeType + " that was thrown by " + (e.ThrownBy != null ? e.ThrownBy.Name : "nobody") + " ended at " + e.Position);
    }
    private void CurrentDemoParser_Blind(object sender, BlindEventArgs e)
    {
        SteamController.LogToConsole((e.Player != null ? e.Player.Name : "Nobody") + " got flashed by " + (e.Attacker != null ? e.Attacker.Name : "nobody") + " for " + e.FlashDuration + " second(s)");
    }
    #endregion
}

public class TempPlayerInfo
{
    public int entityId;
    public bool isFiring;
    public bool isHurt;

    public TempPlayerInfo(int _entityId)
    {
        entityId = _entityId;
    }
    public void ResetValues()
    {
        isFiring = false;
        isHurt = false;
    }
}