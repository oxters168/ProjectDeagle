using UnityEngine;

namespace OxGUI
{
    public class OxMenu : OxContainer
    {
        public float cushion = 5;
        private OxScrollbar scrollbar;
        public int itemsShown = 5;
        public bool horizontal = false, fitItemsWhenLess = false, hideScrollbar = false, switchScrollbarSide = false;
        public float scrollbarPercentSpaceTaken = 0.2f;
        public float scrollProgress { get { return scrollbar.progress; } set { if (value >= 0 && value <= 1) scrollbar.progress = value; } }

        private float preDrag = 0, amountDragged, drift = 0.05f;
        //public bool isBeingDragged { get; private set; }
        public bool dragging { get; private set; }

        public OxMenu() : this(Vector2.zero, Vector2.zero) { }
        public OxMenu(Vector2 position, Vector2 size) : base(position, size)
        {
            ApplyAppearanceFromResources(this, "Textures/OxGUI/Panel2", true, false, false);
            scrollbar = new OxScrollbar();
            scrollbar.parentInfo = new ParentInfo(this, new Rect(position, size));
            dragged += Item_dragged;
            released += Item_released;
        }

        protected override void DrawContainedItems()
        {
            AppearanceInfo dimensions = CurrentAppearanceInfo();
            Rect group = new Rect(x + dimensions.leftSideWidth, y + dimensions.topSideHeight, dimensions.centerWidth, dimensions.centerHeight);
            GUI.BeginGroup(group);
            float xPos = 0, yPos = 0, menuItemWidth = dimensions.centerWidth, menuItemHeight = dimensions.centerHeight;

            #region Add Scrollbar
            if(itemsCount > itemsShown && !hideScrollbar)
            {
                #region Scrollbar
                float scrollbarXPos = 0, scrollbarYPos = 0, scrollbarWidth = dimensions.centerWidth * scrollbarPercentSpaceTaken, scrollbarHeight = dimensions.centerHeight;
                scrollbar.horizontal = horizontal;
                if (horizontal)
                {
                    scrollbarWidth = dimensions.centerWidth;
                    scrollbarHeight = dimensions.centerHeight * scrollbarPercentSpaceTaken;
                    if(!switchScrollbarSide) scrollbarYPos += dimensions.centerHeight - scrollbarHeight;
                }
                else
                {
                    if(switchScrollbarSide) scrollbarXPos += dimensions.centerWidth - scrollbarWidth;
                }
                scrollbar.parentInfo.group = group;
                scrollbar.x = Mathf.RoundToInt(scrollbarXPos);
                scrollbar.y = Mathf.RoundToInt(scrollbarYPos);
                scrollbar.width = Mathf.RoundToInt(scrollbarWidth);
                scrollbar.height = Mathf.RoundToInt(scrollbarHeight);

                scrollbar.Draw();
                #endregion
                #region Fit Menu Items with Scrollbar
                if(horizontal)
                {
                    if(switchScrollbarSide)
                    {
                        yPos += scrollbarHeight;
                    }
                    menuItemHeight -= scrollbarHeight;
                    
                }
                else
                {
                    if(!switchScrollbarSide)
                    {
                        xPos += scrollbarWidth;
                    }
                    menuItemWidth -= scrollbarWidth;
                    
                }
                #endregion
            }
            #endregion

            int actualItemsShown = itemsShown;
            if (itemsCount < itemsShown && fitItemsWhenLess) actualItemsShown = itemsCount;

            if(horizontal)
            {
                menuItemWidth = ((menuItemWidth - (cushion * (actualItemsShown - 1))) / actualItemsShown);
            }
            else
            {
                menuItemHeight = ((menuItemHeight - (cushion * (actualItemsShown - 1))) / actualItemsShown);
            }

            //float menuItemMainSize = menuItemHeight;
            //if (horizontal) menuItemMainSize = menuItemWidth;
            //float fullListSize = (menuItemMainSize * (itemsCount - itemsShown)) + (cushion * ((itemsCount - itemsShown) - 1));

            float scrollPixelProgress = (items.Count - actualItemsShown) * scrollbar.progress;
            //float scrollPixelProgress = fullListSize * scrollbar.progress;
            
            int index = Mathf.RoundToInt(scrollPixelProgress);
            int firstIndex = index;
            for (int i = 0; i < actualItemsShown; i++)
            {
                if (i + index < items.Count)
                {
                    items[i + index].parentInfo.group = group;

                    float specificIndex = scrollPixelProgress - (firstIndex + i);
                    if (horizontal)
                    {
                        items[i + index].x = Mathf.RoundToInt(xPos - (menuItemWidth * specificIndex) - (cushion * specificIndex));
                        items[i + index].y = Mathf.RoundToInt(yPos);
                    }
                    else
                    {
                        items[i + index].x = Mathf.RoundToInt(xPos);
                        items[i + index].y = Mathf.RoundToInt(yPos - (menuItemHeight * specificIndex) - (cushion * specificIndex));
                    }
                    items[i + index].width = Mathf.RoundToInt(menuItemWidth);
                    items[i + index].height = Mathf.RoundToInt(menuItemHeight);
                    items[i + index].Draw();
                }
            }
            GUI.EndGroup();

            if (amountDragged != 0)
            {
                float menuItemMainSize = menuItemHeight;
                if (horizontal) menuItemMainSize = menuItemWidth;
                float fullListSize = (menuItemMainSize * (itemsCount - itemsShown)) + (cushion * ((itemsCount - itemsShown) - 1));

                float scrollAddition = (amountDragged / fullListSize);
                //float scrollAddition = (amountDragged * ((menuItemMainSize * (itemsCount - itemsShown)) / fullListSize));
                //float scrollAddition = amountDragged / (menuItemMainSize * (itemsCount - itemsShown));

                if ((scrollAddition > 0 && (scrollProgress + scrollAddition) < 1) || (scrollAddition < 0 && (scrollProgress + scrollAddition) > 0)) scrollProgress += scrollAddition;
                else if (scrollAddition > 0) { scrollProgress = 1; amountDragged = 0; }
                else if (scrollAddition < 0) { scrollProgress = 0; amountDragged = 0; }

                if(amountDragged > 0)
                {
                    if (amountDragged - drift > 0) amountDragged -= drift;
                    else amountDragged = 0;
                }
                else
                {
                    if (amountDragged + drift < 0) amountDragged += drift;
                    else amountDragged = 0;
                }
            }
        }

        public override void AddItems(params OxBase[] addedItems)
        {
            base.AddItems(addedItems);
            foreach(OxBase item in addedItems)
            {
                item.dragged += Item_dragged;
                item.released += Item_released;
            }
        }

        private void Item_released(OxBase obj)
        {
            preDrag = 0;
            dragging = false;
            SetItemsClickBlock(false);
        }

        protected override void Item_clicked(OxBase obj)
        {
            if(!dragging) base.Item_clicked(obj);
        }

        private void Item_dragged(OxBase obj, Vector2 delta)
        {
            if (horizontal) preDrag += -delta.x;
            else preDrag += -delta.y;

            if (items.Count > itemsShown && (Mathf.Abs(preDrag) > dragDeadZone || dragging))
            {
                dragging = true;
                SetItemsClickBlock(true);
                //AppearanceInfo dimensions = CurrentAppearanceInfo();
                amountDragged = -delta.y;
                //float itemSize = (dimensions.centerHeight - (cushion * (itemsShown - 1))) / itemsShown;
                if (horizontal)
                {
                    amountDragged += -delta.x;
                    //itemSize = (dimensions.centerWidth - (cushion * (itemsShown - 1))) / itemsShown;
                }
            }
        }

        private void SetItemsClickBlock(bool block)
        {
            foreach(OxBase item in items)
            {
                item.blockClick = block;
            }
        }
    }
}