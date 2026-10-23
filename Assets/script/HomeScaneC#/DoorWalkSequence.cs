using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

// После того как мама скрылась за дверью: ГГ поворачивается туда и, пока игрок держит W,
// идёт к двери по пути мамы. Камера следует со спины, у двери — медленный кадр со спины.
// Картинка постепенно «нагнетается»: виньетка, холодный бесцветный тон, зерно, аберрации.
// Запускается из CuttingFinale.
public class DoorWalkSequence : MonoBehaviour
{
    [Header("ГГ")]
    [SerializeField] private Animator ggAnimator;
    [SerializeField] private string speedParam = "Speed";
    [SerializeField] private float walkSpeed = 1.1f;
    [SerializeField] private float turnDuration = 1.6f;
    [SerializeField] private float cornerTurnSpeed = 140f;
    [Tooltip("На каком расстоянии от двери ГГ останавливается")]
    [SerializeField] private float stopBeforeDoor = 0.65f;
    [Tooltip("Свой путь ГГ к двери (по порядку). Пусто — пойдёт по пути мамы")]
    [SerializeField] private Transform[] walkPath;

    [Header("Шаг назад от стола перед поворотом")]
    [SerializeField] private float stepBackDistance = 0.4f;
    [SerializeField] private float stepBackDuration = 1.1f;
    [SerializeField] private string stepBackTrigger = "StepBack";
    [SerializeField] private string stepDoneTrigger = "StepDone";

    [Header("Подсказка")]
    [SerializeField] private GameObject walkPrompt;
    [SerializeField] private string promptText = "удерживайте W, чтобы идти";

    [Header("Камера со спины")]
    [SerializeField] private float followDistance = 1.7f;
    [SerializeField] private float followHeight = 1.75f;
    [SerializeField] private float shoulderOffset = 0.35f;
    [SerializeField] private float followSmoothTime = 0.45f;
    [SerializeField] private float cameraBlendDuration = 2.5f;
    [SerializeField] private float followFieldOfView = 50f;

    [Header("Кадр у двери")]
    [SerializeField] private float doorShotDistance = 1.5f;
    [Tooltip("Камера ниже головы — снизу вверх смотрится тревожнее")]
    [SerializeField] private float doorShotHeight = 1.2f;
    [SerializeField] private float doorShotSide = 0.4f;
    [SerializeField] private float doorShotBlend = 3f;
    [SerializeField] private float doorShotPushIn = 0.3f;
    [SerializeField] private float doorShotPushDuration = 7f;
    [SerializeField] private float doorShotFieldOfView = 42f;

    [Header("Удар по двери и темнота")]
    [SerializeField] private string punchTrigger = "PunchDoor";
    [Tooltip("Сколько ГГ стоит перед дверью, прежде чем ударить")]
    [SerializeField] private float pauseBeforePunch = 0.8f;
    [Tooltip("Пауза тишины после удара перед затемнением")]
    [SerializeField] private float pauseBeforeBlack = 0.7f;
    [SerializeField] private float fadeToBlackDuration = 1.5f;

    [Header("Нагнетающий фильтр")]
    [SerializeField] private float filterAtStart = 0.35f;
    [SerializeField] private float filterAtDoor = 1f;
    [Tooltip("За сколько секунд с момента, как ГГ услышал дверь, фильтр выходит на силу (появляется сразу, но мягко)")]
    [SerializeField] private float filterFadeIn = 2f;

    [Header("Сердцебиение")]
    [SerializeField] private float heartBpmStart = 58f;
    [SerializeField] private float heartBpmDoor = 92f;

    [Header("Что делать после")]
    public UnityEvent onArrived;

    private Camera cam;
    private Transform gg;
    private CharacterController controller;
    private TMP_Text promptLabel;

    private Volume filterVolume;
    private float filter = 0f;
    private float filterStart = 0f;
    private bool atDoor = false;
    private bool doorHit = false;
    private float hitTime = -10f;
    private Vector3 doorPosition;
    private float progress = 0f;
    private bool heartActive = false;

    private Vector3 camVelocity;

    public bool IsPlaying { get; private set; }

    public void Play(Camera shotCamera, List<Vector3> path, Vector3 door)
    {
        if (IsPlaying || ggAnimator == null) return;
        IsPlaying = true;
        cam = shotCamera;
        gg = ggAnimator.transform;
        controller = gg.GetComponent<CharacterController>();
        if (walkPrompt != null) promptLabel = walkPrompt.GetComponentInChildren<TMP_Text>(true);

        // ГГ услышал дверь — с этого момента фильтр и сердце
        CreateFilter();
        filterStart = Time.time;
        StartCoroutine(Heartbeat());
        StartCoroutine(Sequence(path, door));
    }

    private IEnumerator Sequence(List<Vector3> pathPoints, Vector3 door)
    {
        // Путь ГГ: свои точки (или мамины, если своих нет), последняя — чуть не доходя до двери
        var path = new List<Vector3>();
        if (walkPath != null)
            foreach (Transform p in walkPath)
                if (p != null) path.Add(p.position);
        if (path.Count == 0) path.AddRange(pathPoints);
        Vector3 last = path.Count > 0 ? path[path.Count - 1] : gg.position;
        Vector3 toDoor = door - last;
        toDoor.y = 0f;
        Vector3 stop = door - toDoor.normalized * stopBeforeDoor;
        path.Add(stop);
        for (int i = 0; i < path.Count; i++) path[i] = new Vector3(path[i].x, gg.position.y, path[i].z);

        // Своим перемещением управляем сами — контроллер отключаем, чтобы он не мешал
        if (controller != null) controller.enabled = false;
        ggAnimator.applyRootMotion = false;

        Vector3 camStartPos = cam.transform.position;
        Quaternion camStartRot = cam.transform.rotation;
        float camStartFov = cam.fieldOfView;
        float camBlend = 0f;
        float t;

        // 0. Отступает от стола на полшага, чтобы при повороте не задеть его телом
        if (stepBackDistance > 0.01f)
        {
            ggAnimator.SetTrigger(stepBackTrigger);
            Vector3 from = gg.position;
            Vector3 to = from - LookFlat(gg.forward, gg.rotation) * Vector3.forward * stepBackDistance;

            t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime / stepBackDuration;
                gg.position = Vector3.Lerp(from, to, SmootherStep(Mathf.Clamp01(t)));
                yield return null;
            }
            ggAnimator.SetTrigger(stepDoneTrigger);
        }

        float totalLength = PathLength(gg.position, path);
        float travelled = 0f;

        // 1. ГГ медленно поворачивается в сторону двери
        Quaternion fromRot = gg.rotation;
        Quaternion toRot = LookFlat(path[0] - gg.position, gg.rotation);
        t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / turnDuration;
            gg.rotation = Quaternion.Slerp(fromRot, toRot, SmootherStep(Mathf.Clamp01(t)));

            camBlend = Mathf.Min(1f, camBlend + Time.deltaTime / cameraBlendDuration);
            UpdateFollowCamera(camStartPos, camStartRot, camStartFov, camBlend);
            yield return null;
        }

        // 2. Идёт, пока игрок держит W
        float speed = 0f;
        int index = 0;
        SetPrompt(true);

        while (index < path.Count)
        {
            bool holding = Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow);
            speed = Mathf.MoveTowards(speed, holding ? walkSpeed : 0f, Time.deltaTime * (holding ? 2.2f : 3f));
            SetPrompt(!holding && speed < 0.05f);

            Vector3 target = path[index];
            Vector3 dir = target - gg.position;
            dir.y = 0f;

            if (dir.sqrMagnitude > 0.0001f)
            {
                gg.rotation = Quaternion.RotateTowards(gg.rotation, Quaternion.LookRotation(dir), cornerTurnSpeed * Time.deltaTime);
                Vector3 before = gg.position;
                gg.position = Vector3.MoveTowards(gg.position, target, speed * Time.deltaTime);
                travelled += Vector3.Distance(before, gg.position);
            }

            if ((gg.position - target).sqrMagnitude < 0.0025f) index++;

            ggAnimator.SetFloat(speedParam, speed / walkSpeed);

            progress = totalLength > 0.01f ? Mathf.Clamp01(travelled / totalLength) : 1f;

            camBlend = Mathf.Min(1f, camBlend + Time.deltaTime / cameraBlendDuration);
            UpdateFollowCamera(camStartPos, camStartRot, camStartFov, camBlend);
            yield return null;
        }

        // 3. У двери: ГГ останавливается, камера уходит в низкий кадр со спины и медленно наезжает
        SetPrompt(false);
        float stopSpeed = speed;
        while (stopSpeed > 0.01f)
        {
            stopSpeed = Mathf.MoveTowards(stopSpeed, 0f, Time.deltaTime * 3f);
            ggAnimator.SetFloat(speedParam, stopSpeed / walkSpeed);
            yield return null;
        }
        ggAnimator.SetFloat(speedParam, 0f);

        Vector3 lookAt = new Vector3(door.x, gg.position.y + 1.35f, door.z);
        Vector3 back = -gg.forward;
        Vector3 shotPos = gg.position + back * doorShotDistance + Vector3.up * doorShotHeight + gg.right * doorShotSide;
        shotPos = AvoidWalls(gg.position + Vector3.up * 1.4f, shotPos);
        Quaternion shotRot = Quaternion.LookRotation(lookAt - shotPos);
        Vector3 pushEnd = shotPos + (lookAt - shotPos).normalized * doorShotPushIn;

        Vector3 fromPos = cam.transform.position;
        Quaternion fromCamRot = cam.transform.rotation;
        float fromFov = cam.fieldOfView;

        t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / doorShotBlend;
            float k = SmootherStep(Mathf.Clamp01(t));
            cam.transform.SetPositionAndRotation(Vector3.Lerp(fromPos, shotPos, k), Quaternion.Slerp(fromCamRot, shotRot, k));
            cam.fieldOfView = Mathf.Lerp(fromFov, doorShotFieldOfView, k);
            progress = 1f;
            atDoor = true;
            yield return null;
        }

        // 4. Медленный наезд и удар кулаком по двери
        StartCoroutine(PushIn(shotPos, pushEnd, lookAt));
        yield return new WaitForSeconds(pauseBeforePunch);

        doorHit = false;
        doorPosition = door;
        ggAnimator.SetTrigger(punchTrigger);

        // Ждём момент удара из анимации (событие OnDoorHit), на всякий случай — с запасом по времени
        float wait = 0f;
        while (!doorHit && wait < 2f)
        {
            wait += Time.deltaTime;
            yield return null;
        }
        if (!doorHit) OnDoorHit();

        // 5. Тишина после удара — и темнота
        yield return new WaitForSeconds(pauseBeforeBlack);
        yield return SkipHint.Instance.FadeToBlack(fadeToBlackDuration);

        IsPlaying = false;
        onArrived?.Invoke();
    }

    private IEnumerator PushIn(Vector3 from, Vector3 to, Vector3 lookAt)
    {
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / doorShotPushDuration;
            Vector3 pos = Vector3.Lerp(from, to, SmootherStep(Mathf.Clamp01(t)));
            Quaternion rot = Quaternion.LookRotation(lookAt - pos);

            // Толчок камеры от удара по двери: быстро вздрагивает и успокаивается
            float jolt = Mathf.Clamp01(1f - (Time.time - hitTime) / 0.45f);
            if (doorHit && jolt > 0f)
            {
                float n = Time.time * 40f;
                pos += (cam.transform.right * (Mathf.PerlinNoise(n, 0f) - 0.5f) + cam.transform.up * (Mathf.PerlinNoise(0f, n) - 0.5f)) * 0.06f * jolt * jolt;
                rot *= Quaternion.Euler(-2.5f * jolt * jolt, 0f, (Mathf.PerlinNoise(n, 5f) - 0.5f) * 3f * jolt);
            }

            cam.transform.SetPositionAndRotation(pos, rot);
            yield return null;
        }
    }

    // Вызывается Animation Event в момент, когда кулак бьёт по двери
    public void OnDoorHit()
    {
        if (doorHit) return;
        doorHit = true;
        hitTime = Time.time;

        AudioClip bang = GameAudio.LoadRandom("impactWood_heavy_000", "impactWood_heavy_002");
        GameAudio.PlayAt(bang, doorPosition + Vector3.up * 1.2f, 1f, 0.85f, 1f, 20f);
        GameAudio.PlayAt(GameAudio.Load("impactPlank_medium_001"), doorPosition + Vector3.up * 1.2f, 0.6f, 0.8f, 1f, 20f);

        // Сердце обрывается — после удара тишина
        heartActive = false;
    }

    // Камера за спиной ГГ, чуть сбоку через плечо; плавно догоняет его
    private void UpdateFollowCamera(Vector3 startPos, Quaternion startRot, float startFov, float blend)
    {
        Vector3 head = gg.position + Vector3.up * 1.5f;
        Vector3 desired = gg.position - gg.forward * followDistance + Vector3.up * followHeight + gg.right * shoulderOffset;
        desired = AvoidWalls(head, desired);
        Vector3 lookAt = head + gg.forward * 2.5f - Vector3.up * 0.15f;

        Vector3 follow = Vector3.SmoothDamp(cam.transform.position, desired, ref camVelocity, followSmoothTime);
        Quaternion followRot = Quaternion.Slerp(cam.transform.rotation, Quaternion.LookRotation(lookAt - follow), 1f - Mathf.Exp(-Time.deltaTime * 4f));

        // Пока камера переезжает с прошлого ракурса — смешиваем
        float k = SmootherStep(blend);
        if (k < 1f)
        {
            Vector3 targetPos = Vector3.Lerp(startPos, desired, k);
            cam.transform.SetPositionAndRotation(targetPos, Quaternion.Slerp(startRot, Quaternion.LookRotation(lookAt - targetPos), k));
            camVelocity = Vector3.zero;
        }
        else
        {
            cam.transform.SetPositionAndRotation(follow, followRot);
        }
        cam.fieldOfView = Mathf.Lerp(startFov, followFieldOfView, k);
    }

    // Не даём камере уйти в стену: сферой от головы ГГ до нужной точки
    private Vector3 AvoidWalls(Vector3 from, Vector3 to)
    {
        Vector3 dir = to - from;
        float dist = dir.magnitude;
        if (dist < 0.01f) return to;

        foreach (RaycastHit hit in Physics.SphereCastAll(from, 0.2f, dir / dist, dist, ~0, QueryTriggerInteraction.Ignore))
        {
            if (hit.collider.transform.IsChildOf(gg)) continue;
            if (hit.distance < dist) dist = hit.distance;
        }
        return from + dir.normalized * Mathf.Max(0.3f, dist - 0.1f);
    }

    private IEnumerator Heartbeat()
    {
        heartActive = true;
        while (heartActive)
        {
            float bpm = Mathf.Lerp(heartBpmStart, heartBpmDoor, progress);
            GameAudio.Play("heartbeat", Mathf.Lerp(0.12f, 0.55f, filter), 0.92f);
            yield return new WaitForSeconds(60f / bpm);
        }
    }

    private void Update()
    {
        if (filterVolume == null) return;

        if (!atDoor)
        {
            // Фильтр появляется сразу, как ГГ услышал дверь (быстрый старт, мягкий выход — без рывка),
            // дальше усиливается по мере приближения к двери
            float x = Mathf.Clamp01((Time.time - filterStart) / filterFadeIn);
            float fadeIn = 1f - (1f - x) * (1f - x) * (1f - x);
            filter = fadeIn * Mathf.Lerp(filterAtStart, filterAtDoor * 0.85f, progress);
        }
        else
        {
            filter = Mathf.MoveTowards(filter, filterAtDoor, Time.deltaTime * 0.3f);
        }

        filterVolume.weight = filter;
    }

    // ---------- Нагнетающий фильтр (HDRP Volume) ----------

    private void CreateFilter()
    {
        var go = new GameObject("DoorWalk_TensionFilter");
        filterVolume = go.AddComponent<Volume>();
        filterVolume.isGlobal = true;
        filterVolume.priority = 110f;
        filterVolume.weight = 0f;

        var profile = ScriptableObject.CreateInstance<VolumeProfile>();
        filterVolume.sharedProfile = profile;

        var vignette = profile.Add<Vignette>(true);
        vignette.intensity.Override(0.5f);
        vignette.smoothness.Override(0.55f);
        vignette.color.Override(new Color(0.05f, 0f, 0f));

        var color = profile.Add<ColorAdjustments>(true);
        color.saturation.Override(-45f);
        color.contrast.Override(22f);
        color.postExposure.Override(-0.35f);
        color.colorFilter.Override(new Color(0.86f, 0.9f, 1f));

        var grain = profile.Add<FilmGrain>(true);
        grain.type.Override(FilmGrainLookup.Medium3);
        grain.intensity.Override(0.45f);
        grain.response.Override(0.6f);

        var aberration = profile.Add<ChromaticAberration>(true);
        aberration.intensity.Override(0.35f);

        var distortion = profile.Add<LensDistortion>(true);
        distortion.intensity.Override(-0.12f);
    }

    private void SetPrompt(bool show)
    {
        if (walkPrompt == null || walkPrompt.activeSelf == show) return;

        if (show && promptLabel != null)
        {
            promptLabel.text = promptText;
            promptLabel.enableWordWrapping = false;
        }
        walkPrompt.SetActive(show);
    }

    private static float PathLength(Vector3 start, List<Vector3> path)
    {
        float length = 0f;
        Vector3 prev = start;
        foreach (Vector3 p in path)
        {
            length += Vector3.Distance(prev, p);
            prev = p;
        }
        return length;
    }

    private static Quaternion LookFlat(Vector3 dir, Quaternion fallback)
    {
        dir.y = 0f;
        return dir.sqrMagnitude > 0.0001f ? Quaternion.LookRotation(dir) : fallback;
    }

    private static float SmootherStep(float x)
    {
        return x * x * x * (x * (x * 6f - 15f) + 10f);
    }

    private void OnDestroy()
    {
        heartActive = false;
        if (filterVolume != null)
        {
            if (filterVolume.sharedProfile != null) Destroy(filterVolume.sharedProfile);
            Destroy(filterVolume.gameObject);
        }
    }
}
