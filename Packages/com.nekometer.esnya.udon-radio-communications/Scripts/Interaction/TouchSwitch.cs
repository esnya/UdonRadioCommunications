using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon.Common.Interfaces;

#if !COMPILER_UDONSHARP && UNITY_EDITOR
using UnityEditor;
using UdonSharpEditor;
using System.Reflection;
using System.Linq;
#endif

namespace URC
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class TouchSwitch : UdonSharpBehaviour
    {
        [Header("Dimensions")]
        public float radius = 0.0075f;
        public float thickness = 1.0f;
        public float maxPlayerDistance = 5.0f;
        public int awakeCheckInterval = 5;

        [Header("Networked")]
        public bool ownerOnly = false;
        [Tooltip("Deprecated: Use networked")] public bool sentToOwner = false;
        public bool networked = false;
        public NetworkEventTarget networkEventTarget;

        [Header("Touch")]
        public UdonSharpBehaviour eventTarget;
        public string eventName;

        public bool disableInteractInVR = true;
        public float throttlingDelay = 0.5f;
        public bool fingerMode = true;
        public bool grip;
        public string onTouchStart, onTouchEnd;

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

        [Header("Wheel")]
        public bool wheelMode = false;
        public string onWheelRight, onWheelLeft;
        public float wheelStep = 1.0f;

        [Header("Desktop Key")]
        [Tooltip("Deprecated: Use KeyboardInput")] public bool enableDesktopKey;
        [Tooltip("Deprecated: Use KeyboardInput")] public KeyCode desktopKey;

        [Header("Sounds (Optional)")]
        public AudioSource audioSource;

        [Header("Haptics")]
        public bool enableHaptics = true;
        [Range(0, 1)] public float hapticDuration = 0.2f;
        [Range(0, 1)] public float hapticAmplitude = 0.5f;
        [Range(0, 1)] public float hapticFrequency = 0.1f;

        [Header("Debug")]
        public Transform debugTouhSource;

        private float lastKnobStepTime;
        private Quaternion inverseHandRotation;
        private float lastTouchStartTime;
        private Vector3 touchStartPosition;
        private bool _leftTouchState, _rightTouchState;
        private float wheelAngle;

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

            SendCustomEventDelayedSeconds(nameof(_AwakeCheck), UnityEngine.Random.Range(0, awakeCheckInterval));
        }

        public void _PostStart()
        {
            if (disableInteractInVR && Networking.LocalPlayer.IsUserInVR()) DisableInteractive = true;
        }

        public void _AwakeCheck()
        {
            var inRange = Vector3.Distance(transform.position, Networking.LocalPlayer.GetPosition()) <= maxPlayerDistance;
            if (enabled != inRange) enabled = inRange;

            SendCustomEventDelayedSeconds(nameof(_AwakeCheck), awakeCheckInterval);
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
            if (grip && Input.GetAxisRaw(isLeft ? "Oculus_CrossPlatform_PrimaryHandTrigger" : "Oculus_CrossPlatform_SecondaryHandTrigger") < 0.75f) return false;
            var touchPosition = GetTouchPosition(isLeft);
            var relative = switchPosition - touchPosition;
            return relative.sqrMagnitude < Mathf.Pow(radius, 2) && Mathf.Abs(Vector3.Dot(relative, transform.TransformDirection(Vector3.forward))) <= thickness * 0.5f;
        }

        private void PlayHaptic(bool isLeft, float strength)
        {
            Networking.LocalPlayer.PlayHapticEventInHand(isLeft ? VRC_Pickup.PickupHand.Left : VRC_Pickup.PickupHand.Right, hapticDuration * strength, hapticAmplitude * strength, hapticFrequency);
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
            }
            else if (wheelMode)
            {
                wheelAngle = Vector3.SignedAngle(Vector3.up, Vector3.ProjectOnPlane(touchStartPosition, Vector3.forward), Vector3.forward);
            }
            else
            {
                SendCustomEventToTarget(eventName, isLeft);
                SendCustomEventToTarget(onTouchStart, isLeft);
            }

            if (enableHaptics && (knobMode || directionalMode)) PlayHaptic(isLeft, 0.5f);
        }

        private void OnTouchMove(bool isLeft)
        {
            if (knobMode && GetTouchState(isLeft)) ProcessKnob(isLeft);
            if (wheelMode && GetTouchState(isLeft)) ProcessWheel(isLeft);
        }

        private void OnTouchEnd(bool isLeft)
        {
            SendCustomEventToTarget(onTouchEnd, isLeft);

            if (directionalMode)
            {
                var touchPosition = transform.InverseTransformPoint(GetTouchPosition(isLeft));
                var touchDirection = touchPosition.normalized;
                var localDirection = Vector3.ProjectOnPlane(touchDirection, Vector3.forward).normalized;

                if (Vector3.Dot(localDirection, Vector3.up) > directionalThreshold) SendCustomEventToTarget(onUp, isLeft);
                if (Vector3.Dot(localDirection, -Vector3.up) > directionalThreshold) SendCustomEventToTarget(onDown, isLeft);
                if (Vector3.Dot(localDirection, Vector3.right) > directionalThreshold) SendCustomEventToTarget(onRight, isLeft);
                if (Vector3.Dot(localDirection, -Vector3.right) > directionalThreshold) SendCustomEventToTarget(onLeft, isLeft);
            }
        }

        private void ProcessKnob(bool isLeft)
        {
            var time = Time.time;

            var handRotation = GetHandRotaton(isLeft);

            var worldUp = transform.TransformDirection(Vector3.up);
            var angle = Vector3.SignedAngle(worldUp, handRotation * inverseHandRotation * worldUp, transform.TransformDirection(Vector3.forward));

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

        private void ProcessWheel(bool isLeft)
        {
            var touchLocalPosition = transform.InverseTransformPoint(GetTouchPosition(isLeft));
            var nextWheelAngle = Vector3.SignedAngle(Vector3.up, Vector3.ProjectOnPlane(touchLocalPosition, Vector3.forward), Vector3.forward);
            var diffAngle = Mathf.DeltaAngle(wheelAngle, nextWheelAngle);
            var eventCount = Mathf.FloorToInt(Mathf.Abs(diffAngle) / wheelStep);
            if (eventCount > 0)
            {
                var eventName = diffAngle > 0 ? onWheelRight : onWheelLeft;
                for (var i = 0; i < Mathf.Abs(eventCount); i++)
                {
                    SendCustomEventToTarget(eventName, isLeft);
                }
            }
            wheelAngle += Mathf.Sign(diffAngle) * wheelStep * eventCount;
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

            if (enableHaptics) PlayHaptic(isLeft, 1.0f);
            PlaySound();

            if (networked) eventTarget.SendCustomNetworkEvent(networkEventTarget, eventName);
            else eventTarget.SendCustomEvent(eventName);
        }

#if !COMPILER_UDONSHARP && UNITY_EDITOR
        private void OnDrawGizmos()
        {
            try
            {
                this.UpdateProxy();
                Gizmos.color = Color.white;
                Gizmos.matrix = transform.localToWorldMatrix * Matrix4x4.Scale(new Vector3(1.0f, 1.0f, thickness));

                Gizmos.DrawWireSphere(Vector3.zero, radius);

                if (directionalMode)
                {
                    Gizmos.color = Color.red;
                    Vector3 right = Vector3.right;
                    Gizmos.DrawRay(Vector3.zero, right * radius);

                    Gizmos.color = Color.green;
                    Gizmos.DrawRay(Vector3.zero, Vector3.up * radius);
                }

                if (knobMode || wheelMode)
                {
                    Gizmos.color = Color.blue;
                    Gizmos.DrawRay(Vector3.zero, Vector3.forward * radius);
                }
            }
            finally
            {
                Gizmos.matrix = Matrix4x4.identity;
            }
        }

        private void OnSceneGUI()
        {
            if (!EditorApplication.isPlaying) return;

            Handles.BeginGUI();
            try
            {
                this.UpdateProxy();

                if (GUILayout.Button("Touch")) SendCustomEventToTarget(eventName, false);
                if (wheelMode && GUILayout.Button("Wheel Left")) SendCustomEventToTarget(onWheelLeft, false);
                if (wheelMode && GUILayout.Button("Wheel Right")) SendCustomEventToTarget(onWheelRight, false);
                if (knobMode && GUILayout.Button("Knob Left")) SendCustomEventToTarget(onKnobLeft, false);
                if (knobMode && GUILayout.Button("Knob Right")) SendCustomEventToTarget(onKnobRight, false);
            }
            finally
            {
                Handles.EndGUI();
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
            var wheelMode = serializedObject.FindProperty(nameof(TouchSwitch.wheelMode)).boolValue;
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
                    case nameof(TouchSwitch.onWheelLeft):
                    case nameof(TouchSwitch.onWheelRight):
                    case nameof(TouchSwitch.wheelStep):
                        if (!wheelMode) continue;
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
                    case nameof(TouchSwitch.onWheelLeft):
                    case nameof(TouchSwitch.onWheelRight):
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
