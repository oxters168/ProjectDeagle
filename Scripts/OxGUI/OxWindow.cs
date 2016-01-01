using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class OxWindow : OxFrame
{
    public OxWindow(Vector2 position, Vector2 size, string texture) : base(position, size, texture)
    {
        //resized += OxWindow_resized;
        //if (items != null) windowComponents.AddRange(items);
        //visible = false;
    }
    public OxWindow() : this(Vector2.zero, Vector2.zero, "") { }
}
