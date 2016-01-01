using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public abstract class OxGUI
{
    public static float screenDpi;
    public static List<OxGUI> elements = new List<OxGUI>();
    public static List<OxFrame> windows = new List<OxFrame>();
    protected bool drew, checkedDrew;
    protected OxGUI checkedBy;
    protected int clippedFontSize = 1, overflowFontSize = 1;

    private static bool upButton, downButton, leftButton, rightButton, pressButton;
    public bool highlighted;
    protected bool mouseOver, pressing;
    private Vector3 origMouse;

    public static float DRAG_DEAD_ZONE;
    protected bool selectable;
    protected static float SMALLEST_WIDTH { get { return (GetPratioX(Screen.width, 0.5f, 0.05f) * 0.5f); } }
    protected static float SMALLEST_HEIGHT { get { return (GetPratioY(Screen.height, 0.5f, 0.05f) * 0.5f); } }
    protected Vector2 position, size;
    public string text;
    public static string textureLocation = "Textures/UI/";
    protected string texture;
    protected bool visible;
    public float opaque = 1f;

    public event PressedEventHandler pressed;
    public event ReleasedEventHandler released;
    public event ClickedEventHandler clicked;
    public event MovedEventHandler moved;
    public event ResizedEventHandler resized;
    public event VisibleChangedEventHandler visibleChanged;

    public OxGUI(Vector2 pos, Vector2 newSize, string textureLocation)
    {
        drew = false;
        checkedDrew = false;
        checkedBy = null;

        mouseOver = false;
        pressing = false;

        selectable = true;
        Reposition(pos);
        Resize(newSize);
        texture = textureLocation;
        visible = true;

        resized += OxGUI_resized;
    }
    public OxGUI(Vector2 pos, Vector2 newSize) : this(pos, newSize, "") { }

    public virtual void Draw()
    {
        screenDpi = Screen.dpi;
        DRAG_DEAD_ZONE = (screenDpi / 200f) * Mathf.Min(Screen.width, Screen.height) * (3f / 100f);

        drew = true;
        checkedDrew = false;
        checkedBy = null;
        //timesDrawn++;
        //if (timesDrawn > maxCount) timesDrawn = 0;
        //currentCount = timesDrawn;
        
        //if (this is OxFrame && windows.IndexOf((OxFrame)this) == -1) windows.Add((OxFrame)this);
        //else if (elements.IndexOf(this) == -1) elements.Add(this);
        //for (int i = windows.Count - 1; i >= 0; i--) { if (this != windows[i]) { if (windows[i].drew) { windows[i].drew = false; windows[i].checkedBy = this; } else if (windows[i].checkedBy == this || windows.IndexOf((OxFrame) windows[i].checkedBy) == -1) { windows.Remove(windows[i]); } } }
        //for (int i = elements.Count - 1; i >= 0; i--) { if (this != elements[i]) { if (elements[i].drew) { elements[i].drew = false; elements[i].checkedBy = this; } else if (elements[i].checkedBy == this || elements.IndexOf(elements[i].checkedBy) == -1) { elements.Remove(elements[i]); } } }
        //Debug.Log(windows.Count + " window(s) and " + elements.Count + " element(s).");

        MouseControls();
        MenuNavigation();

        //this.isDrawing = false;
        //return false;
    }

    public delegate void PressedEventHandler(OxGUI sender);
    public delegate void ReleasedEventHandler(OxGUI sender);
    public delegate void ClickedEventHandler(OxGUI sender);
    private void MouseControls()
    {
        //Debug.Log("Mouse Controls");
        if ((new Rect(position.x, position.y, size.x, size.y)).Contains(new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y)))
        {
            //Debug.Log("Mouse Over");
            mouseOver = true;
            Highlight();
        }
        else if (mouseOver)
        {
            //Debug.Log("Mouse Exit");
            mouseOver = false;
            UnHighlight();
        }
        if (visible && mouseOver && Input.GetMouseButtonDown(0) && !pressing)
        {
            origMouse = Input.mousePosition;
            //Debug.Log("Mouse Down");
            MouseDown();
            //element.pressed = true;
        }
        else if (visible && Input.GetMouseButtonUp(0) && pressing)
        {
            MouseUp();
            //element.pressed = false;
            //Click();
        }
        
    }
    private static void MenuNavigation()
    {
        //if (InputControl.dPad.y > 0) { upButton = true; }
        //else if (InputControl.dPad.y < 0) { downButton = true; }
        //if (InputControl.dPad.x < 0) { leftButton = true; }
        //else if (InputControl.dPad.x > 0) { rightButton = true; }
        //if (InputControl.aButton) { pressButton = true; }

        //if (upButton && InputControl.dPad.y == 0) { NavigateUp(); upButton = false; }
        //if (downButton && InputControl.dPad.y == 0) { NavigateDown(); downButton = false; }
        //if (leftButton && InputControl.dPad.x == 0) { NavigateLeft(); leftButton = false; }
        //if (rightButton && InputControl.dPad.x == 0) { NavigateRight(); rightButton = false; }
    }
    private static void NavigateUp()
    {
        OxGUI nextSelection = NextElementAbove();
        if (nextSelection != null) nextSelection.Highlight();
        //foreach (OxGUI element in elements) { element.highlighted = false; }
        //if(nextSelection != null) nextSelection.highlighted = true;
    }
    private static OxGUI NextElementAbove()
    {
        OxGUI currentlySelected = null;
        bool nothingHighlighted = true;

        foreach (OxGUI element in elements)
        {
            if (element.highlighted) { currentlySelected = element; nothingHighlighted = false; break; }
            if (element.visible && element.selectable && (currentlySelected == null || element.position.y > currentlySelected.position.y)) { currentlySelected = element; }
        }
        if (nothingHighlighted) { return currentlySelected; }

        OxGUI nextPreciseSelection = null, nextSelection = null;
        foreach (OxGUI element in elements)
        {
            if (element != currentlySelected && element.visible && element.selectable && element.position.y < currentlySelected.position.y && ((element.position.x >= currentlySelected.position.x && element.position.x <= currentlySelected.position.x + currentlySelected.size.x) || (element.position.x + element.size.x >= currentlySelected.position.x && element.position.x + element.size.x <= currentlySelected.position.x + currentlySelected.size.x)) && (nextPreciseSelection == null || currentlySelected.position.y - element.position.y < currentlySelected.position.y - nextPreciseSelection.position.y)) { nextPreciseSelection = element; }
            if (element != currentlySelected && element.visible && element.selectable && element.position.y < currentlySelected.position.y && (nextSelection == null || currentlySelected.position.y - element.position.y < currentlySelected.position.y - nextSelection.position.y)) { nextSelection = element; }
        }
        if (nextPreciseSelection != null) nextSelection = nextPreciseSelection;
        return nextSelection;
    }
    private static void NavigateDown()
    {
        OxGUI nextSelection = NextElementBelow();
        if (nextSelection != null) nextSelection.Highlight();
        //foreach (OxGUI element in elements) { element.highlighted = false; }
        //if (nextSelection != null) nextSelection.highlighted = true;
    }
    private static OxGUI NextElementBelow()
    {
        OxGUI currentlySelected = null;
        bool nothingHighlighted = true;

        foreach (OxGUI element in elements)
        {
            if (element.highlighted) { currentlySelected = element; nothingHighlighted = false; break; }
            if (element.visible && element.selectable && (currentlySelected == null || element.position.y < currentlySelected.position.y)) { currentlySelected = element; }
        }
        if (nothingHighlighted) { return currentlySelected; }

        OxGUI nextPreciseSelection = null, nextSelection = null;
        foreach (OxGUI element in elements)
        {
            if (element != currentlySelected && element.visible && element.selectable && element.position.y > currentlySelected.position.y && ((element.position.x >= currentlySelected.position.x && element.position.x <= currentlySelected.position.x + currentlySelected.size.x) || (element.position.x + element.size.x >= currentlySelected.position.x && element.position.x + element.size.x <= currentlySelected.position.x + currentlySelected.size.x)) && (nextPreciseSelection == null || element.position.y - currentlySelected.position.y < nextPreciseSelection.position.y - currentlySelected.position.y)) { nextPreciseSelection = element; }
            if (element != currentlySelected && element.visible && element.selectable && element.position.y > currentlySelected.position.y && (nextSelection == null || element.position.y - currentlySelected.position.y < nextSelection.position.y - currentlySelected.position.y)) { nextSelection = element; }
        }
        if (nextPreciseSelection != null) nextSelection = nextPreciseSelection;
        return nextSelection;
    }
    private static void NavigateLeft()
    {
        OxGUI nextSelection = NextElementLeft();
        if (nextSelection != null) nextSelection.Highlight();
        //foreach (OxGUI element in elements) { element.highlighted = false; }
        //if (nextSelection != null) nextSelection.highlighted = true;
    }
    private static OxGUI NextElementLeft()
    {
        OxGUI currentlySelected = null;
        bool nothingHighlighted = true;

        foreach (OxGUI element in elements)
        {
            if (element.highlighted) { currentlySelected = element; nothingHighlighted = false; break; }
            if (element.visible && element.selectable && (currentlySelected == null || element.position.x > currentlySelected.position.x)) { currentlySelected = element; }
        }
        if (nothingHighlighted) { return currentlySelected; }

        OxGUI nextPreciseSelection = null, nextSelection = null;
        foreach (OxGUI element in elements)
        {
            if (element != currentlySelected && element.visible && element.selectable && element.position.x < currentlySelected.position.x && ((element.position.y >= currentlySelected.position.y && element.position.y <= currentlySelected.position.y + currentlySelected.size.y) || (element.position.y + element.size.y >= currentlySelected.position.y && element.position.y + element.size.y <= currentlySelected.position.y + currentlySelected.size.y)) && (nextPreciseSelection == null || currentlySelected.position.x - element.position.x < currentlySelected.position.x - nextPreciseSelection.position.x)) { nextPreciseSelection = element; }
            if (element != currentlySelected && element.visible && element.selectable && element.position.x < currentlySelected.position.x && (nextSelection == null || currentlySelected.position.x - element.position.x < currentlySelected.position.x - nextSelection.position.x)) { nextSelection = element; }
        }
        if (nextPreciseSelection != null) nextSelection = nextPreciseSelection;
        return nextSelection;
    }
    private static void NavigateRight()
    {
        OxGUI nextSelection = NextElementRight();
        if (nextSelection != null) nextSelection.Highlight();
        //foreach (OxGUI element in elements) { element.highlighted = false; }
        //if (nextSelection != null) nextSelection.highlighted = true;
    }
    private static OxGUI NextElementRight()
    {
        OxGUI currentlySelected = null;
        bool nothingHighlighted = true;

        foreach (OxGUI element in elements)
        {
            if (element.highlighted) { currentlySelected = element; nothingHighlighted = false; break; }
            if (element.visible && element.selectable && (currentlySelected == null || element.position.x < currentlySelected.position.x)) { currentlySelected = element; }
        }
        if (nothingHighlighted) { return currentlySelected; }

        OxGUI nextPreciseSelection = null, nextSelection = null;
        foreach (OxGUI element in elements)
        {
            if (element != currentlySelected && element.visible && element.selectable && element.position.x > currentlySelected.position.x && ((element.position.y >= currentlySelected.position.y && element.position.y <= currentlySelected.position.y + currentlySelected.size.y) || (element.position.y + element.size.y >= currentlySelected.position.y && element.position.y + element.size.y <= currentlySelected.position.y + currentlySelected.size.y)) && (nextPreciseSelection == null || element.position.x - currentlySelected.position.x < nextPreciseSelection.position.x - currentlySelected.position.x)) { nextPreciseSelection = element; }
            if (element != currentlySelected && element.visible && element.selectable && element.position.x > currentlySelected.position.x && (nextSelection == null || element.position.x - currentlySelected.position.x < nextSelection.position.x - currentlySelected.position.x)) { nextSelection = element; }
        }
        if (nextPreciseSelection != null) nextSelection = nextPreciseSelection;
        return nextSelection;
    }

    public void Highlight()
    {
        foreach (OxGUI element in elements) { element.UnHighlight(); }
        highlighted = true;
    }
    public void UnHighlight()
    {
        highlighted = false;
    }
    public void MouseDown()
    {
        //Debug.Log("Down");
        pressing = true;
        if (pressed != null) pressed(this);
    }
    public void MouseUp()
    {
        //Debug.Log("Up");
        pressing = false;
        if (released != null) released(this);
        if (Mathf.Abs(Input.mousePosition.x - origMouse.x) < size.x && Mathf.Abs(Input.mousePosition.y - origMouse.y) < size.y && clicked != null) clicked(this);
    }

    protected virtual string TextureSize()
    {
        string fileEnd = "11";

        float ratio = size.x / size.y;

        if (ratio <= 0.1875f) fileEnd = "18"; //0.125
        else if (ratio <= 0.375) fileEnd = "14"; //0.25
        else if (ratio <= 0.75) fileEnd = "12"; //0.5
        else if (ratio <= 1.5f) fileEnd = "11";
        else if (ratio <= 3f) fileEnd = "21";
        else if (ratio <= 6f) fileEnd = "41";
        else if (ratio <= 12f) fileEnd = "81";

        return fileEnd;
    }
    /*protected virtual string TextureSize()
    {
        string fileEnd = "512x512";

        float ratio = size.y / size.x;

        if (ratio <= ((128f + 64f) / 512f)) fileEnd = "512x128";
        else if (ratio <= ((256f + 64f) / 512f)) fileEnd = "512x256";
        else if (ratio <= ((384f + 64f) / 512f)) fileEnd = "512x384";
        else if (ratio <= (512f / (512f + 64f))) fileEnd = "512x512";
        else if (ratio <= ((512f + 64f) / 512f)) fileEnd = "512x512";
        else if (ratio <= (512f / (384f + 64f))) fileEnd = "384x512";
        else if (ratio <= (512f / (256f + 64f))) fileEnd = "256x512";
        else if(ratio <= (512f / (128f + 64f))) fileEnd = "128x512";

        return fileEnd;
    }*/

    public delegate void MovedEventHandler(Vector2 newPosition, Vector2 delta);
    public void Reposition(Vector2 newPosition)
    {
        Vector2 oldPosition = new Vector2(position.x, position.y);
        position.x = newPosition.x;
        position.y = newPosition.y;

        if (position.x < 0) position.x = 0;
        if (position.y < 0) position.y = 0;
        if (position.x + size.x > Screen.width) position.x = Screen.width - size.x;
        if (position.y + size.y > Screen.height) position.y = Screen.height - size.y;

        if(moved != null) moved(position, position - oldPosition);
    }
    public void Reposition(float x, float y) { Reposition(new Vector2(x, y)); }
    public Vector2 Position() { return position; }

    public delegate void ResizedEventHandler(Vector2 newSize, Vector2 delta);
    public void SetSize(float width, float height)
    {
        Vector2 oldSize = new Vector2(size.x, size.y);
        float newWidth = width, newHeight = height;

        if (width + position.x > Screen.width) newWidth = Screen.width - position.x;
        else if (width < 0) newWidth = 0;
        if (height + position.y > Screen.height) newHeight = Screen.height - position.y;
        else if (height < 0) newHeight = 0;
        //if (width + position.x <= Screen.width && width >= smallestWidth) newWidth = width;
        //if (height + position.y <= Screen.height && height >= smallestHeight) newHeight = height;

        size.x = newWidth;
        size.y = newHeight;

        if (resized != null) resized(size, size - oldSize);
    }
    protected static void SetSize(OxGUI element, float width, float height)
    {
        element.SetSize(width, height);
    }
    public void SetSize(float width, float height, float smallestWidth, float smallestHeight)
    {
        Vector2 oldSize = new Vector2(size.x, size.y);
        float newWidth = width, newHeight = height;

        if (width < smallestWidth) newWidth = smallestWidth;
        else if (width + position.x > Screen.width) newWidth = Screen.width - position.x;

        if (newHeight < smallestHeight) newHeight = smallestHeight;
        else if (newHeight + position.y > Screen.height) newHeight = Screen.height - position.y;
        //if (width + position.x <= Screen.width && width >= smallestWidth) newWidth = width;
        //if (height + position.y <= Screen.height && height >= smallestHeight) newHeight = height;

        size.x = newWidth;
        size.y = newHeight;

        if (resized != null) resized(size, size - oldSize);
    }
    public virtual void Resize(Vector2 newSize)
    {
        //float newWidth = size.x, newHeight = size.y;
        //Vector2 oldSize = new Vector2(size.x, size.y);
        //if (newSize.x + position.x <= Screen.width && newSize.x >= SMALLEST_WIDTH) newWidth = newSize.x;
        //if (newSize.y + position.y <= Screen.height && newSize.y >= SMALLEST_HEIGHT) newHeight = newSize.y;

        SetSize(newSize.x, newSize.y, SMALLEST_WIDTH, SMALLEST_HEIGHT);
        //if (resized != null) resized(size, size - oldSize);
        //windowSize = new Vector2(size.x, size.y);
    }
    public virtual void Resize(float x, float y) { Resize(new Vector2(x, y)); }
    public Vector2 Size() { return size; }

    public delegate void VisibleChangedEventHandler(bool visibility);
    public void Show()
    {
        visible = true;
        if (visibleChanged != null) visibleChanged(visible);
    }
    public void Hide()
    {
        visible = false;
        if (visibleChanged != null) visibleChanged(visible);
    }
    public bool Visible()
    {
        return visible;
    }

    protected virtual void OxGUI_resized(Vector2 newSize, Vector2 delta)
    {
        if (delta != Vector2.zero)
        {
            clippedFontSize = fitText(text, size.x, size.y);
            overflowFontSize = fitText(text, 0, size.y);
        }
    }

    public static int fitText(string text, float width, float height)
    {
        int fontSize = 1;
        GUIStyle testStyle = new GUIStyle();
        testStyle.fontSize = fontSize;
        //Font nonDynamic = new Font();
        //testStyle.font = nonDynamic;
        testStyle.wordWrap = false;
        //testStyle.clipping = TextClipping.Overflow;
        Vector2 sizeTest = new Vector2();

        while ((sizeTest.x < width || width <= 0) && (sizeTest.y < height || height <= 0) && !(width <= 0 && height <= 0))
        //while(testStyle.CalcHeight(new GUIContent("A"), maxWidth) < height)
        {
                sizeTest = testStyle.CalcSize(new GUIContent(text));
                //Debug.Log("Size: " + fontSize);
                testStyle.fontSize = fontSize++;

                if (fontSize >= 128) break;
        }

        if (sizeTest.y <= height * (20f / 100f))
        {
            while ((sizeTest.y < height * (20f / 100f) || height <= 0) && !(height <= 0))
            //while(testStyle.CalcHeight(new GUIContent("A"), maxWidth) < height)
            {
                sizeTest = testStyle.CalcSize(new GUIContent(text));
                //Debug.Log("Size: " + fontSize);
                testStyle.fontSize = fontSize++;

                if (fontSize >= 128) break;
            }
        }
        if (sizeTest.x >= width || sizeTest.y >= height)
        {
            if (fontSize > 7) fontSize -= 7;
            else fontSize = 1;
        }
        //if (sizeTest.y <= height * (10f / 100f)) fontSize = fitText(text, 0, height);
        //Debug.Log("Size of " + text + ": " + fontSize);
        //Debug.Log("Looped: " + fontSize + " Height: " + testStyle.CalcHeight(new GUIContent(text), maxWidth));
        return fontSize;
        //return (int) (testStyle.CalcHeight(new GUIContent(text), maxWidth) / 2f);
    }

    public static float GetPratio(float windowWidth, float windowHeight, float widthInInches, float heightInInches, float maxPercentWidth, float maxPercentHeight)
    {
        return Mathf.Min(screenDpi, (maxPercentWidth * windowWidth) / widthInInches, (maxPercentHeight * windowHeight) / heightInInches); //pratio = min(dpi,pw/mw,ph/mh)
    }
    public static float GetPratioX(float windowWidth, float widthInInches, float maxPercentWidth)
    {
        return Mathf.Min(screenDpi, (maxPercentWidth * windowWidth) / widthInInches);
    }
    public static float GetPratioY(float windowHeight, float heightInInches, float maxPercentHeight)
    {
        return Mathf.Min(screenDpi, (maxPercentHeight * windowHeight) / heightInInches);
    }
}
