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
    [SerializeField] private GameObject blackscreen;
    [SerializeField] public Animator Animator;
    [SerializeField] public GameObject playerUICanvas;
    [SerializeField] private SpriteRenderer minimapIconRenderer;
    [SerializeField] public Texture2D cursorTexture;
    [SerializeField] public Texture2D cursorHoverTexture;
    [SerializeField] public Texture2D cursorClickTexture;
    [SerializeField] public Vector2 cursorHotspot;
    
    [field: SerializeField] public Health Health { get; private set; }
    [field: SerializeField] public CoinWallet Wallet { get; private set; }

    [Header("Player Settings")]
    [SerializeField] private int ownerPriority = 15;
    [SerializeField] private float fadeDuration = 1f;
    [SerializeField] private Color playerIconColor = Color.orange;

    public NetworkVariable<FixedString32Bytes> PlayerName = new NetworkVariable<FixedString32Bytes>(new FixedString32Bytes("Player"), NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public static event Action<Player> OnPlayerSpawned;
    public static event Action<Player> OnPlayerDespawned;

    public override void OnNetworkSpawn()
    {

    if (IsServer)
        {
            Debug.Log($"Player: OnNetworkSpawn [IsServer] - true - [IsHost] - {IsHost}");
            UserData userData = null;
            if (IsHost)
            {
                userData = HostSingleton.Instance.GameManager.NetworkServer.TryGetUserData(OwnerClientId);
                PlayerName.Value = userData.userName;
                OnPlayerSpawned?.Invoke(this);
            }
#if UNITY_SERVER
            else
            {
                userData = ServerSingleton.Instance.GameManager.NetworkServer.TryGetUserData(OwnerClientId);
                PlayerName.Value = userData.userName;
                OnPlayerSpawned?.Invoke(this);
            }
            return;
#endif
        }

        if (IsOwner)
        {
            if (cmCamera == null) Debug.LogError("Player: cmCamera is null");
            if (blackscreen == null) Debug.LogError("Player: blackscreen is null");
            if (minimapIconRenderer == null) Debug.LogError("Player: minimapIconRenderer is null");
            if (cursorTexture == null) Debug.LogError("Player: cursorTexture is null");

            cmCamera.Priority = ownerPriority;
            Debug.Log($"Player: OnNetworkSpawn - OwnerClientId: {OwnerClientId}, NetworkObjectId: {NetworkObjectId}");

            cmCamera.PreviousStateIsValid = false;

            blackscreen.SetActive(true);
            StartCoroutine(FadeOutblackscreen());

            minimapIconRenderer.color = playerIconColor;

            if (cursorTexture != null)
            {
                Cursor.SetCursor(cursorTexture, cursorHotspot, CursorMode.Auto);
            }
            else
            {
                Debug.LogWarning("Player: cursorTexture is null, using default cursor");
            }
        }
        else
        {
            cmCamera.Priority = 10;
            minimapIconRenderer.color = new Color(playerIconColor.r, playerIconColor.g, playerIconColor.b, 0f);
        }

        if (cmCamera != null)
        {
            cmCamera.Follow = transform;
            cmCamera.LookAt = transform;
        }
        else
        {
            Debug.LogError("Player: cmCamera is null, cannot set Follow or LookAt");
        }
    }

    public void Invisibility(float duration)
    {
        if (IsServer)
        {
            SetInvisibilityClientRpc();
            StartCoroutine(ResetInvisibilityAfterDelay(duration));
        }
    }

    private IEnumerator ResetInvisibilityAfterDelay(float duration)
    {
        yield return new WaitForSeconds(duration);
        ResetInvisibilityClientRpc();
    }

    [ClientRpc]
    private void SetInvisibilityClientRpc()
    {
        foreach (SpriteRenderer sr in GetComponentsInChildren<SpriteRenderer>())
        {
            sr.enabled = false;
        }

        if (!IsOwner && playerUICanvas != null)
        {
            playerUICanvas.SetActive(false);
        }
    }

    [ClientRpc]
    private void ResetInvisibilityClientRpc()
    {
        foreach (SpriteRenderer sr in GetComponentsInChildren<SpriteRenderer>())
        {
            sr.enabled = true;
        }
        if (!IsOwner && playerUICanvas != null)
        {
            playerUICanvas.SetActive(true);
        }
    }


    private IEnumerator FadeOutblackscreen()
    {
        Image blackscreenImage = blackscreen.GetComponentInChildren<Image>();
        if (blackscreenImage != null)
        {
            Color color = blackscreenImage.color;
            for (float t = 0; t < fadeDuration; t += Time.deltaTime)
            {
                color.a = Mathf.Lerp(1, 0, t / fadeDuration);
                blackscreenImage.color = color;
                yield return null;
            }
            color.a = 0;
            blackscreenImage.color = color;
            blackscreen.SetActive(false);
            blackscreenImage.color = new Color(color.r, color.g, color.b, 1);
        }
        else
        {
            Debug.LogError("Player: blackscreenImage is null in FadeOutblackscreen");
            blackscreen.SetActive(false);
        }
    }

    private static GameObject FindChildWithTag(GameObject parent, string tag)
    {
        foreach (Transform child in parent.GetComponentsInChildren<Transform>(true))
        {
            if (child.CompareTag(tag))
                return child.gameObject;
        }
        return null;
    }

    public override void OnNetworkDespawn()
    {
        if (IsServer)
        {
            OnPlayerDespawned?.Invoke(this);
        }
    }
}