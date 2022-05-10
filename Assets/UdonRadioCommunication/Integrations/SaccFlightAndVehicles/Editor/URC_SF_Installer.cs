using System.Linq;
using UdonSharp;
using UdonSharpEditor;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UdonRadioCommunication
{
    public class URC_SF_Installer : EditorWindow
    {
        private readonly BuildTargetGroup[] buildTargetGroups = {
            BuildTargetGroup.Standalone,
            BuildTargetGroup.Android,
        };

        [MenuItem("SaccFlight/UdonRadioCommunication/Installer")]
        public static void ShowWindow()
        {
            var window = GetWindow<URC_SF_Installer>();
            window.Show();
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
        }

#if !URC_SF
        private void OnGUI()
        {
            EditorGUILayout.Space();
            if (GUILayout.Button("Enable URC Integration for SaccFlight"))
            {
                foreach (var buildTargetGroup in buildTargetGroups)
                {
                    var syms = PlayerSettings.GetScriptingDefineSymbolsForGroup(buildTargetGroup);
                    PlayerSettings.SetScriptingDefineSymbolsForGroup(buildTargetGroup, $"URC_SF;{syms}");
                }
                AssetDatabase.Refresh();
            }
        }
#else
        private static UdonRadioCommunication GetOrCreateManager()
        {
            var found = SceneManager.GetActiveScene().GetRootGameObjects().Select(o => o.GetUdonSharpComponent<UdonRadioCommunication>()).FirstOrDefault(a => a != null);
            if (found != null) return found;

            var gameObject = new GameObject("UdonRadioCommunication");
            return gameObject.AddComponent<UdonRadioCommunication>();
        }

        private static void SetupURC()
        {
            var urc = GetOrCreateManager();
            urc.Setup();
        }

        private void Install(Transform seat)
        {
            var transceiverObj = PrefabUtility.InstantiatePrefab(transceiverPrefab, seat) as GameObject;
            Undo.RegisterCreatedObjectUndo(transceiverObj, "Install URC");

            transceiverObj.transform.SetParent(seat, false);
            Undo.RegisterCreatedObjectUndo(transceiverObj, "Install URC");

            SetupURC();
        }

        private void Uninstall(SFComInjector injector)
        {
            Undo.DestroyObjectImmediate(injector.gameObject);

            SetupURC();
        }

        private void OnGUI()
        {
            var scene = SceneManager.GetActiveScene();
            var pilotSeats = scene.GetRootGameObjects().SelectMany(o => o.GetUdonSharpComponentsInChildren<SaccVehicleSeat>());

            var seats = pilotSeats
                .Select(seat =>
                {
                    var seatObject = seat.gameObject;
                    var seatOnly = (GameObject)seat.GetProgramVariable("ThisSeatOnly");
                    var entity = seat.GetComponentInParent<SaccEntity>();
                    var vehicle = entity.GetComponentInChildren<SaccAirVehicle>();
                    return (seat, seatObject, seatOnly, entity, vehicle);
                });

            using (var scrollScope = new EditorGUILayout.ScrollViewScope(scrollPosition))
            {
                scrollPosition = scrollScope.scrollPosition;

                EditorGUILayout.Space();
                transceiverPrefab = EditorGUILayout.ObjectField("Transceiver Template", transceiverPrefab, typeof(GameObject), true) as GameObject;
                EditorGUILayout.Space();

                using (new EditorGUI.DisabledGroupScope(transceiverPrefab == null))
                {
                    foreach (var (seat, seatObject, seatOnly, entity, vehicle) in seats)
                    {
                        using (new EditorGUILayout.HorizontalScope())
                        {
                            EditorGUILayout.ObjectField(entity.gameObject, typeof(GameObject), true);
                            EditorGUILayout.ObjectField(seat, typeof(GameObject), true);

                            var injector = seat.GetUdonSharpComponentInChildren<SFComInjector>();
                            var installed = injector != null;

                            using (new EditorGUI.DisabledGroupScope(installed || !seatOnly))
                            {
                                if (GUILayout.Button(new GUIContent("Install", seatOnly != null ? null : "SaccVehicleSeat.ThisSeatOnly is required"), EditorStyles.miniButtonLeft, miniButtonLayout)) Install(seat.transform);
                            }
                            using (new EditorGUI.DisabledGroupScope(!installed))
                            {
                                if (GUILayout.Button("Uninstall", EditorStyles.miniButtonRight, miniButtonLayout)) Uninstall(injector);
                            }
                        }
                    }
                }

                EditorGUILayout.Space();

                if (GUILayout.Button("Disable URC Integration for SaccFlight"))
                {
                    foreach (var buildTargetGroup in buildTargetGroups)
                    {
                        var syms = PlayerSettings.GetScriptingDefineSymbolsForGroup(buildTargetGroup);
                        PlayerSettings.SetScriptingDefineSymbolsForGroup(buildTargetGroup, syms.Replace("URC_SF;", ""));
                    }
                }
            }
        }
#endif
    }
}
