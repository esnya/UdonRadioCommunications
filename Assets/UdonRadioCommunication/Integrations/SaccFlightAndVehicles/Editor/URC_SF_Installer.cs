using System.Linq;
using UdonSharp;
using UdonSharpEditor;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using VRC.Udon;

namespace UdonRadioCommunication
{
    public class URC_SF_Installer : EditorWindow
    {
        [MenuItem("UdonRadioCommunication/Installer for SaccFight")]
        private static void ShowWindow()
        {
            var window = GetWindow<URC_SF_Installer>();
            window.Show();
        }

        private static void ClearLocalTransform(Transform t)
        {
            t.localPosition = Vector3.zero;
            t.localRotation = Quaternion.identity;
            t.localScale = Vector3.one;
        }

        public GameObject transceiverPrefab;

        private Vector2 scrollPosition;
        private readonly GUILayoutOption[] miniButtonLayout = {
            GUILayout.ExpandWidth(false),
            GUILayout.Width(200),
        };

        private void OnEnable()
        {
            titleContent = new GUIContent("URC Installer for SaccFlight");
            if (transceiverPrefab == null) transceiverPrefab = Resources.Load<GameObject>("SFVehicleTransceiver_SF-1");
        }

#if !URC_SF
        private void OnGUI()
        {
            EditorGUILayout.Space();
            if (GUILayout.Button("Enable URC Integration for SaccFlight"))
            {
                var buildTargetGroup = EditorUserBuildSettings.selectedBuildTargetGroup;
                var syms = PlayerSettings.GetScriptingDefineSymbolsForGroup(buildTargetGroup);
                PlayerSettings.SetScriptingDefineSymbolsForGroup(buildTargetGroup, $"URC_SF;{syms}");
                AssetDatabase.Refresh();
            }
        }
#else
        private static UdonRadioCommunication GetOrCreateManager()
        {
            var found = SceneManager.GetActiveScene().GetRootGameObjects().Select(o => o.GetUdonSharpComponent<UdonRadioCommunication>()).FirstOrDefault(a => a != null);
            if (found != null) return found;

            var gameObject = new GameObject("UdonRadioCommunication");
            var created = gameObject.AddComponent<UdonRadioCommunication>();
            var udon = UdonSharpEditorUtility.ConvertToUdonBehaviours(new[] { created }).First();

            return UdonSharpEditorUtility.GetProxyBehaviour(udon) as UdonRadioCommunication;
        }

        private static void SetupURC()
        {
            var urc = GetOrCreateManager();
            urc.Setup();
        }

        private void Install(Transform planeMesh, Transform pilotOnly)
        {
            var transceiverObj = PrefabUtility.InstantiatePrefab(transceiverPrefab, planeMesh) as GameObject;
            Undo.RegisterCreatedObjectUndo(transceiverObj, "Install URC");

            var triggerObj = new GameObject("TrasceiverTrigger");
            triggerObj.transform.SetParent(pilotOnly, false);
            var trigger = triggerObj.AddUdonSharpComponent<TransceiverEnabledTrigger>();
            trigger.transceiver = transceiverObj.GetUdonSharpComponent<Transceiver>();
            trigger.ApplyProxyModifications();
            Undo.RegisterCreatedObjectUndo(transceiverObj, "Install URC");

            SetupURC();
        }

        private void Uninstall(TransceiverEnabledTrigger trigger)
        {
            if (trigger.transceiver != null) Undo.DestroyObjectImmediate(trigger.transceiver.gameObject);
            Undo.DestroyObjectImmediate(trigger.gameObject);

            SetupURC();
        }

        private GameObject GetSeatedUserOnly(UdonSharpBehaviour seat)
        {
            var type = seat.GetType();
            return (
                type.GetField("LeaveButton")?.GetValue(seat) // 1.4 or before
                    ?? type.GetField("PilotOnly")?.GetValue(seat)
                    ?? type.GetField("PassengerOnly")?.GetValue(seat)
             ) as GameObject;
        }

        private void OnGUI()
        {
            var scene = SceneManager.GetActiveScene();
            var pilotSeats = scene.GetRootGameObjects().SelectMany(o => o.GetUdonSharpComponentsInChildren<PilotSeat>());
            var passengerSeats = scene.GetRootGameObjects().SelectMany(o => o.GetUdonSharpComponentsInChildren<PassengerSeat>());

            var seats = pilotSeats.Select(s => (s.gameObject, s.EngineControl, seatedUserOnly: GetSeatedUserOnly(s), true))
                .Concat(passengerSeats.Select(s => (s.gameObject, s.EngineControl, seatedUserOnly: GetSeatedUserOnly(s), false)))
                .Where(s => s.EngineControl != null && s.seatedUserOnly != null);

            using (var scrollScope = new EditorGUILayout.ScrollViewScope(scrollPosition))
            {
                scrollPosition = scrollScope.scrollPosition;

                EditorGUILayout.Space();
                transceiverPrefab = EditorGUILayout.ObjectField("Transceiver Template", transceiverPrefab, typeof(GameObject), true) as GameObject;
                EditorGUILayout.Space();

                using (new EditorGUI.DisabledGroupScope(transceiverPrefab == null))
                {
                    foreach (var (seat, engineController, pilotOnly, isPilot) in seats)
                    {
                        var planeMesh = engineController.PlaneMesh;
                        using (new EditorGUILayout.HorizontalScope())
                        {
                            EditorGUILayout.ObjectField(engineController.VehicleMainObj ?? engineController.gameObject, typeof(GameObject), true);
                            EditorGUILayout.ObjectField(seat, typeof(GameObject), true);

                            if (planeMesh == null)
                            {
                                EditorGUILayout.HelpBox("EnginController.PlaneMesh is required", MessageType.Error);
                                return;
                            }
                            if (pilotOnly == null)
                            {
                                EditorGUILayout.HelpBox("PilotSeat.LeaveButton/PassengerSeat.LeaveButton is required", MessageType.Error);
                                return;
                            }

                            var trigger = pilotOnly.GetUdonSharpComponentInChildren<TransceiverEnabledTrigger>();
                            var installed = trigger != null;

                            using (new EditorGUI.DisabledGroupScope(trigger))
                            {
                                if (GUILayout.Button(isPilot ? "Install" : "Install (Experimental)", EditorStyles.miniButtonLeft, miniButtonLayout)) Install(planeMesh.transform, pilotOnly.transform);
                            }
                            using (new EditorGUI.DisabledGroupScope(!installed))
                            {
                                if (GUILayout.Button("Uninstall", EditorStyles.miniButtonRight, miniButtonLayout)) Uninstall(trigger);
                            }
                        }
                    }
                }

                EditorGUILayout.Space();

                if (GUILayout.Button("Disable URC Integration for SaccFlight"))
                {
                    var buildTargetGroup = EditorUserBuildSettings.selectedBuildTargetGroup;
                    var syms = PlayerSettings.GetScriptingDefineSymbolsForGroup(buildTargetGroup);
                    PlayerSettings.SetScriptingDefineSymbolsForGroup(buildTargetGroup, syms.Replace("URC_SF;", ""));
                }
            }
        }
#endif
    }
}
