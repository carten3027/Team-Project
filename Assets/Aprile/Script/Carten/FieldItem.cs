// =========================================================================================
// [1. 네임스페이스(Namespace) 불러오기]
// =========================================================================================
using UnityEngine;

// =========================================================================================
// [2. FieldItem 메인 클래스]
// =========================================================================================
public class FieldItem : MonoBehaviour
{
    [Header("아이템 속성 설정")]
    public DropItemType itemType = DropItemType.Heal;
    public float itemWeight = 2f;    // 아이템 무게
    public float effectValue = 1f;   // 효과 수치 (회복량 또는 지속시간)

    [Header("소멸 설정 (기획서 11-⑦, 13-④)")]
    public bool destroyAfterTime = true;
    public float lifeTime = 8f;

    private void Start()
    {
        if (destroyAfterTime)
        {
            Destroy(gameObject, lifeTime);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // 부딪힌 물체(또는 그 부모)에게서 Player 스크립트를 찾음
        Player player = other.GetComponentInParent<Player>();

        // 플레이어가 맞다면
        if (player != null)
        {
            if (InventoryAndItem.Instance != null)
            {
                // 1. 먼저 가방의 개수/무게 제한을 통과하는지 검사하며 가방에 추가 시도
                bool pickedUp = InventoryAndItem.Instance.TryAddItemToBag(itemType, itemWeight, effectValue);

                // 2. 가방 제한을 통과해서 습득에 성공했다면 실제 아이템 효과 발동!
                if (pickedUp)
                {
                    if (itemType == DropItemType.Heal)
                    {
                        player.Heal(Mathf.RoundToInt(effectValue));
                    }
                    else if (itemType == DropItemType.Invincible)
                    {
                        player.ActivateInvincibleItem(effectValue);
                    }
                    else if (itemType == DropItemType.DefenseUp)
                    {
                        player.ActivateDefenseUpItem(effectValue);
                    }

                    // 3. 필드에 있던 아이템 오브젝트 삭제
                    Destroy(gameObject);
                }
            }
        }
    }
}

// =========================================================================================
// [1단계 추가 코드 핵심 문법 & 용어 완벽 정리 사전]
// =========================================================================================
//
// 1. 코루틴(Coroutine)과 시간 대기 문법: IEnumerator & yield return new WaitForSeconds()
// - 왜 일반 함수(void) 대신 코루틴(IEnumerator)을 썼나요?
//   : 기획서에서 "일정 시간 무적, 일정 시간 방어력 강화"처럼 '몇 초 동안 유지되었다가 꺼지는 기능'을 요구했기 때문입니다.
//   : 일반 void 함수는 0.001초 만에 위에서 아래까지 한 번에 다 실행되고 끝나버려서 "3초 기다려라!"가 불가능합니다.
//   : 반면 IEnumerator 함수 안에서 'yield return new WaitForSeconds(3f);'라고 적으면,
//     게임 전체는 멈추지 않고 계속 돌아가면서 이 함수만 정확히 3초 동안 그 줄에서 잠시 멈춰 기다렸다가 다음 줄(isInvincible = false;)을 실행합니다!
// - StartCoroutine(함수이름());
//   : IEnumerator로 만든 시간차 함수(코루틴)를 실행시킬 때 반드시 붙여야 하는 전용 시동 스위치입니다.
//
// -----------------------------------------------------------------------------------------
// 2. 최대/최소값 제한 내장 함수: Mathf.Min() & Mathf.Max()
// - Mathf.Min(A, B)
//   : A와 B 중에서 '더 작은 값'을 골라줍니다.
//   : currentHp = Mathf.Min(maxHp, currentHp + amount);
//     -> 체력을 회복했을 때 최대 체력(예: 5)을 넘어서 6, 7이 되면 안 되므로, (최대 체력 5)와 (회복된 체력) 중 더 작은 값을 선택해 최대 체력을 못 넘게 막아줍니다.
// - Mathf.Max(A, B)
//   : 반대로 A와 B 중에서 '더 큰 값'을 골라줍니다.
//   : currentHp = Mathf.Max(0, currentHp - finalDamage);
//     -> 체력이 깎였을 때 마이너스(-1, -2)가 되지 않도록 최소한 0 이상만 남게 막아줍니다.
//
// -----------------------------------------------------------------------------------------
// 3. 부모까지 샅샅이 뒤져서 컴포넌트 찾기: other.GetComponentInParent<Player>()
// - 왜 CompareTag("Player") 대신 GetComponentInParent<Player>()를 썼나요?
//   : 동료분이 만든 Player 오브젝트나 프리팹 구조에서 콜라이더(충돌체)가 자식 오브젝트에 붙어있을 수도 있기 때문입니다.
//   : GetComponentInParent<Player>()를 쓰면 부딪힌 물체 본인은 물론 그 부모 오브젝트까지 샅샅이 뒤져서 Player 스크립트를 찾아내므로,
//     태그 설정 실수로 인해 아이템이 안 먹어지는 오류를 100% 예방할 수 있습니다.
// =========================================================================================