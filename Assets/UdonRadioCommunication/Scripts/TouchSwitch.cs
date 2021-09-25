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

#if UNITY_EDITOR
        [Header("Debug")]
        public bool debugIsPressed;
#endif
        [HideInInspector] public bool inVR;

        private bool prevIsPressed;
        private Vector3 lastSwitchPosition;
        private SphereCollider sphereCollider;

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
#if UNITY_EDITOR
            if (debugIsPressed) return true;
#endif
            var isLeft = hand == VRC_Pickup.PickupHand.Left;

            var localPlayer = Networking.LocalPlayer;
            var distalPosition = localPlayer.GetBonePosition(isLeft ? HumanBodyBones.LeftIndexDistal : HumanBodyBones.RightIndexDistal);
            var intermediatePosition = localPlayer.GetBonePosition(isLeft ? HumanBodyBones.LeftIndexIntermediate : HumanBodyBones.RightIndexIntermediate);

            var tipPosition = distalPosition + distalPosition - intermediatePosition;

            return (switchPosition + offset - tipPosition).sqrMagnitude < Mathf.Pow(radius, 2);
        }

        private void OnTouchStart(VRC_Pickup.PickupHand hand)
        {
            if (eventTarget != null && (!ownerOnly || Networking.IsOwner(eventTarget.gameObject))) eventTarget.SendCustomEvent(eventName);
            if (enableHaptics && inVR) Networking.LocalPlayer.PlayHapticEventInHand(hand, hapticDuration, hapticAmplitude, hapticFrequency);
            if (audioSource != null && switchSound != null) audioSource.PlayOneShot(switchSound);
        }

        private void OnTouchEnd()
        {
        }

        public override void PostLateUpdate()
        {
#if UNITY_EDITOR
            if (debugIsPressed) inVR = true;
#endif
            if (inVR)
            {
                var radius = sphereCollider.radius * transform.lossyScale.x;
                var center = Vector3.Scale(sphereCollider.center, transform.lossyScale);

                lastSwitchPosition = transform.position;

                var touchRight = DetectTouch(VRC_Pickup.PickupHand.Right, lastSwitchPosition, radius, center);
                var touch = touchRight || DetectTouch(VRC_Pickup.PickupHand.Left, lastSwitchPosition, radius, center);

                if (touch && !prevIsPressed) OnTouchStart(touchRight ? VRC_Pickup.PickupHand.Right : VRC_Pickup.PickupHand.Left);
                else if (!touch && prevIsPressed) OnTouchEnd();

                prevIsPressed = touch;

                lastSwitchPosition = transform.position;
            }
            else if (enableDesktopKey)
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
