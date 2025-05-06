using UnityEngine;
using Unity.Netcode;

public abstract class Coin : NetworkBehaviour
{
    [SerializeField] private SpriteRenderer spriteRenderer;

    protected int coinValue = 10;
    protected bool isCollected;

    public abstract int Collect();

    public void SetCoinValue(int value)
    {
        coinValue = value;
    }

    protected void Show(bool show)
    {
        if (spriteRenderer == null) return;
        spriteRenderer.enabled = show;
    }
}
