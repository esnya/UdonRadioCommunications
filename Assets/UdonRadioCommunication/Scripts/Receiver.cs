
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
