using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Подсказка пропуска в правом нижнем углу: кольцо удержания + клавиша + подпись.
// Сама считывает ввод: короткое нажатие (TapDown) и удержание Пробела (OnHoldComplete).
// Создаётся автоматически при первом обращении, на сцену добавлять не нужно.
public class SkipHint : MonoBehaviour
{
    public const KeyCode SkipKey = KeyCode.Space;
    private const float HoldDuration = 0.9f;
    private const float FadeSpeed = 5f;

    private static readonly Color TextColor = new Color(0.91f, 0.88f, 0.83f, 0.9f);
    private static readonly Color AccentColor = new Color(0.93f, 0.85f, 0.68f, 1f);

    private static SkipHint instance;

    public static SkipHint Instance
    {
        get
        {
            if (instance == null)
            {
                var go = new GameObject("SkipHint");
                DontDestroyOnLoad(go);
                instance = go.AddComponent<SkipHint>();
                instance.Build();
            }
            return instance;
        }
    }

    public static bool HasInstance => instance != null;

    // Короткое нажатие в этом кадре (Пробел или ЛКМ)
    public static bool TapDown => Input.GetKeyDown(SkipKey) || Input.GetMouseButtonDown(0);

    public event Action OnHoldComplete;

    private CanvasGroup group;
    private Image ringFill;
    private TMP_Text label;
    private Image blackout;

    private bool visible = false;
    private float holdTime = 0f;
    private bool holdFired = false;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        instance = null;
    }

    public void ShowDialogueHint()
    {
        label.text = "далее   <alpha=#80>удерживайте — пропустить";
        visible = true;
    }

    public void ShowCutsceneHint()
    {
        label.text = "удерживайте, чтобы пропустить";
        visible = true;
    }

    public void Hide()
    {
        visible = false;
    }

    private void Update()
    {
        group.alpha = Mathf.MoveTowards(group.alpha, visible ? 1f : 0f, FadeSpeed * Time.unscaledDeltaTime);

        if (visible && Input.GetKey(SkipKey))
        {
            holdTime += Time.unscaledDeltaTime;

            if (!holdFired && holdTime >= HoldDuration)
            {
                holdFired = true;
                OnHoldComplete?.Invoke();
            }
        }
        else
        {
            // Отпустил — кольцо плавно опустошается
            holdTime = Mathf.MoveTowards(holdTime, 0f, Time.unscaledDeltaTime * 3f);
            if (!Input.GetKey(SkipKey)) holdFired = false;
        }

        float progress = Mathf.Clamp01(holdTime / HoldDuration);
        ringFill.fillAmount = Mathf.SmoothStep(0f, 1f, progress);
    }

    // ---------- Затемнение для перехода между сценами ----------

    public IEnumerator FadeToBlack(float duration)
    {
        blackout.raycastTarget = true;
        float a = blackout.color.a;
        while (a < 1f)
        {
            a = Mathf.MoveTowards(a, 1f, Time.unscaledDeltaTime / duration);
            blackout.color = new Color(0f, 0f, 0f, a);
            yield return null;
        }
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        visible = false;
        group.alpha = 0f;
        if (blackout.color.a > 0f) StartCoroutine(FadeFromBlack(0.8f));
    }

    private IEnumerator FadeFromBlack(float duration)
    {
        float a = blackout.color.a;
        while (a > 0f)
        {
            a = Mathf.MoveTowards(a, 0f, Time.unscaledDeltaTime / duration);
            blackout.color = new Color(0f, 0f, 0f, a);
            yield return null;
        }
        blackout.raycastTarget = false;
    }

    // ---------- Построение интерфейса ----------

    private void Build()
    {
        var canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 500;

        var scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        // Строка подсказки, растёт влево от правого верхнего угла
        var row = new GameObject("Hint", typeof(RectTransform));
        row.transform.SetParent(transform, false);
        var rowRect = (RectTransform)row.transform;
        rowRect.anchorMin = rowRect.anchorMax = rowRect.pivot = new Vector2(1f, 1f);
        rowRect.anchoredPosition = new Vector2(-56f, -44f);

        var layout = row.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 14f;
        layout.childAlignment = TextAnchor.MiddleRight;
        layout.childControlWidth = layout.childControlHeight = true;
        layout.childForceExpandWidth = layout.childForceExpandHeight = false;

        var fitter = row.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        group = row.AddComponent<CanvasGroup>();
        group.alpha = 0f;
        group.interactable = false;
        group.blocksRaycasts = false;

        BuildRing(row.transform);
        BuildKeyCap(row.transform, "Пробел");
        label = CreateText(row.transform, "Label", "", 22f, TextColor, FontStyles.Normal);

        // Чёрный экран поверх всего
        var black = new GameObject("Blackout", typeof(RectTransform), typeof(Image));
        black.transform.SetParent(transform, false);
        var blackRect = (RectTransform)black.transform;
        blackRect.anchorMin = Vector2.zero;
        blackRect.anchorMax = Vector2.one;
        blackRect.offsetMin = blackRect.offsetMax = Vector2.zero;
        blackout = black.GetComponent<Image>();
        blackout.color = new Color(0f, 0f, 0f, 0f);
        blackout.raycastTarget = false;
    }

    private void BuildRing(Transform parent)
    {
        Sprite ring = CreateRingSprite(128, 0.80f);

        var holder = new GameObject("Ring", typeof(RectTransform), typeof(LayoutElement));
        holder.transform.SetParent(parent, false);
        var le = holder.GetComponent<LayoutElement>();
        le.preferredWidth = le.preferredHeight = 34f;

        Image back = CreateImage(holder.transform, "Back", ring, new Color(1f, 1f, 1f, 0.18f));
        back.type = Image.Type.Simple;

        ringFill = CreateImage(holder.transform, "Fill", ring, AccentColor);
        ringFill.type = Image.Type.Filled;
        ringFill.fillMethod = Image.FillMethod.Radial360;
        ringFill.fillOrigin = (int)Image.Origin360.Top;
        ringFill.fillClockwise = true;
        ringFill.fillAmount = 0f;
    }

    private void BuildKeyCap(Transform parent, string keyName)
    {
        var cap = new GameObject("KeyCap", typeof(RectTransform), typeof(Image));
        cap.transform.SetParent(parent, false);

        var bg = cap.GetComponent<Image>();
        bg.sprite = CreateRoundedSprite(64, 14, 0f);
        bg.type = Image.Type.Sliced;
        bg.color = new Color(1f, 1f, 1f, 0.10f);
        bg.raycastTarget = false;

        var capLayout = cap.AddComponent<HorizontalLayoutGroup>();
        capLayout.padding = new RectOffset(16, 16, 6, 7);
        capLayout.childAlignment = TextAnchor.MiddleCenter;
        capLayout.childControlWidth = capLayout.childControlHeight = true;
        capLayout.childForceExpandWidth = capLayout.childForceExpandHeight = false;

        // Тонкая рамка поверх подложки
        var frame = new GameObject("Frame", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
        frame.transform.SetParent(cap.transform, false);
        frame.GetComponent<LayoutElement>().ignoreLayout = true;
        var frameRect = (RectTransform)frame.transform;
        frameRect.anchorMin = Vector2.zero;
        frameRect.anchorMax = Vector2.one;
        frameRect.offsetMin = frameRect.offsetMax = Vector2.zero;
        var frameImg = frame.GetComponent<Image>();
        frameImg.sprite = CreateRoundedSprite(64, 14, 2f);
        frameImg.type = Image.Type.Sliced;
        frameImg.color = new Color(1f, 1f, 1f, 0.35f);
        frameImg.raycastTarget = false;

        CreateText(cap.transform, "Key", keyName, 18f, AccentColor, FontStyles.Bold);
    }

    private static Image CreateImage(Transform parent, string name, Sprite sprite, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var rect = (RectTransform)go.transform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;

        var img = go.GetComponent<Image>();
        img.sprite = sprite;
        img.color = color;
        img.raycastTarget = false;
        return img;
    }

    private static TMP_Text CreateText(Transform parent, string name, string text, float size, Color color, FontStyles style)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);

        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = size;
        tmp.color = color;
        tmp.fontStyle = style;
        tmp.characterSpacing = 2f;
        tmp.enableWordWrapping = false;
        tmp.alignment = TextAlignmentOptions.MidlineLeft;
        tmp.raycastTarget = false;
        return tmp;
    }

    // Сглаженное кольцо: inner — внутренний радиус относительно внешнего
    private static Sprite CreateRingSprite(int size, float inner)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;

        float r = size * 0.5f;
        float outerR = r - 1f;
        float innerR = outerR * inner;
        var pixels = new Color32[size * size];

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(r, r));
                float a = Mathf.Clamp01(outerR - d + 0.5f) * Mathf.Clamp01(d - innerR + 0.5f);
                pixels[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255));
            }
        }

        tex.SetPixels32(pixels);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
    }

    // Скруглённый прямоугольник для 9-slice; stroke > 0 — только рамка
    private static Sprite CreateRoundedSprite(int size, int radius, float stroke)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        var pixels = new Color32[size * size];
        float half = size * 0.5f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                // Расстояние до края скруглённого прямоугольника (signed distance)
                float px = Mathf.Abs(x + 0.5f - half) - (half - radius);
                float py = Mathf.Abs(y + 0.5f - half) - (half - radius);
                float outside = new Vector2(Mathf.Max(px, 0f), Mathf.Max(py, 0f)).magnitude + Mathf.Min(Mathf.Max(px, py), 0f) - radius;

                float a = Mathf.Clamp01(0.5f - outside);
                if (stroke > 0f) a *= Mathf.Clamp01(outside + stroke + 0.5f);

                pixels[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255));
            }
        }

        tex.SetPixels32(pixels);
        tex.Apply();
        float b = radius + 2f;
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(b, b, b, b));
    }
}
