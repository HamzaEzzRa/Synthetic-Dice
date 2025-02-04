using UnityEngine;

public class LightingRandomizer : Randomizer
{
    [SerializeField, FloatRangeSlider(0f, 2f)] private FloatRange intensityRange;
    [SerializeField] private Vector3Range positionRange;
    [SerializeField] private Vector3Range eulerAnglesRange;

    [SerializeField] private bool randomizePosition = true;
    [SerializeField] private bool randomizeRotation = true;

    public Light Light
    {
        get
        {
            if (lightSource == null)
            {
                lightSource = GetComponent<Light>();
            }

            return lightSource;
        }
    }

    private Light lightSource;

    public override void Randomize()
    {
        RandomizeIntensity();

        if (randomizePosition)
        {
            RandomizePosition();
        }

        if (randomizeRotation)
        {
            RandomizeRotation();
        }
    }

    private void RandomizeIntensity()
    {
        float randomIntensity = intensityRange.RandomInRange;
        Light.intensity = randomIntensity;
    }

    private void RandomizePosition()
    {
        Vector3 randomPosition = positionRange.RandomInRange;
        transform.localPosition = randomPosition;
    }

    private void RandomizeRotation()
    {
        Vector3 randomEulerAngles = eulerAnglesRange.RandomInRange;
        transform.localEulerAngles = randomEulerAngles;
    }
}
