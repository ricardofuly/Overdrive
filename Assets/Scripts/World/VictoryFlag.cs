using UnityEngine;

public class VictoryFlag : MonoBehaviour
{
    private bool activated = false;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (activated) return;

        if (other.CompareTag("Player"))
        {
            activated = true;
            Debug.Log("[VICTORY] Player tocou na bandeira!");
            GameManager.Instance.WinGame();
        }
    }
}