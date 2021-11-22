
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
        public float minDistance = 5.0f;
        public GameObject indicator;

        private void Start() => UpdateIndicator();
        public override void OnDeserialization() => UpdateIndicator();

        private void UpdateIndicator()
        {
            if (indicator != null) indicator.SetActive(active);
        }

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
            _TakeOwnership();
            active = true;
            UpdateIndicator();
            RequestSerialization();
        }
        public void _Deactivate()
        {
            SendCustomEventDelayedSeconds(nameof(_DelayedDeactivate), deactivateDelay);
            if (indicator != null) indicator.SetActive(false);
        }
        public void _DelayedDeactivate()
        {
            _TakeOwnership();
            active = false;
            UpdateIndicator();
            RequestSerialization();
        }
        public void _ToggleActive() => _SetActive(!active);

        public void _SetFrequency(float f)
        {
            _TakeOwnership();
            frequency = f;
            RequestSerialization();
        }

        public float _GetFrequency() => frequency;
    }
}
