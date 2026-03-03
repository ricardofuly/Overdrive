using System;
using UnityEngine;
using UnityEngine.InputSystem;


public class GameManager : MonoBehaviour
{
    public static GameManager Instance;
    private InputSystem_Actions inputActions;

    private float runTime;
    public float RunTime => runTime;

    [Header("Performance")]
    [SerializeField] private int targetFrameRate = 60;

    [Header("Player")]
    [SerializeField] private Transform player;
    [SerializeField] private float maxDistance = 1000f;

    [Header("World Fire")]
    [SerializeField] private GameObject worldFirePrefab;
    [SerializeField] private float fireStartXOffset = 5f;
    [SerializeField] private float fireForcedY = -12.14f;

    private float startX;
    private PlayerMovement playerMovement;
    public float DistanceTraveled { get; private set; }
    public float MaxDistance => maxDistance;


    public bool GameStarted { get; private set; }
    public bool GameOver { get; private set; }
    public bool IsPaused { get; private set; }

    public event Action OnGameStarted;
    public event Action OnGameOver;
    public event Action OnGameWin;
    public event Action<bool> OnTogglePause;
    
    private void Awake()
    {
        Instance = this;

        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = targetFrameRate;

        inputActions = new InputSystem_Actions();
    }

    void Start()
    {
        startX = player.position.x;
        if (player != null)
        {
            playerMovement = player.GetComponent<PlayerMovement>();
        }
    }

    void Update()
    {
        if(!GameStarted) return;

        if (playerMovement != null)
        {
            DistanceTraveled = playerMovement.GetDistanceTravelled();
        }
        else
        {
            DistanceTraveled = player.position.x - startX;
        }

        runTime += Time.deltaTime;

        if(DistanceTraveled >= maxDistance)
        {
            WinGame();
        }
    }

    

    private void OnEnable()
    {
        inputActions.Enable();

        inputActions.UI.StartGame.performed += OnStartPressed;
        inputActions.Player.Pause.performed += OnPausePressed;
    }

    private void OnDisable()
    {
        inputActions.UI.StartGame.performed -= OnStartPressed;
        inputActions.Player.Pause.performed -= OnPausePressed;
        
        inputActions.Disable();
    }

    private void OnStartPressed(InputAction.CallbackContext context)
    {
        if (GameStarted) return;

        StartGame();
    }

    private void OnPausePressed(InputAction.CallbackContext context)
    {
        if (!GameStarted || GameOver) return;
        TogglePause();
    }


    #region Game States
    public void StartGame()
    {
        if (GameStarted) return;

        GameStarted = true;
        Debug.Log("Game Started!");
        OnGameStarted?.Invoke();

        if (worldFirePrefab != null)
        {
            float fireX = -fireStartXOffset;
            if (ChunckLooper.Instance != null)
            {
                fireX = ChunckLooper.LevelMinX - fireStartXOffset;
            }

            Debug.Log($"[FOGO] Spawnando fogo em X: {fireX}");
            Instantiate(worldFirePrefab, new Vector3(fireX, fireForcedY, 0), Quaternion.identity);
        }
    }

    public void WinGame()
    {
        Debug.Log("YOU WIN!");
        OnGameWin?.Invoke();
        Time.timeScale = 0f;
    }

    public void EndGame()
    {
        GameOver = true;
        //Time.timeScale = 0f;
        Debug.Log("Game Over!");
        OnGameOver?.Invoke();
    }

    public void TogglePause()
    {
        PauseGame(!IsPaused);
    }

    public void PauseGame(bool pause)
    {
        IsPaused = pause;
        Time.timeScale = pause ? 0f : 1f;
        OnTogglePause?.Invoke(IsPaused);
    }

    #endregion
}