using UnityEngine;
using TMPro;

public class FPSDisplay : MonoBehaviour
{
    public TextMeshProUGUI fpsText;
    public float pollingTime = 0.5f;

    private float timeUntilNextUpdate;
    private int frameCount;
    private int currentFPS;

    private void Start()
    {
        timeUntilNextUpdate = pollingTime;
    }

    private void Update()
    {
        // Measure average frames per second
        frameCount++;
        timeUntilNextUpdate -= Time.unscaledDeltaTime; // Use unscaledDeltaTime

        if (timeUntilNextUpdate <= 0)
        {
            currentFPS = (int)(frameCount / pollingTime);
            fpsText.text = string.Format("{0} FPS", currentFPS);

            frameCount = 0;
            timeUntilNextUpdate = pollingTime;
        }
    }
}
