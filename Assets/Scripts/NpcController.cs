using UnityEngine;

[RequireComponent(typeof(Animator))]
public class NpcController : MonoBehaviour
{
    [Header("Movement (optional)")]
    public Rigidbody rb;              // Or CharacterController / custom movement
    public float maxSitSpeed = 0.2f;  // How slow we must be to allow sitting

    private Animator _anim;
    private static readonly int IsSittingHash = Animator.StringToHash("IsSitting");
    private static readonly int SpeedHash     = Animator.StringToHash("Speed");

    private void Awake()
    {
        _anim = GetComponent<Animator>();
        if (rb == null)
            rb = GetComponent<Rigidbody>();
    }

    private void Update()
    {
        UpdateSpeedParameter();
        HandleDebugInput(); // you can remove this later
    }

    private void UpdateSpeedParameter()
    {
        if (rb == null)
            return;

        Vector3 horizontalVel = rb.linearVelocity;
        horizontalVel.y = 0f;
        float speed = horizontalVel.magnitude;

        // If you already normalize Speed elsewhere, you can adjust this.
        _anim.SetFloat(SpeedHash, speed);
    }

    private void HandleDebugInput()
    {
        // Example: press 'C' to toggle sitting
        if (Input.GetKeyDown(KeyCode.C))
        {
            if (_anim.GetBool(IsSittingHash))
                Stand();
            else
                Sit();
        }
    }

    public void Sit()
    {
        // Optional: prevent sitting while moving too fast
        float currentSpeed = _anim.GetFloat(SpeedHash);
        if (currentSpeed > maxSitSpeed)
            return;

        _anim.SetBool(IsSittingHash, true);
    }

    public void Stand()
    {
        _anim.SetBool(IsSittingHash, false);
    }

    /// <summary>
    /// Optionally call this from an Animation Event at the end of StandToSit/SitToStand
    /// if you want super-tight syncing or to trigger sounds, camera moves etc.
    /// </summary>
    public void OnSitDownComplete()
    {
        // Hook: play chair creak SFX, adjust camera, etc.
    }

    public void OnStandUpComplete()
    {
        // Hook: reset camera, enable locomotion boost, etc.
    }
}
