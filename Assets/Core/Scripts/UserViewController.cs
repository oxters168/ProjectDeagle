using UnityEngine;
using Rewired;
using UnityHelpers;
using System.Linq;

[RequireComponent(typeof(Camera))]
public class UserViewController : MonoBehaviour
{
    public enum ViewMode { firstPerson, thirdPerson, freeLook }
    public ViewMode currentMode { get; private set; }
    private MatchCharacter currentTarget;

    public TMPro.TextMeshProUGUI playerNameLabel;

    public MatchPlayer matchPlayer;
    public Camera viewingCamera;
    public BaseCameraController currentCameraController;
    public FreeLookCameraController freeLookController;
    public OrbitCameraController orbitController;
    public FirstPersonCameraController firstPersonController;
    public int playerId;
    private Player player;

    public float shiftMinWaitTime = 0.4f;
    private float lastShiftTime;


    private void Awake()
    {
        player = ReInput.players.GetPlayer(playerId);
    }
    private void OnEnable()
    {
        orbitController.shiftLeft += OrbitController_shiftLeft;
        orbitController.shiftRight += OrbitController_shiftRight;
        firstPersonController.shiftLeft += OrbitController_shiftLeft;
        firstPersonController.shiftRight += OrbitController_shiftRight;

        if (playerNameLabel != null)
        {
            playerNameLabel.text = "";
            playerNameLabel.gameObject.SetActive(true);
        }
    }
    private void OnDisable()
    {
        orbitController.shiftLeft -= OrbitController_shiftLeft;
        orbitController.shiftRight -= OrbitController_shiftRight;
        firstPersonController.shiftLeft -= OrbitController_shiftLeft;
        firstPersonController.shiftRight -= OrbitController_shiftRight;

        if (playerNameLabel != null)
        {
            playerNameLabel.gameObject.SetActive(false);
            playerNameLabel.text = "";
        }
    }

    private void FixedUpdate()
    {
        if (currentTarget == null || !currentTarget.gameObject.activeSelf)
            ShiftTarget(true);

        SetCamera();
        ApplyInput();
    }

    private void OrbitController_shiftRight()
    {
        if (Time.time - lastShiftTime > shiftMinWaitTime)
        {
            ShiftTarget(true);
            lastShiftTime = Time.time;
        }
    }
    private void OrbitController_shiftLeft()
    {
        if (Time.time - lastShiftTime > shiftMinWaitTime)
        {
            ShiftTarget(false);
            lastShiftTime = Time.time;
        }
    }
    public void ShiftTarget(bool next)
    {
        var currentPlayers = matchPlayer.GetPlayers();
        if (currentPlayers != null && currentPlayers.Count > 0)
        {
            currentPlayers.Sort((player1, player2) => player1.entityId - player2.entityId);
            if (currentTarget == null)
                SetTarget(currentPlayers.First());
            else
            {
                int index = currentPlayers.FindIndex(player => currentTarget.entityId == player.entityId);
                if (index > -1)
                {
                    index = (index + (next ? 1 : -1) + currentPlayers.Count) % currentPlayers.Count;
                    SetTarget(currentPlayers[index]);
                }
                else
                {
                    index = currentPlayers.FindIndex(player => currentTarget.entityId > player.entityId);
                    if (index > -1)
                        SetTarget(currentPlayers[index]);
                    else
                        SetTarget(currentPlayers.First());
                }
            }
        }
    }
    private void SetTarget(MatchCharacter player)
    {
        if (currentMode == ViewMode.firstPerson)
        {
            currentTarget?.HideFirstPersonModel(true);
            player?.HideFirstPersonModel(false);
        }

        currentTarget = player;
        orbitController.target = currentTarget.transform;
        firstPersonController.target = currentTarget.transform;
        playerNameLabel.text = currentTarget.playerNameLabel.text;
    }

    private void SetCamera()
    {
        if (currentMode == ViewMode.firstPerson)
            currentCameraController = firstPersonController;
        else if (currentMode == ViewMode.thirdPerson)
            currentCameraController = orbitController;
        else
            currentCameraController = freeLookController;

        freeLookController.enabled = currentCameraController == freeLookController;
        orbitController.enabled = currentCameraController == orbitController;
        firstPersonController.enabled = currentCameraController == firstPersonController;
    }
    private void ApplyInput()
    {
        currentCameraController.SetStrafe(player.GetAxis("StrafeHorizontal"));
        currentCameraController.SetPush(player.GetAxis("PushVertical"));
        currentCameraController.SetLookHorizontal(player.GetAxis("LookHorizontal"));
        currentCameraController.SetLookVertical(player.GetAxis("LookVertical"));

        if (currentTarget != null)
            firstPersonController.lookDirection = currentTarget.lookDirection;
    }

    public void NextViewMode()
    {
        int viewModes = System.Enum.GetValues(typeof(ViewMode)).Length;
        SetViewMode((ViewMode)((int)(currentMode + 1) % viewModes));
    }
    public void PrevViewMode()
    {
        int viewModes = System.Enum.GetValues(typeof(ViewMode)).Length;
        SetViewMode((ViewMode)((int)(currentMode + viewModes - 1) % viewModes));
    }

    public void SetViewMode(ViewMode viewMode)
    {
        currentTarget?.HideFirstPersonModel(viewMode != ViewMode.firstPerson);

        currentMode = viewMode;
        SetCamera();

        SettingsController.ShowTouchGuide(viewMode);
    }
}
