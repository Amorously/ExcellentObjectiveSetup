namespace EOS.Modules.World.NavigationSpline
{
    public class NavigationalSplineDefinition
    {
        public string WorldEventObjectFilter { get; set; } = string.Empty;

        public float RevealSpeedMulti { get; set; } = 1f;

        public List<Spline> Splines { get; set; } = new() { new() };
    }
    
    public class Spline
    {
        public Vec3 From { get; set; } = new();

        public Vec3 To { get; set; } = new();
    }
}
