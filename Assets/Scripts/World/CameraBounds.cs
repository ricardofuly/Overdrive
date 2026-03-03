using UnityEngine;

public class CameraBounds : MonoBehaviour
{
    public static CameraBounds Instance;

    [SerializeField] private Transform player;
    [SerializeField] private Vector3 forcedScale = new Vector3(19.6741676f, 10.3150654f, 7.88549995f);
    [SerializeField] private float forcedY = -8.04f;
    [SerializeField] private float xOffset = 2f;
    [SerializeField] private float smoothSpeed = 5f;

    private float shakeTimer = 0f;
    private float shakeMagnitude = 0f;
    private Vector3 shakeOffset = Vector3.zero;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        // Posicionamento inicial sem suavização (snap)
        SnapToPosition();
    }

    void LateUpdate()
    {
        UpdateBoundsPosition();
    }

    private void SnapToPosition()
    {
        if (!GetPlayer()) return;
        if (ChunckLooper.Instance == null) return;

        float targetX = CalculateTargetX();
        transform.position = new Vector3(targetX, forcedY, transform.position.z);
        transform.localScale = forcedScale;
    }

    private void UpdateBoundsPosition()
    {
        if (!GetPlayer()) return;
        if (ChunckLooper.Instance == null) return;

        // Mantém a escala e altura solicitadas
        transform.localScale = forcedScale;

        HandleShake();

        float targetX = CalculateTargetX();
        
        // Movimentação suave apenas no eixo X
        float smoothedX = Mathf.Lerp(transform.position.x, targetX, smoothSpeed * Time.deltaTime);

        transform.position = new Vector3(smoothedX, forcedY, transform.position.z) + shakeOffset;
    }

    private void HandleShake()
    {
        if (shakeTimer > 0)
        {
            shakeOffset = Random.insideUnitSphere * shakeMagnitude;
            shakeOffset.z = 0; // Don't shake on Z for 2D
            shakeTimer -= Time.deltaTime;
        }
        else
        {
            shakeOffset = Vector3.zero;
        }
    }

    public void Shake(float duration, float magnitude)
    {
        shakeTimer = Mathf.Min(duration, 0.5f);
        shakeMagnitude = magnitude;
    }

    public void StopShake()
    {
        shakeTimer = 0f;
        shakeOffset = Vector3.zero;
    }

    private float CalculateTargetX()
    {
        float width = ChunckLooper.Instance.chunkWidth;
        if (width <= 0) return transform.position.x;

        int chunkIndex = Mathf.RoundToInt(player.position.x / width);
        return (chunkIndex * width) - xOffset;
    }

    private bool GetPlayer()
    {
        if (player != null) return true;
        
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) 
        {
            player = playerObj.transform;
            return true;
        }
        return false;
    }
}
