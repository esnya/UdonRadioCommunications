
using UdonSharp;
using VRC.SDKBase;

namespace UdonRadioCommunication
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class Transmitter : UdonSharpBehaviour
    {
        [UdonSynced] public bool active;
        [UdonSynced] public float frequency = 1.0f;

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
