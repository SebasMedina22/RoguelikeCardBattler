using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using RoguelikeCardBattler.Core;
using RoguelikeCardBattler.Gameplay.Cards;
using RoguelikeCardBattler.Gameplay.Combat;
using RoguelikeCardBattler.Gameplay.Relics;
using RoguelikeCardBattler.Gameplay.Cards.UI;
using RoguelikeCardBattler.Gameplay.Relics.UI;
using RoguelikeCardBattler.Run.Campfire;
using RoguelikeCardBattler.Run.Shop;
using RoguelikeCardBattler.Run.Events;

namespace RoguelikeCardBattler.Run
{
    /// <summary>
    /// Controlador mínimo del loop de run: mapa -> nodo -> resultado -> mapa.
    /// Construye UI simple en runtime leyendo/escribiendo RunState.
    /// </summary>
    public class RunFlowController : MonoBehaviour
    {
        [SerializeField] private Font uiFont;
        [SerializeField] private RunCombatConfig runCombatConfig;
        [SerializeField] private CampfireConfig campfireConfig;
        [SerializeField] private ShopConfig shopConfig;

        private const string RunSceneName = "RunScene";
        private const string BattleSceneName = "BattleScene";
        private const string MainMenuSceneName = "MainMenuScene";

        private RunState _state;
        private ActMap _map;
        private Canvas _canvas;
        private RectTransform _root;
        private RectTransform _mapPanel;
        private RectTransform _resolvePanel;
        private RectTransform _defeatPanel;
        private RectTransform _rewardPanel;
        private RectTransform _actoCompletedPanel;
        private Text _statusText;
        private Text _titleText;
        private RunMapView _mapView;
        private RelicInventoryView _relicView;
        private DeckViewerView _deckViewer;
        private Text _resolveTitleText;
        private Text _resolveBodyText;
        private Button _resolveCompleteButton;
        private Button _resolveBackButton;
        private int _resolveNodeId = -1;
        private Button _defeatRetryButton;
        private Button _defeatExitButton;
        private Text _rewardTitleText;
        private Text _rewardGoldText;
        private readonly List<Button> _rewardButtons = new List<Button>();
        private readonly List<CardDeckEntry> _rewardOptions = new List<CardDeckEntry>();
        private int _rewardNodeId = -1;
        private static Sprite _whiteSprite;
        private bool _rewardConfigErrorLogged;
        private CampfireNodeController _campfireController;
        private ShopNodeController _shopController;
        private EventNodeController _eventController;

        private void Awake()
        {
            RunFlowController existing = Object.FindFirstObjectByType<RunFlowController>();
            if (existing != null && existing != this)
            {
                Destroy(gameObject);
                return;
            }

            RunSession session = RunSession.GetOrCreate();
            _state = session.State;
            _map = session.Map;
            _state.EnsureInitialized(_map);
            if (runCombatConfig == null && session.CombatConfig != null)
            {
                runCombatConfig = session.CombatConfig;
            }
            RunCombatConfig effectiveConfig = GetCombatConfig();
            if (effectiveConfig != null)
            {
                session.ConfigureCombat(effectiveConfig);
            }

            if (uiFont == null)
            {
                uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            }

            EnsureEventSystem();
            BuildUI();
            HandlePendingBattleResult();
            if (_rewardPanel.gameObject.activeSelf)
            {
                PrintMapDebugOnce();
                return;
            }
            if (_actoCompletedPanel.gameObject.activeSelf)
            {
                PrintMapDebugOnce();
                return;
            }
            if (_state.RunFailed)
            {
                ShowDefeatPanel();
            }
            else
            {
                ShowMap();
            }
            PrintMapDebugOnce();
        }

        private void BuildUI()
        {
            _canvas = CreateCanvas("RunCanvas");
            _root = _canvas.GetComponent<RectTransform>();

            _mapPanel = CreatePanel("MapPanel", _root, new Color(0f, 0f, 0f, 0f), false);
            _resolvePanel = CreatePanel("NodeResolvePanel", _root, new Color(0f, 0f, 0f, 0.6f), true);
            _defeatPanel = CreatePanel("DefeatPanel", _root, new Color(0f, 0f, 0f, 0.7f), true);
            _rewardPanel = CreatePanel("RewardPanel", _root, new Color(0f, 0f, 0f, 0.7f), true);
            _actoCompletedPanel = CreatePanel("ActoCompletedPanel", _root, new Color(0f, 0f, 0f, 0.7f), true);
            _resolvePanel.gameObject.SetActive(false);
            _defeatPanel.gameObject.SetActive(false);
            _rewardPanel.gameObject.SetActive(false);
            _actoCompletedPanel.gameObject.SetActive(false);

            _titleText = CreateText("Title", _mapPanel, "Run - Acto 1 (placeholder)", 28, TextAnchor.UpperCenter);
            RectTransform titleRect = _titleText.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.1f, 0.88f);
            titleRect.anchorMax = new Vector2(0.9f, 0.98f);

            _statusText = CreateText("Status", _mapPanel, "Gold: 0 | Completados: 0/3", 20, TextAnchor.UpperCenter);
            RectTransform statusRect = _statusText.GetComponent<RectTransform>();
            statusRect.anchorMin = new Vector2(0.1f, 0.8f);
            statusRect.anchorMax = new Vector2(0.9f, 0.88f);

            _mapView = new RunMapView(_mapPanel, _map, _state, uiFont, GetWhiteSprite());
            _mapView.OnNodeClicked += EnterNode;

            // Fila de Retazos en el HUD del mapa (espejo del HUD de combate, Sub-PR 3F):
            // banda dedicada 0.72–0.79, arriba del scroll (techo 0.72) y bajo el status
            // (0.80) → no colisiona con el área scrolleable ni con los raycasts de nodos.
            RectTransform relicBar = CreatePanel("RelicBar", _mapPanel, new Color(0f, 0f, 0f, 0f), false);
            relicBar.anchorMin = new Vector2(0.02f, 0.72f);
            relicBar.anchorMax = new Vector2(0.98f, 0.79f);
            relicBar.offsetMin = Vector2.zero;
            relicBar.offsetMax = Vector2.zero;
            _relicView = new RelicInventoryView(relicBar, uiFont);

            BuildResolvePanel();
            BuildDefeatPanel();
            BuildRewardPanel();
            BuildActoCompletedPanel();
            BuildCampfireController();
            BuildShopController();
            BuildEventController();

            // Visor de mazo: sub-Canvas overlay, flota sobre paneles opacos (Tienda/Hoguera).
            _deckViewer = new DeckViewerView(_canvas, uiFont);
            _deckViewer.Refresh(_state.GetDeckSnapshot());
        }

        private void BuildCampfireController()
        {
            GameObject go = new GameObject("CampfireController");
            go.transform.SetParent(transform, false);
            _campfireController = go.AddComponent<CampfireNodeController>();
            RelicHookDispatcher dispatcher = RunSession.GetOrCreate().RelicDispatcher;
            _campfireController.Initialize(_canvas, _state, dispatcher, campfireConfig, OnCampfireComplete);
        }

        private void BuildShopController()
        {
            GameObject go = new GameObject("ShopController");
            go.transform.SetParent(transform, false);
            _shopController = go.AddComponent<ShopNodeController>();
            RelicHookDispatcher dispatcher = RunSession.GetOrCreate().RelicDispatcher;
            _shopController.Initialize(_canvas, _state, dispatcher, shopConfig, OnShopComplete);
        }

        private void BuildEventController()
        {
            GameObject go = new GameObject("EventController");
            go.transform.SetParent(transform, false);
            _eventController = go.AddComponent<EventNodeController>();
            RunSession session = RunSession.GetOrCreate();
            // El pool vive en RunSession (lo usa también la generación del mapa) →
            // fuente única, sin doble cableado en el inspector. Aquí sólo lo lee para
            // el fondo del panel; el evento concreto de cada nodo viaja en
            // MapNode.AssignedEvent.
            _eventController.Initialize(_canvas, _state, session.RelicDispatcher, session.EventPoolConfig, OnEventComplete);
        }

        private void EnsureEventSystem()
        {
            EventSystem[] all = Object.FindObjectsByType<EventSystem>(FindObjectsSortMode.None);
            EventSystem existing = all.Length > 0 ? all[0] : null;

#if UNITY_EDITOR
            if (all.Length > 1)
            {
                Debug.LogWarning($"[RunUI] Multiple EventSystems detected: {all.Length}. Keeping the first one.");
            }
#endif

            for (int i = 1; i < all.Length; i++)
            {
                if (all[i] != null)
                {
                    Destroy(all[i].gameObject);
                }
            }

            GameObject target = existing != null ? existing.gameObject : new GameObject("EventSystem", typeof(EventSystem));

            System.Type inputSystemType = System.Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
            if (inputSystemType != null)
            {
                StandaloneInputModule legacy = target.GetComponent<StandaloneInputModule>();
                if (legacy != null)
                {
                    Destroy(legacy);
                }

                if (target.GetComponent(inputSystemType) == null)
                {
                    target.AddComponent(inputSystemType);
                }

#if UNITY_EDITOR
                Debug.Log("[RunUI] EventSystem module: InputSystemUIInputModule");
#endif
                return;
            }

            if (target.GetComponent<StandaloneInputModule>() == null)
            {
                foreach (Component component in target.GetComponents<Component>())
                {
                    if (component != null && component.GetType().FullName == "UnityEngine.InputSystem.UI.InputSystemUIInputModule")
                    {
                        Destroy(component);
                    }
                }

                target.AddComponent<StandaloneInputModule>();
            }

#if UNITY_EDITOR
            Debug.Log("[RunUI] EventSystem module: StandaloneInputModule");
#endif
        }

        private void ShowMap()
        {
            _resolvePanel.gameObject.SetActive(false);
            _defeatPanel.gameObject.SetActive(false);
            _rewardPanel.gameObject.SetActive(false);
            _actoCompletedPanel.gameObject.SetActive(false);
            _mapPanel.gameObject.SetActive(true);
            _titleText.text = "Mapa Acto 1";

            int completed = 0;
            foreach (MapNode node in _map.Nodes)
            {
                if (_state.IsNodeCompleted(node.Id))
                {
                    completed++;
                }
            }

            _statusText.text = $"Gold: {_state.Gold} | Completados: {completed}/{_map.Nodes.Count}";
            _mapView?.Refresh(_state);
            // Suficiente refrescar acá: los Retazos sólo cambian fuera del mapa
            // (combate/elite/boss/tienda) y todo retorno al mapa pasa por ShowMap.
            _relicView?.Refresh(_state.Relics);
            // El mazo puede cambiar al volver de combate (recompensa) o de Tienda/Hoguera.
            _deckViewer?.Refresh(_state.GetDeckSnapshot());
        }

        private void EnterNode(int index)
        {
            if (index < 0 || index >= _map.Nodes.Count)
            {
                return;
            }

            if (_state.IsNodeCompleted(index))
            {
                return;
            }

            if (!_state.IsNodeAvailable(index))
            {
                return;
            }

            _state.CurrentNodeId = index;
            _resolveNodeId = index;
            MapNode node = _map.GetNode(index);
            if (node != null && (node.Type == NodeType.Combat || node.Type == NodeType.Elite || node.Type == NodeType.Boss))
            {
                StartCombatForNode(index);
                return;
            }

            if (node != null && node.Type == NodeType.Campfire)
            {
                ShowCampfirePanel(index);
                return;
            }

            if (node != null && node.Type == NodeType.Shop)
            {
                ShowShopPanel(index);
                return;
            }

            if (node != null && node.Type == NodeType.Event && node.AssignedEvent != null && _eventController != null)
            {
                ShowEventPanel(index, node.AssignedEvent);
                return;
            }

            ShowResolvePanel(index);
        }

        private void ShowCampfirePanel(int index)
        {
            _mapPanel.gameObject.SetActive(false);
            _resolvePanel.gameObject.SetActive(false);
            _rewardPanel.gameObject.SetActive(false);
            _actoCompletedPanel.gameObject.SetActive(false);
            _defeatPanel.gameObject.SetActive(false);
            if (_campfireController != null)
            {
                _campfireController.Show(index);
            }
        }

        private void OnCampfireComplete(int nodeId)
        {
            CompleteNode(nodeId);
            _state.CurrentNodeId = -1;
            ShowMap();
        }

        private void ShowShopPanel(int index)
        {
            _mapPanel.gameObject.SetActive(false);
            _resolvePanel.gameObject.SetActive(false);
            _rewardPanel.gameObject.SetActive(false);
            _actoCompletedPanel.gameObject.SetActive(false);
            _defeatPanel.gameObject.SetActive(false);
            if (_shopController != null)
            {
                _shopController.Show(index);
            }
        }

        private void OnShopComplete(int nodeId)
        {
            // Incrementar al COMPLETAR (al salir): el precio escalante de
            // "eliminar carta" usa ShopsCompleted como tiendas previas, así que
            // se incrementa después de armar el stock de esta tienda.
            _state.ShopsCompleted++;
            CompleteNode(nodeId);
            _state.CurrentNodeId = -1;
            ShowMap();
        }

        private void ShowEventPanel(int index, EventDefinition definition)
        {
            _mapPanel.gameObject.SetActive(false);
            _resolvePanel.gameObject.SetActive(false);
            _rewardPanel.gameObject.SetActive(false);
            _actoCompletedPanel.gameObject.SetActive(false);
            _defeatPanel.gameObject.SetActive(false);
            _eventController.Show(index, definition);
        }

        private void OnEventComplete(int nodeId)
        {
            // Las consecuencias de la decisión ya mutaron RunState (oro/HP/mazo/
            // Retazos); el nodo Event NO otorga el +10 oro genérico del placeholder.
            CompleteNode(nodeId);
            _state.CurrentNodeId = -1;
            ShowMap();
        }

        private void OnContinue()
        {
            if (_state.CurrentNodeId < 0 || _state.CurrentNodeId >= _map.Nodes.Count)
            {
                ShowMap();
                return;
            }

            CompleteNode(_state.CurrentNodeId);
            _state.CurrentNodeId = -1;
            _state.Gold += 10;
            ShowMap();
        }

        private void StartCombatForNode(int nodeId)
        {
            _state.CurrentNodeId = nodeId;
            _state.PendingReturnFromBattle = false;
            _state.LastNodeOutcome = RunState.NodeOutcome.None;
            if (!IsSceneInBuild(BattleSceneName))
            {
#if UNITY_EDITOR
                Debug.LogError("[RunFlow] BattleScene no está en Build Settings.");
#endif
                return;
            }
            _mapView?.Cleanup();
            _relicView?.Cleanup();
            _deckViewer?.Cleanup();
            SceneTransitionManager.LoadScene(BattleSceneName);
        }

        private void ShowResolvePanel(int nodeId)
        {
            _mapPanel.gameObject.SetActive(false);
            _resolvePanel.gameObject.SetActive(true);
            _resolvePanel.SetAsLastSibling();

            MapNode node = _map.GetNode(nodeId);
            if (node != null)
            {
                _resolveTitleText.text = $"Nodo {node.Id + 1}";
                _resolveBodyText.text = $"Tipo: {node.Type}\n\nContenido placeholder.";
            }
        }

        private void OnResolveComplete()
        {
            if (_resolveNodeId < 0)
            {
                ShowMap();
                return;
            }

            CompleteNode(_resolveNodeId);
            _state.CurrentNodeId = -1;
            _resolveNodeId = -1;
            _state.Gold += 10;
            ShowMap();
        }

        private void OnResolveBack()
        {
            _resolveNodeId = -1;
            _state.CurrentNodeId = -1;
            ShowMap();
        }

        private void HandlePendingBattleResult()
        {
            if (!_state.PendingReturnFromBattle)
            {
                return;
            }

            // Chequear si el acto fue completado (victoria contra boss)
            if (_state.ActoCompleted)
            {
                _state.PendingReturnFromBattle = false;
                ShowActoCompletedPanel();
                return;
            }

            if (_state.LastNodeOutcome == RunState.NodeOutcome.Victory)
            {
                _state.PendingReturnFromBattle = false;
                _state.LastNodeOutcome = RunState.NodeOutcome.None;
                if (!TryShowRewardPanel())
                {
                    if (_state.CurrentNodeId >= 0)
                    {
                        CompleteNode(_state.CurrentNodeId);
                    }
                    if (runCombatConfig != null)
                    {
                        _state.Gold += runCombatConfig.GoldReward;
                    }
                    _state.CurrentNodeId = -1;
                }
                return;
            }

            if (_state.LastNodeOutcome == RunState.NodeOutcome.Defeat)
            {
                _state.PendingReturnFromBattle = false;
            }
        }

        private void CompleteNode(int nodeId)
        {
            if (_state.IsNodeCompleted(nodeId))
            {
                return;
            }

            _state.CompletedNodes.Add(nodeId);
            _state.AvailableNodes.Clear();
            _state.CurrentPositionNodeId = nodeId;

            MapNode node = _map.GetNode(nodeId);
            if (node == null)
            {
                return;
            }

            foreach (int connection in node.Connections)
            {
                if (!_state.IsNodeCompleted(connection))
                {
                    _state.AvailableNodes.Add(connection);
                }
            }

            if (_state.AvailableNodes.Count == 0 && _state.CompletedNodes.Count < _map.Nodes.Count)
            {
                Debug.LogWarning("No available nodes remaining. Check map connections.");
            }
        }

        private void ShowDefeatPanel()
        {
            _mapPanel.gameObject.SetActive(false);
            _resolvePanel.gameObject.SetActive(false);
            _rewardPanel.gameObject.SetActive(false);
            _actoCompletedPanel.gameObject.SetActive(false);
            _defeatPanel.gameObject.SetActive(true);
            _defeatPanel.SetAsLastSibling();
        }

        private void ShowActoCompletedPanel()
        {
            // Overlay modal: ocultamos el mapa y bloqueamos raycasts para que no haya input detrás.
            _mapPanel.gameObject.SetActive(false);
            _resolvePanel.gameObject.SetActive(false);
            _rewardPanel.gameObject.SetActive(false);
            _defeatPanel.gameObject.SetActive(false);
            _actoCompletedPanel.gameObject.SetActive(true);
            _actoCompletedPanel.SetAsLastSibling();
        }

        private void PrintMapDebugOnce()
        {
            if (_map == null)
            {
                return;
            }

            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.AppendLine("[ActMap Debug] Nodes:");
            foreach (MapNode node in _map.Nodes)
            {
                string connections = node.Connections.Count > 0
                    ? string.Join(", ", node.Connections)
                    : "none";
                sb.AppendLine($"- {node.Id}: {node.Type} -> [{connections}]");
            }
            Debug.Log(sb.ToString());
        }

        private Canvas CreateCanvas(string name)
        {
            GameObject canvasGO = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGO.transform.SetParent(transform, false);

            Canvas canvas = canvasGO.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = canvasGO.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            return canvas;
        }

        private Text CreateText(string name, RectTransform parent, string text, int fontSize, TextAnchor alignment)
        {
            GameObject textGO = new GameObject(name, typeof(RectTransform), typeof(Text));
            textGO.transform.SetParent(parent, false);
            RectTransform rect = textGO.GetComponent<RectTransform>();
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Text label = textGO.GetComponent<Text>();
            label.font = uiFont;
            label.fontSize = fontSize;
            label.alignment = alignment;
            label.color = Color.white;
            label.text = text;
            return label;
        }

        private Button CreateButton(string name, RectTransform parent, string label)
        {
            GameObject buttonGO = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            buttonGO.transform.SetParent(parent, false);
            RectTransform rect = buttonGO.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.35f, 0.2f);
            rect.anchorMax = new Vector2(0.65f, 0.28f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Image image = buttonGO.GetComponent<Image>();
            image.sprite = GetWhiteSprite();
            image.type = Image.Type.Simple;
            image.color = new Color(0.15f, 0.15f, 0.2f, 0.9f);

            GameObject textGO = new GameObject("Label", typeof(RectTransform), typeof(Text));
            textGO.transform.SetParent(buttonGO.transform, false);
            RectTransform textRect = textGO.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            Text text = textGO.GetComponent<Text>();
            text.font = uiFont;
            text.fontSize = 20;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.text = label;

            return buttonGO.GetComponent<Button>();
        }

        private RectTransform CreatePanel(string name, RectTransform parent, Color color, bool blockRaycasts)
        {
            GameObject panelGO = new GameObject(name, typeof(RectTransform), typeof(Image));
            panelGO.transform.SetParent(parent, false);
            RectTransform rect = panelGO.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Image image = panelGO.GetComponent<Image>();
            image.sprite = GetWhiteSprite();
            image.type = Image.Type.Simple;
            image.color = color;
            image.raycastTarget = blockRaycasts;

            return rect;
        }

        private void BuildResolvePanel()
        {
            _resolveTitleText = CreateText("ResolveTitle", _resolvePanel, "Nodo", 26, TextAnchor.UpperCenter);
            RectTransform titleRect = _resolveTitleText.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.2f, 0.7f);
            titleRect.anchorMax = new Vector2(0.8f, 0.9f);

            _resolveBodyText = CreateText("ResolveBody", _resolvePanel, "Tipo:\n\nContenido placeholder.", 20, TextAnchor.UpperCenter);
            RectTransform bodyRect = _resolveBodyText.GetComponent<RectTransform>();
            bodyRect.anchorMin = new Vector2(0.2f, 0.45f);
            bodyRect.anchorMax = new Vector2(0.8f, 0.7f);

            _resolveCompleteButton = CreateButton("ResolveComplete", _resolvePanel, "Completar");
            RectTransform completeRect = _resolveCompleteButton.GetComponent<RectTransform>();
            completeRect.anchorMin = new Vector2(0.3f, 0.25f);
            completeRect.anchorMax = new Vector2(0.7f, 0.33f);
            _resolveCompleteButton.onClick.AddListener(OnResolveComplete);

            _resolveBackButton = CreateButton("ResolveBack", _resolvePanel, "Volver");
            RectTransform backRect = _resolveBackButton.GetComponent<RectTransform>();
            backRect.anchorMin = new Vector2(0.3f, 0.15f);
            backRect.anchorMax = new Vector2(0.7f, 0.23f);
            _resolveBackButton.onClick.AddListener(OnResolveBack);
        }

        private void BuildDefeatPanel()
        {
            Text defeatTitle = CreateText("DefeatTitle", _defeatPanel, "Derrota", 28, TextAnchor.UpperCenter);
            RectTransform titleRect = defeatTitle.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.2f, 0.65f);
            titleRect.anchorMax = new Vector2(0.8f, 0.8f);

            Text defeatBody = CreateText("DefeatBody", _defeatPanel, "La run ha terminado.", 20, TextAnchor.UpperCenter);
            RectTransform bodyRect = defeatBody.GetComponent<RectTransform>();
            bodyRect.anchorMin = new Vector2(0.2f, 0.5f);
            bodyRect.anchorMax = new Vector2(0.8f, 0.65f);

            _defeatRetryButton = CreateButton("DefeatRetry", _defeatPanel, "Reintentar");
            RectTransform retryRect = _defeatRetryButton.GetComponent<RectTransform>();
            retryRect.anchorMin = new Vector2(0.3f, 0.35f);
            retryRect.anchorMax = new Vector2(0.7f, 0.43f);
            _defeatRetryButton.onClick.AddListener(() =>
            {
                // Retry tras derrota: restaura HP a full y limpia flags de outcome
                // (H3). Sin esto el combate recargaba con PlayerCurrentHP = 0, que
                // InitializeCombat clampa a 1 HP → loop de derrota.
                _state.PrepareForRetry();
                _mapView?.Cleanup();
                _relicView?.Cleanup();
                _deckViewer?.Cleanup();
                SceneTransitionManager.LoadScene(BattleSceneName);
            });

            _defeatExitButton = CreateButton("DefeatExit", _defeatPanel, "Volver al mapa");
            RectTransform exitRect = _defeatExitButton.GetComponent<RectTransform>();
            exitRect.anchorMin = new Vector2(0.3f, 0.25f);
            exitRect.anchorMax = new Vector2(0.7f, 0.33f);
            _defeatExitButton.onClick.AddListener(ReturnToMainMenu);
        }

        /// <summary>
        /// Abandono de run (botón "Volver al mapa" de derrota / "Volver al Menú" de
        /// acto completado): vuelve al menú principal en vez de resetear in-place. El
        /// reset de run nuevo lo posee MainMenu→Play (RunSession.ResetForNewRun) →
        /// NewRunScene, donde el jugador re-elige tipos y draftea. El reset in-place
        /// previo dejaba la run degradada (mapa viejo, tipos a default, sin starter
        /// drafteado). Ver spec fix_combat_end_hp_sync (H4).
        /// </summary>
        private void ReturnToMainMenu()
        {
            if (!IsSceneInBuild(MainMenuSceneName))
            {
#if UNITY_EDITOR
                Debug.LogError("[RunFlow] MainMenuScene no está en Build Settings.");
#endif
                return;
            }
            _mapView?.Cleanup();
            _relicView?.Cleanup();
            _deckViewer?.Cleanup();
            SceneTransitionManager.LoadScene(MainMenuSceneName);
        }

        private void BuildActoCompletedPanel()
        {
            Text actoTitle = CreateText("ActoTitle", _actoCompletedPanel, "¡Acto Completado!", 32, TextAnchor.UpperCenter);
            RectTransform titleRect = actoTitle.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.1f, 0.65f);
            titleRect.anchorMax = new Vector2(0.9f, 0.8f);

            Text actoBody = CreateText("ActoBody", _actoCompletedPanel, "Has derrotado al boss del Acto 1.\n¡Felicidades!", 20, TextAnchor.UpperCenter);
            RectTransform bodyRect = actoBody.GetComponent<RectTransform>();
            bodyRect.anchorMin = new Vector2(0.1f, 0.45f);
            bodyRect.anchorMax = new Vector2(0.9f, 0.65f);

            Button menuButton = CreateButton("MenuButton", _actoCompletedPanel, "Volver al Menú");
            RectTransform menuRect = menuButton.GetComponent<RectTransform>();
            menuRect.anchorMin = new Vector2(0.3f, 0.25f);
            menuRect.anchorMax = new Vector2(0.7f, 0.35f);
            menuButton.onClick.AddListener(() =>
            {
#if UNITY_EDITOR
                Debug.Log("[RunFlow] Volviendo al menú principal desde Acto Completado");
#endif
                ReturnToMainMenu();
            });
        }

        private void BuildRewardPanel()
        {
            _rewardTitleText = CreateText("RewardTitle", _rewardPanel, "Recompensa", 26, TextAnchor.UpperCenter);
            RectTransform titleRect = _rewardTitleText.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.2f, 0.78f);
            titleRect.anchorMax = new Vector2(0.8f, 0.92f);

            _rewardGoldText = CreateText("RewardGold", _rewardPanel, "+0 oro", 20, TextAnchor.UpperCenter);
            RectTransform goldRect = _rewardGoldText.GetComponent<RectTransform>();
            goldRect.anchorMin = new Vector2(0.2f, 0.68f);
            goldRect.anchorMax = new Vector2(0.8f, 0.78f);

            for (int i = 0; i < 3; i++)
            {
                Button button = CreateButton($"Reward_{i}", _rewardPanel, "Carta");
                RectTransform rect = button.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0.2f, 0.55f - i * 0.12f);
                rect.anchorMax = new Vector2(0.8f, 0.62f - i * 0.12f);
                int choiceIndex = i;
                button.onClick.AddListener(() => OnRewardSelected(choiceIndex));
                _rewardButtons.Add(button);
            }
        }

        private bool TryShowRewardPanel()
        {
            if (_rewardPanel == null)
            {
                return false;
            }

            RunCombatConfig config = GetCombatConfig();
            if (config == null)
            {
#if UNITY_EDITOR
                if (!_rewardConfigErrorLogged)
                {
                    Debug.LogError("[RunFlow] RunCombatConfig no asignado. Recompensa omitida.");
                    _rewardConfigErrorLogged = true;
                }
#endif
                return false;
            }

            if (config.RewardPool == null || config.RewardPool.Count == 0)
            {
#if UNITY_EDITOR
                if (!_rewardConfigErrorLogged)
                {
                    Debug.LogError("[RunFlow] RewardPool vacío. Recompensa omitida.");
                    _rewardConfigErrorLogged = true;
                }
#endif
                return false;
            }

            _rewardNodeId = _state.CurrentNodeId;
            _rewardGoldText.text = $"+{config.GoldReward} oro";

            _rewardOptions.Clear();
            _rewardOptions.AddRange(GetRewardOptions(config));
            if (_rewardOptions.Count == 0)
            {
#if UNITY_EDITOR
                if (!_rewardConfigErrorLogged)
                {
                    Debug.LogError("[RunFlow] RewardPool sin entradas válidas. Recompensa omitida.");
                    _rewardConfigErrorLogged = true;
                }
#endif
                return false;
            }
            for (int i = 0; i < _rewardButtons.Count; i++)
            {
                Button button = _rewardButtons[i];
                bool active = i < _rewardOptions.Count;
                button.gameObject.SetActive(active);
                if (!active)
                {
                    continue;
                }

                CardDeckEntry entry = _rewardOptions[i];
                CardDefinition card = entry != null ? entry.GetActiveCard(TurnManager.WorldSide.A) : null;
                Text label = button.GetComponentInChildren<Text>();
                if (label != null)
                {
                    label.text = card != null ? card.CardName : "Carta";
                }
            }

            _mapPanel.gameObject.SetActive(false);
            _resolvePanel.gameObject.SetActive(false);
            _defeatPanel.gameObject.SetActive(false);
            _rewardPanel.gameObject.SetActive(true);
            _rewardPanel.SetAsLastSibling();
            return true;
        }

        private List<CardDeckEntry> GetRewardOptions(RunCombatConfig config)
        {
            int count = Mathf.Min(config.ChoicesCount, config.RewardPool.Count);
            var options = new List<CardDeckEntry>();
            for (int i = 0; i < count; i++)
            {
                CardDeckEntry entry = config.RewardPool[i];
                if (entry != null && entry.IsValid)
                {
                    options.Add(entry);
                }
            }

            return options;
        }

        private void OnRewardSelected(int index)
        {
            if (index < 0 || index >= _rewardOptions.Count)
            {
                return;
            }

            CardDeckEntry chosen = _rewardOptions[index];
            _state.AddCardToDeck(chosen);
            RunCombatConfig config = GetCombatConfig();
            if (config != null)
            {
                _state.Gold += config.GoldReward;
            }

            if (_rewardNodeId >= 0)
            {
                CompleteNode(_rewardNodeId);
            }

            _rewardNodeId = -1;
            _state.CurrentNodeId = -1;
            ShowMap();
        }

        private RunCombatConfig GetCombatConfig()
        {
            if (runCombatConfig != null)
            {
                return runCombatConfig;
            }

            return RunSession.GetOrCreate().CombatConfig;
        }

        private bool IsSceneInBuild(string sceneName)
        {
            int total = SceneManager.sceneCountInBuildSettings;
            for (int i = 0; i < total; i++)
            {
                string path = SceneUtility.GetScenePathByBuildIndex(i);
                if (path.EndsWith($"/{sceneName}.unity"))
                {
                    return true;
                }
            }

            return false;
        }

        private static Sprite GetWhiteSprite()
        {
            if (_whiteSprite != null)
            {
                return _whiteSprite;
            }

            _whiteSprite = Sprite.Create(
                Texture2D.whiteTexture,
                new Rect(0, 0, 1, 1),
                new Vector2(0.5f, 0.5f));
            return _whiteSprite;
        }
    }
}
