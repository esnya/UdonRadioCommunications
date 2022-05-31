#pragma warning disable IDE0051,IDE1006
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace UdonRadioCommunication
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class Receiver : UdonSharpBehaviour
    {
        public bool active;
        public float frequency = 118.0f;
        public bool limitRange = true;
        public float maxRange = 5.0f;
        public bool sync = true;
        public GameObject indicator;

        [System.NonSerialized] public UdonSharpBehaviour urc;

        [HideInInspector][UdonSynced] private bool syncActive;
        [HideInInspector][UdonSynced] private float syncFrequency;

        private void Start() => UpdateIndicator();

        public override void OnPreSerialization()
        {
            syncActive = active;
            syncFrequency = frequency;
        }

        public override void OnDeserialization()
        {
            if (sync) {
                active = syncActive;
                frequency = syncFrequency;
                UpdateIndicator();
            }
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
