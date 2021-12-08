using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

#if !COMPILER_UDONSHARP && UNITY_EDITOR
using UnityEditor;
using UdonSharpEditor;
using System.Reflection;
using System.Linq;
#endif

namespace UdonRadioCommunication
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    [RequireComponent(typeof(SphereCollider))]
    public class TouchSwitch : UdonSharpBehaviour
    {
        public UdonSharpBehaviour eventTarget;
        public string eventName;
        public bool ownerOnly = false;
        public bool disableInteractInVR = true;

        [Header("Knob")]
        public bool knobMode = false;
        public string onKnobRight, onKnobLeft;
        public Vector3 knobAxis = Vector3.forward;
        public Vector3 knobUp = Vector3.up;
        public float knobStep = 10.0f;

        [Header("Desktop Key")]
        public bool enableDesktopKey;
        public KeyCode desktopKey;

        [Header("Sounds (Optional)")]
        public AudioSource audioSource;
        public AudioClip switchSound;

        [Header("Haptics")]
        public bool enableHaptics = true;
        [Range(0, 1)] public float hapticDuration = 0.2f;
        [Range(0, 1)] public float hapticAmplitude = 0.5f;
        [Range(0, 1)] public float hapticFrequency = 0.1f;

        [HideInInspector] public bool inVR;

        private bool prevTouch;
        private Vector3 lastSwitchPosition;
        private SphereCollider sphereCollider;
        private Quaternion inverseHandRotaion;

        private void Start()
        {
            sphereCollider = GetComponent<SphereCollider>();
            SendCustomEventDelayedFrames(nameof(_PostStart), 1);
        }

        public void _PostStart()
        {
            if (disableInteractInVR && Networking.LocalPlayer.IsUserInVR()) DisableInteractive = true;
        }

        private bool DetectTouch(VRC_Pickup.PickupHand hand, Vector3 switchPosition, float radius, Vector3 offset)
        {
            var isLeft = hand == VRC_Pickup.PickupHand.Left;

            var localPlayer = Networking.LocalPlayer;
            var distalPosition = localPlayer.GetBonePosition(isLeft ? HumanBodyBones.LeftIndexDistal : HumanBodyBones.RightIndexDistal);
            var intermediatePosition = localPlayer.GetBonePosition(isLeft ? HumanBodyBones.LeftIndexIntermediate : HumanBodyBones.RightIndexIntermediate);

            var tipPosition = distalPosition + distalPosition - intermediatePosition;

            return (switchPosition + offset - tipPosition).sqrMagnitude < Mathf.Pow(radius, 2);
        }

        private void PlayHaptic(VRC_Pickup.PickupHand hand)
        {
            Networking.LocalPlayer.PlayHapticEventInHand(hand, hapticDuration, hapticAmplitude, hapticFrequency);
        }

        private void PlaySound()
        {
            if (audioSource != null && switchSound != null) audioSource.PlayOneShot(switchSound);
        }

        private void OnTouchStart(VRC_Pickup.PickupHand hand)
        {
            if ((hand == VRC_Pickup.PickupHand.None || !knobMode) && eventTarget != null && (!ownerOnly || Networking.IsOwner(eventTarget.gameObject)))
            {
                PlaySound();
                eventTarget.SendCustomEvent(eventName);
            }
            if (enableHaptics)  PlayHaptic(hand);

            if (hand != VRC_Pickup.PickupHand.None)
            {
                inverseHandRotaion = Quaternion.Inverse(Networking.LocalPlayer.GetTrackingData(hand == VRC_Pickup.PickupHand.Left ? VRCPlayerApi.TrackingDataType.LeftHand : VRCPlayerApi.TrackingDataType.RightHand).rotation);
            }
        }

        private void OnTouchEnd()
        {
        }

        public override void PostLateUpdate()
        {
            var localPlayer = Networking.LocalPlayer;
            if (localPlayer == null) return;

            if (localPlayer.IsUserInVR())
            {
                var radius = sphereCollider.radius * transform.lossyScale.x;
                var center = transform.rotation * Vector3.Scale(sphereCollider.center, transform.lossyScale);

                lastSwitchPosition = transform.position;

                var touchRight = DetectTouch(VRC_Pickup.PickupHand.Right, lastSwitchPosition, radius, center);
                var touch = touchRight || DetectTouch(VRC_Pickup.PickupHand.Left, lastSwitchPosition, radius, center);
                var hand = touchRight ? VRC_Pickup.PickupHand.Right : VRC_Pickup.PickupHand.Left;

                if (touch && !prevTouch) OnTouchStart(hand);
                else if (!touch && prevTouch) OnTouchEnd();

                if (touch && knobMode)
                {
                    var handRotation = Networking.LocalPlayer.GetTrackingData(touchRight ? VRCPlayerApi.TrackingDataType.RightHand : VRCPlayerApi.TrackingDataType.LeftHand).rotation;
                    var worldUp = transform.TransformDirection(knobUp);
                    var angle = Vector3.SignedAngle(worldUp, handRotation * inverseHandRotaion * worldUp, transform.TransformDirection(knobAxis));
                    if (Mathf.Abs(angle) >= knobStep)
                    {
                        PlayHaptic(hand);
                        PlaySound();
                        eventTarget.SendCustomEvent(angle > 0 ? onKnobRight : onKnobLeft);
                        inverseHandRotaion = Quaternion.Inverse(handRotation);
                    }
                }

                prevTouch = touch;

                lastSwitchPosition = transform.position;
            }

            if (enableDesktopKey)
            {
                if (Input.GetKeyDown(desktopKey)) OnTouchStart(VRC_Pickup.PickupHand.None);
                else if (Input.GetKeyUp(desktopKey)) OnTouchEnd();
            }
        }

        public override void Interact()
        {
            OnTouchStart(VRC_Pickup.PickupHand.None);
            OnTouchEnd();
        }

#if !COMPILER_UDONSHARP && UNITY_EDITOR
        private void Reset()
        {
            GetComponent<SphereCollider>().isTrigger = true;
        }
#endif
    }

#if !COMPILER_UDONSHARP && UNITY_EDITOR
    [CustomEditor(typeof(TouchSwitch))]
    public class TouchSwitchEditor : Editor
    {
        private static string UdonPublicEventField(string label, UdonSharpBehaviour udon, string value)
        {
            if (udon == null) return EditorGUILayout.TextField(label, value);

            var events = udon.GetType().GetMethods(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public).Select(m => m.Name).ToList();
            if (events.Count == 0) return EditorGUILayout.TextField(label, value);

            var index = Mathf.Max(events.FindIndex(e => e == value), 0);
            index = EditorGUILayout.Popup(label, index, events.ToArray());

            return events.Skip(index).FirstOrDefault();
        }

        public override void OnInspectorGUI()
        {
            if (UdonSharpGUI.DrawDefaultUdonSharpBehaviourHeader(target)) return;

            serializedObject.Update();

            var property = serializedObject.GetIterator();
            property.NextVisible(true);

            while (property.NextVisible(false))
            {
                switch (property.name)
                {
                    case nameof(TouchSwitch.eventName):
                    case nameof(TouchSwitch.onKnobRight):
                    case nameof(TouchSwitch.onKnobLeft):
                        property.stringValue = UdonPublicEventField(property.displayName, serializedObject.FindProperty(nameof(TouchSwitch.eventTarget)).objectReferenceValue as UdonSharpBehaviour, property.stringValue);
                        break;
                    default:
                        EditorGUILayout.PropertyField(property);
                        break;
                }
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
#endif
}
