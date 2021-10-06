using UdonSharp;
using VRC.SDKBase;

namespace UdonRadioCommunication
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    public class RaycastBlocker : UdonSharpBehaviour
    {
        public override void OnPlayerJoined(VRCPlayerApi player)
        {
            if (player.isLocal && !player.IsUserInVR())
            {
                gameObject.SetActive(false);
            }
        }
    }
}
