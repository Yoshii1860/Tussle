using System.Collections;
using UnityEngine.UI;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

// On the Player GameObject
public class PlayerInteractor : MonoBehaviour
{
    [SerializeField] private InputReader inputReader;
    private ItemDropper chestInRange;
    private MerchantNPC merchantInRange;
    private Coroutine chestOpenCoroutine;
    private bool isOpeningChest = false;
    private Character character;
    private Health health;
    private bool wasAttacked = false;

    private void Awake()
    {
        character = GetComponentInParent<Character>();
        if (character == null)
        {
            Debug.LogError("PlayerInteractor: Character component not found in parent.");
        }
        else
        {
            health = character.GetComponent<Health>();
            health.OnDamaged += OnDamaged;
        }
    }

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

    private void OnDestroy()
    {
        health.OnDamaged -= OnDamaged;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.TryGetComponent<ItemDropper>(out ItemDropper dropper))
        {
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
        else if (other.TryGetComponent<MerchantNPC>(out MerchantNPC merchant))
        {
            if (merchantInRange == null)
            {
                Debug.Log($"Exited merchant: {merchant.name}");
                merchantInRange = merchant;
            }
        }
    }
    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.TryGetComponent<ItemDropper>(out ItemDropper dropper) && chestInRange == dropper)
        {
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
        else if (other.TryGetComponent<MerchantNPC>(out MerchantNPC merchant))
        {
            if (merchantInRange != null)
            {
                merchantInRange = null;
            }
        }
    }

    private void OnInteract()
    {
        Debug.Log("Interact action triggered.");
        if (chestInRange != null && !chestInRange.IsLocked() && !isOpeningChest)
        {
            chestOpenCoroutine = StartCoroutine(OpenChestRoutine(chestInRange));
            Debug.Log($"Started opening chest: {chestInRange.name}");
        }
        else if (merchantInRange != null)
        {
            ulong clientId = NetworkManager.Singleton.LocalClientId;
            merchantInRange.InteractWithMerchant(clientId);
            Debug.Log($"Interacted with merchant: {merchantInRange.name}");
        }
    }

    private IEnumerator OpenChestRoutine(ItemDropper chest)
    {
        isOpeningChest = true;
        wasAttacked = false;
        float timer = 0f;
        float chestOpenDuration = chest.ChestOpenDuration;
        Image progressBar = chest.GetComponentInChildren<Image>();
        ToggleOpeningAnimClientRpc();

        while (timer < chestOpenDuration)
        {
            if (character.IsMoving || character.IsAttacking || WasAttacked())
            {
                Debug.Log("Chest opening interrupted!");
                isOpeningChest = false;
                progressBar.fillAmount = 0f;
                ToggleOpeningAnimClientRpc();
                yield break;
            }

            progressBar.fillAmount = timer / chestOpenDuration;

            timer += Time.deltaTime;
            yield return null;
        }

        chest.OpenChest();
        Debug.Log($"Chest opened: {chest.name}");
        isOpeningChest = false;
        progressBar.fillAmount = 0f;
        ToggleOpeningAnimClientRpc();
    }

    [ClientRpc]
    private void ToggleOpeningAnimClientRpc()
    {
        if (character != null)
        {
            Animator anim = character.GetComponent<Animator>();
            if (anim != null)
            {
                anim.SetBool("Open", isOpeningChest);
            }
        }
    }

    private void OnDamaged(Player attacker)
    {
        wasAttacked = true;
    }

    private bool WasAttacked()
    {
        if (wasAttacked)
        {
            wasAttacked = false;
            return true;
        }
        return false;
    }
}