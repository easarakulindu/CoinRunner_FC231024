using UnityEngine;
 
public class PlayerInteractions : MonoBehaviour
{
    public float fallLimitY = -10f;        // fall below this = respawn
 
    private Vector3 startPosition;
    private Rigidbody2D rb;
 
    void Start()
    {
        startPosition = transform.position;    // remember where we started
        rb = GetComponent<Rigidbody2D>();
    }
 
    void Update()
    {
        if (rb.position.y < fallLimitY)
        {
            Respawn();
        }
    }
 
    // TRIGGER = something we pass THROUGH (coins)
    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Coin"))
        {
            GameManager.instance.AddCoin();
            Destroy(other.gameObject);
        }
        else if (other.CompareTag("Goal"))
        {
            GameManager.instance.LevelComplete();
        }
    }
 
    // COLLISION = something SOLID we bump into (spikes)
    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Hazard"))
        {
            Respawn();
        }
    }
 
    void Respawn()
    {
        rb.position = startPosition;
        GetComponent<PlayerMovement>().ResetMotion();
    }
}
