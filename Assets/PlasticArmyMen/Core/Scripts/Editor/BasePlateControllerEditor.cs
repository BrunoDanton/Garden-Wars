using System.Linq;
using UnityEditor;
using UnityEngine;

namespace PlasticArmyMen.Core
{
    [CustomEditor(typeof(BasePlateController))]
    public class BasePlateControllerEditor : Editor
    {
        private SerializedProperty showBasePlateProp;
        private SerializedProperty characterRootsProp;
        private SerializedProperty leftBoneFollowProp;
        private SerializedProperty rightBoneFollowProp;
        private SerializedProperty basePlateRendererProp;
        private SerializedProperty leftBaseBoneProp;
        private SerializedProperty rightBaseBoneProp;
        private SerializedProperty leftFootTargetProp;
        private SerializedProperty rightFootTargetProp;

        private void OnEnable()
        {
            showBasePlateProp = serializedObject.FindProperty("showBasePlate");
            characterRootsProp = serializedObject.FindProperty("characterRoots");
            leftBoneFollowProp = serializedObject.FindProperty("leftBoneFollow");
            rightBoneFollowProp = serializedObject.FindProperty("rightBoneFollow");
            basePlateRendererProp = serializedObject.FindProperty("basePlateRenderer");
            leftBaseBoneProp = serializedObject.FindProperty("leftBaseBone");
            rightBaseBoneProp = serializedObject.FindProperty("rightBaseBone");
            leftFootTargetProp = serializedObject.FindProperty("leftFootTarget");
            rightFootTargetProp = serializedObject.FindProperty("rightFootTarget");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.LabelField("Baseplate Visibility & Foot Follow Settings", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(showBasePlateProp);
            EditorGUILayout.PropertyField(characterRootsProp);

            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(leftBoneFollowProp);
            EditorGUILayout.PropertyField(rightBoneFollowProp);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("References", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(basePlateRendererProp);
            EditorGUILayout.PropertyField(leftBaseBoneProp);
            EditorGUILayout.PropertyField(rightBaseBoneProp);
            EditorGUILayout.PropertyField(leftFootTargetProp);
            EditorGUILayout.PropertyField(rightFootTargetProp);

            EditorGUILayout.Space(10);

            if (GUILayout.Button("Auto Find References"))
            {
                AutoFindReferences();
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void AutoFindReferences()
        {
            BasePlateController controller = (BasePlateController)target;
            Transform root = controller.transform;

            Undo.RecordObject(controller, "Auto Find BasePlate References");
            serializedObject.Update();

            // -----------------------------
            // Character Roots (ARRAY)
            // -----------------------------
            if (characterRootsProp != null && characterRootsProp.arraySize == 0)
            {
                characterRootsProp.arraySize = 1;
                characterRootsProp.GetArrayElementAtIndex(0).objectReferenceValue = root;
            }

            // -----------------------------
            // Baseplate renderer
            // -----------------------------
            if (basePlateRendererProp.objectReferenceValue == null)
            {
                basePlateRendererProp.objectReferenceValue = root
                    .GetComponentsInChildren<SkinnedMeshRenderer>(true)
                    .FirstOrDefault(r =>
                        r.name.ToLower().Contains("base") ||
                        r.name.ToLower().Contains("plate"));
            }

            // -----------------------------
            // Bones & feet
            // -----------------------------
            Transform[] allChildren = root.GetComponentsInChildren<Transform>(true);

            if (leftBaseBoneProp.objectReferenceValue == null)
                leftBaseBoneProp.objectReferenceValue =
                    FindByName(allChildren, "left", "base", "plate");

            if (rightBaseBoneProp.objectReferenceValue == null)
                rightBaseBoneProp.objectReferenceValue =
                    FindByName(allChildren, "right", "base", "plate");

            if (leftFootTargetProp.objectReferenceValue == null)
                leftFootTargetProp.objectReferenceValue =
                    FindByName(allChildren, "ankle_l", "foot_l", "left");

            if (rightFootTargetProp.objectReferenceValue == null)
                rightFootTargetProp.objectReferenceValue =
                    FindByName(allChildren, "ankle_r", "foot_r", "right");

            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(controller);
        }

        private Transform FindByName(Transform[] transforms, params string[] keywords)
        {
            return transforms.FirstOrDefault(t =>
                keywords.All(k => t.name.ToLower().Contains(k)));
        }
    }
}
