using System;
using System.IO;
using System.Diagnostics;

namespace ToDoTools.sources
{
    static class Global
    {
        public const string pcs_dirDump = "G:/Traductions/PSX/ToD/DUMP";

        public struct st_index
        {
            public int id;
            public UInt32 pos;
            public UInt32 size;
        }

        /// <summary>
        /// Ecrit un stream sur le disque
        /// </summary>
        /// <param name="ams_file">Stream à écrire</param>
        /// <param name="as_outPath">Chemin du fichier sur le disque</param>
        /// <returns></returns>
        public static bool writeFileToDisk(MemoryStream ams_file, string as_outPath)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(as_outPath));

                using (FileStream fs = new FileStream(as_outPath, FileMode.Create, FileAccess.Write))
                {
                    ams_file.Position = 0;
                    ams_file.CopyTo(fs);
                }
                ams_file.Seek(0, SeekOrigin.Begin);

                return true;
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message + Environment.NewLine, "ERROR");
                return false;
            }
        }
    }
}
