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
        private const int MODE_KEY_DOWN = 0;
        private const int MODE_KEY_UP = 1;
        private const int MODE_KEY_HOLD = 2;

        public int[] keyCodes = { };
        public int[] modes = { };
        public UdonSharpBehaviour[] eventTargets = { };
        public string[] onKeyDownEvents = { };
        public float holdTime = 1.0f;
        public int eventsPerSeconds = 4;

        public AudioSource audioSource;

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

                var mode = modes[i];
                switch (mode)
                {
                    case MODE_KEY_DOWN:
                        if (Input.GetKeyDown(keyCode)) Trigger(eventTarget, onKeyDownEvent);
                        break;
                    case MODE_KEY_UP:
                        if (Input.GetKeyUp(keyCode)) Trigger(eventTarget, onKeyDownEvent);
                        break;
                    case MODE_KEY_HOLD:
                        if (Input.GetKeyDown(keyCode)) keyDownTimes[i] = frameCount;
                        else if (keyDownTime >= holdFrames && keyDownTime % holdEventInterval == 0 && Input.GetKey(keyCode)) Trigger(eventTarget, onKeyDownEvent);
                        break;
                }
            }
        }

        private void Trigger(UdonSharpBehaviour target, string eventName)
        {
            if (!target) return;
            target.SendCustomEvent(eventName);
            PlaySound();
        }

        private void PlaySound()
        {
            if (audioSource && audioSource.clip)
            {
                var obj = VRCInstantiate(audioSource.gameObject);
                obj.transform.SetParent(transform, false);
                var spawnedAudioSource = obj.GetComponent<AudioSource>();
                spawnedAudioSource.Play();
                Destroy(obj, spawnedAudioSource.clip.length + 1.0f);
            }
        }

    }

#if !COMPILER_UDONSHARP && UNITY_EDITOR
    [CustomEditor(typeof(KeyboardInput))]
    class KeyboardInputEditor : Editor
    {
        private readonly string[] Modes = {
            "KeyDown",
            "KeyUp",
            "KeyHold",
        };

        public override void OnInspectorGUI()
        {
            if (UdonSharpGUI.DrawDefaultUdonSharpBehaviourHeader(target)) return;

            serializedObject.Update();
            var keyCodes = serializedObject.FindProperty(nameof(KeyboardInput.keyCodes));
            var modes = serializedObject.FindProperty(nameof(KeyboardInput.modes));
            var eventTargets = serializedObject.FindProperty(nameof(KeyboardInput.eventTargets));
            var onKeyDownEvents = serializedObject.FindProperty(nameof(KeyboardInput.onKeyDownEvents));

            modes.arraySize = keyCodes.arraySize;
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
                        var mode = modes.GetArrayElementAtIndex(i);

                        keyCode.intValue = (int)(KeyCode)EditorGUILayout.EnumPopup((Enum)Enum.ToObject(typeof(KeyCode), keyCode.intValue));
                        mode.intValue = EditorGUILayout.Popup(mode.intValue, Modes);
                        EditorGUILayout.PropertyField(eventTargets.GetArrayElementAtIndex(i), GUIContent.none);
                        URCUtility.UdonPublicEventField(eventTargets.GetArrayElementAtIndex(i), onKeyDownEvents.GetArrayElementAtIndex(i), GUIContent.none);

                        if (GUILayout.Button("-", EditorStyles.miniButton, GUILayout.ExpandWidth(false)))
                        {
                            keyCodes.DeleteArrayElementAtIndex(i);
                            modes.DeleteArrayElementAtIndex(i);
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

            serializedObject.ApplyModifiedProperties();
        }
    }
#endif
}
