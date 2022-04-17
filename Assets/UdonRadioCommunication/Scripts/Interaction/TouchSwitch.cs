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
        [Tooltip("Deprecated: Use networked")] public bool sentToOwner = false;
        public bool networked = false;
        public NetworkEventTarget networkEventTarget;

        public bool disableInteractInVR = true;
        public float throttlingDelay = 0.5f;
        public bool fingerMode = true;
        public string onTouchStart, onTouchEnd;

        public Vector3 localUp = Vector3.up;
        public Vector3 localRight = Vector3.right;

        [Header("Knob")]
        public bool knobMode = false;
        public string onKnobRight, onKnobLeft;
        public float knobStep = 10.0f;

        [Header("Directional")]
        public bool directionalMode = false;
        public float directionalThreshold = 0.8f;
        public string onUp;
        public string onDown;
        public string onLeft;
        public string onRight;

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

        [Header("Debug")]
        public Transform debugTouhSource;

        private float lastKnobStepTime;
        private Vector3 localForward;
        private Quaternion inverseHandRotation;
        private float lastTouchStartTime;
        private Vector3 touchStartPosition;
        private bool _leftTouchState, _rightTouchState;
        private void SetTouchState(bool isLeft, bool value)
        {
            var currentState = isLeft ? _leftTouchState : _rightTouchState;

            if (value != currentState)
            {
                if (value) OnTouchStart(isLeft);
                else OnTouchEnd(isLeft);
            }
            if (isLeft) _leftTouchState = value;
            else _rightTouchState = value;
        }
        private bool GetTouchState(bool isLeft)
        {
            return isLeft ? _leftTouchState : _rightTouchState;
        }

        private void Start()
        {
            if (sentToOwner && !networked)
            {
                networked = true;
                networkEventTarget = NetworkEventTarget.Owner;
            }

            localForward = Vector3.Cross(localRight, localUp).normalized;
            SendCustomEventDelayedFrames(nameof(_PostStart), 1);
        }

        public void _PostStart()
        {
            if (disableInteractInVR && Networking.LocalPlayer.IsUserInVR()) DisableInteractive = true;
        }

        private Vector3 GetTouchPosition(bool isLeft)
        {
            if (debugTouhSource) return debugTouhSource.position;

            var localPlayer = Networking.LocalPlayer;

            if (fingerMode)
            {
                var distalPosition = localPlayer.GetBonePosition(isLeft ? HumanBodyBones.LeftIndexDistal : HumanBodyBones.RightIndexDistal);
                var intermediatePosition = localPlayer.GetBonePosition(isLeft ? HumanBodyBones.LeftIndexIntermediate : HumanBodyBones.RightIndexIntermediate);

                var tipPosition = distalPosition + distalPosition - intermediatePosition;

                return tipPosition;
            }

            var handPosition = localPlayer.GetTrackingData(isLeft ? VRCPlayerApi.TrackingDataType.LeftHand : VRCPlayerApi.TrackingDataType.RightHand).position;
            return handPosition;
        }

        private bool DetectTouch(bool isLeft, Vector3 switchPosition, float radius)
        {
            var touchPosition = GetTouchPosition(isLeft);
            return (switchPosition - touchPosition).sqrMagnitude < Mathf.Pow(radius, 2);
        }

        private void PlayHaptic(bool isLeft)
        {
            Networking.LocalPlayer.PlayHapticEventInHand(isLeft ? VRC_Pickup.PickupHand.Left : VRC_Pickup.PickupHand.Right, hapticDuration, hapticAmplitude, hapticFrequency);
        }

        private void PlaySound()
        {
            if (audioSource != null && switchSound != null) audioSource.PlayOneShot(switchSound);
        }

        private Quaternion GetHandRotaton(bool isLeft)
        {
            return debugTouhSource ? debugTouhSource.rotation : Networking.LocalPlayer.GetTrackingData(isLeft ? VRCPlayerApi.TrackingDataType.LeftHand : VRCPlayerApi.TrackingDataType.RightHand).rotation;
        }

        private void OnTouchStart(bool isLeft)
        {
            var time = Time.time;
            if (time - lastTouchStartTime < throttlingDelay) return;

            lastTouchStartTime = time;

            touchStartPosition = transform.InverseTransformPoint(GetTouchPosition(isLeft));

            if (knobMode)
            {
                inverseHandRotation = Quaternion.Inverse(GetHandRotaton(isLeft));
                if (enableHaptics) PlayHaptic(isLeft);
            }
            else
            {
                SendCustomEventToTarget(eventName, isLeft);
                SendCustomEventToTarget(onTouchStart, isLeft);
            }
        }

        private void OnTouchMove(bool isLeft)
        {
            if (knobMode && GetTouchState(isLeft)) ProcessKnob(isLeft);
        }

        private void OnTouchEnd(bool isLeft)
        {
            SendCustomEventToTarget(onTouchEnd, isLeft);

            if (directionalMode)
            {
                var touchPosition = transform.InverseTransformPoint(GetTouchPosition(isLeft));
                var touchDirection = touchPosition.normalized;
                var localDirection = Vector3.ProjectOnPlane(touchDirection, localForward).normalized;

                if (Vector3.Dot(localDirection, localUp) > directionalThreshold) SendCustomEventToTarget(onUp, isLeft);
                if (Vector3.Dot(localDirection, -localUp) > directionalThreshold) SendCustomEventToTarget(onDown, isLeft);
                if (Vector3.Dot(localDirection, localRight) > directionalThreshold) SendCustomEventToTarget(onRight, isLeft);
                if (Vector3.Dot(localDirection, -localRight) > directionalThreshold) SendCustomEventToTarget(onLeft, isLeft);
            }
        }

        private void ProcessKnob(bool isLeft)
        {
            var time = Time.time;

            var handRotation = GetHandRotaton(isLeft);

            var worldUp = transform.TransformDirection(localUp);
            var angle = Vector3.SignedAngle(worldUp, handRotation * inverseHandRotation * worldUp, transform.TransformDirection(localForward));

            if (Mathf.Abs(angle) >= knobStep)
            {
                if (time - lastKnobStepTime > throttlingDelay)
                {
                    lastKnobStepTime = time;

                    SendCustomEventToTarget(angle > 0 ? onKnobRight : onKnobLeft, isLeft);

                    inverseHandRotation = Quaternion.Inverse(handRotation);
                }
            }
        }

        public override void PostLateUpdate()
        {
            var localPlayer = Networking.LocalPlayer;
            if (localPlayer == null) return;

            if (localPlayer.IsUserInVR() || debugTouhSource)
            {
                var switchPosition = transform.position;

                var touchRight = DetectTouch(false, switchPosition, radius);
                SetTouchState(false, touchRight);
                var touchLeft = !touchRight && DetectTouch(true, switchPosition, radius);
                SetTouchState(true, touchLeft);

                if (touchRight) OnTouchMove(false);
                if (touchLeft) OnTouchMove(true);
            }

            if (enableDesktopKey)
            {
                if (Input.GetKeyDown(desktopKey)) SendCustomEventToTarget(eventName, false);
            }
        }

        public override void Interact()
        {
            OnTouchStart(false);
            OnTouchEnd(false);
        }

        private void SendCustomEventToTarget(string eventName, bool isLeft)
        {
            if (eventTarget == null || string.IsNullOrEmpty(eventName) || ownerOnly && !Networking.IsOwner(eventTarget.gameObject)) return;

            if (enableHaptics) PlayHaptic(isLeft);
            PlaySound();

            if (networked) eventTarget.SendCustomNetworkEvent(networkEventTarget, eventName);
            else eventTarget.SendCustomEvent(eventName);
        }

#if !COMPILER_UDONSHARP && UNITY_EDITOR
        private void OnDrawGizmos()
        {
            this.UpdateProxy();
            Gizmos.color = Color.white;
            Gizmos.DrawWireSphere(transform.position, radius);

            if (directionalMode)
            {

                Gizmos.color = Color.red;
                Gizmos.DrawRay(transform.position, transform.TransformDirection(localRight) * 0.1f);

                Gizmos.color = Color.green;
                Gizmos.DrawRay(transform.position, transform.TransformDirection(localUp) * 0.1f);
            }

            if (knobMode)
            {
                Gizmos.color = Color.white;
                Gizmos.DrawRay(transform.position, -transform.TransformDirection(Vector3.Cross(localRight, localUp).normalized) * 0.1f);
            }
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

            var networked = serializedObject.FindProperty(nameof(TouchSwitch.networked)).boolValue;
            var knobMode = serializedObject.FindProperty(nameof(TouchSwitch.knobMode)).boolValue;
            var directionalMode = serializedObject.FindProperty(nameof(TouchSwitch.directionalMode)).boolValue;

            while (property.NextVisible(false))
            {
                switch (property.name)
                {
                    case nameof(TouchSwitch.networkEventTarget):
                        if (!networked) continue;
                        break;
                    case nameof(TouchSwitch.onKnobRight):
                    case nameof(TouchSwitch.onKnobLeft):
                    case nameof(TouchSwitch.knobStep):
                        if (!knobMode) continue;
                        break;
                    case nameof(TouchSwitch.directionalThreshold):
                    case nameof(TouchSwitch.onUp):
                    case nameof(TouchSwitch.onDown):
                    case nameof(TouchSwitch.onLeft):
                    case nameof(TouchSwitch.onRight):
                        if (!directionalMode) continue;
                        break;
                }

                switch (property.name)
                {
                    case nameof(TouchSwitch.eventName):
                    case nameof(TouchSwitch.onKnobRight):
                    case nameof(TouchSwitch.onKnobLeft):
                    case nameof(TouchSwitch.onUp):
                    case nameof(TouchSwitch.onDown):
                    case nameof(TouchSwitch.onLeft):
                    case nameof(TouchSwitch.onRight):
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
