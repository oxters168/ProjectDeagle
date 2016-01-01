using UnityEngine;
using System.Collections;

public class OxTextBox : OxGUI
{
	GUIStyle textBoxStyle;
    //public string textBoxText;
    //private bool makeStyle;
    public event TextChangedEventHandler textChanged;

    public OxTextBox(Vector2 position, Vector2 size, GUIStyle style, string text, string textureLoc) : base(position, size, textureLoc)
    {
        if (style != null) textBoxStyle = new GUIStyle(style);
        this.text = text;
        //textBoxText = text;
    }
    public OxTextBox(Vector2 position, Vector2 size, string text) : this(position, size, null, text, "") { }
    public OxTextBox(Vector2 position, Vector2 size) : this(position, size, null, "", "") { }
    public OxTextBox(string text, string texture) : this(new Vector2(0, 0), new Vector2(0, 0), null, text, texture) { }

    public delegate void TextChangedEventHandler(OxGUI sender);
    public override void Draw()
    {
        base.Draw();

        if (visible)
        {
            if (texture.Length > 0)
            {
                if (textBoxStyle == null) textBoxStyle = new GUIStyle();
                if (highlighted) textBoxStyle.normal.background = Resources.Load<Texture2D>(textureLocation + texture + "Hover " + TextureSize());
                else textBoxStyle.normal.background = Resources.Load<Texture2D>(textureLocation + texture + "Up " + TextureSize());
                textBoxStyle.hover.background = Resources.Load<Texture2D>(textureLocation + texture + "Hover " + TextureSize());
                textBoxStyle.active.background = Resources.Load<Texture2D>(textureLocation + texture + "Down " + TextureSize());
                textBoxStyle.normal.textColor = Color.white;
                textBoxStyle.hover.textColor = Color.white;
                textBoxStyle.active.textColor = Color.white;
                textBoxStyle.alignment = TextAnchor.MiddleLeft;
                textBoxStyle.clipping = TextClipping.Clip;
                if (textBoxStyle.clipping == TextClipping.Overflow) textBoxStyle.fontSize = overflowFontSize;
                else textBoxStyle.fontSize = clippedFontSize;
                textBoxStyle.padding.left = 9;
                textBoxStyle.padding.right = 7;
            }
            else
            {
                if (textBoxStyle == null) textBoxStyle = new GUIStyle(GUI.skin.textField);
                textBoxStyle.normal.textColor = Color.white;
                textBoxStyle.hover.textColor = Color.white;
                textBoxStyle.active.textColor = Color.white;
                textBoxStyle.alignment = TextAnchor.MiddleLeft;
                textBoxStyle.clipping = TextClipping.Clip;
                if (textBoxStyle.clipping == TextClipping.Overflow) textBoxStyle.fontSize = overflowFontSize;
                else textBoxStyle.fontSize = clippedFontSize;
                textBoxStyle.padding.left = 9;
                textBoxStyle.padding.right = 7;
            }

            string newText = "";

            //if (textBoxStyle != null)
            //{
                newText = GUI.TextField(new Rect(position.x, position.y, size.x, size.y), text, textBoxStyle);
            //}
            //else
            //{
            //    newText = GUI.TextField(new Rect(position.x, position.y, size.x, size.y), text);
            //}

            if (newText != text)
            {
                text = newText;
                if(textChanged != null) textChanged(this);
                //return true;
            }
        }

        //return false;
    }

    protected override string TextureSize()
    {
        string fileEnd = "512x384";
        float ratio = size.y / size.x;

        if (ratio <= ((128f + 32f) / 512f)) fileEnd = "512x128";
        else if (ratio <= ((192f + 32f) / 512f)) fileEnd = "512x192";
        else if (ratio <= ((256f + 32f) / 512f)) fileEnd = "512x256";
        else if (ratio <= ((320f + 32f) / 512f)) fileEnd = "512x320";
        else if (ratio <= ((384f + 32f) / 512f)) fileEnd = "512x384";

        return fileEnd;
    }
}
