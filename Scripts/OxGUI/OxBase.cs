using UnityEngine;
using System.Collections.Generic;

namespace OxGUI
{
    public abstract class OxBase
    {
        #region Functional Variables
        public bool visible = true;
        public bool enabled = true;
        public bool isSelected { get; internal set; }
        public OxHelpers.Anchor anchor;
        public OxHelpers.ElementType elementFunction;
        public object value;
        public bool blockClick;
        public float dragDeadZone = Mathf.Max(Screen.width * 0.01f, Screen.height * 0.01f);
        #endregion

        #region Text Variables
        public const int MAX_FONT_SIZE = 256, MIN_FONT_SIZE = 8;
        public string text = "";
        public Color textColor = Color.black;
        public OxHelpers.Alignment textPosition = OxHelpers.Alignment.Center;
        public OxHelpers.Alignment textAlignment = OxHelpers.Alignment.Center;
        public static int allTextSize = OxHelpers.CalculateFontSize(OxHelpers.InchesToPixel(new Vector2(0, 0.2f)).y);
        public static bool autoSizeAllText = false, manualSizeAllText = false;
        public int textSize = OxHelpers.CalculateFontSize(OxHelpers.InchesToPixel(new Vector2(0, 0.2f)).y);
        public bool autoSizeText = false;
        #endregion

        #region Texture Variables
        internal ParentInfo parentInfo = null;
        protected Texture2D[,] appearances = new Texture2D[3, 9];
        protected Vector2 centerPercentSize = new Vector2(0.5f, 0.5f);
        public OxHelpers.ElementState currentState { get; internal set; }
        public float centerPercentWidth { get { return centerPercentSize.x; } set { if (value >= 0 && value <= 1) centerPercentSize = new Vector2(value, centerPercentSize.y); } }
        public float centerPercentHeight { get { return centerPercentSize.y; } set { if (value >= 0 && value <= 1) centerPercentSize = new Vector2(centerPercentSize.x, value); } }
        internal AppearanceOrigInfo[] origInfo = new AppearanceOrigInfo[3];
        #endregion

        /// <summary>
        /// Can only be set within the same namespace, but only set if you don't
        /// want to fire the repositioned event.
        /// </summary>
        public int x { get; internal set; }
        public int absoluteX { get { float addedParentX = 0; ParentInfo pi = parentInfo; while (pi != null) { addedParentX += pi.group.x; pi = pi.parent.parentInfo; } return x + Mathf.RoundToInt(addedParentX); } internal set { float addedParentX = 0; ParentInfo pi = parentInfo; while (pi != null) { addedParentX += pi.group.x; pi = pi.parent.parentInfo; } x = Mathf.RoundToInt(value - addedParentX); } }
        /// <summary>
        /// Can only be set within the same namespace, but only set if you don't
        /// want to fire the repositioned event.
        /// </summary>
        public int y { get; internal set; }
        public int absoluteY { get { float addedParentY = 0; ParentInfo pi = parentInfo; while (pi != null) { addedParentY += pi.group.y; pi = pi.parent.parentInfo; } return y + Mathf.RoundToInt(addedParentY); } internal set { float addedParentY = 0; ParentInfo pi = parentInfo; while (pi != null) { addedParentY += pi.group.y; pi = pi.parent.parentInfo; } y = Mathf.RoundToInt(value - addedParentY); } }
        /// <summary>
        /// Can only be set within the same namespace, but only set if you don't
        /// want to fire the resized event.
        /// </summary>
        public int width { get; internal set; }
        /// <summary>
        /// Can only be set within the same namespace, but only set if you don't
        /// want to fire the resized event.
        /// </summary>
        public int height { get; internal set; }
        /// <summary>
        /// When set, calls the Reposition method, which then fires the repositioned event
        /// </summary>
        public Vector2 position { get { return new Vector2(x, y); } set { Reposition(value); } }
        /// <summary>
        /// When set, calls the Resize method, which then fires the resized event
        /// </summary>
        public Vector2 size { get { return new Vector2(width, height); } set { Resize(value); } }

        public OxBase(Vector2 position, Vector2 size)
        {
            x = Mathf.RoundToInt(position.x);
            y = Mathf.RoundToInt(position.y);
            width = Mathf.RoundToInt(size.x);
            height = Mathf.RoundToInt(size.y);
        }

        public virtual void Draw()
        {
            if (visible)
            {
                ManageActiveElements();
                InteractionSystem();
                Paint();
            }
        }

        private static int currentIndex = 0;
        private static bool alternator = false;
        private bool currentAlteration = false;
        //private string prevNumElements = "";
        private void ManageActiveElements()
        {
            if (activeElements.IndexOf(this) > currentIndex)
            {
                alternator = !alternator;
            }

            if (enabled)
            {
                if (activeElements.IndexOf(this) < 0)
                {
                    activeElements.Insert(0, this);
                }
                currentIndex = activeElements.IndexOf(this);
                currentAlteration = alternator;
            }
            else
            {
                if (activeElements.IndexOf(this) > -1)
                {
                    activeElements.Remove(this);
                }
            }

            //while (activeElements.Count > 0 && (activeElements[0] == null || activeElements[0].currentAlteration != alternator))
            //{
            //    activeElements.RemoveAt(0);
            //}

            //for(int i = currentIndex - 1; i >= 0; i--)
            //{
            //    if (activeElements[i] == null || activeElements[i].currentAlteration != alternator)
            //    {
            //        activeElements.RemoveAt(i);
            //        currentIndex--;
            //    }
            //}
            //string currentNumElements = activeElements.Count + " Elements";
            //if(!currentNumElements.Equals(prevNumElements)) Debug.Log(currentNumElements);
            //prevNumElements = currentNumElements;
        }
        internal virtual void Paint()
        {
            TexturePaint();
            TextPaint();
        }
        internal virtual void TexturePaint()
        {
            AppearanceInfo dimensions = CurrentAppearanceInfo();
            Texture2D currentTexture = null;

            UpdateNonPixeliness(((int)dimensions.state));

            float xPos = x, yPos = y, drawWidth = dimensions.leftSideWidth, drawHeight = dimensions.topSideHeight;

            for (int row = 0; row < 3; row++)
            {
                for (int col = 0; col < 3; col++)
                {
                    xPos = x; yPos = y; drawWidth = dimensions.leftSideWidth; drawHeight = dimensions.topSideHeight;
                    if (col > 0) { xPos += dimensions.leftSideWidth; drawWidth = dimensions.centerWidth; }
                    if (col > 1) { xPos += dimensions.centerWidth; drawWidth = dimensions.rightSideWidth; }
                    if (row > 0) { yPos += dimensions.topSideHeight; drawHeight = dimensions.centerHeight; }
                    if (row > 1) { yPos += dimensions.centerHeight; drawHeight = dimensions.bottomSideHeight; }

                    currentTexture = appearances[((int)dimensions.state), ((row * 3) + col)];
                    if (currentTexture != null) GUI.DrawTexture(new Rect(xPos, yPos, drawWidth, drawHeight), currentTexture);
                }
            }
        }
        internal virtual void TextPaint()
        {
            AppearanceInfo dimensions = CurrentAppearanceInfo();
            float xPos = x, yPos = y, drawWidth = dimensions.leftSideWidth, drawHeight = dimensions.topSideHeight;

            GUIStyle textStyle = new GUIStyle();
            if (manualSizeAllText) textStyle.fontSize = allTextSize;
            else if (autoSizeAllText) textStyle.fontSize = OxHelpers.CalculateFontSize(OxHelpers.InchesToPixel(new Vector2(0, 0.2f)).y);
            else if (autoSizeText) textStyle.fontSize = OxHelpers.CalculateFontSize(dimensions.centerHeight);
            else textStyle.fontSize = textSize;
            textStyle.normal.textColor = textColor;
            textStyle.alignment = ((TextAnchor)textAlignment);
            textStyle.clipping = TextClipping.Clip;
            xPos = x; yPos = y; drawWidth = dimensions.leftSideWidth; drawHeight = dimensions.topSideHeight;
            if (textPosition == OxHelpers.Alignment.Top || textPosition == OxHelpers.Alignment.Center || textPosition == OxHelpers.Alignment.Bottom)
            {
                xPos += dimensions.leftSideWidth;
                drawWidth = dimensions.centerWidth;
            }
            else if (textPosition == OxHelpers.Alignment.Top_Right || textPosition == OxHelpers.Alignment.Right || textPosition == OxHelpers.Alignment.Bottom_Right)
            {
                xPos += dimensions.leftSideWidth + dimensions.centerWidth;
                drawWidth = dimensions.rightSideWidth;
            }
            if (textPosition == OxHelpers.Alignment.Left || textPosition == OxHelpers.Alignment.Center || textPosition == OxHelpers.Alignment.Right)
            {
                yPos += dimensions.topSideHeight;
                drawHeight = dimensions.centerHeight;
            }
            else if (textPosition == OxHelpers.Alignment.Bottom_Left || textPosition == OxHelpers.Alignment.Bottom || textPosition == OxHelpers.Alignment.Bottom_Right)
            {
                yPos += dimensions.topSideHeight + dimensions.centerHeight;
                drawHeight = dimensions.bottomSideHeight;
            }

            string shownText = text;
            if (shownText.Length <= 0 && value != null) shownText = value.ToString();
            GUI.Label(new Rect(xPos, yPos, drawWidth, drawHeight), shownText, textStyle);
        }
        internal void UpdateNonPixeliness(int availableState)
        {
            float calculatedSideWidth = origInfo[availableState].originalSideWidth, calculatedSideHeight = origInfo[availableState].originalSideHeight;
            float horizontalPercentDifference = width / origInfo[availableState].originalWidth, verticalPercentDifference = height / origInfo[availableState].originalHeight;

            if (width < origInfo[availableState].originalWidth && horizontalPercentDifference < verticalPercentDifference)
            {
                calculatedSideWidth *= horizontalPercentDifference;
                calculatedSideHeight *= horizontalPercentDifference;
            }
            else if (height < origInfo[availableState].originalHeight)
            {
                calculatedSideWidth *= verticalPercentDifference;
                calculatedSideHeight *= verticalPercentDifference;
            }

            centerPercentWidth = (width - calculatedSideWidth) / width;
            centerPercentHeight = (height - calculatedSideHeight) / height;
        }
        protected OxHelpers.ElementState GetTexturableState()
        {
            OxHelpers.ElementState availableState = currentState;
            if (appearances[((int)availableState), ((int)OxHelpers.Alignment.Center)] == null)
            {
                if (appearances[((int)OxHelpers.ElementState.Normal), ((int)OxHelpers.Alignment.Center)] != null) availableState = OxHelpers.ElementState.Normal;
                if (appearances[((int)OxHelpers.ElementState.Highlighted), ((int)OxHelpers.Alignment.Center)] != null) availableState = OxHelpers.ElementState.Highlighted;
                if (appearances[((int)OxHelpers.ElementState.Down), ((int)OxHelpers.Alignment.Center)] != null) availableState = OxHelpers.ElementState.Down;
            }
            return availableState;
        }
        internal AppearanceInfo CurrentAppearanceInfo()
        {
            AppearanceInfo appearanceInfo = new AppearanceInfo();
            appearanceInfo.state = GetTexturableState();
            appearanceInfo.centerWidth = width * centerPercentSize.x;
            appearanceInfo.centerHeight = height * centerPercentSize.y;
            appearanceInfo.rightSideWidth = (width - appearanceInfo.centerWidth) * origInfo[((int)appearanceInfo.state)].percentRight;
            appearanceInfo.leftSideWidth = (width - appearanceInfo.centerWidth) * (1 - origInfo[((int)appearanceInfo.state)].percentRight);
            appearanceInfo.topSideHeight = (height - appearanceInfo.centerHeight) * origInfo[((int)appearanceInfo.state)].percentTop;
            appearanceInfo.bottomSideHeight = (height - appearanceInfo.centerHeight) * (1 - origInfo[((int)appearanceInfo.state)].percentTop);
            return appearanceInfo;
        }

        #region Common Textures
        private static readonly string[] textureLocationTypes = new string[] { "TopLeft", "Top", "TopRight", "Left", "Center", "Right", "BottomLeft", "Bottom", "BottomRight" };
        public static void ApplyAppearanceFromResources(OxBase element, string location, bool normal = true, bool over = true, bool down = true)
        {
            string adjustedLocation = location.Replace("\\", "/");
            if (adjustedLocation.LastIndexOf("/") < adjustedLocation.Length - 1) adjustedLocation += "/";

            for (int i = 0; i < 3; i++)
            {
                if ((i == 0 && normal) || (i == 1 && over) || (i == 2 && down))
                {
                    bool nulled = false;
                    Texture2D[] loadedTextures = new Texture2D[textureLocationTypes.Length];
                    for (int j = 0; j < textureLocationTypes.Length; j++)
                    {
                        string textureStateType = "Normal/";
                        if (i == 1) textureStateType = "Over/";
                        else if (i == 2) textureStateType = "Down/";

                        loadedTextures[j] = Resources.Load<Texture2D>(adjustedLocation + textureStateType + textureLocationTypes[j]);
                        if (loadedTextures[j] == null) { Debug.Log("Assets/Resources/" + adjustedLocation + textureStateType + textureLocationTypes[j] + " does not exist"); nulled = true; break; }
                    }
                    if (!nulled) element.AddAppearance(((OxHelpers.ElementState)i), loadedTextures);
                }
            }
        }
        #endregion

        #region User Interactibality
        private static List<OxBase> activeElements = new List<OxBase>();
        public static OxBase currentlyHighlighted { get; private set; }
        public static OxBase currentlyPressed { get; private set; }
        private static Vector2 prevMousePosition = new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y);
        private bool mouseIsOver, mouseIsDown;
        private event OxHelpers.MouseMovedHandler mouseMoved;
        private event OxHelpers.MouseOverHandler mouseOver;
        private event OxHelpers.MouseLeaveHandler mouseLeave;
        private event OxHelpers.MouseDownHandler mouseDown;
        private event OxHelpers.MouseUpHandler mouseUp;

        private static void InteractionSystem()
        {
            //if (currentIndex == 1)
            //{
                //string activeElementsString = "";
                //foreach(OxBase someElement in activeElements)
                //{
                //    if(!(someElement is OxButton)) activeElementsString += someElement.GetType().Name + ", ";
                //}
                //Debug.Log(activeElements.Count + ": " + activeElementsString);
                for (int i = activeElements.Count - 1; i >= currentIndex; i--)
                {
                    if (activeElements[i] == null || activeElements[i].currentAlteration != alternator)
                    {
                        activeElements.RemoveAt(i);
                    }
                }

                Vector2 mousePosition = new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y);
                for (int i = 0; i < activeElements.Count; i++)
                {
                    OxBase element = activeElements[i];
                    if (element != null && element.enabled)
                    {
                        if (mousePosition.x > (element.absoluteX) && mousePosition.x < (element.absoluteX + element.width) && mousePosition.y > (element.absoluteY) && mousePosition.y < (element.absoluteY + element.height))
                        {
                            if (!element.mouseIsOver && currentlyHighlighted == null) { element.FireMouseOverEvent(); element.mouseIsOver = true; element.Highlight(true); }
                        }
                        else
                        {
                            if (element.mouseIsOver) { element.FireMouseLeaveEvent(); element.mouseIsOver = false; element.Highlight(false); }
                        }
                        if (Input.GetMouseButton(0))
                        {
                            if (element.mouseIsOver && !element.mouseIsDown && currentlyPressed == null) { element.FireMouseDownEvent(); element.mouseIsDown = true; element.Press(); }
                        }
                        else
                        {
                            if (element.mouseIsDown) { element.FireMouseUpEvent(); element.mouseIsDown = false; if (element.mouseIsOver && !element.blockClick) element.Click(); element.Release(); }
                        }

                        if (element.mouseIsOver && currentlyHighlighted == null) currentlyHighlighted = element;
                        //else if (!element.mouseIsOver && currentlyHighlighted == element) currentlyHighlighted = null;
                        if (element.mouseIsDown && currentlyPressed == null) currentlyPressed = element;
                        if (currentlyPressed != null && (!currentlyPressed.mouseIsDown || activeElements.IndexOf(currentlyPressed) < 0)) currentlyPressed = null;
                        //else if (!element.mouseIsDown && currentlyPressed == element) currentlyPressed = null;
                        //if (activeElements.IndexOf(currentlyPressed) < 0) currentlyPressed = null;
                        //if (currentlyPressed != null) Debug.Log("Pressed: " + currentlyPressed.text);
                        //if (currentlyHighlighted != null) Debug.Log("Highlighted: " + currentlyHighlighted.text);

                        if (Vector2.Distance(prevMousePosition, mousePosition) > 0)
                        {
                            element.FireMouseMovedEvent(mousePosition - prevMousePosition);
                            if (element == currentlyPressed)
                            {
                                element.FireDraggedEvent(mousePosition - prevMousePosition);
                            }
                        }

                        if (element.isSelected) element.currentState = OxHelpers.ElementState.Highlighted;
                    }
                }
                currentlyHighlighted = null;
                //currentlyPressed = null;
                prevMousePosition = mousePosition;
            //}
        }
        #endregion

        #region Extra Shared Attributes
        public event OxHelpers.MovedHandler moved;
        public event OxHelpers.DraggedHandler dragged;
        public event OxHelpers.ResizedHandler resized;
        public event OxHelpers.HighlightedHandler highlightedChanged;
        public event OxHelpers.PressedHandler pressed;
        public event OxHelpers.ReleasedHandler released;
        public event OxHelpers.ClickedHandler clicked;
        public event OxHelpers.SelectedHandler selected;

        public virtual void Reposition(int newX, int newY)
        {
            Reposition(new Vector2(newX, newY));
        }
        public virtual void Reposition(Vector2 newPosition)
        {
            Vector2 delta = newPosition - position;
            x = Mathf.RoundToInt(newPosition.x);
            y = Mathf.RoundToInt(newPosition.y);
            FireMovedEvent(delta);
        }
        public virtual void Resize(int w, int h)
        {
            Resize(new Vector2(w, h));
        }
        public virtual void Resize(Vector2 newSize)
        {
            Vector2 delta = newSize - size;
            if (delta != Vector2.zero)
            {
                width = Mathf.RoundToInt(newSize.x);
                height = Mathf.RoundToInt(newSize.y);

                FireResizedEvent(delta);
            }
        }

        public virtual void Highlight(bool onOff)
        {
            if (onOff) currentState = OxHelpers.ElementState.Highlighted;
            else currentState = OxHelpers.ElementState.Normal;
            FireHighlightChangedEvent(onOff);
        }
        public virtual void Press()
        {
            currentState = OxHelpers.ElementState.Down;
            FirePressedEvent();
        }
        public virtual void Release()
        {
            if(mouseIsOver) currentState = OxHelpers.ElementState.Highlighted;
            else currentState = OxHelpers.ElementState.Normal;
            FireReleasedEvent();
        }
        public virtual void Click()
        {
            if (mouseIsOver) currentState = OxHelpers.ElementState.Highlighted;
            else currentState = OxHelpers.ElementState.Normal;
            FireClickedEvent();
        }
        public virtual void Select(bool onOff)
        {
            if (isSelected != onOff)
            {
                isSelected = onOff;
                if (onOff) currentState = OxHelpers.ElementState.Highlighted;
                else currentState = OxHelpers.ElementState.Normal;
                FireSelectedEvent(onOff);
            }
        }

        public void AddAppearance(OxHelpers.ElementState type, Texture2D[] appearance)
        {
            if (appearance.Length == 9)
            {
                for (int i = 0; i < 9; i++)
                {
                    Texture2D currentTexture = appearance[i];
                    Texture2D safeTexture = new Texture2D(currentTexture.width, currentTexture.height);
                    safeTexture.SetPixels(currentTexture.GetPixels());
                    safeTexture.Apply();
                    appearances[((int)type), i] = safeTexture;
                }

                float centerWidth = appearance[(int)OxHelpers.Alignment.Center].width, rightWidth = appearance[(int)OxHelpers.Alignment.Right].width, leftWidth = appearance[(int)OxHelpers.Alignment.Left].width;
                float centerHeight = appearance[(int)OxHelpers.Alignment.Center].height, topHeight = appearance[(int)OxHelpers.Alignment.Top].height, bottomHeight = appearance[(int)OxHelpers.Alignment.Bottom].height;
                float percentWidth = centerWidth / (centerWidth + rightWidth + leftWidth);
                float percentHeight = centerHeight / (centerHeight + topHeight + bottomHeight);

                centerPercentWidth = percentWidth;
                centerPercentHeight = percentHeight;

                origInfo[(int)type].originalWidth = leftWidth + centerWidth + rightWidth;
                origInfo[(int)type].originalHeight = topHeight + centerHeight + bottomHeight;
                origInfo[(int)type].originalSideWidth = leftWidth + rightWidth;
                origInfo[(int)type].percentRight = rightWidth / origInfo[(int)type].originalSideWidth;
                origInfo[(int)type].originalSideHeight = topHeight + bottomHeight;
                origInfo[(int)type].percentTop = topHeight / origInfo[(int)type].originalSideHeight;
            }
        }
        public void ClearAppearance(OxHelpers.ElementState type)
        {
            for(int i = 0; i < 9; i++)
            {
                appearances[((int)type), i] = null;
            }

            origInfo[(int)type].originalWidth = 0;
            origInfo[(int)type].originalHeight = 0;
            origInfo[(int)type].originalSideWidth = 0;
            origInfo[(int)type].percentRight = 0;
            origInfo[(int)type].originalSideHeight = 0;
            origInfo[(int)type].percentTop = 0;
        }
        public void ClearAllAppearances()
        {
            ClearAppearance(OxHelpers.ElementState.Normal);
            ClearAppearance(OxHelpers.ElementState.Highlighted);
            ClearAppearance(OxHelpers.ElementState.Down);
        }
        #endregion

        #region Event Firers
        protected void FireResizedEvent(Vector2 delta)
        {
            if (resized != null) resized(this, delta);
        }
        protected void FireMovedEvent(Vector2 delta)
        {
            if (moved != null) moved(this, delta);
        }
        protected void FireDraggedEvent(Vector2 delta)
        {
            if (dragged != null) dragged(this, delta);
        }
        protected void FireHighlightChangedEvent(bool onOff)
        {
            if (highlightedChanged != null) highlightedChanged(this, onOff);
        }
        protected void FirePressedEvent()
        {
            if (pressed != null) pressed(this);
        }
        protected void FireReleasedEvent()
        {
            if (released != null) released(this);
        }
        protected void FireClickedEvent()
        {
            if (clicked != null) clicked(this);
        }
        protected void FireSelectedEvent(bool onOff)
        {
            if (selected != null) selected(this, onOff);
        }

        protected void FireMouseMovedEvent(Vector2 delta)
        {
            if (mouseMoved != null) mouseMoved(this, delta);
        }
        protected void FireMouseOverEvent()
        {
            if (mouseOver != null) mouseOver(this);
        }
        protected void FireMouseLeaveEvent()
        {
            if (mouseLeave != null) mouseLeave(this);
        }
        protected void FireMouseDownEvent()
        {
            if (mouseDown != null) mouseDown(this, OxHelpers.MouseButton.Left_Button);
        }
        protected void FireMouseUpEvent()
        {
            if (mouseUp != null) mouseUp(this, OxHelpers.MouseButton.Left_Button);
        }
        #endregion
    }

    internal class ParentInfo
    {
        public ParentInfo(OxBase parent) : this(parent, new Rect()) { }
        public ParentInfo(OxBase parent, Rect group)
        {
            this.parent = parent;
            this.group = group;
        }
        public OxBase parent { get; internal set; }
        public Rect group { get; internal set; }
    }

    internal struct AppearanceOrigInfo
    {
        public float originalWidth, originalHeight, originalSideWidth, percentRight, originalSideHeight, percentTop;
    }

    internal struct AppearanceInfo
    {
        public OxHelpers.ElementState state;
        public float centerWidth, centerHeight, rightSideWidth, leftSideWidth, topSideHeight, bottomSideHeight;
    }
}