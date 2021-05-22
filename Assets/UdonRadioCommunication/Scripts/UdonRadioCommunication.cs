
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

#if !COMPILER_UDONSHARP && UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
using UdonSharpEditor;
#endif

namespace UdonRadioCommunication
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    public class UdonRadioCommunication : UdonSharpBehaviour
    {
        [HideInInspector] public readonly string UdonTypeID = "UdonRadioCommunication.UdonRadioCommunication";

        [Range(0, 24)] public float defaultVoiceGain = 15;
        [Range(0, 1000000)] public float defaultVoiceDistanceNear = 0;
        [Range(0, 1000000)] public float defaultVoiceDistanceFar = 25;
        [Range(0, 1000)] public float defaultVoiceVolumetricRadius = 0;
        public float distanceAttenuation = 10.0f;

        [Space]

        public Transmitter[] transmitters;
        public Receiver[] receivers;
        private bool playerListDirty = true;
        private VRCPlayerApi[] players = {};
        private Transmitter[] playerTransmitters = {};
        private bool[] playerPrevIsDefaultVoice = {};

        private void Start()
        {
            Debug.Log($"[{gameObject.name}] Started with {transmitters.Length} transmitters, {receivers.Length} receivers");
        }

        private int GetPlayerIndex(VRCPlayerApi player)
        {
            if (Utilities.IsValid(player))
            {
                for (int i = 0; i < players.Length; i++)
                {
                    if (Utilities.IsValid(players[i]) && player.playerId == players[i].playerId) {
                        return i;
                    }
                }
            }

            return -1;
        }

        private void UpdatePlayerVoice(VRCPlayerApi player, float gain, float near, float far, float volumetric)
        {
            // Debug.Log($"[{gameObject.name}] Update player ({player.playerId}:{player.displayName}) voice {gain}/{near}-{far}/{volumetric}");
            player.SetVoiceGain(gain);
            player.SetVoiceDistanceNear(near);
            player.SetVoiceDistanceFar(far);
            player.SetVoiceVolumetricRadius(volumetric);
        }

        private Receiver GetReceiver(float frequency)
        {
            var localPosition = Networking.LocalPlayer.GetPosition();
            float minDistance = float.MaxValue;
            Receiver result = null;
            foreach (var r in receivers) {
                if (!r.IsAcive() || r.GetFrequency() != frequency) continue;

                var distance = Vector3.SqrMagnitude(r.transform.position - localPosition);
                if ((!r.limitRange ||  distance <= r.maxRange) && distance < minDistance) result = r;
            }
            return result;
        }

        private void Update()
        {
            var localPlayer = Networking.LocalPlayer;
            if (!Utilities.IsValid(localPlayer)) return;

            if (playerListDirty)
            {
                Debug.Log($"Updating player list");
                playerListDirty = false;
                players = new VRCPlayerApi[VRCPlayerApi.GetPlayerCount()];
                VRCPlayerApi.GetPlayers(players);
                playerTransmitters = new Transmitter[players.Length];
                playerPrevIsDefaultVoice = new bool[players.Length];
            }

            for (int i = 0; i < playerTransmitters.Length; i++)
            {
                playerTransmitters[i] = null;
            }

            foreach (var transmitter in transmitters)
            {
                if (!transmitter.IsActive()) continue;

                var owner = Networking.GetOwner(transmitter.gameObject);
                var index = GetPlayerIndex(owner);
                if (index < 0) continue;
                playerTransmitters[index] = transmitter;
            }

            var localPlayerPosition = localPlayer.GetPosition();
            for (int i = 0; i < players.Length; i++)
            {
                var remotePlayer = players[i];
                if (remotePlayer.isLocal) continue;

                var transmitter = playerTransmitters[i];
                Receiver receiver = transmitter == null ? null : GetReceiver(transmitter.GetFrequency());
                var isDefaultVoice = receiver == null;

                if (isDefaultVoice)
                {
                    if (!playerPrevIsDefaultVoice[i]) UpdatePlayerVoice(remotePlayer, defaultVoiceGain, defaultVoiceDistanceNear, defaultVoiceDistanceFar, defaultVoiceVolumetricRadius);
                }
                else
                {
                    if (Utilities.IsValid(receiver) && Utilities.IsValid(remotePlayer))
                    {
                        var receiverPosition = receiver.transform.position;
                        var remotePlayerPosition = remotePlayer.GetPosition();
                        var transmitterPosition = transmitter.transform.position;

                        var distanceOverRadio = (Vector3.Distance(remotePlayerPosition, transmitterPosition) + Vector3.Distance(localPlayerPosition, receiverPosition)) * 0;
                        var realDistance = Vector3.Distance(localPlayerPosition, remotePlayerPosition);
                        // Debug.Log($"|{remotePlayerPosition}-{localPlayerPosition}|={realDistance}");

                        var near = Mathf.Max(realDistance - distanceOverRadio, 0);
                        var far = near + defaultVoiceDistanceFar - defaultVoiceDistanceNear;
                        var gain = defaultVoiceGain / Mathf.Max(1.0f + Mathf.Pow(distanceOverRadio * distanceAttenuation, 2.0f), 1);
                        UpdatePlayerVoice(remotePlayer, gain, near, far, (near + far) / 2.0f);
                    }
                }

                playerPrevIsDefaultVoice[i] = isDefaultVoice;
            }
        }

        public override void OnPlayerJoined(VRCPlayerApi player)
        {
            playerListDirty = true;
        }
        public override void OnPlayerLeft(VRCPlayerApi player)
        {
            playerListDirty = true;
        }

#if !COMPILER_UDONSHARP && UNITY_EDITOR
        public void Setup()
        {
            this.UpdateProxy();
            var rootObjects = SceneManager.GetActiveScene().GetRootGameObjects();
            transmitters = rootObjects.SelectMany(o => o.GetUdonSharpComponentsInChildren<Transmitter>()).ToArray();
            receivers = rootObjects.SelectMany(o => o.GetUdonSharpComponentsInChildren<Receiver>()).ToArray();
            this.ApplyProxyModifications();
        }

        static private void SetupAll(Scene scene)
        {
            var targets = scene
                .GetRootGameObjects()
                .SelectMany(o => o.GetUdonSharpComponentsInChildren<UdonRadioCommunication>());
            foreach (var urc in targets)
            {
                urc.Setup();
            }
        }

        static UdonRadioCommunication()
        {
            EditorSceneManager.sceneOpened += (s,m) => SetupAll(s);
            EditorSceneManager.sceneClosing += (s,m) => SetupAll(s);
        }
#endif
    }

#if !COMPILER_UDONSHARP && UNITY_EDITOR

    [CustomEditor(typeof(UdonRadioCommunication))]
    public class UdonRadioCommunicationEditor : Editor {
        public override void OnInspectorGUI() {
            if (UdonSharpGUI.DrawDefaultUdonSharpBehaviourHeader(target)) return;
            base.OnInspectorGUI();

            EditorGUILayout.Space();

            var urc = target as UdonRadioCommunication;
            if (GUILayout.Button("Setup"))
            {
                urc.Setup();
            }
        }
    }
#endif
}
