using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

// Финал резки: ГГ задыхается (Orc Idle), камера медленно уезжает за его плечо,
// и становится видна испуганная мама. Всё вблизи размывается, в фокусе только она.
// Потом мама пятится назад, разворачивается и обычной походкой уходит к прачечной;
// скрывшись из виду, она добегает до двери — слышно, как дверь открывается и захлопывается.
// Запускается из MeatCuttingInteraction.
public class CuttingFinale : MonoBehaviour
{
    [Header("ГГ")]
    [SerializeField] private Animator ggAnimator;
    [SerializeField] private string exhaustedTrigger = "Exhausted";

    [Header("Тяжёлое дыхание ГГ")]
    [Tooltip("Сколько секунд длится отдышка, пока дыхание не успокоится")]
    [SerializeField] private float breathingDuration = 15f;
    [Tooltip("Длина цикла вдох+выдох в начале и в конце отдышки")]
    [SerializeField] private float breathCycleStart = 0.85f;
    [SerializeField] private float breathCycleEnd = 2.2f;
    [SerializeField] private float breathVolumeStart = 0.9f;
    [SerializeField] private float breathVolumeEnd = 0.25f;
    [Tooltip("За сколько секунд дыхание плавно нарастает из тишины")]
    [SerializeField] private float breathFadeIn = 3f;

    [Header("Мама")]
    [SerializeField] private Animator momAnimator;
    [SerializeField] private MomWalkToLaundry momWalker;
    [SerializeField] private MomTalkInteraction momTalk;
    [Tooltip("Доворот мамы, если она смотрит чуть мимо ГГ (градусы, + вправо, − влево)")]
    [SerializeField] private float faceYawOffset = 28f;
    [SerializeField] private string fearTrigger = "Fear";
    [SerializeField] private string retreatTrigger = "Retreat";
    [Tooltip("Где стоит испуганная мама, когда камера её показывает")]
    [SerializeField] private Transform momWatchPoint;
    [Tooltip("Точки по пути к двери: до первой мама пятится (Walk Backwards), дальше идёт обычной походкой")]
    [SerializeField] private Transform[] retreatPath;
    [Tooltip("Дверь прачечной — куда мама уходит")]
    [SerializeField] private Transform momHidePoint;
    [Tooltip("Сколько метров мама пятится назад, если промежуточных точек нет")]
    [SerializeField] private float backStepDistance = 1.6f;
    [SerializeField] private float retreatSpeed = 0.6f;
    [Tooltip("Скорость обычной ходьбы к двери и бега, когда её уже не видно")]
    [SerializeField] private float walkSpeed = 1.2f;
    [SerializeField] private float runSpeed = 2.4f;
    [Tooltip("За сколько секунд мама разворачивается к двери")]
    [SerializeField] private float turnDuration = 0.8f;
    [Tooltip("Как быстро мама поворачивает на промежуточных точках, градусов в секунду")]
    [SerializeField] private float cornerTurnSpeed = 160f;

    [Header("Камера")]
    [Tooltip("Куда приезжает камера. Удобно выставить через GameObject > Align With View")]
    [SerializeField] private Transform revealAnchor;
    [SerializeField] private float revealFieldOfView = 38f;

    [Header("Тайминг (секунды)")]
    [SerializeField] private float breathBeforeMove = 2.5f;
    [SerializeField] private float cameraMoveDuration = 5.5f;
    [SerializeField] private float holdOnMom = 3f;

    [Header("Размытие")]
    [Tooltip("Через сколько секунд после начала движения камеры начинает размываться картинка")]
    [SerializeField] private float blurDelay = 1.5f;
    [Tooltip("За сколько секунд размытие нарастает до полного")]
    [SerializeField] private float blurDuration = 5f;
    [Tooltip("Сколько метров перед мамой ещё резко (ближе — размыто)")]
    [SerializeField] private float sharpBeforeMom = 1.2f;
    [Tooltip("Сколько метров за мамой ещё резко (дальше — размыто)")]
    [SerializeField] private float sharpBehindMom = 1.5f;

    [Header("Дальше: ГГ идёт к двери")]
    [SerializeField] private DoorWalkSequence doorWalk;
    [Tooltip("Через сколько секунд после начала звука двери ГГ реагирует (выходит из отдышки)")]
    [SerializeField] private float pauseAfterDoor = 0.15f;
    [Tooltip("За сколько секунд стихает дыхание, когда ГГ услышал дверь")]
    [SerializeField] private float breathStopFade = 1.2f;

    [Header("Что делать после")]
    public UnityEvent onFinished;

    private Camera cam;
    private Volume dofVolume;
    private DepthOfField dof;
    private Transform mom;
    private float blur = 0f;   // 0 — картинка чёткая, 1 — в фокусе только мама
    private float focusDistance = 10f;
    private float breathStopTime = -1f;

    public bool IsPlaying { get; private set; }

    public void Play(Camera shotCamera)
    {
        if (IsPlaying) return;
        IsPlaying = true;
        cam = shotCamera;
        StartCoroutine(Sequence());
    }

    private IEnumerator Sequence()
    {
        // 1. ГГ задыхается. Speed обнуляем: он мог остаться от ходьбы перед взятием ножа,
        // и тогда из отдышки сразу сработал бы переход в ходьбу
        if (ggAnimator != null)
        {
            ggAnimator.SetFloat("Speed", 0f);
            ggAnimator.SetTrigger(exhaustedTrigger);
        }
        StartCoroutine(HeavyBreathing());

        // 2. Мама уже стоит и смотрит (пока камера на ГГ, её не видно)
        PlaceMom();
        CreateDepthOfField();

        yield return new WaitForSeconds(breathBeforeMove);

        // 3. Камера плавно едет за плечо ГГ; размытие нарастает постепенно, отдельно от движения
        StartCoroutine(BlurIn());

        if (cam != null && revealAnchor != null)
        {
            Vector3 fromPos = cam.transform.position;
            Quaternion fromRot = cam.transform.rotation;
            float fromFov = cam.fieldOfView;

            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime / cameraMoveDuration;
                float k = SmootherStep(Mathf.Clamp01(t));

                cam.transform.SetPositionAndRotation(
                    Vector3.Lerp(fromPos, revealAnchor.position, k),
                    Quaternion.Slerp(fromRot, revealAnchor.rotation, k));
                cam.fieldOfView = Mathf.Lerp(fromFov, revealFieldOfView, k);
                yield return null;
            }
        }

        // 4. Пауза на маме
        yield return new WaitForSeconds(holdOnMom);

        // 5. Мама пятится, разворачивается и уходит за дверь
        if (mom != null && momHidePoint != null)
            yield return Retreat();

        // 6. ГГ реагирует сразу на хлопок: дыхание обрывается, размытие уходит, он идёт к двери
        if (doorWalk != null && momHidePoint != null)
        {
            if (pauseAfterDoor > 0f) yield return new WaitForSeconds(pauseAfterDoor);
            breathStopTime = Time.time;
            StartCoroutine(BlurOut(2.5f));

            var path = new System.Collections.Generic.List<Vector3>();
            if (momWatchPoint != null) path.Add(momWatchPoint.position);
            if (retreatPath != null)
                foreach (Transform p in retreatPath)
                    if (p != null) path.Add(p.position);

            doorWalk.Play(cam, path, momHidePoint.position);
        }

        IsPlaying = false;
        onFinished?.Invoke();
    }

    // Вдох-выдох: сначала часто и громко, постепенно реже и тише
    private IEnumerator HeavyBreathing()
    {
        string[] inhales = { "breath_in_1", "breath_in_2", "breath_in_3" };
        string[] exhales = { "breath_out_1", "breath_out_2", "breath_out_3" };
        int lastIn = -1, lastOut = -1;
        float start = Time.time;

        while (true)
        {
            float elapsed = Time.time - start;
            float k = Mathf.Clamp01(elapsed / breathingDuration);
            if (k >= 1f) yield break;

            // Услышал дверь — дыхание быстро затихает (замер)
            float stop = breathStopTime < 0f ? 1f : 1f - Mathf.Clamp01((Time.time - breathStopTime) / breathStopFade);
            if (stop <= 0f) yield break;

            // Спад нелинейный: первые секунды самые тяжёлые
            float ease = 1f - (1f - k) * (1f - k);
            float cycle = Mathf.Lerp(breathCycleStart, breathCycleEnd, ease) * Random.Range(0.92f, 1.08f);

            // Дыхание проступает из тишины, а не начинается сразу в полную силу
            float fadeIn = SmootherStep(Mathf.Clamp01(elapsed / breathFadeIn));
            float volume = Mathf.Lerp(breathVolumeStart, breathVolumeEnd, ease) * Mathf.Lerp(0.15f, 1f, fadeIn) * stop;
            float pitch = Mathf.Lerp(1.06f, 0.97f, ease);

            lastIn = PickOther(inhales.Length, lastIn);
            GameAudio.Play(inhales[lastIn], volume * 0.7f, pitch * Random.Range(0.97f, 1.03f));
            yield return new WaitForSeconds(cycle * 0.42f);

            lastOut = PickOther(exhales.Length, lastOut);
            GameAudio.Play(exhales[lastOut], volume, pitch * Random.Range(0.97f, 1.03f));
            yield return new WaitForSeconds(cycle * 0.58f);
        }
    }

    private static int PickOther(int count, int previous)
    {
        int i = Random.Range(0, count);
        return i == previous ? (i + 1) % count : i;
    }

    private void PlaceMom()
    {
        if (momAnimator == null) return;
        mom = momAnimator.transform;

        if (momWalker != null)
        {
            momWalker.StopAllCoroutines();
            momWalker.enabled = false;
        }

        // Мама больше не разговаривает — убираем её подсказку «нажмите F»
        if (momTalk != null) momTalk.gameObject.SetActive(false);

        momAnimator.applyRootMotion = false;
        momAnimator.SetFloat("Speed", 0f);

        if (momWatchPoint != null) mom.position = momWatchPoint.position;
        FaceGG();
        momAnimator.SetTrigger(fearTrigger);
    }

    private IEnumerator Retreat()
    {
        // Путь: первая промежуточная точка — туда мама пятится; остальные и дверь — обычной походкой
        var points = new System.Collections.Generic.List<Vector3>();
        if (retreatPath != null)
            foreach (Transform p in retreatPath)
                if (p != null) points.Add(p.position);

        Vector3 backTarget;
        if (points.Count > 0)
        {
            backTarget = points[0];
            points.RemoveAt(0);
        }
        else
        {
            // Без промежуточных точек — просто пятится назад по линии «сын → мама»
            Vector3 away = mom.position - ggAnimator.transform.position;
            away.y = 0f;
            backTarget = mom.position + away.normalized * backStepDistance;
        }
        points.Add(momHidePoint.position);
        backTarget.y = mom.position.y;

        // а) Пятится анимацией Walk Backwards: спиной к точке, лицом от неё
        if (momAnimator != null) momAnimator.SetTrigger(retreatTrigger);

        Quaternion fromRot = mom.rotation;
        float align = 0f;
        float backSpeed = 0f;

        while ((mom.position - backTarget).sqrMagnitude > 0.0025f)
        {
            Vector3 dir = backTarget - mom.position;
            dir.y = 0f;

            // Плавно доворачивается так, чтобы идти строго назад
            align = Mathf.Min(1f, align + Time.deltaTime / 0.7f);
            if (dir.sqrMagnitude > 0.001f)
                mom.rotation = Quaternion.Slerp(fromRot, Quaternion.LookRotation(-dir), SmootherStep(align));

            backSpeed = Mathf.MoveTowards(backSpeed, retreatSpeed, Time.deltaTime * 1.5f);
            mom.position = Vector3.MoveTowards(mom.position, backTarget, backSpeed * Time.deltaTime);
            yield return null;
        }

        // б) Разворачивается и обычной походкой идёт через остальные точки к двери
        if (momAnimator != null) momAnimator.SetFloat("Speed", 1f);

        float turn = 0f;
        float speed = 0f;
        bool hidden = false;
        Quaternion startRot = mom.rotation;

        foreach (Vector3 point in points)
        {
            Vector3 target = new Vector3(point.x, mom.position.y, point.z);

            while ((mom.position - target).sqrMagnitude > 0.01f)
            {
                Vector3 dir = target - mom.position;
                dir.y = 0f;

                if (dir.sqrMagnitude > 0.001f)
                {
                    Quaternion look = Quaternion.LookRotation(dir);
                    if (turn < 1f)
                    {
                        // Первый разворот спиной к сыну — плавно, за turnDuration
                        turn = Mathf.Min(1f, turn + Time.deltaTime / turnDuration);
                        mom.rotation = Quaternion.Slerp(startRot, look, SmootherStep(turn));
                    }
                    else
                    {
                        // На промежуточных точках — мягкий поворот на ходу
                        mom.rotation = Quaternion.RotateTowards(mom.rotation, look, cornerTurnSpeed * Time.deltaTime);
                    }
                }

                // Пока разворачивается — только начинает шагать; скрылась из кадра — дальше бежит
                if (!hidden && !IsMomVisible()) hidden = true;
                float targetSpeed = hidden ? runSpeed : walkSpeed * Mathf.Lerp(0.3f, 1f, turn);
                speed = Mathf.MoveTowards(speed, targetSpeed, Time.deltaTime * 2.5f);

                mom.position = Vector3.MoveTowards(mom.position, target, speed * Time.deltaTime);
                yield return null;
            }
        }

        // в) У двери: дверь открылась и захлопнулась (самой двери в кадре не видно).
        // Звук идёт параллельно — ГГ реагирует уже на начало звука, а не после хлопка
        Vector3 door = mom.position;
        mom.gameObject.SetActive(false);
        StartCoroutine(DoorSounds(door));
    }

    private IEnumerator DoorSounds(Vector3 at)
    {
        GameAudio.PlayAt(GameAudio.LoadRandom("doorOpen_1", "doorOpen_2"), at, 0.85f, 1.05f, 1f, 16f);
        yield return new WaitForSeconds(0.45f);
        GameAudio.PlayAt(GameAudio.LoadRandom("doorClose_1", "doorClose_4"), at, 1f, 0.95f, 1f, 18f);
    }

    // Мама смотрит на голову ГГ (корень объекта может стоять не там, где видно тело)
    private void FaceGG()
    {
        if (mom == null || ggAnimator == null) return;

        Transform head = ggAnimator.isHuman ? ggAnimator.GetBoneTransform(HumanBodyBones.Head) : null;
        Vector3 target = head != null ? head.position : ggAnimator.transform.position;

        Vector3 dir = target - mom.position;
        dir.y = 0f;
        if (dir.sqrMagnitude > 0.001f) mom.rotation = Quaternion.LookRotation(dir) * Quaternion.Euler(0f, faceYawOffset, 0f);
    }

    // Мама в кадре и её не загораживают стены
    private bool IsMomVisible()
    {
        if (cam == null) return false;

        Vector3 chest = mom.position + Vector3.up * 1.3f;
        Vector3 vp = cam.WorldToViewportPoint(chest);
        if (vp.z <= 0f || vp.x < -0.05f || vp.x > 1.05f || vp.y < -0.05f || vp.y > 1.05f) return false;

        Vector3 from = cam.transform.position;
        Vector3 dir = chest - from;
        foreach (RaycastHit hit in Physics.RaycastAll(from, dir.normalized, dir.magnitude, ~0, QueryTriggerInteraction.Ignore))
        {
            Transform h = hit.collider.transform;
            if (h.IsChildOf(mom)) continue;
            if (ggAnimator != null && h.IsChildOf(ggAnimator.transform)) continue;
            return false;
        }
        return true;
    }

    // ---------- Размытие (Depth of Field HDRP) ----------

    private void CreateDepthOfField()
    {
        var go = new GameObject("CuttingFinale_DoF");
        dofVolume = go.AddComponent<Volume>();
        dofVolume.isGlobal = true;
        dofVolume.priority = 100f;

        // Вес всегда 1: силу размытия плавно меняем самими границами фокуса (см. UpdateFocus),
        // иначе HDRP переключает режим размытия рывком
        dofVolume.weight = 1f;

        var profile = ScriptableObject.CreateInstance<VolumeProfile>();
        dofVolume.sharedProfile = profile;

        dof = profile.Add<DepthOfField>(true);
        dof.focusMode.Override(DepthOfFieldMode.Manual);
        dof.quality.Override((int)ScalableSettingLevelParameter.Level.High);
        UpdateFocus();
    }

    // Размытие нарастает медленно и независимо от движения камеры
    private IEnumerator BlurIn()
    {
        yield return new WaitForSeconds(blurDelay);

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / blurDuration;
            blur = SmootherStep(Mathf.Clamp01(t));
            yield return null;
        }
        blur = 1f;
    }

    private IEnumerator BlurOut(float duration)
    {
        float start = blur;
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / duration;
            blur = Mathf.Lerp(start, 0f, SmootherStep(Mathf.Clamp01(t)));
            yield return null;
        }
        blur = 0f;
    }

    private void LateUpdate()
    {
        UpdateFocus();
    }

    // blur = 0: зоны размытия «схлопнуты» (всё чётко); blur = 1: чётко только около мамы
    private void UpdateFocus()
    {
        if (dof == null || cam == null || mom == null) return;

        // Когда мама скрылась, фокус остаётся на последнем расстоянии до неё
        if (mom.gameObject.activeInHierarchy)
            focusDistance = Vector3.Distance(cam.transform.position, mom.position + Vector3.up * 1.3f);
        float d = focusDistance;

        float nearEnd = Mathf.Lerp(0.02f, Mathf.Max(0.1f, d - sharpBeforeMom), blur);
        float farStart = Mathf.Lerp(d + 80f, d + sharpBehindMom, blur);
        float farLength = Mathf.Lerp(40f, 6f, blur);

        dof.nearFocusStart.Override(0f);
        dof.nearFocusEnd.Override(nearEnd);
        dof.farFocusStart.Override(farStart);
        dof.farFocusEnd.Override(farStart + farLength);
    }

    private static float SmootherStep(float x)
    {
        return x * x * x * (x * (x * 6f - 15f) + 10f);
    }

    private void OnDestroy()
    {
        if (dofVolume != null)
        {
            if (dofVolume.sharedProfile != null) Destroy(dofVolume.sharedProfile);
            Destroy(dofVolume.gameObject);
        }
    }
}
