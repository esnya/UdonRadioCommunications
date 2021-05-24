
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

        public void TakeOwnership()
        {
            if (Networking.IsOwner(gameObject)) return;
            Networking.SetOwner(Networking.LocalPlayer, gameObject);
        }

        public void SetActive(bool value)
        {
            TakeOwnership();
            active = value;
            RequestSerialization();
        }
        public bool IsActive() => active;
        public void Activate() => SetActive(true);

        public void Deactivate() => SetActive(false);

        public void SetFrequency(float f)
        {
            TakeOwnership();
            frequency = f;
            RequestSerialization();
        }

        public float GetFrequency() => frequency;
    }
}
