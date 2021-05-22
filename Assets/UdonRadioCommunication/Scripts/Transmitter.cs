
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon.Common.Interfaces;

namespace UdonRadioCommunication
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class Transmitter : UdonSharpBehaviour
    {
        [UdonSynced] private bool active;
        [UdonSynced] private float frequency = 122.6f;

        public void Activate()
        {
            Networking.SetOwner(Networking.LocalPlayer, gameObject);
            active = true;
            RequestSerialization();
        }

        public void Deactivate()
        {
            if (active) Networking.SetOwner(Networking.LocalPlayer, gameObject);
            active = false;
            RequestSerialization();
        }

        public bool IsActive() => active;

        public void SetFerquency(float f)
        {
            Networking.SetOwner(Networking.LocalPlayer, gameObject);
            frequency = f;
            RequestSerialization();
        }

        public float GetFrequency() => frequency;
    }
}
