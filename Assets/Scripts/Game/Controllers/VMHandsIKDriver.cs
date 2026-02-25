using QFramework;
using UnityEngine;

[DisallowMultipleComponent]
public class VMHandsIKDriver : MonoBehaviour, IController
{
    private const string DefaultLeftHandGripName = "LeftHandGrip";
    private const string DefaultRightHandGripName = "RightHandGrip";

    [Header("References")]
    public Animator HandsAnimator;

    [Header("Hand IK")]
    [Range(0f, 1f)] public float LeftHandPositionWeight = 1f;
    [Range(0f, 1f)] public float LeftHandRotationWeight = 1f;
    [Range(0f, 1f)] public float RightHandPositionWeight = 1f;
    [Range(0f, 1f)] public float RightHandRotationWeight = 1f;

    [Header("Elbow Hints (Optional)")]
    [Tooltip("Used as fallback when the equipped weapon has no WeaponElbowIKConfig component.")]
    public Transform LeftElbowHint;
    [Tooltip("Used as fallback when the equipped weapon has no WeaponElbowIKConfig component.")]
    public Transform RightElbowHint;

    private WeaponSystem weaponSystem;
    private WeaponBase cachedWeapon;
    private Transform cachedLeftGrip;
    private Transform cachedRightGrip;
    private WeaponElbowIKConfig cachedWeaponElbowIKConfig;
    private bool cachedUseLeftHandGripIK;
    private bool cachedUseRightHandGripIK;
    private Transform cachedLeftElbowHint;
    private Transform cachedRightElbowHint;
    private bool cachedUseLeftElbowIK;
    private bool cachedUseRightElbowIK;
    private bool warnedAnimatorMissing;
    private bool warnedHumanoidRequired;

    private void Awake()
    {
        ResolveAnimator();
    }

    private void Reset()
    {
        ResolveAnimator();
    }

    private void LateUpdate()
    {
        if (weaponSystem == null)
        {
            weaponSystem = this.GetSystem<WeaponSystem>();
        }
    }

    private void OnAnimatorIK(int layerIndex)
    {
        if (!EnsureAnimatorReady())
        {
            return;
        }

        if (weaponSystem == null)
        {
            weaponSystem = this.GetSystem<WeaponSystem>();
            if (weaponSystem == null)
            {
                ClearHandIK();
                return;
            }
        }

        var currentWeapon = weaponSystem.GetCurrentWeapon();
        if (currentWeapon != cachedWeapon)
        {
            CacheWeaponGripTargets(currentWeapon);
        }

        var hasWeaponEquipped = cachedWeapon != null;

        ApplyHandIK(
            AvatarIKGoal.LeftHand,
            (hasWeaponEquipped && cachedUseLeftHandGripIK) ? cachedLeftGrip : null,
            LeftHandPositionWeight,
            LeftHandRotationWeight);

        ApplyHandIK(
            AvatarIKGoal.RightHand,
            (hasWeaponEquipped && cachedUseRightHandGripIK) ? cachedRightGrip : null,
            RightHandPositionWeight,
            RightHandRotationWeight);

        UpdateDriverElbowHintTransforms(hasWeaponEquipped);

        var leftElbowTarget = hasWeaponEquipped && cachedUseLeftElbowIK ? cachedLeftElbowHint : null;
        var rightElbowTarget = hasWeaponEquipped && cachedUseRightElbowIK ? cachedRightElbowHint : null;

        // With a weapon equipped, enabled elbow hints use full weight; otherwise force 0 to avoid arm flipping.
        ApplyHintIK(AvatarIKHint.LeftElbow, leftElbowTarget, leftElbowTarget != null ? 1f : 0f);
        ApplyHintIK(AvatarIKHint.RightElbow, rightElbowTarget, rightElbowTarget != null ? 1f : 0f);
    }

    private void CacheWeaponGripTargets(WeaponBase weapon)
    {
        cachedWeapon = weapon;
        cachedLeftGrip = null;
        cachedRightGrip = null;
        cachedWeaponElbowIKConfig = null;
        cachedUseLeftHandGripIK = false;
        cachedUseRightHandGripIK = false;
        cachedLeftElbowHint = null;
        cachedRightElbowHint = null;
        cachedUseLeftElbowIK = false;
        cachedUseRightElbowIK = false;

        if (cachedWeapon == null)
        {
            return;
        }

        var root = cachedWeapon.transform;
        if (root == null)
        {
            return;
        }

        cachedWeaponElbowIKConfig = root.GetComponent<WeaponElbowIKConfig>();
        CacheHandIKTargets(root);
        CacheElbowHintTargets(root);
    }

    private void CacheHandIKTargets(Transform weaponRoot)
    {
        if (weaponRoot == null)
        {
            return;
        }

        if (cachedWeaponElbowIKConfig != null)
        {
            if (cachedWeaponElbowIKConfig.EnableHandIK)
            {
                cachedUseLeftHandGripIK = cachedWeaponElbowIKConfig.EnableLeftHandIK;
                cachedUseRightHandGripIK = cachedWeaponElbowIKConfig.EnableRightHandIK;
                cachedLeftGrip = cachedWeaponElbowIKConfig.LeftHandIKTarget != null
                    ? cachedWeaponElbowIKConfig.LeftHandIKTarget
                    : weaponRoot.GetChild(DefaultLeftHandGripName);
                cachedRightGrip = cachedWeaponElbowIKConfig.RightHandIKTarget != null
                    ? cachedWeaponElbowIKConfig.RightHandIKTarget
                    : weaponRoot.GetChild(DefaultRightHandGripName);
            }

            return;
        }

        // Backward-compatible fallback: auto left-hand IK on, right-hand IK off.
        cachedUseLeftHandGripIK = true;
        cachedUseRightHandGripIK = false;
        cachedLeftGrip = weaponRoot.GetChild(DefaultLeftHandGripName);
        cachedRightGrip = weaponRoot.GetChild(DefaultRightHandGripName);
    }

    private void CacheElbowHintTargets(Transform weaponRoot)
    {
        if (weaponRoot == null)
        {
            return;
        }

        if (cachedWeaponElbowIKConfig != null)
        {
            if (cachedWeaponElbowIKConfig.EnableElbowIK)
            {
                cachedUseLeftElbowIK = cachedWeaponElbowIKConfig.EnableLeftElbowIK;
                cachedUseRightElbowIK = cachedWeaponElbowIKConfig.EnableRightElbowIK;
                cachedLeftElbowHint = LeftElbowHint;
                cachedRightElbowHint = RightElbowHint;
            }

            return;
        }

        // Backward-compatible fallback: if no per-weapon config exists, use driver-level hint references.
        cachedUseLeftElbowIK = LeftElbowHint != null;
        cachedUseRightElbowIK = RightElbowHint != null;
        cachedLeftElbowHint = LeftElbowHint;
        cachedRightElbowHint = RightElbowHint;
    }

    private void UpdateDriverElbowHintTransforms(bool hasWeaponEquipped)
    {
        if (!hasWeaponEquipped || cachedWeapon == null || cachedWeaponElbowIKConfig == null || !cachedWeaponElbowIKConfig.EnableElbowIK)
        {
            return;
        }

        var weaponRoot = cachedWeapon.transform;
        if (weaponRoot == null)
        {
            return;
        }

        if (cachedUseLeftElbowIK && LeftElbowHint != null)
        {
            ApplyWeaponRelativeHintPose(
                LeftElbowHint,
                weaponRoot,
                cachedWeaponElbowIKConfig.LeftElbowHintLocalPosition,
                cachedWeaponElbowIKConfig.LeftElbowHintLocalEulerAngles);
        }

        if (cachedUseRightElbowIK && RightElbowHint != null)
        {
            ApplyWeaponRelativeHintPose(
                RightElbowHint,
                weaponRoot,
                cachedWeaponElbowIKConfig.RightElbowHintLocalPosition,
                cachedWeaponElbowIKConfig.RightElbowHintLocalEulerAngles);
        }
    }

    private static void ApplyWeaponRelativeHintPose(Transform runtimeHint, Transform weaponRoot, Vector3 localPosition, Vector3 localEulerAngles)
    {
        if (runtimeHint == null || weaponRoot == null)
        {
            return;
        }

        runtimeHint.SetPositionAndRotation(
            weaponRoot.TransformPoint(localPosition),
            weaponRoot.rotation * Quaternion.Euler(localEulerAngles));
    }

    private void ApplyHandIK(AvatarIKGoal goal, Transform target, float posWeight, float rotWeight)
    {
        if (HandsAnimator == null)
        {
            return;
        }

        if (target == null)
        {
            HandsAnimator.SetIKPositionWeight(goal, 0f);
            HandsAnimator.SetIKRotationWeight(goal, 0f);
            return;
        }

        HandsAnimator.SetIKPositionWeight(goal, Mathf.Clamp01(posWeight));
        HandsAnimator.SetIKRotationWeight(goal, Mathf.Clamp01(rotWeight));
        HandsAnimator.SetIKPosition(goal, target.position);
        HandsAnimator.SetIKRotation(goal, target.rotation);
    }

    private void ApplyHintIK(AvatarIKHint hint, Transform hintTarget, float weight)
    {
        if (HandsAnimator == null)
        {
            return;
        }

        if (hintTarget == null || weight <= 0f)
        {
            HandsAnimator.SetIKHintPositionWeight(hint, 0f);
            return;
        }

        HandsAnimator.SetIKHintPositionWeight(hint, Mathf.Clamp01(weight));
        HandsAnimator.SetIKHintPosition(hint, hintTarget.position);
    }

    private void ClearHandIK()
    {
        if (HandsAnimator == null)
        {
            return;
        }

        HandsAnimator.SetIKPositionWeight(AvatarIKGoal.LeftHand, 0f);
        HandsAnimator.SetIKRotationWeight(AvatarIKGoal.LeftHand, 0f);
        HandsAnimator.SetIKPositionWeight(AvatarIKGoal.RightHand, 0f);
        HandsAnimator.SetIKRotationWeight(AvatarIKGoal.RightHand, 0f);
        HandsAnimator.SetIKHintPositionWeight(AvatarIKHint.LeftElbow, 0f);
        HandsAnimator.SetIKHintPositionWeight(AvatarIKHint.RightElbow, 0f);
    }

    private bool EnsureAnimatorReady()
    {
        ResolveAnimator();
        if (HandsAnimator == null)
        {
            if (!warnedAnimatorMissing)
            {
                Debug.LogWarning("VMHandsIKDriver: Animator not found.");
                warnedAnimatorMissing = true;
            }
            return false;
        }

        if (!HandsAnimator.isHuman)
        {
            if (!warnedHumanoidRequired)
            {
                Debug.LogWarning("VMHandsIKDriver: Animator Avatar must be Humanoid for OnAnimatorIK.");
                warnedHumanoidRequired = true;
            }
            ClearHandIK();
            return false;
        }

        return true;
    }

    private void ResolveAnimator()
    {
        if (HandsAnimator == null)
        {
            HandsAnimator = GetComponent<Animator>();
        }

        if (HandsAnimator == null)
        {
            HandsAnimator = GetComponentInChildren<Animator>(true);
        }
    }

    public IArchitecture GetArchitecture()
    {
        return GameArchitecture.Interface;
    }
}
