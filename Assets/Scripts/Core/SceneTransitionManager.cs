using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using DG.Tweening;

namespace RoguelikeCardBattler.Core
{
    /// <summary>
    /// Global singleton that wraps scene loading with a fade-to-black / fade-from-black
    /// transition using DOTween. Persists across scenes via DontDestroyOnLoad.
    ///
    /// Usage: call <c>SceneTransitionManager.LoadScene("SceneName")</c> anywhere
    /// instead of <c>SceneManager.LoadScene</c>.
    ///
    /// How it works:
    ///   1. Fade-out: overlay Image alpha tweens from 0 → 1 (screen goes black).
    ///   2. Scene load: SceneManager.LoadScene fires synchronously at OnComplete.
    ///   3. Fade-in: on sceneLoaded callback, alpha tweens 1 → 0 (screen reveals).
    ///
    /// The overlay Canvas uses sortingOrder = 999 so it renders above every other
    /// Canvas in the project. During a transition raycastTarget is enabled on the
    /// overlay Image to block all input until the fade-in completes.
    /// </summary>
    public class SceneTransitionManager : MonoBehaviour
    {
        private static SceneTransitionManager _instance;
        private Image _overlay;
        private bool _isTransitioning;

        private const int OverlaySortOrder = 999;
        private const float DefaultFadeDuration = 0.3f;

        /// <summary>
        /// Returns the singleton, creating it on demand if needed.
        /// Safe to call from any scene at any time.
        /// </summary>
        public static SceneTransitionManager GetOrCreate()
        {
            if (_instance != null)
            {
                return _instance;
            }

            SceneTransitionManager existing =
                UnityEngine.Object.FindFirstObjectByType<SceneTransitionManager>();
            if (existing != null)
            {
                _instance = existing;
                return _instance;
            }

            GameObject go = new GameObject("SceneTransitionManager");
            _instance = go.AddComponent<SceneTransitionManager>();
            return _instance;
        }

        /// <summary>
        /// Loads a scene with a fade-out → load → fade-in transition.
        /// If a transition is already in progress the call is ignored.
        /// </summary>
        public static void LoadScene(string sceneName, float fadeDuration = DefaultFadeDuration)
        {
            SceneTransitionManager manager = GetOrCreate();
            manager.TransitionTo(sceneName, fadeDuration);
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);
            CreateOverlay();
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void TransitionTo(string sceneName, float fadeDuration)
        {
            if (_isTransitioning)
            {
                return;
            }

            _isTransitioning = true;
            _overlay.raycastTarget = true;

            DOTween.Kill(_overlay);
            DOTween.ToAlpha(() => _overlay.color, c => _overlay.color = c, 1f, fadeDuration)
                .SetTarget(_overlay)
                .SetEase(Ease.InQuad)
                .SetUpdate(true)
                .OnComplete(() =>
                {
                    SceneManager.sceneLoaded += OnSceneLoaded;
                    _fadeDuration = fadeDuration;
                    SceneManager.LoadScene(sceneName);
                });
        }

        private float _fadeDuration = DefaultFadeDuration;

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;

            DOTween.Kill(_overlay);
            DOTween.ToAlpha(() => _overlay.color, c => _overlay.color = c, 0f, _fadeDuration)
                .SetTarget(_overlay)
                .SetEase(Ease.OutQuad)
                .SetUpdate(true)
                .OnComplete(() =>
                {
                    _overlay.raycastTarget = false;
                    _isTransitioning = false;
                });
        }

        /// <summary>
        /// Builds a full-screen black overlay Image on a dedicated Canvas.
        /// Starts fully transparent and non-blocking.
        /// </summary>
        private void CreateOverlay()
        {
            GameObject canvasGO = new GameObject("TransitionCanvas",
                typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGO.transform.SetParent(transform, false);

            Canvas canvas = canvasGO.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = OverlaySortOrder;

            CanvasScaler scaler = canvasGO.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            GameObject imageGO = new GameObject("FadeOverlay", typeof(RectTransform), typeof(Image));
            imageGO.transform.SetParent(canvasGO.transform, false);

            RectTransform rect = imageGO.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            _overlay = imageGO.GetComponent<Image>();
            _overlay.color = new Color(0f, 0f, 0f, 0f);
            _overlay.raycastTarget = false;
        }
    }
}
