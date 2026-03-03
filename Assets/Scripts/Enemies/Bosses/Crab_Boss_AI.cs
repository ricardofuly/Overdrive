using UnityEngine;
using System.Collections;

public class Crab_Boss_AI : MonoBehaviour
{
    [Header("Behavior Settings")]
    [SerializeField] private float walkSpeed = 2f;
    [SerializeField] private float nearDistance = 3f;
    [SerializeField] private float midDistance = 7f;
    [SerializeField] private float attackCooldown = 2.5f;

    [Header("Dash Settings")]
    [SerializeField] private float dashForce = 15f;
    [SerializeField] private float dashDuration = 0.5f;

    [Header("Jump Settings")]
    [SerializeField] private float jumpHeight = 10f;
    [SerializeField] private float jumpHorizontalForce = 8f;

    [Header("Recoil Settings")]
    [SerializeField] private float recoilForce = 8f;
    [SerializeField] private float recoilDuration = 0.5f;

    [Header("Layer Masks")]
    [SerializeField] private LayerMask groundLayer;

    private Rigidbody2D rb;
    private Animator animator;
    private SpriteRenderer sr;
    private Transform player;
    private BossData bossData;

    private bool isAttacking = false;
    private bool isRecoiling = false;
    private float nextAttackTime = 0f;
    private bool isGrounded;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        sr = GetComponent<SpriteRenderer>();
        bossData = GetComponent<BossData>();
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
        
        float distance = Vector2.Distance(transform.position, player.position);
        LookAtPlayer();

        if (Time.time >= nextAttackTime)
        {
            if (distance > midDistance)
            {
                StartCoroutine(JumpAttack());
            }
            else if (distance > nearDistance)
            {
                StartCoroutine(DashAttack());
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
        // isAttacking = false; // Removido para não "quebrar" outras coroutines se não necessário
        // Apenas paramos o movimento de ataque enviando velocidade zero ou deixando o Update cuidar disso

        float dir = player.position.x > transform.position.x ? -1 : 1;
        rb.linearVelocity = new Vector2(dir * recoilForce, rb.linearVelocity.y);
        
        animator.Play("Crab_Idle"); // Ou uma animação de recuo se houver

        yield return new WaitForSeconds(recoilDuration);

        rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
        isRecoiling = false;
        isAttacking = false;
        nextAttackTime = Time.time + 0.5f; // Pequeno delay extra após recuo
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
        // Tenta ficar entre nearDistance e midDistance
        float targetDistance = (nearDistance + midDistance) / 2f;
        
        if (currentDistance < nearDistance + 1f) // Muito perto, afasta
        {
            float moveDir = player.position.x > transform.position.x ? -1 : 1;
            rb.linearVelocity = new Vector2(moveDir * walkSpeed, rb.linearVelocity.y);
        }
        else if (currentDistance > midDistance - 1f) // Muito longe, aproxima um pouco
        {
            float moveDir = player.position.x > transform.position.x ? 1 : -1;
            rb.linearVelocity = new Vector2(moveDir * walkSpeed * 0.5f, rb.linearVelocity.y);
        }
        else // Distância ideal, para
        {
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
        }
    }

    private void CheckGround()
    {
        isGrounded = Physics2D.Raycast(transform.position, Vector2.down, 1.5f, groundLayer);
    }

    private IEnumerator DashAttack()
    {
        isAttacking = true;
        animator.Play("Crab_Run");
        AudioManagerController.Instance?.PlaySFX(AudioManagerController.Instance.bossDashSFX);
        
        float dir = player.position.x > transform.position.x ? 1 : -1;
        float timer = 0;

        while (timer < dashDuration)
        {
            rb.linearVelocity = new Vector2(dir * dashForce, rb.linearVelocity.y);
            timer += Time.deltaTime;
            yield return null;
        }

        rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
        isAttacking = false;
        nextAttackTime = Time.time + attackCooldown;
    }

    private IEnumerator JumpAttack()
    {
        isAttacking = true;
        animator.Play("Crab_Jump");

        float dir = player.position.x > transform.position.x ? 1 : -1;
        rb.linearVelocity = new Vector2(dir * jumpHorizontalForce, jumpHeight);

        yield return new WaitUntil(() => rb.linearVelocity.y < 0);
        animator.Play("Crab_Fall");

        yield return new WaitUntil(() => isGrounded);
        animator.Play("Crab_Ground");
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
                animator.Play("Crab_Run");
            else
                animator.Play("Crab_Idle");
        }
        else
        {
            if (rb.linearVelocity.y > 0.1f)
                animator.Play("Crab_Jump");
            else
                animator.Play("Crab_Fall");
        }
    }
}
