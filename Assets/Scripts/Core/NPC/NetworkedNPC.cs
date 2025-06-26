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
    public Transform[] PatrolPoints;
    [SerializeField] private AudioClip attackSound;
    [Space(10)]

    [Header("NPC Settings")]
    [SerializeField, Range(1, 6)] private float moveSpeed = 2f;
    [SerializeField, Range(5, 150)] private int maxHealth = 10;
    [SerializeField, Range(3, 25)] private int attackDamage = 5;
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
    [SerializeField] private float timeUntilHealed = 30f;
    public int TeamIndex = -2;
    [Space(10)]

    private int currentPatrolIndex = 0;
    private NPCState currentState = NPCState.Idle;
    private NetworkVariable<NPCState> syncState = new NetworkVariable<NPCState>(NPCState.Idle, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private NetworkVariable<Vector2> syncPosition = new NetworkVariable<Vector2>(Vector2.zero, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private NetworkVariable<bool> isMoving = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private NetworkVariable<int> attackId = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private float lastAttackTime = 0f;
    private CircleCollider2D triggerCollider;
    private List<Player> playersInRange = new List<Player>();
    private Player currentTarget;
    //private bool isMoving = false; // Track active movement
    private float idleTimer = 0f; // Timer for idle duration
    private float healTimer = 0f; // Timer for healing

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
        healthComponent.OnDamaged += OnDamaged;
    }

    private void Start()
    {
        if (PatrolPoints.Length == 0)
        {
            FallbackPatrolPoint();
        }   
    }

    private void OnDie()
    {
        SetState(NPCState.Dead); // Optional: set state to Dead
        PlayDeathAnimationClientRpc();
        AudioManager.Instance.PlayRandomSFX("Die");
        Invoke(nameof(RemoveNPC), deathDespawnDelay);
        Debug.Log($"NPC {name} has died and will be removed after {deathDespawnDelay} seconds.");
    }

    private void OnDamaged(Player attacker)
    {
        if (!IsServer || isDead) return;
        if (attacker != null)
        {
            if (!playersInRange.Contains(attacker))
                playersInRange.Add(attacker);

            // If no target or current target is dead/invisible, set new target
            if (currentTarget == null || currentTarget.IsInvisible || currentTarget.GetComponent<Character>().IsDead)
            {
                currentTarget = attacker;
                SetState(NPCState.Approaching);
            }
        }
    }

    private void Update()
    {
        if (!IsServer) return;

        StateMachine();

        if (isDead) return;

        ResetHealth();
    }

    private void ResetHealth()
    {
        if (currentState == NPCState.Idle || currentState == NPCState.Patrolling)
        {
            if (healTimer >= timeUntilHealed)
            {
                healthComponent.Heal(maxHealth);
                healTimer = 0f; // Reset heal timer
            }
            else
            {
                healTimer += Time.deltaTime; // Increment heal timer
            }
        }
        else
        {
            healTimer = 0f; // Reset heal timer when not idle
        }
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

            if (idleTimer >= idleDuration && PatrolPoints.Length > 0)
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
                    if (PatrolPoints.Length > 0)
                    {
                        agent.stoppingDistance = 0.1f;
                        targetPosition = PatrolPoints[currentPatrolIndex].position;
                        moveDirection = (targetPosition - (Vector2)transform.position).normalized;
                        Debug.DrawLine(transform.position, targetPosition, Color.green); // Debug line for patrol path
                        if (Vector2.Distance(transform.position, targetPosition) < 0.8f)
                        {
                            currentPatrolIndex = (currentPatrolIndex + 1) % PatrolPoints.Length; // Advance to next point
                            SetState(NPCState.Idle);
                        }
                    }
                    break;

                case NPCState.Approaching:
                    if (currentTarget != null && !currentTarget.IsInvisible && !currentTarget.GetComponent<Character>().IsDead)
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
                    foreach (Collider collider in GetComponents<Collider>())
                    {
                        collider.enabled = false; // Disable all colliders
                    }
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
        if (currentTarget == null || currentTarget.IsInvisible || currentTarget.GetComponent<Character>().IsDead)
        {
            Debug.LogWarning("No current target for distance attack.");
            return;
        }
        Debug.Log($"Triggering distance attack on {currentTarget.name}");
        npcDistanceAttacks.DistanceAttack(currentTarget);
    }

    public void TriggerMeleeAttack()
    {
        if (currentTarget == null || currentTarget.IsInvisible || currentTarget.GetComponent<Character>().IsDead)
        {
            Debug.LogWarning("No current target for melee attack.");
            return;
        }
        npcMeleeAttacks.MeleeAttack();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (IsServer && other.TryGetComponent<Player>(out Player player) && !isDead)
        {
            if (player.IsInvisible || player.GetComponent<Character>().IsDead) return;

            if (!playersInRange.Contains(player))
            {
                Debug.Log($"Player {player.name} entered trigger range and added to playersInRange.");
                playersInRange.Add(player);
                if (currentTarget == null || currentTarget.IsInvisible)
                {
                    currentTarget = player;
                }
                UpdateStateBasedOnTrigger();
            }
        }
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (IsServer && other.TryGetComponent<Player>(out Player player) && !isDead)
        {
            Debug.Log($"OnTriggerStay2D: {player.name}");

            if (player.IsInvisible)
            {
                if (playersInRange.Contains(player))
                {
                    playersInRange.Remove(player);
                    if (currentTarget == player)
                    {
                        currentTarget = playersInRange.FirstOrDefault(p => !p.IsInvisible);
                        if (currentTarget == null)
                        {
                            SetState(NPCState.Idle);
                        }
                        else
                        {
                            UpdateStateBasedOnTrigger();
                        }
                    }
                }
            }
            else
            {
                if (!playersInRange.Contains(player))
                {
                    playersInRange.Add(player);
                    if (currentTarget == null || currentTarget.IsInvisible)
                    {
                        currentTarget = player;
                    }
                }
                UpdateStateBasedOnTrigger();
            }
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (IsServer && other.TryGetComponent<Player>(out Player player) && !isDead)
        {
            Debug.Log($"OnTriggerExit2D: {player.name}");
            playersInRange.Remove(player);
            if (player == currentTarget)
            {
                currentTarget = playersInRange.FirstOrDefault(p => !p.IsInvisible);
                if (currentTarget == null)
                {
                    SetState(NPCState.Idle);
                }
                else
                {
                    UpdateStateBasedOnTrigger();
                }
            }
        }
    }

    private void UpdateStateBasedOnTrigger()
    {
        if (!IsServer) return;

        playersInRange = playersInRange.Where(p => p != null && !p.IsInvisible && !p.GetComponent<Character>().IsDead).ToList();

        if (currentTarget == null || currentTarget.IsInvisible || currentTarget.GetComponent<Character>().IsDead)
        {
            currentTarget = playersInRange.FirstOrDefault(p => !p.IsInvisible && !p.GetComponent<Character>().IsDead);
        }

        if (currentTarget == null)
        {
            SetState(NPCState.Idle);
            return;
        }

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
            if (currentState == NPCState.Idle && newState != NPCState.Idle)
            {
                idleTimer = 0f; // Start idle timer
            }
            currentState = newState;
            syncState.Value = newState;
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
            PatrolPoints = new Transform[patrolPointsContainer.childCount];
            for (int i = 0; i < patrolPointsContainer.childCount; i++)
            {
                PatrolPoints[i] = patrolPointsContainer.GetChild(i);
            }
            if (PatrolPoints.Length > 0) SetState(NPCState.Patrolling);
        }
    }

    private void FallbackPatrolPoint()
    {
        PatrolPoints = new Transform[1];
        GameObject patrolPoint = new GameObject($"{gameObject.name}_StartPoint");
        patrolPoint.transform.position = transform.position;
        PatrolPoints[0] = patrolPoint.transform;
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

    public void PlayAttackSound()
    {
        AudioManager.Instance.PlaySFX(attackSound);
    }
}