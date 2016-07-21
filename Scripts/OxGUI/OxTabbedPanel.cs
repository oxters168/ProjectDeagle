using UnityEngine;

namespace OxGUI
{
    public class OxTabbedPanel : OxPanel
    {
        private OxMenu tabs;
        public bool verticalTabs, switchTabsSide, switchTabsScrollbarSide;
        public float tabPercentSize = 0.2f;
        public new int selectedIndex { get; protected set; }

        public OxTabbedPanel() : this(Vector2.zero, Vector2.zero) { }
        public OxTabbedPanel(Vector2 position, Vector2 size) : base(position, size)
        {
            selectedIndex = -1;
            tabs = new OxMenu();
            tabs.parentInfo = new ParentInfo(this);
            tabs.hideScrollbar = true;
            tabs.fitItemsWhenLess = true;
            tabs.enableItemSelection = true;
            tabs.disableDeselection = true;
            tabs.horizontal = !verticalTabs;
            tabs.ClearAllAppearances();
            ClearAllAppearances();
        }

        #region Tab Control
        public OxPanel AddTab()
        {
            return AddTab("");
        }
        public OxPanel AddTab(string name)
        {
            OxButton tabButton = new OxButton(name);
            tabButton.clicked += TabButton_clicked;
            tabButton.ClearAllAppearances();
            ApplyAppearanceFromResources(tabButton, "Textures/OxGUI/Element3");
            tabs.AddItems(tabButton);

            Rect tabPanelDimensions = CalculateTabPanelDimensions();
            OxPanel tabPanel = new OxPanel(new Vector2(tabPanelDimensions.x, tabPanelDimensions.y), new Vector2(tabPanelDimensions.width, tabPanelDimensions.height));
            tabPanel.parentInfo = new ParentInfo(this);
            tabPanel.anchor = (OxHelpers.Anchor.Top | OxHelpers.Anchor.Bottom | OxHelpers.Anchor.Left | OxHelpers.Anchor.Right);
            tabPanel.ClearAllAppearances();
            ApplyAppearanceFromResources(tabPanel, "Textures/OxGUI/Element4", true, false, false);
            items.Add(tabPanel);

            if (selectedIndex < 0) { selectedIndex = tabs.IndexOf(tabButton); tabs.SelectItem(tabButton); }
            return tabPanel;
        }
        public bool RemoveTab(OxPanel tabPanel)
        {
            int index = items.IndexOf(tabPanel);
            if(index > -1)
            {
                tabs.RemoveAt(index);
                return items.Remove(tabPanel);
            }

            if (tabs.itemsCount <= 0) selectedIndex = -1;
            return false;
        }
        public void SetTabName(OxPanel tabPanel, string name)
        {
            SetTabName(IndexOf(tabPanel), name);
        }
        public void SetTabName(int index, string name)
        {
            tabs.ItemAt(index).text = name;
        }
        #endregion

        private void TabButton_clicked(OxBase obj)
        {
            //if (!tabs.dragging)
            //{
                OxBase tab = obj;
                if (!tab.isSelected)
                    selectedIndex = tabs.IndexOf(tab);
                //else
                //    selectedIndex = -1;
            //}
        }

        protected override void DrawContainedItems()
        {
            Rect group = new Rect(x, y, width, height);
            float xPos = 0, yPos = 0, drawWidth = width, drawHeight = height;

            GUI.BeginGroup(group);
            #region Tabs
            tabs.parentInfo.group = group;
            tabs.horizontal = !verticalTabs;
            if (verticalTabs) tabs.switchScrollbarSide = !switchTabsSide;
            else tabs.switchScrollbarSide = switchTabsSide;
            if (switchTabsScrollbarSide) tabs.switchScrollbarSide = !tabs.switchScrollbarSide;
            if(verticalTabs)
            {
                drawWidth *= tabPercentSize;
                if(switchTabsSide)
                {
                    xPos = width - drawWidth;
                }
            }
            else
            {
                drawHeight *= tabPercentSize;
                if(switchTabsSide)
                {
                    yPos = height - drawHeight;
                }
            }
            tabs.x = Mathf.RoundToInt(xPos);
            tabs.y = Mathf.RoundToInt(yPos);
            tabs.width = Mathf.RoundToInt(drawWidth);
            tabs.height = Mathf.RoundToInt(drawHeight);
            tabs.Draw();
            #endregion

            #region Panel
            OxBase item = null;
            if (selectedIndex > -1 && selectedIndex < items.Count) item = items[selectedIndex];
            if (item != null)
            {
                item.parentInfo.group = group;
                Rect tabPanelDimensions = CalculateTabPanelDimensions();
                item.position = new Vector2(tabPanelDimensions.x, tabPanelDimensions.y);
                item.size = new Vector2(tabPanelDimensions.width, tabPanelDimensions.height);
                //Debug.Log("Drawing: " + selectedIndex);
                item.Draw();
            }
            #endregion
            GUI.EndGroup();
        }
        private Rect CalculateTabPanelDimensions()
        {
            Rect dimensions = new Rect();
            float tabsSize = height * tabPercentSize;
            if (verticalTabs)
            {
                tabsSize = width * tabPercentSize;
                if (!switchTabsSide)
                {
                    dimensions.x = tabsSize;
                }
                dimensions.width = width - tabsSize;
                dimensions.height = height;
            }
            else
            {
                if (!switchTabsSide)
                {
                    dimensions.y = tabsSize;
                }
                dimensions.width = width;
                dimensions.height = height - tabsSize;
            }
            return dimensions;
        }

        #region Interface
        public override void AddItems(params OxBase[] addedItems)
        {
            //foreach(OxBase item in addedItems)
            //{
            //    if(item is OxPanel)
            //    {
            //        AddTab(((OxPanel)item));
            //    }
            //}
        }
        public override bool RemoveItems(params OxBase[] removedItems)
        {
            bool allRemoved = true;
            foreach(OxBase item in removedItems)
            {
                if (item is OxPanel)
                {
                    bool removedSpecific = RemoveTab(((OxPanel)item));
                    if (!removedSpecific) allRemoved = false;
                }
                else allRemoved = false;
            }
            return allRemoved;
        }
        public override void RemoveAt(int index)
        {
            if(index > -1 && index < items.Count)
            {
                tabs.ItemAt(index).clicked -= TabButton_clicked;
                tabs.RemoveAt(index);
                items[index].parentInfo = null;
                items.RemoveAt(index);
            }
        }
        public override void ClearItems()
        {
            for(int i = items.Count - 1; i >= 0; i--)
            {
                RemoveAt(i);
            }
        }
        #endregion
    }
}