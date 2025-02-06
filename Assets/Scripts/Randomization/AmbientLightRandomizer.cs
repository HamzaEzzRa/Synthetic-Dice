using UnityEngine;

public class AmbientLightRandomizer : Randomizer
{
    [SerializeField, FloatRangeSlider(0f, 1f)] private FloatRange ambientIntensityRange = new FloatRange(0.0f, 1.0f);

    public override void Randomize()
    {
        float ambientIntensity = ambientIntensityRange.RandomInRange;
        RenderSettings.ambientLight = new Color(
            ambientIntensity,
            ambientIntensity,
            ambientIntensity,
            1f
        );
    }
}
