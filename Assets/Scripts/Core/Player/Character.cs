using System;
using Unity.Netcode;
using UnityEngine;

public abstract class Character : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] protected InputReader inputReader;
    [SerializeField] protected Rigidbody2D rb;
    [SerializeField] protected Animator animator;
    [SerializeField] protected GameObject projectilePrefab; // For Peasant (arrow) and Priest (spell)

    [Header("Movement Settings")]
    [SerializeField] protected float moveSpeed = 5f;
    [SerializeField] protected float sprintSpeed = 10f;

    [Header("Character Settings")]
    [SerializeField] protected CharacterType characterType;

    protected NetworkVariable<bool> isMoving = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    protected NetworkVariable<bool> isFacingLeft = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    protected NetworkVariable<bool> isAttacking = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    protected NetworkVariable<bool> isSecondaryAction = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    protected Vector2 previousMovementInput;

    public override void OnNetworkSpawn()
    {
        Debug.Log($"OnNetworkSpawn: CharacterType={characterType}, IsOwner={IsOwner}, OwnerClientId={OwnerClientId}, NetworkObjectId={NetworkObjectId}");
        if (!IsOwner)
        {
            isMoving.OnValueChanged += OnIsMovingChanged;
            isFacingLeft.OnValueChanged += OnFacingLeftChanged;
            isAttacking.OnValueChanged += OnIsAttackingChanged;
            isSecondaryAction.OnValueChanged += OnIsSecondaryActionChanged;
            // Apply initial states
            OnIsMovingChanged(false, isMoving.Value);
            OnFacingLeftChanged(false, isFacingLeft.Value);
            OnIsAttackingChanged(false, isAttacking.Value);
            OnIsSecondaryActionChanged(false, isSecondaryAction.Value);
            return;
        }

        if (inputReader == null)
        {
            Debug.LogWarning("InputReader is not assigned!");
            return;
        }
        inputReader.MoveEvent += HandleMove;
        inputReader.PrimaryAttackEvent += HandlePrimaryAttack;
        inputReader.SecondaryAttackEvent += HandleSecondaryAttack;

        isMoving.OnValueChanged += OnIsMovingChanged;
        isFacingLeft.OnValueChanged += OnFacingLeftChanged;
        isAttacking.OnValueChanged += OnIsAttackingChanged;
        isSecondaryAction.OnValueChanged += OnIsSecondaryActionChanged;
    }

    public override void OnNetworkDespawn()
    {
        if (IsOwner)
        {
            inputReader.MoveEvent -= HandleMove;
            inputReader.PrimaryAttackEvent -= HandlePrimaryAttack;
            inputReader.SecondaryAttackEvent -= HandleSecondaryAttack;
        }
        isMoving.OnValueChanged -= OnIsMovingChanged;
        isFacingLeft.OnValueChanged -= OnFacingLeftChanged;
        isAttacking.OnValueChanged -= OnIsAttackingChanged;
        isSecondaryAction.OnValueChanged -= OnIsSecondaryActionChanged;
    }

    private void Update()
    {
        if (!IsOwner) return;

        UpdateMovement();
        UpdateAnimations();
        UpdateScale();
    }

    private void FixedUpdate()
    {
        if (!IsOwner) return;

        rb.linearVelocity = new Vector2(previousMovementInput.x * moveSpeed, previousMovementInput.y * moveSpeed);
    }

    private void UpdateMovement()
    {
        if (!IsOwner) return;

        isMoving.Value = previousMovementInput != Vector2.zero;

        if (previousMovementInput.x != 0)
        {
            isFacingLeft.Value = previousMovementInput.x > 0;
        }
    }

    private void HandleMove(Vector2 movementInput)
    {
        previousMovementInput = movementInput;
    }

    private void HandlePrimaryAttack()
    {
        if (!IsOwner) return;
        PerformPrimaryAction();
        isAttacking.Value = true;
        Invoke(nameof(ResetAttack), 0.4f);
    }

    private void HandleSecondaryAttack(bool isPressed)
    {
        if (!IsOwner) return;
        if (isPressed)
        {
            PerformSecondaryAction();
            isSecondaryAction.Value = true;
        }
        else
        {
            isSecondaryAction.Value = false;
        }
    }

    protected abstract void PerformPrimaryAction();
    protected abstract void PerformSecondaryAction();

    [ServerRpc]
    protected void SpawnProjectileServerRpc(Vector3 position, Quaternion rotation)
    {
        GameObject projectile = Instantiate(projectilePrefab, position, rotation);
        NetworkObject networkObject = projectile.GetComponent<NetworkObject>();
        networkObject.Spawn();
    }

    private void UpdateScale()
    {
        if (!IsOwner) return;

        bool facingLeft = isFacingLeft.Value;
        transform.localScale = new Vector3(facingLeft ? -1 : 1, 1, 1);
    }

    private void OnFacingLeftChanged(bool previousValue, bool newValue)
    {
        // NetworkTransform syncs scale
    }

    private void UpdateAnimations()
    {
        if (!IsOwner) return;

        animator.SetBool("Walk", isMoving.Value);
        animator.SetBool("Secondary", isSecondaryAction.Value);
    }

    private void OnIsMovingChanged(bool previousValue, bool newValue)
    {
        animator.SetBool("Walk", newValue);
    }

    private void OnIsAttackingChanged(bool previousValue, bool newValue)
    {
        if (newValue)
        {
            animator.SetTrigger("Attack");
        }
    }

    private void OnIsSecondaryActionChanged(bool previousValue, bool newValue)
    {
        // Use Bool for continuous actions (e.g., block), Trigger for one-shot actions (e.g., spell)
        animator.SetBool("Secondary", newValue);
    }

    private void ResetAttack()
    {
        if (IsOwner)
        {
            isAttacking.Value = false;
        }
    }
}