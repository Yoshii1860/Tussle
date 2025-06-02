using UnityEngine;
using TMPro;
using Unity.Collections;
using Unity.Netcode;

public class LeaderboardEntityDisplay : MonoBehaviour
{
    [SerializeField] private TMP_Text displayText;
    //[SerializeField] private Color selfColor = Color.red;
    [SerializeField] public const int KillScoreMultiplier = 50;

    private FixedString32Bytes displayName;
    public int TeamIndex { get; private set; }
    public ulong ClientId { get; private set; }
    public int Kills { get; private set; }
    public int Coins { get; private set; }

    public void Initialise(ulong clientId, FixedString32Bytes displayName, int kills, int coins)
    {
        ClientId = clientId;
        this.displayName = displayName;
/*
        if (clientId == NetworkManager.Singleton.LocalClientId)
        {
            displayText.color = selfColor;
        }
*/        
        UpdateValues(coins, kills);
    }

    public void Initialise(int teamIndex, FixedString32Bytes displayName, int kills, int coins)
    {
        TeamIndex = teamIndex;
        this.displayName = displayName;

        UpdateValues(coins, kills);
    }

    public void SetColor(Color color)
    {
        displayText.color = color;
    }

    public void UpdateValues(int coins, int kills)
    {
        Coins = coins;
        Kills = kills;

        UpdateDisplayText();
    }

    public void UpdateDisplayText()
    {
        displayText.text = $"{transform.GetSiblingIndex()+1}. {displayName} - {CalculateScore()}";
    }

    public int CalculateScore()
    {
        return (Kills * KillScoreMultiplier) + Coins;
    }
}
