using UnityEngine;

public class HealthPotion : MonoBehaviour
{
    [SerializeField] private int healthAmount = 3;
    [SerializeField] public int dropChance = 30;

    public bool ShouldDrop()
    {
        return Random.Range(0, 100) <= dropChance;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerMovement player = other.GetComponent<PlayerMovement>();
            if (player != null)
            {
                if (player.IsFullHealth()) return;

                player.Heal(healthAmount);
                Destroy(gameObject);
            }
        }
    }
}
