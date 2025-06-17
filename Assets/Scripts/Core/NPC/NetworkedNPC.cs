using Unity.Netcode;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.AI;
using System.Linq;
using Unity.VisualScripting;

public class NetworkedNPC : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private NavMeshAgent agent;
    [SerializeField] private Animator animator;
    [SerializeField] private Health healthComponent;
    [SerializeField] private NpcDistanceAttacks npcDistanceAttacks;
    [SerializeField] private NpcMeleeAttacks npcMeleeAttacks;
    [SerializeField] private Transform patrolPointsContainer;
    private Transform[] patrolPoints;
    [Space(10)]

    [Header("NPC Settings")]
    [SerializeField, Range(1, 6)] private float moveSpeed = 2f;
    [SerializeField, Range(5, 150)] private int maxHealth = 10;
    [SerializeField, Range (3, 25)] private int attackDamage = 5;
    [Space(10)]

    [Header("NPC Ranges")]
    [SerializeField] private float attackRange = 1f;
    [SerializeField] private float fleeRange = 3f;
    [SerializeField] private float triggerRadius = 12f;
    [Space(10)]

    [Header("Other Settings")]
    [SerializeField] private float attackCooldown = 1f;
    [SerializeField] private float idleDurationMin = 2f; // Minimum idle time (2 seconds)
    [SerializeField] private float idleDurationMax = 3f; // Maximum idle time (3 seconds)
    [SerializeField] private float deathDespawnDelay = 2f; // Delay before despawning NPC after death
    public int TeamIndex = -2;
    [Space(10)]

    private int currentPatrolIndex = 0;
    private NPCState currentState = NPCState.Idle;
    private NetworkVariable<NPCState> syncState = new NetworkVariable<NPCState>(NPCState.Idle, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private NetworkVariable<Vector2> syncPosition = new NetworkVariable<Vector2>(Vector2.zero, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private NetworkVariable<bool> isMoving =  new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private NetworkVariable<int> attackId = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private float lastAttackTime = 0f;
    private CircleCollider2D triggerCollider;
    private List<Player> playersInRange = new List<Player>();
    private Player currentTarget;
    //private bool isMoving = false; // Track active movement
    private float idleTimer = 0f; // Timer for idle duration

    private bool isDead = false; // Track if NPC is dead

    public enum NPCState
    {
        Idle,
        Patrolling,
        Approaching,
        Attacking,
        Fleeing,
        Dead
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            SetUpNavMesh();
            SetUpTriggerCollider();
            SetUpPatrolPoints();

            if (animator == null)
            {
                animator = GetComponent<Animator>();
            }

            if (npcDistanceAttacks == null)
            {
                npcDistanceAttacks = GetComponent<NpcDistanceAttacks>();
            }

            if (npcMeleeAttacks == null)
            {
                npcMeleeAttacks = GetComponent<NpcMeleeAttacks>();
            }
        }

        syncState.OnValueChanged += OnStateChanged;
        syncPosition.OnValueChanged += OnPositionChanged;
        isMoving.OnValueChanged += OnIsMovingChanged;
        attackId.OnValueChanged += OnAttackIdChanged;
        healthComponent.OnDie += OnDie;
    }

    private void OnDie(Health health)
    {
        SetState(NPCState.Dead); // Optional: set state to Dead
        PlayDeathAnimationClientRpc();
        Invoke(nameof(RemoveNPC), deathDespawnDelay);
    }

    private void Update()
    {
        if (!IsServer || isDead) return;

        StateMachine();
    }

    private void StateMachine()
    {
        Vector2 targetPosition = transform.position;
        Vector2 moveDirection = Vector2.zero;

        // Update idle timer and handle state transitions
        if (currentState == NPCState.Idle)
        {
            idleTimer += Time.deltaTime;
            float idleDuration = Random.Range(idleDurationMin, idleDurationMax);
            if (idleTimer >= idleDuration && patrolPoints.Length > 0)
            {
                SetState(NPCState.Patrolling);
                idleTimer = 0f; // Reset timer
            }
            moveDirection = Vector2.zero; // No movement during idle
        }
        else
        {
            // State-specific behavior within switch
            switch (currentState)
            {
                case NPCState.Patrolling:
                    if (patrolPoints.Length > 0)
                    {
                        agent.stoppingDistance = 0.1f;
                        targetPosition = patrolPoints[currentPatrolIndex].position;
                        moveDirection = (targetPosition - (Vector2)transform.position).normalized;
                        Debug.DrawLine(transform.position, targetPosition, Color.green); // Debug line for patrol path
                        if (Vector2.Distance(transform.position, targetPosition) < 0.8f)
                        {
                            currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Length; // Advance to next point
                            SetState(NPCState.Idle);
                            idleTimer = 0f; // Start idle timer
                        }
                    }
                    break;

                case NPCState.Approaching:
                    if (currentTarget != null)
                    {
                        agent.stoppingDistance = attackRange;
                        targetPosition = currentTarget.transform.position;
                        moveDirection = (targetPosition - (Vector2)transform.position).normalized;
                    }
                    else
                    {
                        SetState(NPCState.Patrolling);
                    }
                    break;

                case NPCState.Attacking:
                    if (currentTarget != null && Time.time - lastAttackTime >= attackCooldown)
                    {
                        attackId.Value++;
                        lastAttackTime = Time.time;
                    }
                    if (currentTarget != null)
                    {
                        targetPosition = currentTarget.transform.position;
                        moveDirection = (targetPosition - (Vector2)transform.position).normalized;
                    }
                    else
                    {
                        SetState(NPCState.Patrolling);
                    }
                    break;

                case NPCState.Fleeing:
                    if (currentTarget != null)
                    {
                        targetPosition = transform.position - (currentTarget.transform.position - transform.position).normalized * fleeRange;
                        moveDirection = (targetPosition - (Vector2)transform.position).normalized;
                    }
                    else
                    {
                        SetState(NPCState.Patrolling);
                    }
                    break;
                case NPCState.Dead:
                    isDead = true;
                    return;
            }
        }

        // Movement and avoidance
        if (moveDirection != Vector2.zero)
        {
            agent.SetDestination(targetPosition);
        }
        else
        {
            isMoving.Value = false;
            agent.ResetPath();
        }

        if (agent.velocity.magnitude > 0.5f)
        {
            isMoving.Value = true;
        }
        else
        {
            isMoving.Value = false;
        }

        // Sync position
        if (isMoving.Value)
        {
            syncPosition.Value = transform.position;
        }

        UpdateSpriteDirection(moveDirection);

        // Check for player-triggered state changes
        UpdateStateBasedOnTrigger();
    }

    private void RemoveNPC()
    {
        if (IsServer)
        {
            Debug.Log($"Removing NPC {name} from the game.");
            NetworkObject.Despawn();
        }
    }

    public void TriggerDistanceAttack()
    {
        if (currentTarget == null)
        {
            Debug.LogWarning("No current target for distance attack.");
            return;
        }
        Debug.Log($"Triggering distance attack on {currentTarget.name}");
        npcDistanceAttacks.DistanceAttack(currentTarget);
    }

    public void TriggerMeleeAttack()
    {
        npcMeleeAttacks.MeleeAttack();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log($"OnTriggerEnter2D: {other.name}");
        if (IsServer && other.TryGetComponent<Player>(out Player player))
        {
            if (!playersInRange.Contains(player))
            {
                Debug.Log($"Player {player.name} entered trigger range and added to playersInRange.");
                playersInRange.Add(player);
                if (currentTarget == null) currentTarget = player;
                UpdateStateBasedOnTrigger();
            }
        }
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (IsServer && other.TryGetComponent<Player>(out Player player))
        {
            Debug.Log($"OnTriggerStay2D: {player.name}");
            UpdateStateBasedOnTrigger();
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (IsServer && other.TryGetComponent<Player>(out Player player))
        {
            Debug.Log($"OnTriggerExit2D: {player.name}");
            playersInRange.Remove(player);
            if (player == currentTarget)
            {
                currentTarget = playersInRange.Count > 0 ? playersInRange[0] : null;
                if (currentTarget == null) SetState(NPCState.Idle);
                else UpdateStateBasedOnTrigger();
            }
        }
    }

    private void UpdateStateBasedOnTrigger()
    {
        if (!IsServer || currentTarget == null) return;
        float distance = Vector2.Distance(transform.position, currentTarget.transform.position);

        if (distance <= attackRange && Time.time - lastAttackTime >= attackCooldown)
        {
            Debug.Log($"Target {currentTarget.name} is within attack range. Attacking.");
            SetState(NPCState.Attacking);
        }
        else if (distance <= triggerRadius && currentState != NPCState.Attacking && currentState != NPCState.Fleeing)
        {
            Debug.Log($"Target {currentTarget.name} is within trigger radius. Approaching.");
            SetState(NPCState.Approaching);
        }
    }

    private void UpdateSpriteDirection(Vector2 moveDirection)
    {
        if (moveDirection.x != 0)
        {
            // Flip sprite: left if x < 0, right if x > 0
            transform.localScale = new Vector3(moveDirection.x > 0 ? -1 : 1, 1, 1);
        }
    }

    private void SetState(NPCState newState)
    {
        if (currentState != newState)
        {
            currentState = newState;
            syncState.Value = newState;
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void DamagePlayerServerRpc(ulong playerId, int damage)
    {
        Player player = GameManager.Instance.GetPlayer(playerId);
        if (player != null)
        {
            Health maxHealth = player.GetComponent<Health>();
            if (maxHealth != null)
            {
                maxHealth.TakeDamage(damage, default);
                if (maxHealth.CurrentHealth.Value <= 0)
                {
                    SetState(NPCState.Idle);
                }
            }
        }
    }

    private void OnIsMovingChanged(bool previous, bool current)
    {
        if (current != previous)
        {
            animator.SetBool("Walk", current);
        }
    }

    private void OnAttackIdChanged(int previous, int current)
    {
        if (animator != null && current != previous)
        {
            animator.SetTrigger("Attack");
            animator.SetBool("Walk", false);
        }
    }

    private void OnStateChanged(NPCState previous, NPCState current)
    {
        currentState = current;
    }

    private void OnPositionChanged(Vector2 previous, Vector2 current)
    {
        if (!IsServer)
        {
            transform.position = current; // Client snaps to server position
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void TakeDamageServerRpc(int damage)
    {
        maxHealth -= damage;
        if (maxHealth <= 0) SetState(NPCState.Fleeing);
    }

    [ClientRpc]
    private void PlayDeathAnimationClientRpc()
    {
        if (animator != null)
        {
            animator.SetTrigger("Die");
        }
    }

    private void SetUpTriggerCollider()
    {
        triggerCollider = GetComponents<CircleCollider2D>().FirstOrDefault(c => c.isTrigger);
        if (triggerCollider == null)
        {
            triggerCollider = gameObject.AddComponent<CircleCollider2D>();
            triggerCollider.isTrigger = true;
            triggerCollider.radius = triggerRadius;
        }
        else
        {
            triggerCollider.radius = triggerRadius; // Ensure the radius is set correctly
        }
    }

    private void SetUpPatrolPoints()
    {
        if (patrolPointsContainer != null)
        {
            patrolPoints = new Transform[patrolPointsContainer.childCount];
            for (int i = 0; i < patrolPointsContainer.childCount; i++)
            {
                patrolPoints[i] = patrolPointsContainer.GetChild(i);
            }
            if (patrolPoints.Length > 0) SetState(NPCState.Patrolling);
        }
        else
        {
            patrolPoints = new Transform[] { transform };
        }
    }

    private void SetUpNavMesh()
    {
        if (agent == null)
        {
            agent = GetComponent<NavMeshAgent>();
            if (agent == null)
            {
                agent = gameObject.AddComponent<NavMeshAgent>();
                Debug.LogWarning("NavMeshAgent was not found, added a new one.");
            }
        }
        agent.speed = moveSpeed;
        agent.stoppingDistance = 0.1f; // Set stopping distance for attacking
        agent.autoBraking = false; // Disable auto-braking to allow smooth transitions
        agent.updateRotation = false;
        agent.updateUpAxis = false;
    }
}