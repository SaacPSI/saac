using MathNet.Spatial.Euclidean;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SAAC.Groups
{
    public class DistanceClusterDefinition
    {
        public List<uint> Ids { get; private set; }
        public Circle3D Area { get; private set; }

        public DistanceClusterDefinition(List<uint> ids, Circle3D area) 
        {
            Ids = ids;
            Area = area;
        }
    }
}
