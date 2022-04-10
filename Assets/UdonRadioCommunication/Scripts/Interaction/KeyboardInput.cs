using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using System;

#if !COMPILER_UDONSHARP && UNITY_EDITOR
using UnityEditor;
using UdonSharpEditor;
#endif

namespace UdonRadioCommunication
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class KeyboardInput : UdonSharpBehaviour
    {
        public int[] keyCodes = { };
        public bool[] holds = {};
        public UdonSharpBehaviour[] eventTargets = { };
        public string[] onKeyDownEvents = { };
        public float holdTime = 1.0f;
        public int eventsPerSeconds = 4;

        public AudioSource audioSource;
        public AudioClip audioClip;

        private int[] keyDownTimes;
        private void Start()
        {
            keyDownTimes = new int[keyCodes.Length];
        }

        private void Update()
        {
            var frameCount = Time.frameCount;
            var fixedUnscaledDeitaTime = Time.fixedUnscaledDeltaTime;
            var holdEventInterval = Mathf.FloorToInt(1.0f / eventsPerSeconds / fixedUnscaledDeitaTime);
            var holdFrames = Mathf.FloorToInt(holdTime / fixedUnscaledDeitaTime);
            for (int i = 0; i < keyCodes.Length; i++)
            {
                var eventTarget = eventTargets[i];
                if (!eventTarget) continue;

                var keyDownTime = frameCount - keyDownTimes[i];
                var keyCode = (KeyCode)keyCodes[i];
                var onKeyDownEvent = onKeyDownEvents[i];

                var hold = holds[i];
                if (Input.GetKeyDown(keyCode))
                {
                    if (!hold) Trigger(eventTarget, onKeyDownEvent);
                    keyDownTimes[i] = frameCount;
                }
                else if (hold && keyDownTime >= holdFrames && keyDownTime % holdEventInterval == 0 && Input.GetKey(keyCode))
                {
                    Trigger(eventTarget, onKeyDownEvent);
                }
            }
        }

        private void Trigger(UdonSharpBehaviour target, string eventName)
        {
            if (target) target.SendCustomEvent(eventName);
            if (audioSource && audioClip) audioSource.PlayOneShot(audioClip);
        }
    }

#if !COMPILER_UDONSHARP && UNITY_EDITOR
    [CustomEditor(typeof(KeyboardInput))]
    class KeyboardInputEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            if (UdonSharpGUI.DrawDefaultUdonSharpBehaviourHeader(target)) return;

            serializedObject.Update();
            var keyCodes = serializedObject.FindProperty(nameof(KeyboardInput.keyCodes));
            var holds = serializedObject.FindProperty(nameof(KeyboardInput.holds));
            var eventTargets = serializedObject.FindProperty(nameof(KeyboardInput.eventTargets));
            var onKeyDownEvents = serializedObject.FindProperty(nameof(KeyboardInput.onKeyDownEvents));

            holds.arraySize = keyCodes.arraySize;
            eventTargets.arraySize = keyCodes.arraySize;
            onKeyDownEvents.arraySize = keyCodes.arraySize;

            EditorGUILayout.PropertyField(keyCodes, new GUIContent("Key Mapping"), false);

            if (keyCodes.isExpanded)
            {
                for (var i = 0; i < keyCodes.arraySize; i++)
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        var keyCode = keyCodes.GetArrayElementAtIndex(i);
                        var hold = holds.GetArrayElementAtIndex(i);

                        keyCode.intValue = (int)(KeyCode)EditorGUILayout.EnumPopup((Enum)Enum.ToObject(typeof(KeyCode), keyCode.intValue));
                        hold.boolValue = EditorGUILayout.ToggleLeft("Hold", hold.boolValue, new [] { GUILayout.ExpandWidth(false), GUILayout.Width(100) });
                        EditorGUILayout.PropertyField(eventTargets.GetArrayElementAtIndex(i), GUIContent.none);
                        URCUtility.UdonPublicEventField(eventTargets.GetArrayElementAtIndex(i), onKeyDownEvents.GetArrayElementAtIndex(i), GUIContent.none);

                        if (GUILayout.Button("-", EditorStyles.miniButton, GUILayout.ExpandWidth(false)))
                        {
                            keyCodes.DeleteArrayElementAtIndex(i);
                            eventTargets.DeleteArrayElementAtIndex(i);
                            onKeyDownEvents.DeleteArrayElementAtIndex(i);
                        }
                    }
                }

                if (GUILayout.Button("Add Element"))
                {
                    keyCodes.arraySize++;
                    eventTargets.arraySize = keyCodes.arraySize;
                    onKeyDownEvents.arraySize = keyCodes.arraySize;
                }
            }

            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(KeyboardInput.holdTime)));
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(KeyboardInput.eventsPerSeconds)));
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(KeyboardInput.audioSource)));
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(KeyboardInput.audioClip)));

            serializedObject.ApplyModifiedProperties();
        }
    }
#endif
}
