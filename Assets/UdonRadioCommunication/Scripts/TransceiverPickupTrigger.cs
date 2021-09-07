
using UdonSharp;

namespace UdonRadioCommunication
{
    public class TransceiverPickupTrigger : UdonSharpBehaviour
    {
        public Transceiver trasnceiver;

        public override void OnPickupUseDown() => trasnceiver._StartTransmit();
        public override void OnPickupUseUp() => trasnceiver._StopTransmit();
    }
}
