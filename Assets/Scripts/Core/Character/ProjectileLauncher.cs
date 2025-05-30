using System;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;


public class ProjectileLauncher : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private Player player;
    [SerializeField] private Transform projectileSpawnPoint;
    [SerializeField] private GameObject serverProjectilePrefab;
    [SerializeField] private GameObject clientProjectilePrefab;
    [SerializeField] private Collider2D playerCollider;

    [Header("Settings")]
    [SerializeField] private float projectileSpeed = 5f;
    [SerializeField] private bool directionLock = false;

    private bool shouldFire = false;
    private Character character;
    private bool isPointerOverUI = false;

    private int damageOnStart;

    public override void OnNetworkSpawn()
    {
        if (!IsOwner) { return; }

        character = GetComponent<Character>();
        if (character == null)
        {
            Debug.LogWarning("Character component not found on the GameObject.");
            return;
        }

        damageOnStart = serverProjectilePrefab.GetComponent<DealDamageOnContact>().DamageAmount;
    }

    private void Update()
    {
        if (!IsOwner) { return; }

        isPointerOverUI = EventSystem.current.IsPointerOverGameObject();

        if (!shouldFire) { return; }

        Vector2 direction = CalculateDirection();

        SecondaryFireServerRPC(projectileSpawnPoint.position, direction);

        shouldFire = false;
    }

    public void ApplyDamageBoost(float damageMultiplier, float duration)
    {
        if (serverProjectilePrefab.TryGetComponent<DealDamageOnContact>(out DealDamageOnContact damageComponent))
        {
            damageComponent.DamageAmount = Mathf.RoundToInt(damageOnStart * damageMultiplier);
        }

        Invoke(nameof(ResetDamage), duration);
    }

    private object ResetDamage()
    {
        if (serverProjectilePrefab.TryGetComponent<DealDamageOnContact>(out DealDamageOnContact damageComponent))
        {
            damageComponent.DamageAmount = damageOnStart;
        }
        return null;
    }

    private Vector2 CalculateDirection()
    {
        // Get cursor position in screen space and convert to world space
        Vector3 mousePosition = Mouse.current.position.ReadValue();
        Vector3 worldMousePosition = Camera.main.ScreenToWorldPoint(new Vector3(mousePosition.x, mousePosition.y, Camera.main.nearClipPlane));

        if (directionLock)
        {
            // Check if cursor is behind the facing direction
            bool isFacingLeft = character.IsFacingLeft;
            bool cursorIsBehind = (isFacingLeft && worldMousePosition.x < transform.position.x) || 
                                (!isFacingLeft && worldMousePosition.x > transform.position.x);

            // If cursor is behind, shoot straight in the facing direction
            if (cursorIsBehind)
            {
                return isFacingLeft ? Vector2.right : Vector2.left;
            }
            else
            {
                // If cursor is not behind, shoot toward the cursor
                return (worldMousePosition - projectileSpawnPoint.position).normalized;
            }
        }
        else
        {
            // If directionLock is false, always shoot toward the cursor
            return (worldMousePosition - projectileSpawnPoint.position).normalized;
        }
    }

    public void HandleSecondaryAttack(bool shouldFire)
    {
        if (isPointerOverUI) { return; }

        this.shouldFire = shouldFire;
    }

    [ServerRpc]
    private void SecondaryFireServerRPC(Vector2 spawnPos, Vector2 direction)
    {
        GameObject projectileInstance = Instantiate(
            serverProjectilePrefab, 
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

        SecondaryFireClientRPC(spawnPos, direction, player.TeamIndex.Value);
    }

    [ClientRpc]
    private void SecondaryFireClientRPC(Vector2 spawnPos, Vector2 direction, int teamIndex)
    {
        SpawnDummyProjectile(spawnPos, direction, teamIndex);
    }

    private void SpawnDummyProjectile(Vector2 spawnPos, Vector2 direction, int teamIndex)
    {
        GameObject projectileInstance = Instantiate(
            clientProjectilePrefab, 
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

