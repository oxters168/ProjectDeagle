using UnityEngine;
using TMPro;

public class ChatMessageController : MonoBehaviour
{
    public TMP_InputField messageArea;
    public bool showOnLeft { set { SetMessageSide(value); } }

    public void SetMessage(string text)
    {
        messageArea.text = text;
        messageArea.ForceLabelUpdate();
    }
    private void SetMessageSide(bool left)
    {
        if (messageArea)
        {
            if (left)
                messageArea.transform.SetAsFirstSibling();
            else
                messageArea.transform.SetAsLastSibling();
        }
    }
}
