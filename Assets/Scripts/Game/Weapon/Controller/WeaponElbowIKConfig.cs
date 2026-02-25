using UnityEngine;

[DisallowMultipleComponent]
public class WeaponElbowIKConfig : MonoBehaviour
{
    private const string DefaultLeftHandGripName = "LeftHandGrip";
    private const string DefaultRightHandGripName = "RightHandGrip";

    [Header("Hand IK")]
    public bool EnableHandIK = true;
    public bool EnableLeftHandIK = true;
    public bool EnableRightHandIK = false;
    public Transform LeftHandIKTarget;
    public Transform RightHandIKTarget;

    [Header("Elbow IK")]
    public bool EnableElbowIK = false;
    public bool EnableLeftElbowIK = false;
    public bool EnableRightElbowIK = false;
    public Vector3 LeftElbowHintLocalPosition;
    public Vector3 LeftElbowHintLocalEulerAngles;
    public Vector3 RightElbowHintLocalPosition;
    public Vector3 RightElbowHintLocalEulerAngles;

    private void Reset()
    {
        LeftHandIKTarget = transform.GetChild(DefaultLeftHandGripName);
        RightHandIKTarget = transform.GetChild(DefaultRightHandGripName);
    }
}
