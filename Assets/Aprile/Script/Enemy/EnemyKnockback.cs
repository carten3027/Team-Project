using System.Collections;
using UnityEngine;

public class EnemyKnockback : MonoBehaviour
{
    private Coroutine knockbackRoutine;

    // 넉백 중인지 여부 (적 이동 스크립트에서 참고 가능)
    public bool IsKnockedBack { get; private set; }

    public void ApplyKnockback(Vector2 direction, float distance, float duration)
    {
        // OnTriggerStay2D에서 매 프레임 호출되므로, 넉백 중이면 무시
        if (IsKnockedBack) return;
        if (direction.sqrMagnitude < 0.0001f) return;

        knockbackRoutine = StartCoroutine(KnockbackRoutine(direction.normalized, distance, duration));
    }

    private IEnumerator KnockbackRoutine(Vector2 dir, float distance, float duration)
    {
        IsKnockedBack = true;

        Vector3 startPos = transform.position;
        Vector3 targetPos = startPos + (Vector3)(dir * distance);

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            // 처음엔 빠르게, 끝에서 부드럽게 감속 (ease-out)
            float eased = 1f - (1f - t) * (1f - t);

            transform.position = Vector3.Lerp(startPos, targetPos, eased);
            yield return null;
        }

        transform.position = targetPos;
        IsKnockedBack = false;
        knockbackRoutine = null;
    }

    private void OnDisable()
    {
        // 비활성화될 때 상태 초기화 (오브젝트 풀링 대비)
        IsKnockedBack = false;
        knockbackRoutine = null;
    }
}