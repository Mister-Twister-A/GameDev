using UnityEngine;

public class TestEnemy : EnemyData
{
    public float speed = 3f;
    public float walkDistance = 5f;

    private Vector3 startPosition;
    private bool movingRight = true;

    void Start()
    {
        startPosition = transform.position; 
    }

    void Update()
    {
        Behaviour(); 
        if (health <= 0)
        {
            OnDeath();
        }
    }

    public override void Behaviour()
    {
        float rightLimit = startPosition.x + walkDistance;
        float leftLimit = startPosition.x - walkDistance;

        if (movingRight)
        {
            transform.Translate(Vector2.right * speed * Time.deltaTime);
            if (transform.position.x >= rightLimit)
            {
                movingRight = false;
                Flip();
            }
        }
        else
        {
            transform.Translate(Vector2.left * speed * Time.deltaTime);
            if (transform.position.x <= leftLimit)
            {
                movingRight = true;
                Flip();
            }
        }
    }

    public override void OnDeath()
    {
        Destroy(gameObject);
    }

    private void Flip()
    {
        Vector3 scale = transform.localScale;
        scale.x *= -1;
        transform.localScale = scale;
    }
}
