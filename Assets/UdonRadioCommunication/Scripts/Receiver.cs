
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace UdonRadioCommunication
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class Receiver : UdonSharpBehaviour
    {
        public bool active;
        public float frequency = 1.0f;
        public bool limitRange = true;
        public float maxRange = 5.0f;
        public bool sync = true;

        [HideInInspector][UdonSynced] private bool syncActive;
        [HideInInspector][UdonSynced] private float syncFrequency;

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
            }
        }

        public void _TakeOwnership()
        {
            if (!sync || Networking.IsOwner(gameObject)) return;
            Networking.SetOwner(Networking.LocalPlayer, gameObject);
        }

        public void _SetActive(bool value)
        {
            _TakeOwnership();
            active = value;
            if (sync) RequestSerialization();
        }
        public bool _IsActive() => active;
        public void _Activate() => _SetActive(true);

        public void _Deactivate() => _SetActive(false);

        public void _SetFrequency(float f)
        {
            _TakeOwnership();
            frequency = f;
            if (sync) RequestSerialization();
        }
    }
}
