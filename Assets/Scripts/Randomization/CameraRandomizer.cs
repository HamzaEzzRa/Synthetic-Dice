using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

public class CameraRandomizer : Randomizer
{
    [Header("Transform")]
    [SerializeField] private Transform cameraTarget;
    [SerializeField] private Vector3Range positionRange;
    [SerializeField] private Vector3Range targetPositionRange;

    [Header("Camera")]
    [SerializeField, FloatRangeSlider(0.00001f, 179f)] private FloatRange fieldOfViewRange;

    [Header("Post Processing")]
    [SerializeField] private PostProcessVolume volume;
    [SerializeField, FloatRangeSlider(0f, 1f)] private FloatRange grainIntensityRange;
    [SerializeField, FloatRangeSlider(0f, 3f)] private FloatRange grainSizeRange;
    [SerializeField, FloatRangeSlider(0f, 2f)] private FloatRange bloomIntensityRange;
    [SerializeField, FloatRangeSlider(0f, 2f)] private FloatRange bloomThresholdRange;

    public Vector3Range PositionRange => positionRange;

    public Camera Camera
    {
        get
        {
            if (cameraCache == null)
            {
                cameraCache = GetComponent<Camera>();
            }

            return cameraCache;
        }
    }

    public Grain GrainFX
    {
        get
        {
            if (grainFX == null)
            {
                grainFX = volume.sharedProfile.GetSetting<Grain>();
            }

            return grainFX;
        }
    }

    public Bloom BloomFX
    {
        get
        {
            if (bloomFX == null)
            {
                bloomFX = volume.sharedProfile.GetSetting<Bloom>();
            }
            return bloomFX;
        }
    }

    private Camera cameraCache;

    private Grain grainFX;
    private Bloom bloomFX;

    public override void Randomize()
    {
        RandomizeTransform();
        RandomizeCamera();
        RandomizePostProcessing();
    }

    private void RandomizeTransform()
    {
        Vector3 randomPosition = positionRange.RandomInRange;
        transform.localPosition = randomPosition;

        Vector3 randomTargetPosition = targetPositionRange.RandomInRange;
        cameraTarget.localPosition = randomTargetPosition;

        transform.LookAt(cameraTarget);
    }

    private void RandomizeCamera()
    {
        Camera.fieldOfView = fieldOfViewRange.RandomInRange;
    }

    private void RandomizePostProcessing()
    {
        GrainFX.intensity.Override(grainIntensityRange.RandomInRange);
        GrainFX.size.Override(grainSizeRange.RandomInRange);

        BloomFX.intensity.Override(bloomIntensityRange.RandomInRange);
        BloomFX.threshold.Override(bloomThresholdRange.RandomInRange);
    }
}
