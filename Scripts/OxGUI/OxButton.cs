using UnityEngine;
using System.Collections;

public class OxButton : OxGUI
{
    GUIStyle buttonStyle;
    public string replaceWhat, replaceWith, substringBefore, substringAfter;
    //string text;
    //private bool makeStyle;
    //public event ButtonClickedEventHandler buttonClicked;
    //public event MouseDownEventHandler mouseDown;

    public OxButton(Vector2 position, Vector2 size, GUIStyle style, string buttonText, string textureName) : base(position, size, textureName)
    {
        if (style != null) buttonStyle = new GUIStyle(style);
        text = buttonText;
    }
    public OxButton(Vector2 position, Vector2 size, string text) : this(position, size, null, text, "") { }
    public OxButton(Vector2 position, Vector2 size) : this(position, size, null, "", "") { }
    public OxButton(string text, string texture) : this(new Vector2(0, 0), new Vector2(0, 0), null, text, texture) { }
    public OxButton(string text) : this(new Vector2(0, 0), new Vector2(0, 0), null, text, "") { }

    //public delegate void ButtonClickedEventHandler(OxGUI sender);
    //public delegate void MouseDownEventHandler(OxGUI sender);
    public override void Draw()
    {
        base.Draw();

        if (visible)
        {
            if (texture.Length > 0)
            {
                //Debug.Log(textureLocation + texture + "Up " + TextureSize());
                if (buttonStyle == null) buttonStyle = new GUIStyle();
                if (highlighted) buttonStyle.normal.background = Resources.Load<Texture2D>(textureLocation + texture + "Hover " + TextureSize());
                else buttonStyle.normal.background = Resources.Load<Texture2D>(textureLocation + texture + "Up " + TextureSize());
                buttonStyle.hover.background = Resources.Load<Texture2D>(textureLocation + texture + "Hover " + TextureSize());
                buttonStyle.active.background = Resources.Load<Texture2D>(textureLocation + texture + "Down " + TextureSize());
                buttonStyle.normal.textColor = Color.white;
                buttonStyle.hover.textColor = Color.white;
                buttonStyle.active.textColor = Color.white;
                buttonStyle.alignment = TextAnchor.MiddleCenter;
                buttonStyle.clipping = TextClipping.Clip;
                buttonStyle.fontSize = clippedFontSize;
            }
            else
            {
                if (buttonStyle == null) buttonStyle = new GUIStyle(GUI.skin.button);
                buttonStyle.normal.textColor = Color.white;
                buttonStyle.hover.textColor = Color.white;
                buttonStyle.active.textColor = Color.white;
                buttonStyle.alignment = TextAnchor.MiddleCenter;
                buttonStyle.clipping = TextClipping.Clip;
                buttonStyle.fontSize = clippedFontSize;
            }

            GUI.color = new Color(1, 1, 1, opaque);
            //if (buttonStyle != null)
            //{
            
                GUI.Button(new Rect(position.x, position.y, size.x, size.y), ShownText(), buttonStyle);
                //if (GUI.Button(new Rect(position.x, position.y, size.x, size.y), text, buttonStyle))
                //{
                //    if (buttonClicked != null) buttonClicked(this);
                //    return true;
                //}
            //}
            //else
            //{
            //    GUI.Button(new Rect(position.x, position.y, size.x, size.y), text);
                //if (GUI.Button(new Rect(position.x, position.y, size.x, size.y), text))
                //{
                //    if (buttonClicked != null) buttonClicked(this);
                //    return true;
                //}
            //}
            GUI.color = Color.white;
        }

        //return false;
    }

    public string ShownText()
    {
        string shownText = text;
        if (replaceWhat != null && replaceWhat.Length > 0 && replaceWith != null) shownText.Replace(replaceWhat, replaceWith);
        if (substringBefore != null && substringBefore.Length > 0 && shownText.LastIndexOf(substringBefore) > -1) shownText = shownText.Substring(shownText.LastIndexOf(substringBefore) + 1);
        if (substringAfter != null && substringAfter.Length > 0 && shownText.LastIndexOf(substringAfter) > -1) shownText = shownText.Substring(0, shownText.LastIndexOf(substringAfter));
        return shownText;
    }

    protected override void OxGUI_resized(Vector2 newSize, Vector2 delta)
    {
        if (delta != Vector2.zero)
        {
            clippedFontSize = fitText(ShownText(), size.x, size.y);
        }
    }

    /*protected override string TextureSize()
    {
        string fileEnd = "512x384";
        float ratio = size.y / size.x;

        if (ratio <= ((128f + 32f) / 512f)) fileEnd = "512x128";
        else if (ratio <= ((192f + 32f) / 512f)) fileEnd = "512x192";
        else if (ratio <= ((256f + 32f) / 512f)) fileEnd = "512x256";
        else if (ratio <= ((320f + 32f) / 512f)) fileEnd = "512x320";
        else if (ratio <= ((384f + 32f) / 512f)) fileEnd = "512x384";

        return fileEnd;
    }*/
}
