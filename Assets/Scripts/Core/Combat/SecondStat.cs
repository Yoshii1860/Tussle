using Unity.Netcode;
using UnityEngine;
using System;
using System.Collections;

public class SecondStat : NetworkBehaviour
{
    [SerializeField] private int secondStatRegAmount = 5;
    [SerializeField] private float secondStatRegInterval = 1f;
    private Coroutine secondStatRegCoroutine;

    [field: SerializeField] public int MaxSecondStat { get; private set; } = 100;

    public NetworkVariable<int> CurrentSecondStat = new NetworkVariable<int>();

    public Action<SecondStat> OnOutOfSecondStat;

    public override void OnNetworkSpawn()
    {
        if (!IsServer) { return; }

        CurrentSecondStat.Value = MaxSecondStat;

        ApplyRegeneration(secondStatRegAmount);
    }

    public bool TryCast(int secondStatCost)
    {
        if (CurrentSecondStat.Value < secondStatCost) { return false; }

        Debug.Log($"SecondStat: Casting spell with cost: {secondStatCost}");
        ModifySecondStatServerRpc(-secondStatCost);
        return true;
    }

    public void Restore(int secondStatAmount)
    {
        ModifySecondStatServerRpc(secondStatAmount);
    }

    private void ApplyRegeneration(int regenerationAmount)
    {
        secondStatRegCoroutine = StartCoroutine(RegenerateSecondStat(regenerationAmount));
    }

    private IEnumerator RegenerateSecondStat(int regenerationAmount)
    {
        while (true)
        {
            ModifySecondStatServerRpc(regenerationAmount);
            yield return new WaitForSeconds(secondStatRegInterval);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void ModifySecondStatServerRpc(int value)
    {
        if (!IsServer) { return; }

        Debug.Log($"SecondStat: Modifying second stat by {value}. Current: {CurrentSecondStat.Value}, Max: {MaxSecondStat}");
        CurrentSecondStat.Value = Mathf.Clamp(CurrentSecondStat.Value + value, 0, MaxSecondStat);

        if (CurrentSecondStat.Value == 0)
        {
            OnOutOfSecondStat?.Invoke(this);
        }
    }

    public override void OnNetworkDespawn()
    {
        if (secondStatRegCoroutine != null)
        {
            StopCoroutine(secondStatRegCoroutine);
            secondStatRegCoroutine = null;
        }
    }
}
