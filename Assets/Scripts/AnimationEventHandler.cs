using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem.XR;

public class AnimationEventHandler : MonoBehaviour
{
    [Serializable]
    public sealed class MyAnimationEvent : UnityEvent<AnimationEvent> { }
    [SerializeField]
    MyAnimationEvent OnFootstepEvent;
    [SerializeField]
    MyAnimationEvent OnLandEvent;

    private void OnFootstep(AnimationEvent animationEvent)
    {
        OnFootstepEvent?.Invoke(animationEvent);
    }

    private void OnLand(AnimationEvent animationEvent)
    {
        OnLandEvent?.Invoke(animationEvent);
    }
}
