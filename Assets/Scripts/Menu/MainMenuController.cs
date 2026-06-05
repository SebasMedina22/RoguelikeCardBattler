using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using DG.Tweening;
using RoguelikeCardBattler.Core;
using RoguelikeCardBattler.Core.Audio;
using RoguelikeCardBattler.Core.UI;
using RoguelikeCardBattler.Run;

namespace RoguelikeCardBattler.Menu
{
    /// <summary>
    /// Controlador scene-owned del Main Menu. Solo navega entre escenas.
    /// </summary>
    public class MainMenuController : MonoBehaviour
    {
        private const string SceneNewRun = "NewRunScene";
        private const string SceneRun = "RunScene";
        private const string SceneBattle = "BattleScene";
        private const string ContinuePlaceholder = "Coming in E5-P03";

        [SerializeField] private Font uiFont;

        private Canvas _canvas;
        private Text _messageText;
        private Button _playButton;
        private Button _continueButton;
        private Button _settingsButton;
        private Button _quitButton;
        private RectTransform _mainMenuPanel;
        private RectTransform _settingsPanel;
        private Slider _masterVolumeSlider;
        private Text _masterVolumeValueText;
        private Slider _musicVolumeSlider;
        private Text _musicVolumeValueText;
        private Text _resolutionValueText;
        private Text _screenModeValueText;
        private int _resolutionIndex;
        private int _screenModeIndex;
        private bool _scenesValid;

        // ── Juice: CanvasGroups for entry animations ──
        private CanvasGroup _backgroundGroup;
        private CanvasGroup _titleGroup;
        private CanvasGroup _playButtonGroup;
        private CanvasGroup _continueButtonGroup;
        private CanvasGroup _settingsButtonGroup;
        private CanvasGroup _quitButtonGroup;
        private RectTransform _titleRect;
        private Sequence _introSequence;

        private readonly string[] _resolutionOptions = { "1920x1080", "1280x720" };
        private readonly string[] _screenModeOptions = { "Pantalla completa", "Ventana" };
        private const int ResolutionDefaultIndex = 0;
        private const int ScreenModeDefaultIndex = 0;

        private void Awake()
        {
            if (uiFont == null)
            {
                uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            }

            EnsureEventSystem();
            BuildUI();
            _scenesValid = ValidateScenesInBuildSettings();
            ApplyValidationState();
        }

        /// <summary>
        /// Play inicia una nueva run y carga NewRunScene (Sub-PR 3E): la pantalla
        /// de selección de tipos + draft de carta dual previa a RunScene. Sigue
        /// reseteando RunState aquí; NewRunScene sólo lo muta al confirmar.
        /// </summary>
        public void OnPlayClicked()
        {
            AudioManager.Instance?.PlaySFX(AudioManager.Instance.ClickSFX);
            if (!_scenesValid)
            {
                Debug.LogError("NewRunScene/RunScene/BattleScene not in Build Settings. Add them in File > Build Settings.");
                return;
            }

            RunSession session = RunSession.GetOrCreate();
            session.ResetForNewRun();
            SceneTransitionManager.LoadScene(SceneNewRun);
        }

        /// <summary>
        /// Continue placeholder (sin funcionalidad en esta fase).
        /// </summary>
        public void OnContinueClicked()
        {
            AudioManager.Instance?.PlaySFX(AudioManager.Instance.ClickSFX);
            SetMessage(ContinuePlaceholder);
        }

        /// <summary>
        /// Settings placeholder visual.
        /// </summary>
        public void OnSettingsClicked()
        {
            AudioManager.Instance?.PlaySFX(AudioManager.Instance.ClickSFX);
            ShowSettingsPanel();
        }

        /// <summary>
        /// Quit cierra la aplicación en build.
        /// </summary>
        public void OnQuitClicked()
        {
            AudioManager.Instance?.PlaySFX(AudioManager.Instance.ClickSFX);
#if UNITY_EDITOR
            Debug.Log("[MainMenu] Quit requested (no-op in editor).");
#endif
            Application.Quit();
        }

        /// <summary>
        /// Vuelve al panel principal desde Settings.
        /// </summary>
        public void OnBackFromSettingsClicked()
        {
            ShowMainMenuPanel();
        }

        private bool ValidateScenesInBuildSettings()
        {
            bool newRunExists = Application.CanStreamedLevelBeLoaded(SceneNewRun);
            bool runExists = Application.CanStreamedLevelBeLoaded(SceneRun);
            bool battleExists = Application.CanStreamedLevelBeLoaded(SceneBattle);
            if (!newRunExists || !runExists || !battleExists)
            {
                Debug.LogError("NewRunScene/RunScene/BattleScene not in Build Settings. Add them in File > Build Settings.");
                return false;
            }

            return true;
        }

        private void ApplyValidationState()
        {
            if (_playButton == null)
            {
                return;
            }

            if (!_scenesValid)
            {
                _playButton.interactable = false;
                SetMessage("Missing NewRunScene/RunScene/BattleScene in Build Settings.");
            }
        }

        private void SetMessage(string message)
        {
            if (_messageText != null)
            {
                _messageText.text = message;
            }
        }

        private void EnsureEventSystem()
        {
            EventSystem existing = UnityEngine.Object.FindFirstObjectByType<EventSystem>();
            if (existing != null)
            {
                return;
            }

            GameObject eventSystemObject = new GameObject("EventSystem", typeof(EventSystem));
            Type inputSystemType = Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
            if (inputSystemType != null)
            {
                eventSystemObject.AddComponent(inputSystemType);
            }
            else
            {
                eventSystemObject.AddComponent<StandaloneInputModule>();
            }
        }

        private void BuildUI()
        {
            _canvas = CreateCanvas("MainMenuCanvas");
            RectTransform root = _canvas.GetComponent<RectTransform>();

            RectTransform background = CreatePanel("Background", root, new Color(0.08f, 0.08f, 0.1f, 1f));
            background.anchorMin = Vector2.zero;
            background.anchorMax = Vector2.one;
            background.offsetMin = Vector2.zero;
            background.offsetMax = Vector2.zero;

            Text title = CreateText("Title", background, "Roguelike Card Battler", 40, TextAnchor.UpperCenter);
            RectTransform titleRect = title.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.1f, 0.85f);
            titleRect.anchorMax = new Vector2(0.9f, 0.98f);

            _messageText = CreateText("Message", background, string.Empty, 20, TextAnchor.LowerCenter);
            RectTransform messageRect = _messageText.GetComponent<RectTransform>();
            messageRect.anchorMin = new Vector2(0.1f, 0.05f);
            messageRect.anchorMax = new Vector2(0.9f, 0.15f);

            _mainMenuPanel = CreatePanel("MainMenuPanel", background, new Color(0f, 0f, 0f, 0f));
            _mainMenuPanel.anchorMin = new Vector2(0.2f, 0.2f);
            _mainMenuPanel.anchorMax = new Vector2(0.8f, 0.78f);
            _mainMenuPanel.offsetMin = Vector2.zero;
            _mainMenuPanel.offsetMax = Vector2.zero;

            _settingsPanel = CreatePanel("SettingsPanel", background, new Color(0f, 0f, 0f, 0f));
            _settingsPanel.anchorMin = new Vector2(0.15f, 0.15f);
            _settingsPanel.anchorMax = new Vector2(0.85f, 0.8f);
            _settingsPanel.offsetMin = Vector2.zero;
            _settingsPanel.offsetMax = Vector2.zero;

            _playButton = CreateButton("PlayButton", _mainMenuPanel, "Play", new Vector2(0.5f, 0.68f));
            _continueButton = CreateButton("ContinueButton", _mainMenuPanel, "Continue", new Vector2(0.5f, 0.5f));
            _settingsButton = CreateButton("SettingsButton", _mainMenuPanel, "Settings", new Vector2(0.5f, 0.32f));
            _quitButton = CreateButton("QuitButton", _mainMenuPanel, "Quit", new Vector2(0.5f, 0.14f));

            _playButton.onClick.AddListener(OnPlayClicked);
            _continueButton.onClick.AddListener(OnContinueClicked);
            _settingsButton.onClick.AddListener(OnSettingsClicked);
            _quitButton.onClick.AddListener(OnQuitClicked);

            _continueButton.interactable = false;

            BuildSettingsPanel();
            ShowMainMenuPanel();

            // ── Hover effects on main menu buttons ──
            AddHoverEffect(_playButton);
            AddHoverEffect(_continueButton);
            AddHoverEffect(_settingsButton);
            AddHoverEffect(_quitButton);

            // ── Entry animation sequence ──
            PlayIntroSequence(background, title, titleRect);
        }

        private void BuildSettingsPanel()
        {
            Text settingsTitle = CreateText("SettingsTitle", _settingsPanel, "Settings", 32, TextAnchor.UpperCenter);
            RectTransform titleRect = settingsTitle.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.1f, 0.82f);
            titleRect.anchorMax = new Vector2(0.9f, 0.98f);

            _masterVolumeSlider = CreateLabeledSlider("MasterVolume", _settingsPanel, "Volumen general", new Vector2(0.2f, 0.65f), out _masterVolumeValueText);
            _musicVolumeSlider = CreateLabeledSlider("MusicVolume", _settingsPanel, "Música", new Vector2(0.2f, 0.5f), out _musicVolumeValueText);

            CreateOptionSelector("Resolution", _settingsPanel, "Resolución", new Vector2(0.2f, 0.35f), _resolutionOptions, out _resolutionValueText,
                OnResolutionPrevClicked, OnResolutionNextClicked);
            CreateOptionSelector("ScreenMode", _settingsPanel, "Modo de pantalla", new Vector2(0.2f, 0.2f), _screenModeOptions, out _screenModeValueText,
                OnScreenModePrevClicked, OnScreenModeNextClicked);

            Button backButton = CreateButton("BackButton", _settingsPanel, "Back", new Vector2(0.5f, 0.06f));
            backButton.onClick.AddListener(OnBackFromSettingsClicked);

            _masterVolumeSlider.onValueChanged.AddListener(OnMasterVolumeChanged);
            _musicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeChanged);

            _masterVolumeSlider.value = 80f;
            _musicVolumeSlider.value = 80f;
            _resolutionIndex = ResolutionDefaultIndex;
            _screenModeIndex = ScreenModeDefaultIndex;
            UpdateVolumeLabel(_masterVolumeValueText, _masterVolumeSlider.value);
            UpdateVolumeLabel(_musicVolumeValueText, _musicVolumeSlider.value);
            UpdateOptionLabel(_resolutionValueText, _resolutionOptions, _resolutionIndex);
            UpdateOptionLabel(_screenModeValueText, _screenModeOptions, _screenModeIndex);

            ApplyDisplaySettings();
        }

        private void OnMasterVolumeChanged(float value)
        {
            UpdateVolumeLabel(_masterVolumeValueText, value);
            AudioListener.volume = Mathf.Clamp01(value / 100f);
        }

        private void OnMusicVolumeChanged(float value)
        {
            UpdateVolumeLabel(_musicVolumeValueText, value);
            SetMessage("Música: placeholder (sin sistema de audio aún)");
        }

        private void ApplyDisplaySettings()
        {
            int width = 1920;
            int height = 1080;
            if (_resolutionIndex == 1)
            {
                width = 1280;
                height = 720;
            }

            bool fullscreen = _screenModeIndex == 0;
            Screen.SetResolution(width, height, fullscreen);
        }

        private void OnResolutionPrevClicked()
        {
            _resolutionIndex = Mathf.Clamp(_resolutionIndex - 1, 0, _resolutionOptions.Length - 1);
            UpdateOptionLabel(_resolutionValueText, _resolutionOptions, _resolutionIndex);
            ApplyDisplaySettings();
        }

        private void OnResolutionNextClicked()
        {
            _resolutionIndex = Mathf.Clamp(_resolutionIndex + 1, 0, _resolutionOptions.Length - 1);
            UpdateOptionLabel(_resolutionValueText, _resolutionOptions, _resolutionIndex);
            ApplyDisplaySettings();
        }

        private void OnScreenModePrevClicked()
        {
            _screenModeIndex = Mathf.Clamp(_screenModeIndex - 1, 0, _screenModeOptions.Length - 1);
            UpdateOptionLabel(_screenModeValueText, _screenModeOptions, _screenModeIndex);
            ApplyDisplaySettings();
        }

        private void OnScreenModeNextClicked()
        {
            _screenModeIndex = Mathf.Clamp(_screenModeIndex + 1, 0, _screenModeOptions.Length - 1);
            UpdateOptionLabel(_screenModeValueText, _screenModeOptions, _screenModeIndex);
            ApplyDisplaySettings();
        }

        private void ShowSettingsPanel()
        {
            if (_mainMenuPanel != null)
            {
                _mainMenuPanel.gameObject.SetActive(false);
            }

            if (_settingsPanel != null)
            {
                _settingsPanel.gameObject.SetActive(true);
            }
        }

        private void ShowMainMenuPanel()
        {
            if (_settingsPanel != null)
            {
                _settingsPanel.gameObject.SetActive(false);
            }

            if (_mainMenuPanel != null)
            {
                _mainMenuPanel.gameObject.SetActive(true);
            }
        }

        // ────────────────────────────────────────────────────────
        // Juice: entry animation, hover feedback, title bob loop
        // ────────────────────────────────────────────────────────

        /// <summary>
        /// Orchestrates the staggered entry animation: background fade → title scale+fade → buttons slide in.
        /// Called once at the end of <see cref="BuildUI"/>.
        /// </summary>
        private void PlayIntroSequence(RectTransform background, Text title, RectTransform titleRect)
        {
            _titleRect = titleRect;

            // Prepare CanvasGroups — elements start invisible
            _backgroundGroup = background.gameObject.AddComponent<CanvasGroup>();
            _backgroundGroup.alpha = 0f;

            _titleGroup = title.gameObject.AddComponent<CanvasGroup>();
            _titleGroup.alpha = 0f;
            titleRect.localScale = Vector3.zero;

            _playButtonGroup = _playButton.gameObject.AddComponent<CanvasGroup>();
            _playButtonGroup.alpha = 0f;
            _continueButtonGroup = _continueButton.gameObject.AddComponent<CanvasGroup>();
            _continueButtonGroup.alpha = 0f;
            _settingsButtonGroup = _settingsButton.gameObject.AddComponent<CanvasGroup>();
            _settingsButtonGroup.alpha = 0f;
            _quitButtonGroup = _quitButton.gameObject.AddComponent<CanvasGroup>();
            _quitButtonGroup.alpha = 0f;

            CanvasGroup[] buttonGroups = { _playButtonGroup, _continueButtonGroup, _settingsButtonGroup, _quitButtonGroup };
            RectTransform[] buttonRects =
            {
                _playButton.GetComponent<RectTransform>(),
                _continueButton.GetComponent<RectTransform>(),
                _settingsButton.GetComponent<RectTransform>(),
                _quitButton.GetComponent<RectTransform>()
            };

            // Build DOTween Sequence
            _introSequence = DOTween.Sequence().SetUpdate(true);

            // 1) Background fade in (0.3s)
            _introSequence.Append(UIAnimationHelper.FadeIn(_backgroundGroup, 0.3f));

            // 2) Title fade + scale in parallel (after 0.2s delay from background start → total offset 0.2s into sequence)
            _introSequence.AppendInterval(0.2f);
            _introSequence.Append(UIAnimationHelper.FadeIn(_titleGroup, 0.2f));
            _introSequence.Join(UIAnimationHelper.ScaleIn(titleRect.transform, 0.2f));

            // 3) Buttons staggered: 0.1s apart, each with fade + slide in parallel
            for (int i = 0; i < buttonGroups.Length; i++)
            {
                if (i > 0)
                {
                    _introSequence.AppendInterval(0.1f);
                }
                _introSequence.Append(UIAnimationHelper.FadeIn(buttonGroups[i], 0.25f));
                _introSequence.Join(UIAnimationHelper.SlideIn(buttonRects[i], new Vector2(0, -60f), 0.25f));
            }

            // After intro completes, start the infinite title bob
            _introSequence.OnComplete(() => StartTitleBob(titleRect));
        }

        /// <summary>
        /// Adds pointer-enter/exit hover scale feedback to a button using EventTrigger.
        /// Scales to 1.05x on hover, back to 1.0x on exit.
        /// </summary>
        private void AddHoverEffect(Button button)
        {
            RectTransform target = button.GetComponent<RectTransform>();
            EventTrigger trigger = button.gameObject.AddComponent<EventTrigger>();

            // PointerEnter → scale up to 1.05
            EventTrigger.Entry enterEntry = new EventTrigger.Entry();
            enterEntry.eventID = EventTriggerType.PointerEnter;
            enterEntry.callback.AddListener(_ =>
            {
                DOTween.Kill(target);
                DOTween.To(() => target.localScale, x => target.localScale = x,
                    new Vector3(1.05f, 1.05f, 1.05f), 0.1f)
                    .SetTarget(target)
                    .SetEase(Ease.OutQuad)
                    .SetUpdate(true);
            });
            trigger.triggers.Add(enterEntry);

            // PointerExit → scale back to 1.0
            EventTrigger.Entry exitEntry = new EventTrigger.Entry();
            exitEntry.eventID = EventTriggerType.PointerExit;
            exitEntry.callback.AddListener(_ =>
            {
                DOTween.Kill(target);
                DOTween.To(() => target.localScale, x => target.localScale = x,
                    Vector3.one, 0.1f)
                    .SetTarget(target)
                    .SetEase(Ease.OutQuad)
                    .SetUpdate(true);
            });
            trigger.triggers.Add(exitEntry);
        }

        /// <summary>
        /// Starts an infinite vertical bob on the title (±3px, 2s full cycle, InOutSine yoyo).
        /// Runs unscaled so it works even at timeScale 0.
        /// </summary>
        private void StartTitleBob(RectTransform titleRect)
        {
            Vector2 restPosition = titleRect.anchoredPosition;
            DOTween.To(
                    () => titleRect.anchoredPosition,
                    x => titleRect.anchoredPosition = x,
                    new Vector2(restPosition.x, restPosition.y + 3f),
                    1f) // 1s per half-cycle = 2s full cycle
                .SetTarget(titleRect)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo)
                .SetUpdate(true);
        }

        /// <summary>
        /// Cleanup: kill all DOTween tweens owned by this controller's UI elements.
        /// </summary>
        private void OnDestroy()
        {
            _introSequence?.Kill();
            if (_titleRect != null) DOTween.Kill(_titleRect);
            if (_backgroundGroup != null) DOTween.Kill(_backgroundGroup);
            if (_titleGroup != null) DOTween.Kill(_titleGroup);
            if (_playButton != null) DOTween.Kill(_playButton.GetComponent<RectTransform>());
            if (_continueButton != null) DOTween.Kill(_continueButton.GetComponent<RectTransform>());
            if (_settingsButton != null) DOTween.Kill(_settingsButton.GetComponent<RectTransform>());
            if (_quitButton != null) DOTween.Kill(_quitButton.GetComponent<RectTransform>());
            if (_playButtonGroup != null) DOTween.Kill(_playButtonGroup);
            if (_continueButtonGroup != null) DOTween.Kill(_continueButtonGroup);
            if (_settingsButtonGroup != null) DOTween.Kill(_settingsButtonGroup);
            if (_quitButtonGroup != null) DOTween.Kill(_quitButtonGroup);
        }

        private Canvas CreateCanvas(string name)
        {
            GameObject go = new GameObject(name, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Canvas canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            return canvas;
        }

        private RectTransform CreatePanel(string name, RectTransform parent, Color color)
        {
            GameObject panel = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            panel.transform.SetParent(parent, false);
            Image image = panel.GetComponent<Image>();
            image.color = color;
            return panel.GetComponent<RectTransform>();
        }

        private Text CreateText(string name, RectTransform parent, string content, int fontSize, TextAnchor anchor)
        {
            GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            textObject.transform.SetParent(parent, false);
            Text text = textObject.GetComponent<Text>();
            text.font = uiFont;
            text.fontSize = fontSize;
            text.alignment = anchor;
            text.text = content;
            text.color = Color.white;
            RectTransform rect = text.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.1f, 0.1f);
            rect.anchorMax = new Vector2(0.9f, 0.2f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return text;
        }

        private Button CreateButton(string name, RectTransform parent, string label, Vector2 anchor)
        {
            GameObject buttonObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, false);

            RectTransform rect = buttonObject.GetComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.sizeDelta = new Vector2(320f, 70f);
            rect.anchoredPosition = Vector2.zero;

            Image image = buttonObject.GetComponent<Image>();
            image.color = new Color(0.2f, 0.2f, 0.2f, 0.95f);

            Button button = buttonObject.GetComponent<Button>();

            Text text = CreateText("Text", rect, label, 24, TextAnchor.MiddleCenter);
            RectTransform textRect = text.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            return button;
        }

        private Slider CreateLabeledSlider(string name, RectTransform parent, string label, Vector2 anchor, out Text valueText)
        {
            RectTransform container = CreatePanel(name, parent, new Color(0f, 0f, 0f, 0f));
            container.anchorMin = new Vector2(anchor.x, anchor.y);
            container.anchorMax = new Vector2(anchor.x + 0.6f, anchor.y + 0.1f);
            container.offsetMin = Vector2.zero;
            container.offsetMax = Vector2.zero;

            Text labelText = CreateText("Label", container, label, 20, TextAnchor.MiddleLeft);
            RectTransform labelRect = labelText.GetComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0f, 0f);
            labelRect.anchorMax = new Vector2(0.4f, 1f);

            GameObject sliderObject = new GameObject("Slider", typeof(RectTransform), typeof(Slider), typeof(Image));
            sliderObject.transform.SetParent(container, false);
            Image backgroundImage = sliderObject.GetComponent<Image>();
            backgroundImage.color = new Color(0.2f, 0.2f, 0.2f, 0.9f);

            RectTransform sliderRect = sliderObject.GetComponent<RectTransform>();
            sliderRect.anchorMin = new Vector2(0.42f, 0.2f);
            sliderRect.anchorMax = new Vector2(0.85f, 0.8f);
            sliderRect.offsetMin = Vector2.zero;
            sliderRect.offsetMax = Vector2.zero;

            Slider slider = sliderObject.GetComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 100f;

            GameObject fillArea = new GameObject("Fill Area", typeof(RectTransform));
            fillArea.transform.SetParent(sliderObject.transform, false);
            RectTransform fillAreaRect = fillArea.GetComponent<RectTransform>();
            fillAreaRect.anchorMin = new Vector2(0f, 0f);
            fillAreaRect.anchorMax = new Vector2(1f, 1f);
            fillAreaRect.offsetMin = new Vector2(10f, 0f);
            fillAreaRect.offsetMax = new Vector2(-10f, 0f);

            GameObject fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fill.transform.SetParent(fillArea.transform, false);
            Image fillImage = fill.GetComponent<Image>();
            fillImage.color = new Color(0.6f, 0.8f, 1f, 1f);
            RectTransform fillRect = fill.GetComponent<RectTransform>();
            fillRect.anchorMin = new Vector2(0f, 0f);
            fillRect.anchorMax = new Vector2(1f, 1f);
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
            slider.fillRect = fillRect;

            GameObject handle = new GameObject("Handle", typeof(RectTransform), typeof(Image));
            handle.transform.SetParent(sliderObject.transform, false);
            Image handleImage = handle.GetComponent<Image>();
            handleImage.color = new Color(1f, 1f, 1f, 1f);
            RectTransform handleRect = handle.GetComponent<RectTransform>();
            handleRect.sizeDelta = new Vector2(20f, 20f);
            slider.handleRect = handleRect;
            slider.targetGraphic = handleImage;

            valueText = CreateText("Value", container, "0", 18, TextAnchor.MiddleRight);
            RectTransform valueRect = valueText.GetComponent<RectTransform>();
            valueRect.anchorMin = new Vector2(0.86f, 0f);
            valueRect.anchorMax = new Vector2(1f, 1f);

            return slider;
        }

        private void CreateOptionSelector(string name, RectTransform parent, string label, Vector2 anchor, string[] options, out Text valueText, Action onPrev, Action onNext)
        {
            RectTransform container = CreatePanel(name, parent, new Color(0f, 0f, 0f, 0f));
            container.anchorMin = new Vector2(anchor.x, anchor.y);
            container.anchorMax = new Vector2(anchor.x + 0.6f, anchor.y + 0.1f);
            container.offsetMin = Vector2.zero;
            container.offsetMax = Vector2.zero;

            Text labelText = CreateText("Label", container, label, 20, TextAnchor.MiddleLeft);
            RectTransform labelRect = labelText.GetComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0f, 0f);
            labelRect.anchorMax = new Vector2(0.4f, 1f);

            Button prevButton = CreateSmallButton("Prev", container, "<", new Vector2(0.44f, 0.5f));
            Button nextButton = CreateSmallButton("Next", container, ">", new Vector2(0.92f, 0.5f));
            prevButton.onClick.AddListener(() => onPrev?.Invoke());
            nextButton.onClick.AddListener(() => onNext?.Invoke());

            valueText = CreateText("Value", container, options.Length > 0 ? options[0] : string.Empty, 18, TextAnchor.MiddleCenter);
            RectTransform valueRect = valueText.GetComponent<RectTransform>();
            valueRect.anchorMin = new Vector2(0.5f, 0f);
            valueRect.anchorMax = new Vector2(0.88f, 1f);
        }

        private Button CreateSmallButton(string name, RectTransform parent, string label, Vector2 anchor)
        {
            GameObject buttonObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, false);

            RectTransform rect = buttonObject.GetComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.sizeDelta = new Vector2(40f, 36f);
            rect.anchoredPosition = Vector2.zero;

            Image image = buttonObject.GetComponent<Image>();
            image.color = new Color(0.25f, 0.25f, 0.25f, 0.95f);

            Button button = buttonObject.GetComponent<Button>();

            Text text = CreateText("Text", rect, label, 18, TextAnchor.MiddleCenter);
            RectTransform textRect = text.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            return button;
        }

        private void UpdateVolumeLabel(Text target, float value)
        {
            if (target == null)
            {
                return;
            }

            int rounded = Mathf.RoundToInt(value);
            target.text = rounded.ToString();
        }

        private void UpdateOptionLabel(Text target, string[] options, int index)
        {
            if (target == null || options == null || options.Length == 0)
            {
                return;
            }

            int safeIndex = Mathf.Clamp(index, 0, options.Length - 1);
            target.text = options[safeIndex];
        }

    }
}
