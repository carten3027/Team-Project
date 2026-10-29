//using System.Collections.Generic;
//using UnityEngine;


//public class Item : MonoBehaviour
//{
//    public enum ItemType { Use, Equip }         // 아이템 종류

//    public enum ItemPart                        // 파츠 부위
//    {
//        None = 0,
//        Wing = 1,                               // 날개
//        Body = 2,                               // 몸체
//        Engine = 3                              // 엔진
//    }

//    public enum EquipSkill                      // 장착 아이템 능력
//    {
//        None = 0,
//        ForceGuide = 1,                         // 강제 유도 (날개)
//        TimeStop = 2,                           // 시간 정지 (날개)
//        Reflect = 3,                            // 공격 반사 (몸)
//        BlackHole = 4,                          // 블랙홀 (몸)
//        SlowField = 5                           // 강속장 (엔진)
//    }

//    // ---------------- 설정 ----------------
//    [Header("태그")]
//    [SerializeField] private string throwTag = "Throw";         // 미사일, 총알 태그
//    [SerializeField] private string enemyTag = "Enemy";         // 일반 적 태그
//    [SerializeField] private string bossTag = "Boss";           // 보스 태그

//    [Header("장착 상태")]
//    [SerializeField] private EquipSkill wingSlot_1 = EquipSkill.None;     // 날개 칸 1
//    [SerializeField] private EquipSkill wingSlot_2 = EquipSkill.None;     // 날개 칸 2
//    [SerializeField] private EquipSkill bodySlot_1 = EquipSkill.None;     // 몸체 칸 1
//    [SerializeField] private EquipSkill bodySlot_2 = EquipSkill.None;     // 몸체 칸 2
//    [SerializeField] private EquipSkill engineSlot = EquipSkill.None;     // 엔진 칸

//    [Header("발동 키 (파트별)")]
//    [SerializeField] private KeyCode forceGuideKey = KeyCode.Alpha1;    // 강제 유도 키
//    [SerializeField] private KeyCode timeStopKey = KeyCode.Alpha2;      // 시간 정지 키
//    [SerializeField] private KeyCode reflectKey = KeyCode.Alpha3;       // 공격 반사 키
//    [SerializeField] private KeyCode blackHoleKey = KeyCode.Alpha4;     // 블랙홀 키
//    [SerializeField] private KeyCode slowFieldKey = KeyCode.Alpha5;     // 강속장 키

//    [Header("공통")]
//    [SerializeField] private float skillCooldown = 2f;          // 능력 재사용 시간
//    [SerializeField] private float reverseHitDamage = 1f;       // 반대로 유도된 미사일이 적에게 주는 데미지
//    [SerializeField] private float reverseDefaultSpeed = 8f;    // 속도를 알 수 없는 오브젝트의 반대 유도 속도
//    [SerializeField] private float reverseLifetime = 10f;       // 반대 유도 후 사라지기까지의 시간

//    [Header("시간 정지")]
//    [SerializeField] private float timeFreezeDuration = 3f;     // 시간 정지 지속 시간

//    [Header("공격 반사")]
//    [SerializeField] private float reflectRadius = 1.5f;        // 공격 반사 인식 범위

//    [Header("블랙홀")]
//    [SerializeField] private GameObject blackHolePrefab;            // 블랙홀 이펙트 (없으면 원을 자동 생성)
//    [SerializeField] private float blackHoleDuration = 3f;          // 블랙홀 유지 시간
//    [SerializeField] private float blackHolePullRadius = 5f;        // 블랙홀 흡수 범위
//    [SerializeField] private float blackHolePullSpeed = 8f;         // 블랙홀 흡수 속도
//    [SerializeField] private float blackHoleAbsorbDistance = 0.3f;  // 중심에 이 거리까지 오면 소멸

//    [Header("강속장")]
//    [SerializeField] private float slowFieldRadius = 4.0f;          // 강속장 범위
//    [SerializeField] private float slowFieldDuration = 4.0f;        // 강속장 지속 시간
//    [SerializeField] private float slowSpeedAmount = 2f;            // 강속장 내 미사일 속도

//    // ---------------- 내부 상태 ----------------
//    private class ThrowState                    // Throw 오브젝트 하나의 상태
//    {
//        public Transform tf;
//        public Boss_Missile missile;            // 미사일이면 Boss_Missile 스크립트
//        public Rigidbody2D rb;                  // 총알처럼 물리로 움직이는 경우
//        public bool reversed;                   // 반대 유도 중인지
//        public Transform target;                // 반대 유도 목표(적)
//        public Vector2 reverseDir;              // 반대 유도 방향
//        public float reverseSpeed;              // 반대 유도 속도
//        public float reverseUntil;              // 반대 유도 종료 시각
//    }

//    private class BlackHoleData                 // 설치된 블랙홀 하나
//    {
//        public Vector2 pos;
//        public float endTime;
//        public GameObject visual;
//    }

//    private readonly Dictionary<GameObject, ThrowState> states = new Dictionary<GameObject, ThrowState>();
//    private readonly List<BlackHoleData> blackHoles = new List<BlackHoleData>();
//    private readonly List<GameObject> enemies = new List<GameObject>();             // 이번 프레임의 적 목록
//    private readonly Dictionary<EquipSkill, float> cooldownEnd = new Dictionary<EquipSkill, float>();
//    private readonly HashSet<string> warnedTags = new HashSet<string>();            // 없는 태그 경고 한 번만 출력

//    private float timeStopUntil;                // 시간 정지 종료 시각
//    private float slowFieldUntil;               // 강속장 종료 시각

//    private bool IsTimeStopped => Time.time < timeStopUntil;
//    private bool IsSlowFieldActive => Time.time < slowFieldUntil;

//    private static Sprite circleSprite;         // 블랙홀 기본 원 스프라이트

//    // ---------------- 장착 / 해제 ----------------

//    // 능력이 들어가는 부위
//    public static ItemPart GetPart(EquipSkill skill)
//    {
//        switch (skill)
//        {
//            case EquipSkill.ForceGuide:
//            case EquipSkill.TimeStop:
//                return ItemPart.Wing;
//            case EquipSkill.Reflect:
//            case EquipSkill.BlackHole:
//                return ItemPart.Body;
//            case EquipSkill.SlowField:
//                return ItemPart.Engine;
//        }
//        return ItemPart.None;
//    }

//    public bool IsEquipped(EquipSkill skill)            // 능력이 장착되어 있는지
//    {
//        ItemPart part = GetPart(skill);
//        int count = GetSlotCount(part);

//        for (int i = 0; i < count; i++)
//        {
//            if (SlotRef(part, i) == skill) return true;
//        }
//        return false;
//    }

//    public EquipSkill GetEquipped(ItemPart part, int slotNumber)    // 부위의 슬롯(1부터)에 장착된 능력
//    {
//        int index = slotNumber - 1;
//        if (index < 0 || index >= GetSlotCount(part)) return EquipSkill.None;

//        return SlotRef(part, index);
//    }

//    public bool Equip(EquipSkill skill)                 // 장착 (비어 있는 슬롯에 자동으로)
//    {
//        ItemPart part = GetPart(skill);
//        if (part == ItemPart.None) return false;

//        if (IsEquipped(skill))                          // 같은 능력은 중복 장착 불가
//        {
//            Debug.Log($"{skill}은(는) 이미 장착되어 있습니다.");
//            return false;
//        }

//        int count = GetSlotCount(part);
//        for (int i = 0; i < count; i++)
//        {
//            if (SlotRef(part, i) != EquipSkill.None) continue;

//            SlotRef(part, i) = skill;                   // 비어 있는 첫 슬롯에 장착
//            Debug.Log($"{skill} 장착 ({part} 슬롯 {i + 1})");
//            return true;
//        }

//        Debug.Log($"{part} 슬롯이 가득 찼습니다. 먼저 해제하세요.");
//        return false;
//    }

//    public bool Equip(EquipSkill skill, int slotNumber) // 슬롯 번호(1부터)를 지정해서 장착 (그 칸의 기존 능력은 교체)
//    {
//        ItemPart part = GetPart(skill);
//        int count = GetSlotCount(part);
//        int index = slotNumber - 1;

//        if (part == ItemPart.None || index < 0 || index >= count)
//        {
//            Debug.Log($"{skill}을(를) 장착할 수 없는 슬롯입니다.");
//            return false;
//        }

//        for (int i = 0; i < count; i++)                 // 다른 슬롯에 있던 같은 능력은 옮김
//        {
//            if (i != index && SlotRef(part, i) == skill)
//                SlotRef(part, i) = EquipSkill.None;
//        }

//        SlotRef(part, index) = skill;
//        Debug.Log($"{skill} 장착 ({part} 슬롯 {slotNumber})");
//        return true;
//    }

//    public void Unequip(EquipSkill skill)               // 능력으로 해제
//    {
//        ItemPart part = GetPart(skill);
//        int count = GetSlotCount(part);

//        for (int i = 0; i < count; i++)
//        {
//            if (SlotRef(part, i) != skill) continue;

//            SlotRef(part, i) = EquipSkill.None;
//            Debug.Log($"{skill} 해제 ({part} 슬롯 {i + 1})");
//            return;
//        }
//    }

//    public void Unequip(ItemPart part, int slotNumber)  // 슬롯 번호(1부터)로 해제
//    {
//        EquipSkill skill = GetEquipped(part, slotNumber);
//        if (skill != EquipSkill.None) Unequip(skill);
//    }

//    private int GetSlotCount(ItemPart part)             // 부위별 슬롯 수
//    {
//        switch (part)
//        {
//            case ItemPart.Wing: return 2;               // 날개 2칸
//            case ItemPart.Body: return 2;               // 몸체 2칸
//            case ItemPart.Engine: return 1;             // 엔진 1칸
//        }
//        return 0;
//    }

//    private ref EquipSkill SlotRef(ItemPart part, int index)        // 슬롯 변수 접근 (index는 0부터)
//    {
//        switch (part)
//        {
//            case ItemPart.Wing:
//                if (index == 0) return ref wingSlot_1;
//                return ref wingSlot_2;
//            case ItemPart.Body:
//                if (index == 0) return ref bodySlot_1;
//                return ref bodySlot_2;
//        }
//        return ref engineSlot;
//    }

//    // ---------------- 입력 ----------------
//    void Update()
//    {
//        if (Input.GetKeyDown(forceGuideKey)) UseSkill(EquipSkill.ForceGuide);   // 강제 유도
//        if (Input.GetKeyDown(timeStopKey)) UseSkill(EquipSkill.TimeStop);       // 시간 정지
//        if (Input.GetKeyDown(reflectKey)) UseSkill(EquipSkill.Reflect);         // 공격 반사
//        if (Input.GetKeyDown(blackHoleKey)) UseSkill(EquipSkill.BlackHole);     // 블랙홀
//        if (Input.GetKeyDown(slowFieldKey)) UseSkill(EquipSkill.SlowField);     // 강속장

//        UpdateBlackHoles();
//    }

//    void LateUpdate()
//    {
//        ProcessThrows();                // 미사일 이동이 끝난 뒤에 덮어쓰기
//    }

//    private void UseSkill(EquipSkill skill)
//    {
//        if (!IsEquipped(skill))                                     // 해당 파트가 장착되어 있어야 사용 가능
//        {
//            Debug.Log($"{skill} 파트가 장착되어 있지 않습니다.");
//            return;
//        }

//        if (cooldownEnd.TryGetValue(skill, out float end) && Time.time < end)
//        {
//            Debug.Log($"{skill} 재사용 대기 중");
//            return;
//        }

//        if (ExecuteSkill(skill))                                    // 발동에 성공했을 때만 쿨타임 적용
//            cooldownEnd[skill] = Time.time + skillCooldown;
//    }

//    private bool ExecuteSkill(EquipSkill skill)
//    {
//        switch (skill)
//        {
//            case EquipSkill.ForceGuide: return ExecuteForcedGuidance();
//            case EquipSkill.TimeStop: return ExecuteTimeStop();
//            case EquipSkill.Reflect: return ExecuteReflection();
//            case EquipSkill.BlackHole: return ExecuteBlackHole();
//            case EquipSkill.SlowField: return ExecuteSlowField();
//        }
//        return false;
//    }

//    // ---------------- 능력 ----------------

//    public bool ExecuteForcedGuidance()     // 강제 유도
//    {
//        RefreshEnemies();

//        Transform target = FindEnemyInFront();                      // 플레이어가 바라보는 방향의 적
//        if (target == null)
//        {
//            Debug.Log("유도할 적이 없습니다.");
//            return false;
//        }

//        int count = 0;
//        foreach (GameObject obj in FindTagged(throwTag))
//        {
//            if (obj == null) continue;
//            if (!IsOnScreen(obj.transform.position)) continue;      // 화면 밖은 제외

//            ThrowState st = GetState(obj);
//            if (st == null || st.reversed) continue;                // 이미 반대 유도 중이면 제외

//            StartReverse(st, target);
//            count++;
//        }

//        Debug.Log($"강제 유도 발동 ({count}개)");
//        return count > 0;
//    }

//    public bool ExecuteTimeStop()       // 시간 정지 (미사일만 정지)
//    {
//        timeStopUntil = Time.time + timeFreezeDuration;
//        Debug.Log("시간 정지 발동");
//        return true;
//    }

//    public bool ExecuteReflection()     // 공격 반사 (닿기 직전에 눌러야 함)
//    {
//        RefreshEnemies();
//        Transform target = FindNearestEnemy(transform.position);    // 없으면 반대 방향으로 반사

//        int count = 0;
//        foreach (GameObject obj in FindTagged(throwTag))
//        {
//            if (obj == null) continue;
//            if (Vector2.Distance(transform.position, obj.transform.position) > reflectRadius) continue;

//            ThrowState st = GetState(obj);
//            if (st == null || st.reversed) continue;

//            StartReverse(st, target);
//            count++;
//        }

//        if (count > 0) Debug.Log($"공격 반사 발동 ({count}개)");
//        return count > 0;               // 반사할 게 없으면 쿨타임 없이 취소
//    }

//    public bool ExecuteBlackHole()      // 블랙홀
//    {
//        Camera cam = Camera.main;
//        if (cam == null)
//        {
//            Debug.LogWarning("카메라가 없습니다.");
//            return false;
//        }

//        Vector3 screenPos = Input.mousePosition;
//        screenPos.z = -cam.transform.position.z;                    // 2D 카메라 깊이 보정
//        Vector3 mouseWorldPos = cam.ScreenToWorldPoint(screenPos);  // 마우스 위치를 월드 좌표로 변환
//        mouseWorldPos.z = 0f;                                       // 2D라서 z값 고정

//        BlackHoleData bh = new BlackHoleData();
//        bh.pos = mouseWorldPos;
//        bh.endTime = Time.time + blackHoleDuration;
//        bh.visual = CreateBlackHoleVisual(mouseWorldPos);
//        blackHoles.Add(bh);

//        Debug.Log("블랙홀 생성");
//        return true;
//    }

//    public bool ExecuteSlowField()      // 강속장
//    {
//        slowFieldUntil = Time.time + slowFieldDuration;
//        Debug.Log("강속장 발동");
//        return true;
//    }

//    // ---------------- Throw 오브젝트 처리 ----------------

//    // 매 프레임 Throw 태그 오브젝트의 이동을 능력에 맞게 덮어씀
//    private void ProcessThrows()
//    {
//        RefreshEnemies();
//        CleanStates();

//        foreach (GameObject obj in FindTagged(throwTag))
//        {
//            if (obj == null) continue;                              // 이미 사라진 오브젝트는 넘기기

//            ThrowState st = GetState(obj);
//            if (st == null) continue;                               // 움직이는 오브젝트가 아니면 넘기기

//            Vector2 pos = obj.transform.position;
//            Vector2 natural = GetNaturalVelocity(st);               // 원래 이동 속도
//            Vector2 desired = natural;                              // 이번 프레임에 원하는 속도

//            if (st.reversed)                                        // 반대 유도 중인 오브젝트
//            {
//                if (Time.time >= st.reverseUntil) { Destroy(obj); continue; }
//                if (HitEnemy(obj, pos)) continue;                   // 적에게 맞으면 데미지 후 소멸

//                desired = GetReverseVelocity(st, pos);
//                RotateMissile(st, desired);
//            }
//            else if (st.missile != null)                            // 아직 적 편이 아닌 미사일 (우선순위: 블랙홀 > 시간 정지 > 강속장)
//            {
//                BlackHoleData bh = FindBlackHole(pos);

//                if (bh != null)                                     // 블랙홀 범위 안
//                {
//                    Vector2 toCenter = bh.pos - pos;
//                    if (toCenter.magnitude <= blackHoleAbsorbDistance)
//                    {
//                        Destroy(obj);                               // 중심에 닿으면 흡수
//                        continue;
//                    }
//                    desired = toCenter.normalized * blackHolePullSpeed;
//                }
//                else if (IsTimeStopped)                             // 시간 정지
//                {
//                    desired = Vector2.zero;
//                }
//                else if (IsSlowFieldActive && Vector2.Distance(transform.position, pos) <= slowFieldRadius)    // 강속장 범위 안
//                {
//                    desired = natural.normalized * Mathf.Min(natural.magnitude, slowSpeedAmount);
//                }
//            }

//            ApplyVelocity(st, natural, desired);
//        }
//    }

//    private void ApplyVelocity(ThrowState st, Vector2 natural, Vector2 desired)
//    {
//        if (st.missile != null)
//        {
//            // 미사일은 Boss_Missile이 이미 natural만큼 움직였으므로 차이만큼만 보정
//            st.tf.position += (Vector3)((desired - natural) * Time.deltaTime);
//        }
//        else if (st.reversed)
//        {
//            if (st.rb != null) st.rb.linearVelocity = desired;                  // 물리 오브젝트
//            else st.tf.position += (Vector3)(desired * Time.deltaTime);
//        }
//    }

//    // 원래 이동 속도 (Boss_Missile이 이번 프레임에 쓴 값)
//    private Vector2 GetNaturalVelocity(ThrowState st)
//    {
//        if (st.missile != null) return st.missile.dir.normalized * st.missile.Missile_Spd;
//        if (st.rb != null) return st.rb.linearVelocity;
//        return Vector2.zero;
//    }

//    // 반대 유도 시작 (target이 null이면 원래 방향의 반대로)
//    private void StartReverse(ThrowState st, Transform target)
//    {
//        Vector2 natural = GetNaturalVelocity(st);
//        Vector2 pos = st.tf.position;

//        st.reversed = true;
//        st.target = target;
//        st.reverseSpeed = natural.magnitude > 0.1f ? natural.magnitude : reverseDefaultSpeed;
//        st.reverseUntil = Time.time + reverseLifetime;

//        if (target != null) st.reverseDir = ((Vector2)target.position - pos).normalized;
//        else st.reverseDir = natural.sqrMagnitude > 0.0001f ? -natural.normalized : Vector2.up;

//        IgnorePlayerCollision(st.tf);                               // 반대로 날아가는 도중 플레이어를 맞히지 않게
//    }

//    private Vector2 GetReverseVelocity(ThrowState st, Vector2 pos)
//    {
//        if (st.target == null)                                      // 목표가 사라졌으면 가장 가까운 적으로
//        {
//            Transform next = FindNearestEnemy(pos);
//            if (next != null) st.target = next;
//        }

//        if (st.target != null)
//        {
//            Vector2 to = (Vector2)st.target.position - pos;
//            if (to.sqrMagnitude > 0.0001f) st.reverseDir = to.normalized;
//        }
//        return st.reverseDir * st.reverseSpeed;
//    }

//    private void RotateMissile(ThrowState st, Vector2 dir)          // 미사일이 날아가는 방향을 보게 함 (y축이 정면)
//    {
//        if (st.missile == null || dir.sqrMagnitude < 0.0001f) return;

//        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
//        st.tf.rotation = Quaternion.AngleAxis(angle - 90f, Vector3.forward);
//    }

//    private void IgnorePlayerCollision(Transform obj)               // 플레이어와의 충돌 무시
//    {
//        Collider2D[] mine = obj.GetComponentsInChildren<Collider2D>();
//        Collider2D[] player = GetComponentsInChildren<Collider2D>();

//        foreach (Collider2D a in mine)
//        {
//            foreach (Collider2D b in player)
//                Physics2D.IgnoreCollision(a, b, true);
//        }
//    }

//    // 반대 유도된 오브젝트가 적에게 닿았는지 확인
//    private bool HitEnemy(GameObject obj, Vector2 pos)
//    {
//        foreach (GameObject enemy in enemies)
//        {
//            if (enemy == null) continue;

//            Collider2D col = enemy.GetComponent<Collider2D>();
//            bool hit = col != null ? col.OverlapPoint(pos) : Vector2.Distance(pos, enemy.transform.position) <= 0.5f;
//            if (!hit) continue;

//            Boss boss = enemy.GetComponent<Boss>();
//            if (boss != null) boss.TakeDamage(reverseHitDamage);    // 보스는 데미지
//            else Destroy(enemy);                                    // 일반 적은 즉시 파괴

//            Destroy(obj);
//            return true;
//        }
//        return false;
//    }

//    // ---------------- 블랙홀 관리 ----------------

//    private void UpdateBlackHoles()                                 // 시간이 지난 블랙홀 제거
//    {
//        for (int i = blackHoles.Count - 1; i >= 0; i--)
//        {
//            if (Time.time < blackHoles[i].endTime) continue;

//            if (blackHoles[i].visual != null) Destroy(blackHoles[i].visual);
//            blackHoles.RemoveAt(i);
//            Debug.Log("블랙홀 소멸");
//        }
//    }

//    private BlackHoleData FindBlackHole(Vector2 pos)                // 해당 위치를 끌어당기는 블랙홀
//    {
//        foreach (BlackHoleData bh in blackHoles)
//        {
//            if (Vector2.Distance(bh.pos, pos) <= blackHolePullRadius) return bh;
//        }
//        return null;
//    }

//    private GameObject CreateBlackHoleVisual(Vector3 pos)
//    {
//        if (blackHolePrefab != null)
//            return Instantiate(blackHolePrefab, pos, Quaternion.identity);

//        GameObject go = new GameObject("BlackHole");                // 프리팹이 없으면 반투명 원을 자동 생성
//        go.transform.position = pos;
//        go.transform.localScale = Vector3.one * blackHolePullRadius * 2f;

//        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
//        sr.sprite = GetCircleSprite();
//        sr.color = new Color(0.3f, 0f, 0.5f, 0.45f);
//        sr.sortingOrder = 5;
//        return go;
//    }

//    private static Sprite GetCircleSprite()                         // 지름 1유닛짜리 원
//    {
//        if (circleSprite != null) return circleSprite;

//        const int size = 64;
//        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
//        Vector2 center = new Vector2(size / 2f, size / 2f);

//        for (int y = 0; y < size; y++)
//        {
//            for (int x = 0; x < size; x++)
//            {
//                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), center);
//                tex.SetPixel(x, y, d <= size / 2f ? Color.white : Color.clear);
//            }
//        }
//        tex.Apply();

//        circleSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
//        return circleSprite;
//    }

//    void OnDestroy()
//    {
//        foreach (BlackHoleData bh in blackHoles)
//        {
//            if (bh.visual != null) Destroy(bh.visual);
//        }
//    }

//    // ---------------- 유틸 ----------------

//    private ThrowState GetState(GameObject obj)                     // 상태 가져오기 (없으면 새로 만듦)
//    {
//        if (states.TryGetValue(obj, out ThrowState st)) return st;

//        Boss_Missile missile = obj.GetComponent<Boss_Missile>();
//        Rigidbody2D rb = obj.GetComponent<Rigidbody2D>();
//        if (missile == null && rb == null) return null;             // 움직이는 오브젝트가 아니면 제외

//        st = new ThrowState();
//        st.tf = obj.transform;
//        st.missile = missile;
//        st.rb = rb;
//        states.Add(obj, st);
//        return st;
//    }

//    private void CleanStates()                                      // 사라진 오브젝트 정리
//    {
//        List<GameObject> dead = null;
//        foreach (KeyValuePair<GameObject, ThrowState> kv in states)
//        {
//            if (kv.Key == null)
//            {
//                if (dead == null) dead = new List<GameObject>();
//                dead.Add(kv.Key);
//            }
//        }

//        if (dead == null) return;
//        foreach (GameObject key in dead) states.Remove(key);
//    }

//    private void RefreshEnemies()                                   // 적(Enemy + Boss) 목록 갱신
//    {
//        enemies.Clear();
//        enemies.AddRange(FindTagged(enemyTag));
//        enemies.AddRange(FindTagged(bossTag));
//    }

//    // 플레이어가 바라보는 방향(transform.up)과 가장 가까운 방향의 적
//    private Transform FindEnemyInFront()
//    {
//        Transform best = null;
//        float bestDot = -2f;

//        foreach (GameObject e in enemies)
//        {
//            if (e == null) continue;

//            Vector2 toEnemy = (e.transform.position - transform.position).normalized;
//            float dot = Vector2.Dot(transform.up, toEnemy);
//            if (dot > bestDot)
//            {
//                bestDot = dot;
//                best = e.transform;
//            }
//        }
//        return best;
//    }

//    private Transform FindNearestEnemy(Vector2 from)
//    {
//        Transform best = null;
//        float bestDist = float.MaxValue;

//        foreach (GameObject e in enemies)
//        {
//            if (e == null) continue;

//            float dist = Vector2.Distance(from, e.transform.position);
//            if (dist < bestDist)
//            {
//                bestDist = dist;
//                best = e.transform;
//            }
//        }
//        return best;
//    }

//    private bool IsOnScreen(Vector3 worldPos)
//    {
//        Camera cam = Camera.main;
//        if (cam == null) return true;

//        Vector3 v = cam.WorldToViewportPoint(worldPos);
//        return v.z > 0f && v.x >= 0f && v.x <= 1f && v.y >= 0f && v.y <= 1f;
//    }

//    // 태그 오브젝트 찾기
//    private GameObject[] FindTagged(string tag)
//    {
//        try
//        {
//            return GameObject.FindGameObjectsWithTag(tag);
//        }
//        catch (UnityException)
//        {
//            if (warnedTags.Add(tag))
//                Debug.LogWarning($"'{tag}' 태그가 없습니다");
//            return new GameObject[0];
//        }
//    }
//}