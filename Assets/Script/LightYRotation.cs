using UnityEngine;

public class LightYRotation : MonoBehaviour
{
    [Tooltip("Rotation speed in degrees per second")]
    public float rotationSpeed = 30f;

    private float currentYRotation = 0f;

    void Update()
    {
        // Increase rotation over time
        currentYRotation += rotationSpeed * Time.deltaTime;

        // Wrap angle between 0 and 360
        if (currentYRotation >= 360f)
            currentYRotation -= 360f;

        // Apply rotation (only on Y axis, keep original X and Z rotation)
        transform.rotation = Quaternion.Euler(currentYRotation, transform.rotation.eulerAngles.y, transform.rotation.eulerAngles.z);
    }
}
