using UnityEngine;

public class AnimationController : MonoBehaviour
{
    [SerializeField]
    private Animator _animator;

    private int _animIDForwardSpeed;
    private int _animIDSidewardSpeed;
    private int _animIDGrounded;
    private int _animIDJump;
    private int _animIDFreeFall;

    private void Awake()
    {
        _animIDForwardSpeed = Animator.StringToHash("ForwardSpeed");
        _animIDSidewardSpeed = Animator.StringToHash("SideSpeed");
        _animIDGrounded = Animator.StringToHash("Grounded");
        _animIDJump = Animator.StringToHash("Jump");
        _animIDFreeFall = Animator.StringToHash("FreeFall");

        if (_animator == null && !TryGetComponent<Animator>(out _animator))
        {
            Debug.LogError("Animator component not found on " + gameObject.name);
        }
    }


    /// <param name="animMoveVelocity">
    /// walk/run dicrection: use x for side, y for forward, walk<=>run threshold is 0.7
    /// </param>
    /// <param name="motionSpeedMultiply">
    /// walk/run animation speed multiply for analog input
    /// </param>
    public void UpdateMovementAnimation(Vector3 animMoveVelocity, float motionSpeedMultiply)
    {
        if (_animator != null)
        {
            _animator.SetFloat(_animIDForwardSpeed, animMoveVelocity.y);
            _animator.SetFloat(_animIDSidewardSpeed, animMoveVelocity.x);
        }
    }

    public void SetGrounded(bool isGrounded)
    {
        if (_animator != null)
        {
            _animator.SetBool(_animIDGrounded, isGrounded);
        }
    }

    public void SetIsJump(bool isJump)
    {
        if (_animator != null)
        {
            _animator.SetBool(_animIDJump, isJump);
        }
    }

    public void SetFalling(bool isFalling)
    {
        if (_animator != null)
        {
            _animator.SetBool(_animIDFreeFall, isFalling);
        }
    }
}
