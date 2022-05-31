using System;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace URC
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class Receiver : UdonSharpBehaviour
    {
        public bool active;
        public bool limitRange = true;
        public float maxRange = 5.0f;
        public bool sync = true;
        public GameObject indicator;
        [NonSerialized] public float frequency;

        [NonSerialized] public UdonRadioCommunication urc;

        [UdonSynced][FieldChangeCallback(nameof(SyncedActive))] private bool _syncedActive;
        public bool SyncedActive {
            set {
                if (!sync) return;
                active = _syncedActive = value;
                UpdateIndicator();
            }
            get => _syncedActive;
        }
        [UdonSynced][FieldChangeCallback(nameof(SyncedFrequency))] private float _syncedFrequency;
        public float SyncedFrequency {
            set {
                if (!sync) return;
                frequency = _syncedFrequency = value;
            }
            get => _syncedFrequency;
        }

        private void Start() => UpdateIndicator();

        public void _Initialize(UdonRadioCommunication urc)
        {
            this.urc = urc;
            frequency = urc.defaultFrequency;
        }

        public override void OnPreSerialization()
        {
            _syncedActive = active;
            _syncedFrequency = frequency;
        }

        private void UpdateIndicator()
        {
            if (indicator != null) indicator.SetActive(active);
        }

        public void _TakeOwnership()
        {
            if (!sync || Networking.IsOwner(gameObject)) return;
            Networking.SetOwner(Networking.LocalPlayer, gameObject);
        }

        public void _SetActive(bool value)
        {
            if (sync) _TakeOwnership();
            active = value;
            UpdateIndicator();
            if (sync) RequestSerialization();
        }
        public bool _IsActive() => active;
        public void _Activate() => _SetActive(true);
        public void _Deactivate() => _SetActive(false);
        public void _ToggleActive() => _SetActive(!active);

        public void _SetFrequency(float f)
        {
            if (sync) _TakeOwnership();
            frequency = f;
            if (sync) RequestSerialization();
        }
    }
}
