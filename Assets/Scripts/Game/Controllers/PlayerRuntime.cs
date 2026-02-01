using QFramework;
using UnityEngine;

public class PlayerRuntime : MonoBehaviour, IController
{
    private const float InputUnlockDelay = 1f;
    private InputSys inputSys;
    private bool inputLocked;
    private float groundedTimer;

    public void InitializeRuntime(InputSys input)
    {
        inputSys = input;
    }

    public void OnSpawned()
    {
        LockInput("Spawn");
    }

    public void AttachCamera(Camera camera)
    {
        if (camera == null)
        {
            Debug.LogWarning("PlayerRuntime: Main Camera not found, skip attach.");
            return;
        }

        Transform cameraRoot = null;
        var fpsController = GetComponent<FirstPersonController>();
        if (fpsController != null)
        {
            cameraRoot = fpsController.CameraRoot;
        }

        if (cameraRoot == null)
        {
            var found = transform.Find("CameraRoot");
            if (found != null)
            {
                cameraRoot = found;
            }
        }

        if (cameraRoot == null)
        {
            Debug.LogWarning("PlayerRuntime: CameraRoot not found on player, skip attach.");
            return;
        }

        if (camera.transform.parent != cameraRoot)
        {
            camera.transform.SetParent(cameraRoot, false);
            camera.transform.localPosition = Vector3.zero;
            camera.transform.localRotation = Quaternion.identity;
        }

        if (fpsController != null)
        {
            if (fpsController.PlayerCamera == null)
            {
                fpsController.PlayerCamera = camera;
            }

            if (fpsController.CameraRoot == null)
            {
                fpsController.CameraRoot = cameraRoot;
            }
        }

        var interactor = GetComponent<InteractorView>();
        if (interactor != null && interactor.ViewCamera == null)
        {
            interactor.ViewCamera = camera;
        }
    }

    public void Teleport(Vector3 position, float yaw, bool useYaw, bool alignToGround, LayerMask groundLayers, float groundRayHeight, float groundRayDistance)
    {
        if (alignToGround)
        {
            position = SampleGround(position, groundLayers, groundRayHeight, groundRayDistance);
        }

        var controller = GetComponent<CharacterController>();
        var wasEnabled = false;
        if (controller != null)
        {
            wasEnabled = controller.enabled;
            controller.enabled = false;
        }

        transform.position = position;
        if (useYaw)
        {
            var euler = transform.eulerAngles;
            euler.y = yaw;
            transform.eulerAngles = euler;
        }

        if (controller != null)
        {
            controller.enabled = wasEnabled;
        }
    }

    public void TickInputLock(float deltaTime, float groundedStableTime, LayerMask groundLayers)
    {
        if (!inputLocked)
        {
            return;
        }

        if (IsGrounded(groundLayers))
        {
            groundedTimer += deltaTime;
            if (groundedTimer >= groundedStableTime + InputUnlockDelay)
            {
                UnlockInput();
            }
        }
        else
        {
            groundedTimer = 0f;
        }
    }

    private void LockInput(string reason)
    {
        if (inputSys == null)
        {
            inputSys = this.GetSystem<InputSys>();
        }

        if (inputSys != null)
        {
            inputSys.SetInputEnabled(false);
            inputLocked = true;
            groundedTimer = 0f;
            Debug.Log($"PlayerRuntime: Input locked. reason={reason}");
        }
    }

    private void UnlockInput()
    {
        if (inputSys == null)
        {
            inputSys = this.GetSystem<InputSys>();
        }

        if (inputSys != null)
        {
            inputSys.SetInputEnabled(true);
        }

        inputLocked = false;
        Debug.Log("PlayerRuntime: Input unlocked (grounded).");
    }

    private bool IsGrounded(LayerMask groundLayers)
    {
        var fpsController = GetComponent<FirstPersonController>();
        if (fpsController != null)
        {
            return fpsController.Grounded;
        }

        var controller = GetComponent<CharacterController>();
        if (controller != null)
        {
            return controller.isGrounded;
        }

        var origin = transform.position + Vector3.up * 0.1f;
        return Physics.Raycast(origin, Vector3.down, 0.3f, groundLayers, QueryTriggerInteraction.Ignore);
    }

    private static Vector3 SampleGround(Vector3 position, LayerMask groundLayers, float rayHeight, float rayDistance)
    {
        var origin = position + Vector3.up * rayHeight;
        var distance = rayDistance + rayHeight;
        if (Physics.Raycast(origin, Vector3.down, out var hit, distance, groundLayers, QueryTriggerInteraction.Ignore))
        {
            return hit.point;
        }

        return position;
    }

    public IArchitecture GetArchitecture()
    {
        return GameArchitecture.Interface;
    }
}
