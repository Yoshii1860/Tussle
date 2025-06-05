using System;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;


public class ProjectileLauncher : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private Player player;
    [SerializeField] public Transform ProjectileSpawnPoint;
    [SerializeField] private Collider2D playerCollider;
    [SerializeField] private ProjectileRegistry projectileRegistry;

    [Header("Settings")]
    [SerializeField] private float projectileSpeed = 5f;
    [SerializeField] private bool directionLock = false;

    private bool shouldFire = false;
    private Character character;
    private bool isPointerOverUI = false;

    private float damageMultiplier = 1f;

    public Vector2 ArrowDirection { get; private set; }

    public override void OnNetworkSpawn()
    {
        if (!IsOwner) { return; }

        character = GetComponent<Character>();
        if (character == null)
        {
            Debug.LogWarning("Character component not found on the GameObject.");
            return;
        }
    }

    private void Update()
    {
        if (!IsOwner) { return; }

        isPointerOverUI = EventSystem.current.IsPointerOverGameObject();
    }

    public GameObject GetPrefab(string projectileKey, bool isServer)
    {
        return projectileRegistry.GetPrefab(projectileKey, isServer);
    }

    public void ApplyDamageBoost(float damageMultiplier, float duration)
    {
        this.damageMultiplier = damageMultiplier;
        Invoke(nameof(ResetDamage), duration);
    }

    private object ResetDamage()
    {
        damageMultiplier = 1f;
        return null;
    }

    private Vector2 CalculateDirection()
    {
        // Get cursor position in screen space and convert to world space
        Vector3 mousePosition = Mouse.current.position.ReadValue();
        Vector3 worldMousePosition = Camera.main.ScreenToWorldPoint(new Vector3(mousePosition.x, mousePosition.y, Camera.main.nearClipPlane));

        if (directionLock)
        {
            Debug.Log($"ProjectileLauncher: CalculateDirection");
            if (character == null) Debug.LogWarning("Character component is null in CalculateDirection.");
            // Check if cursor is behind the facing direction
            bool isFacingLeft = character.IsFacingLeft;
            bool cursorIsBehind = (isFacingLeft && worldMousePosition.x < transform.position.x) || 
                                (!isFacingLeft && worldMousePosition.x > transform.position.x);

            // If cursor is behind, shoot straight in the facing direction
            if (cursorIsBehind)
            {
                Debug.Log($"ProjectileLauncher: Cursor is behind, shooting in facing direction: {isFacingLeft}");
                return isFacingLeft ? Vector2.right : Vector2.left;
            }
            else
            {
                Debug.Log($"ProjectileLauncher: Cursor is in front, shooting towards cursor");
                return (worldMousePosition - ProjectileSpawnPoint.position).normalized;
            }
        }
        else
        {
            Debug.Log($"ProjectileLauncher: CalculateDirection without direction lock");
            return (worldMousePosition - ProjectileSpawnPoint.position).normalized;
        }
    }

    public void HandleShot(Attack attack)
    {
        if (isPointerOverUI) { return; }
        if (!IsOwner) { return; }

        if (attack.projectileBehavior != null)
        {
            ArrowDirection = CalculateDirection();
            attack.projectileBehavior.Launch(this, attack);
        }
    }

    public void FireProjectileServer(Vector2 spawnPos, Vector2 direction, string projectileKey)
    {
        ShootServerRPC(spawnPos, direction, projectileKey);
    }

    [ServerRpc]
    private void ShootServerRPC(Vector2 spawnPos, Vector2 direction, string projectileKey)
    {
        GameObject serverProjectile = projectileRegistry.GetPrefab(projectileKey, true);
        int dmg = serverProjectile.GetComponent<DealDamageOnContact>().DamageAmount;
        serverProjectile.GetComponent<DealDamageOnContact>().DamageAmount = Mathf.RoundToInt(dmg * damageMultiplier);
        GameObject projectileInstance = Instantiate(
            serverProjectile, 
            spawnPos, 
            Quaternion.identity);

        projectileInstance.transform.up = direction;
        Physics2D.IgnoreCollision(playerCollider, projectileInstance.GetComponent<Collider2D>());

        if (projectileInstance.TryGetComponent<TeamIndexStorage>(out TeamIndexStorage teamIndexStorage))
        {
            teamIndexStorage.Initialize(player.TeamIndex.Value);
        }

        if (projectileInstance.TryGetComponent<DealDamageOnContact>(out DealDamageOnContact damageComponent))
        {
            damageComponent.SetOwner(OwnerClientId);
        }

        if(projectileInstance.TryGetComponent<Rigidbody2D>(out Rigidbody2D rb))
        {
            rb.linearVelocity = rb.transform.up * projectileSpeed;
        }

        ShootClientRPC(spawnPos, direction, player.TeamIndex.Value, projectileKey);
    }

    [ClientRpc]
    private void ShootClientRPC(Vector2 spawnPos, Vector2 direction, int teamIndex, string projectileKey)
    {
        SpawnDummyProjectile(spawnPos, direction, teamIndex, projectileKey);
    }

    private void SpawnDummyProjectile(Vector2 spawnPos, Vector2 direction, int teamIndex, string projectileKey)
    {
        GameObject clientProjectile = projectileRegistry.GetPrefab(projectileKey, false);
        GameObject projectileInstance = Instantiate(
            clientProjectile, 
            spawnPos, 
            Quaternion.identity);

        projectileInstance.transform.up = direction;
        Physics2D.IgnoreCollision(playerCollider, projectileInstance.GetComponent<Collider2D>());

        if (projectileInstance.TryGetComponent<TeamIndexStorage>(out TeamIndexStorage teamIndexStorage))
        {
            teamIndexStorage.Initialize(teamIndex);
        }

        if (projectileInstance.TryGetComponent<Rigidbody2D>(out Rigidbody2D rb))
        {
            rb.linearVelocity = rb.transform.up * projectileSpeed;
        }
    }
}

