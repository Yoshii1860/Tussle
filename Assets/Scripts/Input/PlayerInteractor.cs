using System.Collections;
using UnityEngine.UI;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

// On the Player GameObject
public class PlayerInteractor : NetworkBehaviour
{
    private NetworkVariable<bool> isOpeningChest = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    private NetworkVariable<bool> wasAttacked = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Owner, NetworkVariableWritePermission.Server);
    [SerializeField] private InputReader inputReader;
    private ItemDropper chestInRange;
    private MerchantNPC merchantInRange;
    private Coroutine chestOpenCoroutine;
    [SerializeField] private Character character;
    private Health health;
    private Animator animator;

    public override void OnNetworkSpawn()
    {
        animator = character.GetComponent<Animator>();
        health = character.GetComponent<Health>();
        isOpeningChest.OnValueChanged += OnIsOpeningChestChanged;
        OnIsOpeningChestChanged(false, isOpeningChest.Value);
        if (IsOwner)
        {
            inputReader.InteractEvent += OnInteract;
        }
        else if (IsServer)
        {
            health.OnDamaged += OnDamaged; 
        }
    }

    public override void OnNetworkDespawn()
    {
        if (IsOwner)
        {
            inputReader.InteractEvent -= OnInteract;
        }
        else if (IsServer)
        {
            health.OnDamaged -= OnDamaged;
        }
        isOpeningChest.OnValueChanged -= OnIsOpeningChestChanged;
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
        if (chestInRange != null && !chestInRange.IsLocked() && !isOpeningChest.Value)
        {
            chestOpenCoroutine = StartCoroutine(OpenChestRoutine(chestInRange));
            Debug.Log($"Started opening chest: {chestInRange.name}");
        }
        else if (merchantInRange != null)
        {
            merchantInRange.RequestMerchantInteractionServerRpc(NetworkManager.Singleton.LocalClientId);
            Debug.Log($"Interacted with merchant: {merchantInRange.name}");
        }
    }

    private IEnumerator OpenChestRoutine(ItemDropper chest)
    {
        ResetWasAttackedServerRpc();
        float timer = 0f;
        float chestOpenDuration = chest.ChestOpenDuration;
        Image progressBar = chest.GetComponentInChildren<Image>();
        isOpeningChest.Value = true;

        while (timer < chestOpenDuration)
        {
            if (character.IsMoving || character.IsAttacking || wasAttacked.Value)
            {
                Debug.Log("Chest opening interrupted!");
                progressBar.fillAmount = 0f;
                isOpeningChest.Value = false;
                ResetWasAttackedServerRpc();
                yield break;
            }

            progressBar.fillAmount = timer / chestOpenDuration;

            timer += Time.deltaTime;
            yield return null;
        }

        chest.OpenChest();
        Debug.Log($"Chest opened: {chest.name}");
        progressBar.fillAmount = 0f;
        isOpeningChest.Value = false;
    }

    private void OnIsOpeningChestChanged(bool previousValue, bool newValue)
    {
        animator.SetBool("Open", newValue);
    }

    private void OnDamaged(Player attacker)
    {
        wasAttacked.Value = true;
    }

    [ServerRpc(RequireOwnership = false)]
    private void ResetWasAttackedServerRpc()
    {
        wasAttacked.Value = false;
    }
}