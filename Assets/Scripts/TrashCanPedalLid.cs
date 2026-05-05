using UnityEngine;

public class TrashCanLidToggle : MonoBehaviour
{
    public Transform lid;

    public Vector3 closedRotation = Vector3.zero;
    public Vector3 openRotation = new Vector3(-75f, 0f, 0f);

    public float speed = 8f;

    private bool isOpen = false;

    void Update()
    {
        if (lid == null) return;

        Quaternion targetRotation = Quaternion.Euler(isOpen ? openRotation : closedRotation);

        lid.localRotation = Quaternion.Lerp(
            lid.localRotation,
            targetRotation,
            Time.deltaTime * speed
        );
    }

    public void ToggleLid()
    {
        isOpen = !isOpen;
    }
}