using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ArtUnityWorkshop
{
    public sealed class BattleSlotPanel : MonoBehaviour
    {
        [SerializeField, InspectorName("머리 위 여백"), Tooltip("플레이어 HeadAnchor에서 패널 하단 중앙까지의 기준 해상도 픽셀 거리입니다.")]
        private Vector2 screenOffset = new Vector2(0, 30);
        [SerializeField, InspectorName("플레이어 슬롯 영역"), Tooltip("탐색 중에는 숨기고 전투 중에 표시할 슬롯 두 개의 부모입니다.")]
        private GameObject playerSlots;
        [SerializeField, InspectorName("적 표시 영역"), Tooltip("전투 중에만 표시할 적 체력과 슬롯의 부모입니다. 위치는 공통 패널의 Layout Group이 정합니다.")]
        private GameObject enemyContent;
        [SerializeField, InspectorName("체력 채움 이미지"), Tooltip("플레이어, 적 순서로 Image 두 개를 연결합니다.")]
        private Image[] healthFills;
        [SerializeField, InspectorName("체력 숫자"), Tooltip("플레이어, 적 순서로 텍스트 두 개를 연결합니다.")]
        private TMP_Text[] healthTexts;
        [SerializeField, InspectorName("손 이미지"), Tooltip("플레이어 왼쪽·오른쪽, 적 왼쪽·오른쪽 순서로 연결합니다.")]
        private Image[] handImages;
        [SerializeField, InspectorName("다음 손 이미지"), Tooltip("손 이미지와 같은 순서로 위에서 내려오는 이미지를 연결합니다.")]
        private Image[] incomingImages;
        [SerializeField, InspectorName("손 이름"), Tooltip("플레이어 왼쪽·오른쪽, 적 왼쪽·오른쪽 순서입니다.")]
        private TMP_Text[] handTexts;
        [SerializeField, InspectorName("선택 테두리"), Tooltip("플레이어 왼쪽·오른쪽, 적 왼쪽·오른쪽 순서입니다.")]
        private GameObject[] highlights;
        [SerializeField, InspectorName("바위·가위·보 이미지"), Tooltip("바위, 가위, 보 순서로 Sprite 세 개를 연결합니다.")]
        private Sprite[] handSprites;

        public void Display(Combatant player, BattleCoordinator battle, Camera camera, Canvas canvas)
        {
            bool active = battle != null && battle.IsBattleActive;
            var phase = active ? battle.Phase : BattlePhase.Inactive;
            if (player == null || camera == null || canvas == null || phase == BattlePhase.Defeated)
            { gameObject.SetActive(false); return; }
            var anchor = player.transform.Find("HeadAnchor");
            var screen = camera.WorldToScreenPoint(anchor != null ? anchor.position : player.transform.position + Vector3.up);
            if (screen.z <= 0) { gameObject.SetActive(false); return; }
            gameObject.SetActive(true);
            if (playerSlots != null) playerSlots.SetActive(active);
            var rect = (RectTransform)transform;
            bool showEnemy = active && battle.Opponent != null;
            if (enemyContent != null && enemyContent.activeSelf != showEnemy)
            {
                enemyContent.SetActive(showEnemy);
                LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
            }
            var parent = (RectTransform)rect.parent;
            Vector2 local;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, screen,
                canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera, out local);
            local += screenOffset;
            var size = Vector2.Scale(rect.rect.size, new Vector2(Mathf.Abs(rect.localScale.x), Mathf.Abs(rect.localScale.y)));
            var bounds = parent.rect;
            // Clamp the common panel as a whole. If it is wider than the screen, let its right edge overflow.
            local.x = Mathf.Clamp(local.x, bounds.xMin + size.x * rect.pivot.x,
                Mathf.Max(bounds.xMin + size.x * rect.pivot.x, bounds.xMax - size.x * (1 - rect.pivot.x)));
            local.y = Mathf.Clamp(local.y, bounds.yMin + size.y * rect.pivot.y,
                Mathf.Max(bounds.yMin + size.y * rect.pivot.y, bounds.yMax - size.y * (1 - rect.pivot.y)));
            rect.localPosition = new Vector3(local.x, local.y, 0);
            for (int side = 0; side < 2; side++)
            {
                var actor = side == 0 ? player : active ? battle.Opponent : null;
                if (actor == null) continue;
                if (healthFills != null && side < healthFills.Length && healthFills[side] != null)
                    healthFills[side].fillAmount = (float)actor.Stats.CurrentHealth / actor.Stats.MaxHealth;
                if (healthTexts != null && side < healthTexts.Length && healthTexts[side] != null)
                    healthTexts[side].text = $"{actor.Stats.CurrentHealth} / {actor.Stats.MaxHealth}";
            }
            if (!active) return;
            bool result = phase == BattlePhase.Resolving || phase == BattlePhase.Dying;
            for (int i = 0; i < 4; i++)
            {
                Hand hand = i == 0 ? battle.PlayerLeft : i == 1 ? battle.PlayerRight : i == 2 ? battle.EnemyLeft : battle.EnemyRight;
                bool spin = i == 0 ? phase == BattlePhase.StopLeft : i == 1 && (phase == BattlePhase.StopLeft || phase == BattlePhase.StopRight);
                int selection = i < 2 ? (phase == BattlePhase.Selecting || result ? battle.SelectedIndex : -1) : (result ? battle.EnemySelectedIndex : -1);
                if (handImages != null && i < handImages.Length && handImages[i] != null)
                {
                    var current = handImages[i];
                    current.sprite = SpriteFor(hand);
                    current.enabled = current.sprite != null;
                    float height = ((RectTransform)current.transform.parent).rect.height;
                    current.rectTransform.anchoredPosition = new Vector2(0, spin ? -height * battle.SlotScrollProgress : 0);
                    if (incomingImages != null && i < incomingImages.Length && incomingImages[i] != null)
                    {
                        var incoming = incomingImages[i];
                        incoming.sprite = SpriteFor((Hand)(((int)hand + 1) % 3));
                        incoming.enabled = spin && incoming.sprite != null;
                        incoming.rectTransform.anchoredPosition = new Vector2(0, height * (1 - battle.SlotScrollProgress));
                    }
                }
                if (handTexts != null && i < handTexts.Length && handTexts[i] != null)
                    handTexts[i].text = hand == Hand.Rock ? "바위" : hand == Hand.Scissors ? "가위" : "보";
                if (highlights != null && i < highlights.Length && highlights[i] != null)
                    highlights[i].SetActive(selection == i % 2);
            }
        }

        private Sprite SpriteFor(Hand hand) => handSprites != null && (int)hand < handSprites.Length ? handSprites[(int)hand] : null;
    }
}
