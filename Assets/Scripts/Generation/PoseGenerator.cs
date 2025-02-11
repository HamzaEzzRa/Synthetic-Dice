using UnityEngine;

public class PoseGenerator : MonoBehaviour
{
    [SerializeField] private Randomizer[] randomizers;

    public void Generate()
    {
        DiceRandomizer.RandomizedDice.Clear();

        foreach (Randomizer randomizer in randomizers)
        {
            if (randomizer != null)
            {
                randomizer.Randomize();
            }
        }
    }
}
