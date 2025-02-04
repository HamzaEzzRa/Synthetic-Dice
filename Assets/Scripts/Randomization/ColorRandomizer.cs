using UnityEngine;

[RequireComponent(typeof(Renderer))]
public class ColorRandomizer : Randomizer
{
    [SerializeField] private Color[] randomColors;

    public Renderer Renderer
    {
        get
        {
            if (renderer == null)
            {
                renderer = GetComponent<Renderer>();
            }

            return renderer;
        }
    }

    private new Renderer renderer;

    public override void Randomize()
    {
        Color randomColor = randomColors[Random.Range(0, randomColors.Length)];
        Renderer.sharedMaterial.color = randomColor;
    }
}
