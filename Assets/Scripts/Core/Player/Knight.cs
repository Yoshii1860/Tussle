using UnityEngine;

public class Knight : Character
{
    protected override void PerformPrimaryAction()
    {
        Debug.Log("Knight: Sword Attack");
        // Play sword attack animation (handled by NetworkVariable isAttacking)
    }

    protected override void PerformSecondaryAction()
    {
        Debug.Log("Knight: Block with Shield");
        // Play shield block animation (handled by NetworkVariable isSecondaryAction)
    }
}