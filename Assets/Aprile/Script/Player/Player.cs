using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("이동 설정")]
    [SerializeField] private float moveSpeed = 6f;
    [SerializeField] private float slowSpeed = 2.5f;   // Shift 누르면 저속 이동

    [Header("회전 설정")]
    [SerializeField] private float rotateSpeed = 180f;      // 초당 회전 각도(도)
    [SerializeField] private float slowRotateSpeed = 90f;   // 저속 모드 회전 속도

    [Header("이동 범위 (월드 좌표)")]
    [SerializeField] private Vector2 minBounds = new Vector2(-8f, -4.5f);
    [SerializeField] private Vector2 maxBounds = new Vector2(8f, 4.5f);

    private void Update()
    {
        float turn = Input.GetAxisRaw("Horizontal");     // A(-1), D(+1)
        float forward = Input.GetAxisRaw("Vertical");    // W(+1), S(-1)

        bool isSlow = Input.GetKey(KeyCode.LeftShift);
        float speed = isSlow ? slowSpeed : moveSpeed;
        float rotSpeed = isSlow ? slowRotateSpeed : rotateSpeed;

        // 회전: D는 오른쪽(시계 방향), A는 왼쪽(반시계 방향)
        // 2D에서 Z축 양의 회전이 반시계 방향이므로 부호를 반전
        transform.Rotate(0f, 0f, -turn * rotSpeed * Time.deltaTime);

        // 이동: 플레이어가 바라보는 방향(위쪽)을 기준으로 전진/후진
        Vector3 newPos = transform.position + transform.up * (forward * speed * Time.deltaTime);

        // 범위 제한
        newPos.x = Mathf.Clamp(newPos.x, minBounds.x, maxBounds.x);
        newPos.y = Mathf.Clamp(newPos.y, minBounds.y, maxBounds.y);

        transform.position = newPos;
    }
}