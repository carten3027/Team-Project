using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class CheatManager : MonoBehaviour
{
    public static CheatManager Instance { get; private set; }

    [Header("치트키 (F1 ~ F12 중 자유롭게 설정)")]
    [SerializeField] private KeyCode debugModeKey = KeyCode.F1;
    [SerializeField] private KeyCode invincibleKey = KeyCode.F2;
    [SerializeField] private KeyCode killEnemiesKey = KeyCode.F3;
    [SerializeField] private KeyCode addMoneyKey = KeyCode.F4;

    [Header("참조 (비워두면 자동으로 찾음)")]
    [SerializeField] private Player player;

    [Header("태그 설정")]
    [SerializeField] private string enemyTag = "Enemy";
    [SerializeField] private string enemyBulletTag = "EnemyBullet";

    [Header("돈 추가 설정")]
    [SerializeField] private int moneyAmount = 1000;
    [Tooltip("돈 시스템에 연결하세요. 인자로 추가할 금액이 전달됩니다.")]
    public UnityEvent<int> onAddMoney;

    [Header("디버그 모드 표시 설정")]
    [SerializeField] private float lineWidth = 0.04f;
    [SerializeField] private float refreshInterval = 0.1f;
    [SerializeField] private int circleSegments = 24;
    [SerializeField] private bool showStatusGUI = true;

    private bool debugMode;
    private bool invincible;

    private Material lineMaterial;
    private float refreshTimer;
    private readonly List<Collider2D> colliders = new List<Collider2D>();
    private readonly Dictionary<Collider2D, LineRenderer> lines = new Dictionary<Collider2D, LineRenderer>();

    public bool IsDebugMode => debugMode;
    public bool IsInvincible => invincible;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        lineMaterial = new Material(Shader.Find("Sprites/Default"));
    }

    private void Start()
    {
        if (player == null)
            player = FindObjectOfType<Player>();
    }

    private void Update()
    {
        if (Input.GetKeyDown(debugModeKey)) ToggleDebugMode();
        if (Input.GetKeyDown(invincibleKey)) ToggleInvincible();
        if (Input.GetKeyDown(killEnemiesKey)) KillEnemiesOnScreen();
        if (Input.GetKeyDown(addMoneyKey)) AddMoney();
    }

    private void LateUpdate()
    {
        if (debugMode) UpdateDebugLines();
    }

    // ---------------- F1: 디버그 모드 (온/오프) ----------------
    private void ToggleDebugMode()
    {
        debugMode = !debugMode;
        Debug.Log($"[치트] 디버그 모드: {(debugMode ? "ON" : "OFF")}");

        if (debugMode)
            refreshTimer = refreshInterval; // 켜자마자 바로 갱신
        else
            ClearDebugLines();
    }

    private void UpdateDebugLines()
    {
        refreshTimer += Time.deltaTime;
        if (refreshTimer >= refreshInterval)
        {
            refreshTimer = 0f;
            RefreshColliderList();
        }

        foreach (Collider2D col in colliders)
        {
            if (col == null || !col.enabled || !col.gameObject.activeInHierarchy) continue;

            LineRenderer lr = GetOrCreateLine(col);
            lr.enabled = true;
            DrawCollider(col, lr);
        }
    }

    private void RefreshColliderList()
    {
        colliders.Clear();
        colliders.AddRange(FindObjectsOfType<Collider2D>());

        // 사라진 콜라이더의 선 제거
        HashSet<Collider2D> alive = new HashSet<Collider2D>(colliders);
        List<Collider2D> removeList = null;

        foreach (var pair in lines)
        {
            if (pair.Key == null || !alive.Contains(pair.Key))
            {
                if (pair.Value != null) Destroy(pair.Value.gameObject);
                if (removeList == null) removeList = new List<Collider2D>();
                removeList.Add(pair.Key);
            }
        }

        if (removeList != null)
        {
            foreach (var key in removeList)
                lines.Remove(key);
        }
    }

    private LineRenderer GetOrCreateLine(Collider2D col)
    {
        if (lines.TryGetValue(col, out LineRenderer existing) && existing != null)
            return existing;

        GameObject go = new GameObject("DebugCollider");
        go.transform.SetParent(transform, false);

        LineRenderer lr = go.AddComponent<LineRenderer>();
        lr.useWorldSpace = true;
        lr.loop = true;
        lr.startWidth = lineWidth;
        lr.endWidth = lineWidth;
        lr.material = lineMaterial;
        lr.sortingOrder = 1000;

        Color c = GetColor(col);
        lr.startColor = c;
        lr.endColor = c;

        lines[col] = lr;
        return lr;
    }

    private Color GetColor(Collider2D col)
    {
        if (col.GetComponentInParent<Player>() != null) return Color.green;
        if (col.CompareTag(enemyTag)) return Color.red;
        if (col.CompareTag(enemyBulletTag)) return Color.yellow;
        return Color.cyan;
    }

    private void DrawCollider(Collider2D col, LineRenderer lr)
    {
        Transform t = col.transform;

        if (col is CircleCollider2D circle)
        {
            float r = circle.radius * Mathf.Max(Mathf.Abs(t.lossyScale.x), Mathf.Abs(t.lossyScale.y));
            Vector3 center = t.TransformPoint(circle.offset);

            lr.positionCount = circleSegments;
            for (int i = 0; i < circleSegments; i++)
            {
                float angle = (i / (float)circleSegments) * Mathf.PI * 2f;
                lr.SetPosition(i, center + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * r);
            }
        }
        else if (col is BoxCollider2D box)
        {
            Vector2 h = box.size * 0.5f;
            Vector2 o = box.offset;

            lr.positionCount = 4;
            lr.SetPosition(0, t.TransformPoint(o + new Vector2(-h.x, -h.y)));
            lr.SetPosition(1, t.TransformPoint(o + new Vector2(-h.x, h.y)));
            lr.SetPosition(2, t.TransformPoint(o + new Vector2(h.x, h.y)));
            lr.SetPosition(3, t.TransformPoint(o + new Vector2(h.x, -h.y)));
        }
        else if (col is PolygonCollider2D poly && poly.pathCount > 0)
        {
            Vector2[] path = poly.GetPath(0);

            lr.positionCount = path.Length;
            for (int i = 0; i < path.Length; i++)
                lr.SetPosition(i, t.TransformPoint(path[i] + poly.offset));
        }
        else
        {
            // 그 외 콜라이더(Capsule 등)는 bounds 사각형으로 표시
            Bounds b = col.bounds;
            lr.positionCount = 4;
            lr.SetPosition(0, new Vector3(b.min.x, b.min.y, 0f));
            lr.SetPosition(1, new Vector3(b.min.x, b.max.y, 0f));
            lr.SetPosition(2, new Vector3(b.max.x, b.max.y, 0f));
            lr.SetPosition(3, new Vector3(b.max.x, b.min.y, 0f));
        }
    }

    private void ClearDebugLines()
    {
        foreach (var pair in lines)
        {
            if (pair.Value != null) Destroy(pair.Value.gameObject);
        }
        lines.Clear();
        colliders.Clear();
    }

    // ---------------- F2: 무적 (온/오프) ----------------
    private void ToggleInvincible()
    {
        invincible = !invincible;
        Debug.Log($"[치트] 무적: {(invincible ? "ON" : "OFF")}");

        if (player == null) player = FindObjectOfType<Player>();
        if (player != null) player.IsCheatInvincible = invincible;
    }

    // ---------------- F3: 강제 적 사망 (일회성) ----------------
    private void KillEnemiesOnScreen()
    {
        Camera cam = Camera.main;
        if (cam == null) return;

        GameObject[] enemies = GameObject.FindGameObjectsWithTag(enemyTag);
        int count = 0;

        foreach (GameObject enemy in enemies)
        {
            Vector3 view = cam.WorldToViewportPoint(enemy.transform.position);
            bool visible = view.z > 0f && view.x >= 0f && view.x <= 1f && view.y >= 0f && view.y <= 1f;

            if (visible)
            {
                Destroy(enemy);
                count++;
            }
        }

        Debug.Log($"[치트] 강제 적 사망: {count}마리");
    }

    // ---------------- F4: 돈 추가 (일회성) ----------------
    private void AddMoney()
    {
        onAddMoney?.Invoke(moneyAmount);
        Debug.Log($"[치트] 돈 +{moneyAmount}");
    }

    // ---------------- 화면 상태 표시 ----------------
    private void OnGUI()
    {
        if (!showStatusGUI) return;

        GUI.Label(new Rect(10, 10, 300, 24),
            $"[F1] 디버그: {(debugMode ? "ON" : "OFF")}   [F2] 무적: {(invincible ? "ON" : "OFF")}");
        GUI.Label(new Rect(10, 30, 300, 24), "[F3] 적 즉사   [F4] 돈 추가");
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}