using UnityEngine;

public class PetrifiedVisualController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private SpriteRenderer mainRenderer;
    [SerializeField] private SpriteRenderer petrifiedRenderer;
    [SerializeField] private Transform maskTransform;

    [Header("Settings")]
    [SerializeField] private float maxMaskHeight = 1.5f;

    private void LateUpdate()
    {
        SyncSprite();
    }

    public void UpdateCurseVisual(float normalizedCurse)
    {
        UpdateMask(normalizedCurse);
    }

    private void SyncSprite()
    {
        petrifiedRenderer.sprite = mainRenderer.sprite;
        petrifiedRenderer.flipX = mainRenderer.flipX;
    }

    private void UpdateMask(float normalized)
    {
        Vector3 scale = maskTransform.localScale;
        scale.y = Mathf.Lerp(0f, maxMaskHeight, normalized);
        maskTransform.localScale = scale;
    }
}
