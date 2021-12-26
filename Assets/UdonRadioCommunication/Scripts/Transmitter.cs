
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace UdonRadioCommunication
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class Transmitter : UdonSharpBehaviour
    {
        [UdonSynced] public float frequency = 1.0f;
        public float deactivateDelay = 1.0f;
        public float minDistance = 5.0f;
        public GameObject indicator;
        public bool indicatorAsLocal = false;

        private float lastActivatedTime;
        [UdonSynced][FieldChangeCallback(nameof(Active))] private bool _active;
        public bool Active
        {
            get => _active;
            set {
                if (value) lastActivatedTime = Time.time;
                _active = value;
                if (indicator != null) indicator.SetActive((!indicatorAsLocal || Networking.IsOwner(gameObject)) && value);
            }
        }

        private void Start()
        {
            Active = false;
        }

        public void _TakeOwnership()
        {
            if (Networking.IsOwner(gameObject)) return;
            Networking.SetOwner(Networking.LocalPlayer, gameObject);
        }

        public void _SetActive(bool value)
        {
            if (value) _Activate();
            else _Deactivate();
        }
        public void _Activate()
        {
            _TakeOwnership();
            Active = true;
            RequestSerialization();
        }
        public void _Deactivate()
        {
            _TakeOwnership();
            SendCustomEventDelayedSeconds(nameof(_DelayedDeactivate), deactivateDelay);
        }
        public void _DelayedDeactivate()
        {
            if (Time.time - lastActivatedTime < deactivateDelay) return;
            Active = false;
            RequestSerialization();
        }
        public void _ToggleActive() => _SetActive(!Active);

        public void _SetFrequency(float f)
        {
            _TakeOwnership();
            frequency = f;
            RequestSerialization();
        }

        public float _GetFrequency() => frequency;
    }
}
