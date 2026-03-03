using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerCombat : MonoBehaviour
{
    [Header("Combo Settings")]
    [SerializeField] private float comboResetTime = 0.8f;
    [SerializeField] private float attackCooldown = 0.2f;

    [Header("Hitbox")]
    [SerializeField] private Collider2D attackHitBox;
    [SerializeField] private float hitBoxDuration = 0.15f;

    private PlayerMovement playerMovement;
    private Animator animator;

    private int comboCount = 0;
    private float lastAttackTime = 0f;
    private float nextAttackTime = 0f;

    private void Awake()
    {
        playerMovement = GetComponent<PlayerMovement>();
        animator = GetComponent<Animator>();

        if (attackHitBox != null)
        {
            attackHitBox.enabled = false;
        }
    }

    private void Update()
    {
        // Reseta o combo se o player demorar muito entre os ataques
        if (Time.time - lastAttackTime > comboResetTime)
        {
            comboCount = 0;
        }
    }

    public void OnAttack(InputValue value)
    {
        if (!value.isPressed || Time.time < nextAttackTime || !enabled || playerMovement.IsKnocked) return;

        if (playerMovement.IsGrounded)
        {
            HandleGroundAttack();
        }
        else
        {
            HandleAirAttack();
        }

        lastAttackTime = Time.time;
        nextAttackTime = Time.time + attackCooldown;

        if (attackHitBox != null)
        {
            StopAllCoroutines(); // Para hitboxes anteriores se estiver no meio do combo rápido
            StartCoroutine(EnableHitBox());
        }
    }

    private System.Collections.IEnumerator EnableHitBox()
    {
        attackHitBox.enabled = true;
        yield return new WaitForSeconds(hitBoxDuration);
        attackHitBox.enabled = false;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Enemy") || other.CompareTag("Boss"))
        {
            EnemyData enemy = other.GetComponent<EnemyData>();
            if (enemy != null)
            {
                enemy.TakeDamage(1);
                AudioManagerController.Instance?.PlaySFXRandomPitch(AudioManagerController.Instance.playerHitEnemySFX);
            }

            BossData boss = other.GetComponent<BossData>();
            if (boss != null)
            {
                boss.TakeDamage(1);
                AudioManagerController.Instance?.PlaySFXRandomPitch(AudioManagerController.Instance.playerHitEnemySFX);
            }
        }
    }

    private void HandleGroundAttack()
    {
        comboCount++;
        if (comboCount > 3) comboCount = 1;

        string attackName = "Attack" + comboCount;
        playerMovement.PlayAnimation(attackName, 0.3f);
    }

    private void HandleAirAttack()
    {
        // No ar o combo pode ser diferente ou apenas 2 hits
        comboCount++;
        if (comboCount > 2) comboCount = 1;

        string attackName = "AirAttack" + comboCount;
        playerMovement.PlayAnimation(attackName, 0.3f);
    }
}
