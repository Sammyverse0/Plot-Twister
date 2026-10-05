using UnityEngine;
using UnityEngine.InputSystem;

public class FollowCursorSimple : MonoBehaviour
{
    public Camera targetCamera;
    public float lookDistance = 2f;
    public float rotateSpeed = 10f;
    public float maxAngle = 60f;
    public Vector3 rotationOffset;

    private Quaternion _baseLocalRotation;

    void Start()
    {
        if (!targetCamera)
            targetCamera = Camera.main;

        _baseLocalRotation = transform.localRotation;
    }

    void LateUpdate()
    {
        if (Mouse.current == null || targetCamera == null) return;

        Vector2 screenPos = Mouse.current.position.ReadValue();

        float headDepth = targetCamera.WorldToScreenPoint(transform.position).z;
        float depth = Mathf.Max(0.1f, headDepth - lookDistance);
        Vector3 lookPoint = targetCamera.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, depth));

        Vector3 direction = lookPoint - transform.position;
        if (direction.sqrMagnitude < 0.0001f) return;

        Quaternion targetWorld = Quaternion.LookRotation(direction) * Quaternion.Euler(rotationOffset);
        Quaternion parentRotation = transform.parent != null ? transform.parent.rotation : Quaternion.identity;
        Quaternion targetLocal = Quaternion.Inverse(parentRotation) * targetWorld;
        targetLocal = Quaternion.RotateTowards(_baseLocalRotation, targetLocal, maxAngle);

        transform.localRotation = Quaternion.Slerp(transform.localRotation, targetLocal, rotateSpeed * Time.deltaTime);
    }
}