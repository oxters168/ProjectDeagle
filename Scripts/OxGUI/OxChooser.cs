using UnityEngine;
using System.Collections;

public class OxChooser : OxListable
{
    private OxButton acceptButton, cancelButton;
    private string centerTexture, topTexture, bottomTexture, leftTexture, rightTexture;
    public int itemsShown = 5;
    public event Done done;
    //public event Canceled canceled;

    private bool choseItem = false;

    private bool down, outOfDead;
    private float mouseY = -1f;
    private float listOffset = 0f, listScroll = 0f;

    public OxChooser() : this("", "", "", "", "") { }
    public OxChooser(string cTexture, string lTexture, string rTexture, string tTexture, string bTexture) : base(new Vector2(0, 0), new Vector2(0, 0))
    {
        centerTexture = cTexture;
        leftTexture = lTexture;
        rightTexture = rTexture;
        topTexture = tTexture;
        bottomTexture = bTexture;
        acceptButton = new OxButton("Accept", "MenuButton");
        acceptButton.clicked += Button_clicked;
        cancelButton = new OxButton("Cancel", "MenuButton");
        cancelButton.clicked += Button_clicked;
    }

    void Button_clicked(OxGUI sender)
    {
        if (sender == acceptButton && done != null) done(this, true);
        if (sender == cancelButton && done != null) done(this, false);
    }

    public delegate void Done(OxChooser sender, bool accepted);
    //public delegate void Canceled();

    public override void Draw()
    {
        base.Draw();

        if (visible)
        {
            choseItem = false;

            float decreasedListSize = size.y * (75f / 100f);
            float padding = Mathf.Min(size.x, decreasedListSize) * (5f / 100f), leftPad = 0, rightPad = 0, topPad = 0, bottomPad = 0;

            GUIStyle panelStyle = new GUIStyle();
            if (leftTexture.Length > 0)
            {
                leftPad = padding;
                panelStyle.normal.background = Resources.Load<Texture2D>(textureLocation + leftTexture);
                panelStyle.hover.background = Resources.Load<Texture2D>(textureLocation + leftTexture);
                panelStyle.active.background = Resources.Load<Texture2D>(textureLocation + leftTexture);
                GUI.Label(new Rect(position.x, position.y, leftPad, decreasedListSize), "", panelStyle);
            }
            if (rightTexture.Length > 0)
            {
                rightPad = padding;
                panelStyle.normal.background = Resources.Load<Texture2D>(textureLocation + rightTexture);
                panelStyle.hover.background = Resources.Load<Texture2D>(textureLocation + rightTexture);
                panelStyle.active.background = Resources.Load<Texture2D>(textureLocation + rightTexture);
                GUI.Label(new Rect(position.x + size.x - rightPad, position.y, rightPad, decreasedListSize), "", panelStyle);
            }
            if (topTexture.Length > 0)
            {
                topPad = padding;
                panelStyle.normal.background = Resources.Load<Texture2D>(textureLocation + topTexture);
                panelStyle.hover.background = Resources.Load<Texture2D>(textureLocation + topTexture);
                panelStyle.active.background = Resources.Load<Texture2D>(textureLocation + topTexture);
                GUI.Label(new Rect(position.x, position.y, size.x, topPad), "", panelStyle);
            }
            if (bottomTexture.Length > 0)
            {
                bottomPad = padding;
                panelStyle.normal.background = Resources.Load<Texture2D>(textureLocation + bottomTexture);
                panelStyle.hover.background = Resources.Load<Texture2D>(textureLocation + bottomTexture);
                panelStyle.active.background = Resources.Load<Texture2D>(textureLocation + bottomTexture);
                GUI.Label(new Rect(position.x, position.y + decreasedListSize - bottomPad, size.x, bottomPad), "", panelStyle);
            }
            if (centerTexture.Length > 0)
            {
                panelStyle.normal.background = Resources.Load<Texture2D>(textureLocation + centerTexture);
                panelStyle.hover.background = Resources.Load<Texture2D>(textureLocation + centerTexture);
                panelStyle.active.background = Resources.Load<Texture2D>(textureLocation + centerTexture);
                GUI.Label(new Rect(position.x + leftPad, position.y + topPad, size.x - leftPad - rightPad, decreasedListSize - topPad - bottomPad), "", panelStyle);
            }

            if(items.Count > 0) {
                
                if ((new Rect(position.x, position.y, size.x, decreasedListSize)).Contains(new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y)))
                {
                    listScroll += Input.mouseScrollDelta.y;
                    if (Input.GetMouseButtonDown(0)) { down = true; mouseY = Input.mousePosition.y; }
                }
                if (Input.GetMouseButtonUp(0)) { down = false; outOfDead = false; mouseY = -1; }
                if (down)
                {
                    if (outOfDead) { listScroll += (mouseY - Input.mousePosition.y) / 10f; mouseY = Input.mousePosition.y; }
                    else if (Mathf.Abs(mouseY - Input.mousePosition.y) > DRAG_DEAD_ZONE) { outOfDead = true; mouseY = Input.mousePosition.y; }
                }
                listOffset += listScroll;
                listScroll = Mathf.Lerp(listScroll, 0, Time.deltaTime * 2f);
                float buttonHeight = (decreasedListSize - topPad - bottomPad) / itemsShown;

                if (items.Count > itemsShown)
                {
                    float maxOffsetY = -((buttonHeight * items.Count) - (buttonHeight * itemsShown));
                    if (listOffset > 0) listOffset = 0;
                    if (listOffset < maxOffsetY) listOffset = maxOffsetY;
                }
                else listOffset = 0;

                for (int i = 0; i < itemsShown; i++)
                {
                    //float buttonY = position.y + topPad + (buttonHeight * i) + listOffset;
                    float buttonY = position.y + topPad + (buttonHeight * i) + (listOffset % buttonHeight);

                    int indexInList = Mathf.CeilToInt(Mathf.Abs(listOffset) / buttonHeight) + i;
                    if (indexInList > -1 && indexInList < items.Count)
                    {
                        items[indexInList].Reposition(position.x + leftPad, buttonY);
                        SetSize(items[indexInList], size.x - leftPad - rightPad, buttonHeight);
                        //items[indexInList].Show();
                        items[indexInList].Draw();
                    }

                    //if (i == 0 && indexInList > 0) items[indexInList - 1].Hide();
                    //if (i == itemsShown - 1 && indexInList < items.Count - 1) items[indexInList + 1].Hide();
                }
                /*for (int i = 0; i < items.Count; i++)
                {
                    float buttonY = position.y + topPad + (buttonHeight * i) + listOffset;

                    if (buttonY >= (position.y + topPad - 1) && buttonY + buttonHeight <= (position.y + decreasedListSize - bottomPad + 1))
                    {
                        items[i].Show();
                    }
                    else
                    {
                        items[i].Hide();
                    }

                    float opaqueValue = 1f;
                    //if (i > 0 && i < items.Count - 1) opaqueValue = Mathf.Min(opaqueValue, ((buttonY - (position.y + topPad)) / buttonHeight), (((position.y + decreasedListSize - bottomPad) - (buttonY + buttonHeight)) / buttonHeight));
                    items[i].opaque = opaqueValue;

                    items[i].Reposition(position.x + leftPad, buttonY);
                    SetSize(items[i], size.x - leftPad - rightPad, buttonHeight);
                    items[i].Draw();
                }*/
            }
            SetSize(acceptButton, size.x / 2f, size.y - decreasedListSize);
            SetSize(cancelButton, size.x / 2f, size.y - decreasedListSize);
            acceptButton.Reposition(position.x, position.y + decreasedListSize);
            cancelButton.Reposition(position.x + acceptButton.Size().x, position.y + decreasedListSize);
            acceptButton.Draw();
            cancelButton.Draw();

            //if (!choseItem && ((Input.GetMouseButtonUp(0) && (new Rect(position.x, position.y, size.x, decreasedListSize)).Contains(new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y))) || Input.GetMouseButtonUp(1))) { Deselect(); }
        }
    }
}
