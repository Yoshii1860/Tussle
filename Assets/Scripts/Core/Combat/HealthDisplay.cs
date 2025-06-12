using UnityEngine;
using Unity.Netcode;
using UnityEngine.UI;

public class HealthDisplay : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private Health health;
    [SerializeField] private Image healthBarImage;
    //[SerializeField] private Color fullHealthColor = Color.green;
    //[SerializeField] private Color lowHealthColor = Color.red;

    public override void OnNetworkSpawn()
    {
        if (!IsClient) { return; }

        Player player = GetComponent<Player>();
        if (player != null && player.IsOwner)
        {
            healthBarImage.gameObject.SetActive(false);
            return;
        }

        if (health != null)
            {
                health.CurrentHealth.OnValueChanged += OnHealthChanged;
                OnHealthChanged(0, health.CurrentHealth.Value);
            }
    }

    public void InitializeGameHUDHealthBar(GameObject character)
    {
        health = character.GetComponent<Health>();
        
        if (IsClient && health != null)
        {
            health.CurrentHealth.OnValueChanged += OnHealthChanged;
            OnHealthChanged(0, health.CurrentHealth.Value);
        }
        else
        {
            Debug.LogWarning("Health component not found on LocalPlayerObject. HealthDisplay will not function correctly.");
        }
    }

    public override void OnNetworkDespawn()
    {
        if (!IsClient) { return; }

        health.CurrentHealth.OnValueChanged -= OnHealthChanged;
    }

    private void OnHealthChanged(int oldHealth, int newHealth)
    {
        float healthPercentage = (float)newHealth / health.MaxHealth;
        healthBarImage.fillAmount = healthPercentage;
        //healthBarImage.color = Color.Lerp(lowHealthColor, fullHealthColor, healthPercentage);
    }
}
