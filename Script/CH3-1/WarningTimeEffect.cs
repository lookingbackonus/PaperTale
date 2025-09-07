using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class WarningTimeEffect : MonoBehaviour
{
    [Header("참조할 Volume")]
    [SerializeField] private Volume globalVolume;

    [Header("변경할 Saturation Delta (음수로 어둡게)")]
    [SerializeField] private float saturationDelta = -50f;

    [Header("변환에 걸릴 시간 (초) - 기본 50초")]
    [SerializeField] private float duration = 50f;

    [Header("활성화할 게임오브젝트들")]
    [SerializeField] private GameObject[] objectsToActivate;

    [Header("SpotLight 설정")]
    [SerializeField] private Light spotLight;
    [SerializeField] private float targetSpotAngle = 120f;
    [SerializeField] private bool useSpotLightEffect = true;

    [Header("페이드 곡선 설정")]
    [SerializeField] private AnimationCurve fadeCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [SerializeField] private bool useEaseInOut = true;

    [Header("디버그 정보")]
    [SerializeField] private bool showDebugInfo = false;

    private ColorAdjustments colorAdjust;
    private bool isFading = false;
    private bool hasActivated = false;
    private bool isEffectActive = false;
    private float originalSaturation = 0f;
    private float originalSpotAngle = 30f;
    
    // 진행 상황 추적용
    private float currentFadeProgress = 0f;
    private Coroutine currentFadeCoroutine = null;

    private void Awake()
    {
        if (globalVolume == null)
        {
            Debug.LogError("Global Volume이 할당되지 않았습니다!");
            enabled = false;
            return;
        }

        if (!globalVolume.profile.TryGet<ColorAdjustments>(out colorAdjust))
        {
            Debug.LogError("Volume Profile에 Color Adjustments Override가 없습니다!");
            enabled = false;
            return;
        }

        // 원래 saturation 값 저장
        originalSaturation = colorAdjust.saturation.value;

        // SpotLight 설정 확인 및 원래 각도 저장
        if (useSpotLightEffect && spotLight != null)
        {
            if (spotLight.type != LightType.Spot)
            {
                Debug.LogWarning("할당된 Light가 SpotLight가 아닙니다! SpotLight 효과가 비활성화됩니다.");
                useSpotLightEffect = false;
            }
            else
            {
                originalSpotAngle = spotLight.spotAngle;
            }
        }

        // 시작할 땐 효과 비활성화 상태
        colorAdjust.active = false;
        colorAdjust.saturation.overrideState = false;

        // 기본 페이드 곡선 설정 (부드러운 EaseInOut)
        if (fadeCurve == null || fadeCurve.keys.Length == 0)
        {
            fadeCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        }
    }

    private void Update()
    {
        // 디버그 정보 표시
        if (showDebugInfo && isFading)
        {
            Debug.Log($"페이드 진행률: {currentFadeProgress:P1} | 현재 Saturation: {colorAdjust.saturation.value:F2}");
        }
    }

    /// <summary>
    /// 50초에 걸쳐 서서히 Saturation을 변경합니다.
    /// 최초 1회 실행 후에는 효과가 계속 유지됩니다.
    /// </summary>
    public void TriggerFade()
    {
        if (hasActivated || isFading) return;
        hasActivated = true;

        ActivateGameObjects();

        colorAdjust.active = true;
        colorAdjust.saturation.overrideState = true;

        float from = originalSaturation;
        float to = originalSaturation + saturationDelta;

        currentFadeCoroutine = StartCoroutine(FadeEffects(from, to, duration, true));
    }

    /// <summary>
    /// 50초에 걸쳐 서서히 이펙트를 원래 상태로 되돌립니다.
    /// </summary>
    public void ResetEffect()
    {
        if (!hasActivated || isFading) return;

        float from = colorAdjust.saturation.value;
        float to = originalSaturation;

        currentFadeCoroutine = StartCoroutine(FadeEffects(from, to, duration, false));
    }

    /// <summary>
    /// 50초에 걸쳐 서서히 이펙트를 토글합니다.
    /// </summary>
    public void ToggleEffect()
    {
        if (isFading) return;

        if (!hasActivated || !isEffectActive)
        {
            hasActivated = true;
            ActivateGameObjects();
            
            colorAdjust.active = true;
            colorAdjust.saturation.overrideState = true;

            float from = isEffectActive ? colorAdjust.saturation.value : originalSaturation;
            float to = originalSaturation + saturationDelta;

            currentFadeCoroutine = StartCoroutine(FadeEffects(from, to, duration, true));
        }
        else
        {
            float from = colorAdjust.saturation.value;
            float to = originalSaturation;

            currentFadeCoroutine = StartCoroutine(FadeEffects(from, to, duration, false));
        }
    }

    /// <summary>
    /// 커스텀 시간을 지정하여 페이드를 실행합니다.
    /// </summary>
    public void TriggerFadeWithDuration(float customDuration)
    {
        if (hasActivated || isFading) return;
        hasActivated = true;

        ActivateGameObjects();

        colorAdjust.active = true;
        colorAdjust.saturation.overrideState = true;

        float from = originalSaturation;
        float to = originalSaturation + saturationDelta;

        currentFadeCoroutine = StartCoroutine(FadeEffects(from, to, customDuration, true));
    }

    /// <summary>
    /// 현재 진행 중인 페이드를 중단합니다.
    /// </summary>
    public void StopCurrentFade()
    {
        if (currentFadeCoroutine != null)
        {
            StopCoroutine(currentFadeCoroutine);
            currentFadeCoroutine = null;
        }
        isFading = false;
        currentFadeProgress = 0f;
    }

    /// <summary>
    /// 이펙트를 즉시 활성화합니다 (페이드 없음)
    /// </summary>
    public void ActivateEffectInstant()
    {
        if (isFading) StopCurrentFade();

        hasActivated = true;
        isEffectActive = true;
        
        ActivateGameObjects();
        
        colorAdjust.active = true;
        colorAdjust.saturation.overrideState = true;
        colorAdjust.saturation.value = originalSaturation + saturationDelta;
        
        if (useSpotLightEffect && spotLight != null)
        {
            spotLight.spotAngle = targetSpotAngle;
        }
    }

    /// <summary>
    /// 이펙트를 즉시 비활성화합니다 (페이드 없음)
    /// </summary>
    public void DeactivateEffectInstant()
    {
        if (isFading) StopCurrentFade();

        isEffectActive = false;
        colorAdjust.saturation.value = originalSaturation;
        colorAdjust.saturation.overrideState = false;
        colorAdjust.active = false;
        
        if (useSpotLightEffect && spotLight != null)
        {
            spotLight.spotAngle = originalSpotAngle;
        }
    }

    /// <summary>
    /// 완전히 초기화합니다 (다시 TriggerFade 사용 가능)
    /// </summary>
    public void ResetToInitialState()
    {
        StopCurrentFade();
        StopAllCoroutines();
        
        hasActivated = false;
        isEffectActive = false;
        isFading = false;
        currentFadeProgress = 0f;
        
        colorAdjust.saturation.value = originalSaturation;
        colorAdjust.saturation.overrideState = false;
        colorAdjust.active = false;
    }

    private IEnumerator FadeEffects(float saturationFrom, float saturationTo, float overSeconds, bool activating)
    {
        isFading = true;
        currentFadeProgress = 0f;
        float elapsed = 0f;

        // SpotLight 시작/끝 각도 설정
        float spotAngleFrom = originalSpotAngle;
        float spotAngleTo = originalSpotAngle;
        
        if (useSpotLightEffect && spotLight != null)
        {
            spotAngleFrom = activating ? originalSpotAngle : spotLight.spotAngle;
            spotAngleTo = activating ? targetSpotAngle : originalSpotAngle;
        }

        Debug.Log($"50초 페이드 시작: {saturationFrom:F2} → {saturationTo:F2} ({(activating ? "활성화" : "비활성화")})");

        while (elapsed < overSeconds)
        {
            elapsed += Time.deltaTime;
            currentFadeProgress = Mathf.Clamp01(elapsed / overSeconds);
            
            // 부드러운 곡선을 적용한 t 값 계산
            float t = useEaseInOut ? fadeCurve.Evaluate(currentFadeProgress) : currentFadeProgress;
            
            // Saturation 페이드
            float currentSaturation = Mathf.Lerp(saturationFrom, saturationTo, t);
            colorAdjust.saturation.value = currentSaturation;
            
            // SpotLight 각도 페이드
            if (useSpotLightEffect && spotLight != null)
            {
                spotLight.spotAngle = Mathf.Lerp(spotAngleFrom, spotAngleTo, t);
            }
            
            // 10초마다 진행 상황 로그 출력
            if (Mathf.FloorToInt(elapsed) % 10 == 0 && elapsed > 0)
            {
                float remainingTime = overSeconds - elapsed;
                Debug.Log($"페이드 진행: {currentFadeProgress:P1} 완료, 남은 시간: {remainingTime:F1}초");
            }
            
            yield return null;
        }

        // 최종 값 설정
        colorAdjust.saturation.value = saturationTo;
        if (useSpotLightEffect && spotLight != null)
        {
            spotLight.spotAngle = spotAngleTo;
        }
        
        currentFadeProgress = 1f;
        isFading = false;
        isEffectActive = activating;
        currentFadeCoroutine = null;

        Debug.Log($"50초 페이드 완료: 최종 Saturation = {saturationTo:F2}");

        // 비활성화 완료 시 ColorAdjustments 완전 비활성화
        if (!activating)
        {
            colorAdjust.saturation.overrideState = false;
            colorAdjust.active = false;
        }
    }

    // 현재 상태 확인용 프로퍼티들
    public bool IsEffectActive => isEffectActive;
    public bool IsFading => isFading;
    public bool HasActivated => hasActivated;
    public float CurrentFadeProgress => currentFadeProgress;
    public float RemainingFadeTime => isFading && currentFadeCoroutine != null ? 
        duration * (1f - currentFadeProgress) : 0f;

    /// <summary>
    /// 설정된 게임오브젝트들을 모두 활성화합니다.
    /// </summary>
    private void ActivateGameObjects()
    {
        if (objectsToActivate == null || objectsToActivate.Length == 0)
            return;

        foreach (GameObject obj in objectsToActivate)
        {
            if (obj != null && !obj.activeInHierarchy)
            {
                obj.SetActive(true);
                Debug.Log($"게임오브젝트 활성화: {obj.name}");
            }
        }
    }

    /// <summary>
    /// 설정된 게임오브젝트들을 모두 비활성화합니다.
    /// </summary>
    public void DeactivateGameObjects()
    {
        if (objectsToActivate == null || objectsToActivate.Length == 0)
            return;

        foreach (GameObject obj in objectsToActivate)
        {
            if (obj != null && obj.activeInHierarchy)
            {
                obj.SetActive(false);
                Debug.Log($"게임오브젝트 비활성화: {obj.name}");
            }
        }
    }

    /// <summary>
    /// 게임오브젝트들만 따로 활성화합니다 (이펙트와 별개로)
    /// </summary>
    public void ActivateObjectsOnly()
    {
        ActivateGameObjects();
    }

    /// <summary>
    /// SpotLight 각도만 따로 변경합니다 (이펙트와 별개로)
    /// </summary>
    public void ChangeSpotLightAngle(float angle, float fadeTime = 50f)
    {
        if (useSpotLightEffect && spotLight != null)
        {
            StartCoroutine(FadeSpotLightAngle(spotLight.spotAngle, angle, fadeTime));
        }
    }

    /// <summary>
    /// SpotLight 각도를 원래대로 복원합니다
    /// </summary>
    public void ResetSpotLightAngle(float fadeTime = 50f)
    {
        if (useSpotLightEffect && spotLight != null)
        {
            StartCoroutine(FadeSpotLightAngle(spotLight.spotAngle, originalSpotAngle, fadeTime));
        }
    }

    private IEnumerator FadeSpotLightAngle(float from, float to, float overSeconds)
    {
        float elapsed = 0f;

        while (elapsed < overSeconds)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / overSeconds);
            
            // 부드러운 곡선 적용
            if (useEaseInOut)
            {
                t = fadeCurve.Evaluate(t);
            }
            
            spotLight.spotAngle = Mathf.Lerp(from, to, t);
            yield return null;
        }

        spotLight.spotAngle = to;
    }

    /// <summary>
    /// 현재 페이드 상태 정보를 문자열로 반환합니다.
    /// </summary>
    public string GetFadeStatusInfo()
    {
        if (!isFading) return "페이드 진행 중 아님";
        
        float remainingTime = RemainingFadeTime;
        return $"페이드 진행: {currentFadeProgress:P1} | 남은 시간: {remainingTime:F1}초 | 현재 Saturation: {colorAdjust.saturation.value:F2}";
    }
}