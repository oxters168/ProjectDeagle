using UnityEngine;
using System.Collections;

public class CameraControl : MonoBehaviour {

    public Transform target;
    public Vector3 defaultPosition = new Vector3(0, 5000, 0);
    public Quaternion defaultRotation = Quaternion.Euler(90, 0, 0);
    public float distance = 50f;
    public float rotationStrength = 0.25f, scrollStrength = 25f;
    public bool blockControl = true, invertX = false, invertY = false, invertZoom = false;
    private bool mouseDown = false;
    private Vector3 previousMouse = Vector3.zero;
    private int firstTouch = -1, secondTouch = -1;
    private float fingerDistance = 0;
    public float movementDeadZone = 1f, zoomDeadZone = 1f;
    private bool moved = false, zoomed = false;

    public Color leftColor = Color.green, rightColor = Color.green, upColor = Color.green, downColor = Color.green;
    public float crosshairLength = 15f, crosshairGap = 3f, crosshairThickness = 2f;

	void Start ()
    {
        GoToDefault();
	}
	
	void LateUpdate ()
    {
        float horizontal = 0, vertical = 0, scroll = 0;

        if (!blockControl)
        {
            #region Touch Controls
            if (Input.touches.Length >= 2)
            {
                Vector2 firstPosition = Input.touches[0].position;
                Vector2 secondPosition = Input.touches[1].position;

                if (firstTouch < 0 || secondTouch < 0 || firstTouch != Input.touches[0].fingerId || secondTouch != Input.touches[1].fingerId)
                {
                    firstTouch = Input.touches[0].fingerId;
                    secondTouch = Input.touches[1].fingerId;
                    fingerDistance = (firstPosition - secondPosition).magnitude;
                    zoomed = false;
                }
                else
                {
                    if (zoomed || (firstPosition - secondPosition).magnitude - fingerDistance > zoomDeadZone)
                    {
                        scroll += (firstPosition - secondPosition).magnitude - fingerDistance;
                        fingerDistance = (firstPosition - secondPosition).magnitude;
                        zoomed = true;
                    }
                }
            }
            else if (Input.touches.Length >= 1)
            {
                if (firstTouch < 0)
                {
                    firstTouch = Input.touches[0].fingerId;
                    previousMouse = new Vector3(Input.touches[0].position.x, Input.touches[0].position.y, 0);
                    moved = false;
                }
                if (firstTouch > -1)
                {
                    if (moved || Mathf.Abs(Input.touches[0].position.x - previousMouse.x) > movementDeadZone || Mathf.Abs(Input.touches[0].position.y - previousMouse.y) > movementDeadZone)
                    {
                        horizontal = Input.touches[0].position.x - previousMouse.x;
                        vertical = previousMouse.y - Input.touches[0].position.y;
                        previousMouse = new Vector3(Input.touches[0].position.x, Input.touches[0].position.y, 0);
                        moved = true;
                    }
                }
            }
            else { firstTouch = -1; secondTouch = -1; }
            #endregion

            #region Mouse Controls
            if (Input.touches.Length <= 0)
            {
                if (!mouseDown && Input.GetMouseButton(0))
                {
                    mouseDown = true;
                    previousMouse = Input.mousePosition;
                }
                if (!Input.GetMouseButton(0)) mouseDown = false;
                if (mouseDown)
                {
                    horizontal = Input.mousePosition.x - previousMouse.x;
                    vertical = previousMouse.y - Input.mousePosition.y;
                    previousMouse = Input.mousePosition;
                }
            }
            if (target == null) scroll += Input.mouseScrollDelta.y;
            else scroll += -Input.mouseScrollDelta.y;
            #endregion
        }
        #region Add Buffs
        if (invertZoom) scroll *= -1;
        if (invertX) horizontal *= -1;
        if (invertY) vertical *= -1;
        scroll *= scrollStrength;
        horizontal *= rotationStrength;
        vertical *= rotationStrength;
        #endregion

        if (target != null)
        {
            CSGOPlayer targetPlayer = target.GetComponent<CSGOPlayer>();

            if (distance + scroll <= 0) distance = 0;
            else distance += scroll;

            #region Third Person View
            if (distance > 0)
            {
                if (targetPlayer != null && targetPlayer.chestPosition != null)
                {
                    Camera.main.transform.rotation *= Quaternion.AngleAxis(horizontal, Vector3.up) * Quaternion.AngleAxis(vertical, Vector3.right);
                    Camera.main.transform.position = targetPlayer.chestPosition.position - (Camera.main.transform.forward * distance);
                    Camera.main.transform.rotation = Quaternion.LookRotation((targetPlayer.chestPosition.position - Camera.main.transform.position).normalized);
                }
                else
                {
                    Camera.main.transform.rotation *= Quaternion.AngleAxis(horizontal, Vector3.up) * Quaternion.AngleAxis(vertical, Vector3.right);
                    Camera.main.transform.position = target.position - (Camera.main.transform.forward * distance);
                    Camera.main.transform.rotation = Quaternion.LookRotation((target.position - Camera.main.transform.position).normalized);
                }
            }
            #endregion
            #region First Person View
            else
            {
                if (targetPlayer != null)
                {
                    //Vector3 steadyPosition = new Vector3(targetPlayer.headPosition.position.x, 0, targetPlayer.headPosition.position.z);
                    //Quaternion steadyRotation = Quaternion.Euler(targetPlayer.headPosition.eulerAngles.x, targetPlayer.headPosition.eulerAngles.y, 0);

                    Camera.main.transform.position = targetPlayer.transform.position + (targetPlayer.transform.forward * targetPlayer.aimPosition.z) + (targetPlayer.transform.up * targetPlayer.aimPosition.y);
                    //Camera.main.transform.position = targetPlayer.transform.position + targetPlayer.aimPosition;
                    Camera.main.transform.rotation = Quaternion.Euler(targetPlayer.aimDirection.y - 90f, targetPlayer.aimDirection.x, 0);
                }
                else
                {
                    Camera.main.transform.position = target.position;
                    Camera.main.transform.rotation = target.rotation;
                }
            }
            #endregion
        }
        else
        {
            Camera.main.transform.rotation = (Quaternion.AngleAxis(horizontal, Vector3.up) * Camera.main.transform.rotation) * Quaternion.AngleAxis(vertical, Vector3.right);
            Camera.main.transform.position += Camera.main.transform.forward * scroll;
        }
	}

    void OnGUI()
    {
        if (target != null)
        {
            GUIStyle blackFont = new GUIStyle(), whiteFont = new GUIStyle();
            blackFont.normal.textColor = Color.black;
            whiteFont.normal.textColor = Color.white;
            blackFont.fontSize = 48;
            whiteFont.fontSize = 48;
            blackFont.alignment = TextAnchor.MiddleCenter;
            whiteFont.alignment = TextAnchor.MiddleCenter;
            blackFont.clipping = TextClipping.Overflow;
            whiteFont.clipping = TextClipping.Overflow;

            GUI.Label(new Rect((Screen.width / 2f) + 1f, 1f, 0, 0), target.name, blackFont);
            GUI.Label(new Rect((Screen.width / 2f), 0, 0, 0), target.name, whiteFont);

            if(distance <= 0) DrawCrosshair();
        }
    }

    public void DrawCrosshair()
    {
        //float pratio = OxGUI.GetPratio(Screen.width, Screen.height, 0.25f, 0.25f, 0.1f, 0.1f);
        //float crosshairWidth = pratio * 0.25f;
        //float crosshairHeight = pratio * 0.25f;
        //float crosshairGap = crosshairWidth * (crosshairGapMultiplier / 100f);
        //float crosshairThickness = crosshairWidth * (crosshairThicknessMultiplier / 100f), crosshairLineHeight = crosshairHeight * (crosshairThicknessMultiplier / 100f);

        GUIStyle crosshairStyle = new GUIStyle();
        Texture2D tempTexture = new Texture2D(1, 1);

        tempTexture.SetPixel(0, 0, leftColor);
        tempTexture.Apply();
        crosshairStyle.normal.background = tempTexture;
        GUI.Label(new Rect((Screen.width / 2f) - crosshairLength - crosshairGap, (Screen.height / 2f) - (crosshairThickness / 2f), crosshairLength, crosshairThickness), "", crosshairStyle); //Left
        tempTexture.SetPixel(0, 0, upColor);
        tempTexture.Apply();
        crosshairStyle.normal.background = tempTexture;
        GUI.Label(new Rect((Screen.width / 2f) - (crosshairThickness / 2f), (Screen.height / 2f) - crosshairLength - crosshairGap, crosshairThickness, crosshairLength), "", crosshairStyle); //Top
        tempTexture.SetPixel(0, 0, rightColor);
        tempTexture.Apply();
        crosshairStyle.normal.background = tempTexture;
        GUI.Label(new Rect((Screen.width / 2f) + crosshairGap, (Screen.height / 2f) - (crosshairThickness / 2f), crosshairLength, crosshairThickness), "", crosshairStyle); //Right
        tempTexture.SetPixel(0, 0, downColor);
        tempTexture.Apply();
        crosshairStyle.normal.background = tempTexture;
        GUI.Label(new Rect((Screen.width / 2f) - (crosshairThickness / 2f), (Screen.height / 2f) + crosshairGap, crosshairThickness, crosshairLength), "", crosshairStyle); //Bottom
    }

    public void GoToDefault()
    {
        transform.position = defaultPosition;
        transform.rotation = defaultRotation;
    }
}
