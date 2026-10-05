using UnityEngine;
using System.Collections;
using System;


public class Gun : MonoBehaviour
{
    [Header("Gun Settings")]
    [SerializeField] private float reloadTime = 1f;
    [SerializeField] private float fireRate = 0.45f;
    [SerializeField] public int magSize = 9;

    [Header("Spread")]
    [SerializeField] private float standingSpread = 0f;
    [SerializeField] private float walkingSpread = 2.5f;
    [SerializeField] private float runningSpread = 6f;
    [SerializeField] private float jumpingSpread = 7f;
    [SerializeField] private float spreadChangeSpeed = 10f;

    [Header("Recoil")]
    [SerializeField] private float kickBack = 0.06f;
    [SerializeField] private float kickUp = 8f;
    [SerializeField] private float cameraKick = 1.2f;
    [SerializeField] private float recoilReturnSpeed = 12f;

    [Header("References")]
    [SerializeField] private GameObject bullet;
    [SerializeField] private Transform bulletSpawnPoint;

    [Header("Audio")]
    [SerializeField] private AudioSource gunshotSource;
    [SerializeField] private AudioClip gunshotClip;

    public int _currentAmmo;
    private bool _isReloading = false;
    private float _nextTimeToFire = 0f;

    private Quaternion initialRotation;
    private Vector3 initialPosition;
    private Vector3 reloadRotationOffset = new Vector3(66, 50, 50);
    private Collider[] _shooterColliders;

    private FPSMovement _player;
    private CameraShake _cameraShake;
    private Camera _cam;
    private float _recoil;

    public float CurrentSpread { get; private set; }

    private void Start()
    {
        _currentAmmo = magSize;
        initialRotation = transform.localRotation;
        initialPosition = transform.localPosition;

        CharacterController owner = GetComponentInParent<CharacterController>();
        Transform ownerRoot = owner != null ? owner.transform : transform.root;
        _shooterColliders = ownerRoot.GetComponentsInChildren<Collider>();

        _player = GetComponentInParent<FPSMovement>();
        _cameraShake = GetComponentInParent<CameraShake>();
        _cam = Camera.main;
    }

    private void Update()
    {
        CurrentSpread = Mathf.Lerp(CurrentSpread, GetTargetSpread(), spreadChangeSpeed * Time.deltaTime);

        _recoil = Mathf.Lerp(_recoil, 0f, recoilReturnSpeed * Time.deltaTime);

        if (!_isReloading)
            ApplyRecoil();
    }

    private float GetTargetSpread()
    {
        if (_player == null) return standingSpread;
        if (!_player.IsGrounded) return jumpingSpread;
        if (!_player.IsMoving) return standingSpread;
        return _player.IsSprinting ? runningSpread : walkingSpread;
    }

    private void ApplyRecoil()
    {
        Vector3 back = transform.parent.InverseTransformDirection(bulletSpawnPoint.right);
        Vector3 side = transform.parent.InverseTransformDirection(_cam.transform.right);

        transform.localPosition = initialPosition + back * (kickBack * _recoil);
        transform.localRotation = Quaternion.AngleAxis(-kickUp * _recoil, side) * initialRotation;
    }

    public void Shoot()
    {
        if (_isReloading)
            return;
        if (Time.time < _nextTimeToFire)
            return;
        if (_currentAmmo <= 0)
        {
            StartCoroutine(Reload());
            return;
        }

        _nextTimeToFire = Time.time + fireRate;

        GameObject newBullet = Instantiate(bullet, bulletSpawnPoint.position, bulletSpawnPoint.rotation);
        IgnoreShooterCollisions(newBullet);

        if (newBullet.TryGetComponent(out Bullet shot))
            shot.Fire(GetShotDirection());


        _currentAmmo--;
        _recoil = 1f;

        if (gunshotSource != null && gunshotClip != null)
            gunshotSource.PlayOneShot(gunshotClip);

        if (_cameraShake != null)
            _cameraShake.Kick(cameraKick);
    }

    private Vector3 GetShotDirection()
    {
        Transform camTransform = _cam.transform;

        Vector2 random = UnityEngine.Random.insideUnitCircle * Mathf.Tan(CurrentSpread * Mathf.Deg2Rad);
        Vector3 aimDir = (camTransform.forward + camTransform.right * random.x + camTransform.up * random.y).normalized;

        Vector3 target = camTransform.position + aimDir * 200f;

        RaycastHit[] hits = Physics.RaycastAll(camTransform.position, aimDir, 200f, ~0, QueryTriggerInteraction.Ignore);
        float closest = float.MaxValue;

        foreach (RaycastHit hit in hits)
        {
            if (hit.collider.GetComponentInParent<CharacterController>() != null) continue;
            if (hit.distance < closest)
            {
                closest = hit.distance;
                target = hit.point;
            }
        }

        return (target - bulletSpawnPoint.position).normalized;
    }

    private void IgnoreShooterCollisions(GameObject newBullet)
    {
        Collider bulletCollider = newBullet.GetComponentInChildren<Collider>();
        if (bulletCollider == null) return;

        foreach (Collider shooterCollider in _shooterColliders)
            Physics.IgnoreCollision(bulletCollider, shooterCollider);
    }

    IEnumerator Reload()
    {
        _isReloading = true;
        Debug.Log("Reloading...");
        transform.localPosition = initialPosition;
        Quaternion targetRotation = Quaternion.Euler(initialRotation.eulerAngles + reloadRotationOffset);
        float halfReloadTime = reloadTime / 2f;
        float t = 0f;
        while (t < halfReloadTime)
        {
            t += Time.deltaTime;
            transform.localRotation = Quaternion.Slerp(initialRotation, targetRotation, t / halfReloadTime);
            yield return null;
        }

        t = 0f;

        while (t < halfReloadTime)
        {
            t += Time.deltaTime;
            transform.localRotation = Quaternion.Slerp(targetRotation, initialRotation, t / halfReloadTime);
            yield return null;
        }

        _currentAmmo = magSize;
        _recoil = 0f;
        _isReloading = false;
    }

    public void TryReloading()
    {
        if (_isReloading || _currentAmmo == magSize)
            return;
        StartCoroutine(Reload());
    }
}
