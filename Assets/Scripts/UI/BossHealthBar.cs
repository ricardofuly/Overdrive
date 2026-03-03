using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BossHealthBar : MonoBehaviour
{
    [SerializeField] private Slider healthSlider;
    [SerializeField] private TextMeshProUGUI bossNameText;
    private BossData currentBoss;

    public void SetBoss(BossData boss, int maxHealth)
    {
        currentBoss = boss;
        bossNameText.text = boss.BossName;
        healthSlider.maxValue = maxHealth;
        healthSlider.value = maxHealth;
        gameObject.SetActive(true);
    }

    public void UpdateHealth(int currentHealth)
    {
        healthSlider.value = currentHealth;
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    // Opcional: Update constante caso a vida mude por outros meios
    /*
    private void Update()
    {
        if (currentBoss != null)
        {
            // Update slider based on currentBoss.currentHealth (se for público)
        }
    }
    */
}