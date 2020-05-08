using TMPro;
using UnityEngine.UI;

public class MatchListItemController : ListItemController, IInteractableListItem
{
    public TextMeshProUGUI score1, score2;
    public TextMeshProUGUI map;
    public Image result;

    public UnityEngine.Color ctColor, tColor, tieColor;

    public event InteractionEventHandler onClick;

    void Update()
    {
        RefreshInfo();
    }

    private void RefreshInfo()
    {
        if (item != null && item is MatchInfo)
        {
            var matchInfo = (MatchInfo)item;

            var lastRoundStats = matchInfo.GetLastRoundStats();
            if (lastRoundStats != null)
            {
                score1.text = lastRoundStats.team_scores[0].ToString();
                score2.text = lastRoundStats.team_scores[1].ToString();
                //map.text = ((GameType)lastRoundStats.reservation.game_type).ToString();
                map.text = matchInfo.GetMap().GetMapName();
                result.color = lastRoundStats.match_result == 1 ? ctColor : (lastRoundStats.match_result == 2 ? tColor : tieColor);
            }
            else
            {
                map.text = matchInfo.fileName;
            }
        }
    }

    public void Click()
    {
        onClick?.Invoke(item);
    }
}
