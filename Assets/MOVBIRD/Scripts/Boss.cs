//using System.Collections;
//using UnityEngine;

//public class Boss : MonoBehaviour
//{
//    private Transform Target_Player;

//    public float Boss_Hp = 1f;          // 보스 체력
//    public float Boss_FireRate = 2f;    // 보스 재장전 시간
//    public float Boss_Spd = 1f;         // 보스 속도

//    public int Boss_Pattern;

//    [SerializeField] private GameObject Missile;    // 미사일 프리팹 받기
//    [SerializeField] private GameObject Bullet;     // 총알 프리팹 받기

//    [SerializeField] private float bulletSpeed = 8f;

//    [SerializeField] private float BarrierTime = 3f;       // 무적 지속 시간
//    private bool isInvincible = false;                     // 무적 상태 여부

//    private float fireTimer = 0f;                           // 발사 간격

//    [SerializeField] private float dashDistance = 5f;       // 돌진 거리
//    [SerializeField] private float dashSpeed = 20f;         // 돌진 속도
//    private bool isDashing = false;                         // 돌진 중 여부
//    private Rigidbody2D rb;

//    void Awake()
//    {
//        ResetPattern();                                 // 공격 패턴 리셋
//    }

//    void ResetPattern()                                 // 패턴 다시 뽑는 함수
//    {
//        rb = GetComponent<Rigidbody2D>();
//        Boss_Pattern = Random.Range(1, 6);
//    }

//    void Missile_Luncher()
//    {
//        if (Missile != null)
//        {
//            Instantiate(Missile, transform.position, Quaternion.identity);          // 보스의 위치에서 미사일 소환

//            Debug.Log("보스가 유도 미사일을 소환했습니다!");
//        }
//    }

//    void SpawnShotgunBullet()
//    {
//        // 플레이어 찾기
//        GameObject player = GameObject.FindWithTag("Player");
//        if (player == null) return; // 플레이어가 없으면 발사 안 함

//        Vector2 baseDirection = (player.transform.position - transform.position).normalized; // 보스 -> 플레이어 방향 구하기

//        float baseAngle = Mathf.Atan2(baseDirection.y, baseDirection.x) * Mathf.Rad2Deg;    // 방향의 각도 구함

//        int bulletCount = Random.Range(5, 9);                                               // 탄수 = 5~8

//        float totalSpreadAngle = 60f;                                                       // 탄퍼짐 각도
//        float angleStep = totalSpreadAngle / (bulletCount - 1);
//        float startAngle = baseAngle - (totalSpreadAngle / 2f);

//        for (int i = 0; i < bulletCount; i++)                                               // 총알 소환 및 발사
//        {
//            float targetAngle = startAngle + (angleStep * i);                               // 총알이 날아갈 각도 계산

//            float rad = targetAngle * Mathf.Deg2Rad;                                        // 각도를 다시 방향으로 변환
//            Vector2 shotDirection = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));

//            Quaternion bulletRotation = Quaternion.Euler(0, 0, targetAngle);                // 총알이 나라가는 방향을 보게함


//            Vector3 spawnPos = transform.position + (Vector3)(shotDirection * 1f);          // 보스 몸 밖에서
//            GameObject bullet = Instantiate(Bullet, spawnPos, bulletRotation);              // 총알 생성

//            Rigidbody2D rb = bullet.GetComponent<Rigidbody2D>();                            // 총알 속도 부여
//            if (rb != null)
//                rb.linearVelocity = shotDirection * bulletSpeed;
//        }
//    }

//    void Boss_Skill_Dash()
//    {
//        if (isDashing) return;              // 돌진 중 중복 실행 방지
//        StartCoroutine(DashRoutine());
//    }

//    IEnumerator DashRoutine()
//    {
//        isDashing = true;

//        GameObject player = GameObject.FindWithTag("Player");   // 플레이어 방향으로 몸을 돌리기
//        if (player != null)
//        {
//            Vector2 toPlayer = player.transform.position - transform.position;
//            transform.up = toPlayer;
//        }

//        Vector2 dir = transform.up;         // 현재 바라보는 방향
//        float moved = 0f;

//        while (moved < dashDistance)
//        {
//            float step = Mathf.Min(dashSpeed * Time.fixedDeltaTime, dashDistance - moved);

//            if (rb != null)
//                rb.MovePosition(rb.position + dir * step);
//            else
//                transform.position += (Vector3)(dir * step);

//            moved += step;
//            yield return new WaitForFixedUpdate();
//        }

//        isDashing = false;
//    }

//    void Boss_Skill_Barrier()
//    {
//        if (isInvincible) return;           // 이미 무적이면 중복 실행 방지
//        StartCoroutine(BarrierRoutine());   // 
//    }

//    IEnumerator BarrierRoutine()
//    {
//        isInvincible = true;
//        Debug.Log("보스 배리어 발동");

//        yield return new WaitForSeconds(BarrierTime);   // 베리어 발동 시간동안 베리어 발동

//        isInvincible = false;
//        Debug.Log("배리어 해제");
//    }

//    public void TakeDamage(float damage)                // 보스한테 데미지 주기
//    {
//        if (isInvincible) return;                       // 무적이면 데미지 무시

//        Boss_Hp -= damage;

//        if (Boss_Hp <= 0)
//        {
//            Debug.Log("보스가 죽었습니다.");
//            Destroy(gameObject);
//        }
//    }

//    void Boss_Pattern_1()
//    {
//        Boss_Skill_Dash();      // 데시
//        SpawnShotgunBullet();   // 샷건
//        Missile_Luncher();      // 미사일
//        Missile_Luncher();      // 미사일
//        Boss_Skill_Barrier();   // 베리어
//    }

//    void Boss_Pattern_2()
//    {
//        Missile_Luncher();      // 미사일
//        Boss_Skill_Barrier();   // 베리어
//        Boss_Skill_Dash();      // 데시
//        SpawnShotgunBullet();   // 샷건
//        SpawnShotgunBullet();   // 샷건
//    }

//    void Boss_Pattern_3()
//    {
//        Boss_Skill_Barrier();   // 베리어
//        Missile_Luncher();      // 미사일
//        SpawnShotgunBullet();   // 샷건
//        Boss_Skill_Barrier();   // 베리어
//        Boss_Skill_Dash();      // 데시
//    }

//    void Boss_Pattern_4()
//    {
//        Boss_Skill_Dash();      // 데시
//        SpawnShotgunBullet();   // 샷건
//        Boss_Skill_Barrier();   // 베리어
//        SpawnShotgunBullet();   // 샷건
//        Boss_Skill_Barrier();   // 베리어
//    }

//    void Boss_Pattern_5()
//    {
//        Missile_Luncher();      // 미사일
//        SpawnShotgunBullet();   // 샷건
//        Missile_Luncher();      // 미사일
//        SpawnShotgunBullet();   // 샷건
//        Boss_Skill_Dash();      // 데시
//    }

//    private void OnCollisionEnter2D(Collision2D collision)
//    {
//        if (collision.gameObject.CompareTag("Player"))
//        {
//            Debug.Log("플레이어와 충돌");
//            TakeDamage(1);              // 1만큼 데미지 주기
//        }
//    }

//    void Update()
//    {
//        // 쿨타임 처리
//        fireTimer += Time.deltaTime;
//        if (fireTimer < Boss_FireRate) return;
//        fireTimer = 0f;

//        switch (Boss_Pattern)                       // 보스 패턴
//        {
//            case 1: Boss_Pattern_1(); break;        // 패턴 1
//            case 2: Boss_Pattern_2(); break;        // 패턴 2
//            case 3: Boss_Pattern_3(); break;        // 패턴 3
//            case 4: Boss_Pattern_4(); break;        // 패턴 4
//            case 5: Boss_Pattern_5(); break;        // 패턴 5
//        }

//        ResetPattern();                             // 패턴 리셋
//    }
//}
