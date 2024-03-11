using MathNet.Spatial.Euclidean;

namespace SAAC.Groups
{
    public class SimplifiedFlockGroup
    {
        public uint Id { get; private set; }
        public List<uint> Constituants { get; private set; }
        public Circle2D Area { get; private set; }
        public Vector2D Direction { get; private set; }
        public double Velocity {  get; private set; } 

        public SimplifiedFlockGroup(uint id, List<uint> constituants, Circle2D area, Vector2D direction, double velocity) 
        {
            Id = id;
            Constituants = constituants;
            Area = area;
            Direction = direction;
            Velocity = velocity;
        }
    }
}
