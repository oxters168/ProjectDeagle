using UnityEngine;

namespace OxGUI
{
    public class OxPanel : OxContainer
    {
        public OxPanel() : this(Vector2.zero, Vector2.zero) { }
        public OxPanel(int x, int y, int width, int height) : this(new Vector2(x, y), new Vector2(width, height)) { }
        public OxPanel(Vector2 position, Vector2 size) : base(position, size) { }
    }
}
