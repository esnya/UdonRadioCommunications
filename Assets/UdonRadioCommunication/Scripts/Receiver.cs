
using UdonSharp;
using UnityEngine;

namespace UdonRadioCommunication
{
    public class Receiver : UdonSharpBehaviour
    {
        [HideInInspector] public readonly string UdonTypeID = "UdonRadioCommunication.Receiver";
        [HideInInspector] public bool active;
        public float frequency = 122.6f;

        public void _Activate()
        {
            active = true;
            Debug.Log($"[{gameObject.name}] Activated");
        }

        public void _Deactivate()
        {
            active = false;
            Debug.Log($"[{gameObject.name}] Deactivated");
        }
    }
}
