using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
public class TabbedPanel : MonoBehaviour
{
    public TwoPartButton tabButtonPrefab;
    public RectTransform buttonsPanel;

    [Space(10)]
    public TabInfo[] tabs;
    [SerializeField, HideInInspector]
    private int prevTabCount;
    [SerializeField, HideInInspector]
    private List<TwoPartButton> existingTabButtons = new List<TwoPartButton>();
    public int tabIndex;

    private bool addedListeners;

    private void Update()
    {
        UpdateTabs();
        RemoveUnusedButtons();
        AddListeners();
    }

    private void AddListeners()
    {
        if (Application.isPlaying)
        {
            if (tabs != null && !addedListeners)
            {
                for (int i = 0; i < tabs.Length; i++)
                {
                    if (tabs[i] != null && tabs[i].tabButton)
                    {
                        Button buttonComponent = tabs[i].tabButton.GetComponentInChildren<Button>();
                        if (buttonComponent)
                            buttonComponent.onClick.AddListener(() => { TabButtonClicked(buttonComponent.gameObject); });
                    }
                }
                addedListeners = true;
            }
        }
    }
    private void UpdateTabs()
    {
        if (tabs != null)
        {
            for (int i = 0; i < tabs.Length; i++)
            {
                if (tabs[i] != null)
                {
                    if (!tabs[i].tabButton || i >= prevTabCount)
                    {
                        tabs[i].tabButton = Instantiate(tabButtonPrefab);
                        tabs[i].tabButton.transform.SetParent(buttonsPanel, false);
                        existingTabButtons.Add(tabs[i].tabButton);
                    }

                    tabs[i].RefreshButton();

                    if (tabs[i].panel)
                        tabs[i].panel.gameObject.SetActive(i == tabIndex);
                }
            }
        }
        prevTabCount = tabs != null ? tabs.Length : 0;
    }
    private void RemoveUnusedButtons()
    {
        if (existingTabButtons != null)
        {
            for (int i = existingTabButtons.Count - 1; i >= 0; i--)
            {
                if (tabs == null || tabs.Where(tab => tab.tabButton == existingTabButtons[i]).Count() <= 0)
                {
                    if (existingTabButtons[i])
                        DestroyImmediate(existingTabButtons[i].gameObject);

                    existingTabButtons.RemoveAt(i);
                }
            }
        }
    }

    public void TabButtonClicked(GameObject button)
    {
        if (tabs != null)
        {
            TabInfo tabbedTo = tabs.Where(tab => tab.tabButton.gameObject == button).FirstOrDefault();
            if (tabbedTo != null)
                tabIndex = Array.IndexOf(tabs, tabbedTo);
        }
    }
}

[Serializable]
public class TabInfo
{
    public RectTransform panel;
    public Sprite icon;
    public string title;

    public TwoPartButton tabButton;

    public void RefreshButton()
    {
        tabButton.icon.gameObject.SetActive(icon);
        if (icon)
            tabButton.icon.sprite = icon;

        tabButton.buttonText.gameObject.SetActive(!string.IsNullOrEmpty(title));
        if (!string.IsNullOrEmpty(title))
            tabButton.buttonText.text = title;
    }
}