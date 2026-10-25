using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

public class Player : MonoBehaviour
{
    public enum ControlMode
    {
        [InspectorName("탱크식 - ←→ 회전, ↑ 전진 (기획서)")] Tank,
        [InspectorName("방향식 - 누른 방향으로 머리를 돌리며 전진")] Direction
    }

    [Header("조작 방식")]
    [SerializeField] private ControlMode controlMode = ControlMode.Tank;

    [Header("이동")]
    [SerializeField] private float moveSpeed = 6f;
    [SerializeField] private float slowSpeed = 2.5f;

    [Header("회전")]
    [Tooltip("초당 회전 각도")]
    [SerializeField] private float turnSpeed = 300f;
    [Tooltip("감속(Shift) 중 회전 각도. 느리게 움직이는 대신 더 빨리 돎")]
    [SerializeField] private float slowTurnSpeed = 450f;
    [Tooltip("탱크식에서 멈춰 있을 때 회전 배율 (제자리 회전을 더 빠르게)")]
    [SerializeField] private float idleTurnMultiplier = 1.3f;

    [Header("화면")]
    [Tooltip("화면 끝에서 안쪽으로 남길 여백")]
    [SerializeField] private float screenMargin = 0.5f;

    [Header("조작 키")]
    [SerializeField] private KeyCode slowKey = KeyCode.LeftShift;
    [SerializeField] private KeyCode bombKey = KeyCode.Q;

    [Header("태그")]
    [Tooltip("폭탄이 지우는 탄, 운석 같은 방해물에 붙이는 태그")]
    [SerializeField] private string enemyBulletTag = "EnemyBullet";

    [Header("HP")]
    [SerializeField] private int maxHp = 5;
    [Tooltip("미사일이 아닌 EnemyBullet 태그 물체(운석 등)에 맞았을 때 깎이는 양")]
    [SerializeField] private int damagePerHit = 1;

    [Header("폭탄")]
    [SerializeField] private int bombCount = 3;

    [Header("적 밀어내기")]
    [SerializeField] private float enemyKnockbackDistance = 2f;
    [SerializeField] private float enemyKnockbackDuration = 0.2f;

    [Header("무적 (피격 후)")]
    [SerializeField] private float invincibleTime = 1.5f;
    [SerializeField] private float blinkInterval = 0.1f;

    [Header("게임오버")]
    [Tooltip("켜면 죽은 뒤 같은 스테이지를 다시 시작 (돈·파츠는 유지)")]
    [SerializeField] private bool restartOnDeath = true;
    [SerializeField] private float restartDelay = 2f;
    [SerializeField] private UnityEvent onDeath;

    private Camera mainCam;
    private SpriteRenderer spriteRenderer;
    private Collider2D col;

    private int currentHp;
    private int stageStartScore;
    private bool isInvincible;
    private bool isMoving;
    private bool isDead;

    public int CurrentHp => currentHp;
    public int MaxHp => maxHp;
    public float HpRatio => (float)currentHp / maxHp;
    public int BombCount => bombCount;
    public bool IsDead => isDead;

    private void Awake()
    {
        mainCam = Camera.main;
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        col = GetComponentInChildren<Collider2D>();
        currentHp = maxHp;
        Enemy.KillCount = 0;
    }

    private void Start()
    {
        if (DataManager.Instance != null)
        {
            stageStartScore = DataManager.Instance.currentScore;
        }

        if (col == null)
        {
            Debug.LogWarning("[Player] Collider 2D가 없어요. Add Component → Circle Collider 2D를 붙여 주세요.");
        }

        if (GetComponentInChildren<Collider>() != null)
        {
            Debug.LogWarning("[Player] 3D Collider가 붙어 있어요. 이름에 2D가 들어간 Collider로 바꿔 주세요.");
        }
    }

    private void Update()
    {
        if (isDead) return;

        HandleMovement();

        if (Input.GetKeyDown(bombKey) && bombCount > 0)
        {
            UseBomb();
        }
    }

    private void HandleMovement()
    {
        bool isSlow = Input.GetKey(slowKey);
        float speed = isSlow ? slowSpeed : moveSpeed;
        float turnRate = isSlow ? slowTurnSpeed : turnSpeed;
        float forward;

        if (controlMode == ControlMode.Tank)
        {
            float turn = Input.GetAxisRaw("Horizontal");
            forward = Mathf.Max(0f, Input.GetAxisRaw("Vertical"));

            if (forward == 0f) turnRate *= idleTurnMultiplier;

            transform.Rotate(0f, 0f, -turn * turnRate * Time.deltaTime);
        }
        else
        {
            Vector2 input = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
            forward = input.sqrMagnitude > 0.01f ? 1f : 0f;

            if (forward > 0f)
            {
                float targetAngle = Mathf.Atan2(input.y, input.x) * Mathf.Rad2Deg - 90f;
                float angle = Mathf.MoveTowardsAngle(transform.eulerAngles.z, targetAngle, turnRate * Time.deltaTime);
                transform.rotation = Quaternion.Euler(0f, 0f, angle);
            }
        }

        isMoving = forward > 0f;

        Vector2 min = (Vector2)mainCam.ViewportToWorldPoint(Vector3.zero) + Vector2.one * screenMargin;
        Vector2 max = (Vector2)mainCam.ViewportToWorldPoint(Vector3.one) - Vector2.one * screenMargin;

        Vector3 newPos = transform.position + transform.up * (forward * speed * Time.deltaTime);
        newPos.x = Mathf.Clamp(newPos.x, min.x, max.x);
        newPos.y = Mathf.Clamp(newPos.y, min.y, max.y);
        transform.position = newPos;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isDead || !other.CompareTag(enemyBulletTag)) return;
        if (other.GetComponentInParent<EnemyBullet>() != null) return;

        Destroy(other.gameObject);
        TakeDamage(damagePerHit);
    }

    public void PushEnemy(Enemy enemy)
    {
        if (isDead) return;

        Vector2 dir = isMoving
            ? (Vector2)transform.up
            : (Vector2)(enemy.transform.position - transform.position);

        enemy.ApplyKnockback(dir, enemyKnockbackDistance, enemyKnockbackDuration);
    }

    public void TakeDamage(int damage)
    {
        if (isDead || isInvincible) return;

        currentHp = Mathf.Max(0, currentHp - damage);
        Debug.Log($"피격! HP: {currentHp}/{maxHp}");

        if (currentHp <= 0)
        {
            Die();
            return;
        }

        StartCoroutine(InvincibleRoutine());
    }

    private void Die()
    {
        isDead = true;
        StopAllCoroutines();

        if (spriteRenderer != null) spriteRenderer.enabled = false;
        if (col != null) col.enabled = false;

        if (DataManager.Instance != null)
        {
            DataManager.Instance.currentScore = stageStartScore;
        }

        Debug.Log("플레이어 사망");
        onDeath.Invoke();

        if (restartOnDeath)
        {
            Invoke(nameof(Restart), restartDelay);
        }
    }

    private void Restart()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private IEnumerator InvincibleRoutine()
    {
        isInvincible = true;

        float elapsed = 0f;
        while (elapsed < invincibleTime)
        {
            if (spriteRenderer != null)
                spriteRenderer.enabled = !spriteRenderer.enabled;

            yield return new WaitForSeconds(blinkInterval);
            elapsed += blinkInterval;
        }

        if (spriteRenderer != null)
            spriteRenderer.enabled = true;

        isInvincible = false;
    }

    private void UseBomb()
    {
        bombCount--;

        foreach (GameObject bullet in GameObject.FindGameObjectsWithTag(enemyBulletTag))
        {
            if (IsVisibleOnCamera(bullet.transform.position))
            {
                Destroy(bullet);
            }
        }
    }

    private bool IsVisibleOnCamera(Vector3 worldPos)
    {
        Vector3 viewPos = mainCam.WorldToViewportPoint(worldPos);

        return viewPos.z > 0f &&
               viewPos.x >= 0f && viewPos.x <= 1f &&
               viewPos.y >= 0f && viewPos.y <= 1f;
    }
}