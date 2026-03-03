using UnityEngine;
using System.Collections;

public class PinkStar_Boss_AI : MonoBehaviour
{
    [Header("Behavior Settings")]
    [SerializeField] private float walkSpeed = 2.2f;
    [SerializeField] private float nearDistance = 3f;
    [SerializeField] private float midDistance = 7.5f;
    [SerializeField] private float attackCooldown = 2.8f;

    [Header("Spinning Dash")]
    [SerializeField] private float spinDashForce = 16f;
    [SerializeField] private float spinDashDuration = 0.7f;

    [Header("Spinning Jump")]
    [SerializeField] private float jumpHeight = 11f;
    [SerializeField] private float jumpHorizontalForce = 7f;

    [Header("Recoil Settings")]
    [SerializeField] private float recoilForce = 9f;
    [SerializeField] private float recoilDuration = 0.5f;

    [Header("Layer Masks")]
    [SerializeField] private LayerMask groundLayer;

    private Rigidbody2D rb;
    private Animator animator;
    private SpriteRenderer sr;
    private Transform player;
    private bool isAttacking = false;
    private bool isRecoiling = false;
    private float nextAttackTime = 0f;
    private bool isGrounded;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        sr = GetComponent<SpriteRenderer>();
    }

    void Start()
    {
        // O player agora é passado pelo BossArenaManager via SetPlayerReference
        // Isso evita o freeze causado por FindGameObjectWithTag durante o spawn
        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null) player = playerObj.transform;
        }
    }

    public void SetPlayerReference(Transform playerTransform)
    {
        player = playerTransform;
    }

    void Update()
    {
        if (!BossArenaManager.IsArenaActive || player == null || isAttacking || isRecoiling) return;

        CheckGround();
        LookAtPlayer();

        float distance = Vector2.Distance(transform.position, player.position);

        if (Time.time >= nextAttackTime)
        {
            if (distance > midDistance)
            {
                StartCoroutine(SpinningJump());
            }
            else if (distance > nearDistance)
            {
                StartCoroutine(SpinningDash());
            }
            else
            {
                WalkTowardsPlayer();
            }
        }
        else
        {
            // Cooldown: Mantém distância do player
            MaintainSafeDistance(distance);
        }

        UpdateAnimations();
    }

    public void OnPlayerHit()
    {
        if (isRecoiling) return;
        StartCoroutine(RecoilCoroutine());
    }

    private IEnumerator RecoilCoroutine()
    {
        isRecoiling = true;
        // isAttacking = false;

        float dir = player.position.x > transform.position.x ? -1 : 1;
        rb.linearVelocity = new Vector2(dir * recoilForce, rb.linearVelocity.y);
        
        animator.Play("PinkStar_Idle");

        yield return new WaitForSeconds(recoilDuration);

        rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
        isRecoiling = false;
        isAttacking = false;
        nextAttackTime = Time.time + 0.5f;
    }

    private void LookAtPlayer()
    {
        if (player.position.x > transform.position.x)
            sr.flipX = false;
        else
            sr.flipX = true;
    }

    private void WalkTowardsPlayer()
    {
        float dir = player.position.x > transform.position.x ? 1 : -1;
        rb.linearVelocity = new Vector2(dir * walkSpeed, rb.linearVelocity.y);
    }

    private void MaintainSafeDistance(float currentDistance)
    {
        if (currentDistance < nearDistance + 1f)
        {
            float moveDir = player.position.x > transform.position.x ? -1 : 1;
            rb.linearVelocity = new Vector2(moveDir * walkSpeed, rb.linearVelocity.y);
        }
        else if (currentDistance > midDistance - 1f)
        {
            float moveDir = player.position.x > transform.position.x ? 1 : -1;
            rb.linearVelocity = new Vector2(moveDir * walkSpeed * 0.5f, rb.linearVelocity.y);
        }
        else
        {
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
        }
    }

    private void CheckGround()
    {
        isGrounded = Physics2D.Raycast(transform.position, Vector2.down, 1.5f, groundLayer);
    }

    private IEnumerator SpinningDash()
    {
        isAttacking = true;
        animator.Play("PinkStar_Run"); 
        AudioManagerController.Instance?.PlaySFX(AudioManagerController.Instance.bossDashSFX);
        
        float dir = player.position.x > transform.position.x ? 1 : -1;
        float timer = 0;

        while (timer < spinDashDuration)
        {
            rb.linearVelocity = new Vector2(dir * spinDashForce, rb.linearVelocity.y);
            timer += Time.deltaTime;
            yield return null;
        }

        rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
        isAttacking = false;
        nextAttackTime = Time.time + attackCooldown;
    }

    private IEnumerator SpinningJump()
    {
        isAttacking = true;
        animator.Play("PinkStar_Jump");

        float dir = player.position.x > transform.position.x ? 1 : -1;
        rb.linearVelocity = new Vector2(dir * jumpHorizontalForce, jumpHeight);

        yield return new WaitUntil(() => rb.linearVelocity.y < 0);
        animator.Play("PinkStar_Fall");

        yield return new WaitUntil(() => isGrounded);
        animator.Play("PinkStar_Ground");
        AudioManagerController.Instance?.PlaySFX(AudioManagerController.Instance.bossLandSFX);
        CameraBounds.Instance?.Shake(0.25f, 0.4f);

        yield return new WaitForSeconds(0.2f);
        isAttacking = false;
        nextAttackTime = Time.time + attackCooldown;
    }

    private void UpdateAnimations()
    {
        if (isAttacking || isRecoiling) return;

        if (isGrounded)
        {
            if (Mathf.Abs(rb.linearVelocity.x) > 0.1f)
                animator.Play("PinkStar_Run");
            else
                animator.Play("PinkStar_Idle");
        }
        else
        {
            if (rb.linearVelocity.y > 0.1f)
                animator.Play("PinkStar_Jump");
            else
                animator.Play("PinkStar_Fall");
        }
    }
}
