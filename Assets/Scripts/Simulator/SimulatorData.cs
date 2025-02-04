using UnityEngine;

[System.Serializable]
public class SimulatorData
{
    public Vector3Range positionRange;
    public Vector3Range targetPositionRange;
    public FloatRange fieldOfViewRange;
    public FloatRange ambientColorIntensityRange;
    public FloatRange grainIntensityRange;
    public FloatRange grainSizeRange;
    public FloatRange bloomIntensityRange;
    public FloatRange bloomThresholdRange;
}
