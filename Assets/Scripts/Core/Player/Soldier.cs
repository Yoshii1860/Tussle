using UnityEngine;

public class Soldier : Character
{
    protected override void PerformPrimaryAction()
    {
        Debug.Log("Soldier: Sword Attack");
    }

    protected override void PerformSecondaryAction()
    {
        Debug.Log("Soldier: No Secondary Action");
    }
}