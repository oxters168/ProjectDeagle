using UnityEngine;

namespace OxGUI
{
    public class OxListFileSelectorPrompt : OxListFileSelector
    {
        private OxButton acceptButton, cancelButton;
        public bool hideCancelButton, horizontalPromptButtons, switchPromptButtonsSide;
        public float percentPromptButtonsSize = 0.2f;
        public int buttonCushion = 2;
        public event OxHelpers.SelectionDone selectionDone;

        public OxListFileSelectorPrompt() : this(Vector2.zero, Vector2.zero) { }
        public OxListFileSelectorPrompt(Vector2 position, Vector2 size) : this(position, size, "") { }
        public OxListFileSelectorPrompt(Vector2 position, Vector2 size, string startingDir) : base(position, size, startingDir)
        {
            acceptButton = new OxButton("Accept");
            acceptButton.elementFunction = OxHelpers.ElementType.Accept;
            acceptButton.parentInfo = new ParentInfo(this, new Rect(position, size));
            acceptButton.clicked += promptButton_clicked;
            cancelButton = new OxButton("Cancel");
            cancelButton.elementFunction = OxHelpers.ElementType.Cancel;
            cancelButton.parentInfo = new ParentInfo(this, new Rect(position, size));
            cancelButton.clicked += promptButton_clicked;
        }

        public override void Draw()
        {
            if (visible)
            {
                float buttonsSize = percentPromptButtonsSize * size.y;

                #region ListFileSelector
                if (horizontalPromptButtons) buttonsSize = percentPromptButtonsSize * size.x;
                Vector2 origPos = position, origSize = size;
                if (horizontalPromptButtons)
                {
                    if (switchPromptButtonsSide)
                    {
                        x += Mathf.RoundToInt(buttonsSize);
                    }
                    width -= Mathf.FloorToInt(buttonsSize);
                }
                else
                {
                    if (switchPromptButtonsSide)
                    {
                        y += Mathf.RoundToInt(buttonsSize);
                    }
                    height -= Mathf.FloorToInt(buttonsSize);
                }

                base.Draw();

                x = Mathf.RoundToInt(origPos.x);
                y = Mathf.RoundToInt(origPos.y);
                width = Mathf.RoundToInt(origSize.x);
                height = Mathf.RoundToInt(origSize.y);
                #endregion

                #region Prompt Buttons
                if (horizontalPromptButtons)
                {
                    if (switchPromptButtonsSide)
                    {
                        acceptButton.x = 0;
                        cancelButton.x = 0;
                    }
                    else
                    {
                        acceptButton.x = Mathf.RoundToInt(width * (1 - percentPromptButtonsSize)) + buttonCushion;
                        cancelButton.x = Mathf.RoundToInt(width * (1 - percentPromptButtonsSize)) + buttonCushion;
                    }
                    acceptButton.y = 0;
                    cancelButton.y = (height / 2) + Mathf.RoundToInt(buttonCushion / 2f);
                    acceptButton.width = Mathf.FloorToInt(buttonsSize) - buttonCushion;
                    cancelButton.width = Mathf.FloorToInt(buttonsSize) - buttonCushion;
                    if (hideCancelButton)
                    {
                        acceptButton.height = height;
                    }
                    else
                    {
                        acceptButton.height = (height / 2) - Mathf.RoundToInt(buttonCushion / 2f);
                    }
                    cancelButton.height = (height / 2) - Mathf.RoundToInt(buttonCushion / 2f);
                }
                else
                {
                    if (switchPromptButtonsSide)
                    {
                        acceptButton.y = 0;
                        cancelButton.y = 0;
                    }
                    else
                    {
                        acceptButton.y = Mathf.RoundToInt(height * (1 - percentPromptButtonsSize)) + buttonCushion;
                        cancelButton.y = Mathf.RoundToInt(height * (1 - percentPromptButtonsSize)) + buttonCushion;
                    }
                    acceptButton.x = 0;
                    cancelButton.x = (width / 2) + Mathf.RoundToInt(buttonCushion / 2f);
                    acceptButton.height = Mathf.FloorToInt(buttonsSize) - buttonCushion;
                    cancelButton.height = Mathf.FloorToInt(buttonsSize) - buttonCushion;
                    if (hideCancelButton)
                    {
                        acceptButton.width = width;
                    }
                    else
                    {
                        acceptButton.width = (width / 2) - Mathf.RoundToInt(buttonCushion / 2f);
                    }
                    cancelButton.width = (width / 2) - Mathf.RoundToInt(buttonCushion / 2f);
                }

                Rect group = new Rect(x, y, width, height);
                acceptButton.parentInfo.group = group;
                cancelButton.parentInfo.group = group;

                GUI.BeginGroup(group);
                acceptButton.Draw();
                if (!hideCancelButton) cancelButton.Draw();
                GUI.EndGroup();
                #endregion
            }
        }

        private void promptButton_clicked(OxBase obj)
        {
            if(obj == acceptButton && selectedItem != null)
            {
                FireSelectionDoneEvent(acceptButton.elementFunction);
            }
            else if(obj == cancelButton)
            {
                FireSelectionDoneEvent(cancelButton.elementFunction);
            }
        }

        protected void FireSelectionDoneEvent(OxHelpers.ElementType selectionType)
        {
            if (selectionDone != null) selectionDone(this, selectionType);
        }
    }
}