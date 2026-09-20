using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ArtUnityWorkshop
{
    [RequireComponent(typeof(Canvas)), DefaultExecutionOrder(100)]
    public sealed class WorkshopHud : MonoBehaviour
    {
        [SerializeField, InspectorName("전투 슬롯 패널"), Tooltip("플레이어 머리 위에서 양측 체력과 슬롯을 함께 표시하는 패널입니다.")]
        private BattleSlotPanel battleSlotPanel;
        [SerializeField, InspectorName("조작 안내"), Tooltip("현재 Input Action Binding을 표시합니다.")]
        private TMP_Text inputText;
        [SerializeField, InspectorName("전투 안내 영역"), Tooltip("탐색 중에는 숨기는 안내 영역입니다.")]
        private GameObject battleInfo;
        [SerializeField, InspectorName("진행 안내"), Tooltip("라운드와 정지·선택 안내를 표시합니다.")]
        private TMP_Text phaseText;
        [SerializeField, InspectorName("남은 시간"), Tooltip("선택 중에만 초 단위로 표시합니다.")]
        private TMP_Text timerText;
        [SerializeField, InspectorName("시간 채움 이미지"), Tooltip("Filled 방식으로 남은 3초를 표시합니다.")]
        private Image timerFill;
        [SerializeField, InspectorName("판정 결과"), Tooltip("승리·패배·무승부를 표시합니다.")]
        private TMP_Text resultText;
        private Canvas canvas;
        private Camera worldCamera;
        private InputRouter input;
        private Combatant player;
        private BattleCoordinator battle;
        private string finalResult;

        private void Start()
        {
            canvas = GetComponent<Canvas>();
            foreach (var root in gameObject.scene.GetRootGameObjects())
            {
                if (input == null) input = root.GetComponentInChildren<InputRouter>();
                var movement = root.GetComponentInChildren<PlayerMovement>();
                if (movement != null) player = movement.GetComponent<Combatant>();
                if (worldCamera == null) worldCamera = root.GetComponentInChildren<Camera>();
            }
            battle = BattleCoordinator.FindInScene(gameObject.scene);
            if (battle != null) { battle.BattleStarted += ClearResult; battle.BattleFinished += Finish; }
        }
        private void ClearResult() => finalResult = null;
        private void Finish(bool won) => finalResult = won ? "승리! 적을 처치했습니다" : "패배";
        private void OnDestroy()
        {
            if (battle != null) { battle.BattleStarted -= ClearResult; battle.BattleFinished -= Finish; }
        }
        private void LateUpdate()
        {
            bool active = battle != null && battle.IsBattleActive;
            var phase = active ? battle.Phase : BattlePhase.Inactive;
            bool selecting = phase == BattlePhase.Selecting;
            bool showingResult = phase == BattlePhase.Resolving || phase == BattlePhase.Dying;
            if (inputText != null && input != null)
                inputText.text = $"{input.BindingLabel("MoveLeft")} / {input.BindingLabel("MoveRight")}  이동·후보 선택     {input.BindingLabel("Interact")}  정지·확정";
            if (battleSlotPanel != null) battleSlotPanel.Display(player, battle, worldCamera, canvas);
            if (battleInfo != null) battleInfo.SetActive(active || finalResult != null);
            if (phaseText != null) phaseText.text = !active ? "전투 종료" :
                $"라운드 {battle.RoundNumber} · " + (phase == BattlePhase.StopLeft ? "왼쪽 슬롯을 정지하세요" :
                phase == BattlePhase.StopRight ? "오른쪽 슬롯을 정지하세요" : selecting ? "후보를 선택하고 확정하세요" :
                phase == BattlePhase.Defeated ? "전투 종료" : "판정 결과");
            if (timerText != null) timerText.text = selecting ? $"{battle.RemainingTime:F1}초" : "";
            if (timerFill != null) { timerFill.gameObject.SetActive(selecting); timerFill.fillAmount = selecting ? battle.RemainingTime / 3 : 0; }
            if (resultText != null) resultText.text = finalResult ?? (showingResult ?
                battle.Result > 0 ? "승리 · 플레이어 공격" : battle.Result < 0 ? "패배 · 적 공격" : "무승부" : "");
        }

    }
}
