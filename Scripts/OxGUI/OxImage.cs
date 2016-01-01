using UnityEngine;
using System.Collections;

public class OxImage : OxGUI {

    public Texture2D image;
    public ScaleMode imageScale = ScaleMode.ScaleToFit;

    public OxImage(Texture2D i) : base(new Vector2(0, 0), new Vector2(0, 0))
    {
        selectable = false;
        image = i;
    }
    public OxImage() : this(null) { }

    public override void Draw()
    {
        base.Draw();

        if (visible && image != null)
        {
            GUI.DrawTexture(new Rect(position.x, position.y, size.x, size.y), image, imageScale);
            //GUIStyle imageStyle = new GUIStyle();
            //imageStyle.normal.background = image;
            //GUI.Label(new Rect(position.x, position.y, size.x, size.y), "", imageStyle);
            //return true;
        }
        //return false;
    }
}
