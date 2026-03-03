using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class ChunckLooper : MonoBehaviour
{
    [Header("Setup")]
    [SerializeField] private Transform player;
    [SerializeField] private GameObject[] chunkPrefabs;
    [SerializeField] private GameObject startRoomPrefab;
    [SerializeField] private GameObject endRoomPrefab;
    [SerializeField] private GameObject bossTilePrefab;
    [SerializeField] private int bossTileInterval = 10;
    [SerializeField] private float spawnTriggerDistance = 15f;
    [SerializeField] private float levelLength = 500f;
    public float LevelLength => levelLength;
    [SerializeField] private int initialChunks = 3;

    [Header("Camera")]
    public static float LevelMinX;
    public static float LevelMaxX;
    
    public static ChunckLooper Instance;
    private Queue<GameObject> activeChunks = new Queue<GameObject>();

    public static bool endSpawned = false;
    private int tilesSinceLastBoss = 0;
    private float nextSpawnX = 0f;
    public float chunkWidth;
    private int lastChunkIndex = -1;

    void Awake()
    {
        Instance = this;

        chunkWidth = chunkPrefabs[0].GetComponent<LevelTile>().GetWidth();

        nextSpawnX = 0f;

        SpawnStartRoom();
        tilesSinceLastBoss = 1; 
       
        for (int i = 0; i < initialChunks - 1; i++)
        {
            SpawnChunk();
        }
    }

    private void SpawnStartRoom()
    {
        float height = startRoomPrefab.GetComponent<LevelTile>().GetHeight();

        GameObject startRoom = Instantiate(
            startRoomPrefab,
            new Vector3(nextSpawnX, height, 0f),
            Quaternion.identity
        );

        LevelMinX = startRoom.transform.position.x - chunkWidth / 2f;
        activeChunks.Enqueue(startRoom);
        nextSpawnX += chunkWidth;
    }

    private void Update()
    {
        if (activeChunks.Count > initialChunks)
        {
            GameObject oldestChunk = activeChunks.Peek();
            if (player.position.x > oldestChunk.transform.position.x + (chunkWidth * 2f))
            {
                Destroy(oldestChunk);
                activeChunks.Dequeue();
            }
        }

        if(endSpawned) return;
        
        if (player.position.x + spawnTriggerDistance > nextSpawnX - chunkWidth)
        {
            SpawnChunk();
        }
    }

    private void SpawnChunk()
    {
        GameObject newChunk;
        tilesSinceLastBoss++;

        if (tilesSinceLastBoss >= bossTileInterval)
        {
            float height = bossTilePrefab.GetComponent<LevelTile>().GetHeight();
            newChunk = Instantiate(bossTilePrefab, new Vector3(nextSpawnX, height, 0f), Quaternion.identity);
            tilesSinceLastBoss = 0;
        }
        else if (GameManager.Instance.DistanceTraveled >= levelLength)
        {
            float height = endRoomPrefab.GetComponent<LevelTile>().GetHeight();
            newChunk = Instantiate(endRoomPrefab, new Vector3(nextSpawnX, height, 0f), Quaternion.identity);
            LevelMaxX = nextSpawnX - chunkWidth / 2f;
            endSpawned = true;
        }
        else
        {
            GameObject prefab = GetRandomChunk();
            float height = prefab.GetComponent<LevelTile>().GetHeight();
            newChunk = Instantiate(prefab, new Vector3(nextSpawnX, height, 0f), Quaternion.identity);
        }

        activeChunks.Enqueue(newChunk);
        nextSpawnX += chunkWidth;
    }

    public float GetDistanceToBoss()
    {
        float bossX;
        if (tilesSinceLastBoss == 0) bossX = nextSpawnX - chunkWidth;
        else bossX = nextSpawnX + (bossTileInterval - tilesSinceLastBoss - 1) * chunkWidth;
        return bossX - player.position.x;
    }

    private GameObject GetRandomChunk()
    {
        int randomIndex;
        do
        {
            randomIndex = Random.Range(0, chunkPrefabs.Length);
        }
        while (randomIndex == lastChunkIndex && chunkPrefabs.Length > 1);
        lastChunkIndex = randomIndex;
        return chunkPrefabs[randomIndex];
    }
}