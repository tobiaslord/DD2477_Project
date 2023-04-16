static class Utility {
    public static float ParseDecimalString(string val) {
        val = val.Replace('.', ',');
        if (float.TryParse(val, out float result)) {
            return result;
        }
        return -1F;
    }
    public static int ParseRatingCount(string val) {
        val = val.Replace("rating", "");
        return ParseIntString(val);
    }
    public static int ParseReviewCount(string val) {
        val = val.Replace("review", "");
        return ParseIntString(val);
    }
    public static int ParseIntString(string val) {
        val = val.Replace("s", "").Replace(",", "").Trim();
        if (int.TryParse(val, out int result)) {
            return result;
        }
        return -1;
    }
}