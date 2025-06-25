using UnityEngine;
using UnityEngine.InputSystem;

// On the Player GameObject
public class PlayerChestInteractor : MonoBehaviour
{
    [SerializeField] private InputReader inputReader;
    private ItemDropper chestInRange;

    private void OnEnable()
    {
        if (inputReader != null)
            inputReader.InteractEvent += OnInteract;
    }
    private void OnDisable()
    {
        if (inputReader != null)
            inputReader.InteractEvent -= OnInteract;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.TryGetComponent<ItemDropper>(out ItemDropper dropper))
            if (!dropper.IsChest()) // Ensure it's a chest
            {
                Debug.Log($"ItemDropper {dropper.name} is not a chest, ignoring.");
                return;
            }
            else
            {
                chestInRange = dropper;
                Debug.Log($"Chest in range: {dropper.name}");
            }
    }
    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.TryGetComponent<ItemDropper>(out ItemDropper dropper) && chestInRange == dropper)
            if (!dropper.IsChest()) // Ensure it's a chest
            {
                Debug.Log($"ItemDropper {dropper.name} is not a chest, ignoring.");
                return;
            }
            else
            {
                chestInRange = null;
                Debug.Log($"Exited chest: {dropper.name}");
            }
    }

    private void OnInteract()
    {
        Debug.Log("Interact action triggered.");
        if (chestInRange != null && !chestInRange.IsLocked())
            chestInRange.OpenChest();
            Debug.Log($"Interacted with chest: {chestInRange.name}");
    }
}