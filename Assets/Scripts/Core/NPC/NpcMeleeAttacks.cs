using UnityEngine;
using Unity.Netcode;

public class NpcMeleeAttacks : NetworkBehaviour
{
    [SerializeField] private Collider2D weaponCollider;
    [SerializeField] private int damage = 5;

    public void MeleeAttack()
    {
        weaponCollider.enabled = !weaponCollider.enabled; // Toggle the collider on/off
    }
}
