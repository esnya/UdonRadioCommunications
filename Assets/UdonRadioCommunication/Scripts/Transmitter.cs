
using UdonSharp;
using VRC.SDKBase;

namespace UdonRadioCommunication
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class Transmitter : UdonSharpBehaviour
    {
        [UdonSynced] private bool active;
        [UdonSynced] private float frequency = 122.6f;

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
