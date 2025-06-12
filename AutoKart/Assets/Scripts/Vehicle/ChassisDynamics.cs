using UnityEngine;

public class ChassisDynamics
{
    private VehicleSpecs specs;

    public ChassisDynamics(VehicleSpecs vehicleSpecs)
    {
        this.specs = vehicleSpecs;
    }

    public (float frontLoad, float rearLoad) ComputeLongitudinalLoadTransfer(
        float accel
    )
    {
        float h = specs.cgHeight;
        float L = specs.wheelbase;
        float m = specs.TotalMass;

        float deltaF = (accel * h * m) / L;
        float FzFront = (m * 9.81f * specs.frontWeightDistribution) - deltaF;
        float FzRear = (m * 9.81f * (1f - specs.frontWeightDistribution)) + deltaF;

        return (FzFront, FzRear);
    }
}
