using UnityEngine;

public class GameDebug : MonoBehaviour
{
    public static GameDebug Instance;

    [Header("Fire Settings")]
    public bool spawnWorldFire = true;

    [Header("Tile Spawning Overrides")]
    public bool useDebugSpawning = false;
    public GameObject[] debugChunkPrefabs;
    public int debugChunkCount = 3;
    public bool forceOnlyDebugPrefabs = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}
