//using System.Collections;
//using UnityEngine;

//[RequireComponent(typeof(Rigidbody2D))]
//public class Enemy : MonoBehaviour
//{
//    public enum EnemyType
//    {
//        [InspectorName("정찰기 - 단발 유도")] Scout,
//        [InspectorName("전투기 - 3연발 유도")] Fighter,
//        [InspectorName("중장갑함 - 산탄 + 유도")] Heavy,
//        [InspectorName("요격기 - 직선 고속탄")] Interceptor,
//        [InspectorName("폭격기 - 곡사 폭탄")] Bomber
//    }

//    public static int KillCount;

//    [Header("종류")]
//    [SerializeField] private EnemyType type;

//    [Header("능력치")]
//    [SerializeField] private int maxHp = 2;
//    [SerializeField] private float moveSpeed = 3f;
//    [Tooltip("플레이어와 이 거리를 유지하며 주위를 돎")]
//    [SerializeField] private float keepDistance = 5f;
//    [Tooltip("공격 간격(초)")]
//    [SerializeField] private float fireInterval = 2f;
//    [Tooltip("미사일 공격력. 플레이어·적 누구에게 맞아도 이만큼 깎임")]
//    [SerializeField] private int damage = 1;

//    [Header("탄")]
//    [SerializeField] private EnemyBullet missilePrefab;
//    [Tooltip("폭격기만 사용 (비우면 미사일 사용)")]
//    [SerializeField] private EnemyBullet bombPrefab;

//    [Header("처치 보상 (DataManager)")]
//    [SerializeField] private int score = 100;
//    [SerializeField] private int money = 10;

//    [Header("화면 밖으로 밀려났을 때")]
//    [SerializeField] private float returnSpeed = 6f;

//    [Header("연결")]
//    [SerializeField] private string playerTag = "Player";

//    private const float LeadTime = 1f;
//    private const float ReturnTime = 0.5f;

//    private Rigidbody2D rb;
//    private Camera cam;
//    private Player target;
//    private Vector2 lastPlayerPos;
//    private Vector2 playerVelocity;
//    private int hp;
//    private float fireTimer;
//    private float pushTimer;
//    private float orbitDir;
//    private bool entered;
//    private bool dead;

//    void Awake()
//    {
//        rb = GetComponent<Rigidbody2D>();
//        cam = Camera.main;
//        hp = maxHp;
//        fireTimer = Random.Range(0.5f, fireInterval);
//        orbitDir = Random.value < 0.5f ? 1f : -1f;
//    }

//    void FixedUpdate()
//    {
//        if (target == null) FindPlayer();

//        if (target == null || target.IsDead)
//        {
//            rb.linearVelocity = Vector2.zero;
//            return;
//        }

//        Vector2 playerPos = target.transform.position;
//        playerVelocity = (playerPos - lastPlayerPos) / Time.fixedDeltaTime;
//        lastPlayerPos = playerPos;

//        rb.angularVelocity = 0f;
//        rb.MoveRotation(AngleTo(playerPos));

//        CheckScreen();

//        if (pushTimer > 0f)
//        {
//            pushTimer -= Time.fixedDeltaTime;
//            return;
//        }

//        Move(playerPos);
//        UpdateFire(playerPos);
//    }

//    void FindPlayer()
//    {
//        GameObject found = GameObject.FindWithTag(playerTag);
//        if (found == null || !found.TryGetComponent(out target)) return;

//        lastPlayerPos = target.transform.position;
//    }

//    void Move(Vector2 playerPos)
//    {
//        Vector2 toPlayer = playerPos - rb.position;
//        Vector2 dir = toPlayer.normalized;
//        Vector2 side = new Vector2(-dir.y, dir.x) * orbitDir;

//        float approach = Mathf.Clamp01(toPlayer.magnitude - keepDistance);
//        Vector2 move = dir * approach + side * 0.3f;

//        if (type == EnemyType.Interceptor)
//        {
//            move += side * Mathf.Sin(Time.time * 5f) * 0.7f;
//        }

//        rb.linearVelocity = Vector2.ClampMagnitude(move, 1f) * moveSpeed;
//    }

//    void CheckScreen()
//    {
//        Vector2 min = cam.ViewportToWorldPoint(Vector3.zero);
//        Vector2 max = cam.ViewportToWorldPoint(Vector3.one);
//        Vector2 p = rb.position;
//        bool inside = p.x > min.x && p.x < max.x && p.y > min.y && p.y < max.y;

//        if (inside)
//        {
//            entered = true;
//        }
//        else if (entered)
//        {
//            Vector2 center = cam.transform.position;
//            rb.linearVelocity = (center - p).normalized * returnSpeed;
//            pushTimer = ReturnTime;
//        }
//    }

//    void UpdateFire(Vector2 playerPos)
//    {
//        if (!entered) return;

//        fireTimer -= Time.fixedDeltaTime;
//        if (fireTimer > 0f) return;

//        Fire(playerPos);
//        fireTimer = fireInterval;
//    }

//    void Fire(Vector2 playerPos)
//    {
//        float aim = AngleTo(playerPos);

//        switch (type)
//        {
//            case EnemyType.Scout:
//                Shoot(missilePrefab, aim, true);
//                break;

//            case EnemyType.Fighter:
//                StartCoroutine(Burst());
//                break;

//            case EnemyType.Heavy:
//                for (int i = -2; i <= 2; i++)
//                {
//                    Shoot(missilePrefab, aim + i * 20f, i == 0);
//                }
//                break;

//            case EnemyType.Interceptor:
//                Shoot(missilePrefab, aim, false, 2.5f);
//                break;

//            case EnemyType.Bomber:
//                EnemyBullet bomb = bombPrefab != null ? bombPrefab : missilePrefab;
//                Vector2 landing = playerPos + playerVelocity * LeadTime;
//                float fuse = Mathf.Max(Vector2.Distance(rb.position, landing) / bomb.Speed, 0.5f);
//                Shoot(bomb, AngleTo(landing), false, 1f, fuse);
//                break;
//        }
//    }

//    IEnumerator Burst()
//    {
//        for (int i = 0; i < 3; i++)
//        {
//            if (target == null || target.IsDead) yield break;

//            Shoot(missilePrefab, AngleTo(target.transform.position), true);
//            yield return new WaitForSeconds(0.15f);
//        }
//    }

//    void Shoot(EnemyBullet prefab, float angle, bool homing, float speedScale = 1f, float life = 0f)
//    {
//        EnemyBullet bullet = Instantiate(prefab, rb.position, Quaternion.Euler(0f, 0f, angle));
//        bullet.Launch(target.transform, damage, homing, speedScale, life);
//    }

//    float AngleTo(Vector2 point)
//    {
//        Vector2 d = point - rb.position;
//        return Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg - 90f;
//    }

//    void OnTriggerStay2D(Collider2D other)
//    {
//        Player player = other.GetComponentInParent<Player>();
//        if (player != null)
//        {
//            player.PushEnemy(this);
//        }
//    }

//    public void ApplyKnockback(Vector2 dir, float distance, float duration)
//    {
//        rb.linearVelocity = dir.normalized * (distance / duration);
//        pushTimer = duration;
//    }

//    public void TakeDamage(int amount)
//    {
//        if (dead) return;

//        hp -= amount;
//        Debug.Log($"{name} HP {Mathf.Max(hp, 0)}/{maxHp}");

//        if (hp <= 0)
//        {
//            Die();
//        }
//    }

//    public void Die()
//    {
//        if (dead) return;
//        dead = true;

//        KillCount++;

//        if (DataManager.Instance != null)
//        {
//            DataManager.Instance.currentScore += score;
//            DataManager.Instance.money += money;
//        }

//        Destroy(gameObject);
//    }
//}