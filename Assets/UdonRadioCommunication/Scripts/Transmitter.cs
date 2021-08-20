
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace UdonRadioCommunication
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class Transmitter : UdonSharpBehaviour
    {
        [UdonSynced] public bool active;
        [UdonSynced] public float frequency = 1.0f;
        public float deactivateDelay = 3.0f;
        [Tooltip("Optional")] public GameObject ownerDetector;

        public void _TakeOwnership()
        {
            if (Networking.IsOwner(gameObject)) return;
            Networking.SetOwner(Networking.LocalPlayer, gameObject);
        }

        public bool _IsActive() => active;
        public void _SetActive(bool value)
        {
            _TakeOwnership();
            if (value) _Activate();
            else _Deactivate();
        }
        public void _Activate()
        {
            if (ownerDetector != null && !Networking.IsOwner(ownerDetector)) return;
            _TakeOwnership();
            active = true;
            RequestSerialization();
        }
        public void _Deactivate()
        {
            SendCustomEventDelayedSeconds(nameof(_DelayedDeactivate), deactivateDelay);
        }

        public void _DelayedDeactivate()
        {
            _TakeOwnership();
            active = false;
            RequestSerialization();
        }

        public void _SetFrequency(float f)
        {
            _TakeOwnership();
            frequency = f;
            RequestSerialization();
        }

        public float _GetFrequency() => frequency;
    }
}
