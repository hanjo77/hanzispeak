using UnityEngine;

public class CameraShake : MonoBehaviour
{
    [Header("Shake Settings")]
    public Transform targetObject;
    public float shakeDuration = 0.1f;    // Total shake time
    public float shakeIntensity = 0.5f;   // Max position/rotation offset
    public float rotationShakeScale = 0.5f; // How much rotation is applied (vs position)

    private Vector3 originalPosition;
    private Quaternion originalRotation;
    private bool isShaking = false;

    void Awake()
    {
        // Store the original transform values
        originalPosition = targetObject.localPosition;
        originalRotation = targetObject.localRotation;
    }

    // Call this to trigger the shake
    public void StartShake()
    {
        if (!isShaking)
        {
            StartCoroutine(ShakeCoroutine());
        }
    }

    private System.Collections.IEnumerator ShakeCoroutine()
    {
        isShaking = true;
        float elapsed = 0f;

        while (elapsed < shakeDuration)
        {
            // Fade out the intensity over time
            float fade = 1 - (elapsed / shakeDuration); // 1 -> 0
            float currentIntensity = shakeIntensity * fade;

            // Generate Perlin noise-based offsets (smoother than Random.insideUnitSphere)
            float noiseX = Mathf.PerlinNoise(Time.time * 10f, 0) * 2 - 1;
            float noiseY = Mathf.PerlinNoise(0, Time.time * 10f) * 2 - 1;
            float noiseZ = Mathf.PerlinNoise(Time.time * 10f, Time.time * 10f) * 2 - 1;

            // Apply position shake
            targetObject.localPosition = originalPosition + new Vector3(
                noiseX * currentIntensity,
                noiseY * currentIntensity,
                noiseZ * currentIntensity
            );

            // Apply rotation shake (scaled down for subtlety)
            targetObject.localRotation = originalRotation * Quaternion.Euler(
                noiseX * currentIntensity * rotationShakeScale,
                noiseY * currentIntensity * rotationShakeScale,
                noiseZ * currentIntensity * rotationShakeScale
            );

            elapsed += Time.deltaTime;

            yield return null; // Wait one frame
        }
        UnityEngine.Debug.Log("Finished loop");

        // Reset to original transform
        StopAllCoroutines();
        targetObject.localPosition = originalPosition;
        targetObject.localRotation = originalRotation;
        isShaking = false;
    }
}