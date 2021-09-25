using UdonSharp;
using VRC.SDKBase;

namespace UdonRadioCommunication
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    public class RaycastBlocker : UdonSharpBehaviour
    {
        private void Start()
        {
            if (!Networking.LocalPlayer.IsUserInVR())
            {
                gameObject.SetActive(false);
            }
        }
    }
}
