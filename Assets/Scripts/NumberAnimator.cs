using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Corrotina reutilizável para animar um valor numérico entre dois pontos (lerp),
/// usada pelo CanvasManager e pelo GameOverManager para os textos de UI.
/// </summary>
public static class NumberAnimator
{
    /// <summary>
    /// Anima um valor de 'fromValue' até 'toValue' ao longo de 'duration' segundos,
    /// chamando 'onUpdate' a cada frame com o valor interpolado (útil para formatar e atribuir a um texto).
    /// </summary>
    public static IEnumerator Animate(float fromValue, float toValue, float duration, Action<float> onUpdate)
    {
        if (duration <= 0f)
        {
            onUpdate(toValue);
            yield break;
        }

        float elapsed = 0f;
        onUpdate(fromValue);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            onUpdate(Mathf.Lerp(fromValue, toValue, t));
            yield return null;
        }

        onUpdate(toValue);
    }
}