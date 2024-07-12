using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using TimeMap = System.Func<float, float>;

public class Timer : MonoBehaviour
{
    public float delay, period;
    public TimeMap timeMap;
    public UnityAction<float> onUpdate;
    public UnityAction onStart, onStop;
    public bool autoDestroy = true, periodical = false, confirmUpdate = true;
    private float time;
    private bool hasStarted;

    void Start()
    {
        time = -delay;
        hasStarted = false;
        timeMap ??= Identity();
    }

    void Update()
    {
        time += Time.deltaTime;
        if(time > period && !periodical)
        {
            if(confirmUpdate)
                onUpdate?.Invoke(1);
            onStop?.Invoke();
            if (autoDestroy)
                Destroy(this);
        }
        else if (time > 0)
        {
            if (!hasStarted)
            {
                hasStarted = true;
                onStart?.Invoke();
            }
            onUpdate?.Invoke(timeMap(time / period));
        }
    }

    public static TimeMap Identity() => x => x;

    public static TimeMap Linear(float startValue, float endValue)
    {
        return x => Mathf.LerpUnclamped(startValue, endValue, x);
    }

    public static TimeMap LinearClamp(float startValue, float endValue)
    {
        return x => Mathf.Lerp(startValue, endValue, x);
    }

    public static TimeMap Smooth(float startValue, float endValue)
    {
        float k = (endValue - startValue) * 2.0f;
        return x => {
            x = Mathf.Clamp(x, 0, 1);
            return k * x * x * (1.5f - x) + startValue;
        };
    }

    public static TimeMap Accelaration(float startValue, float endValue, float pow)
    {
        float k = endValue - startValue;
        return x => k * Mathf.Pow(x, pow) + startValue;
    }

    public static TimeMap PingPong(float startValue, float endValue){
        float k = endValue - startValue;
        return x => Mathf.PingPong(x, 1) * k + startValue;
    }

    public static TimeMap Wave(float minValue, float maxValue, float phase=0)
    {
        float a = 0.5f * (maxValue - minValue),
            c = 0.5f * (minValue + maxValue);
        return x => Mathf.Sin(2 * Mathf.PI * x + phase) * a + c;
    }
}
