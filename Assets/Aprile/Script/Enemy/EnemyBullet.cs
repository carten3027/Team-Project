using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class EnemyBullet : MonoBehaviour
{
    [Header("미사일")]
    [SerializeField] private float speed = 5f;
    [Tooltip("유도 미사일이 초당 꺾는 각도 (작을수록 피하기 쉬움)")]
    [SerializeField] private float turnSpeed = 90f;
    [SerializeField] private float lifeTime = 6f;
    [Tooltip("발사 직후 적에게는 맞지 않는 시간(초)")]
    [SerializeField] private float armTime = 0.3f;
    [Tooltip("유도 미사일이 따라오는 시간(초). 이후엔 그 방향으로 직진")]
    [SerializeField] private float homingTime = 2f;

    [Header("폭탄 (비워두면 일반 미사일)")]
    [SerializeField] private EnemyBullet fragmentPrefab;
    [SerializeField] private int fragmentCount = 8;

    [Header("진단")]
    [Tooltip("켜면 미사일이 닿은 물체 이름을 Console에 표시")]
    [SerializeField] private bool logHits = true;

    private Rigidbody2D rb;
    private Vector3 baseScale;
    private Transform target;
    private int damage = 1;
    private bool homing;
    private float age;

    public float Speed => speed;
    public int Damage => damage;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        baseScale = transform.localScale;
    }

    public void Launch(Transform target, int damage, bool homing, float speedScale = 1f, float life = 0f)
    {
        this.target = target;
        this.damage = damage;
        this.homing = homing;
        speed *= speedScale;

        if (life > 0f) lifeTime = life;
    }

    void FixedUpdate()
    {
        age += Time.fixedDeltaTime;

        if (age >= lifeTime)
        {
            Explode();
            return;
        }

        if (homing && age < homingTime && target != null && target.gameObject.activeInHierarchy)
        {
            Vector2 d = (Vector2)target.position - rb.position;
            float targetAngle = Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg - 90f;
            rb.MoveRotation(Mathf.MoveTowardsAngle(rb.rotation, targetAngle, turnSpeed * Time.fixedDeltaTime));
        }

        if (fragmentPrefab != null)
        {
            transform.localScale = baseScale * (1f + Mathf.Sin(age / lifeTime * Mathf.PI) * 0.5f);
        }

        rb.linearVelocity = transform.up * speed;
    }

    void Explode()
    {
        if (fragmentPrefab != null)
        {
            for (int i = 0; i < fragmentCount; i++)
            {
                EnemyBullet fragment = Instantiate(fragmentPrefab, rb.position, Quaternion.Euler(0f, 0f, 360f / fragmentCount * i));
                fragment.Launch(target, damage, false);
            }
        }

        Destroy(gameObject);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (logHits)
        {
            Debug.Log($"[미사일] {other.name}에 닿음 (레이어: {LayerMask.LayerToName(other.gameObject.layer)})");
        }

        Player player = other.GetComponentInParent<Player>();
        if (player != null)
        {
            if (player.IsDead) return;

            player.TakeDamage(damage);
            Destroy(gameObject);
            return;
        }

        if (age <= armTime) return;

        Enemy enemy = other.GetComponentInParent<Enemy>();
        if (enemy != null)
        {
            enemy.TakeDamage(damage);
            Destroy(gameObject);
            return;
        }

        Boss boss = other.GetComponentInParent<Boss>();
        if (boss != null)
        {
            boss.TakeDamage(damage);
            Destroy(gameObject);
        }
    }
}