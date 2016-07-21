using UnityEngine;

namespace OxGUI
{
    public class OxTextbox : OxBase
    {
        public Font font;
        public bool multiline = false;
        public bool wordWrap = false;
        public TextClipping clipping = TextClipping.Clip;
        public Vector2 contentOffset;
        public FontStyle fontStyle;
        public bool richText;
        public event OxHelpers.TextChanged textChanged;

        public OxTextbox() : this(Vector2.zero, Vector2.zero, "") { }
        public OxTextbox(string text) : this(Vector2.zero, Vector2.zero, text) { }
        public OxTextbox(Vector2 position, Vector2 size) : this(position, size, "") { }
        public OxTextbox(Vector2 position, Vector2 size, string text) : base(position, size)
        {
            this.text = text;
            ApplyAppearanceFromResources(this, "Textures/OxGUI/Checkbox", true, true, false);
        }

        internal override void TextPaint()
        {
            AppearanceInfo dimensions = CurrentAppearanceInfo();
            GUIStyle textStyle = new GUIStyle();
            textStyle.font = font;
            if (manualSizeAllText) textStyle.fontSize = allTextSize;
            else if (autoSizeAllText) textStyle.fontSize = OxHelpers.CalculateFontSize(OxHelpers.InchesToPixel(new Vector2(0, 0.2f)).y);
            else if (autoSizeText) textStyle.fontSize = OxHelpers.CalculateFontSize(dimensions.centerHeight);
            else textStyle.fontSize = textSize;
            textStyle.normal.textColor = textColor;
            textStyle.wordWrap = wordWrap;
            textStyle.alignment = ((TextAnchor)textAlignment);
            textStyle.clipping = clipping;
            textStyle.contentOffset = contentOffset;
            textStyle.fontStyle = fontStyle;
            textStyle.richText = richText;

            //string shownText = text;
            if (text.Length <= 0 && value != null) text = value.ToString();
            string prevText = text;
            if (multiline) text = GUI.TextArea(new Rect(x + dimensions.leftSideWidth, y + dimensions.topSideHeight, dimensions.centerWidth, dimensions.centerHeight), text, textStyle);
            else text = GUI.TextField(new Rect(x + dimensions.leftSideWidth, y + dimensions.topSideHeight, dimensions.centerWidth, dimensions.centerHeight), text, textStyle);
            if (!prevText.Equals(text)) FireTextChangedEvent(prevText);
        }

        protected void FireTextChangedEvent(string prevText)
        {
            if (textChanged != null) textChanged(this, prevText);
        }
    }
}