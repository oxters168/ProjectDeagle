using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class OxMenu : OxListable
{
    //private List<OxGUI> items = new List<OxGUI>();
    public bool horizontal;
    public int split = -1;

    //OxGUI selectedItem;
    //int selectedIndex = -1;
    //public event IndexChangedEventHandler indexChanged;

    public OxMenu(Vector2 position, Vector2 size, bool horz) : base(position, size)
    {
        //selectable = false;
        horizontal = horz;
    }
    public OxMenu(bool horz) : this(new Vector2(0, 0), new Vector2(0, 0), horz) { }

    //public delegate void IndexChangedEventHandler(int itemIndex);
    public override void Draw()
    {
        base.Draw();

        if (visible)
        {
            float splitNumber = items.Count;
            if (split > 0) splitNumber = split;

            for (int i = 0; i < items.Count; i++)
            {
                float shiftToCenter = ((Mathf.Max(splitNumber, items.Count) - Mathf.Min(items.Count, splitNumber) - Mathf.Min(1, Mathf.Abs(splitNumber - items.Count))) * (size.y / splitNumber));
                float buttonX = position.x, buttonY = shiftToCenter + ((position.y + (size.y / 2f)) + ((size.y / splitNumber) * (i - (splitNumber / 2f)))), buttonWidth = size.x, buttonHeight = (size.y / splitNumber);

                if (horizontal)
                {
                    shiftToCenter = ((Mathf.Max(splitNumber, items.Count) - Mathf.Min(items.Count, splitNumber) - Mathf.Min(1, Mathf.Abs(splitNumber - items.Count))) * (size.x / splitNumber));
                    buttonX = shiftToCenter + ((position.x + (size.x / 2f)) + ((size.x / splitNumber) * (i - (splitNumber / 2f))));
                    buttonY = position.y;
                    buttonWidth = (size.x / splitNumber);
                    buttonHeight = size.y;
                }

                items[i].Reposition(buttonX, buttonY);
                SetSize(items[i], buttonWidth, buttonHeight);
                items[i].Draw();
            }

            //return true;
        }

        //return false;
    }
}
