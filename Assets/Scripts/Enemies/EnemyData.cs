using UnityEngine;

public class EnemyData : MonoBehaviour
{
    [Header("HP Settings")]
    [SerializeField] private int maxHealth = 1;
    private int currentHealth;

    [Header("Drop Settings")]
    [SerializeField] private GameObject healthPotionPrefab;
    
    public LevelTile ParentTile { get; private set; }
    
    // Components
    private CrabMovement crabMovement;
    private FierceTooth_Movement toothMovement;
    private PinkStar_Movement starMovement;

    void Awake()
    {
        currentHealth = maxHealth;
        crabMovement = GetComponent<CrabMovement>();
        toothMovement = GetComponent<FierceTooth_Movement>();
        starMovement = GetComponent<PinkStar_Movement>();
    }

    public void Initialize(LevelTile tile)
    {
        ParentTile = tile;
        ParentTile.OnTileDestroyed += HandleTileDestroyed;
    }

    private void HandleTileDestroyed()
    {
        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        if (ParentTile != null)
            ParentTile.OnTileDestroyed -= HandleTileDestroyed;
    }

    public void TakeDamage(int damage)
    {
        if (currentHealth <= 0) return;

        currentHealth -= damage;

        if (crabMovement != null) crabMovement.PlayAnimation("Crab_Hit", 0.3f);
        if (toothMovement != null) toothMovement.PlayAnimation("FierceTooth_Hit", 0.3f);
        if (starMovement != null) starMovement.PlayAnimation("PinkStar_Hit", 0.3f);

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void DropHealthPotion()
    {
        if (healthPotionPrefab != null && healthPotionPrefab.GetComponent<HealthPotion>().ShouldDrop())
        {
            Instantiate(healthPotionPrefab, transform.position, Quaternion.identity);
        }
    }

    private void Die()
    {
        // Crab logic
        if (crabMovement != null)
        {
            string deathAnim = crabMovement.IsGrounded ? "Crab_DeadGround" : "Crab_DeadHit";
            crabMovement.PlayAnimation(deathAnim, 2f);

            DropHealthPotion();

            CleanupDeath();
        }
        // FierceTooth logic
        else if (toothMovement != null)
        {
            string deathAnim = toothMovement.IsGrounded ? "FierceTooth_DeadGround" : "FierceTooth_DeadHit";
            toothMovement.PlayAnimation(deathAnim, 2f);

            DropHealthPotion();

            CleanupDeath();
        }
        // PinkStar logic
        else if (starMovement != null)
        {
            string deathAnim = starMovement.IsGrounded ? "PinkStar_DeadGround" : "PinkStar_DeadHit";
            starMovement.PlayAnimation(deathAnim, 2f);

            DropHealthPotion();

            CleanupDeath();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void CleanupDeath()
    {
        GetComponent<Collider2D>().enabled = false;
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null) rb.simulated = false;
        
        // Desativa AIs
        MonoBehaviour aiCrab = GetComponent<CrabAI>() as MonoBehaviour;
        if (aiCrab != null) aiCrab.enabled = false;
        
        MonoBehaviour aiTooth = GetComponent<FierceTooth_AI>() as MonoBehaviour;
        if (aiTooth != null) aiTooth.enabled = false;

        MonoBehaviour aiStar = GetComponent<PinkStar_AI>() as MonoBehaviour;
        if (aiStar != null) aiStar.enabled = false;

        Destroy(gameObject, 1f);
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (currentHealth <= 0) return;

        PlayerMovement player = collision.collider.GetComponent<PlayerMovement>();

        if (player != null)
        {
            Vector2 knockbackDir = (player.transform.position - transform.position).normalized;
            if (knockbackDir.y < 0.2f) knockbackDir.y = 0.3f;
            
            player.TakeDamage(1, knockbackDir);
            TakeDamage(1);
            AudioManagerController.Instance?.PlaySFX(AudioManagerController.Instance.enemyHitPlayerSFX);
            CameraBounds.Instance?.Shake(0.1f, 0.1f);
        }
    }
}
