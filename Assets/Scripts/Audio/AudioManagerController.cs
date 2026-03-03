using UnityEngine;
using System.Collections;

public class AudioManagerController : MonoBehaviour
{
    public static AudioManagerController Instance;

    [Header("Audio Sources")]
    [SerializeField] private AudioSource sourceA;
    [SerializeField] private AudioSource sourceB;

    [Header("Music Clips")]
    [SerializeField] private AudioClip menuMusic;
    public AudioClip gameplayMusic; // Exposto para retorno pós-boss
    [SerializeField] private AudioClip victoryMusic;
    [SerializeField] private AudioClip gameOverMusic;

    [Header("SFX Clips")]
    public AudioClip playerHitEnemySFX;
    public AudioClip playerHealSFX;
    public AudioClip enemyHitPlayerSFX;
    public AudioClip bossHitPlayerSFX;
    public AudioClip bossLandSFX;
    public AudioClip playerDashSFX;
    public AudioClip bossDashSFX;

    [Header("Settings")]
    [SerializeField] private float fadeDuration = 1.0f;
    [SerializeField] private float maxVolume = 0.5f;

    private AudioSource activeSource;
    private Coroutine fadeCoroutine;

    private void Awake()
    {
        Instance = this;
        activeSource = sourceA;
    }

    private void Start()
    {
        // Subscreve aos eventos do GameManager
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameStarted += HandleGameStarted;
            GameManager.Instance.OnGameOver += HandleGameOver;
            GameManager.Instance.OnGameWin += HandleGameWin;
        }

        // Toca a música inicial do menu se o jogo não começou
        if (GameManager.Instance != null && !GameManager.Instance.GameStarted)
        {
            PlayMusic(menuMusic);
        }
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameStarted -= HandleGameStarted;
            GameManager.Instance.OnGameOver -= HandleGameOver;
            GameManager.Instance.OnGameWin -= HandleGameWin;
        }
    }

    private void HandleGameStarted() => PlayMusic(gameplayMusic);
    private void HandleGameOver() => PlayMusic(gameOverMusic);
    private void HandleGameWin() => PlayMusic(victoryMusic);

    public void PlayMusic(AudioClip clip)
    {
        if (clip == null || (activeSource != null && activeSource.clip == clip)) return;
        StartCrossFade(clip);
    }

    private void StartCrossFade(AudioClip newClip)
    {
        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
        fadeCoroutine = StartCoroutine(CrossFade(newClip));
    }

    private IEnumerator CrossFade(AudioClip nextClip)
    {
        AudioSource nextSource = (activeSource == sourceA) ? sourceB : sourceA;

        nextSource.clip = nextClip;
        nextSource.volume = 0;
        nextSource.Play();

        float timer = 0;
        float startVolume = activeSource.volume;

        while (timer < fadeDuration)
        {
            timer += Time.unscaledDeltaTime;
            float t = timer / fadeDuration;

            activeSource.volume = Mathf.Lerp(startVolume, 0, t);
            nextSource.volume = Mathf.Lerp(0, maxVolume, t);

            yield return null;
        }

        activeSource.Stop();
        activeSource.volume = 0;
        nextSource.volume = maxVolume;

        activeSource = nextSource;
        fadeCoroutine = null;
    }

    public void PlaySFX(AudioClip clip, float volume = 1f)
    {
        if (clip == null) return;
        // Usa a source ativa ou cria uma temporária para SFX se preferir, 
        // mas aqui vamos usar PlayOneShot na source ativa para simplicidade
        activeSource.PlayOneShot(clip, volume);
    }

    public void PlaySFXRandomPitch(AudioClip clip, float volume = 1f, float pitchRange = 0.1f)
    {
        if (clip == null) return;
        
        // Para pitch variado, o ideal é uma source dedicada ou temporária
        // Vamos usar a SourceB para SFX rápidos se não estiver em fade, ou apenas PlayOneShot
        activeSource.pitch = Random.Range(1f - pitchRange, 1f + pitchRange);
        activeSource.PlayOneShot(clip, volume);
        activeSource.pitch = 1f; // Reseta pitch
    }
}