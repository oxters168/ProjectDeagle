using UnityEngine;

namespace OxGUI
{
    public class OxLabel : OxBase
    {
        public OxLabel(OxHelpers.Alignment textAlignment) : this("", Color.black, textAlignment) { }
        public OxLabel(Color textColor) : this("", textColor, OxHelpers.Alignment.Center) { }
        public OxLabel(string text) : this(text, Color.black, OxHelpers.Alignment.Center) { }
        public OxLabel(string text, Color textColor, OxHelpers.Alignment textAlignment) : this(Vector2.zero, Vector2.zero, text, textColor, textAlignment) { }
        public OxLabel() : this(Vector2.zero, Vector2.zero) { }
        public OxLabel(Vector2 position, Vector2 size) : this(position, size, "", Color.black, OxHelpers.Alignment.Center) { }
        public OxLabel(Vector2 position, Vector2 size, string text, Color textColor, OxHelpers.Alignment textAlignment) : base(position, size)
        {
            this.text = text;
            this.textColor = textColor;
            this.textAlignment = textAlignment;
        }
    }
}