using UnityEngine;
using System.Collections;

public class OxCheckBox : OxGUI
{
	GUIStyle checkBoxStyle;
    string checkBoxText, checkBoxCheckedTexture;
    public bool isChecked;
    public event CheckBoxSwitchedEventHandler checkBoxSwitched;

    public OxCheckBox(Vector2 position, Vector2 size, GUIStyle style, bool check, string text, string textureLoc, string checkedTextureLocation) : base(position, size, textureLoc)
    {
        if (style != null) checkBoxStyle = new GUIStyle(style);
        isChecked = check;
        checkBoxText = text;
        checkBoxCheckedTexture = checkedTextureLocation;
    }
    public OxCheckBox(Vector2 position, Vector2 size, bool check, string text) : this(position, size, null, check, text, "", "") { }
    public OxCheckBox(Vector2 position, Vector2 size, bool check) : this(position, size, null, check, "", "", "") { }
    public OxCheckBox(bool check, string text, string texture, string checkedTextureLocation) : this(new Vector2(0, 0), new Vector2(0, 0), null, check, text, texture, checkedTextureLocation) { }
    public OxCheckBox(bool check, string text) : this(new Vector2(0, 0), new Vector2(0, 0), null, check, text, "", "") { }
    public OxCheckBox(string text) : this(new Vector2(0, 0), new Vector2(0, 0), null, false, text, "", "") { }

    public delegate void CheckBoxSwitchedEventHandler(OxGUI sender, bool check);
    public override void Draw()
    {
        base.Draw();

        if (visible)
        {
            if (texture.Length > 0)
            {
                if (checkBoxStyle == null) checkBoxStyle = new GUIStyle();
                checkBoxStyle.normal.textColor = Color.white;
                checkBoxStyle.hover.textColor = Color.white;
                checkBoxStyle.active.textColor = Color.white;

                checkBoxStyle.clipping = TextClipping.Overflow;
                checkBoxStyle.alignment = TextAnchor.MiddleLeft;
                checkBoxStyle.padding.left = (int)(size.y + (size.y * 0.25f));
                if (checkBoxStyle.clipping == TextClipping.Overflow) checkBoxStyle.fontSize = overflowFontSize;
                else checkBoxStyle.fontSize = clippedFontSize;

                if (isChecked)
                {
                    if (highlighted) checkBoxStyle.normal.background = Resources.Load<Texture2D>(textureLocation + checkBoxCheckedTexture + "Hover 512x512");
                    else checkBoxStyle.normal.background = Resources.Load<Texture2D>(textureLocation + checkBoxCheckedTexture + "Up 512x512");
                    checkBoxStyle.hover.background = Resources.Load<Texture2D>(textureLocation + checkBoxCheckedTexture + "Hover 512x512");
                    checkBoxStyle.active.background = Resources.Load<Texture2D>(textureLocation + checkBoxCheckedTexture + "Down 512x512");
                }
                else
                {
                    if (highlighted) checkBoxStyle.normal.background = Resources.Load<Texture2D>(textureLocation + texture + "Hover 512x512");
                    else checkBoxStyle.normal.background = Resources.Load<Texture2D>(textureLocation + texture + "Up 512x512");
                    checkBoxStyle.hover.background = Resources.Load<Texture2D>(textureLocation + texture + "Hover 512x512");
                    checkBoxStyle.active.background = Resources.Load<Texture2D>(textureLocation + texture + "Down 512x512");
                }
            }
            else
            {
                if (checkBoxStyle == null) checkBoxStyle = new GUIStyle(GUI.skin.toggle);
                checkBoxStyle.normal.textColor = Color.white;
                checkBoxStyle.hover.textColor = Color.white;
                checkBoxStyle.active.textColor = Color.white;

                checkBoxStyle.clipping = TextClipping.Overflow;
                checkBoxStyle.alignment = TextAnchor.MiddleLeft;
                checkBoxStyle.padding.left = (int)(size.y + (size.y * 0.25f));
                if (checkBoxStyle.clipping == TextClipping.Overflow) checkBoxStyle.fontSize = overflowFontSize;
                else checkBoxStyle.fontSize = clippedFontSize;
            }

            //if (checkBoxStyle != null)
            //{
                bool check = GUI.Toggle(new Rect(position.x, position.y, size.y, size.y), isChecked, checkBoxText, checkBoxStyle);
                if (check != isChecked)
                {
                    isChecked = check;
                    if (checkBoxSwitched != null) checkBoxSwitched(this, isChecked);
                    //return true; 
                }
            //}
            //else
            //{
            //    bool check = GUI.Toggle(new Rect(position.x, position.y, size.y, size.y), isChecked, checkBoxText);
            //    if (check != isChecked)
            //    {
            //        isChecked = check;
            //        if (checkBoxSwitched != null) checkBoxSwitched(this, isChecked);
                    //return true;
            //    }
            //}
        }

        //return false;
    }
}
