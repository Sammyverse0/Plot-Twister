using Unity.Cinemachine;
using UnityEngine;

public class CameraShake : CinemachineExtension
{
    [Header("Walking")]
    [SerializeField] private float walkAmount = 0.015f;
    [SerializeField] private float walkSpeed = 10f;
    [SerializeField] private float walkTilt = 0.15f;

    [Header("Running")]
    [SerializeField] private float runAmount = 0.05f;
    [SerializeField] private float runSpeed = 15f;
    [SerializeField] private float runTilt = 0.8f;

    [SerializeField] private float blendSpeed = 6f;

    [Header("Recoil")]
    [SerializeField] private float kickReturnSpeed = 10f;

    private FPSMovement player;
    private float timer;
    private float amount;
    private float speed;
    private float tilt;
    private float kick;

    public void Kick(float degrees)
    {
        kick += degrees;
    }

    protected override void Awake()
    {
        base.Awake();
        player = GetComponentInParent<FPSMovement>();
    }

    private void Update()
    {
        kick = Mathf.Lerp(kick, 0f, kickReturnSpeed * Time.deltaTime);

        if (player == null) return;

        float targetAmount = 0f;
        float targetSpeed = walkSpeed;
        float targetTilt = 0f;

        if (player.IsMoving)
        {
            bool running = player.IsSprinting;
            targetAmount = running ? runAmount : walkAmount;
            targetSpeed = running ? runSpeed : walkSpeed;
            targetTilt = running ? runTilt : walkTilt;
        }

        float blend = blendSpeed * Time.deltaTime;
        amount = Mathf.Lerp(amount, targetAmount, blend);
        speed = Mathf.Lerp(speed, targetSpeed, blend);
        tilt = Mathf.Lerp(tilt, targetTilt, blend);

        timer += Time.deltaTime * speed;
    }

    protected override void PostPipelineStageCallback(CinemachineVirtualCameraBase vcam, CinemachineCore.Stage stage, ref CameraState state, float deltaTime)
    {
        if (stage != CinemachineCore.Stage.Noise) return;

        float up = Mathf.Sin(timer) * amount;
        float side = Mathf.Cos(timer * 0.5f) * amount * 0.5f;
        float roll = Mathf.Cos(timer * 0.5f) * tilt;

        state.PositionCorrection += state.RawOrientation * new Vector3(side, up, 0f);
        state.OrientationCorrection *= Quaternion.Euler(-kick, 0f, roll);
    }
}
