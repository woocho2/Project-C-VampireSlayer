using UnityEngine;
using System.Collections;

// 플레이어의 전투 행동(공격, 스킬, 턴 관리 등)을 총괄하는 클래스입니다.
[RequireComponent(typeof(UnitController))]
public class PlayerBattleController : MonoBehaviour
{
    private UnitController myUnit;                  // 내 유닛 정보
    private UnitController currentEnemy;            // 현재 상대하는 적 유닛
    private System.Action turnFinishedCallback;     // 턴 종료를 매니저에게 알리기 위한 콜백 함수
    private PlayerAniController aniController;      // 플레이어 애니메이션 컨트롤러

    private void Awake()
    {
        // 컴포넌트들을 미리 찾아 변수에 담아둡니다.
        myUnit = GetComponent<UnitController>();
        aniController = GetComponent<PlayerAniController>();
    }

    private void Start()
    {
        // 게임이 시작될 때 아군 턴이 아니므로 공격/스킬 버튼을 비활성화해 오작동을 방지합니다.
        if (BattleUIManager.Instance != null)
        {
            if (BattleUIManager.Instance.btn_attack != null) BattleUIManager.Instance.btn_attack.interactable = false;
            if (BattleUIManager.Instance.btn_skill != null) BattleUIManager.Instance.btn_skill.interactable = false;
        }
    }

    /// <summary>
    /// BattleManager가 아군의 턴일 때 호출하는 함수입니다.
    /// </summary>
    public void StartTurn(UnitController enemy, System.Action onTurnFinished)
    {
        currentEnemy = enemy;                 // 대상 적 설정
        turnFinishedCallback = onTurnFinished; // 턴 종료 콜백 저장

        Debug.Log($"{myUnit.unitName}의 턴! 명령을 대기합니다. (적 대상: {enemy.unitName})");

        // 턴이 시작되면 UI 버튼에 내 명령 함수들을 동적으로 연결하고 활성화합니다.
        if (BattleUIManager.Instance != null)
        {
            if (BattleUIManager.Instance.btn_attack != null)
            {
                BattleUIManager.Instance.btn_attack.onClick.RemoveAllListeners();
                BattleUIManager.Instance.btn_attack.onClick.AddListener(OnAttackCommand);
                BattleUIManager.Instance.btn_attack.interactable = true;
            }

            if (BattleUIManager.Instance.btn_skill != null)
            {
                BattleUIManager.Instance.btn_skill.onClick.RemoveAllListeners();
                BattleUIManager.Instance.btn_skill.onClick.AddListener(OnSkillCommand);
                BattleUIManager.Instance.btn_skill.interactable = true;
            }

            // 행동 UI를 서서히 화면에 띄웁니다.
            BattleUIManager.Instance.ShowPlayerActionUI(true);
        }
    }

    // 공격 버튼을 눌렀을 때 실행되는 함수
    public void OnAttackCommand()
    {
        // 적이나 콜백 데이터가 유효한지 검사합니다.
        if (currentEnemy == null || turnFinishedCallback == null)
        {
            Debug.LogError($"턴 데이터 누락 오류! currentEnemy: {currentEnemy}, turnFinishedCallback: {turnFinishedCallback}");
            return;
        }

        // 중복 클릭을 막기 위해 버튼을 끄고 UI를 숨깁니다.
        DisableButtonsAndHideUI();
        StartCoroutine(AttackRoutine());
    }

    // 공격 연출과 데미지 판정 타이밍을 조절하는 코루틴
    private IEnumerator AttackRoutine()
    {
        // 공격 애니메이션을 재생합니다.
        if (aniController != null)
        {
            aniController.PlayAttack();
        }

        Debug.Log($"{myUnit.unitName}이(가) {currentEnemy.unitName}을(를) 공격합니다!");

        // 칼을 휘두르는 모션 타이밍에 맞춰 0.6초 대기합니다.
        yield return new WaitForSeconds(0.6f);

        // 적에게 15의 데미지를 입힙니다.
        if (currentEnemy != null)
        {
            currentEnemy.TakeDamage(15);
        }

        // 공격 후 잔여 모션 재생을 위해 1.0초 대기합니다.
        yield return new WaitForSeconds(1.0f);

        // 턴을 종료합니다.
        EndTurn();
    }

    // 스킬 버튼을 눌렀을 때 실행되는 함수
    public void OnSkillCommand()
    {
        // 적이나 콜백 데이터가 유효한지 검사합니다.
        if (currentEnemy == null || turnFinishedCallback == null)
        {
            Debug.LogError($"턴 데이터 누락 오류! currentEnemy: {currentEnemy}, turnFinishedCallback: {turnFinishedCallback}");
            return;
        }

        Debug.Log($"{myUnit.unitName}이(가) 스킬을 사용합니다!");

        // 중복 클릭을 막기 위해 버튼을 끄고 UI를 숨깁니다.
        DisableButtonsAndHideUI();
        currentEnemy.TakeDamage(30);

        // 턴을 종료합니다.
        EndTurn();
    }

    // 버튼 상호작용을 차단하고 액션 UI를 숨기는 내부 편의 함수
    private void DisableButtonsAndHideUI()
    {
        if (BattleUIManager.Instance != null)
        {
            if (BattleUIManager.Instance.btn_attack != null) BattleUIManager.Instance.btn_attack.interactable = false;
            if (BattleUIManager.Instance.btn_skill != null) BattleUIManager.Instance.btn_skill.interactable = false;

            BattleUIManager.Instance.ShowPlayerActionUI(false);
        }
    }

    // 턴 종료를 매니저에게 알리는 함수
    private void EndTurn()
    {
        turnFinishedCallback?.Invoke();
    }
}