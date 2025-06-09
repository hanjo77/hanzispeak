using TMPro;
using UnityEngine;

public class FlyInPinyin : MonoBehaviour
{
    public TextMeshPro textPrefab;
    public float flightSpeed = 2.0f;
    public float fadeOutDistance = 50.0f;
    public float fadeOutThreshold = 0.01f;
    private HanziCharacter targetHanzi;

    public void Fly(string pinyin, bool isCorrect, Transform sourceTransform, HanziCharacter hanzi)
    {
        targetHanzi = hanzi;
        TextMeshPro clone = Instantiate(textPrefab);
        clone.gameObject.tag = "PinyinHintClone";
        clone.text = pinyin;

        // Assign a new material so we can modify it
        Material mat = new Material(clone.fontMaterial);
        clone.fontMaterial = mat;
        SetAlpha(mat, 0f);

        // Get static flight direction
        Vector3 startPos = sourceTransform.position;
        Vector3 targetPos = targetHanzi.transform.position;
        Vector3 direction = targetPos - startPos;

        if (!isCorrect)
        {
            // Apply a miss offset angle
            Vector3 missAxis = Vector3.Cross(Vector3.up, direction).normalized;
            float missAngle = Random.Range(-30f, 30f);
            Quaternion missRotation = Quaternion.AngleAxis(missAngle, missAxis);
            direction = (missRotation * direction).normalized;
            missAxis = Vector3.Cross(Vector3.left, direction).normalized;
            missAngle = Random.Range(-30f, 30f);
            missRotation = Quaternion.AngleAxis(missAngle, missAxis);
            direction = (missRotation * direction).normalized;
        }
        else
        {
            targetHanzi.OnRecognized();
        }

        clone.transform.position = startPos;
        clone.transform.rotation = Quaternion.LookRotation(direction);

        StartCoroutine(FlyRoutine(clone, mat, direction, isCorrect));
    }

    private System.Collections.IEnumerator FlyRoutine(TextMeshPro tmp, Material mat, Vector3 direction, bool isCorrect)
    {
        float traveled = 0f;
        float alpha = 0f;

        Color baseColor = isCorrect ? Color.green : Color.red;

        while (true)
        {
            float delta = flightSpeed * Time.deltaTime;
            traveled += delta;
            tmp.transform.position += direction * delta;

            // Fade in at start
            if (traveled < 0.3f)
            {
                alpha = Mathf.Clamp01(traveled / 0.3f);
            }
            else if (!isCorrect)
            {
                // Begin fade out past halfway
                float fadeStart = fadeOutDistance * 0.5f;
                if (traveled > fadeStart)
                {
                    float fadeProgress = (traveled - fadeStart) / (fadeOutDistance - fadeStart);
                    alpha = Mathf.Clamp01(1f - fadeProgress);
                }
            }

            SetAlpha(mat, alpha);

            // End conditions
            if (!isCorrect && alpha <= fadeOutThreshold)
            {
                Destroy(tmp.gameObject);
                yield break;
            }

            yield return null;
        }
    }

    private void SetAlpha(Material mat, float alpha)
    {
        if (mat.HasProperty("_FaceColor"))
        {
            Color c = mat.GetColor("_FaceColor");
            c.a = alpha;
            mat.SetColor("_FaceColor", c);
        }
    }
}
