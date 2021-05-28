
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

        public void TakeOwnership()
        {
            if (!sync || Networking.IsOwner(gameObject)) return;
            Networking.SetOwner(Networking.LocalPlayer, gameObject);
        }

        public void SetActive(bool value)
        {
            TakeOwnership();
            active = value;
            if (sync) RequestSerialization();
        }
        public bool IsActive() => active;
        public void Activate() => SetActive(true);

        public void Deactivate() => SetActive(false);

        public void SetFrequency(float f)
        {
            TakeOwnership();
            frequency = f;
            if (sync) RequestSerialization();
        }
    }
}
