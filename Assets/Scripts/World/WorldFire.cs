using UnityEngine;

public class WorldFire : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 4.5f;

    void Update()
    {
        // Só se move se o jogo começou
        if (GameManager.Instance == null || !GameManager.Instance.GameStarted) return;

        // Pausa se uma arena estiver ativa
        if (BossArenaManager.IsArenaActive) 
        {
            // Debug.Log("[FOGO] Pausado pela Arena");
            return;
        }

        // Move para a direita (mantém Y e Z originais) em velocidade constante
        transform.Translate(Vector3.right * moveSpeed * Time.deltaTime);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log("Player hit by World Fire!");
            PlayerMovement player = other.GetComponent<PlayerMovement>();
            if (player != null)
            {
                player.Die();
                GameManager.Instance.EndGame();
            }            
        }
    }
}
