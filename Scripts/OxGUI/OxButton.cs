using UnityEngine;

namespace OxGUI
{
    public class OxButton : OxBase
    {
        public OxButton() : this(Vector2.zero, Vector2.zero, "") { }
        public OxButton(string text) : this(Vector2.zero, Vector2.zero, text) { }
        public OxButton(int x, int y, int width, int height) : this(new Vector2(x, y), new Vector2(width, height), "") { }
        public OxButton(Vector2 position, Vector2 size) : this(position, size, "") { }
        public OxButton(Vector2 position, Vector2 size, string text) : base(position, size)
        {
            this.text = text;
            ApplyAppearanceFromResources(this, "Textures/OxGUI/Element5");
        }
    }
}