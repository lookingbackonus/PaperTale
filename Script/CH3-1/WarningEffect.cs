using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class WarningEffect : MonoBehaviour
{
    [Header("참조할 Volume")]
    [SerializeField] private Volume globalVolume;

    [Header("변경할 Saturation Delta (음수로 어둡게)")]
    [SerializeField] private float saturationDelta = -50f;

    [Header("변환에 걸릴 시간 (초)")]
    [SerializeField] private float duration = 2f;

    [Header("활성화할 게임오브젝트들")]
    [SerializeField] private GameObject[] objectsToActivate;

    [Header("SpotLight 설정")]
    [SerializeField] private Light spotLight;
    [SerializeField] private float targetSpotAngle = 120f;
    [SerializeField] private bool useSpotLightEffect = true;

    private ColorAdjustments colorAdjust;
    private bool isFading = false;
    private bool hasActivated = false;
    private bool isEffectActive = false;  // 현재 이펙트 활성화 상태
    private float originalSaturation = 0f;  // 원래 saturation 값 저장
    private float originalSpotAngle = 30f;  // 원래 SpotLight 각도 저장

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
    }

    /// <summary>
    /// 외부에서 호출하여 Saturation을 일정값만큼 페이드합니다.
    /// 최초 1회 실행 후에는 효과가 계속 유지됩니다.
    /// 동시에 설정된 게임오브젝트들을 활성화하고 SpotLight 각도를 증가시킵니다.
    /// </summary>
    public void TriggerFade()
    {
        if (hasActivated || isFading) return;  // 이미 한 번 실행되었거나 페이딩 중이면 무시
        hasActivated = true;

        // 게임오브젝트들 활성화
        ActivateGameObjects();

        colorAdjust.active = true;
        colorAdjust.saturation.overrideState = true;

        float from = originalSaturation;
        float to = originalSaturation + saturationDelta;

        StartCoroutine(FadeEffects(from, to, duration, true));
    }

    /// <summary>
    /// 이펙트를 원래 상태로 되돌립니다.
    /// </summary>
    public void ResetEffect()
    {
        if (!hasActivated || isFading) return;  // 활성화되지 않았거나 페이딩 중이면 무시

        float from = colorAdjust.saturation.value;
        float to = originalSaturation;

        StartCoroutine(FadeEffects(from, to, duration, false));
    }

    /// <summary>
    /// 이펙트를 토글합니다 (활성화 <-> 비활성화)
    /// </summary>
    public void ToggleEffect()
    {
        if (isFading) return;  // 페이딩 중이면 무시

        if (!hasActivated || !isEffectActive)
        {
            // 이펙트 활성화
            hasActivated = true;
            
            // 게임오브젝트들 활성화
            ActivateGameObjects();
            
            colorAdjust.active = true;
            colorAdjust.saturation.overrideState = true;

            float from = isEffectActive ? colorAdjust.saturation.value : originalSaturation;
            float to = originalSaturation + saturationDelta;

            StartCoroutine(FadeEffects(from, to, duration, true));
        }
        else
        {
            // 이펙트 비활성화
            float from = colorAdjust.saturation.value;
            float to = originalSaturation;

            StartCoroutine(FadeEffects(from, to, duration, false));
        }
    }

    /// <summary>
    /// 이펙트를 즉시 활성화합니다 (페이드 없음)
    /// </summary>
    public void ActivateEffectInstant()
    {
        if (isFading) return;

        hasActivated = true;
        isEffectActive = true;
        
        // 게임오브젝트들 활성화
        ActivateGameObjects();
        
        colorAdjust.active = true;
        colorAdjust.saturation.overrideState = true;
        colorAdjust.saturation.value = originalSaturation + saturationDelta;
        
        // SpotLight 즉시 변경
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
        isEffectActive = false;
        colorAdjust.saturation.value = originalSaturation;
        colorAdjust.saturation.overrideState = false;
        colorAdjust.active = false;
        
        // SpotLight 원래 각도로 복원
        if (useSpotLightEffect && spotLight != null)
        {
            spotLight.spotAngle = originalSpotAngle;
        }
        
        // SpotLight 원래 각도로 복원
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
        StopAllCoroutines();
        hasActivated = false;
        isEffectActive = false;
        isFading = false;
        
        colorAdjust.saturation.value = originalSaturation;
        colorAdjust.saturation.overrideState = false;
        colorAdjust.active = false;
    }

    private IEnumerator FadeEffects(float saturationFrom, float saturationTo, float overSeconds, bool activating)
    {
        isFading = true;
        float elapsed = 0f;

        // SpotLight 시작/끝 각도 설정
        float spotAngleFrom = originalSpotAngle;
        float spotAngleTo = originalSpotAngle;
        
        if (useSpotLightEffect && spotLight != null)
        {
            spotAngleFrom = activating ? originalSpotAngle : spotLight.spotAngle;
            spotAngleTo = activating ? targetSpotAngle : originalSpotAngle;
        }

        while (elapsed < overSeconds)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / overSeconds);
            
            // Saturation 페이드
            colorAdjust.saturation.value = Mathf.Lerp(saturationFrom, saturationTo, t);
            
            // SpotLight 각도 페이드
            if (useSpotLightEffect && spotLight != null)
            {
                spotLight.spotAngle = Mathf.Lerp(spotAngleFrom, spotAngleTo, t);
            }
            
            yield return null;
        }

        // 최종 값 설정
        colorAdjust.saturation.value = saturationTo;
        if (useSpotLightEffect && spotLight != null)
        {
            spotLight.spotAngle = spotAngleTo;
        }
        
        isFading = false;
        isEffectActive = activating;

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
    public void ChangeSpotLightAngle(float angle, float fadeTime = 1f)
    {
        if (useSpotLightEffect && spotLight != null)
        {
            StartCoroutine(FadeSpotLightAngle(spotLight.spotAngle, angle, fadeTime));
        }
    }

    /// <summary>
    /// SpotLight 각도를 원래대로 복원합니다
    /// </summary>
    public void ResetSpotLightAngle(float fadeTime = 1f)
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
            spotLight.spotAngle = Mathf.Lerp(from, to, t);
            yield return null;
        }

        spotLight.spotAngle = to;
    }
}