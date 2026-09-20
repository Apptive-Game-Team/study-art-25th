using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ArtUnityWorkshop
{
    public sealed class HeadSlotPanel : MonoBehaviour
    {
        [SerializeField, InspectorName("체력 채움 이미지"), Tooltip("Filled 방식의 Image입니다.")]
        private Image healthFill;
        [SerializeField, InspectorName("체력 숫자"), Tooltip("현재 체력과 최대 체력을 표시합니다.")]
        private TMP_Text healthText;
        [SerializeField, InspectorName("슬롯 영역"), Tooltip("전투 중에만 표시할 두 슬롯의 부모입니다.")]
        private GameObject slots;
        [SerializeField, InspectorName("손 이미지"), Tooltip("왼쪽, 오른쪽 순서의 Image 두 개입니다.")]
        private Image[] handImages;
        [SerializeField, InspectorName("다음 손 이미지"), Tooltip("슬롯 위에서 내려오는 왼쪽·오른쪽 Image입니다. 손 이미지와 같은 마스크 안에 배치합니다.")]
        private Image[] incomingImages;
        [SerializeField, InspectorName("손 이름"), Tooltip("왼쪽, 오른쪽 순서의 텍스트 두 개입니다.")]
        private TMP_Text[] handTexts;
        [SerializeField, InspectorName("선택 테두리"), Tooltip("왼쪽, 오른쪽 순서의 강조 이미지 두 개입니다.")]
        private GameObject[] highlights;
        [SerializeField, InspectorName("바위·가위·보 이미지"), Tooltip("바위, 가위, 보 순서로 교체할 Sprite 세 개입니다.")]
        private Sprite[] handSprites;
        [SerializeField, InspectorName("머리 위 여백"), Tooltip("HeadAnchor에서 UI까지의 기준 해상도 픽셀 거리입니다.")]
        private Vector2 screenOffset = new Vector2(0, 30);

        public void Display(Combatant actor, Camera camera, Canvas canvas, bool showSlots, Hand left, Hand right, int selected,
            bool spinLeft = false, bool spinRight = false, float scrollProgress = 0)
        {
            if (actor == null || camera == null || canvas == null) { gameObject.SetActive(false); return; }
            var anchor = actor.transform.Find("HeadAnchor");
            var screen = camera.WorldToScreenPoint(anchor != null ? anchor.position : actor.transform.position + Vector3.up);
            if (screen.z <= 0) { gameObject.SetActive(false); return; }
            gameObject.SetActive(true);
            var rect = (RectTransform)transform;
            var parent = (RectTransform)rect.parent;
            Vector2 local;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, screen,
                canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera, out local);
            local += screenOffset;
            var bounds = parent.rect;
            var size = Vector2.Scale(rect.rect.size, new Vector2(Mathf.Abs(rect.localScale.x), Mathf.Abs(rect.localScale.y)));
            local.x = Mathf.Clamp(local.x, bounds.xMin + size.x * rect.pivot.x, bounds.xMax - size.x * (1 - rect.pivot.x));
            local.y = Mathf.Clamp(local.y, bounds.yMin + size.y * rect.pivot.y, bounds.yMax - size.y * (1 - rect.pivot.y));
            rect.localPosition = new Vector3(local.x, local.y, 0);
            if (healthFill != null) healthFill.fillAmount = (float)actor.Stats.CurrentHealth / actor.Stats.MaxHealth;
            if (healthText != null) healthText.text = $"{actor.Stats.CurrentHealth} / {actor.Stats.MaxHealth}";
            if (slots != null) slots.SetActive(showSlots);
            for (int i = 0; i < 2; i++)
            {
                var hand = i == 0 ? left : right;
                if (handImages != null && i < handImages.Length && handImages[i] != null)
                {
                    var sprite = handSprites != null && (int)hand < handSprites.Length ? handSprites[(int)hand] : null;
                    handImages[i].sprite = sprite;
                    handImages[i].enabled = sprite != null;
                    bool spinning = showSlots && (i == 0 ? spinLeft : spinRight);
                    var current = handImages[i].rectTransform;
                    var incoming = incomingImages != null && i < incomingImages.Length ? incomingImages[i] : null;
                    if (incoming != null)
                    {
                        float height = ((RectTransform)current.parent).rect.height;
                        current.anchoredPosition = new Vector2(0, spinning ? -height * scrollProgress : 0);
                        int next = ((int)hand + 1) % 3;
                        incoming.sprite = handSprites != null && next < handSprites.Length ? handSprites[next] : null;
                        incoming.enabled = spinning && incoming.sprite != null;
                        incoming.rectTransform.anchoredPosition = new Vector2(0, height * (1 - scrollProgress));
                    }
                }
                if (handTexts != null && i < handTexts.Length && handTexts[i] != null)
                    handTexts[i].text = hand == Hand.Rock ? "바위" : hand == Hand.Scissors ? "가위" : "보";
                if (highlights != null && i < highlights.Length && highlights[i] != null)
                    highlights[i].SetActive(showSlots && selected == i);
            }
        }
    }
}
