using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace VXShared
{
    public static class Utils
    {
        // Not sure why this isn't a thing already, but...
        public static String GetEnvarWithDefault(String key_name, String default_value)
        {
            String result_value = Environment.GetEnvironmentVariable(key_name);
            if (string.IsNullOrEmpty(result_value)) { return default_value; }
            return result_value;
        }

        // The Actual Generation Code for our AppCode - Inspired by Mario Maker 2!
        public static String GenerateAppCode(string in_path)
        {
            string code_pool = "0123456789ABCDEFGHJKLMNPQRSTUVWXY";
            var hash = new SHA1Managed().ComputeHash(Encoding.UTF8.GetBytes(in_path));
            String cv = "";
            foreach (byte val in hash)
            {
                cv += code_pool[val % code_pool.Length];
            }
            return cv.Substring(0, 3) + "-" + cv.Substring(3, 3) + "-" + cv.Substring(6, 3);
        }

        public static void CopyFilesRecursively(string sourcePath, string targetPath)
        {
            //Now Create all of the directories
            foreach (string dirPath in Directory.GetDirectories(sourcePath, "*", SearchOption.AllDirectories))
            {
                Directory.CreateDirectory(dirPath.Replace(sourcePath, targetPath));
            }

            //Copy all the files & Replaces any files with the same name
            foreach (string newPath in Directory.GetFiles(sourcePath, "*.*", SearchOption.AllDirectories))
            {
                File.Copy(newPath, newPath.Replace(sourcePath, targetPath), true);
            }
        }

        // Derives AppCode from Directory Name
        public static String DeriveAppCode(String path_to_app)
        {
            return GenerateAppCode(Path.GetFileName(path_to_app).Replace(".vxapp", ""));
        }

        public static String GetPipeName(String path_to_app)
        {
            return "VX_" + DeriveAppCode(path_to_app);
        }

        static int SearchBytes(byte[] haystack, byte[] needle)
        {
            var len = needle.Length;
            var limit = haystack.Length - len;
            for (var i = 0; i <= limit; i++)
            {
                var k = 0;
                for (; k < len; k++)
                {
                    if (needle[k] != haystack[i + k]) break;
                }
                if (k == len) return i;
            }
            return -1;
        }

        public static int DetectArch(String path_to_executable)
        {
            if (!File.Exists(path_to_executable))
            {
                return 64;
            }
            byte[] pe_new = { 0x50, 0x45, 0x00, 0x00, 0x64 };
            byte[] pe_old = { 0x50, 0x45, 0x00, 0x00, 0x4C };

            FileStream fsSource = new FileStream(path_to_executable, FileMode.Open, FileAccess.Read);
            byte[] file_data = new byte[0x1000];
            fsSource.Read(file_data, 0, file_data.Length);
            fsSource.Close();
            if (SearchBytes(file_data, pe_new) != -1) { return 64; }
            // This could be 32 or 64bit still via 32bit Word Machine...
            int offset = SearchBytes(file_data, pe_old);
            if (offset != -1)
            {
                offset += 0x16;
                short characteristics = BitConverter.ToInt16(file_data, offset);
                if (((characteristics & 0x0100) > 0)) { return 32; }
                return 64;
            }
            // Linux Handling
            if (file_data[0x12] == 0x03) { return 32; }
            if (file_data[0x12] == 0x3E) { return 64; }
            return 64;
        }
    }
}
