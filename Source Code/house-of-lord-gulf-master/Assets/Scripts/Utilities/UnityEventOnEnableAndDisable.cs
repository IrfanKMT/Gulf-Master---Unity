using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class UnityEventOnEnableAndDisable : MonoBehaviour
{

    /// <summary>
    /// Perfomance Optimized
    /// THis is just to disble and enable some method or object in Start Of UI panel or Object
    /// </summary>


    public UnityEvent OnEnableEvent;
    [Space]
    public UnityEvent OnDisbleEvent;
    private void OnDisable()
    {
        OnDisbleEvent.Invoke();
    }

    private void OnEnable()
    {
        OnEnableEvent.Invoke();
    }
}
