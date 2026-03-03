using UnityEngine;

public class BossData : MonoBehaviour
{
    public enum AbilityReward { None, Dash, DoubleJump }

    [Header("Health")]
    [SerializeField] private int maxHealth = 10;
    public int MaxHealth => maxHealth;
    private int currentHealth;

    [Header("Name")]
    [SerializeField] private string bossName = "Boss";
    public string BossName => bossName;

    [Header("Rewards")]
    [SerializeField] private AbilityReward reward = AbilityReward.None;
    public AbilityReward Reward => reward;

    [Header("Audio")]
    [SerializeField] private AudioClip bossMusic;
    public AudioClip BossMusic => bossMusic;

    public System.Action OnBossDied;

    void Awake()
    {
        currentHealth = maxHealth;
    }

    public void ActivateBoss()
    {
        Debug.Log("Boss Activated!");
    }

    public void TakeDamage(int damage)
    {
        currentHealth -= damage;
        Debug.Log($"Boss took {damage} damage. HP: {currentHealth}");

        if (UIManagerController.Instance != null && UIManagerController.Instance.BossBar != null)
        {
            UIManagerController.Instance.BossBar.UpdateHealth(currentHealth);
        }

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    public void Die()
    {
        UnlockPlayerAbility();
        OnBossDied?.Invoke();
        Destroy(gameObject);
    }

    private void UnlockPlayerAbility()
    {
        if (reward == AbilityReward.None) return;

        PlayerMovement player = FindFirstObjectByType<PlayerMovement>();
        if (player != null)
        {
            if (reward == AbilityReward.Dash)
                player.UnlockDash();
            else if (reward == AbilityReward.DoubleJump)
                player.UnlockDoubleJump();
            
            Debug.Log($"Boss defeated! Unlocked: {reward}");
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        PlayerMovement player = collision.collider.GetComponent<PlayerMovement>();
        if (player != null)
        {
            // Calcula direção do knockback (oposto ao boss)
            Vector2 knockbackDir = (player.transform.position - transform.position).normalized;
            player.TakeDamage(1, knockbackDir);
            AudioManagerController.Instance?.PlaySFX(AudioManagerController.Instance.bossHitPlayerSFX);
            CameraBounds.Instance?.Shake(0.15f, 0.15f);

            // Avisa a IA do Boss que ele acertou o player
            SendMessage("OnPlayerHit", SendMessageOptions.DontRequireReceiver);
        }
    }

    // private void OnGUI()
    // {
    //     if (currentHealth <= 0 || !BossArenaManager.IsArenaActive) return;

    //     if (Camera.main == null) return;

    //     Vector3 screenPos = Camera.main.WorldToScreenPoint(transform.position + Vector3.up * 2f);
    //     if (screenPos.z > 0)
    //     {
    //         Rect rect = new Rect(screenPos.x - 50, Screen.height - screenPos.y - 25, 100, 25);
    //         GUIStyle style = new GUIStyle(GUI.skin.box);
    //         style.normal.textColor = Color.red;
    //         style.fontStyle = FontStyle.Bold;
    //         GUI.Box(rect, $"BOSS: {currentHealth}", style);
    //     }
    // }
}
