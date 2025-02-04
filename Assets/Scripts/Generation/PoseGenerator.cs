using UnityEngine;

public class PoseGenerator : MonoBehaviour
{
    [SerializeField] private Randomizer[] randomizers;

    [SerializeField, FloatRangeSlider(0f, 1f)] private FloatRange ambientColorIntensityRange;

    public void Generate()
    {
        DiceRandomizer.RandomizedDice.Clear();

        foreach (Randomizer randomizer in randomizers)
        {
            randomizer.Randomize();
        }

        float ambientColorIntensity = ambientColorIntensityRange.RandomInRange;
        RenderSettings.ambientLight = new Color(
            ambientColorIntensity,
            ambientColorIntensity,
            ambientColorIntensity,
            1f
        );
    }
}
