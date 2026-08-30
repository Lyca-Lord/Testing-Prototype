using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class MyCameraController : MonoBehaviour
{
    public static MyCameraController Instance;

    [Header("Parameter")]
    [SerializeField] private float offsetY = 1;
    [SerializeField] private float vel = 0;
    [SerializeField] private float friction = 0.25f;
    [SerializeField] private bool isMoving = false;

    [Header("Component")]
    private Rigidbody2D rb;

    public Vector2 target = Vector2.zero;
    public float velocity = 0;
    public float acceleration = 0;

    private void Awake()
    {
        Instance = this;
    }

    private void OnValidate()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    public void GetDown()
    {
        isMoving = true;
        target = new Vector2(transform.position.x, -offsetY);
        velocity = 0;
    }

    public void GetUp()
    {
        isMoving = true;
        target = Vector2.zero;
        velocity = 0;
    }

    public void Update()
    {
        if (isMoving == false) return;

        acceleration = Vector2.Distance(transform.position, target) * Vector2.Distance(transform.position, target) * vel;
        velocity += (target - (Vector2)transform.position).y * acceleration * vel;

        if (velocity > 0) velocity = Mathf.Clamp(velocity - friction, 0, 10000);
        else velocity = Mathf.Clamp(velocity + friction, -10000, 0);

        transform.position = new Vector3(transform.position.x, transform.position.y + velocity * Time.deltaTime, transform.position.z);

        if (Mathf.Abs(velocity) < 0.001)
        {
            isMoving = false;
            return;
        }
    }
}
