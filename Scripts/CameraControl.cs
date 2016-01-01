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
                }
                else
                {
                    scroll += (firstPosition - secondPosition).magnitude - fingerDistance;
                    fingerDistance = (firstPosition - secondPosition).magnitude;
                }
            }
            else if (Input.touches.Length >= 1)
            {
                if (firstTouch < 0)
                {
                    firstTouch = Input.touches[0].fingerId;
                    previousMouse = new Vector3(Input.touches[0].position.x, Input.touches[0].position.y, 0);
                }
                if (firstTouch > -1)
                {
                    horizontal = Input.touches[0].position.x - previousMouse.x;
                    vertical = previousMouse.y - Input.touches[0].position.y;
                    previousMouse = new Vector3(Input.touches[0].position.x, Input.touches[0].position.y, 0);
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

            if (distance <= 0)
            {
                //GUIStyle crosshairStyle = new GUIStyle();
                float pratio = OxGUI.GetPratio(Screen.width, Screen.height, 1f, 1f, 0.2f, 0.2f);
                float crosshairWidth = pratio * 1f;
                float crosshairHeight = pratio * 1f;
                float crosshairGap = crosshairWidth * (20f / 100f);
                float crosshairLineWidth = crosshairWidth * (10f / 100f), crosshairLineHeight = crosshairHeight * (10f / 100f);
                GUI.Button(new Rect((Screen.width / 2f) - (crosshairWidth / 2f), (Screen.height / 2f) - (crosshairLineHeight / 2f), (crosshairWidth / 2f) - (crosshairGap / 2f), crosshairLineHeight), ""); //Left
                GUI.Button(new Rect((Screen.width / 2f) - (crosshairLineWidth / 2f), (Screen.height / 2f) - (crosshairHeight / 2f), crosshairLineWidth, (crosshairHeight / 2f) - (crosshairGap / 2f)), ""); //Top
                GUI.Button(new Rect((Screen.width / 2f) + (crosshairGap / 2f), (Screen.height / 2f) - (crosshairLineHeight / 2f), (crosshairWidth / 2f) - (crosshairGap / 2f), crosshairLineHeight), ""); //Right
                GUI.Button(new Rect((Screen.width / 2f) - (crosshairLineWidth / 2f), (Screen.height / 2f) + (crosshairGap / 2f), crosshairLineWidth, (crosshairHeight / 2f) - (crosshairGap / 2f)), ""); //Bottom
            }
        }
    }

    public void GoToDefault()
    {
        transform.position = defaultPosition;
        transform.rotation = defaultRotation;
    }
}
