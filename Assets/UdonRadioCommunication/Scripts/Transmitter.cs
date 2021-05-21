
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon.Common.Interfaces;

namespace UdonRadioCommunication
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class Transmitter : UdonSharpBehaviour
    {
        [UdonSynced] public bool active;
        [UdonSynced] public float frequency = 122.6f;

        public void Activate()
        {
            Networking.SetOwner(Networking.LocalPlayer, gameObject);
            active = true;
            RequestSerialization();
        }

        public void Deactivate()
        {
            active = false;
            RequestSerialization();
        }
    }
}
