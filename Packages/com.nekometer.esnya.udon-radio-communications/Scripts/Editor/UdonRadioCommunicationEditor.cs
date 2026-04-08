using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using UdonSharp;
using UdonSharpEditor;
using VRC.Udon;
using VRC.SDKBase.Editor.BuildPipeline;

namespace URC.Editor
{
    [CustomEditor(typeof(UdonRadioCommunication))]
    public class UdonRadioCommunicationEditor : UnityEditor.Editor
    {
        private static IEnumerable<T> GetComponentsInScene<T>() where T : UdonSharpBehaviour
        {
            return FindObjectsByType<UdonBehaviour>(FindObjectsSortMode.None)
                .Where(UdonSharpEditorUtility.IsUdonSharpBehaviour)
                .Select(UdonSharpEditorUtility.GetProxyBehaviour)
                .OfType<T>();
        }

        private ReorderableList audioObjectTemplatesList;

        private void OnEnable()
        {
            var audioObjectTemplatesProperty = serializedObject.FindProperty(nameof(UdonRadioCommunication.audioObjectTemplates));
            var audioObjectFrequenciesProperty = serializedObject.FindProperty(nameof(UdonRadioCommunication.audioObjectFrequencies));
            audioObjectTemplatesList = new ReorderableList(serializedObject, audioObjectTemplatesProperty)
            {
                drawHeaderCallback = (rect) =>
                {
                    var itemRect = rect;
                    itemRect.width /= 2;
                    EditorGUI.LabelField(itemRect, "Template");
                    itemRect.x += itemRect.width;
                    EditorGUI.LabelField(itemRect, "Frequency");
                },
                drawElementCallback = (rect, index, isActive, isFocused) =>
                {
                    var itemRect = rect;
                    itemRect.width /= 2;
                    EditorGUI.PropertyField(itemRect, audioObjectTemplatesProperty.GetArrayElementAtIndex(index), GUIContent.none);
                    itemRect.x += itemRect.width;
                    EditorGUI.PropertyField(itemRect, audioObjectFrequenciesProperty.GetArrayElementAtIndex(index), GUIContent.none);
                },
                onAddCallback = (list) =>
                {
                    audioObjectTemplatesProperty.arraySize += 1;
                    audioObjectFrequenciesProperty.arraySize = audioObjectTemplatesProperty.arraySize;
                },
                onRemoveCallback = (list) =>
                {
                    audioObjectTemplatesProperty.arraySize -= 1;
                    audioObjectFrequenciesProperty.arraySize = audioObjectTemplatesProperty.arraySize;
                },
                onCanRemoveCallback = (list) => audioObjectTemplatesProperty.arraySize >= 1,
                onReorderCallbackWithDetails = (list, oldIndex, newIndex) =>
                {
                    audioObjectTemplatesProperty.MoveArrayElement(oldIndex, newIndex);
                    audioObjectFrequenciesProperty.MoveArrayElement(oldIndex, newIndex);
                },
            };

            serializedObject.Update();
            audioObjectFrequenciesProperty.arraySize = audioObjectTemplatesProperty.arraySize;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        public override void OnInspectorGUI()
        {
            if (UdonSharpGUI.DrawDefaultUdonSharpBehaviourHeader(target)) return;

            serializedObject.Update();

            var property = serializedObject.GetIterator();
            property.NextVisible(true);
            do
            {
                switch (property.name)
                {
                    case nameof(UdonRadioCommunication.audioObjectTemplates):
                        EditorGUILayout.Space();
                        EditorGUILayout.LabelField("Audio Objects");
                        audioObjectTemplatesList.DoLayoutList();
                        break;
                    case nameof(UdonRadioCommunication.audioObjectFrequencies):
                        break;
                    default:
                        EditorGUILayout.PropertyField(property, true);
                        break;
                }
            } while (property.NextVisible(false));

            serializedObject.ApplyModifiedProperties();

            EditorGUILayout.Space();

            if (GUILayout.Button("Setup"))
            {
                var urc = (UdonRadioCommunication)target;
                urc.Setup();
            }
        }

        private static void SetupAll()
        {
            foreach (var urc in GetComponentsInScene<UdonRadioCommunication>())
            {
                Debug.Log($"[{urc.gameObject.name}] Auto setup");
                urc.Setup();
            }
        }

        public class BuildCallback : IVRCSDKBuildRequestedCallback
        {
            public int callbackOrder => 10;

            public bool OnBuildRequested(VRCSDKRequestedBuildType requestedBuildType)
            {
                SetupAll();
                return true;
            }
        }
    }
}
