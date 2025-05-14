using UnityEngine;
using Unity.Netcode;
using Unity.Cinemachine;
using System.Collections;
using UnityEngine.UI;
using Unity.Collections;
using System;


public class Player : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private CinemachineCamera cmCamera;
    [SerializeField] private GameObject blackScreen;
    [SerializeField] public Animator Animator;
    [SerializeField] public GameObject playerUICanvas;
    
    [field: SerializeField] public Health Health { get; private set; }
    [field: SerializeField] public CoinWallet Wallet { get; private set; }

    [Header("Player Settings")]
    [SerializeField] private int ownerPriority = 15;
    [SerializeField] private float fadeDuration = 1f;

    public NetworkVariable<FixedString32Bytes> PlayerName = new NetworkVariable<FixedString32Bytes>(new FixedString32Bytes("Player"), NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public static event Action<Player> OnPlayerSpawned;
    public static event Action<Player> OnPlayerDespawned;

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            UserData userData = HostSingleton.Instance.GameManager.NetworkServer.TryGetUserData(OwnerClientId);

            PlayerName.Value = userData.userName;

            OnPlayerSpawned?.Invoke(this);
        }

        if (IsOwner)
        {
            cmCamera.Priority = ownerPriority;
            Debug.Log($"Player: OnNetworkSpawn - OwnerClientId: {OwnerClientId}, NetworkObjectId: {NetworkObjectId}");

            cmCamera.PreviousStateIsValid = false;

            blackScreen.SetActive(true);
            StartCoroutine(FadeOutBlackscreen());
        }
        else
        {
            cmCamera.Priority = 10;
        }

        cmCamera.Follow = transform;
        cmCamera.LookAt = transform;
    }

    public void Invisibility(float duration)
    {
        if (IsServer)
        {
            // Broadcast invisibility state to all clients
            SetInvisibilityClientRpc(duration);
        }
    }

    [ClientRpc]
    private void SetInvisibilityClientRpc(float duration)
    {
        if (IsOwner)
        {
            // For the owner, disable only the SpriteRenderers (keep UI visible)
            foreach (SpriteRenderer sr in GetComponentsInChildren<SpriteRenderer>())
            {
                sr.enabled = false;
            }

            // Schedule reset for the owner
            Invoke(nameof(ResetInvisibility), duration);
        }
        else
        {
            // For other clients, disable both the SpriteRenderers and the UI
            foreach (SpriteRenderer sr in GetComponentsInChildren<SpriteRenderer>())
            {
                sr.enabled = false;
            }

            if (playerUICanvas != null)
            {
                playerUICanvas.SetActive(false);
            }
        }

        // Schedule reset for all clients
        if (IsServer)
        {
            Invoke(nameof(ResetInvisibilityClientRpc), duration);
        }
    }

    [ClientRpc]
    private void ResetInvisibilityClientRpc()
    {
        // Re-enable SpriteRenderers for all clients
        foreach (SpriteRenderer sr in GetComponentsInChildren<SpriteRenderer>())
        {
            sr.enabled = true;
        }

        // Re-enable the UI for other clients
        if (!IsOwner && playerUICanvas != null)
        {
            playerUICanvas.SetActive(true);
        }
    }

    private void ResetInvisibility()
    {
        // Re-enable SpriteRenderers for the owner
        foreach (SpriteRenderer sr in GetComponentsInChildren<SpriteRenderer>())
        {
            sr.enabled = true;
        }
    }

    private IEnumerator FadeOutBlackscreen()
    {
        yield return new WaitForSeconds(0.5f);

        Image blackScreenImage = blackScreen.GetComponentInChildren<Image>();
        Color color = blackScreenImage.color;

        for (float t = 0; t < fadeDuration; t += Time.deltaTime)
        {
            color.a = Mathf.Lerp(1, 0, t / fadeDuration);
            blackScreenImage.color = color;
            yield return null;
        }

        color.a = 0;
        blackScreenImage.color = color;
        blackScreen.SetActive(false);
        blackScreenImage.color = new Color(color.r, color.g, color.b, 1);
    }

    public override void OnNetworkDespawn()
    {
        if (IsServer)
        {
            OnPlayerDespawned?.Invoke(this);
        }
    }
}
