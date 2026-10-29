using System.Collections;
using UnityEngine;

public class Player : MonoBehaviour
{
    [Header("이동 설정")]
    [SerializeField] private float moveSpeed = 6f;
    [SerializeField] private float slowSpeed = 2.5f;

    [Header("회전 설정")]
    [SerializeField] private float rotateSpeed = 180f;
    [SerializeField] private float slowRotateSpeed = 90f;

    [Header("이동 범위 (월드 좌표)")]
    [SerializeField] private Vector2 minBounds = new Vector2(-8f, -4.5f);
    [SerializeField] private Vector2 maxBounds = new Vector2(8f, 4.5f);

    [Header("태그 설정")]
    [SerializeField] private string enemyTag = "Enemy";
    [SerializeField] private string enemyBulletTag = "EnemyBullet";

    [Header("HP 설정")]
    [SerializeField] private int maxHp = 5;
    [SerializeField] private int damagePerHit = 1;

    [Header("적 넉백 설정 (적이 밀려남)")]
    [SerializeField] private float enemyKnockbackDistance = 2f;
    [SerializeField] private float enemyKnockbackDuration = 0.2f;

    [Header("무적 설정 (총알 피격 후)")]
    [SerializeField] private float invincibleTime = 1.5f;
    [SerializeField] private float blinkInterval = 0.1f;

    private Camera mainCam;
    private SpriteRenderer spriteRenderer;

    private int currentHp;
    private bool isInvincible;

    public int CurrentHp => currentHp;
    public int MaxHp => maxHp;

    // 치트 매니저(F2)에서 켜고 끄는 무적 상태
    public bool IsCheatInvincible { get; set; }

    private void Awake()
    {
        mainCam = Camera.main;
        spriteRenderer = GetComponent<SpriteRenderer>();
        currentHp = maxHp;
    }

    private void Update()
    {
        HandleMovement();

        if (Input.GetKeyDown(KeyCode.Q))
        {
            DestroyEnemiesOnScreen();
        }
    }

    // ---------------- 이동 ----------------
    private void HandleMovement()
    {
        float turn = Input.GetAxisRaw("Horizontal");
        float forward = 1f; // 자동 전진: 키 입력 없이 항상 앞으로 이동

        bool isSlow = Input.GetKey(KeyCode.LeftShift);
        float speed = isSlow ? slowSpeed : moveSpeed;
        float rotSpeed = isSlow ? slowRotateSpeed : rotateSpeed;

        transform.Rotate(0f, 0f, -turn * rotSpeed * Time.deltaTime);

        Vector3 newPos = transform.position + transform.up * (forward * speed * Time.deltaTime);
        newPos.x = Mathf.Clamp(newPos.x, minBounds.x, maxBounds.x);
        newPos.y = Mathf.Clamp(newPos.y, minBounds.y, maxBounds.y);
        transform.position = newPos;
    }

    // ---------------- 충돌 처리 ----------------
    private void OnTriggerEnter2D(Collider2D other)
    {
        // 적 총알: HP 감소
        if (other.CompareTag(enemyBulletTag))
        {
            Destroy(other.gameObject);
            TakeDamage(damagePerHit);
            return;
        }
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        // 적 본체: HP는 그대로, 적이 밀려남
        if (other.CompareTag(enemyTag))
        {
            EnemyKnockback knockback = other.GetComponent<EnemyKnockback>();
            if (knockback != null)
            {
                Vector2 dir = other.transform.position - transform.position; // 플레이어 → 적 방향
                knockback.ApplyKnockback(dir, enemyKnockbackDistance, enemyKnockbackDuration);
            }
        }
    }

    // ---------------- HP ----------------
    private void TakeDamage(int damage)
    {
        if (isInvincible || IsCheatInvincible) return; // 치트 무적 추가

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
        Debug.Log("플레이어 사망");
        gameObject.SetActive(false);
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

    // ---------------- 폭탄 ----------------
    private void DestroyEnemiesOnScreen()
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag(enemyTag);

        foreach (GameObject enemy in enemies)
        {
            if (IsVisibleOnCamera(enemy.transform.position))
            {
                Destroy(enemy);
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