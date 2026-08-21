using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Handles hover and click scale animations for UI buttons independently,
/// with support for keyboard-triggered animations.
/// </summary>
public class ButtonHoverAnimator : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    public float hoverScaleFactor = 1.1f;
    /* Alteration: Added click scale factor */
    public float clickScaleFactor = 0.9f;
    public float animationDuration = 0.15f;
    public Func<bool> keyPressCondition;

    private Coroutine scaleCoroutine;
    private Coroutine simulateClickCoroutine;
    private bool isHovered = false;
    private bool isPressed = false;

    private void Update()
    {
        if (keyPressCondition != null && keyPressCondition.Invoke() && !isPressed)
        {
            if (simulateClickCoroutine != null) StopCoroutine(simulateClickCoroutine);
            simulateClickCoroutine = StartCoroutine(SimulateClickSequence());
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovered = true;
        if (!isPressed) TriggerScaleAnimation(hoverScaleFactor);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;
        if (!isPressed) TriggerScaleAnimation(1f);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        isPressed = true;
        TriggerScaleAnimation(clickScaleFactor);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        isPressed = false;
        TriggerScaleAnimation(isHovered ? hoverScaleFactor : 1f);
    }

    private void TriggerScaleAnimation(float targetScaleMultiplier)
    {
        if (simulateClickCoroutine != null)
        {
            StopCoroutine(simulateClickCoroutine);
            simulateClickCoroutine = null;
        }

        if (scaleCoroutine != null) StopCoroutine(scaleCoroutine);
        
        Vector3 targetScale = Vector3.one * targetScaleMultiplier;
        scaleCoroutine = StartCoroutine(LerpButtonScale(targetScale));
    }

    private IEnumerator SimulateClickSequence()
    {
        isPressed = true;
        if (scaleCoroutine != null) StopCoroutine(scaleCoroutine);
        
        Vector3 targetDownScale = Vector3.one * clickScaleFactor;
        yield return StartCoroutine(LerpButtonScale(targetDownScale));
        
        isPressed = false;
        Vector3 targetUpScale = Vector3.one * (isHovered ? hoverScaleFactor : 1f);
        scaleCoroutine = StartCoroutine(LerpButtonScale(targetUpScale));
    }

    private IEnumerator LerpButtonScale(Vector3 targetScale)
    {
        Vector3 initialScale = transform.localScale;
        float elapsed = 0f;

        while (elapsed < animationDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            transform.localScale = Vector3.Lerp(initialScale, targetScale, elapsed / animationDuration);
            yield return null;
        }

        transform.localScale = targetScale;
    }

    public static void ApplyTo(Button button, float hoverFactor = 1.1f, float clickFactor = 0.9f, float duration = 0.15f, Func<bool> condition = null)
    {
        if (button == null) return;

        ButtonHoverAnimator animator = button.gameObject.GetComponent<ButtonHoverAnimator>();
        if (animator == null)
        {
            animator = button.gameObject.AddComponent<ButtonHoverAnimator>();
        }

        animator.hoverScaleFactor = hoverFactor;
        animator.clickScaleFactor = clickFactor;
        animator.animationDuration = duration;
        animator.keyPressCondition = condition;
    }
}