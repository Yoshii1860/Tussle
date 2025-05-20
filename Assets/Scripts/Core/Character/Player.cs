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
    [SerializeField] private SpriteRenderer minimapIconRenderer;
    [SerializeField] private GameObject mapsPrefab;
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
        
#if UNITY_SERVER
        if (IsServer)
        {
            UserData userData = null;
            if (IsHost)
            {
                userData = HostSingleton.Instance.GameManager.NetworkServer.TryGetUserData(OwnerClientId);
                PlayerName.Value = userData.userName;
                OnPlayerSpawned?.Invoke(this);
            }
            else
            {
                userData = ServerSingleton.Instance.GameManager.NetworkServer.TryGetUserData(OwnerClientId);
                PlayerName.Value = userData.userName;
                OnPlayerSpawned?.Invoke(this);
            }
        }
#endif

        if (IsOwner)
        {
            if (cmCamera == null) Debug.LogError("Player: cmCamera is null");
            if (blackScreen == null) Debug.LogError("Player: blackScreen is null");
            if (minimapIconRenderer == null) Debug.LogError("Player: minimapIconRenderer is null");
            if (mapsPrefab == null) Debug.LogError("Player: mapsPrefab is null");
            if (cursorTexture == null) Debug.LogError("Player: cursorTexture is null");

            cmCamera.Priority = ownerPriority;
            Debug.Log($"Player: OnNetworkSpawn - OwnerClientId: {OwnerClientId}, NetworkObjectId: {NetworkObjectId}");

            cmCamera.PreviousStateIsValid = false;

            blackScreen.SetActive(true);
            StartCoroutine(FadeOutBlackscreen());

            minimapIconRenderer.color = playerIconColor;

            GameObject hud = GameObject.FindWithTag("GameHUD");
            if (hud != null)
            {
                Instantiate(mapsPrefab, hud.transform);
            }
            else
            {
                Debug.LogError("Player: GameHUD not found in scene");
            }

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
            SetInvisibilityClientRpc(duration);
        }
    }

    [ClientRpc]
    private void SetInvisibilityClientRpc(float duration)
    {
        if (IsOwner)
        {
            foreach (SpriteRenderer sr in GetComponentsInChildren<SpriteRenderer>())
            {
                sr.enabled = false;
            }
            Invoke(nameof(ResetInvisibility), duration);
        }
        else
        {
            foreach (SpriteRenderer sr in GetComponentsInChildren<SpriteRenderer>())
            {
                sr.enabled = false;
            }
            if (playerUICanvas != null)
            {
                playerUICanvas.SetActive(false);
            }
        }

        if (IsServer)
        {
            Invoke(nameof(ResetInvisibilityClientRpc), duration);
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

    private void ResetInvisibility()
    {
        foreach (SpriteRenderer sr in GetComponentsInChildren<SpriteRenderer>())
        {
            sr.enabled = true;
        }
    }

    private IEnumerator FadeOutBlackscreen()
    {
        yield return new WaitForSeconds(0.5f);

        Image blackScreenImage = blackScreen.GetComponentInChildren<Image>();
        if (blackScreenImage != null)
        {
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
        else
        {
            Debug.LogError("Player: blackScreenImage is null in FadeOutBlackscreen");
            blackScreen.SetActive(false);
        }
    }

    public override void OnNetworkDespawn()
    {
        if (IsServer)
        {
            OnPlayerDespawned?.Invoke(this);
        }
    }
}