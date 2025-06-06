using UnityEngine;
using Unity.Netcode;
using UnityEngine.UI;

public class SecondStatDisplay : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private SecondStat secondStat;
    [SerializeField] private Image secondStatBarImage;
    //[SerializeField] private Color fullHealthColor = Color.green;
    //[SerializeField] private Color lowHealthColor = Color.red;

    public override void OnNetworkSpawn()
    {
        if (!IsClient) { return; }
    }

    public void InitializeGameHUDSecondStatBar(GameObject character, Sprite secondStatSprite)
    {
        secondStat = character.GetComponent<SecondStat>();
        if (secondStat == null) { Debug.LogError("SecondStat component not found on character. SecondStatDisplay will not function correctly."); }
        secondStatBarImage.sprite = secondStatSprite;
        
        if (IsClient && secondStat != null)
        {
            secondStat.CurrentSecondStat.OnValueChanged += OnSecondStatChanged;
            OnSecondStatChanged(0, secondStat.CurrentSecondStat.Value);
        }
        else
        {
            Debug.LogWarning("SecondStat component not found on LocalPlayerObject. SecondStatDisplay will not function correctly.");
        }
    }

    public override void OnNetworkDespawn()
    {
        if (!IsClient) { return; }

        secondStat.CurrentSecondStat.OnValueChanged -= OnSecondStatChanged;
    }

    private void OnSecondStatChanged(int oldSecondStat, int newSecondStat)
    {
        float secondStatPercentage = (float)newSecondStat / secondStat.MaxSecondStat;
        secondStatBarImage.fillAmount = secondStatPercentage;
        Debug.Log($"SecondStat changed: {oldSecondStat} -> {newSecondStat}, Percentage: {secondStatPercentage}");
        Debug.Log($"SecondStatBarImage fill amount: {secondStatBarImage.fillAmount}");
        //healthBarImage.color = Color.Lerp(lowHealthColor, fullHealthColor, healthPercentage);
    }
}
