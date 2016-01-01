using UnityEngine;
using System.Collections;

public abstract class OxFrame : OxGUI
{
    protected static float SMALLEST_WINDOW_WIDTH { get { return (GetPratioX(Screen.width, 3f, 0.25f) * 3f); } }
    protected static float SMALLEST_WINDOW_HEIGHT { get { return (GetPratioY(Screen.height, 3f, 0.25f) * 3f); } }
    public OxLayout layout;
    public bool showCloseButton = true, showMaximizeButton = true, showMinimizeButton = true;
    //private List<OxGUI> windowComponents = new List<OxGUI>();
    private OxButton closeButton, minimizeButton, maximizeButton;
    private float barSize, resizerSize;
    private bool draggingWindow, draggingSize;
    private float origX, origY, mouseX, mouseY;

    public OxFrame(Vector2 position, Vector2 size, string texture) : base(position, size, texture)
    {
        //resized += OxWindow_resized;
        //if (items != null) windowComponents.AddRange(items);
        visible = false;
    }

    public override void Draw()
    {
        base.Draw();

        if (visible)
        {
            //barSize = size.x * (48f / 512f);
            //resizerSize = size.x * (24f / 512f);
            barSize = GetPratio(Screen.width, Screen.height, 0.5f, 0.5f, 0.05f, 0.05f) * 0.5f;
            resizerSize = GetPratio(Screen.width, Screen.height, 0.5f, 0.5f, 0.05f, 0.05f) * 0.5f;

            GUIStyle guiStyle = new GUIStyle(GUI.skin.label);
            if (textureLocation.Length > 0)
            {
                guiStyle.normal.background = Resources.Load<Texture2D>(textureLocation + "WindowBackground 512x512");
                guiStyle.hover.background = Resources.Load<Texture2D>(textureLocation + "WindowBackground 512x512");
                guiStyle.active.background = Resources.Load<Texture2D>(textureLocation + "WindowBackground 512x512");
            }
            GUI.Label(new Rect(position.x, position.y, size.x, size.y), "", guiStyle);

            Rect thirdPosition = new Rect(position.x + size.x - (barSize * 3f), position.y, barSize, barSize);
            Rect secondPosition = new Rect(position.x + size.x - (barSize * 2f), position.y, barSize, barSize);
            Rect firstPosition = new Rect(position.x + size.x - (barSize * 1f), position.y, barSize, barSize);

            if (!showCloseButton)
            {
                if (!showMaximizeButton)
                {
                    thirdPosition = firstPosition;
                }
                else
                {
                    thirdPosition = secondPosition;
                    secondPosition = firstPosition;
                }
            }
            else if (!showMaximizeButton)
            {
                thirdPosition = secondPosition;
            }

            guiStyle = new GUIStyle(GUI.skin.button);
            if (showMinimizeButton)
            {
                guiStyle.normal.background = Resources.Load<Texture2D>(textureLocation + "MinimizeButtonUp 512x512");
                guiStyle.hover.background = Resources.Load<Texture2D>(textureLocation + "MinimizeButtonHover 512x512");
                guiStyle.active.background = Resources.Load<Texture2D>(textureLocation + "MinimizeButtonDown 512x512");
                GUI.Button(thirdPosition, "", guiStyle);
            }
            if (showMaximizeButton)
            {
                guiStyle.normal.background = Resources.Load<Texture2D>(textureLocation + "MaximizeButtonUp 512x512");
                guiStyle.hover.background = Resources.Load<Texture2D>(textureLocation + "MaximizeButtonHover 512x512");
                guiStyle.active.background = Resources.Load<Texture2D>(textureLocation + "MaximizeButtonDown 512x512");
                if (GUI.Button(secondPosition, "", guiStyle)) { Reposition(0, 0); Resize(Screen.width, Screen.height); }
            }
            if (showCloseButton)
            {
                guiStyle.normal.background = Resources.Load<Texture2D>(textureLocation + "CloseButtonUp 512x512");
                guiStyle.hover.background = Resources.Load<Texture2D>(textureLocation + "CloseButtonHover 512x512");
                guiStyle.active.background = Resources.Load<Texture2D>(textureLocation + "CloseButtonDown 512x512");
                if (GUI.Button(firstPosition, "", guiStyle)) Hide();
            }

            if (textureLocation.Length > 0)
            {
                guiStyle.normal.background = Resources.Load<Texture2D>(textureLocation + "WindowBar 512x512");
                guiStyle.hover.background = Resources.Load<Texture2D>(textureLocation + "WindowBar 512x512");
                guiStyle.active.background = Resources.Load<Texture2D>(textureLocation + "WindowBar 512x512");
            }
            if (GUI.RepeatButton(new Rect(position.x, position.y, size.x - (barSize * 3f), barSize), "", guiStyle)) { if (!draggingWindow) { draggingWindow = true; origX = position.x; origY = position.y; mouseX = Input.mousePosition.x; mouseY = Input.mousePosition.y; } }
            if (draggingWindow) { Reposition(origX + (Input.mousePosition.x - mouseX), origY + (mouseY - Input.mousePosition.y)); }

            guiStyle.normal.background = Resources.Load<Texture2D>(textureLocation + "WindowResizer 512x512");
            guiStyle.hover.background = Resources.Load<Texture2D>(textureLocation + "WindowResizer 512x512");
            guiStyle.active.background = Resources.Load<Texture2D>(textureLocation + "WindowResizer 512x512");
            if (GUI.RepeatButton(new Rect(position.x + size.x - resizerSize, position.y + size.y - resizerSize, resizerSize, resizerSize), "", guiStyle)) { if (!draggingSize) { draggingSize = true; origX = size.x; origY = size.y; mouseX = Input.mousePosition.x; mouseY = Input.mousePosition.y; } }
            if (draggingSize) { Resize(origX + (Input.mousePosition.x - mouseX), origY + (mouseY - Input.mousePosition.y)); }

            if (!Input.GetMouseButton(0)) { draggingWindow = false; draggingSize = false; }

            if (layout != null)
            {
                layout.Reposition(position.x, position.y + barSize);
                SetSize(layout, size.x, size.y - barSize - barSize);
                layout.Draw();
            }

            //return true;
        }

        //return false;
    }

    public override void Resize(Vector2 newSize)
    {
        //float newWidth = size.x, newHeight = size.y;
        //Vector2 oldSize = new Vector2(size.x, size.y);
        //if (newSize.x + position.x <= Screen.width && newSize.x >= SMALLEST_WIDTH) newWidth = newSize.x;
        //if (newSize.y + position.y <= Screen.height && newSize.y >= SMALLEST_HEIGHT) newHeight = newSize.y;

        SetSize(newSize.x, newSize.y, SMALLEST_WINDOW_WIDTH, SMALLEST_WINDOW_HEIGHT);
        //if (resized != null) resized(size, size - oldSize);
        //windowSize = new Vector2(size.x, size.y);
    }
    public override void Resize(float x, float y) { Resize(new Vector2(x, y)); }
}
