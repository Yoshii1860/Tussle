using System;
using Unity.Netcode;
using UnityEngine;
using Unity.Cinemachine;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using System.Linq;

public abstract class Character : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] protected InputReader inputReader;
    [SerializeField] protected Rigidbody2D rb;
    [SerializeField] protected Animator animator;
    [SerializeField] protected CinemachineCamera cmCamera;
    protected GameHUD gameHUD;
    protected const string PlayerLayerMask = "Walk";

    [Header("Movement Settings")]
    [SerializeField] protected float moveSpeed = 5f;
    [SerializeField] protected float sprintSpeed = 10f;

    [Header("Character Settings")]
    [SerializeField] protected CharacterType characterType;
    [SerializeField] protected Attack[] attacks;
    [SerializeField] protected Attack secondaryAttack;
    protected Attack currentAttack;
    public Attack CurrentAttack { get; protected set; }

    [Header("Zoom Settings")]
    [SerializeField] protected float zoomSpeed = 0.5f;
    [SerializeField] protected float minFOV = 5f;
    [SerializeField] protected float maxFOV = 10f;

    [Header("Cooldown Settings")]
    private Dictionary<int, float> attackCooldowns = new Dictionary<int, float>();
    private const float GlobalCooldown = 0.3f;
    private float lastAttackTime = -Mathf.Infinity;

    protected NetworkVariable<bool> isMoving = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    protected NetworkVariable<bool> isFacingLeft = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    protected NetworkVariable<bool> isAttacking = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    protected NetworkVariable<bool> isSecondaryAction = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    protected NetworkVariable<int> currentAttackIndex = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

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

            // Apply initial states
            OnIsMovingChanged(false, isMoving.Value);
            OnFacingLeftChanged(false, isFacingLeft.Value);
            OnIsAttackingChanged(false, isAttacking.Value);
            OnIsSecondaryActionChanged(false, isSecondaryAction.Value);
            return;
        }

        if (inputReader == null)
        {
            Debug.LogError("InputReader is not assigned!");
            return;
        }
        inputReader.MoveEvent += HandleMove;
        inputReader.ZoomEvent += HandleZoom;
        inputReader.ChangeAttackEvent += OnAttackChange;

        isMoving.OnValueChanged += OnIsMovingChanged;
        isFacingLeft.OnValueChanged += OnFacingLeftChanged;
        isAttacking.OnValueChanged += OnIsAttackingChanged;
        isSecondaryAction.OnValueChanged += OnIsSecondaryActionChanged;

        startMoveSpeed = moveSpeed;

        for (int i = 0; i < attacks.Length; i++)
        {
            attackCooldowns[i] = 0f;
        }
        if (secondaryAttack != null)
        {
            attackCooldowns[-1] = 0f;
        }

        gameHUD = FindFirstObjectByType<GameHUD>();
        gameHUD.SetIcons(attacks, secondaryAttack);
        gameHUD.SetLocalCharacter(gameObject);

        OnAttackChange(0);
    }

    public override void OnNetworkDespawn()
    {
        if (IsOwner)
        {
            inputReader.MoveEvent -= HandleMove;
            inputReader.ZoomEvent -= HandleZoom;
            inputReader.ChangeAttackEvent -= OnAttackChange;
        }

        isMoving.OnValueChanged -= OnIsMovingChanged;
        isFacingLeft.OnValueChanged -= OnFacingLeftChanged;
        isAttacking.OnValueChanged -= OnIsAttackingChanged;
        isSecondaryAction.OnValueChanged -= OnIsSecondaryActionChanged;
    }

    private void OnAttackChange(int index)
    {
        if (!IsOwner) return;
        currentAttackIndex.Value = index;

        if (attackCooldowns[index] > 0f)
        {
            Debug.LogWarning($"Attack {attacks[index].attackName} is still on cooldown: {attackCooldowns[index]} seconds remaining.");
        }
    }

    protected virtual bool CanPerformAttack()
    {
        if (Time.time - lastAttackTime < GlobalCooldown) return false;
        return attackCooldowns[currentAttackIndex.Value] <= 0f;
    }

    private void Update()
    {
        if (!IsOwner) return;

        UpdateMovement();
        UpdateAnimations();
        UpdateScale();
        UpdateCooldowns();
    }

    private void UpdateCooldowns()
    {
        foreach (var cd in attackCooldowns.ToList())
        {
            if (cd.Value > 0f)
            {
                attackCooldowns[cd.Key] = Mathf.Max(0f, cd.Value - Time.deltaTime);

                float maxCooldown = cd.Key == -1 ? secondaryAttack.cooldown : attacks[cd.Key].cooldown;
                gameHUD.UpdateCooldown(cd.Key, attackCooldowns[cd.Key] / maxCooldown);
            }
        }

        if (Time.time - lastAttackTime > GlobalCooldown)
        {
            isAttacking.Value = false;
        }
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
        if (newValue && !previousValue)
        {
            if (currentAttack.isTriggerBool)
            {
                animator.SetBool(currentAttack.animationTrigger, newValue);
            }
            else
            {
                animator.SetTrigger(currentAttack.animationTrigger);
            }

            attackCooldowns[currentAttackIndex.Value] = currentAttack.cooldown;
            Debug.Log($"OnIsAttackingChanged: Attack={currentAttack.attackName} with cooldown={currentAttack.cooldown}");
            lastAttackTime = Time.time;
        }
    }

    protected virtual void OnIsSecondaryActionChanged(bool previousValue, bool newValue)
    {
        if (newValue)
        {
            if (secondaryAttack.isTriggerBool)
            {
                animator.SetBool(secondaryAttack.animationTrigger, newValue);
            }
            else
            {
                animator.SetTrigger(secondaryAttack.animationTrigger);
                attackCooldowns[-1] = secondaryAttack.cooldown;
            }
        }
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