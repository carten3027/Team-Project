// =========================================================================================
// [1. 네임스페이스(Namespace) 불러오기]
// 네임스페이스란? 연관된 도구(클래스/메서드)들을 묶어놓은 '서류 가방'입니다.
// =========================================================================================
using System;                   // C# 기본 핵심 도구 모음 (Serializable, Type 변환 등)
using System.Collections.Generic; // 가변형 자료구조 도구 모음 (List<T> 등)
using System.Linq;              // LINQ (데이터 정렬, 필터링, 가공용 쿼리문 기능)
using UnityEngine;              // 유니티 엔진 핵심 기능 (MonoBehaviour, PlayerPrefs, Debug 등)

// =========================================================================================
// [2. 열거형 (enum : Enumeration)]
// - 개념: 숫자에 '직관적인 이름'을 붙여주는 C#의 기본 데이터 타입입니다.
// - 이유: 코드 내에서 숫자 1, 2 대신 PartType.ForceHoming 처럼 작성하여 가독성을 높이고 실수를 방지합니다.
// =========================================================================================
public enum PartType
{
    None = 0,        // 장착 안 됨 / 없음 (0번으로 기본값 지정)
    ForceHoming = 1, // 강제 유도 (1번)
    TimeStop = 2,    // 시간 정지 (2번)
    Reflect = 3,     // 공격 반사 (3번)
    BlackHole = 4,   // 블랙홀 (4번)
    SlowField = 5    // 감속장 (5번)
}

// =========================================================================================
// [3. 데이터 클래스 & 어트리뷰트(Attribute)]
// - 클래스(Class): 관련 데이터(이름, 점수)와 기능을 묶는 설계도입니다.
// - [Serializable] (어트리뷰트): C# 객체 데이터를 유니티 인스펙터(Inspector) 창에 노출시키고,
//   텍스트(JSON)로 변환할 수 있도록 "직렬화"를 허가하는 표식입니다.
// =========================================================================================
[Serializable]
public class RankEntry
{
    public string playerName; // 플레이어 이름 (string: 텍스트 문자열 타입)
    public int score;         // 플레이어 점수 (int: 정수 숫자 타입)
}

// =========================================================================================
// [4. 래퍼 클래스 (Wrapper Class) 패턴]
// - 유니티 JsonUtility의 한계: List<T> 데이터는 단독으로 최상위에 있으면 JSON 변환을 못합니다.
// - 해결법: 리스트를 일반 클래스로 한 번 감싸서(Wrap) 저장 가능한 형태로 만드는 테크닉입니다.
// =========================================================================================
[Serializable]
public class RankListWrapper
{
    // List<RankEntry>: RankEntry 객체들을 담을 수 있는 가변 길이의 리스트
    public List<RankEntry> rankings = new List<RankEntry>();
}

// =========================================================================================
// [5. DataManager 메인 클래스]
// - MonoBehaviour 상속: 이 클래스가 유니티 게임 오브젝트에 컴포넌트로 붙을 수 있게 만들어 줍니다.
// =========================================================================================
public class DataManager : MonoBehaviour
{
    // -------------------------------------------------------------------------------------
    // [싱글톤 패턴 (Singleton Pattern)]
    // - 개념: 게임 내에서 이 객체의 인스턴스(복사본)가 오직 '하나만' 존재하도록 보장하는 디자인 패턴.
    // - static: 메모리의 '정적 영역'에 할당되어 DataManager.Instance 로 전역 어디서나 접근 가능.
    // - 프로퍼티 { get; private set; }: 읽기는 외부(public) 허용, 값 수정은 내부(private)로 보호.
    // -------------------------------------------------------------------------------------
    public static DataManager Instance { get; private set; }

    // [Header("...")] : 유니티 인스펙터 창에서 변수들을 시각적으로 그룹화해주는 어트리뷰트
    [Header("현재 플레이 데이터 (사망 및 스테이지 전환 시 유지)")]
    public int currentStage = 1; // 현재 진행 스테이지
    public int currentScore = 0; // 현재 누적 점수
    public int money = 0;        // 보유 재화(골드)

    [Header("파츠 보유 및 퀵슬롯 (최대 2개 제한)")]
    // List<T> (가변 리스트): 크기가 자유롭게 늘어나고 줄어드는 리스트 (보유 파츠 관리용)
    public List<PartType> ownedParts = new List<PartType>();

    // Array (고정 배열 : T[]): 크기가 딱 2개로 고정된 공간 (퀵슬롯 1번, 2번 전용)
    public PartType[] quickSlots = new PartType[2] { PartType.None, PartType.None };

    [Header("랭킹 데이터 (Top 5 내림차순)")]
    public List<RankEntry> rankList = new List<RankEntry>();

    // const (상수): 한 번 정하면 절대로 값이 바뀌지 않는 컴파일 타임 변수 (오타 방지용 Key 값)
    private const string SAVE_KEY = "SpaceShooter_SaveData";
    private const string RANK_KEY = "SpaceShooter_RankData";

    // -------------------------------------------------------------------------------------
    // [Awake() : 유니티 생명주기(Life Cycle) 함수]
    // - 스크립트가 실행될 때 Start()보다 먼저, 딱 1번 실행되는 초기화 타임라인 함수입니다.
    // -------------------------------------------------------------------------------------
    private void Awake()
    {
        // 싱글톤 검사 로직
        if (Instance == null) // 1. 내가 최초로 생성된 DataManager라면
        {
            Instance = this; // 정적 변수에 자기 자신을 등록

            // DontDestroyOnLoad: 씬(Scene)이 변경되어도 이 게임 오브젝트를 파괴하지 않고 유지함
            DontDestroyOnLoad(gameObject);

            LoadRankings(); // 게임 시작 시 랭킹 데이터를 로드
        }
        else // 2. 이미 기존에 등록된 DataManager가 존재한다면 (씬 이동 후 중복 생성 시)
        {
            Destroy(gameObject); // 나중에 생성된 자신(중복 객체)을 삭제하여 오직 1개만 유지
        }
    }

    // -------------------------------------------------------------------------------------
    // [파츠 상점 로직]
    // - bool: 참(true) 또는 거짓(false)을 반환하는 조건형 리턴 타입
    // -------------------------------------------------------------------------------------
    public bool BuyPart(PartType part, int price)
    {
        // Early Exit 패턴: 조건에 맞지 않으면 즉시 함수를 종료하여 불필요한 계산을 방지
        if (ownedParts.Contains(part)) return false; // List.Contains(): 리스트에 특정 요소가 있는지 검사
        if (money < price) return false;             // 돈이 부족하면 구매 불가

        money -= price;       // 복합 대입 연산자: money = money - price 와 동일
        ownedParts.Add(part); // List.Add(): 리스트 맨 뒤에 요소 추가

        // 문자열 보간 ($"..."): 텍스트 내에 {변수}를 직접 삽입하는 C#의 편리한 문법
        Debug.Log($"{part} 파츠 구매 완료! 남은 돈: {money}");
        return true;          // 구매 성공 반환
    }

    // -------------------------------------------------------------------------------------
    // [퀵슬롯 파츠 장착 로직]
    // -------------------------------------------------------------------------------------
    public void EquipPartToSlot(int slotIndex, PartType part)
    {
        // 예외 처리: 배열의 범위를 벗어난 인덱스가 들어오면 무시 (0, 1번 슬롯만 허용)
        if (slotIndex < 0 || slotIndex >= 2) return;

        // 논리 연산자 (!, &&): 보유 중이 아니면서(&&) 장착 해제(None) 목적도 아닌(!) 경우 차단
        if (!ownedParts.Contains(part) && part != PartType.None) return;

        // 삼항 연산자 (조건식 ? 참일때 : 거짓일때)
        // slotIndex가 0이면 상대방 슬롯은 1, slotIndex가 1이면 상대방 슬롯은 0
        int otherSlot = slotIndex == 0 ? 1 : 0;

        // 중복 장착 방지: 상대방 슬롯에 내가 장착하려는 파츠가 이미 있다면 그쪽 슬롯을 해제(None)
        if (quickSlots[otherSlot] == part)
        {
            quickSlots[otherSlot] = PartType.None;
        }

        // 선택한 슬롯에 파츠 장착
        quickSlots[slotIndex] = part;
        Debug.Log($"퀵슬롯 [{slotIndex + 1}]에 {part} 장착!");
    }

    // -------------------------------------------------------------------------------------
    // [세이브 / 로드 로직]
    // -------------------------------------------------------------------------------------

    // 표현식 본문 (Expression-bodied member, =>): 중괄호{ return ...; }를 줄여 쓰는 한 줄 표현법
    // PlayerPrefs.HasKey(): 해당 키 이름으로 저장된 데이터가 저장소에 존재하는지 검사
    public bool HasSaveFile() => PlayerPrefs.HasKey(SAVE_KEY);

    public void SaveGame()
    {
        // 1. LINQ의 Select(): ownedParts 내 enum 요소들을 하나씩 꺼내 int 정수로 변환(캐스팅)
        // 2. string.Join(",", ...): 변환된 정수들을 반점(,)을 구분자로 이어붙여 문자열 생성 (예: "1,3")
        string ownedStr = string.Join(",", ownedParts.Select(p => (int)p));

        // 파이프(|) 기호를 구분자(Delimiter)로 활용하여 세이브 데이터를 하나의 단일 텍스트로 합성
        // 예: "1|100|500|1|3|1,3" (스테이지|점수|돈|슬롯0|슬롯1|보유파츠)
        string saveData = $"{currentStage}|{currentScore}|{money}|{(int)quickSlots[0]}|{(int)quickSlots[1]}|{ownedStr}";

        // PlayerPrefs: 유니티에서 제공하는 레지스트리 기반 간단한 로컬 데이터 저장소 (Key-Value)
        PlayerPrefs.SetString(SAVE_KEY, saveData);
        PlayerPrefs.Save(); // 디스크 하드에 즉시 반영
        Debug.Log("게임 세이브 완료!");
    }

    public void LoadGame()
    {
        if (!HasSaveFile()) return; // 저장 파일이 없으면 로드 중단

        // string.Split('|'): 파이프(|) 구분자를 기준으로 텍스트를 자르고 string[] 배열로 분할 반환
        string[] data = PlayerPrefs.GetString(SAVE_KEY).Split('|');

        // int.Parse(): 문자열 형태의 숫자 "100"을 실제 계산 가능한 정수 100 으로 형변환(Parsing)
        currentStage = int.Parse(data[0]);
        currentScore = int.Parse(data[1]);
        money = int.Parse(data[2]);

        // (PartType)int.Parse(...): 정수를 다시 열거형(Enum)으로 명시적 형변환(Casting)
        quickSlots[0] = (PartType)int.Parse(data[3]);
        quickSlots[1] = (PartType)int.Parse(data[4]);

        ownedParts.Clear(); // 기존 보유 리스트 비우기

        // string.IsNullOrEmpty(): 문자열이 비어있거나 null인지 검사
        if (!string.IsNullOrEmpty(data[5]))
        {
            // foreach 반복문: 구분자 반점(,)으로 잘라낸 데이터 배열을 하나씩 순회
            foreach (var p in data[5].Split(','))
            {
                ownedParts.Add((PartType)int.Parse(p));
            }
        }
        Debug.Log($"세이브 로드 완료! 스테이지: {currentStage}, 보유 돈: {money}");
    }

    // -------------------------------------------------------------------------------------
    // [랭킹 시스템 로직]
    // -------------------------------------------------------------------------------------

    public bool IsTop5Score(int score)
    {
        // 랭킹 리스트 개수(Count)가 5개 미만이면 무조건 랭킹 진입
        if (rankList.Count < 5) return true;

        // LINQ의 Last(): 내림차순 정렬되어 있으므로 리스트의 맨 마지막(5위) 점수를 가져옴
        return score > rankList.Last().score;
    }

    public void AddRanking(string playerName, int score)
    {
        // 새로운 랭킹 항목(객체) 생성 후 추가
        rankList.Add(new RankEntry { playerName = playerName, score = score });

        // [LINQ 체이닝 기법]
        // 1. OrderByDescending(r => r.score) : 점수 기준 내림차순 정렬 (높은 점수 -> 낮은 점수)
        // 2. Take(5)                         : 정렬된 목록 중 맨 위에서 5개만 추출
        // 3. ToList()                        : 결과를 다시 List<RankEntry> 형태로 변환
        rankList = rankList.OrderByDescending(r => r.score).Take(5).ToList();

        // [JSON 직렬화]
        // RankListWrapper에 담은 후 JsonUtility.ToJson()으로 클래스 객체를 JSON 텍스트 문자열로 변환
        RankListWrapper wrapper = new RankListWrapper { rankings = rankList };
        PlayerPrefs.SetString(RANK_KEY, JsonUtility.ToJson(wrapper));
        PlayerPrefs.Save();

        Debug.Log($"랭킹 등록 완료: {playerName} - {score}점");
    }

    private void LoadRankings()
    {
        if (PlayerPrefs.HasKey(RANK_KEY))
        {
            // JsonUtility.FromJson<T>(): 저장된 JSON 문자열을 원래 C# 객체 형태로 복원 (역직렬화)
            RankListWrapper wrapper = JsonUtility.FromJson<RankListWrapper>(PlayerPrefs.GetString(RANK_KEY));

            if (wrapper != null) rankList = wrapper.rankings;
        }
    }
}

// =========================================================================================
// [DataManager.cs 코드 전체 용어 & 문법 완벽 정리 사전]
// =========================================================================================
//
// 1. 네임스페이스 및 클래스 선언부 (Namespaces & Classes)
// - using System;
//   : C#의 가장 기본이 되는 기능들([Serializable] 표시, 데이터 형변환 등)을 쓰겠다는 선언입니다.
// - using System.Collections.Generic;
//   : 데이터를 여러 개 담는 가변 바구니인 'List<T>'를 사용하기 위해 반드시 적어줘야 하는 선언입니다.
// - using System.Linq;
//   : 리스트 안의 데이터를 정렬(OrderByDescending)하거나 변환(Select), 추출(Take)하는 강력한 검색 도구(LINQ)를 부릅니다.
// - enum (열거형)
//   : 숫자에 별명을 붙여주는 문법입니다. 예를 들어 강제 유도 파츠를 숫자 '1'로 외우면 헷갈리므로 'PartType.ForceHoming'이라는 이름을 붙여 코드에서 읽기 쉽게 만듭니다.
// - [Serializable] (직렬화 어트리뷰트)
//   : 내가 직접 만든 일반 클래스(RankEntry, RankListWrapper)는 기본적으로 유니티 인스펙터 창에 보이지 않고 저장도 안 됩니다.
//   : 클래스 위에 [Serializable] 딱지를 붙여주면 "이 클래스 안의 변수들을 인스펙터 창에 보여주고, 파일(JSON)로 저장할 수 있게 허락한다!"는 뜻이 됩니다.
//
// -----------------------------------------------------------------------------------------
// 2. 싱글톤과 변수 제어 키워드 (Singleton & Keywords)
// - static (정적 키워드)
//   : 이 변수나 함수를 컴퓨터 메모리의 '공용 공간'에 딱 하나만 고정시켜 두겠다는 뜻입니다.
//   : 그래서 다른 스크립트에서 복잡하게 찾지 않고도 'DataManager.Instance'라고 이름만 부르면 즉시 접근할 수 있습니다.
// - { get; private set; } (프로퍼티 접근 제어)
//   : get(가져와서 읽기)은 누구나(public) 할 수 있지만, private set(새로운 값 덮어쓰기)은 오직 이 DataManager 스크립트 내부에서만 할 수 있도록 잠가두는 안전장치입니다.
// - this
//   : 코드가 실행되고 있는 '자기 자신(이 스크립트 컴포넌트)'을 가리키는 대명사입니다. (Instance = this; 는 "내가 바로 그 공용 DataManager다!"라고 등록하는 코드입니다.)
// - DontDestroyOnLoad(gameObject);
//   : 유니티는 원래 새로운 씬(예: 1스테이지 -> 상점 씬 -> 2스테이지)으로 넘어가면 이전 씬의 모든 오브젝트를 파괴합니다.
//   : 하지만 이 함수 안에 gameObject(자기 자신)를 넣어주면, 씬이 바뀌어도 절대 사라지지 않고 돈과 파츠 데이터를 끝까지 들고 살아남습니다.
// - const (상수)
//   : 한 번 값을 정하면 게임이 끝날 때까지 절대 바꿀 수 없는 변수입니다. 세이브 파일 이름("SpaceShooter_SaveData")처럼 오타가 나면 안 되는 고정 글자를 저장할 때 씁니다.
//
// -----------------------------------------------------------------------------------------
// 3. 자료구조 비교: List(리스트) vs Array(배열)
// - List<PartType> ownedParts = new List<PartType>();
//   : '고무줄 바구니'입니다. 처음엔 비어있지만 상점에서 파츠를 살 때마다 .Add()로 칸을 계속 늘릴 수 있고, .Clear()로 싹 비울 수도 있습니다.
//   : .Contains(찾을값) 함수를 쓰면 바구니 안에 해당 파츠가 들어있는지 0.001초 만에 찾아내어 참(true)/거짓(false)으로 알려줍니다.
//   : .Count 속성은 현재 리스트 안에 몇 개의 데이터가 들어있는지 개수를 알려줍니다.
// - PartType[] quickSlots = new PartType[2];
//   : '칸이 딱 2개로 고정된 도시락통'입니다. 기획서에서 퀵슬롯은 무조건 2개라고 했으므로, 크기가 변하지 않는 배열([])을 썼습니다.
//   : 컴퓨터는 숫자를 0부터 세기 때문에 첫 번째 칸은 quickSlots[0], 두 번째 칸은 quickSlots[1]로 부릅니다.
//
// -----------------------------------------------------------------------------------------
// 4. 연산자 및 조건/반복 문법 (Operators & Loops)
// - -= (복합 빼기 연산자)
//   : 'money -= price;'는 'money = money - price;'와 완전히 같은 뜻입니다. 내 돈에서 파츠 가격만큼 빼서 다시 내 돈에 저장합니다.
// - || (OR 논리 연산자) / && (AND 논리 연산자) / ! (NOT 부정 연산자)
//   : 'A || B'는 A와 B 중 하나라도 참이면 실행합니다. (예: slotIndex < 0 || slotIndex >= 2 는 슬롯 번호가 0보다 작거나 2 이상이면 잘못된 번호이므로 차단!)
//   : 'A && B'는 둘 다 참이어야 실행합니다.
//   : '!조건'은 조건의 결과를 반대로 뒤집습니다. (!ownedParts.Contains(part) 는 "보유 파츠 목록에 들어있지 않다면!"이라는 뜻입니다.)
// - 조건식 ? 참일때값 : 거짓일때값 (삼항 연산자)
//   : if~else 문을 딱 한 줄로 압축한 문법입니다.
//   : 'int otherSlot = slotIndex == 0 ? 1 : 0;' -> 지금 선택한 슬롯(slotIndex)이 0번이니? 맞으면(?) 상대 슬롯은 1번이고, 아니면(:) 상대 슬롯은 0번이다!
// - => (람다식 / 화살표 함수)
//   : 1) 함수 옆에 쓰일 때(public bool HasSaveFile() => ...): 중괄호와 return을 생략하고 "이 함수는 화살표 뒤의 결과를 바로 돌려준다"는 뜻입니다.
//   : 2) LINQ 안에 쓰일 때(p => (int)p 또는 r => r.score): 리스트 안의 요소를 하나씩 꺼내서 'p'나 'r'이라고 부를 때, "p를 (int)p로 바꿔라", "r에서 r.score(점수)만 봐라"라고 지시하는 화살표입니다.
// - foreach (var p in 배열또는리스트) { ... }
//   : 바구니(배열/리스트) 안에 들어있는 내용물을 처음부터 끝까지 하나씩 꺼내서 'p'라는 임시 변수에 담으며 중괄호 안의 작업을 반복하는 문법입니다.
//
// -----------------------------------------------------------------------------------------
// 5. 문자열 가공 및 형변환 (String & Type Casting) - 세이브/로드의 핵심!
// - $"텍스트 {변수명}" (문자열 보간)
//   : 큰따옴표 앞에 달러($) 기호를 붙이면, 따옴표 안에서 중괄호 { }를 쳐서 변수 안의 실제 값을 글자 사이에 바로 끼워 넣을 수 있습니다.
// - (int)part / (PartType)숫자 (명시적 형변환, Casting)
//   : 열거형(PartType)과 정수(int)는 서로 짝꿍입니다. 변수 앞에 (int)를 붙이면 열거형 이름을 숫자(예: ForceHoming -> 1)로 바꿔주고, 숫자 앞에 (PartType)을 붙이면 숫자를 다시 열거형 이름으로 복구해 줍니다.
// - string.Join("구분자", 목록)
//   : 여러 개의 글자나 숫자 목록 사이에 "구분자"(예: 반점 ",")를 끼워 넣어서 하나의 긴 문자열로 합쳐줍니다. (예: 1, 3 -> "1,3")
// - 문자열.Split('구분자')
//   : 합쳐진 긴 문자열에서 '구분자'(예: '|' 또는 ',')를 찾아 가위질하듯 싹둑 잘라 여러 조각의 배열(string[])로 만들어 줍니다.
// - int.Parse("숫자글자")
//   : 컴퓨터에게 글자 "100"(string)과 숫자 100(int)은 완전히 다른 데이터입니다. 글자 "100"은 더하기/빼기 계산을 못하므로, int.Parse("100")을 통해 실제 계산 가능한 숫자 100으로 번역해 줍니다.
// - string.IsNullOrEmpty(문자열)
//   : 해당 문자열이 아예 존재하지 않거나(null), 글자가 한 글자도 없는 빈칸("")인지 검사해 주는 안전한 내장 함수입니다.
//
// -----------------------------------------------------------------------------------------
// 6. 데이터 저장소 및 랭킹 정렬 (PlayerPrefs, LINQ, JsonUtility)
// - PlayerPrefs (플레이어 프렙스)
//   : 유니티가 기본 제공하는 '간단한 내장 수첩'입니다. 게임을 껐다 켜도 컴퓨터(레지스트리)에 데이터가 남아있게 해줍니다.
//   : .SetString("열쇠이름", "저장할내용") -> 열쇠 이름으로 내용을 기록합니다.
//   : .GetString("열쇠이름") -> 열쇠 이름으로 기록해 둔 내용을 꺼내옵니다.
//   : .HasKey("열쇠이름") -> 해당 열쇠 이름으로 저장된 세이브 파일이 있는지 없는지 참/거짓으로 확인합니다.
//   : .Save() -> 기록한 내용을 하드디스크에 최종 도장 찍듯 완벽히 저장합니다.
// - LINQ 정렬 3단 콤보 (rankList.OrderByDescending(r => r.score).Take(5).ToList())
//   : 1) .OrderByDescending(r => r.score) : 랭킹 목록(rankList)을 점수(score) 기준으로 높은 사람부터 낮은 사람 순(내림차순)으로 줄 세웁니다.
//   : 2) .Take(5) : 줄 세운 사람들 중 1등부터 5등까지만 딱 5명을 잘라냅니다. (6등부터는 자동 탈락)
//   : 3) .ToList() : 잘라낸 5명을 다시 List<RankEntry> 바구니 형태로 포장해서 반환합니다.
//   : .Last() : 리스트의 맨 마지막 칸에 있는 데이터(여기서는 꼴찌인 5등)를 가져옵니다.
// - JsonUtility.ToJson(객체) / JsonUtility.FromJson<타입>(JSON문자열)
//   : JSON(제이슨)은 복잡한 클래스 데이터(이름+점수 묶음 리스트 등)를 인터넷이나 파일로 주고받기 좋게 정해진 규칙의 텍스트로 변환하는 세계 표준 포맷입니다.
//   : ToJson()은 C# 클래스 객체를 저장 가능한 JSON 문자열로 압축(직렬화)해주고, FromJson<RankListWrapper>()는 저장된 문자열을 다시 원래의 C# 클래스 객체로 압축 해제(역직렬화)해 줍니다.
// =========================================================================================