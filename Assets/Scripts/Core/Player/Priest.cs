using UnityEngine;

public class Priest : Character
{
    protected override void PerformPrimaryAction()
    {
        Debug.Log("Priest: Attack with Stick");
    }

    protected override void PerformSecondaryAction()
    {
        Debug.Log("Priest: Cast Spell");
        Vector3 spawnPosition = transform.position + (isFacingLeft.Value ? Vector3.right : Vector3.left) * 1f;
        Quaternion rotation = Quaternion.identity;
        SpawnProjectileServerRpc(spawnPosition, rotation);
    }
}