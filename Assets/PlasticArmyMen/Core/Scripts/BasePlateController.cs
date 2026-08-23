using UnityEngine;

namespace PlasticArmyMen.Core
{
    [ExecuteAlways]
    [SelectionBase]
    public class BasePlateController : MonoBehaviour
    {
        [Header("Baseplate")]
        [SerializeField] private SkinnedMeshRenderer basePlateRenderer;
        private bool autoDetectThickness = true;

        [Header("Baseplate Bones")]
        [SerializeField] private Transform leftBaseBone;
        [SerializeField] private Transform rightBaseBone;

        [Tooltip("Transforms to offset when baseplate is hidden (eg character root, backpack, accessories)")]
        [SerializeField] private Transform[] characterRoots;

        [Header("Foot Targets")]
        [SerializeField] private Transform leftFootTarget;
        [SerializeField] private Transform rightFootTarget;

        [Header("Follow Strength")]
        [Range(0f, 1f)][SerializeField] private float leftBoneFollow = 1f;
        [Range(0f, 1f)][SerializeField] private float rightBoneFollow = 1f;

        [Header("Visibility")]
        [SerializeField] private bool showBasePlate = true;

        // Cached default (bind) pose
        private Vector3 leftDefaultLocalPos;
        private Quaternion leftDefaultLocalRot;

        private Vector3 rightDefaultLocalPos;
        private Quaternion rightDefaultLocalRot;

        // Cached bind pose offsets
        private Vector3 leftPosOffset;
        private Quaternion leftRotOffset;

        private Vector3 rightPosOffset;
        private Quaternion rightRotOffset;

        private bool offsetsCached;
        private bool bindPoseCached;

        // Cached bind/root position
        private float cachedBasePlateThickness;
        private bool thicknessCached;
        private bool lastShowBasePlate;



        private void OnEnable()
        {
            CacheBindPoseOnce();
            CacheBasePlateThicknessOnce();
            ApplyVisibility();

            lastShowBasePlate = showBasePlate;

#if UNITY_EDITOR
            UnityEditor.Undo.undoRedoPerformed += OnUndoRedo;
            UnityEditor.EditorApplication.hierarchyChanged += ForceUpdate;
#endif
        }

        private void OnDisable()
        {
#if UNITY_EDITOR
            UnityEditor.Undo.undoRedoPerformed -= OnUndoRedo;
            UnityEditor.EditorApplication.hierarchyChanged -= ForceUpdate;
#endif
        }

        private void OnUndoRedo()
        {
#if UNITY_EDITOR
            // Delay one editor tick so transforms are fully restored
            UnityEditor.EditorApplication.delayCall += () =>
            {
                if (this == null) return;

                ForceUpdate();
            };
#endif
        }

        private void ForceUpdate()
        {
            if (!offsetsCached) return;

            ApplyBoneFollow(
                leftBaseBone,
                leftFootTarget,
                leftDefaultLocalPos,
                leftDefaultLocalRot,
                leftPosOffset,
                leftRotOffset,
                leftBoneFollow
            );

            ApplyBoneFollow(
                rightBaseBone,
                rightFootTarget,
                rightDefaultLocalPos,
                rightDefaultLocalRot,
                rightPosOffset,
                rightRotOffset,
                rightBoneFollow
            );

#if UNITY_EDITOR
            // Ensure scene view updates immediately
            UnityEditor.SceneView.RepaintAll();
#endif
        }

        private void OnValidate()
        {
            CacheBindPoseOnce();

            thicknessCached = false;   // allow recalculation when toggling
            CacheBasePlateThicknessOnce();
        }

        private void LateUpdate()
        {
            if (!offsetsCached) return;

            // Handle animated visibility
            if (basePlateRenderer && basePlateRenderer.enabled != showBasePlate)
            {
                basePlateRenderer.enabled = showBasePlate;
            }

            ApplyBoneFollow(
                leftBaseBone,
                leftFootTarget,
                leftDefaultLocalPos,
                leftDefaultLocalRot,
                leftPosOffset,
                leftRotOffset,
                leftBoneFollow
            );

            ApplyBoneFollow(
                rightBaseBone,
                rightFootTarget,
                rightDefaultLocalPos,
                rightDefaultLocalRot,
                rightPosOffset,
                rightRotOffset,
                rightBoneFollow
            );

            if (showBasePlate != lastShowBasePlate)
            {
                ApplyRootOffsetDelta(lastShowBasePlate, showBasePlate);
                lastShowBasePlate = showBasePlate;
            }
        }


        private void ApplyBoneFollow(
            Transform bone,
            Transform target,
            Vector3 defaultLocalPos,
            Quaternion defaultLocalRot,
            Vector3 posOffset,
            Quaternion rotOffset,
            float weight)
        {
            if (!bone || !target) return;

            // World-space target pose
            Vector3 targetWorldPos = target.TransformPoint(posOffset);
            Quaternion targetWorldRot = target.rotation * rotOffset;

            // Convert target pose into bone parent space
            Transform parent = bone.parent;
            Vector3 targetLocalPos = parent.InverseTransformPoint(targetWorldPos);
            Quaternion targetLocalRot =
                Quaternion.Inverse(parent.rotation) * targetWorldRot;

            // Blend from default pose → target pose
            bone.localPosition = Vector3.Lerp(
                defaultLocalPos,
                targetLocalPos,
                weight
            );

            bone.localRotation = Quaternion.Slerp(
                defaultLocalRot,
                targetLocalRot,
                weight
            );
        }

        private void ApplyVisibility()
        {
            if (basePlateRenderer)
                basePlateRenderer.enabled = showBasePlate;
        }

        private void ApplyRootOffsetDelta(bool wasVisible, bool isVisible)
        {
            if (characterRoots == null || characterRoots.Length == 0)
                return;

            Vector3 delta = Vector3.zero;

            // Turning OFF → move DOWN
            if (wasVisible && !isVisible)
            {
                delta = Vector3.down * cachedBasePlateThickness;
            }
            // Turning ON → move UP
            else if (!wasVisible && isVisible)
            {
                delta = Vector3.up * cachedBasePlateThickness;
            }

            if (delta == Vector3.zero)
                return;

            for (int i = 0; i < characterRoots.Length; i++)
            {
                Transform t = characterRoots[i];
                if (!t) continue;

                t.localPosition += delta;
            }
        }

        private void CacheBindPoseOnce()
        {
            if (bindPoseCached) return;
            if (!leftBaseBone || !rightBaseBone || !leftFootTarget || !rightFootTarget)
                return;

            // Bind pose
            leftDefaultLocalPos = leftBaseBone.localPosition;
            leftDefaultLocalRot = leftBaseBone.localRotation;

            rightDefaultLocalPos = rightBaseBone.localPosition;
            rightDefaultLocalRot = rightBaseBone.localRotation;

            // Offsets FROM BIND POSE (critical!)
            leftPosOffset = leftFootTarget.InverseTransformPoint(leftBaseBone.position);
            leftRotOffset = Quaternion.Inverse(leftFootTarget.rotation) * leftBaseBone.rotation;

            rightPosOffset = rightFootTarget.InverseTransformPoint(rightBaseBone.position);
            rightRotOffset = Quaternion.Inverse(rightFootTarget.rotation) * rightBaseBone.rotation;

            bindPoseCached = true;
            offsetsCached = true;
        }

        private void CacheBasePlateThicknessOnce()
        {
            if (thicknessCached) return;
            if (!basePlateRenderer || !basePlateRenderer.sharedMesh) return;

            Bounds meshBounds = basePlateRenderer.sharedMesh.bounds;
            float meshHeight = meshBounds.size.y;

            cachedBasePlateThickness = meshHeight / 10;

            thicknessCached = true;
        }
    }
}

