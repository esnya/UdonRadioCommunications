using SaccFlightAndVehicles;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace URC
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class DFUNC_URC_RX : UdonSharpBehaviour
    {
        public GameObject Dial_Funcon;
        public KeyCode desktopKey = KeyCode.L;
        public Receiver receiver;
        public bool activeOnEnter = true;

        private string triggerAxis;
        public void DFUNC_LeftDial()
        {
            triggerAxis = "Oculus_CrossPlatform_PrimaryIndexTrigger";
        }
        public void DFUNC_RightDial()
        {
            triggerAxis = "Oculus_CrossPlatform_SecondaryIndexTrigger";
        }

        private SaccEntity CommonInit()
        {
            var entity = GetComponentInParent<SaccEntity>();

            receiver.indicator = Dial_Funcon;

            return entity;
        }

        public void SFEXT_L_EntityStart()
        {
            var entity = CommonInit();
            foreach (var seat in entity.gameObject.GetComponentsInChildren<SaccVehicleSeat>())
            {
                if (seat.IsPilotSeat)
                {
                    receiver.transform.SetParent(entity.transform, true);
                    break;
                }
            }
        }
        public void SFEXTP_L_EntityStart()
        {
            var entity = CommonInit();
            var seat = gameObject.GetComponentInParent<SaccVehicleSeat>();
            receiver.transform.SetParent(seat ? seat.transform : entity.transform, true);
        }

        public void SFEXT_O_PilotEnter()
        {
            receiver.Active = activeOnEnter;
            if (!Networking.LocalPlayer.IsUserInVR()) DFUNC_Selected();
        }
        public void SFEXT_O_PilotExit()
        {
            receiver._Deactivate();
            DFUNC_Deselected();
        }
        public void SFEXTP_O_UserEnter() => SFEXT_O_PilotEnter();
        public void SFEXTP_O_UserExit() => SFEXT_O_PilotExit();

        public void DFUNC_Selected()
        {
            gameObject.SetActive(true);
        }
        public void DFUNC_Deselected()
        {
            gameObject.SetActive(false);
        }

        private bool prevInput;
        private void Update()
        {
            var input = GetInput();
            if (input && !prevInput) receiver._ToggleActive();
            prevInput = input;
        }

        private bool GetInput()
        {
            return Input.GetKey(desktopKey) || Input.GetAxisRaw(triggerAxis) > 0.75f;
        }
    }
}
