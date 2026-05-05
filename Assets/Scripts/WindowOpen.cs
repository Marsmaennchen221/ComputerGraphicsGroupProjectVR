using System.Collections;
using UnityEngine;

public class MoveObjectY : MonoBehaviour
{
    [Header("Movement")]
    public float yAmount = -0.033f;
    public float duration = 1f;

    [Header("Behavior")]
    public bool moveOnlyOnce = true;

    private bool isMoving = false;
    private bool hasMoved = false;

    public void MoveY()
    {
        if (isMoving)
            return;

        if (moveOnlyOnce && hasMoved)
            return;

        StartCoroutine(MoveRoutine());
    }

    private IEnumerator MoveRoutine()
    {
        isMoving = true;

        Vector3 startPosition = transform.position;
        Vector3 targetPosition = startPosition + new Vector3(0f, yAmount, 0f);

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            transform.position = Vector3.Lerp(startPosition, targetPosition, t);

            yield return null;
        }

        transform.position = targetPosition;

        hasMoved = true;
        isMoving = false;
    }
}