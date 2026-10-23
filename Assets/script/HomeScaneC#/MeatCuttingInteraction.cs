using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

// Резка мяса: после взятия ножа ГГ ставит руку на стол (HandOnTable),
// появляется подсказка, и по ЛКМ проигрывается удар ножом.
// Чем чаще игрок кликает, тем агрессивнее удар: Low -> Medium -> High.
// Висит на объекте игрока, рядом с Animator — получает Animation Event OnKnifeHit.
public class MeatCuttingInteraction : MonoBehaviour
{
    [SerializeField] private Animator ggAnimator;
    [SerializeField] private GameObject interactPrompt;
    [SerializeField] private string promptText = "нажмите ЛКМ, чтобы резать";

    [Header("Аниматор")]
    [SerializeField] private string readyStateName = "CutReady";
    [SerializeField] private string punchTag = "Punch";
    [SerializeField] private string cutTrigger = "Cut";
    [SerializeField] private string levelParam = "CutLevel";
    [SerializeField] private string speedParam = "CutSpeed";

    [Header("Агрессия")]
    [Tooltip("Частота кликов (в секунду), при которой ГГ режет спокойно")]
    [SerializeField] private float calmClicksPerSecond = 1.2f;
    [Tooltip("Частота кликов, при которой агрессия растёт до максимума")]
    [SerializeField] private float furiousClicksPerSecond = 3.2f;
    [Tooltip("Как быстро агрессия нарастает и спадает (в долях за секунду)")]
    [SerializeField] private float heatRiseSpeed = 0.22f;
    [SerializeField] private float heatFallSpeed = 0.3f;
    [Tooltip("Пороги агрессии: переход на Medium и на High (вниз — чуть ниже, чтобы не дёргалось)")]
    [SerializeField] private float mediumThreshold = 0.35f;
    [SerializeField] private float highThreshold = 0.75f;
    [Tooltip("Сколько ударов сделать на уровне, прежде чем перейти на следующий")]
    [SerializeField] private int hitsBeforeLevelUp = 2;
    [Tooltip("За сколько последних секунд считать частоту кликов")]
    [SerializeField] private float rateWindow = 1.5f;
    [Tooltip("С какого момента удара (0..1) можно сразу начать следующий")]
    [SerializeField, Range(0f, 1f)] private float chainFrom = 0.45f;
    [Tooltip("Скорость анимации удара на высокой агрессии")]
    [SerializeField] private float speedAtHigh = 1.3f;

    [Header("Склейка: второй ракурс на первом ударе")]
    [Tooltip("Где стоит камера второго ракурса. Удобно выставить через GameObject > Align With View")]
    [SerializeField] private Transform shotAnchor;
    [SerializeField] private Camera playerCamera;
    [Tooltip("В какой момент анимации удара (0..1) переключиться на второй ракурс")]
    [SerializeField, Range(0f, 1f)] private float cutAtNormalizedTime = 0.4f;
    [Tooltip("Медленный наезд камеры вперёд за время кадра, в метрах")]
    [SerializeField] private float pushIn = 0.12f;
    [SerializeField] private float shotFieldOfView = 45f;
    [Tooltip("Сила тряски камеры от удара на низкой / средней / высокой агрессии (1 = максимум)")]
    [SerializeField] private Vector3 hitShake = new Vector3(0.5f, 0.75f, 1f);
    [Tooltip("Смещение (м) и поворот (градусы) камеры при тряске силой 1")]
    [SerializeField] private float shakeMaxOffset = 0.035f;
    [SerializeField] private float shakeMaxAngle = 3f;
    [Tooltip("Резкий «кивок» камеры вниз в момент удара, в градусах при силе 1")]
    [SerializeField] private float kickAngle = 2.5f;
    [Tooltip("Сколько секунд затухает тряска")]
    [SerializeField] private float shakeDuration = 0.4f;

    [Header("Конец резки")]
    [SerializeField] private CuttingFinale finale;
    [Tooltip("Сколько ударов на высокой агрессии нужно, чтобы резка закончилась")]
    [SerializeField] private int highHitsToFinish = 6;
    [Tooltip("Запасной предел: резка закончится после стольких ударов в любом случае")]
    [SerializeField] private int maxHits = 50;

    private bool finished = false;
    private bool finaleStarted = false;
    private int highHits = 0;

    private TMP_Text promptLabel;
    private string originalPromptText;
    private bool promptShown = false;
    private bool shotPlayed = false;
    private bool firstCutStarted = false;
    private bool inShot = false;

    private readonly Queue<float> clickTimes = new Queue<float>();
    private bool clickQueued = false;
    private int currentLevel = 0;
    private int hitsAtLevel = 0;
    private float heat = 0f;

    private Camera shotCamera;
    private Vector3 shotBasePosition;
    private float shake = 0f;
    private float shakeTime = 999f;

    private CuttingPressure pressure;

    public int CutsDone { get; private set; } = 0;
    public int CurrentLevel => currentLevel;

    // 0..1 — плавная «злость»: растёт при частых кликах, спадает при редких
    public float Heat => heat;

    // Сколько кликов в секунду игрок делает сейчас
    public float ClickRate
    {
        get
        {
            DropOldClicks();
            return clickTimes.Count / rateWindow;
        }
    }

    private void Awake()
    {
        if (ggAnimator == null) ggAnimator = GetComponent<Animator>();

        if (interactPrompt != null)
            promptLabel = interactPrompt.GetComponentInChildren<TMP_Text>(true);
    }

    private void Update()
    {
        if (ggAnimator == null) return;
        if (finished)
        {
            UpdateHeat();
            return;
        }

        if (firstCutStarted) UpdateHeat();

        AnimatorStateInfo state = ggAnimator.GetCurrentAnimatorStateInfo(0);
        bool inTransition = ggAnimator.IsInTransition(0);
        bool ready = !inShot && !inTransition && state.IsName(readyStateName);
        bool punching = state.IsTag(punchTag) || (inTransition && ggAnimator.GetNextAnimatorStateInfo(0).IsTag(punchTag));

        // Подсказка нужна только перед первым порезом
        bool wantPrompt = ready && !firstCutStarted;
        if (wantPrompt != promptShown)
            SetPrompt(wantPrompt);

        if (Input.GetMouseButtonDown(0) && (ready || (firstCutStarted && punching)))
        {
            RegisterClick();
            clickQueued = true;
        }

        if (!clickQueued) return;

        // Следующий удар: сразу из стойки или прервав текущий удар после chainFrom
        bool canChain = !inTransition && state.IsTag(punchTag) && state.normalizedTime >= chainFrom;
        if (ready || canChain)
        {
            clickQueued = false;
            StartPunch();
        }
    }

    private void RegisterClick()
    {
        clickTimes.Enqueue(Time.time);

        if (!firstCutStarted)
        {
            firstCutStarted = true;
            SetPrompt(false);

            pressure = gameObject.AddComponent<CuttingPressure>();
            pressure.Init(this);
        }

        if (pressure != null) pressure.OnClick();
    }

    // Агрессия плавно тянется к частоте кликов — без резких скачков ни вверх, ни вниз
    private void UpdateHeat()
    {
        float target = Mathf.InverseLerp(calmClicksPerSecond, furiousClicksPerSecond, ClickRate);
        float speed = target > heat ? heatRiseSpeed : heatFallSpeed;
        heat = Mathf.MoveTowards(heat, target, speed * Time.deltaTime);
    }

    // Уровень меняется только на одну ступень за раз: Low <-> Medium <-> High
    private int PickLevel()
    {
        const float hysteresis = 0.1f;
        int level = currentLevel;

        float upThreshold = level == 0 ? mediumThreshold : highThreshold;
        float downThreshold = (level == 2 ? highThreshold : mediumThreshold) - hysteresis;

        if (level < 2 && heat >= upThreshold && hitsAtLevel >= hitsBeforeLevelUp) level++;
        else if (level > 0 && heat < downThreshold) level--;

        return level;
    }

    private void StartPunch()
    {
        int level = PickLevel();
        hitsAtLevel = level == currentLevel ? hitsAtLevel + 1 : 1;
        currentLevel = level;

        float speed = Mathf.Lerp(1f, speedAtHigh, heat);
        ggAnimator.SetInteger(levelParam, currentLevel);
        ggAnimator.SetFloat(speedParam, speed);
        ggAnimator.SetTrigger(cutTrigger);

        if (!shotPlayed && shotAnchor != null && playerCamera != null)
        {
            shotPlayed = true;
            StartCoroutine(CinematicShot());
        }
    }

    private void DropOldClicks()
    {
        while (clickTimes.Count > 0 && Time.time - clickTimes.Peek() > rateWindow)
            clickTimes.Dequeue();
    }

    // Резкая склейка на ракурс спереди посреди первого удара; камера там и остаётся
    private IEnumerator CinematicShot()
    {
        inShot = true;

        // Ждём, пока удар дойдёт до нужного момента
        float waitLimit = 3f;
        while (true)
        {
            AnimatorStateInfo state = ggAnimator.GetCurrentAnimatorStateInfo(0);
            if (state.IsTag(punchTag) && state.normalizedTime >= cutAtNormalizedTime) break;

            waitLimit -= Time.deltaTime;
            if (waitLimit <= 0f)
            {
                inShot = false;
                yield break;
            }
            yield return null;
        }

        shotCamera = CreateShotCamera();
        Vector3 startPos = shotAnchor.position;
        Vector3 endPos = startPos + shotAnchor.forward * pushIn;
        shotBasePosition = startPos;

        playerCamera.enabled = false;
        shotCamera.enabled = true;
        inShot = false;

        // Плавный наезд: быстрее в начале, мягко замедляется
        float elapsed = 0f;
        while (elapsed < 4f)
        {
            elapsed += Time.deltaTime;
            shotBasePosition = Vector3.Lerp(startPos, endPos, 1f - Mathf.Exp(-elapsed * 1.5f));
            yield return null;
        }
        shotBasePosition = endPos;
    }

    private void LateUpdate()
    {
        // После резки камерой управляет CuttingFinale
        if (shotCamera == null || finaleStarted) return;

        // Короткая затухающая тряска от ударов: смещение + поворот камеры
        shakeTime += Time.deltaTime;
        Vector3 offset = Vector3.zero;
        Quaternion tilt = Quaternion.identity;

        float life = Mathf.Clamp01(shakeTime / shakeDuration);
        if (life < 1f)
        {
            // Сила затухает от удара к нулю; кивок — быстрый провал вниз и возврат
            float k = shake * (1f - life);
            float kick = shake * Mathf.Sin(Mathf.Min(1f, shakeTime / 0.18f) * Mathf.PI) * kickAngle;

            float t = Time.time * 32f;
            float nx = Mathf.PerlinNoise(t, 0.1f) * 2f - 1f;
            float ny = Mathf.PerlinNoise(0.7f, t) * 2f - 1f;
            float nr = Mathf.PerlinNoise(t, 3.3f) * 2f - 1f;

            offset = (shotAnchor.right * nx + shotAnchor.up * ny) * shakeMaxOffset * k;
            tilt = Quaternion.Euler(ny * shakeMaxAngle * k + kick, nx * shakeMaxAngle * 0.6f * k, nr * shakeMaxAngle * k);
        }

        shotCamera.transform.SetPositionAndRotation(shotBasePosition + offset, shotAnchor.rotation * tilt);
    }

    // Копия камеры игрока (те же настройки рендера и постобработки), но без скриптов
    private Camera CreateShotCamera()
    {
        // Копируем внутрь выключенного объекта, чтобы скрипты камеры не успели запуститься
        var holder = new GameObject("ShotCameraHolder");
        holder.SetActive(false);
        GameObject copy = Instantiate(playerCamera.gameObject, holder.transform);
        copy.name = "CuttingShotCamera (runtime)";

        foreach (MonoBehaviour mb in copy.GetComponents<MonoBehaviour>())
        {
            // HDAdditionalCameraData оставляем — в нём настройки HDRP
            if (mb.GetType().Name != "HDAdditionalCameraData") DestroyImmediate(mb);
        }

        AudioListener listener = copy.GetComponent<AudioListener>();
        if (listener != null) DestroyImmediate(listener);

        Camera cam = copy.GetComponent<Camera>();
        cam.fieldOfView = shotFieldOfView;
        cam.enabled = false;

        copy.transform.SetParent(null, false);
        copy.transform.SetPositionAndRotation(shotAnchor.position, shotAnchor.rotation);
        copy.transform.localScale = Vector3.one;
        Destroy(holder);
        return cam;
    }

    private void SetPrompt(bool show)
    {
        promptShown = show;
        if (interactPrompt == null) return;

        if (show && promptLabel != null)
        {
            if (originalPromptText == null) originalPromptText = promptLabel.text;
            promptLabel.text = promptText;
            promptLabel.enableWordWrapping = false;
        }
        else if (!show && promptLabel != null && originalPromptText != null)
        {
            promptLabel.text = originalPromptText;
        }

        interactPrompt.SetActive(show);
    }

    // Вызывается Animation Event в момент удара ножа по мясу
    public void OnKnifeHit()
    {
        CutsDone++;

        float volume = 0.6f + 0.15f * currentLevel;
        float pitch = Random.Range(0.95f, 1.05f) - 0.04f * currentLevel;
        GameAudio.Play(GameAudio.LoadRandom("chop", "knifeSlice", "knifeSlice2"), volume, pitch);

        // Новый удар перезапускает тряску (если предыдущая ещё сильнее — берём её)
        float strength = hitShake[currentLevel];
        float remaining = shake * (1f - Mathf.Clamp01(shakeTime / shakeDuration));
        shake = Mathf.Max(strength, remaining);
        shakeTime = 0f;
        Debug.Log($"[Резка] удар, уровень {currentLevel}, тряска {shake:0.00}");
        if (pressure != null) pressure.OnHit(currentLevel);

        if (currentLevel == 2) highHits++;
        if (!finished && (highHits >= highHitsToFinish || CutsDone >= maxHits)) Finish();
    }

    private void Finish()
    {
        finished = true;
        clickQueued = false;
        SetPrompt(false);
        if (pressure != null) pressure.Calm();
        StartCoroutine(StartFinaleAfterPunch());
    }

    // Даём последнему удару доиграть, потом — отдышка и финал
    private IEnumerator StartFinaleAfterPunch()
    {
        float limit = 2f;
        while (limit > 0f)
        {
            AnimatorStateInfo state = ggAnimator.GetCurrentAnimatorStateInfo(0);
            if (!state.IsTag(punchTag) || state.normalizedTime >= 0.8f) break;
            limit -= Time.deltaTime;
            yield return null;
        }

        // Даём тряске от последнего удара улечься
        yield return new WaitForSeconds(shakeDuration);

        finaleStarted = true;
        Camera cam = shotCamera != null ? shotCamera : playerCamera;
        if (finale != null) finale.Play(cam);
    }
}
