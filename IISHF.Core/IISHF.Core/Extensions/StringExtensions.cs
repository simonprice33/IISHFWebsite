namespace IISHF.Extensions
{
    public static class StringExtensions
    {
        private static string delimiter = "|"; // Ensure this character does not appear in your strings

        public static string EncodeBase64MultipleTimes(this string input, int times = 8)
        {
            var result = input;
            for (var i = 0; i < times; i++)
            {
                var bytesToEncode = System.Text.Encoding.UTF8.GetBytes(result);
                result = Convert.ToBase64String(bytesToEncode);
            }
            return result;
        }

        public static string DecodeBase64MultipleTimes(this string input, int times = 8)
        {
            var result = input;
            for (var i = 0; i < times; i++)
            {
                var decodedBytes = Convert.FromBase64String(result);
                result = System.Text.Encoding.UTF8.GetString(decodedBytes);
            }
            return result;
        }

        public static string EncodeStringArray(string[] array, int times)
        {
            var combinedString = string.Join(delimiter, array);
            return EncodeBase64MultipleTimes(combinedString, times);
        }

        public static string[] DecodeStringArray(string encodedString, int times)
        {
            var decodedString = DecodeBase64MultipleTimes(encodedString, times);
            return decodedString.Split(new string[] { delimiter }, StringSplitOptions.None);
        }
    }
}
