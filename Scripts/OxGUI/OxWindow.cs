using UnityEngine;

namespace OxGUI
{
    public class OxWindow : OxContainer
    {
        private OxButton[] containerButtons = new OxButton[9];

        public OxWindow(Vector2 position, Vector2 size) : base(position, size)
        {
            CreateContainerButtons();
        }

        public override void Draw()
        {
            base.Draw();
            if (visible) DrawContainerButtons();
        }

        #region Container Buttons
        private void CreateContainerButtons()
        {
            Rect group = new Rect(position, size);
            for (int i = 0; i < containerButtons.Length; i++)
            {
                containerButtons[i] = new OxButton();
                containerButtons[i].ClearAllAppearances();
                containerButtons[i].parentInfo = new ParentInfo(this, group);
                if (i == ((int)OxHelpers.Alignment.Top)) containerButtons[i].elementFunction = OxHelpers.ElementType.Position_Changer;
                else if (i == ((int)OxHelpers.Alignment.Center)) containerButtons[i].elementFunction = OxHelpers.ElementType.None;
                else containerButtons[i].elementFunction = OxHelpers.ElementType.Size_Changer;
                containerButtons[i].dragged += ContainerButton_dragged;
            }
        }
        private void DrawContainerButtons()
        {
            AppearanceInfo dimensions = CurrentAppearanceInfo();
            Rect group = new Rect(position, size);
            GUI.BeginGroup(group);
            for (int row = 0; row < 3; row++)
            {
                for (int col = 0; col < 3; col++)
                {
                    if (containerButtons[((row * 3) + col)].elementFunction != OxHelpers.ElementType.None)
                    {
                        float xPos = 0, yPos = 0, drawWidth = dimensions.leftSideWidth, drawHeight = dimensions.topSideHeight;
                        if (col > 0) { xPos += dimensions.leftSideWidth; drawWidth = dimensions.centerWidth; }
                        if (col > 1) { xPos += dimensions.centerWidth; drawWidth = dimensions.rightSideWidth; }
                        if (row > 0) { yPos += dimensions.topSideHeight; drawHeight = dimensions.centerHeight; }
                        if (row > 1) { yPos += dimensions.centerHeight; drawHeight = dimensions.bottomSideHeight; }

                        containerButtons[((row * 3) + col)].parentInfo.group = group;
                        containerButtons[((row * 3) + col)].x = Mathf.RoundToInt(xPos);
                        containerButtons[((row * 3) + col)].y = Mathf.RoundToInt(yPos);
                        containerButtons[((row * 3) + col)].width = Mathf.RoundToInt(drawWidth);
                        containerButtons[((row * 3) + col)].height = Mathf.RoundToInt(drawHeight);

                        containerButtons[((row * 3) + col)].Draw();
                    }
                }
            }
            GUI.EndGroup();
        }
        public void SetContainerButtonFunction(OxHelpers.Alignment buttonPosition, OxHelpers.ElementType function)
        {
            containerButtons[((int)buttonPosition)].elementFunction = function;
        }
        public void UndefineContainerButtons()
        {
            for (int i = 0; i < 9; i++)
            {
                SetContainerButtonFunction(((OxHelpers.Alignment)i), OxHelpers.ElementType.None);
            }
        }
        #endregion

        #region Events
        private void ContainerButton_dragged(OxBase obj, Vector2 delta)
        {
            if (obj is OxBase)
            {
                if (obj.elementFunction == OxHelpers.ElementType.Position_Changer)
                {
                    Reposition(position + delta);
                }
                else if (obj.elementFunction == OxHelpers.ElementType.Size_Changer)
                {
                    if (obj == containerButtons[((int)OxHelpers.Alignment.Right)])
                    {
                        Resize(new Vector2(width + delta.x, height));
                    }
                    if (obj == containerButtons[((int)OxHelpers.Alignment.Bottom)])
                    {
                        Resize(new Vector2(width, height + delta.y));
                    }
                    if (obj == containerButtons[((int)OxHelpers.Alignment.Bottom_Right)])
                    {
                        Resize(size + delta);
                    }

                    if (obj == containerButtons[((int)OxHelpers.Alignment.Top_Right)])
                    {
                        Reposition(new Vector2(x, y + delta.y));
                        Resize(new Vector2(width + delta.x, height - delta.y));
                        //MoveContainedItems(new Vector2(0, delta.y));
                        DeepMove(this, new Vector2(0, delta.y));
                    }
                    if (obj == containerButtons[((int)OxHelpers.Alignment.Bottom_Left)])
                    {
                        Reposition(new Vector2(x + delta.x, y));
                        Resize(new Vector2(width - delta.x, height + delta.y));
                        //MoveContainedItems(new Vector2(delta.x, 0));
                        DeepMove(this, new Vector2(delta.x, 0));
                    }

                    if (obj == containerButtons[((int)OxHelpers.Alignment.Left)])
                    {
                        Reposition(new Vector2(x + delta.x, y));
                        Resize(new Vector2(width - delta.x, height));
                        //MoveContainedItems(new Vector2(delta.x, 0));
                        DeepMove(this, new Vector2(delta.x, 0));
                    }
                    if (obj == containerButtons[((int)OxHelpers.Alignment.Top)])
                    {
                        Reposition(new Vector2(x, y + delta.y));
                        Resize(new Vector2(width, height - delta.y));
                        //MoveContainedItems(new Vector2(0, delta.y));
                        DeepMove(this, new Vector2(0, delta.y));
                    }
                    if (obj == containerButtons[((int)OxHelpers.Alignment.Top_Left)])
                    {
                        Reposition(position + delta);
                        Resize(size - delta);
                        //MoveContainedItems(delta);
                        DeepMove(this, delta);
                    }
                }
            }
        }
        #endregion
    }
}