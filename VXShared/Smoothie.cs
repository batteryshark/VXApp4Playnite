using System;
using System.Runtime.InteropServices;
using System.Text;

namespace VXShared
{
    public static class Smoothie
    {
        [DllImport("libsmoothie", CharSet = CharSet.Ansi)]
        private static extern int smoothie_create(string path_to_mapfile, string path_to_root, string path_to_persistence);
        [DllImport("libsmoothie", CharSet = CharSet.Ansi)]
        private static extern int smoothie_destroy(string path_to_root);
        [DllImport("libsmoothie", CharSet = CharSet.Ansi)]
        private static extern int smoothie_resolve(string path_to_root, string virtual_path, StringBuilder sbout_path);

        public static Boolean Create(string path_to_mapfile, string path_to_root, string path_to_persistence)
        {
            return Convert.ToBoolean(smoothie_create(path_to_mapfile, path_to_root, path_to_persistence));
        }

        public static Boolean Destroy(string path_to_root)
        {
            return Convert.ToBoolean(smoothie_destroy(path_to_root));
        }

        public static Boolean Resolve(string path_to_root, string virtual_path, out string out_path)
        {
            out_path = "";
            StringBuilder cb = new StringBuilder(1024);
            Boolean status = Convert.ToBoolean(smoothie_resolve(path_to_root, virtual_path, cb));
            if (status)
            {
                out_path = cb.ToString();
            }
            return status;
        }
    }
}
