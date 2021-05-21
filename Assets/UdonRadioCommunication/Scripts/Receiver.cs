
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace UdonRadioCommunication
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Continuous)]
    public class Receiver : UdonSharpBehaviour
    {
        [HideInInspector] public readonly string UdonTypeID = "UdonRadioCommunication.Receiver";
        [UdonSynced] public bool active;
        [UdonSynced] public float frequency = 122.6f;
        public bool limitRange = true;
        public float maxRange = 5.0f;

        public void Activate()
        {
            active = true;
        }

        public void Deactivate()
        {
            active = false;
        }
    }
}
