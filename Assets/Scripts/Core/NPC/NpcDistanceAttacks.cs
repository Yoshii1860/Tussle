using UnityEngine;
using Unity.Netcode;

public class NpcDistanceAttacks : NetworkBehaviour
{
    [SerializeField] private string projectileKey = "Arrow";
    [SerializeField] private ProjectileRegistry projectileRegistry;
    [SerializeField] private float projectileSpeed = 5f;
    [SerializeField] private int damage = 5;
    [SerializeField] private Transform arrowSpawnPoint;

    public void DistanceAttack(Player target)
    {
        if (target == null)
        {
            Debug.LogError("Target player is null. Cannot perform distance attack.");
            return;
        }
        Vector2 targetDirection = target.transform.position - arrowSpawnPoint.position;
        targetDirection.Normalize();

        ShootServerRPC(arrowSpawnPoint.position, targetDirection, projectileKey);
    }

    public void MeleeAttack(Player target)
    {
        // Implement melee attack logic here
        Debug.Log("NPC performs a melee attack.");
    }


    [ServerRpc]
    private void ShootServerRPC(Vector2 spawnPos, Vector2 direction, string projectileKey)
    {
        GameObject serverProjectile = projectileRegistry.GetPrefab(projectileKey, true);
        serverProjectile.GetComponent<DealDamageOnContact>().DamageAmount = damage;
        GameObject projectileInstance = Instantiate(
            serverProjectile,
            spawnPos,
            Quaternion.identity);

        projectileInstance.transform.up = direction;
        if (projectileInstance.TryGetComponent<TeamIndexStorage>(out TeamIndexStorage teamIndexStorage))
        {
            teamIndexStorage.Initialize(-2); // NPCs can have a specific team index if needed
        }
        Physics2D.IgnoreCollision(GetComponent<Collider2D>(), projectileInstance.GetComponent<Collider2D>());

        if (projectileInstance.TryGetComponent<DealDamageOnContact>(out DealDamageOnContact damageComponent))
        {
            damageComponent.SetOwner(GetComponent<NetworkObject>().OwnerClientId);
        }

        if (projectileInstance.TryGetComponent<Rigidbody2D>(out Rigidbody2D rb))
        {
            rb.linearVelocity = rb.transform.up * projectileSpeed;
        }

        ShootClientRPC(spawnPos, direction, -2, projectileKey);
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
        if (projectileInstance.TryGetComponent<TeamIndexStorage>(out TeamIndexStorage teamIndexStorage))
        {
            teamIndexStorage.Initialize(teamIndex); // NPCs can have a specific team index if needed
        }
        Physics2D.IgnoreCollision(GetComponent<Collider2D>(), projectileInstance.GetComponent<Collider2D>());

        if (projectileInstance.TryGetComponent<Rigidbody2D>(out Rigidbody2D rb))
        {
            rb.linearVelocity = rb.transform.up * projectileSpeed;
        }
    }
}
