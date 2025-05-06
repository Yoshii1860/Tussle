using System;
using Unity.Netcode;
using UnityEngine;

public abstract class Character : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] protected InputReader inputReader;
    [SerializeField] protected Rigidbody2D rb;
    [SerializeField] protected Animator animator;

    [Header("Movement Settings")]
    [SerializeField] protected float moveSpeed = 5f;
    [SerializeField] protected float sprintSpeed = 10f;

    [Header("Character Settings")]
    [SerializeField] protected CharacterType characterType;

    protected NetworkVariable<bool> isMoving = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    protected NetworkVariable<bool> isFacingLeft = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    protected NetworkVariable<bool> isAttacking = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    protected NetworkVariable<bool> isSecondaryAction = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    protected NetworkVariable<bool> isSecondaryTrigger = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    protected NetworkVariable<bool> isDead = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    protected Vector2 previousMovementInput;

    public bool IsFacingLeft => isFacingLeft.Value;

    public override void OnNetworkSpawn()
    {
        isDead.OnValueChanged += OnIsDeadChanged;
        
        Debug.Log($"OnNetworkSpawn: CharacterType={characterType}, IsOwner={IsOwner}, OwnerClientId={OwnerClientId}, NetworkObjectId={NetworkObjectId}");
        if (!IsOwner)
        {
            isMoving.OnValueChanged += OnIsMovingChanged;
            isFacingLeft.OnValueChanged += OnFacingLeftChanged;
            isAttacking.OnValueChanged += OnIsAttackingChanged;
            isSecondaryAction.OnValueChanged += OnIsSecondaryActionChanged;
            isSecondaryTrigger.OnValueChanged += OnIsSecondaryTriggerChanged;
            if (TryGetComponent<Health>(out Health health))
            {
                health.OnDie += HandleDeath;
            }
            else 
            {
                Debug.LogWarning("Health component not found on Character!");
            }
            // Apply initial states
            OnIsMovingChanged(false, isMoving.Value);
            OnFacingLeftChanged(false, isFacingLeft.Value);
            OnIsAttackingChanged(false, isAttacking.Value);
            OnIsSecondaryActionChanged(false, isSecondaryAction.Value);
            OnIsSecondaryTriggerChanged(false, isSecondaryTrigger.Value);
            OnIsDeadChanged(false, isDead.Value);
            return;
        }

        if (inputReader == null)
        {
            Debug.LogError("InputReader is not assigned!");
            return;
        }
        inputReader.MoveEvent += HandleMove;

        isMoving.OnValueChanged += OnIsMovingChanged;
        isFacingLeft.OnValueChanged += OnFacingLeftChanged;
        isAttacking.OnValueChanged += OnIsAttackingChanged;
        isSecondaryAction.OnValueChanged += OnIsSecondaryActionChanged;
        isSecondaryTrigger.OnValueChanged += OnIsSecondaryTriggerChanged;
    }

    public override void OnNetworkDespawn()
    {
        if (IsOwner)
        {
            inputReader.MoveEvent -= HandleMove;
        }

        isDead.OnValueChanged -= OnIsDeadChanged;
        isMoving.OnValueChanged -= OnIsMovingChanged;
        isFacingLeft.OnValueChanged -= OnFacingLeftChanged;
        isAttacking.OnValueChanged -= OnIsAttackingChanged;
        isSecondaryAction.OnValueChanged -= OnIsSecondaryActionChanged;
        isSecondaryTrigger.OnValueChanged -= OnIsSecondaryTriggerChanged;
        if (TryGetComponent<Health>(out Health health))
        {
            health.OnDie -= HandleDeath;
        }
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

    protected virtual void UpdateAnimations()
    {
        if (!IsOwner) return;
        animator.SetBool("Walk", isMoving.Value);
    }

    protected virtual void OnIsMovingChanged(bool previousValue, bool newValue)
    {
        animator.SetBool("Walk", newValue);
    }

    protected virtual void OnIsAttackingChanged(bool previousValue, bool newValue)
    {
        if (newValue)
        {
            animator.SetTrigger("Attack");
        }
    }

    protected virtual void OnIsSecondaryActionChanged(bool previousValue, bool newValue)
    {
        if (newValue)
        {
            animator.SetTrigger("SecondaryAction");
        }
    }

    protected virtual void OnIsSecondaryTriggerChanged(bool previousValue, bool newValue)
    {

    }

    protected virtual void OnIsDeadChanged(bool previousValue, bool newValue)
    {
        if (newValue)
        {
            animator.SetTrigger("Die");
        }
    }

    protected virtual void HandleDeath(Health health)
    {
        if (IsServer)
        {
            isDead.Value = true;
        }
    }
}