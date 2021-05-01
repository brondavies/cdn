using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;

namespace cdn
{
    public static class Extensions
    {
        public static string BlobName(this FileInfo value, string root) 
            => value.FullName.Replace(root, "").Replace('\\', '/').RemoveLeading("/");

        public static bool FileExists(this string value) => File.Exists(value);

        public static string FingerPrintName(this string value, FileStream stream)
        {
            using (var cryptoProvider = new SHA1CryptoServiceProvider())
            {
                string hash = BitConverter.ToString(cryptoProvider.ComputeHash(stream))
                    .ToLower()
                    .Replace("-", "")
                    .Substring(30);
                var dir = Path.GetDirectoryName(value).Replace('\\', '/');
                var ext = Path.GetExtension(value);
                var name = Path.GetFileNameWithoutExtension(value);
                return $"{dir}/{name}-{hash}{ext}";
            }
        }

        public static IEnumerable<FileInfo> GetFilesWithExtensions(this DirectoryInfo dirInfo, SearchOption searchOption, params string[] extensions)
        {
            var allowedExtensions = new HashSet<string>(extensions, StringComparer.OrdinalIgnoreCase);

            return dirInfo.EnumerateFiles("*.*", searchOption)
                          .Where(f => allowedExtensions.Contains(f.Extension));
        }

        public static bool IsEmpty(this string value) => string.IsNullOrEmpty(value);

        public static string RemoveLeading(this string value, string lead) 
            => value.StartsWith(lead) ? value.Substring(lead.Length) : value;
    }
}
