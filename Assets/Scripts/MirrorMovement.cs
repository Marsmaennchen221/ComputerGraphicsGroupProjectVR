using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(Camera))]
public class MirrorCameraController : MonoBehaviour
{
    [Header("References")]
    public Camera playerCamera;
    public Transform mirrorPlane;

    [Header("Settings")]
    public bool copyCameraSettings = true;
    public bool useObliqueClipping = false;
    public float clipPlaneOffset = 0.05f;

    private Camera mirrorCamera;

    private void Awake()
    {
        mirrorCamera = GetComponent<Camera>();
    }

    private void LateUpdate()
    {
        if (playerCamera == null || mirrorPlane == null)
            return;

        if (mirrorCamera == null)
            mirrorCamera = GetComponent<Camera>();

        if (copyCameraSettings)
            CopyCameraSettings();

        ReflectCamera();

        if (useObliqueClipping)
            ApplyObliqueClipping();
        else
            mirrorCamera.ResetProjectionMatrix();
    }

    private void CopyCameraSettings()
    {
        mirrorCamera.fieldOfView = playerCamera.fieldOfView;
        mirrorCamera.nearClipPlane = playerCamera.nearClipPlane;
        mirrorCamera.farClipPlane = playerCamera.farClipPlane;
        mirrorCamera.orthographic = playerCamera.orthographic;
        mirrorCamera.orthographicSize = playerCamera.orthographicSize;
    }

    private void ReflectCamera()
    {
        Vector3 mirrorPosition = mirrorPlane.position;
        Vector3 mirrorNormal = mirrorPlane.forward.normalized;

        Transform playerTransform = playerCamera.transform;

        Vector3 reflectedPosition = ReflectPoint(
            playerTransform.position,
            mirrorPosition,
            mirrorNormal
        );

        Vector3 reflectedForward = Vector3.Reflect(playerTransform.forward, mirrorNormal);
        Vector3 reflectedUp = Vector3.Reflect(playerTransform.up, mirrorNormal);

        mirrorCamera.transform.position = reflectedPosition;
        mirrorCamera.transform.rotation = Quaternion.LookRotation(reflectedForward, reflectedUp);
    }

    private Vector3 ReflectPoint(Vector3 point, Vector3 planePoint, Vector3 planeNormal)
    {
        float distance = Vector3.Dot(point - planePoint, planeNormal);
        return point - 2f * distance * planeNormal;
    }

    private void ApplyObliqueClipping()
    {
        Vector3 mirrorPosition = mirrorPlane.position;
        Vector3 mirrorNormal = mirrorPlane.forward.normalized;

        Vector3 clipPosition = mirrorPosition + mirrorNormal * clipPlaneOffset;

        Vector4 clipPlane = CameraSpacePlane(
            mirrorCamera,
            clipPosition,
            mirrorNormal
        );

        mirrorCamera.projectionMatrix = mirrorCamera.CalculateObliqueMatrix(clipPlane);
    }

    private Vector4 CameraSpacePlane(Camera cam, Vector3 position, Vector3 normal)
    {
        Matrix4x4 worldToCamera = cam.worldToCameraMatrix;

        Vector3 cameraPosition = worldToCamera.MultiplyPoint(position);
        Vector3 cameraNormal = worldToCamera.MultiplyVector(normal).normalized;

        return new Vector4(
            cameraNormal.x,
            cameraNormal.y,
            cameraNormal.z,
            -Vector3.Dot(cameraPosition, cameraNormal)
        );
    }

    private void OnDrawGizmos()
    {
        if (mirrorPlane == null)
            return;

        Gizmos.color = Color.blue;
        Gizmos.DrawRay(mirrorPlane.position, mirrorPlane.forward * 1.0f);

        if (playerCamera != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawRay(playerCamera.transform.position, playerCamera.transform.forward * 1.0f);
        }

        Gizmos.color = Color.red;
        Gizmos.DrawRay(transform.position, transform.forward * 1.0f);
    }
}