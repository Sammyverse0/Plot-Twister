using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class PlayerShooting : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Gun gun;
    [SerializeField] private InputActionReference shootAction;
    [SerializeField] private InputActionReference reloadAction;
    [SerializeField] private TMP_Text ammoText;

    private bool _isHoldingShoot;

    private void OnEnable()
    {
        shootAction.action.started += OnShootStarted;
        shootAction.action.canceled += OnShootCanceled;
        reloadAction.action.performed += OnReload;
    }

    private void OnDisable()
    {
        shootAction.action.started -= OnShootStarted;
        shootAction.action.canceled -= OnShootCanceled;
        reloadAction.action.performed -= OnReload;
    }

    private void OnShootStarted(InputAction.CallbackContext context) => _isHoldingShoot = true;
    private void OnShootCanceled(InputAction.CallbackContext context) => _isHoldingShoot = false;

    private void OnReload(InputAction.CallbackContext context)
    {
        if (gun != null)
        {
            gun.TryReloading();
        }
    }

    private void Update()
    {
        if (_isHoldingShoot && gun != null)
        {
            gun.Shoot();
        }

        ammoText.text = $"{gun._currentAmmo}/{gun.magSize}";

    }
}