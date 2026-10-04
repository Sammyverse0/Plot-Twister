using UnityEngine;
using System.Collections;
using System;


public class Gun : MonoBehaviour
{
    [Header("Gun Settings")]
    [SerializeField] private float reloadTime = 1f;
    [SerializeField] private float fireRate = 0.3f;
    [SerializeField] public int magSize = 9;

    [Header("References")]
    [SerializeField] private GameObject bullet;
    [SerializeField] private Transform bulletSpawnPoint;
    

    public int _currentAmmo;
    private bool _isReloading=false;
    private float _nextTimeToFire = 0f;

    private Quaternion initialRotation;
    private Vector3 initialPosition;
    private Vector3 reloadRotationOffset = new Vector3(66,50,50); 
    
    private void Start()
    {
        _currentAmmo = magSize;
        initialRotation = transform.localRotation;
        initialPosition = transform.localPosition;
    }

    
    

    public void Shoot()
    {
        if (_isReloading)
            return;
        if(Time.time < _nextTimeToFire)
            return;
        if (_currentAmmo <= 0)
        {
            StartCoroutine(Reload());
            return;
        }
        if (Time.time >= _nextTimeToFire)
        {
            _nextTimeToFire = Time.time + fireRate;
            Instantiate(bullet, bulletSpawnPoint.position, bulletSpawnPoint.rotation);
            _currentAmmo--;
        }
    }

    IEnumerator Reload()
    {
        _isReloading = true;
        Debug.Log("Reloading...");
        Quaternion targetRotation = Quaternion.Euler(initialRotation.eulerAngles + reloadRotationOffset);
        float halfReloadTime = reloadTime / 2f;
        float t = 0f;
        while(t < halfReloadTime)
        {
            t += Time.deltaTime; 
            transform.localRotation = Quaternion.Slerp(initialRotation, targetRotation, t/halfReloadTime);
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
        _isReloading = false;
    }

    public void TryReloading()
    {
        if (_isReloading || _currentAmmo == magSize)
            return;
        StartCoroutine(Reload());
    }
}
