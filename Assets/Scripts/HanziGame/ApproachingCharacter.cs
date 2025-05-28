using System.Drawing;
using UnityEngine;

public class ApproachingCharacter : MonoBehaviour
{
    private Transform target;
    private float speed;
    private bool isMoving = true;

    public void Init(Transform playerHead, float moveSpeed)
    {
        target = playerHead;
        speed = moveSpeed;
    }

    void Update()
    {
        if (!isMoving) return;

        Vector3 targetPos = target.position;
        Renderer renderer = transform.GetComponent<Renderer>();
        if (renderer != null)
        {
            Vector3 size = renderer.bounds.size;
            targetPos = new Vector3(
                target.position.x - (size.x / 2),
                target.position.y - (size.y / 2),
                target.position.z - (size.z / 2)
            );
        }

            // Move toward player
            transform.position = Vector3.MoveTowards(
                transform.position,
                targetPos,
                speed * Time.deltaTime
            );

        // Rotate to always face player
        transform.LookAt(targetPos);

        // Destroy if too close
        if (Vector3.Distance(transform.position, targetPos) < 0.5f)
        {
            try
            {
                Destroy(gameObject);
                HanziSpawner.Instance.SpawnCharacter(true);
            }
            catch
            {
                UnityEngine.Debug.Log("Could not delete object");
            }
            // TODO: Player penalty logic
        }
    }

    public void StopMoving() => isMoving = false;
}