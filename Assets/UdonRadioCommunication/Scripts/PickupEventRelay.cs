
using UdonSharp;

namespace UdonRadioCommunication
{
    public class PickupEventRelay : UdonSharpBehaviour
    {
        public UdonSharpBehaviour target;
        public string onPickupUseDown;
        public string onPickupUseUp;

        public override void OnPickupUseDown() => target.SendCustomEvent(onPickupUseDown);
        public override void OnPickupUseUp() => target.SendCustomEvent(onPickupUseUp);
    }
}
