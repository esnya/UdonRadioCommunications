using UdonSharp;
using VRC.SDKBase;

namespace URC
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class DesktopOnly : UdonSharpBehaviour
    {
        private void Start()
        {
            if (Networking.LocalPlayer.IsUserInVR()) gameObject.SetActive(false);
        }
    }
}
