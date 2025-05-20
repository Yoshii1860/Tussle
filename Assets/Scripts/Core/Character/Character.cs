using System;
using Unity.Netcode;
using UnityEngine;
using Unity.Cinemachine;
using UnityEngine.EventSystems;

public abstract class Character : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] protected InputReader inputReader;
    [SerializeField] protected Rigidbody2D rb;
    [SerializeField] protected Animator animator;
    [SerializeField] protected CinemachineCamera cmCamera;

    [Header("Movement Settings")]
    [SerializeField] protected float moveSpeed = 5f;
    [SerializeField] protected float sprintSpeed = 10f;

    [Header("Character Settings")]
    [SerializeField] protected CharacterType characterType;

    [Header("Zoom Settings")]
    [SerializeField] protected float zoomSpeed = 0.5f;
    [SerializeField] protected float minFOV = 5f;
    [SerializeField] protected float maxFOV = 10f;

    protected NetworkVariable<bool> isMoving = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    protected NetworkVariable<bool> isFacingLeft = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    protected NetworkVariable<bool> isAttacking = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    protected NetworkVariable<bool> isSecondaryAction = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    protected NetworkVariable<bool> isSecondaryTrigger = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
   
    protected Vector2 previousMovementInput;

    public bool IsFacingLeft => isFacingLeft.Value;

    private float startMoveSpeed;

    public override void OnNetworkSpawn()
    {
        Debug.Log($"OnNetworkSpawn: CharacterType={characterType}, IsOwner={IsOwner}, OwnerClientId={OwnerClientId}, NetworkObjectId={NetworkObjectId}");
        if (!IsOwner)
        {
            isMoving.OnValueChanged += OnIsMovingChanged;
            isFacingLeft.OnValueChanged += OnFacingLeftChanged;
            isAttacking.OnValueChanged += OnIsAttackingChanged;
            isSecondaryAction.OnValueChanged += OnIsSecondaryActionChanged;
            isSecondaryTrigger.OnValueChanged += OnIsSecondaryTriggerChanged;

            // Apply initial states
            OnIsMovingChanged(false, isMoving.Value);
            OnFacingLeftChanged(false, isFacingLeft.Value);
            OnIsAttackingChanged(false, isAttacking.Value);
            OnIsSecondaryActionChanged(false, isSecondaryAction.Value);
            OnIsSecondaryTriggerChanged(false, isSecondaryTrigger.Value);
            return;
        }

        if (inputReader == null)
        {
            Debug.LogError("InputReader is not assigned!");
            return;
        }
        inputReader.MoveEvent += HandleMove;
        inputReader.ZoomEvent += HandleZoom;

        isMoving.OnValueChanged += OnIsMovingChanged;
        isFacingLeft.OnValueChanged += OnFacingLeftChanged;
        isAttacking.OnValueChanged += OnIsAttackingChanged;
        isSecondaryAction.OnValueChanged += OnIsSecondaryActionChanged;
        isSecondaryTrigger.OnValueChanged += OnIsSecondaryTriggerChanged;

        startMoveSpeed = moveSpeed;
    }

    public override void OnNetworkDespawn()
    {
        if (IsOwner)
        {
            inputReader.MoveEvent -= HandleMove;
            inputReader.ZoomEvent -= HandleZoom;
        }

        isMoving.OnValueChanged -= OnIsMovingChanged;
        isFacingLeft.OnValueChanged -= OnFacingLeftChanged;
        isAttacking.OnValueChanged -= OnIsAttackingChanged;
        isSecondaryAction.OnValueChanged -= OnIsSecondaryActionChanged;
        isSecondaryTrigger.OnValueChanged -= OnIsSecondaryTriggerChanged;
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

    public void MovementBoost(float boost, float duration)
    {
        if (!IsOwner) return;

        moveSpeed *= boost;
        Invoke(nameof(ResetMovement), duration);
    }

    private object ResetMovement()
    {
        moveSpeed = startMoveSpeed;
        return null;
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

    private void HandleZoom(Vector2 vector)
    {
        float scrollInput = vector.y;
        if (scrollInput != 0)
        {
            float newFOV = cmCamera.Lens.OrthographicSize - (scrollInput * zoomSpeed);
            cmCamera.Lens.OrthographicSize = Mathf.Clamp(newFOV, minFOV, maxFOV);
        }
    }
}