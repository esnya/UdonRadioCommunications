using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common.Interfaces;

#if !COMPILER_UDONSHARP && UNITY_EDITOR
using UnityEditor;
using UdonSharpEditor;
using System.Reflection;
using System.Linq;
#endif

namespace UdonRadioCommunication
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class TouchSwitch : UdonSharpBehaviour
    {
        public float radius = 0.0075f;
        public UdonSharpBehaviour eventTarget;
        public string eventName;
        public bool ownerOnly = false;
        public bool sentToOwner = false;
        public bool disableInteractInVR = true;
        public float throttlingDelay = 0.2f;

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
        private Quaternion inverseHandRotaion;

        private void Start()
        {
            SendCustomEventDelayedFrames(nameof(_PostStart), 1);
        }

        public void _PostStart()
        {
            if (disableInteractInVR && Networking.LocalPlayer.IsUserInVR()) DisableInteractive = true;
        }

        private bool DetectTouch(VRC_Pickup.PickupHand hand, Vector3 switchPosition, float radius)
        {
            var isLeft = hand == VRC_Pickup.PickupHand.Left;

            var localPlayer = Networking.LocalPlayer;
            var distalPosition = localPlayer.GetBonePosition(isLeft ? HumanBodyBones.LeftIndexDistal : HumanBodyBones.RightIndexDistal);
            var intermediatePosition = localPlayer.GetBonePosition(isLeft ? HumanBodyBones.LeftIndexIntermediate : HumanBodyBones.RightIndexIntermediate);

            var tipPosition = distalPosition + distalPosition - intermediatePosition;

            return (switchPosition - tipPosition).sqrMagnitude < Mathf.Pow(radius, 2);
        }

        private void PlayHaptic(VRC_Pickup.PickupHand hand)
        {
            Networking.LocalPlayer.PlayHapticEventInHand(hand, hapticDuration, hapticAmplitude, hapticFrequency);
        }

        private void PlaySound()
        {
            if (audioSource != null && switchSound != null) audioSource.PlayOneShot(switchSound);
        }

        private float lastTouchStartTime;
        private void OnTouchStart(VRC_Pickup.PickupHand hand)
        {
            var time = Time.time;
            if (time - lastTouchStartTime < throttlingDelay) return;

            lastTouchStartTime = time;

            if ((hand == VRC_Pickup.PickupHand.None || !knobMode) && eventTarget != null && (!ownerOnly || Networking.IsOwner(eventTarget.gameObject)))
            {
                PlaySound();
                SendCustomEventToTarget(eventName);
            }
            if (enableHaptics) PlayHaptic(hand);

            if (hand != VRC_Pickup.PickupHand.None)
            {
                inverseHandRotaion = Quaternion.Inverse(Networking.LocalPlayer.GetTrackingData(hand == VRC_Pickup.PickupHand.Left ? VRCPlayerApi.TrackingDataType.LeftHand : VRCPlayerApi.TrackingDataType.RightHand).rotation);
            }
        }

        private void OnTouchEnd()
        {
        }

        private float lastKnobStepTime;
        public override void PostLateUpdate()
        {
            var localPlayer = Networking.LocalPlayer;
            if (localPlayer == null) return;

            if (localPlayer.IsUserInVR())
            {
                var time = Time.time;
                lastSwitchPosition = transform.position;

                var touchRight = DetectTouch(VRC_Pickup.PickupHand.Right, lastSwitchPosition, radius);
                var touch = touchRight || DetectTouch(VRC_Pickup.PickupHand.Left, lastSwitchPosition, radius);
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
                        if (time - lastKnobStepTime > throttlingDelay)
                        {
                            lastKnobStepTime = time;

                            PlayHaptic(hand);
                            PlaySound();
                            SendCustomEventToTarget(angle > 0 ? onKnobRight : onKnobLeft);
                            inverseHandRotaion = Quaternion.Inverse(handRotation);
                        }
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

        private void SendCustomEventToTarget(string eventName)
        {
            if (sentToOwner) eventTarget.SendCustomNetworkEvent(NetworkEventTarget.Owner, eventName);
            else eventTarget.SendCustomEvent(eventName);
        }

#if !COMPILER_UDONSHARP && UNITY_EDITOR
        private void OnDrawGizmos()
        {
            this.UpdateProxy();
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, radius);
        }
#endif
    }

#if !COMPILER_UDONSHARP && UNITY_EDITOR
    [CustomEditor(typeof(TouchSwitch))]
    public class TouchSwitchEditor : Editor
    {
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
                        URCUtility.UdonPublicEventField(serializedObject.FindProperty(nameof(TouchSwitch.eventTarget)), property);
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
