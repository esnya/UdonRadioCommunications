using UdonSharp;
using UnityEngine;
using VRC.Udon;
using VRC.SDKBase;
#if !COMPILER_UDONSHARP && UNITY_EDITOR
using UdonSharpEditor;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
#endif

namespace UdonRadioCommunication
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    public class SFRelay : UdonSharpBehaviour
    {
        public Transceiver transceiver;
        public UdonSharpBehaviour[] onEnterEventTargets = {};
        public string[] onEnterEventNames = {};
        public UdonSharpBehaviour[] onLeaveEventTargets = {};
        public string[] onLeaveEventNames = {};

        private void SendEvents(UdonSharpBehaviour[] targets, string[] names)
        {
            for (int i = 0; i < targets.Length; i++)
            {
                var target = targets[i];
                if (target == null) continue;
                target.SendCustomEvent(names[i]);
            }
        }

        public void PilotEnter()
        {
            transceiver.exclusive = false;
            transceiver.receiver.sync = false;
            transceiver.Activate();
            transceiver.StartTalking();
            SendEvents(onEnterEventTargets, onEnterEventNames);
        }

        public void PilotExit()
        {
            transceiver.StopTalking();
            transceiver.Deactivate();
            SendEvents(onLeaveEventTargets, onLeaveEventNames);
        }
    }

#if !COMPILER_UDONSHARP && UNITY_EDITOR
    [CustomEditor(typeof(SFRelay))]
    public class SFRelayEditor : Editor
    {
        private static void UdonSharpEventListenersField(SerializedProperty eventListeners, SerializedProperty eventNames)
        {
            eventNames.arraySize = eventListeners.arraySize;

            EditorGUILayout.LabelField(eventListeners.displayName);

            for (int i = 0; i < eventListeners.arraySize; i++)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    var eventListenerProperty = eventListeners.GetArrayElementAtIndex(i);
                    var eventNameProperty = eventNames.GetArrayElementAtIndex(i);

                    EditorGUILayout.PropertyField(eventListenerProperty, new GUIContent());

                    var target = eventListenerProperty.objectReferenceValue as UdonSharpBehaviour;
                    var events = (target == null
                        ? Enumerable.Empty<string>()
                        : target.GetType().GetMethods(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public).Select(f => f.Name)
                    ).ToArray();

                    var index = events.Select((e, j) => (e, j)).Where(t => t.e == eventNameProperty.stringValue).Select(t => t.j).FirstOrDefault();
                    index = EditorGUILayout.Popup(index, events);
                    eventNameProperty.stringValue = events.Skip(index).FirstOrDefault();

                    if (GUILayout.Button("-", EditorStyles.miniButton, GUILayout.ExpandWidth(false)))
                    {
                        eventListeners.DeleteArrayElementAtIndex(i);
                        eventNames.DeleteArrayElementAtIndex(i);
                    }
                }
            }

            if (GUILayout.Button("Add Element"))
            {
                eventListeners.arraySize++;
            }
        }

        public override void OnInspectorGUI()
        {
            if (UdonSharpGUI.DrawDefaultUdonSharpBehaviourHeader(target)) return;

            serializedObject.Update();

            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(SFRelay.transceiver)));
            UdonSharpEventListenersField(serializedObject.FindProperty(nameof(SFRelay.onEnterEventTargets)), serializedObject.FindProperty(nameof(SFRelay.onEnterEventNames)));
            UdonSharpEventListenersField(serializedObject.FindProperty(nameof(SFRelay.onLeaveEventTargets)), serializedObject.FindProperty(nameof(SFRelay.onLeaveEventNames)));

            serializedObject.ApplyModifiedProperties();
        }
    }
#endif
}
