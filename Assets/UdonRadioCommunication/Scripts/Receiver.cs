
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace UdonRadioCommunication
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class Receiver : UdonSharpBehaviour
    {
        [HideInInspector] public readonly string UdonTypeID = "UdonRadioCommunication.Receiver";
        [UdonSynced] private bool active;
        [UdonSynced] private float frequency = 122.6f;
        public bool limitRange = true;
        public float maxRange = 5.0f;

        public void Activate()
        {
            Networking.SetOwner(Networking.LocalPlayer, gameObject);
            active = true;
            RequestSerialization();
        }
        public bool IsActive() => active;

        public void Deactivate()
        {
            if (active) Networking.SetOwner(Networking.LocalPlayer, gameObject);
            active = false;
            RequestSerialization();
        }

        public void SetFrequency(float f)
        {
            Networking.SetOwner(Networking.LocalPlayer, gameObject);
            frequency = f;
            RequestSerialization();
        }

        public float GetFrequency() => frequency;
    }
}
