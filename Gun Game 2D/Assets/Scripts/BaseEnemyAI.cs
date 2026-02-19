using System.Collections;
using UnityEngine;

/// <summary>
/// State-machine based Enemy AI for a 2D platformer/shooter.
/// States: Patrol → Chase → Attack
///
/// Setup Requirements:
///   • Attach this script to an enemy GameObject.
///   • Assign 'player' in the Inspector (or tag your Player "Player").
///   • Add a Health component to this GameObject.
///   • Add a Rigidbody2D (Freeze Z rotation) and a Collider2D.
///   • Optionally add a Transform[] patrolPoints array in Inspector.
///   • If the enemy should shoot, assign 'projectilePrefab' and 'muzzlePoint'.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Health))]
public class BaseEnemyAI : MonoBehaviour
{
    // ─── State ───────────────────────────────────────────────────────────────
    private enum State { Patrol, Chase, Attack }
    private State currentState = State.Patrol;

    // ─── Inspector: References ────────────────────────────────────────────────
    [Header("References")]
    [Tooltip("Drag the Player here, or leave blank to auto-find by tag 'Player'")]
    public Transform player;

    [Tooltip("Optional muzzle origin for shooting. If null, spawns at this transform.")]
    public Transform muzzlePoint;

    [Tooltip("The gun child Transform to rotate toward the player. Mirrors player Gun.cs rotation logic.")]
    public Transform gunTransform;

    [Tooltip("How fast (degrees/sec) the gun rotates to track the player. Use 0 for instant.")]
    public float gunRotationSpeed = 360f;

    [Tooltip("Projectile prefab to shoot. Leave null for melee-only enemy.")]
    public GameObject projectilePrefab;

    // ─── Inspector: Detection ─────────────────────────────────────────────────
    [Header("Detection")]
    [Tooltip("Distance at which enemy spots the player")]
    public float detectionRange = 8f;

    [Tooltip("Distance at which the enemy stops chasing and forgets the player")]
    public float loseRange = 12f;

    [Tooltip("Layers that block line of sight")]
    public LayerMask obstacleMask = ~0;

    // ─── Inspector: Movement ──────────────────────────────────────────────────
    [Header("Movement")]
    public float moveSpeed = 3f;

    [Tooltip("Waypoints to patrol between. If empty, the enemy will pace left/right on a set distance.")]
    public Transform[] patrolPoints;

    [Tooltip("Used for edge/wall detection while patrolling (only when no patrolPoints set)")]
    public float edgeCheckDistance = 0.5f;
    public float wallCheckDistance = 0.3f;
    public string groundTag = "Ground";
    public LayerMask groundMask;

    // ─── Inspector: Combat ────────────────────────────────────────────────────
    [Header("Combat")]
    [Tooltip("Distance at which enemy tries to attack")]
    public float attackRange = 4f;

    [Tooltip("Shots per second (for ranged enemies)")]
    public float fireRate = 1.5f;

    [Tooltip("Speed of spawned projectiles")]
    public float bulletSpeed = 12f;

    [Tooltip("Damage dealt per hit (melee)")]
    public int meleeDamage = 1;

    [Tooltip("Seconds between melee hits")]
    public float meleeRate = 1f;

    // ─── Private state ────────────────────────────────────────────────────────
    private Rigidbody2D rb;
    private Health health;

    private int patrolIndex = 0;
    private int patrolDir = 1;          // +1 or -1 for simple left/right patrol
    private float fireCooldown = 0f;
    private float meleeCooldown = 0f;

    private bool facingRight = true;

    // ─────────────────────────────────────────────────────────────────────────
    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        health = GetComponent<Health>();

        // Auto-find player if not assigned
        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null) player = playerObj.transform;
        }

        // Freeze Z rotation so the body stays upright
        rb.freezeRotation = true;
    }

    private void Update()
    {
        fireCooldown  -= Time.deltaTime;
        meleeCooldown -= Time.deltaTime;

        if (player == null) return;

        float distToPlayer = Vector2.Distance(transform.position, player.position);
        bool canSeePlayer   = HasLineOfSight();

        // ── State transitions ─────────────────────────────────────────
        switch (currentState)
        {
            case State.Patrol:
                if (distToPlayer <= detectionRange && canSeePlayer)
                    SetState(distToPlayer <= attackRange ? State.Attack : State.Chase);
                break;

            case State.Chase:
                if (distToPlayer > loseRange || !canSeePlayer)
                    SetState(State.Patrol);
                else if (distToPlayer <= attackRange)
                    SetState(State.Attack);
                break;

            case State.Attack:
                if (distToPlayer > attackRange * 1.2f) // small hysteresis
                    SetState(distToPlayer > loseRange ? State.Patrol : State.Chase);
                break;
        }

        // ── State behaviour ───────────────────────────────────────────
        switch (currentState)
        {
            case State.Patrol: DoPatrol();  break;
            case State.Chase:  DoChase();   break;
            case State.Attack: DoAttack();  break;
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // State helpers
    // ─────────────────────────────────────────────────────────────────────────
    private void SetState(State next)
    {
        currentState = next;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Patrol
    // ─────────────────────────────────────────────────────────────────────────
    private void DoPatrol()
    {
        if (patrolPoints != null && patrolPoints.Length >= 2)
        {
            WaypointPatrol();
        }
        else
        {
            EdgePatrol();
        }
    }

    private void WaypointPatrol()
    {
        Transform target = patrolPoints[patrolIndex];
        float dist = Mathf.Abs(transform.position.x - target.position.x);

        if (dist < 0.15f)
        {
            patrolIndex = (patrolIndex + 1) % patrolPoints.Length;
            return;
        }

        float dir = Mathf.Sign(target.position.x - transform.position.x);
        rb.velocity = new Vector2(dir * moveSpeed, rb.velocity.y);
        FaceDirection(dir > 0);
        ResetGunRotation();
    }

    private void EdgePatrol()
    {
        // Move in current direction; reverse on wall or edge
        Vector2 feetPos   = (Vector2)transform.position + Vector2.down * 0.6f;
        Vector2 aheadFeet = feetPos + Vector2.right * patrolDir * edgeCheckDistance;
        bool groundAhead  = Physics2D.Raycast(aheadFeet, Vector2.down, 0.5f, groundMask);

        Vector2 wallCheck = (Vector2)transform.position + Vector2.right * patrolDir * wallCheckDistance;
        bool wallAhead    = Physics2D.Raycast(wallCheck, Vector2.right * patrolDir, 0.1f, groundMask);

        if (!groundAhead || wallAhead)
        {
            patrolDir *= -1;
        }

        rb.velocity = new Vector2(patrolDir * moveSpeed, rb.velocity.y);
        FaceDirection(patrolDir > 0);
        ResetGunRotation();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Chase
    // ─────────────────────────────────────────────────────────────────────────
    private void DoChase()
    {
        float dir = Mathf.Sign(player.position.x - transform.position.x);
        rb.velocity = new Vector2(dir * moveSpeed, rb.velocity.y);
        FaceDirection(dir > 0);
        RotateGunToTarget(player.position);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Attack
    // ─────────────────────────────────────────────────────────────────────────
    private void DoAttack()
    {
        // Stop horizontal movement while attacking
        rb.velocity = new Vector2(0f, rb.velocity.y);

        // Face the player while attacking
        FaceDirection(player.position.x > transform.position.x);
        RotateGunToTarget(player.position);

        if (projectilePrefab != null)
        {
            // Ranged
            if (fireCooldown <= 0f)
            {
                ShootProjectile();
                fireCooldown = 1f / Mathf.Max(0.001f, fireRate);
            }
        }
        else
        {
            // Melee
            if (meleeCooldown <= 0f)
            {
                DoMeleeHit();
                meleeCooldown = meleeRate;
            }
        }
    }

    private void ShootProjectile()
    {
        Transform origin = muzzlePoint != null ? muzzlePoint : transform;
        Vector2 dir = ((Vector2)player.position - (Vector2)origin.position).normalized;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

        GameObject proj = Instantiate(projectilePrefab, origin.position, Quaternion.Euler(0, 0, angle));
        Destroy(proj, 5f);
        Rigidbody2D projRb = proj.GetComponent<Rigidbody2D>();
        if (projRb != null) projRb.velocity = dir * bulletSpeed;
    }

    private void DoMeleeHit()
    {
        float dist = Vector2.Distance(transform.position, player.position);
        if (dist <= attackRange)
        {
            player.SendMessage("TakeDamage", meleeDamage, SendMessageOptions.DontRequireReceiver);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Line of Sight
    // ─────────────────────────────────────────────────────────────────────────
    private bool HasLineOfSight()
    {
        if (player == null) return false;
        Vector2 origin    = transform.position;
        Vector2 direction = (player.position - transform.position).normalized;
        float   distance  = Vector2.Distance(transform.position, player.position);

        RaycastHit2D hit = Physics2D.Raycast(origin, direction, distance, obstacleMask);
        // If we hit something and it's NOT the player, LOS is blocked
        if (hit.collider != null && hit.transform != player)
            return false;

        return true;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Gun Rotation
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Rotates the gun child to aim toward 'target' in world space.
    /// Mirrors the RotateToMouse logic in Gun.cs, accounting for parent flip.
    /// </summary>
    private void RotateGunToTarget(Vector2 target)
    {
        if (gunTransform == null) return;

        Vector2 dir = target - (Vector2)gunTransform.position;

        // Mirror Gun.cs: if parent is flipped, invert X so local rotation stays correct
        if (gunTransform.lossyScale.x < 0)
            dir.x = -dir.x;

        if (dir.sqrMagnitude <= 0.0001f) return;

        float targetAngle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

        if (gunRotationSpeed <= 0f)
        {
            gunTransform.localRotation = Quaternion.Euler(0f, 0f, targetAngle);
        }
        else
        {
            float current = gunTransform.localEulerAngles.z;
            float smoothed = Mathf.MoveTowardsAngle(current, targetAngle, gunRotationSpeed * Time.deltaTime);
            gunTransform.localRotation = Quaternion.Euler(0f, 0f, smoothed);
        }
    }

    /// <summary>Resets the gun to a neutral forward-facing local rotation.</summary>
    private void ResetGunRotation()
    {
        if (gunTransform == null) return;
        float current = gunTransform.localEulerAngles.z;
        float smoothed = Mathf.MoveTowardsAngle(current, 0f,
            (gunRotationSpeed > 0f ? gunRotationSpeed : 720f) * Time.deltaTime);
        gunTransform.localRotation = Quaternion.Euler(0f, 0f, smoothed);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Sprite Flip
    // ─────────────────────────────────────────────────────────────────────────
    private void FaceDirection(bool right)
    {
        if (facingRight == right) return;
        facingRight = right;
        Vector3 s = transform.localScale;
        s.x = right ? Mathf.Abs(s.x) : -Mathf.Abs(s.x);
        transform.localScale = s;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Gizmos (editor visualisation)
    // ─────────────────────────────────────────────────────────────────────────
    private void OnDrawGizmosSelected()
    {
        // Detection range
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        // Attack range
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        // Lose range
        Gizmos.color = new Color(1f, 0.5f, 0f);
        Gizmos.DrawWireSphere(transform.position, loseRange);
    }
}
