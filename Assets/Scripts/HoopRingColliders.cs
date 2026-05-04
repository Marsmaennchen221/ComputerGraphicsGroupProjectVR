using UnityEngine;

public class HoopRingColliders : MonoBehaviour
{
    [SerializeField] float radius = 0.23f;
    [SerializeField] Vector3 positionOffset = Vector3.zero;
    [SerializeField] float colliderLength = 0.12f;
    [SerializeField] float colliderRadius = 0.015f;
    [SerializeField] int colliderCount = 16;

    void Start()
    {
        CreateRingColliders();
    }

    [ContextMenu("Create Ring Colliders")]
    public void CreateRingColliders()
    {
        ClearOldColliders();

        for (int i = 0; i < colliderCount; i++)
        {
            float angle = i * Mathf.PI * 2f / colliderCount;

            GameObject colliderObject = new GameObject("RimCollider_" + i);
            colliderObject.transform.SetParent(transform, false);
            colliderObject.transform.localPosition = positionOffset + new Vector3(
                Mathf.Cos(angle) * radius,
                0f,
                Mathf.Sin(angle) * radius
            );
            colliderObject.transform.localRotation = Quaternion.Euler(0f, 90f - angle * Mathf.Rad2Deg, 0f);

            CapsuleCollider capsuleCollider = colliderObject.AddComponent<CapsuleCollider>();
            capsuleCollider.radius = colliderRadius;
            capsuleCollider.height = colliderLength;
            capsuleCollider.direction = 0;
            capsuleCollider.isTrigger = false;
        }
    }

    void ClearOldColliders()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Transform child = transform.GetChild(i);
            if (!child.name.StartsWith("RimCollider_"))
                continue;

            if (Application.isPlaying)
                Destroy(child.gameObject);
            else
                DestroyImmediate(child.gameObject);
        }
    }
}
