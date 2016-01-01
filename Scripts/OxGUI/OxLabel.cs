using UnityEngine;
using System.Collections;

public class OxLabel : OxGUI {

    GUIStyle labelStyle;
    Color textColor;
    TextAnchor textAnchor;
    TextClipping textClipping;

    public OxLabel(string text, GUIStyle style, Color color, TextAnchor anchor, TextClipping clipping, string texture) : base(new Vector2(0, 0), new Vector2(0, 0), texture)
    {
        selectable = false;
        this.text = text;
        labelStyle = style;
        textColor = color;
        if (textColor == null) textColor = Color.white;
        textAnchor = anchor;
        textClipping = clipping;
    }
    public OxLabel(string text, Color color, TextAnchor anchor) : this(text, null, color, anchor, TextClipping.Clip, "") { }
    public OxLabel() : this("", null, Color.white, TextAnchor.MiddleCenter, TextClipping.Clip, "") { }

    public override void Draw()
    {
        base.Draw();

        if (visible)
        {
            if (texture.Length > 0)
            {
                if (labelStyle == null) labelStyle = new GUIStyle();
                labelStyle.normal.background = Resources.Load<Texture2D>(textureLocation + texture + " " + TextureSize());
                labelStyle.normal.textColor = textColor;
                labelStyle.hover.textColor = textColor;
                labelStyle.active.textColor = textColor;
                labelStyle.alignment = textAnchor;
                labelStyle.clipping = textClipping;
                labelStyle.fontSize = clippedFontSize;
            }
            else
            {
                if(labelStyle == null) labelStyle = new GUIStyle(GUI.skin.label);
                labelStyle.normal.background = null;
                if (textColor != null)
                {
                    labelStyle.normal.textColor = textColor;
                    labelStyle.hover.textColor = textColor;
                    labelStyle.active.textColor = textColor;
                }
                if(textAnchor != null) labelStyle.alignment = textAnchor;
                if(textClipping != null) labelStyle.clipping = textClipping;
                labelStyle.fontSize = clippedFontSize;
            }

            GUI.Label(new Rect(position.x, position.y, size.x, size.y), text, labelStyle);
        }
    }
}
