using UnityEngine;

public class ListableItem
{
    public object value;
    public string shownName;
    public string itemType;
    public Sprite image;
    private ListableButton correspondingButton;

    public ListableItem(string _shownName, string _itemType, Sprite _image, object _value)
    {
        shownName = _shownName;
        itemType = _itemType;
        image = _image;
        value = _value;
    }

    public void SetPercent(float value)
    {
        if (correspondingButton)
            correspondingButton.progressBar.value = value;
    }
    public void SetCorrespondingButton(ListableButton button)
    {
        correspondingButton = button;
    }
    public void SetProgressBarVisibility(bool visible)
    {
        if (correspondingButton)
            correspondingButton.progressBar.gameObject.SetActive(visible);
    }

    public override string ToString()
    {
        return shownName;
    }
}
