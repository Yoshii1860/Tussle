using UnityEngine;
using System;

public class BountyCoin : Coin
{
    public event Action<BountyCoin> OnCollected;

    public override int Collect()
    {
        if (!IsServer)
        {
            Show(false);
            return 0;
        }

        if (isCollected) return 0;

        isCollected = true;

        Destroy(gameObject);

        return coinValue;
    }
}
