using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Давление на игрока во время резки: требуемый темп кликов постепенно растёт.
// Если игрок не успевает — всплывают навязчивые мысли «Быстрее.», «Режь.» и т.п.
// Чем агрессивнее удары, тем чаще сердце и тем сильнее красная виньетка по краям.
// Добавляется автоматически из MeatCuttingInteraction после первого пореза.
public class CuttingPressure : MonoBehaviour
{
    [Header("Темп")]
    public float startRequiredRate = 1.0f;   // кликов в секунду в начале
    public float rateIncreasePerClick = 0.06f;
    public float maxRequiredRate = 4.5f;

    [Header("Сердцебиение")]
    public float calmBpm = 72f;
    public float panicBpm = 160f;

    private static readonly string[] Whispers =
    {
        "Быстрее.", "Режь.", "Ещё.", "Сильнее.", "Не останавливайся.", "Давай.", "Мало.", "Продолжай."
    };

    private MeatCuttingInteraction cutting;
    private float requiredRate;
    private float urge = 0f;       // 0..1 — насколько игрок отстаёт от темпа
    private float rage = 0f;       // 0..1 — насколько агрессивно он режет
    private float nextBeat = 0f;
    private float pulse = 0f;
    private float nextWhisper = 0f;

    private const float CalmDuration = 7f;
    private bool calming = false;
    private float calmStart = 0f;

    private Image vignette;
    private RectTransform whisperRoot;
    private TMP_FontAsset font;

    public void Init(MeatCuttingInteraction owner)
    {
        cutting = owner;
        requiredRate = startRequiredRate;
        nextBeat = Time.time + 0.3f;
        nextWhisper = Time.time + 2.5f;
        BuildUI();
    }

    public void OnClick()
    {
        requiredRate = Mathf.Min(maxRequiredRate, requiredRate + rateIncreasePerClick);
    }

    // Резка закончилась: мысли пропадают, виньетка гаснет, сердце постепенно успокаивается
    public void Calm()
    {
        if (calming) return;
        calming = true;
        calmStart = Time.time;
    }

    public void OnHit(int level)
    {
        // Удар немного «отпускает» давление; вспышка виньетки — только на мощных ударах
        urge = Mathf.Max(0f, urge - 0.08f);
        pulse = Mathf.Max(pulse, 0.2f * level);
    }

    private void Update()
    {
        if (cutting == null) return;

        if (calming)
        {
            UpdateCalming();
            return;
        }

        float rate = cutting.ClickRate;

        // Отставание от требуемого темпа — от него только мысли и чуть чаще сердце
        float behind = Mathf.Clamp01((requiredRate - rate) / requiredRate);
        float speed = behind > urge ? 0.45f : 1.2f;
        urge = Mathf.MoveTowards(urge, behind, speed * Time.deltaTime);

        // Ярость — плавно следует за агрессией ударов; на спокойной резке она нулевая
        float heat = cutting.Heat;
        rage = Mathf.MoveTowards(rage, Mathf.InverseLerp(0.3f, 1f, heat), Time.deltaTime * 0.8f);

        UpdateHeartbeat(Mathf.Max(heat, urge * 0.5f));
        UpdateVignette();
        UpdateWhispers();
    }

    private void UpdateHeartbeat(float intensity)
    {
        if (Time.time < nextBeat) return;

        float bpm = Mathf.Lerp(calmBpm, panicBpm, intensity);
        nextBeat = Time.time + 60f / bpm;

        GameAudio.Play("heartbeat", Mathf.Lerp(0.2f, 0.9f, intensity), Mathf.Lerp(0.95f, 1.08f, intensity));
        pulse = Mathf.Max(pulse, 0.3f * rage);
    }

    private void UpdateVignette()
    {
        pulse = Mathf.MoveTowards(pulse, 0f, Time.deltaTime * 1.6f);

        // Виньетка только от ярости: спокойные удары — чистый экран
        float alpha = Mathf.Clamp01(rage * 0.5f + pulse * rage);
        vignette.color = new Color(0.35f, 0.02f, 0.02f, alpha);

        // Чем сильнее ярость, тем ближе виньетка подбирается к центру
        float scale = Mathf.Lerp(1.35f, 1.0f, rage);
        vignette.rectTransform.localScale = new Vector3(scale, scale, 1f);
    }

    private void UpdateCalming()
    {
        float k = Mathf.Clamp01((Time.time - calmStart) / CalmDuration);

        rage = Mathf.MoveTowards(rage, 0f, Time.deltaTime * 0.6f);
        urge = 0f;
        UpdateVignette();

        // Сердце: от текущего темпа к спокойному, всё тише
        if (Time.time >= nextBeat && k < 1f)
        {
            float bpm = Mathf.Lerp(Mathf.Lerp(calmBpm, panicBpm, cutting.Heat), calmBpm * 0.85f, k);
            nextBeat = Time.time + 60f / bpm;
            GameAudio.Play("heartbeat", Mathf.Lerp(0.8f, 0f, k), 1f);
        }

        if (k >= 1f && rage <= 0f && pulse <= 0f) Destroy(this);
    }

    private void UpdateWhispers()
    {
        if (Time.time < nextWhisper) return;

        if (urge > 0.3f)
        {
            StartCoroutine(ShowWhisper(Whispers[Random.Range(0, Whispers.Length)], urge));
            nextWhisper = Time.time + Mathf.Lerp(2.2f, 0.7f, urge);
        }
        else
        {
            nextWhisper = Time.time + 0.5f;
        }
    }

    private IEnumerator ShowWhisper(string text, float strength)
    {
        var go = new GameObject("Whisper", typeof(RectTransform));
        go.transform.SetParent(whisperRoot, false);

        var tmp = go.AddComponent<TextMeshProUGUI>();
        if (font != null) tmp.font = font;
        tmp.text = text;
        tmp.fontSize = Mathf.Lerp(34f, 52f, strength);
        tmp.characterSpacing = 6f;
        tmp.fontStyle = FontStyles.Italic;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.enableWordWrapping = false;
        tmp.raycastTarget = false;

        var rect = (RectTransform)go.transform;
        rect.anchoredPosition = new Vector2(Random.Range(-420f, 420f), Random.Range(-140f, 220f));
        rect.localRotation = Quaternion.Euler(0f, 0f, Random.Range(-4f, 4f));

        // На спокойной резке мысли бледные, с яростью краснеют
        Color color = Color.Lerp(new Color(0.85f, 0.82f, 0.78f, 0f), new Color(0.78f, 0.12f, 0.1f, 0f), rage);
        float fadeIn = 0.12f, hold = 0.45f, fadeOut = 0.7f;
        float total = fadeIn + hold + fadeOut;
        float t = 0f;

        while (t < total)
        {
            t += Time.deltaTime;
            float a = t < fadeIn ? t / fadeIn : t < fadeIn + hold ? 1f : 1f - (t - fadeIn - hold) / fadeOut;
            color.a = Mathf.Clamp01(a) * Mathf.Lerp(0.55f, 0.95f, strength);
            tmp.color = color;

            float s = 1f + 0.08f * (t / total);
            rect.localScale = new Vector3(s, s, 1f);
            yield return null;
        }

        Destroy(go);
    }

    private void OnDestroy()
    {
        if (vignette != null) Destroy(vignette.canvas.gameObject);
    }

    private void BuildUI()
    {
        // Шрифт берём у любого текста на сцене, чтобы стиль совпадал
        TMP_Text anyText = FindObjectOfType<TMP_Text>(true);
        if (anyText != null) font = anyText.font;

        var canvasGo = new GameObject("CuttingPressureUI");
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 400;

        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        var vig = new GameObject("Vignette", typeof(RectTransform), typeof(Image));
        vig.transform.SetParent(canvasGo.transform, false);
        var vigRect = (RectTransform)vig.transform;
        vigRect.anchorMin = Vector2.zero;
        vigRect.anchorMax = Vector2.one;
        vigRect.offsetMin = vigRect.offsetMax = Vector2.zero;
        vignette = vig.GetComponent<Image>();
        vignette.sprite = CreateVignetteSprite(256);
        vignette.color = new Color(0.35f, 0.02f, 0.02f, 0f);
        vignette.raycastTarget = false;

        var root = new GameObject("Whispers", typeof(RectTransform));
        root.transform.SetParent(canvasGo.transform, false);
        whisperRoot = (RectTransform)root.transform;
        whisperRoot.anchorMin = whisperRoot.anchorMax = new Vector2(0.5f, 0.5f);
        whisperRoot.sizeDelta = Vector2.zero;
    }

    // Мягкая радиальная виньетка: прозрачный центр, плотные края
    private static Sprite CreateVignetteSprite(int size)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        var pixels = new Color32[size * size];
        float half = size * 0.5f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = (x + 0.5f - half) / half;
                float dy = (y + 0.5f - half) / half;
                float d = Mathf.Sqrt(dx * dx + dy * dy) / 1.414f;
                float a = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.35f, 1f, d));
                pixels[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255));
            }
        }

        tex.SetPixels32(pixels);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
    }
}
