using UnityEngine;
using System.Collections.Generic;

namespace OxGUI
{
    public abstract class OxContainer : OxBase, OxContainable
    {
        protected List<OxBase> items = new List<OxBase>();
        public int itemsCount { get { return items.Count; } private set { } }
        private bool itemSelection;
        private bool multiSelect;
        private bool deselectionDisabled;
        public bool enableItemSelection { get { return itemSelection; } set { if (!value) { multiSelect = false; deselectionDisabled = false; } itemSelection = value; } }
        public bool enableMultiSelect { get { return multiSelect; } set { if (itemSelection) multiSelect = value; } }
        public bool disableDeselection { get { return deselectionDisabled; } set { if (itemSelection) deselectionDisabled = value; } }
        public OxBase selectedItem { get { if (selectedItems.Count > 0) return selectedItems[0]; else return null; } }
        public int selectedIndex { get { if (selectedItems.Count > 0) return items.IndexOf(selectedItems[0]); else return -1; } }
        protected List<OxBase> selectedItems = new List<OxBase>();
        public event OxHelpers.SelectionChanged selectionChanged;

        public OxContainer(int x, int y, int width, int height) : this(new Vector2(x, y), new Vector2(width, height)) { }
        public OxContainer(Vector2 position, Vector2 size) : base(position, size)
        {
            ApplyAppearanceFromResources(this, "Textures/OxGUI/Panel2", true, false, false);
            resized += OxPanel_resized;
            moved += OxPanel_moved;
            UpdateNonPixeliness((int)GetTexturableState());
        }

        public override void Draw()
        {
            base.Draw();
            if (visible) DrawContainedItems();
        }

        #region Contained Items
        protected virtual void DrawContainedItems()
        {
            AppearanceInfo dimensions = CurrentAppearanceInfo();
            Rect group = new Rect(x + dimensions.leftSideWidth, y + dimensions.topSideHeight, dimensions.centerWidth, dimensions.centerHeight);
            GUI.BeginGroup(group);
            foreach (OxBase item in items)
            {
                item.parentInfo.group = group;
                item.Draw();
            }
            GUI.EndGroup();
        }
        protected virtual void ResizeContainedItems(Vector2 delta)
        {
            foreach (OxBase item in items)
            {
                Vector2 changeInPosition = delta, changeInSize = Vector2.zero;

                if ((item.anchor & OxHelpers.Anchor.Left) == OxHelpers.Anchor.Left)
                {
                    changeInPosition = new Vector2(0, changeInPosition.y);
                    if ((item.anchor & OxHelpers.Anchor.Right) == OxHelpers.Anchor.Right)
                    {
                        changeInSize = new Vector2(changeInSize.x + delta.x, changeInSize.y);
                    }
                }

                if ((item.anchor & OxHelpers.Anchor.Top) == OxHelpers.Anchor.Top)
                {
                    changeInPosition = new Vector2(changeInPosition.x, 0);
                    if ((item.anchor & OxHelpers.Anchor.Bottom) == OxHelpers.Anchor.Bottom)
                    {
                        changeInSize = new Vector2(changeInSize.x, changeInSize.y + delta.y);
                    }
                }

                item.position += changeInPosition;
                item.size += changeInSize;
            }
        }
        protected virtual void MoveContainedItems(Vector2 delta)
        {
            foreach (OxBase item in items)
            {
                Vector2 changeInPosition = delta;

                if ((item.anchor & OxHelpers.Anchor.Left) == OxHelpers.Anchor.Left || (item.anchor & OxHelpers.Anchor.Right) == OxHelpers.Anchor.Right)
                {
                    changeInPosition = new Vector2(0, changeInPosition.y);
                }

                if ((item.anchor & OxHelpers.Anchor.Top) == OxHelpers.Anchor.Top || (item.anchor & OxHelpers.Anchor.Bottom) == OxHelpers.Anchor.Bottom)
                {
                    changeInPosition = new Vector2(changeInPosition.x, 0);
                }

                item.position += changeInPosition;
            }
        }
        protected static void DeepMove(OxContainer parent, Vector2 delta)
        {
            parent.MoveContainedItems(delta);
            foreach(OxBase item in parent.items)
            {
                if(item is OxContainer)
                {
                    Vector2 configuredDelta = delta;
                    if ((item.anchor & OxHelpers.Anchor.Left) != OxHelpers.Anchor.Left || (item.anchor & OxHelpers.Anchor.Right) != OxHelpers.Anchor.Right) configuredDelta = new Vector2(0, configuredDelta.y);
                    if ((item.anchor & OxHelpers.Anchor.Top) != OxHelpers.Anchor.Top || (item.anchor & OxHelpers.Anchor.Bottom) != OxHelpers.Anchor.Bottom) configuredDelta = new Vector2(configuredDelta.x, 0);
                    DeepMove((OxContainer)item, configuredDelta);
                }
            }
        }
        #endregion

        #region Interface
        public virtual void AddItems(params OxBase[] addedItems)
        {
            AppearanceInfo dimensions = CurrentAppearanceInfo();
            Rect group = new Rect(x + dimensions.leftSideWidth, y + dimensions.topSideHeight, dimensions.centerWidth, dimensions.centerHeight);
            foreach (OxBase item in addedItems)
            {
                if (item != null)
                {
                    item.x = item.absoluteX;
                    item.y = item.absoluteY;
                    item.parentInfo = new ParentInfo(this, group);
                    item.absoluteX = item.x;
                    item.absoluteY = item.y;
                    item.clicked += Item_clicked;
                    items.Add(item);
                }
            }
        }
        public virtual bool RemoveItems(params OxBase[] removedItems)
        {
            bool allRemoved = true;
            foreach (OxBase item in removedItems)
            {
                if (item != null)
                {
                    if (items.IndexOf(item) > -1)
                    {
                        item.x = item.absoluteX;
                        item.y = item.absoluteY;
                        item.parentInfo = null;
                        item.clicked -= Item_clicked;
                        selectedItems.Remove(item);
                        bool removedCurrent = items.Remove(item);
                        if (!removedCurrent) allRemoved = false;
                    }
                }
                else throw new System.ArgumentNullException();
            }

            return allRemoved;
        }
        public virtual void RemoveAt(int index)
        {
            if(index > -1 && index < items.Count) items.RemoveAt(index);
        }
        public virtual void ClearItems()
        {
            items.Clear();
            selectedItems.Clear();
        }
        public virtual OxBase[] GetItems()
        {
            return items.ToArray();
        }
        public virtual OxBase[] GetSelectedItems()
        {
            return selectedItems.ToArray();
        }
        public virtual OxBase ItemAt(int index)
        {
            if (index > -1 && index < items.Count) return items[index];
            return null;
        }
        public virtual int IndexOf(OxBase item)
        {
            return items.IndexOf(item);
        }
        #endregion

        #region Events
        private void OxPanel_resized(OxBase obj, Vector2 delta)
        {
            ResizeContainedItems(delta);
        }
        private void OxPanel_moved(OxBase obj, Vector2 delta)
        {
            //MoveContainedItems(delta);
        }
        protected virtual void Item_clicked(OxBase obj)
        {
            SelectItem(obj);
        }
        #endregion

        public void SelectItem(OxBase item)
        {
            if (item != null && items.IndexOf(item) > -1)
            {
                if (enableMultiSelect)
                {
                    if (item.isSelected)
                    {
                        selectedItems.Remove(item);
                    }
                    else
                    {
                        selectedItems.Add(item);
                    }
                    item.Select(!item.isSelected);
                    FireSelectionChangedEvent(item, item.isSelected);
                }
                else
                {
                    for (int i = selectedItems.Count - 1; i >= 0; i--)
                    {
                        if (selectedItems[i] != item)
                        {
                            selectedItems[i].Select(false);
                            FireSelectionChangedEvent(selectedItems[i], false);
                            selectedItems.RemoveAt(i);
                        }
                    }

                    if (selectedItems.IndexOf(item) > -1 && !deselectionDisabled)
                    {
                        selectedItems.Remove(item);
                        item.Select(false);
                        FireSelectionChangedEvent(item, false);
                    }
                    else
                    {
                        selectedItems.Add(item);
                        if (enableItemSelection) item.Select(true);
                        FireSelectionChangedEvent(item, true);
                    }
                }
            }
        }

        protected void FireSelectionChangedEvent(OxBase item, bool selected)
        {
            if (selectionChanged != null) selectionChanged(this, item, selected);
        }
    }
}