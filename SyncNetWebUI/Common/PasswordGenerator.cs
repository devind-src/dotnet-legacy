using System.Security.Cryptography;
using System.Text;

namespace SyncNetWasm.Common
{
    /// <summary>Client-side only — generates a password an admin can hand off to someone else
    /// (Add User / Reset Password). Not used for the user's own Ganti Password flow, where the
    /// user has to remember their own password rather than have one relayed to them.</summary>
    public static class PasswordGenerator
    {
        private const string Uppercase = "ABCDEFGHJKLMNPQRSTUVWXYZ"; // no I/O — avoid look-alikes
        private const string Lowercase = "abcdefghijkmnpqrstuvwxyz";
        private const string Digits = "23456789"; // no 0/1
        private const string Symbols = "!@#$%^&*-_+=";
        private const string AllChars = Uppercase + Lowercase + Digits + Symbols;

        public static string Generate(int length = 12)
        {
            if (length < 8) length = 8;

            var chars = new char[length];
            // Guarantee at least one of each character class so the result always satisfies
            // "reasonably strong", regardless of what the random draw below produces.
            chars[0] = PickFrom(Uppercase);
            chars[1] = PickFrom(Lowercase);
            chars[2] = PickFrom(Digits);
            chars[3] = PickFrom(Symbols);

            for (var i = 4; i < length; i++)
                chars[i] = PickFrom(AllChars);

            Shuffle(chars);
            return new string(chars);
        }

        private static char PickFrom(string alphabet) => alphabet[RandomNumberGenerator.GetInt32(alphabet.Length)];

        private static void Shuffle(char[] chars)
        {
            for (var i = chars.Length - 1; i > 0; i--)
            {
                var j = RandomNumberGenerator.GetInt32(i + 1);
                (chars[i], chars[j]) = (chars[j], chars[i]);
            }
        }
    }
}
