using UnityEngine;

[DefaultExecutionOrder(-100)]
[RequireComponent(typeof(Rigidbody2D))]
public class MovingPlatform : MonoBehaviour
{
    public float distance = 3f;
    private Rigidbody2D body;
    private Vector2 startPosition;
    private float startTime;
    public Vector2 Velocity { get; private set; }

    void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        startPosition = body.position;
        startTime = Time.fixedTime;
    }

    void FixedUpdate()
    {
        Vector2 destination = startPosition + Vector2.right * Mathf.PingPong(Time.fixedTime - startTime, distance);
        Velocity = (destination - body.position) / Time.fixedDeltaTime;
        body.MovePosition(destination);
    }
}
