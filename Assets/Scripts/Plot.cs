using UnityEngine;

public enum PipeShape { Straight, Elbow }

public class Plot : MonoBehaviour
{
    public PipeShape shape;
    [HideInInspector] public int rotationState;

    public void Twist()
    {
        rotationState = (rotationState + 1) % 4;
        transform.localRotation = Quaternion.Euler(0f, rotationState * 90f, 0f);
    }
}