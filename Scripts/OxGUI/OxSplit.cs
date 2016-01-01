using UnityEngine;
using System.Collections;

public class OxSplit : OxLayout {

    public OxGUI westComponent, eastComponent;
    public float westPercentSize = 1f, eastPercentSize = 1f, division = 0.5f;
    public bool horizontal = true;

    public OxSplit()
    {
    }

    public override void Draw()
    {
        base.Draw();

        if (visible)
        {
            if (westPercentSize < 0) westPercentSize = 0f;
            else if (westPercentSize > 1) westPercentSize = 1f;
            if (eastPercentSize < 0) eastPercentSize = 0f;
            else if (eastPercentSize > 1) eastPercentSize = 1f;
            if (division < 0) division = 0f;
            else if (division > 1) division = 1f;

            float currentDivision = division;
            if (westComponent == null) currentDivision = 0f;
            else if (eastComponent == null) currentDivision = 1f;

            float westSplitX = size.x, westSplitY = size.y * currentDivision;
            if (horizontal) { westSplitX = size.x * currentDivision; westSplitY = size.y; }
            float eastSplitX = size.x, eastSplitY = size.y * (1f - currentDivision), eastPushX = 0f, eastPushY = westSplitY;
            if (horizontal) { eastSplitX = size.x * (1f - currentDivision); eastSplitY = size.y; eastPushX = westSplitX; eastPushY = 0f; }

            if (westComponent != null && currentDivision > 0f)
            {
                westComponent.Reposition(position.x + ((westSplitX * (1f - westPercentSize)) / 2f), position.y + ((westSplitY * (1f - westPercentSize)) / 2f));
                SetSize(westComponent, (westSplitX * westPercentSize), (westSplitY * westPercentSize));
                westComponent.Draw();
            }
            if (eastComponent != null && currentDivision < 1f)
            {
                eastComponent.Reposition(position.x + eastPushX + ((eastSplitX * (1f - eastPercentSize)) / 2f), position.y + eastPushY + ((eastSplitY * (1f - eastPercentSize)) / 2f));
                SetSize(eastComponent, (eastSplitX * eastPercentSize), (eastSplitY * eastPercentSize));
                eastComponent.Draw();
            }
        }
        //return false;
    }
}
