#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using RoguelikeCardBattler.Run;
using RoguelikeCardBattler.Gameplay.Relics;

namespace RoguelikeCardBattler.DevTools
{
    // Overlay de debug para agregar/quitar Retazos durante Play Mode.
    // Se auto-instancia al entrar en play — no requiere attach manual.
    // Toggle: ` (backtick) o F2. Solo compila en Editor.
    public class RelicDebugOverlay : MonoBehaviour
    {
        private bool _visible = true;
        private Vector2 _activeScroll;
        private Vector2 _availableScroll;
        private RelicDefinition[] _allRelics;
        private Rect _windowRect = new Rect(10, 40, 340, 520);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoInstantiate()
        {
            GameObject go = new GameObject("[RelicDebug]");
            go.AddComponent<RelicDebugOverlay>();
            DontDestroyOnLoad(go);
        }

        private void Start()
        {
            string[] guids = AssetDatabase.FindAssets("t:RelicDefinition");
            List<RelicDefinition> list = new List<RelicDefinition>();
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                RelicDefinition def = AssetDatabase.LoadAssetAtPath<RelicDefinition>(path);
                if (def != null) list.Add(def);
            }
            list.Sort((a, b) => string.Compare(a.name, b.name, System.StringComparison.Ordinal));
            _allRelics = list.ToArray();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.BackQuote) || Input.GetKeyDown(KeyCode.F2))
                _visible = !_visible;
        }

        private void OnGUI()
        {
            if (!_visible) return;
            _windowRect = GUI.Window(9001, _windowRect, DrawWindow, "Relic Debug  [` o F2 para toggle]");
        }

        private void DrawWindow(int id)
        {
            RunSession session = RunSession.GetOrCreate();
            RunState state = session.State;

            GUILayout.Label("ACTIVOS (" + state.Relics.Count + ")");
            _activeScroll = GUILayout.BeginScrollView(_activeScroll, GUILayout.Height(140));
            for (int i = state.Relics.Count - 1; i >= 0; i--)
            {
                RelicInstance inst = state.Relics[i];
                if (inst?.Definition == null) continue;
                GUILayout.BeginHorizontal();
                GUILayout.Label(inst.Definition.DisplayName, GUILayout.ExpandWidth(true));
                if (GUILayout.Button("Quitar", GUILayout.Width(55)))
                    state.Relics.RemoveAt(i);
                GUILayout.EndHorizontal();
            }
            GUILayout.EndScrollView();

            if (GUILayout.Button("Limpiar todo"))
                state.Relics.Clear();

            GUILayout.Space(6);

            GUILayout.Label("DISPONIBLES");
            _availableScroll = GUILayout.BeginScrollView(_availableScroll, GUILayout.Height(280));
            if (_allRelics != null)
            {
                foreach (RelicDefinition def in _allRelics)
                {
                    if (def == null) continue;
                    bool owned = HasRelic(state, def);
                    GUILayout.BeginHorizontal();
                    GUI.enabled = !owned;
                    GUILayout.Label((owned ? "[ya] " : "") + def.DisplayName, GUILayout.ExpandWidth(true));
                    if (GUILayout.Button("+", GUILayout.Width(24)))
                        state.AddRelic(def);
                    GUI.enabled = true;
                    GUILayout.EndHorizontal();
                }
            }
            GUILayout.EndScrollView();

            GUI.DragWindow();
        }

        private static bool HasRelic(RunState state, RelicDefinition def)
        {
            foreach (RelicInstance inst in state.Relics)
                if (inst?.Definition == def) return true;
            return false;
        }
    }
}
#endif
