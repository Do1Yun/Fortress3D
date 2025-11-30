using System.Collections;
using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public class UITweener : MonoBehaviour
{
    public enum TweenDirection { FromLeft, FromRight, FromTop, FromBottom }

    [Header("애니메이션 설정")]
    [Tooltip("UI가 나타날 방향을 설정합니다.")]
    public TweenDirection direction = TweenDirection.FromLeft;
    [Tooltip("애니메이션이 재생되는 시간입니다.")]
    public float animationDuration = 0.5f;
    [Tooltip("애니메이션의 움직임 곡선입니다. (예: EaseInOut)")]
    public AnimationCurve easingCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [Tooltip("화면 밖으로 얼마나 더 멀리 나갈지 정합니다. (1 = 딱 맞게, 1.1 = 10% 더)")]
    public float movementMultiplier = 1.1f;

    private RectTransform rectTransform;
    private Vector2 hiddenPosition;
    private Vector2 shownPosition;

    private bool isShown = true;
    private bool isAnimating = false;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        // 시작 시 에디터에 설정된 현재 위치를 '보이는 위치'로 한 번만 저장합니다.
        shownPosition = rectTransform.anchoredPosition;
        CalculateHiddenPosition();
    }

    // 화면 크기나 방향이 변경될 때를 대비해 숨겨질 위치를 다시 계산합니다.
    private void OnRectTransformDimensionsChange()
    {
        if (rectTransform == null)
        {
            rectTransform = GetComponent<RectTransform>();
        }
        // 보이는 위치가 바뀌었을 수 있으므로 다시 할당합니다.
        shownPosition = rectTransform.anchoredPosition;
        CalculateHiddenPosition();
    }

    // ▼▼▼ [수정] 숨겨지는 위치 계산 로직 개선 ▼▼▼
    private void CalculateHiddenPosition()
    {
        if (rectTransform == null)
        {
            rectTransform = GetComponent<RectTransform>();
        }
        // 부모 RectTransform의 사각형 정보와 자신의 크기를 가져옵니다.
        Rect parentRect = ((RectTransform)rectTransform.parent).rect;
        Vector2 selfSize = rectTransform.rect.size;

        // 방향에 따라 화면 밖의 위치를 정확하게 계산합니다.
        switch (direction)
        {
            case TweenDirection.FromLeft:
                // 부모의 왼쪽 경계(parentRect.xMin)에서 자신의 전체 너비만큼 더 왼쪽으로 이동
                hiddenPosition = new Vector2(parentRect.xMin - (selfSize.x * movementMultiplier), shownPosition.y);
                break;
            case TweenDirection.FromRight:
                // 부모의 오른쪽 경계(parentRect.xMax)에서 자신의 전체 너비만큼 더 오른쪽으로 이동
                hiddenPosition = new Vector2(parentRect.xMax + (selfSize.x * movementMultiplier), shownPosition.y);
                break;
            case TweenDirection.FromTop:
                // 부모의 위쪽 경계(parentRect.yMax)에서 자신의 전체 높이만큼 더 위쪽으로 이동
                hiddenPosition = new Vector2(shownPosition.x, parentRect.yMax + (selfSize.y * movementMultiplier));
                break;
            case TweenDirection.FromBottom:
                // 부모의 아래쪽 경계(parentRect.yMin)에서 자신의 전체 높이만큼 더 아래쪽으로 이동
                hiddenPosition = new Vector2(shownPosition.x, parentRect.yMin - (selfSize.y * movementMultiplier));
                break;
        }
    }
    // ▲▲▲ [여기까지 수정] ▲▲▲

    private IEnumerator AnimatePosition(Vector3 targetPosition)
    {
        isAnimating = true;
        Vector3 startPosition = rectTransform.anchoredPosition;
        float elapsedTime = 0f;

        while (elapsedTime < animationDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / animationDuration;
            rectTransform.anchoredPosition = Vector3.LerpUnclamped(startPosition, targetPosition, easingCurve.Evaluate(t));
            yield return null;
        }

        rectTransform.anchoredPosition = targetPosition;
        isAnimating = false;
        // 애니메이션이 끝나도 오브젝트를 비활성화하지 않습니다.
    }

    public void Show()
    {
        if (isShown || isAnimating) return;
        isShown = true;

        gameObject.SetActive(true);

        StartCoroutine(AnimatePosition(shownPosition));
    }

    public void Hide()
    {
        if (!isShown || isAnimating) return;
        isShown = false;

        StartCoroutine(AnimatePosition(hiddenPosition));
    }
}

