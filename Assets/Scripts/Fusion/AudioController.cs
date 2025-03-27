using UnityEngine;

public class AudioController : MonoBehaviour
{
    [SerializeField]
    AudioClip LandingAudioClip;
    [SerializeField]
    AudioClip[] FootstepAudioClips;
    [Range(0, 1)] public float FootstepAudioVolume = 0.5f;

    public void PlayFootstep(AnimationEvent animationEvent, Vector3 position)
    {
        if (animationEvent.animatorClipInfo.weight > 0.5f && FootstepAudioClips.Length > 0)
        {
            int index = Random.Range(0, FootstepAudioClips.Length);
            AudioSource.PlayClipAtPoint(FootstepAudioClips[index], position, FootstepAudioVolume);
        }
    }

    public void PlayLanding(AnimationEvent animationEvent, Vector3 position)
    {
        if (animationEvent.animatorClipInfo.weight > 0.5f && LandingAudioClip != null)
        {
            AudioSource.PlayClipAtPoint(LandingAudioClip, position, FootstepAudioVolume);
        }
    }
}
