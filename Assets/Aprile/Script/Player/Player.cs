using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("이동 설정")]
    [SerializeField] private float moveSpeed = 6f;
    [SerializeField] private float slowSpeed = 2.5f; // Shift 누르면 저속 이동 (탄막 게임 필수)

    [Header("이동 범위 (월드 좌표)")]
    [SerializeField] private Vector2 minBounds = new Vector2(-8f, -4.5f);
    [SerializeField] private Vector2 maxBounds = new Vector2(8f, 4.5f);

    private void Update()
    {
        // WASD 입력 (GetAxisRaw는 가속 없이 즉시 반응)
        float x = Input.GetAxisRaw("Horizontal"); // A, D
        float y = Input.GetAxisRaw("Vertical");   // W, S

        // normalized: 대각선 이동 시 속도가 빨라지는 것 방지
        Vector2 direction = new Vector2(x, y).normalized;

        // 저속 모드
        float speed = Input.GetKey(KeyCode.LeftShift) ? slowSpeed : moveSpeed;

        // 이동
        Vector3 newPos = transform.position + (Vector3)(direction * speed * Time.deltaTime);

        // 범위 제한
        newPos.x = Mathf.Clamp(newPos.x, minBounds.x, maxBounds.x);
        newPos.y = Mathf.Clamp(newPos.y, minBounds.y, maxBounds.y);

        transform.position = newPos;
    }
}