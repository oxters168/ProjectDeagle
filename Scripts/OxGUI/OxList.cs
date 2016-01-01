using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class OxList : OxListable
{
    private string centerTexture, topTexture, bottomTexture, leftTexture, rightTexture;
    public int itemsShown = 5;
    public bool horizontal = false;

    private bool choseItem = false;

    private bool down, outOfDead;
    private float origMouse = -1f;
    private float listOffset = 0f, listScroll = 0f;

    public OxList() : this("", "", "", "", "") { }
    public OxList(string cTexture, string lTexture, string rTexture, string tTexture, string bTexture) : base(new Vector2(0, 0), new Vector2(0, 0))
    {
        centerTexture = cTexture;
        leftTexture = lTexture;
        rightTexture = rTexture;
        topTexture = tTexture;
        bottomTexture = bTexture;
    }

    public override void Draw()
    {
        base.Draw();

        if (visible)
        {
            choseItem = false;

            float padding = Mathf.Min(size.x, size.y) * (5f / 100f), leftPad = 0, rightPad = 0, topPad = 0, bottomPad = 0;

            GUIStyle panelStyle = new GUIStyle();
            if (leftTexture.Length > 0)
            {
                leftPad = padding;
                panelStyle.normal.background = Resources.Load<Texture2D>(textureLocation + leftTexture);
                panelStyle.hover.background = Resources.Load<Texture2D>(textureLocation + leftTexture);
                panelStyle.active.background = Resources.Load<Texture2D>(textureLocation + leftTexture);
                GUI.Label(new Rect(position.x, position.y, leftPad, size.y), "", panelStyle);
            }
            if (rightTexture.Length > 0)
            {
                rightPad = padding;
                panelStyle.normal.background = Resources.Load<Texture2D>(textureLocation + rightTexture);
                panelStyle.hover.background = Resources.Load<Texture2D>(textureLocation + rightTexture);
                panelStyle.active.background = Resources.Load<Texture2D>(textureLocation + rightTexture);
                GUI.Label(new Rect(position.x + size.x - rightPad, position.y, rightPad, size.y), "", panelStyle);
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
                GUI.Label(new Rect(position.x, position.y + size.y - bottomPad, size.x, bottomPad), "", panelStyle);
            }
            if (centerTexture.Length > 0)
            {
                panelStyle.normal.background = Resources.Load<Texture2D>(textureLocation + centerTexture);
                panelStyle.hover.background = Resources.Load<Texture2D>(textureLocation + centerTexture);
                panelStyle.active.background = Resources.Load<Texture2D>(textureLocation + centerTexture);
                GUI.Label(new Rect(position.x + leftPad, position.y + topPad, size.x - leftPad - rightPad, size.y - topPad - bottomPad), "", panelStyle);
            }

            if(items.Count > 0)
            {
                #region Scrolling Controls
                if ((new Rect(position.x, position.y, size.x, size.y)).Contains(new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y)))
                {
                    listScroll += Input.mouseScrollDelta.y;
                    
                    if (Input.GetMouseButtonDown(0))
                    {
                        down = true;
                        origMouse = Input.mousePosition.y;
                        if (horizontal) origMouse = Input.mousePosition.x;
                    }
                }
                if (Input.GetMouseButtonUp(0)) { down = false; outOfDead = false; origMouse = -1; }
                if (down)
                {
                    if (outOfDead)
                    {
                        if (horizontal) listScroll += (Input.mousePosition.x - origMouse) / 10f;
                        else listScroll += (origMouse - Input.mousePosition.y) / 10f;
                        origMouse = Input.mousePosition.y;
                        if (horizontal) origMouse = Input.mousePosition.x;
                    }
                    else if ((horizontal && Mathf.Abs(origMouse - Input.mousePosition.x) > DRAG_DEAD_ZONE) || (!horizontal && Mathf.Abs(origMouse - Input.mousePosition.y) > DRAG_DEAD_ZONE))
                    {
                        outOfDead = true;
                        origMouse = Input.mousePosition.y;
                        if (horizontal) origMouse = Input.mousePosition.x;
                    }
                }
                listOffset += listScroll;
                listScroll = Mathf.Lerp(listScroll, 0, Time.deltaTime * 2f);
                #endregion

                float buttonWidth = size.x - leftPad - rightPad, buttonHeight = (size.y - topPad - bottomPad) / itemsShown;
                if (horizontal) { buttonWidth = (size.x - leftPad - rightPad) / itemsShown; buttonHeight = (size.y - topPad - bottomPad); }

                if (items.Count > itemsShown)
                {
                    //float maxOffset = -((buttonHeight * items.Count) - (buttonHeight * itemsShown));
                    float maxOffset = -((buttonHeight * items.Count) - (buttonHeight * itemsShown));
                    if (horizontal) maxOffset = -((buttonWidth * items.Count) - (buttonWidth * itemsShown));
                    if (listOffset > 0) listOffset = 0;
                    if (listOffset < maxOffset) listOffset = maxOffset;
                }
                else listOffset = 0;

                for (int i = 0; i < itemsShown; i++)
                {
                    //float buttonY = position.y + topPad + (buttonHeight * i) + listOffset;
                    float buttonX = position.x + leftPad, buttonY = position.y + topPad + (buttonHeight * i) + (listOffset % buttonHeight);
                    if (horizontal) { buttonX = position.x + leftPad + (buttonWidth * i) + (listOffset % (buttonWidth)); buttonY = position.y + topPad; }

                    int indexInList = Mathf.CeilToInt(Mathf.Abs(listOffset) / buttonHeight) + i;
                    if (horizontal) indexInList = Mathf.CeilToInt(Mathf.Abs(listOffset) / buttonWidth) + i;
                    if (indexInList > -1 && indexInList < items.Count)
                    {
                        items[indexInList].Reposition(buttonX, buttonY);
                        SetSize(items[indexInList], buttonWidth, buttonHeight);
                        //items[indexInList].Show();
                        items[indexInList].Draw();
                    }

                    //if (i == 0 && indexInList > 0) items[indexInList - 1].Hide();
                    //if (i == itemsShown - 1 && indexInList < items.Count - 1) items[indexInList + 1].Hide();
                }
                /*for (int i = 0; i < items.Count; i++)
                {
                    float buttonY = position.y + topPad + (buttonHeight * i) + listOffset;

                    if (buttonY >= (position.y + topPad - 1) && buttonY + buttonHeight <= (position.y + size.y - bottomPad + 1))
                    {
                        items[i].Show();
                    }
                    else items[i].Hide();

                    float opaqueValue = 1f;
                    if (i > 0 && i < items.Count - 1) opaqueValue = Mathf.Min(opaqueValue, ((buttonY - (position.y + topPad)) / buttonHeight), (((position.y + size.y - bottomPad) - (buttonY + buttonHeight)) / buttonHeight));
                    items[i].opaque = opaqueValue;

                    items[i].Reposition(position.x + leftPad, buttonY);
                    SetSize(items[i], size.x - leftPad - rightPad, buttonHeight);
                    items[i].Draw();
                }*/
            }

            //if (!choseItem && ((Input.GetMouseButtonUp(0) && (new Rect(position.x, position.y, size.x, size.y)).Contains(new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y))) || Input.GetMouseButtonUp(1))) { Deselect(); }
        }
    }
}
