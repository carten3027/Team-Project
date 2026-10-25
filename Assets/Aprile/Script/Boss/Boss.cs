using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class Boss : MonoBehaviour
{
    public enum BossPattern
    {
        [InspectorName("유도 부채꼴 (3발 유도)")] HomingFan,
        [InspectorName("원형 탄막 (12방향)")] Ring,
        [InspectorName("나선 탄막")] Spiral,
        [InspectorName("예측 폭격 (폭탄 3개)")] Bombing,
        [InspectorName("유도 연사 (5발 유도)")] HomingBurst
    }

    public static event System.Action Defeated;

    [Header("능력치")]
    [SerializeField] private int maxHp = 15;
    [SerializeField] private float moveSpeed = 1.5f;
    [Tooltip("공격 간격(초)")]
    [SerializeField] private float fireInterval = 2.5f;
    [Tooltip("HP가 절반 이하일 때 공격 간격 배율 (작을수록 빨라짐)")]
    [SerializeField, Range(0.3f, 1f)] private float rageMultiplier = 0.6f;
    [Tooltip("미사일 공격력. Enemy의 Damage와 같게")]
    [SerializeField] private int damage = 1;

    [Header("패턴 (위에서부터 순서대로 반복, 유도 패턴 1개 이상 필수)")]
    [SerializeField]
    private BossPattern[] patterns =
    {
        BossPattern.HomingFan,
        BossPattern.Ring,
        BossPattern.HomingBurst
    };

    [Header("탄")]
    [SerializeField] private EnemyBullet missilePrefab;
    [Tooltip("예측 폭격 패턴에 쓰는 폭탄 (비우면 미사일 사용)")]
    [SerializeField] private EnemyBullet bombPrefab;

    [Header("처치 보상 (DataManager)")]
    [SerializeField] private int score = 1000;
    [SerializeField] private int money = 100;

    [Header("이동")]
    [Tooltip("화면 끝에서 이만큼 안쪽에서만 돌아다님")]
    [SerializeField] private float screenMargin = 1.5f;

    [Header("연결")]
    [SerializeField] private string playerTag = "Player";

    private const float LeadTime = 1f;

    private Rigidbody2D rb;
    private Camera cam;
    private Player target;
    private Vector2 lastPlayerPos;
    private Vector2 playerVelocity;
    private Vector2 wanderPoint;
    private int hp;
    private int patternIndex;
    private float fireTimer;
    private bool entered;
    private bool dead;

    public float HpRatio => (float)Mathf.Max(hp, 0) / maxHp;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        cam = Camera.main;
        hp = maxHp;
        fireTimer = fireInterval;
    }

    void FixedUpdate()
    {
        if (target == null) FindPlayer();

        if (target == null || target.IsDead)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        Vector2 playerPos = target.transform.position;
        playerVelocity = (playerPos - lastPlayerPos) / Time.fixedDeltaTime;
        lastPlayerPos = playerPos;

        rb.angularVelocity = 0f;
        rb.MoveRotation(AngleTo(playerPos));

        Move();
        UpdateFire(playerPos);
    }

    void FindPlayer()
    {
        GameObject found = GameObject.FindWithTag(playerTag);
        if (found == null || !found.TryGetComponent(out target)) return;

        lastPlayerPos = target.transform.position;
    }

    void Move()
    {
        if (!entered)
        {
            Vector2 center = cam.transform.position;
            rb.linearVelocity = (center - rb.position).normalized * moveSpeed * 2f;

            if (InScreen(rb.position))
            {
                entered = true;
                PickWanderPoint();
            }
            return;
        }

        if (Vector2.Distance(rb.position, wanderPoint) < 0.3f)
        {
            PickWanderPoint();
        }

        rb.linearVelocity = (wanderPoint - rb.position).normalized * moveSpeed;
    }

    void GetScreen(out Vector2 min, out Vector2 max)
    {
        min = (Vector2)cam.ViewportToWorldPoint(Vector3.zero) + Vector2.one * screenMargin;
        max = (Vector2)cam.ViewportToWorldPoint(Vector3.one) - Vector2.one * screenMargin;
    }

    bool InScreen(Vector2 p)
    {
        GetScreen(out Vector2 min, out Vector2 max);
        return p.x > min.x && p.x < max.x && p.y > min.y && p.y < max.y;
    }

    void PickWanderPoint()
    {
        GetScreen(out Vector2 min, out Vector2 max);
        wanderPoint = new Vector2(Random.Range(min.x, max.x), Random.Range(min.y, max.y));
    }

    void UpdateFire(Vector2 playerPos)
    {
        if (!entered || patterns.Length == 0) return;

        fireTimer -= Time.fixedDeltaTime;
        if (fireTimer > 0f) return;

        Fire(playerPos, patterns[patternIndex % patterns.Length]);
        patternIndex++;

        bool rage = hp <= maxHp / 2;
        fireTimer = fireInterval * (rage ? rageMultiplier : 1f);
    }

    void Fire(Vector2 playerPos, BossPattern pattern)
    {
        float aim = AngleTo(playerPos);

        switch (pattern)
        {
            case BossPattern.HomingFan:
                for (int i = -1; i <= 1; i++)
                {
                    Shoot(missilePrefab, aim + i * 30f, true);
                }
                break;

            case BossPattern.Ring:
                for (int i = 0; i < 12; i++)
                {
                    Shoot(missilePrefab, aim + i * 30f, false);
                }
                break;

            case BossPattern.Spiral:
                StartCoroutine(SpiralRoutine(aim));
                break;

            case BossPattern.Bombing:
                EnemyBullet bomb = bombPrefab != null ? bombPrefab : missilePrefab;
                Vector2 landing = playerPos + playerVelocity * LeadTime;
                Vector2 toLanding = landing - rb.position;
                Vector2 side = new Vector2(-toLanding.y, toLanding.x).normalized * 2f;

                for (int i = -1; i <= 1; i++)
                {
                    Vector2 point = landing + side * i;
                    float fuse = Mathf.Max(Vector2.Distance(rb.position, point) / bomb.Speed, 0.5f);
                    Shoot(bomb, AngleTo(point), false, 1f, fuse);
                }
                break;

            case BossPattern.HomingBurst:
                StartCoroutine(HomingBurstRoutine());
                break;
        }
    }

    IEnumerator SpiralRoutine(float startAngle)
    {
        for (int i = 0; i < 24; i++)
        {
            float angle = startAngle + i * 25f;
            Shoot(missilePrefab, angle, false);
            Shoot(missilePrefab, angle + 180f, false);
            yield return new WaitForSeconds(0.05f);
        }
    }

    IEnumerator HomingBurstRoutine()
    {
        for (int i = 0; i < 5; i++)
        {
            if (target == null || target.IsDead) yield break;

            Shoot(missilePrefab, AngleTo(target.transform.position), true);
            yield return new WaitForSeconds(0.2f);
        }
    }

    void Shoot(EnemyBullet prefab, float angle, bool homing, float speedScale = 1f, float life = 0f)
    {
        EnemyBullet bullet = Instantiate(prefab, rb.position, Quaternion.Euler(0f, 0f, angle));
        bullet.Launch(target.transform, damage, homing, speedScale, life);
    }

    float AngleTo(Vector2 point)
    {
        Vector2 d = point - rb.position;
        return Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg - 90f;
    }

    public void TakeDamage(int amount)
    {
        if (dead) return;

        hp -= amount;
        Debug.Log($"보스 HP {Mathf.Max(hp, 0)}/{maxHp}");

        if (hp <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        dead = true;

        if (DataManager.Instance != null)
        {
            DataManager.Instance.currentScore += score;
            DataManager.Instance.money += money;
        }

        Debug.Log("보스 처치");
        Defeated?.Invoke();
        Destroy(gameObject);
    }
}