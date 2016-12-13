using UnityEngine;
using UnityEngine.UI;

public class Tooltip : MonoBehaviour
{
    Text textComponent;

    public void SetText(string text)
    {
        if (textComponent) textComponent.text = text;
    }
}
