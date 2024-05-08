using System.Diagnostics;
using System.IO;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using NLog;
using OpenMcdf;

namespace RevitScript.Runtime.Common {
    public static class CommonUtils {
        // private static object ProgressLock = new object();
        // private static int lastReport;

        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        [DllImport("ole32.dll")]
        private static extern int StgIsStorageFile([MarshalAs(UnmanagedType.LPWStr)] string pwcsName);

        public static bool VerifyFile(string filePath) {
            if (!string.IsNullOrEmpty(filePath))
                return File.Exists(filePath);
            return false;
        }

        public static bool VerifyPath(string path) {
            if (!string.IsNullOrEmpty(path))
                return Directory.Exists(path);
            return false;
        }

        public static bool VerifyPythonScript(string path) {
            return VerifyFile(path) && path.ToLower().EndsWith(".py");
        }

        // helper for deleting directories recursively
        // @handled @logs
        public static void DeleteDirectory(string targetDir, bool verbose = true)
        {
            if (!VerifyPath(targetDir)) return;
            if (verbose)
                logger.Debug("Recursive deleting directory \"{0}\"", targetDir);
            string[] files = Directory.GetFiles(targetDir);
            string[] dirs = Directory.GetDirectories(targetDir);

            try {
                foreach (string file in files) {
                    File.SetAttributes(file, FileAttributes.Normal);
                    File.Delete(file);
                }

                foreach (string dir in dirs) {
                    DeleteDirectory(dir, verbose: false);
                }

                Directory.Delete(targetDir, false);
            }
            catch (Exception ex) {
                throw new PyRevitException(string.Format("Error recursive deleting directory \"{0}\" | {1}",
                    targetDir, ex.Message));
            }
        }

        // helper for copying a directory recursively
        // @handled @logs
        public static void CopyDirectory(string sourceDir, string destDir) {
            EnsurePath(destDir);
            logger.Debug("Copying \"{0}\" to \"{1}\"", sourceDir, destDir);
            try {
                // create all of the directories
                foreach (string dirPath in Directory.GetDirectories(sourceDir, "*",
                    SearchOption.AllDirectories))
                    Directory.CreateDirectory(dirPath.Replace(sourceDir, destDir));

                // copy all the files & Replaces any files with the same name
                foreach (string newPath in Directory.GetFiles(sourceDir, "*.*",
                    SearchOption.AllDirectories))
                    File.Copy(newPath, newPath.Replace(sourceDir, destDir), true);
            }
            catch (Exception ex) {
                throw new PyRevitException(
                    $"Error copying \"{sourceDir}\" to \"{destDir}\" | {ex.Message}"
                );
            }
        }

        public static void EnsurePath(string path) {
            Directory.CreateDirectory(path);
        }

        public static void EnsureFile(string filePath) {
            EnsurePath(Path.GetDirectoryName(filePath));
            if (File.Exists(filePath)) return;
            var file = File.CreateText(filePath);
            file.Close();
        }

        public static string EnsureFileExtension(string filepath, string extension) => Path.ChangeExtension(filepath, extension);

        public static bool EnsureFileNameIsUnique(string targetDir, string fileName) {
            foreach (var subdir in Directory.GetDirectories(targetDir))
                if (Path.GetFileNameWithoutExtension(subdir).ToLower() == fileName.ToLower())
                    return false;

            foreach (var subFile in Directory.GetFiles(targetDir))
                if (Path.GetFileNameWithoutExtension(subFile).ToLower() == fileName.ToLower())
                    return false;

            return true;
        }

        public static string GetFileSignature(string filepath) {
            return Math.Abs(File.GetLastWriteTimeUtc(filepath).GetHashCode()).ToString();
        }

        public static WebClient GetWebClient() {
            if (CheckInternetConnection()) {
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                return new WebClient();
            }
            else
                throw new pyRevitNoInternetConnectionException();
        }

        public static HttpWebRequest GetHttpWebRequest(string url) {
            logger.Debug("Building HTTP request for: \"{}\"", url);
            if (CheckInternetConnection()) {
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                request.UserAgent = "pyrevit-cli";
                return request;
            }
            else
                throw new pyRevitNoInternetConnectionException();
        }

        public static string DownloadFile(string url, string destPath) {
            try
            {
                using var client = GetWebClient();
                client.Headers.Add("User-Agent", "pyrevit-cli");

                logger.Debug("Downloading \"{0}\"", url);
                client.DownloadFile(url, destPath);
            }
            catch (Exception dlEx) {
                logger.Debug("Error downloading file. | {0}", dlEx.Message);
                throw dlEx;
            }

            return destPath;
        }

        public static bool CheckInternetConnection() {
            try {
                using (var client = new WebClient())
                using (client.OpenRead("http://clients3.google.com/generate_204")) {
                    return true;
                }
            }
            catch {
                return false;
            }
        }

        public static byte[] GetStructuredStorageStream(string filePath, string streamName) {
            logger.Debug(string.Format("Attempting to read \"{0}\" stream from structured storage file at \"{1}\"",
                                       streamName, filePath));
            int res = StgIsStorageFile(filePath);

            if (res != 0) throw new NotSupportedException("File is not a structured storage file");
            CompoundFile cf = new CompoundFile(filePath);
            logger.Debug($"Found CF Root: {cf.RootStorage}");
            if (!cf.RootStorage.TryGetStream(streamName, out var foundStream)) return null;
            byte[] streamData = foundStream.GetData();
            cf.Close();
            return streamData;

        }

        public static void OpenUrl(string url, string logErrMsg = null) {
            if (CheckInternetConnection()) {
                if (!Regex.IsMatch(url, @"'^https*://'"))
                    url = "http://" + url;
                logger.Debug("Opening {0}", url);
                Process.Start(url);
            }
            else
            {
                logErrMsg ??= $"Error opening url \"{url}\"";

                logger.Error($"{logErrMsg}. No internet connection detected.");
            }
        }

        public static bool VerifyUrl(string url) {
            if (!CheckInternetConnection()) return true;
            HttpWebRequest request = GetHttpWebRequest(url);
            try {
                var response = request.GetResponse();
            }
            catch (Exception ex) {
                logger.Debug(ex);
                return false;
            }

            return true;
        }

        public static void OpenInExplorer(string resourcePath) {
            Process.Start("explorer.exe", resourcePath);
        }

        public static string NewShortUUID() {
            return Convert.ToBase64String(Guid.NewGuid().ToByteArray());
        }

        // https://en.wikipedia.org/wiki/ISO_8601
        // https://stackoverflow.com/a/27321188/2350244
        public static string GetISOTimeStamp(DateTime dtimeValue) => dtimeValue.ToString("yyyy-MM-ddTHH:mm:ssK");

        public static string GetISOTimeStampNow() => GetISOTimeStamp(DateTime.Now.ToUniversalTime());

        public static string GetISOTimeStampLocalNow() => GetISOTimeStamp(DateTime.Now);

        public static Encoding GetUTF8NoBOMEncoding() {
            // https://coderwall.com/p/o59zug/encoding-multiply-files-to-utf8-without-bom-with-c
            return new UTF8Encoding(false);
        }

        public static int FindBytes(byte[] src, byte[] find) {
            int index = -1;
            int matchIndex = 0;
            // handle the complete source array
            for (int i = 0; i < src.Length; i++) {
                if (src[i] == find[matchIndex]) {
                    if (matchIndex == (find.Length - 1)) {
                        index = i - matchIndex;
                        break;
                    }
                    matchIndex++;
                }
                else if (src[i] == find[0]) {
                    matchIndex = 1;
                }
                else {
                    matchIndex = 0;
                }

            }
            return index;
        }

        public static byte[] ReplaceBytes(byte[] src, byte[] search, byte[] repl) {
            byte[] dst = null;
            int index = FindBytes(src, search);
            if (index >= 0) {
                dst = new byte[src.Length - search.Length + repl.Length];
                // before found array
                Buffer.BlockCopy(src, 0, dst, 0, index);
                // repl copy
                Buffer.BlockCopy(repl, 0, dst, index, repl.Length);
                // rest of src array
                Buffer.BlockCopy(
                    src,
                    index + search.Length,
                    dst,
                    index + repl.Length,
                    src.Length - (index + search.Length));
                return dst;
            }
            return src;
        }

        // https://stackoverflow.com/a/49922533/2350244
        public static string GenerateRandomName(int len = 16) {
            Random r = new Random();
            string[] consonants = { "b", "c", "d", "f", "g", "h", "j", "k", "l", "m", "l", "n", "p", "q", "r", "s", "sh", "zh", "t", "v", "w", "x" };
            string[] vowels = { "a", "e", "i", "o", "u", "ae", "y" };
            string Name = "";
            Name += consonants[r.Next(consonants.Length)].ToUpperInvariant();
            Name += vowels[r.Next(vowels.Length)];
            int b = 2; //b tells how many times a new letter has been added. It's 2 right now because the first two letters are already in the name.
            while (b < len) {
                Name += consonants[r.Next(consonants.Length)];
                b++;
                Name += vowels[r.Next(vowels.Length)];
                b++;
            }
            return Name;
        }

        public static string GetProcessFileName() => Process.GetCurrentProcess().MainModule?.FileName;
        public static string GetProcessPath() => Path.GetDirectoryName(GetProcessFileName());
        public static string GetAssemblyPath<T>() => Path.GetDirectoryName(typeof(T).Assembly.Location);

        public static string GenerateSHA1Hash(string filePath)
        {
            // Use input string to calculate SHA1 hash
            using FileStream fs = new FileStream(filePath, FileMode.Open);
            using BufferedStream bs = new BufferedStream(fs);
            using var sha1 = new System.Security.Cryptography.SHA1Managed();
            StringBuilder sb = new StringBuilder();
            foreach (byte b in sha1.ComputeHash(bs)) {
                sb.Append(b.ToString("X2"));
            }
            return sb.ToString();
        }
    }
}

