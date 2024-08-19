namespace SAAC.AttentionMeasures
{
    public class ObjectsRankerConfiguration
    {
        public double MaxScore { get; set; } = 100;
        public double MinScore { get; set; } = 0;
        public double A { get; set; } = 2;
        public double B { get; set; } = 10;
        public double C { get; set; } = 0.9;
        public double D { get; set; } = -1;
    }
}
