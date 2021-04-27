
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace UdonRadioCommunication
{
    public class Receiver : UdonSharpBehaviour
    {
        [HideInInspector] public readonly string UdonTypeID = "UdonRadioCommunication.Receiver";
        [HideInInspector] public bool active;
        public float frequency = 122.6f;
        public bool limitRange = true;
        public float maxRange = 5.0f;

        public void Activate()
        {
            if (!Networking.IsOwner(gameObject))
            {
                Networking.SetOwner(Networking.LocalPlayer, gameObject);
                SendCustomEventDelayedSeconds(nameof(Activate), 0.5f);
                return;
            }
            active = true;
            Debug.Log($"[{gameObject.name}] Activated");
        }

        public void Deactivate()
        {
            active = false;
            Debug.Log($"[{gameObject.name}] Deactivated");
        }
    }
}
