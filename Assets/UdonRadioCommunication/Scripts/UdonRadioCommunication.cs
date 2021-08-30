#pragma warning disable IDE0051

using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using TMPro;

#if !COMPILER_UDONSHARP && UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UdonSharpEditor;
#endif

namespace UdonRadioCommunication
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    public class UdonRadioCommunication : UdonSharpBehaviour
    {
        public const float MaxDistance = 1000000.0f;
        public const float MaxVolumetricRadius = 1000.0f;

        [Range(0, 24)] public float defaultVoiceGain = 15;
        [Range(0, 1000000)] public float defaultVoiceDistanceNear = 0;
        [Range(0, 1000000)] public float defaultVoiceDistanceFar = 25;
        [Range(0, 1000)] public float defaultVoiceVolumetricRadius = 0;
        public float distanceAttenuation = 10.0f;
        public bool disableLowpassFilter = true;

        [Space]
        public bool autoSetupBeforeSave = true;

        [Space]
        public Transmitter[] transmitters;
        public Receiver[] receivers;

        [Space]
        public TextMeshPro debugText;
        public TextMeshProUGUI debugTextUi;

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

        private void UpdatePlayerVoice(VRCPlayerApi player, float gain, float near, float far, float volumetric, bool lowpassEnabled)
        {
            // Debug.Log($"[{gameObject.name}] Update player ({player.playerId}:{player.displayName}) voice {gain}/{near}-{far}/{volumetric}/{lowpassEnabled}");
            player.SetVoiceGain(gain);
            player.SetVoiceDistanceNear(Mathf.Clamp(near, 0.0f, MaxDistance));
            player.SetVoiceDistanceFar(Mathf.Clamp(far, 0.0f, MaxDistance));
            player.SetVoiceVolumetricRadius(Mathf.Clamp(volumetric, 0.0f, MaxVolumetricRadius));
            if (disableLowpassFilter) player.SetVoiceLowpass(lowpassEnabled);
        }

        private Receiver GetReceiver(float frequency)
        {
            var localPosition = Networking.LocalPlayer.GetPosition();
            float minDistance = float.MaxValue;
            Receiver result = null;
            foreach (var r in receivers) {
                if (r == null || !r.active || r.frequency != frequency) continue;

                var distance = Vector3.SqrMagnitude(r.transform.position - localPosition);
                if ((!r.limitRange || distance <= Mathf.Pow(r.maxRange, 2.0f)) && distance < minDistance) result = r;
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

            var localPlayerPosition = localPlayer.GetPosition();
            foreach (var transmitter in transmitters)
            {
                if (
                    transmitter == null
                    || !transmitter.active
                    || (transmitter.transform.position - localPlayerPosition).sqrMagnitude < Mathf.Pow(transmitter.minDistance, 2)
                ) continue;

                var owner = Networking.GetOwner(transmitter.gameObject);
                var index = GetPlayerIndex(owner);
                if (index < 0) continue;

                playerTransmitters[index] = transmitter;

            }

            for (int i = 0; i < players.Length; i++)
            {
                var remotePlayer = players[i];
                if (remotePlayer.isLocal) continue;

                var transmitter = playerTransmitters[i];
                Receiver receiver = transmitter == null ? null : GetReceiver(transmitter.frequency);
                var isDefaultVoice = receiver == null;

                if (isDefaultVoice)
                {
                    if (!playerPrevIsDefaultVoice[i]) UpdatePlayerVoice(remotePlayer, defaultVoiceGain, defaultVoiceDistanceNear, defaultVoiceDistanceFar, defaultVoiceVolumetricRadius, true);
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

                        var near = Mathf.Max(realDistance - distanceOverRadio, 0);
                        var far = near + defaultVoiceDistanceFar - defaultVoiceDistanceNear;
                        var gain = defaultVoiceGain / Mathf.Max(1.0f + Mathf.Pow(distanceOverRadio * distanceAttenuation, 2.0f), 1);
                        UpdatePlayerVoice(remotePlayer, gain, near, far, far, false);
                    }
                }

                playerPrevIsDefaultVoice[i] = isDefaultVoice;
            }

            if (debugText != null && debugText.gameObject.activeInHierarchy || debugTextUi != null && ((Component)debugTextUi).gameObject.activeInHierarchy)
            {
                var text = "<color=red>!! For Debug Only !!</color>\n\nTransmitters:\n";
                var activeText = "<color=green>Active</color>";
                var nonActiveText = "<color=blue>Disabled</color>";

                for (int i = 0; i < transmitters.Length; i++)
                {
                    var transmitter = transmitters[i];
                    if (transmitter == null) continue;
                    var owner = Networking.GetOwner(transmitter.gameObject);
                    var near = (transmitter.transform.position - localPlayerPosition).sqrMagnitude < Mathf.Pow(transmitter.minDistance, 2);
                    text += $"\t{i:000}:{GetUniqueName(transmitter)}\t{(transmitter.active ? activeText : nonActiveText)}\t{transmitter.frequency:#0.00}\t{(near ? "<color=red>Too Close</color>" : "<color=green>Range Clear</color>")}\t{GetDebugPlayerString(owner)}\n";
                }

                text += "\nReceivers:\n";
                for (int i = 0; i < receivers.Length; i++)
                {
                    var receiver = receivers[i];
                    if (receiver == null) continue;
                    var owner = Networking.GetOwner(receiver.gameObject);
                    text += $"\t{i:000}:{GetUniqueName(receiver)}\t{(receiver.active ? activeText : nonActiveText)}\t{receiver.frequency:#0.00}\t{(receiver.sync ? "Sync" : "Local")}\t{GetDebugPlayerString(owner)}\n";
                }

                text += "\nPlayers:\n";
                var talkingText = "<color=red>Talking</color>";
                var defaultVoiceText = "<color=green>Default</color>";
                for (int i = 0; i < players.Length; i++)
                {
                    var player = players[i];
                    if (!Utilities.IsValid(player)) continue;

                    var transmitter = playerTransmitters[i];
                    var receiver = transmitter == null ? (Receiver)null : GetReceiver(transmitter.frequency);

                    text += $"\t{i:000}:{GetDebugPlayerString(player)}\t{GetUniqueName(transmitter)}\t{GetUniqueName(receiver)}\t{(player.isLocal ? "<color=blue>Local</color>" : playerPrevIsDefaultVoice[i] ? defaultVoiceText : talkingText)}\n";
                }

                if (debugText != null) debugText.text = text;
                if (debugTextUi != null) debugTextUi.text = text;
            }
        }

        private string GetUniqueName(Object o)
        {
            if (o == null) return " - ";
            return $"{o.GetInstanceID():x8}@{o}";
        }

        private string GetDebugPlayerString(VRCPlayerApi player)
        {
            return $"({player.playerId:000}){player.displayName}";
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
        private static IEnumerable<T> GetUdonSharpComponentsInScene<T>() where T : UdonSharpBehaviour
        {
            return FindObjectsOfType<UdonBehaviour>()
                .Where(UdonSharpEditorUtility.IsUdonSharpBehaviour)
                .Select(UdonSharpEditorUtility.GetProxyBehaviour)
                .Select(u => u as T)
                .Where(u => u != null);
        }

        public void Setup()
        {
            this.UpdateProxy();
            transmitters = GetUdonSharpComponentsInScene<Transmitter>().ToArray();
            this.ApplyProxyModifications();
            EditorUtility.SetDirty(UdonSharpEditorUtility.GetBackingUdonBehaviour(this));

            this.UpdateProxy();
            receivers = GetUdonSharpComponentsInScene<Receiver>().ToArray();
            this.ApplyProxyModifications();

            EditorUtility.SetDirty(UdonSharpEditorUtility.GetBackingUdonBehaviour(this));
        }

        static private void SetupAll()
        {
            var targets = GetUdonSharpComponentsInScene<UdonRadioCommunication>();
            foreach (var urc in targets)
            {
                urc.Setup();
            }
        }
#endif
    }

#if !COMPILER_UDONSHARP && UNITY_EDITOR

    [CustomEditor(typeof(UdonRadioCommunication))]
    public class UdonRadioCommunicationEditor : Editor {
        private static IEnumerable<T> GetUdonSharpComponentsInScene<T>() where T : UdonSharpBehaviour
        {
            return FindObjectsOfType<UdonBehaviour>()
                .Where(UdonSharpEditorUtility.IsUdonSharpBehaviour)
                .Select(UdonSharpEditorUtility.GetProxyBehaviour)
                .Select(u => u as T)
                .Where(u => u != null);
        }

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

        [InitializeOnLoadMethod]
        static public void RegisterCallback()
        {
            EditorSceneManager.sceneSaving += (_, __) => SetupAll();
        }

        private static void SetupAll()
        {
            var urcs = GetUdonSharpComponentsInScene<UdonRadioCommunication>();
            foreach (var urc in urcs)
            {
                if (urc?.autoSetupBeforeSave != true) continue;
                Debug.Log($"[{urc.gameObject.name}] Auto setup");
                urc.Setup();
            }
        }
    }
#endif
}
