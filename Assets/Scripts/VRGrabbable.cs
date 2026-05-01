using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody))]
[AddComponentMenu("VR/VR Grabbable")]
public class VRGrabbable : MonoBehaviour
{
    [Header("Grab Settings")]
    [SerializeField] bool allowTwoHandGrab = false;
    [SerializeField] bool createBoxColliderIfMissing = true;
    [SerializeField] bool useDynamicAttach = true;
    [SerializeField] bool throwOnRelease = true;
    [SerializeField] bool useGravity = true;
    [SerializeField, Min(0.01f)] float mass = 1f;
    [SerializeField] XRBaseInteractable.MovementType movementType = XRBaseInteractable.MovementType.VelocityTracking;

    [Header("Events")]
    [SerializeField] UnityEvent onGrabbed = new UnityEvent();
    [SerializeField] UnityEvent onReleased = new UnityEvent();

    Rigidbody objectRigidbody;
    XRGrabInteractable grabInteractable;

    public Rigidbody ObjectRigidbody => objectRigidbody;
    public XRGrabInteractable GrabInteractable => grabInteractable;

    void Reset()
    {
        SetupGrabInteraction();
    }

    void Awake()
    {
        SetupGrabInteraction();
    }

    void OnEnable()
    {
        SetupGrabInteraction();

        grabInteractable.selectEntered.AddListener(HandleSelectEntered);
        grabInteractable.selectExited.AddListener(HandleSelectExited);
    }

    void OnDisable()
    {
        if (grabInteractable == null)
            return;

        grabInteractable.selectEntered.RemoveListener(HandleSelectEntered);
        grabInteractable.selectExited.RemoveListener(HandleSelectExited);
    }

    [ContextMenu("Setup Grab Interaction")]
    public void SetupGrabInteraction()
    {
        objectRigidbody = GetComponent<Rigidbody>();
        if (objectRigidbody == null)
            objectRigidbody = gameObject.AddComponent<Rigidbody>();

        objectRigidbody.mass = Mathf.Max(0.01f, mass);
        objectRigidbody.useGravity = useGravity;
        objectRigidbody.isKinematic = false;
        objectRigidbody.interpolation = RigidbodyInterpolation.Interpolate;
        objectRigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        EnsureCollider();

        grabInteractable = GetComponent<XRGrabInteractable>();
        if (grabInteractable == null)
            grabInteractable = gameObject.AddComponent<XRGrabInteractable>();

        grabInteractable.movementType = movementType;
        grabInteractable.selectMode = allowTwoHandGrab
            ? InteractableSelectMode.Multiple
            : InteractableSelectMode.Single;
        grabInteractable.useDynamicAttach = useDynamicAttach;
        grabInteractable.matchAttachPosition = true;
        grabInteractable.matchAttachRotation = true;
        grabInteractable.snapToColliderVolume = true;
        grabInteractable.reinitializeDynamicAttachEverySingleGrab = true;
        grabInteractable.throwOnDetach = throwOnRelease;
        grabInteractable.throwVelocityScale = throwOnRelease ? 1.5f : 0f;
        grabInteractable.throwAngularVelocityScale = throwOnRelease ? 1f : 0f;
        grabInteractable.forceGravityOnDetach = useGravity;
        grabInteractable.retainTransformParent = true;

        RefreshColliderList();
    }

    void EnsureCollider()
    {
        if (!createBoxColliderIfMissing || HasNonTriggerCollider())
            return;

        var boxCollider = gameObject.AddComponent<BoxCollider>();
        FitColliderToRenderers(boxCollider);
    }

    bool HasNonTriggerCollider()
    {
        var colliders = GetComponentsInChildren<Collider>(true);
        foreach (var collider in colliders)
        {
            if (collider != null && collider.enabled && !collider.isTrigger)
                return true;
        }

        return false;
    }

    void RefreshColliderList()
    {
        if (grabInteractable == null)
            return;

        grabInteractable.colliders.Clear();

        var colliders = GetComponentsInChildren<Collider>(true);
        foreach (var collider in colliders)
        {
            if (collider != null && collider.enabled)
                grabInteractable.colliders.Add(collider);
        }
    }

    void FitColliderToRenderers(BoxCollider boxCollider)
    {
        var renderers = GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
        {
            boxCollider.center = Vector3.zero;
            boxCollider.size = Vector3.one;
            return;
        }

        var hasBounds = false;
        var localBounds = new Bounds();

        foreach (var meshRenderer in renderers)
        {
            var worldBounds = meshRenderer.bounds;
            var extents = worldBounds.extents;

            EncapsulateLocalPoint(worldBounds.center + new Vector3(extents.x, extents.y, extents.z), ref localBounds, ref hasBounds);
            EncapsulateLocalPoint(worldBounds.center + new Vector3(extents.x, extents.y, -extents.z), ref localBounds, ref hasBounds);
            EncapsulateLocalPoint(worldBounds.center + new Vector3(extents.x, -extents.y, extents.z), ref localBounds, ref hasBounds);
            EncapsulateLocalPoint(worldBounds.center + new Vector3(extents.x, -extents.y, -extents.z), ref localBounds, ref hasBounds);
            EncapsulateLocalPoint(worldBounds.center + new Vector3(-extents.x, extents.y, extents.z), ref localBounds, ref hasBounds);
            EncapsulateLocalPoint(worldBounds.center + new Vector3(-extents.x, extents.y, -extents.z), ref localBounds, ref hasBounds);
            EncapsulateLocalPoint(worldBounds.center + new Vector3(-extents.x, -extents.y, extents.z), ref localBounds, ref hasBounds);
            EncapsulateLocalPoint(worldBounds.center + new Vector3(-extents.x, -extents.y, -extents.z), ref localBounds, ref hasBounds);
        }

        boxCollider.center = localBounds.center;
        boxCollider.size = localBounds.size;
    }

    void EncapsulateLocalPoint(Vector3 worldPoint, ref Bounds localBounds, ref bool hasBounds)
    {
        var localPoint = transform.InverseTransformPoint(worldPoint);
        if (!hasBounds)
        {
            localBounds = new Bounds(localPoint, Vector3.zero);
            hasBounds = true;
            return;
        }

        localBounds.Encapsulate(localPoint);
    }

    void HandleSelectEntered(SelectEnterEventArgs args)
    {
        onGrabbed.Invoke();
    }

    void HandleSelectExited(SelectExitEventArgs args)
    {
        onReleased.Invoke();
    }
}
