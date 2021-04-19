
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon.Common.Interfaces;

namespace UdonRadioCommunication
{
    public class Transmitter : UdonSharpBehaviour
    {
        [HideInInspector] public readonly string UdonTypeID = "UdonRadioCommunication.Transmitter";
        [HideInInspector] public bool active;
        [UdonSynced] public float frequency = 122.6f;
        private bool activating = false;

        public override void OnPreSerialization()
        {
            if (activating)
            {
                activating = false;
                SendCustomNetworkEvent(NetworkEventTarget.All, nameof(Activated));
            }
        }

        public void Activate()
        {
            if (Networking.IsOwner(gameObject)) SendCustomNetworkEvent(NetworkEventTarget.All, nameof(Activated));
            else
            {
                activating = true;
                Networking.SetOwner(Networking.LocalPlayer, gameObject);
            }
        }

        public void Deactivate()
        {
            activating = false;
            if (Networking.IsOwner(gameObject)) SendCustomNetworkEvent(NetworkEventTarget.All, nameof(Deactivated));
        }

        public void Activated()
        {
            active = true;
            Debug.Log($"[{gameObject.name}] Activated");
        }
        public void Deactivated()
        {
            active = false;
            Debug.Log($"[{gameObject.name}] Deactivated");
        }
    }
}
