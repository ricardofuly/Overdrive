using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class UIManagerController : MonoBehaviour
{
    public static UIManagerController Instance;

    [Header("UI Panels")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private GameObject victoryPanel;
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private BossHealthBar bossBar;
    [SerializeField] private GameObject bossDistPanel;
    [SerializeField] private UnityEngine.UI.Slider playerHealthBar;
    [SerializeField] private UnityEngine.UI.Slider playerCurseBar;
    [SerializeField] private GameObject gameplayPanel;
    [SerializeField] private GameObject dashIcon;
    [SerializeField] private GameObject dashPanel;
    [SerializeField] private GameObject doubleJumpIcon;
    [SerializeField] private GameObject doubleJumpPanel;
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private UnityEngine.UI.Slider levelProgressBar;
    [SerializeField] private GameObject levelProgressPanel;
    [SerializeField] private TextMeshProUGUI bossDistanceText;

    public BossHealthBar BossBar => bossBar;

    private PlayerMovement player;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        // Garante o estado inicial das telas
        ShowPanel(mainMenuPanel);
    
        gameOverPanel.SetActive(false);
        victoryPanel.SetActive(false);
        pausePanel.SetActive(false);
        dashPanel.SetActive(false);
        doubleJumpPanel.SetActive(false);

        UpdateGameplayPanelVisibility();

        if (bossBar != null) bossBar.gameObject.SetActive(false);

        // Subscrito aos eventos do GameManager
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameStarted += HandleGameStarted;
            GameManager.Instance.OnGameOver += HandleGameOver;
            GameManager.Instance.OnGameWin += HandleGameWin;
            GameManager.Instance.OnTogglePause += HandleTogglePause;
        }

        // Subscrito à vida do player
        player = FindFirstObjectByType<PlayerMovement>();
        if (player != null)
        {
            player.OnHealthChanged += UpdatePlayerHealth;
            player.OnAbilityUnlocked += HandleAbilityUnlocked;

            // Estado inicial dos ícones
            if (dashIcon != null) dashIcon.SetActive(player.HasDash());
            if (doubleJumpIcon != null) doubleJumpIcon.SetActive(player.HasDoubleJump());
        }

        // Subscrito ao curse
        CurseSystem curse = FindFirstObjectByType<CurseSystem>();
        if (curse != null)
        {
            curse.OnCurseChanged += UpdatePlayerCurse;
        }
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameStarted -= HandleGameStarted;
            GameManager.Instance.OnGameOver -= HandleGameOver;
            GameManager.Instance.OnGameWin -= HandleGameWin;
            GameManager.Instance.OnTogglePause -= HandleTogglePause;
        }

        if (player != null)
        {
            player.OnHealthChanged -= UpdatePlayerHealth;
            player.OnAbilityUnlocked -= HandleAbilityUnlocked;
        }

        CurseSystem curse = FindFirstObjectByType<CurseSystem>();
        if (curse != null)
        {
            curse.OnCurseChanged -= UpdatePlayerCurse;
        }
    }

    #region Event Handlers
    private void HandleGameStarted()
    {
        mainMenuPanel.SetActive(false);
        UpdateGameplayPanelVisibility();
        Time.timeScale = 1f;
    }

    private void HandleGameOver()
    {
        gameOverPanel.SetActive(true);
        UpdateGameplayPanelVisibility();
    }

    private void HandleGameWin()
    {
        victoryPanel.SetActive(true);
        UpdateGameplayPanelVisibility();
    }

    private void HandleTogglePause(bool isPaused)
    {
        pausePanel.SetActive(isPaused);
        UpdateGameplayPanelVisibility();
    }

    private void UpdatePlayerHealth(int current, int max)
    {
        if (playerHealthBar != null)
        {
            playerHealthBar.maxValue = max;
            playerHealthBar.value = current;
        }
    }

    private void UpdatePlayerCurse(float current, float max)
    {
        if (playerCurseBar != null)
        {
            playerCurseBar.maxValue = max;
            playerCurseBar.value = current;
        }
    }

    private void HandleAbilityUnlocked(string abilityName)
    {
        if (abilityName == "Dash" && dashIcon != null)
        {
            dashIcon.SetActive(true);
            dashPanel.SetActive(true);
            Time.timeScale = 0f;
        } 

        if (abilityName == "DoubleJump" && doubleJumpIcon != null)
        {
            doubleJumpIcon.SetActive(true);
            doubleJumpPanel.SetActive(true);
            Time.timeScale = 0f;
        }
    }

    private void UpdateGameplayPanelVisibility()
    {
        if (gameplayPanel == null) return;

        bool anyOtherPanelActive = 
            (mainMenuPanel != null && mainMenuPanel.activeSelf) ||
            (gameOverPanel != null && gameOverPanel.activeSelf) ||
            (victoryPanel != null && victoryPanel.activeSelf) ||
            (pausePanel != null && pausePanel.activeSelf);

        gameplayPanel.SetActive(!anyOtherPanelActive);
    }

    private void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.GameStarted && !GameManager.Instance.GameOver)
        {
            UpdateTimerUI(GameManager.Instance.RunTime);
            UpdateLevelProgressUI();
            UpdateBossDistanceUI();
        }
    }

    private void UpdateLevelProgressUI()
    {
        if (levelProgressBar == null || ChunckLooper.Instance == null || GameManager.Instance == null || player == null) return;

        levelProgressBar.maxValue = ChunckLooper.Instance.LevelLength;
        levelProgressBar.value = player.GetDistanceTravelled();
        // Debug.Log("Level Progress: " + player.GetDistanceTravelled() + " / " + ChunckLooper.Instance.LevelLength);
    }

    private void UpdateBossDistanceUI()
    {
        if (bossDistanceText == null || ChunckLooper.Instance == null) return;

        float distance = ChunckLooper.Instance.GetDistanceToBoss();
        bossDistanceText.text = string.Format("{0:0}m", Mathf.Max(0, distance));
    }

    private void UpdateTimerUI(float time)
    {
        if (timerText == null) return;
        
        int minutes = Mathf.FloorToInt(time / 60);
        int seconds = Mathf.FloorToInt(time % 60);
        int centiseconds = Mathf.FloorToInt((time * 100) % 100);
        timerText.text = string.Format("{0:00}:{1:00}:{2:00}", minutes, seconds, centiseconds);
    }
    #endregion

    #region Public Methods for Buttons
    public void StartGame()
    {
        GameManager.Instance.StartGame();
        mainMenuPanel.SetActive(false);
        UpdateGameplayPanelVisibility();
    }

    public void ResumeGame()
    {
        GameManager.Instance.PauseGame(false);
    
        Time.timeScale = 1f;

        if (dashPanel != null) dashPanel.SetActive(false);
        if (doubleJumpPanel != null) doubleJumpPanel.SetActive(false);
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        Debug.Log("Game Restarted!");
    }

    public void QuitGame()
    {
        Application.Quit();
    }

    public void ShowBossBar(bool show)
    {
        if (bossBar != null)
        {
            bossBar.gameObject.SetActive(show);
            levelProgressPanel.SetActive(!show);
            bossDistPanel.SetActive(!show);
        }
    }

    public void OnBossDefeated()
    {
        // Pausa o jogo quando o boss é derrotado
        Time.timeScale = 0f;
        
        // Ativa um dos painéis de habilidade como feedback (mesmo que seja vazio)
        // Isso garante que o player veja uma feedback visual
        if (dashPanel != null)
        {
            dashPanel.SetActive(true);
        }
    }
    #endregion

    private void ShowPanel(GameObject panel)
    {
        if (panel != null) panel.SetActive(true);
    }
}