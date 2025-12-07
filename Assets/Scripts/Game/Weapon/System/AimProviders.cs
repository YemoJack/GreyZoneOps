using UnityEngine;

public interface IAimRayProvider
{
    Ray GetAimRay();
    Vector3 GetAimForward();
}

public class CameraAimProvider : IAimRayProvider
{
    private readonly Camera camera;

    public CameraAimProvider(Camera camera)
    {
        this.camera = camera;
    }

    public Ray GetAimRay()
    {
        if (camera == null)
        {
            return new Ray(Vector3.zero, Vector3.forward);
        }

        return camera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
    }

    public Vector3 GetAimForward()
    {
        if (camera == null)
        {
            return Vector3.forward;
        }

        return camera.transform.forward.normalized;
    }
}
