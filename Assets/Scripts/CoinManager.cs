using UnityEngine;
using TMPro;
using System;

/// <summary>
/// Manages the coin UI display with lerp animations.
/// </summary>
public class CoinManager : MonoBehaviour
{
    public static int totalMoney = 0;
    public TextMeshProUGUI totalMoneyText;

    [Header("Animation Settings")]
    [SerializeField] private float numberLerpDuration = 0.4f;

    private const float UNINITIALIZED = float.MinValue;
    private float displayedMoney;
    private float lastTargetMoney = UNINITIALIZED;
    private Coroutine moneyCoroutine;

    void Update()
    {
        AnimateIfChanged(totalMoney, ref lastTargetMoney, ref displayedMoney, ref moneyCoroutine, value =>
        {
            displayedMoney = value;
            totalMoneyText.text = Mathf.RoundToInt(value).ToString();
        });
    }

    private void AnimateIfChanged(float targetValue, ref float lastTarget, ref float displayedValue, ref Coroutine coroutine, Action<float> onUpdate)
    {
        if (lastTarget == UNINITIALIZED)
        {
            lastTarget = targetValue;
            displayedValue = targetValue;
            onUpdate(targetValue);
            return;
        }

        if (Mathf.Abs(targetValue - lastTarget) > Mathf.Epsilon)
        {
            lastTarget = targetValue;

            if (coroutine != null)
            {
                StopCoroutine(coroutine);
            }

            coroutine = StartCoroutine(NumberAnimator.Animate(displayedValue, targetValue, numberLerpDuration, onUpdate));
        }
    }
}