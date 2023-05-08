namespace Vectors
{
    public static class Vectors
    {

        public static double EuclidianLength(Dictionary<string, double> vector)
        {
            if (vector is null || vector.Count == 0)
                return 0;
            return Math.Sqrt(vector.Values.Aggregate(0d, (a, b) => a + b * b));
        }

        public static double ManhattanLength(Dictionary<string, double> vector)
        {
            return vector.Values.Sum();
        }

        public static double DotProduct(Dictionary<string, double> v1, Dictionary<string, double> v2)
        {
            return v1.Join(v2, x => x.Key, y => y.Key, (x, y) => x.Value * y.Value).Sum();
        }

        public static double CosineSimilarityMahattan(Dictionary<string, double> v1, Dictionary<string, double> v2)
        {
            return DotProduct(v1, v2) / (ManhattanLength(v1) * ManhattanLength(v2));
        }

        public static double CosineSimilarityEuclidian(Dictionary<string, double> v1, Dictionary<string, double> v2)
        {
            if (v1 is null || v2 is null || v1.Count == 0 || v2.Count == 0)
                return 0;
            return DotProduct(v1, v2) / (EuclidianLength(v1) * EuclidianLength(v2));
        }

    }
}
