using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace UdonRadioCommunication
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync), RequireComponent(typeof(SphereCollider))]
    public class TouchSwitch : UdonSharpBehaviour
    {
        public UdonSharpBehaviour eventTarget;
        public string eventName;
        public bool ownerOnly = false;

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
        }

        public override void OnPlayerJoined(VRCPlayerApi player)
        {
            if (player.isLocal) inVR = Networking.LocalPlayer.IsUserInVR();
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
            if (Networking.LocalPlayer.IsUserInVR())
            {
                var radius = sphereCollider.radius;
                var center = sphereCollider.center;

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
    }
}
