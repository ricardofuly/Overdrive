using UnityEngine;

public class BossArenaManager : MonoBehaviour
{
    [Header("Setup")]
    [SerializeField] private BossData boss;
    [SerializeField] private ArenaBounds arenaBounds;
    [SerializeField] private GameObject[] arenaWalls; // Paredes fixas no prefab
    public static bool IsArenaActive { get; private set; } = false;
    private bool arenaStarted = false;

    private void Awake()
    {
        // Garante que as paredes começam desativadas
        ToggleWalls(false);
    }

    private void ToggleWalls(bool active)
    {
        if (arenaWalls == null) return;
        foreach (var wall in arenaWalls)
        {
            if (wall != null) wall.SetActive(active);
        }
    }

    public void SetBoss(BossData newBoss)
    {
        boss = newBoss;
        
        // Se a arena já começou, ativa o boss imediatamente ao ser spawnado
        if (arenaStarted && boss != null)
        {
            boss.ActivateBoss();
            boss.OnBossDied += HandleBossDeath;
            
            // Passa a referência do player para o boss para evitar FindGameObjectWithTag
            PlayerMovement player = FindFirstObjectByType<PlayerMovement>();
            if (player != null)
            {
                // Chama um método no boss AI para definir o player diretamente
                SendMessage("SetPlayerReference", player.transform, SendMessageOptions.DontRequireReceiver);
            }
            
            // Ativa Barra de Vida
            if (UIManagerController.Instance != null && UIManagerController.Instance.BossBar != null)
            {
                UIManagerController.Instance.BossBar.SetBoss(boss, boss.MaxHealth);
                UIManagerController.Instance.ShowBossBar(true);
            }

            // Toca a música específica do Boss
            if (boss.BossMusic != null && AudioManagerController.Instance != null)
            {
                AudioManagerController.Instance.PlayMusic(boss.BossMusic);
            }
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (arenaStarted) return;

        if (other.CompareTag("Player"))
        {
            Debug.Log($"[ARENA] Player entrou na arena! Colisor: {other.name}");
            arenaStarted = true;
            IsArenaActive = true;

            arenaBounds.CreateBounds(Camera.main);
            ToggleWalls(true);

            // Delay de 1.5s para o spawn do boss
            Invoke(nameof(TriggerDelayedSpawn), 1.5f);
        }
    }

    private void TriggerDelayedSpawn()
    {
        LevelTile tile = GetComponent<LevelTile>();
        if (tile != null)
        {
            tile.SpawnBoss();
        }
    }

    private void HandleBossDeath()
    {
        if (boss != null)
            boss.OnBossDied -= HandleBossDeath;

        // Verifica se o boss tem algum reward para entregar
        bool hasReward = boss != null && boss.Reward != BossData.AbilityReward.None;

        arenaBounds.RemoveBounds();
        ToggleWalls(false);

        if (UIManagerController.Instance != null)
        {
            if (UIManagerController.Instance.BossBar != null)
            {
                UIManagerController.Instance.BossBar.Hide();
                UIManagerController.Instance.ShowBossBar(false);
            }
            
            // Mostra painel de vitória quando boss morre, apenas se tiver reward
            if (hasReward)
            {
                UIManagerController.Instance.OnBossDefeated();
            }
                
        }
        
        // Retoma o fogo após 1 segundo
        Invoke(nameof(DeactivateArena), 1f);
    }

    private void DeactivateArena()
    {
        Debug.Log("[ARENA] Finalizada - Fogo retomado!");
        IsArenaActive = false;

        // Retoma a música de gameplay normal
        if (AudioManagerController.Instance != null)
        {
            AudioManagerController.Instance.PlayMusic(AudioManagerController.Instance.gameplayMusic);
        }
    }

    private void OnDrawGizmos()
    {
        BoxCollider2D collider = GetComponent<BoxCollider2D>();
        if (collider != null)
        {
            Gizmos.color = new Color(0, 1, 0, 0.3f);
            Gizmos.DrawCube(transform.position + (Vector3)collider.offset, collider.size);
        }
    }

    private Vector3 GetTileCenter()
    {
        return transform.position;
    }
}
