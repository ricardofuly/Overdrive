using UnityEngine;
using UnityEngine.Events;
using System;

public class CurseSystem : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerMovement player;

    [Header("Curse Settings")]
    [SerializeField] private float maxCurse = 100f;
    [SerializeField] private float curseIncreaseRate = 40f;
    [SerializeField] private float curseDecreaseRate = 25f;
    [SerializeField] private float minimumSpeedThreshold = 1f;
    [SerializeField] private float curseTimerDuration = 1f;

    private float curseTimer = 0f;
    private bool isHit = false;

    [Header("Events")]
    public UnityEvent OnCurseMaxed;
    public event Action<float, float> OnCurseChanged;

    private float currentCurse;

    private void Start()
    {
        OnCurseChanged?.Invoke(currentCurse, maxCurse);
    }

    private void Update()
    {
        if (!isHit)
        {
            HandleCurse();
        }

        if (curseTimer > 0f)
        {
            curseTimer -= Time.deltaTime;

            if (curseTimer <= 0f)
            {
                curseTimer = 0f;
                isHit = false;
            }
        }
    }

    private void HandleCurse()
    {
        float currentSpeed = player.GetCurrentSpeed();

        if (currentSpeed < minimumSpeedThreshold)
        {
            currentCurse += curseIncreaseRate * Time.deltaTime;
        }
        else
        {
            currentCurse -= curseDecreaseRate * Time.deltaTime;
        }

        currentCurse = Mathf.Clamp(currentCurse, 0f, maxCurse);
        OnCurseChanged?.Invoke(currentCurse, maxCurse);

        if (currentCurse >= maxCurse)
        {
            OnCurseMaxed?.Invoke();
        }
    }

    public void IncreaseCurseRate(float amount)
    {
        currentCurse += amount;
        currentCurse = Mathf.Clamp(currentCurse, 0f, maxCurse);
        OnCurseChanged?.Invoke(currentCurse, maxCurse);

        if (currentCurse >= maxCurse)
        {
            OnCurseMaxed?.Invoke();
        }

        curseTimer = curseTimerDuration;
        isHit = true;
    }

    public float GetCurseNormalized()
    {
        return currentCurse / maxCurse;
    }

    public float GetCurseDuation()
    {
        return curseTimer;
    }
}
