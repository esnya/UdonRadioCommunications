
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

        public void TakeOwnership()
        {
            if (Networking.IsOwner(gameObject)) return;
            Networking.SetOwner(Networking.LocalPlayer, gameObject);
        }

        public bool IsActive() => active;
        public void SetActive(bool value)
        {
            if (value) Activate();
            else Deactivate();
        }
        public void Activate()
        {
            if (ownerDetector != null && !Networking.IsOwner(ownerDetector)) return;
            active = true;
            RequestSerialization();
        }
        public void Deactivate()
        {
            SendCustomEventDelayedSeconds(nameof(_DelayedDeactivate), deactivateDelay);
        }

        public void _DelayedDeactivate()
        {
            TakeOwnership();
            active = false;
            RequestSerialization();
        }

        public void SetFrequency(float f)
        {
            TakeOwnership();
            frequency = f;
            RequestSerialization();
        }

        public float GetFrequency() => frequency;
    }
}
