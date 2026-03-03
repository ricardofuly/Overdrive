using UnityEngine;
using Action = System.Action;
using System.Collections.Generic;

public class LevelTile : MonoBehaviour
{
    [Header("Spawn Settings")]
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private int minEnemies = 0;
    [SerializeField] private int maxEnemies = 3;
    [SerializeField] private float minDistanceBetweenEnemies = 3f;
    [SerializeField] private int maxSpawnAttempts = 15;
    [SerializeField] private Collider2D tileBounds;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Transform anchorTransform;
    [SerializeField] private LayerMask groundLayer;

    [Header("Manual Spawning")]
    public Transform[] spawnPoints;

    [Header("Boss Settings")]
    [SerializeField] private bool isBossTile = false;
    [SerializeField] private GameObject[] bossPrefabs;
    [SerializeField] private Transform bossSpawnPoint;

    private GameObject selectedBossPrefab;
    private GameObject spawnedBossObject;
    private BossData spawnedBossData;

    public event Action OnTileDestroyed;

    private void Start()
    {
        SpawnEnemies();
        
        // Se for tile de boss, instancia o boss imediatamente mas mantem oculto
        
    }

    void FixedUpdate()
    {
        if (isBossTile)
        {
            PreSpawnBoss();
        }
    }

    private void SpawnEnemies()
    {
        // Se for tile de Boss, ele não spawna inimigos normais
        if (isBossTile) return;

        // Para inimigos normais, o jogo precisa ter começado
        if (!GameManager.Instance.GameStarted || enemyPrefab == null) return;

        // Opção 1: Pontos de Spawn Manuais (Se houver algum definido no Inspector)
        if (spawnPoints != null && spawnPoints.Length > 0)
        {
            SpawnFromPoints();
        }
        else
        {
            // Opção 2: Spawn Aleatório Inteligente (Raycasting para achar o chão)
            SpawnRandomSmart();
        }
    }
    
    private void PreSpawnBoss()
    {
        // Instancia o boss quando a sala é criada, mas mantem oculto
        if (bossSpawnPoint == null || bossPrefabs == null || bossPrefabs.Length == 0) return;

        int randomIndex = Random.Range(0, bossPrefabs.Length);
        selectedBossPrefab = bossPrefabs[randomIndex];

        if (selectedBossPrefab != null)
        {
            // Instancia o boss e guarda referencia ao objeto instanciado
            spawnedBossObject = Instantiate(selectedBossPrefab, bossSpawnPoint.position, Quaternion.identity);
            spawnedBossObject.transform.parent = transform;
            
            // Mantem o boss oculto ate a arena ser ativada
            spawnedBossObject.SetActive(false);
            
            spawnedBossData = spawnedBossObject.GetComponent<BossData>();
        }
    }

    public void SpawnBoss()
    {
        BossArenaManager arena = GetComponent<BossArenaManager>();

        // Se o boss ja foi instanciado, apenas ativa ele
        if (spawnedBossObject != null && spawnedBossData != null && arena != null)
        {
            // Ativa o boss PRIMEIRO, antes de chamar SetBoss
            spawnedBossObject.SetActive(true);
            
            // Passa o boss para o arena manager (agora arenaStarted ja deve ser true)
            arena.SetBoss(spawnedBossData);
        }
        else
        {
            // Fallback: metodo original se algo deu errado
            SpawnBossOriginal();
        }
    }
    
    private void SpawnBossOriginal()
    {
        // Metodo original mantido para compatibilidade
        if (bossSpawnPoint == null || bossPrefabs == null || bossPrefabs.Length == 0) return;

        int randomIndex = Random.Range(0, bossPrefabs.Length);
        selectedBossPrefab = bossPrefabs[randomIndex];

        if (selectedBossPrefab != null)
        {
            spawnedBossObject = Instantiate(selectedBossPrefab, bossSpawnPoint.position, Quaternion.identity);
            spawnedBossObject.transform.parent = transform;

            spawnedBossData = spawnedBossObject.GetComponent<BossData>();
            BossArenaManager arena = GetComponent<BossArenaManager>();

            if (arena != null && spawnedBossData != null)
            {
                arena.SetBoss(spawnedBossData);
            }
        }
    }

    private void SpawnFromPoints()
    {
        // Embaralha ou decide quantos spawnar dos pontos
        int enemyCount = Random.Range(minEnemies, Mathf.Min(maxEnemies + 1, spawnPoints.Length + 1));
        
        List<Transform> availablePoints = new List<Transform>(spawnPoints);
        
        for (int i = 0; i < enemyCount && availablePoints.Count > 0; i++)
        {
            int randomIndex = Random.Range(0, availablePoints.Count);
            Transform pt = availablePoints[randomIndex];
            
            CreateEnemy(pt.position);
            availablePoints.RemoveAt(randomIndex);
        }
    }

    private void SpawnRandomSmart()
    {
        int enemyCount = Random.Range(minEnemies, maxEnemies + 1);
        Bounds bounds = tileBounds.bounds;
        List<Vector3> usedPositions = new List<Vector3>();

        for (int i = 0; i < enemyCount; i++)
        {
            bool spawned = false;

            for (int attempt = 0; attempt < maxSpawnAttempts; attempt++)
            {
                // Escolhe um X aleatorio
                float randomX = Random.Range(bounds.min.x + 1f, bounds.max.x - 1f);
                
                // Raycast de cima para baixo para achar o chao
                // Comeca de um ponto alto (ex: 5 unidades acima do centro do tile)
                Vector2 rayOrigin = new Vector2(randomX, bounds.center.y + 5f);
                RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.down, 15f, groundLayer);

                if (hit.collider != null)
                {
                    Vector3 spawnPos = hit.point;
                    spawnPos.y += 0.5f; // Sobe um pouquinho do chao para nao enterrar os pes

                    // Verifica se o espaco esta livre (nao dentro de uma parede)
                    // Usa um Circle pequeno (0.4f) para ver se ha obstaculos
                    if (!Physics2D.OverlapCircle(spawnPos, 0.4f, groundLayer))
                    {
                        // Verifica distancia de outros inimigos
                        bool tooClose = false;
                        foreach (var pos in usedPositions)
                        {
                            if (Vector3.Distance(pos, spawnPos) < minDistanceBetweenEnemies)
                            {
                                tooClose = true;
                                break;
                            }
                        }

                        if (!tooClose)
                        {
                            CreateEnemy(spawnPos);
                            usedPositions.Add(spawnPos);
                            spawned = true;
                            break;
                        }
                    }
                }
            }

            if (!spawned) continue; 
        }
    }

    private void CreateEnemy(Vector3 position)
    {
        GameObject enemy = Instantiate(enemyPrefab, position, Quaternion.identity);
        EnemyData enemyData = enemy.GetComponent<EnemyData>();
        
        if (enemyData != null)
        {
            enemyData.Initialize(this);
        }
        
        enemy.transform.parent = transform;
    }

    public float GetWidth()
    {
        return spriteRenderer != null ? spriteRenderer.bounds.size.x : 20f;
    }

    public float GetHeight()
    {
        return anchorTransform != null ? anchorTransform.position.y : 0f;
    }

    public Bounds GetBounds()
    {
        return tileBounds.bounds;
    }

    void OnDestroy()
    {
        OnTileDestroyed?.Invoke();
    }
}
