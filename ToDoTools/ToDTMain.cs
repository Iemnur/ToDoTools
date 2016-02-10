using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using CRH.Framework.Disk;
using CRH.Framework.Disk.DataTrack;
using ToDoTools.sources;
using TalesOfComp;

namespace ToDoTools
{
    class ToDTMain
    {
        static TextWriterTraceListener twtl_trace;
        static ConsoleTraceListener ctl_trace;
        static Stopwatch sw_watch;

        static void Main(string[] args)
        {
            StreamWriter sw = new StreamWriter("ToDoTools.log");
            twtl_trace = new TextWriterTraceListener(sw);
            ctl_trace = new ConsoleTraceListener();
            Trace.Listeners.Add(twtl_trace);
            Trace.Listeners.Add(ctl_trace);
            Trace.AutoFlush = true;
            sw_watch = new Stopwatch();

            try
            {
                if (args.Length != 2 && args.Length != 3)
                    throw new Exception("Incorrect parameters !");

                sw_watch.Start();
                
                if (args[0] == "-e")
                    extractFromIso(args[1]);
                else if (args[0] == "-u")
                    Archive.unpackFile(args[1]);
                else
                    throw new Exception(string.Format("Unknow action {0}", args[0]));
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message + Environment.NewLine, "ERROR");
                usage();
                return;
            }
            finally
            {
                sw_watch.Stop();
                Trace.WriteLine(string.Format("Terminated. Execution time : {0}", sw_watch.Elapsed));
            }
        }

        public static void usage()
        {
            Trace.WriteLine("Usage : ToDoTools.exe <action> <file> [mode]");
            Trace.WriteLine("");
            Trace.WriteLine("Actions :");
            Trace.WriteLine("  -e      : Extract files from image disk");
            Trace.WriteLine("  -u      : Unpack file");
            Trace.WriteLine("  -p      : Pack file");
            Trace.WriteLine("  -d      : Decompress file");
            Trace.WriteLine("  -c      : compress file");
            Trace.WriteLine("<file>    : Pathname of the file");
            Trace.WriteLine("<mode>    : Compression (-c) mode (0, 1, 3) or Archive (-p) mode (1, 2)");
        }

        /// <summary>
        /// Ouvre un fichier image (iso) et retourne le track 1
        /// </summary>
        /// <param name="as_isoName">Chemin de l'iso</param>
        /// <returns></returns>
        private static DataTrackReader openIso(string as_isoName)
        {
            if (!File.Exists(as_isoName))
            {
                Trace.WriteLine("Unknown file : {0}", as_isoName);
                return null;
            }

            DiskReader dr_iso = DiskReader.InitFromIso(as_isoName, DiskFileSystem.ISO9660, DataTrackMode.MODE2_XA);

            return (DataTrackReader)dr_iso.DataTrack;
        }

        /// <summary>
        /// Récupère un index présent dans le SLUS
        /// </summary>
        /// <param name="dtr_track">Track 1 de l'iso</param>
        /// <param name="i_position">Adresse de l'index dans le fichier SLUS</param>
        /// <param name="nb">Nombre de pointeurs de l'index</param>
        /// <returns></returns>
        private static List<Global.st_index> readSlusIndex(DataTrackReader dtr_track, int i_position, int nb)
        {
            List<Global.st_index> index = new List<Global.st_index>();

            Stream st_file = dtr_track.ReadFile("/SLUS_006.26");

            using (BinaryReader br_file = new BinaryReader(st_file))
            {
                br_file.BaseStream.Seek(i_position, SeekOrigin.Begin);

                Global.st_index elem;

                for (int i = 0; i < nb; i++)
                {
                    elem.id = i;
                    elem.pos = br_file.ReadUInt32();
                    elem.size = br_file.ReadUInt32();

                    index.Add(elem);
                }
            }

            return index;
        }

        /// <summary>
        /// Extract all files for image disk and unpack those are archive
        /// </summary>
        /// <param name="as_isoName">Pathnam of the file</param>
        /// <returns></returns>
        public static bool extractFromIso(string as_isoName)
        {
            DataTrackReader t_track = openIso(as_isoName);

            try
            {
                t_track.ReadVolumeDescriptors();
                t_track.BuildIndex();

                foreach (DataTrackIndexEntry entry in t_track.FileEntries)
                {
                    Trace.WriteLine(string.Format("Extracting {0}...", entry.FullPath));

                    switch (Path.GetFileName(entry.FullPath))
                    {
                        case "B.DAT":
                            t_track.ExtractFile(entry.FullPath, Global.pcs_dirDump + entry.FullPath);
                            List<Global.st_index> index = readSlusIndex(t_track, 0xF3C00, 339);
                            MemoryStream st_file = (MemoryStream)t_track.ReadFile(entry.FullPath);
                            Archive.unpackBDat(st_file, index, entry.FullPath.Replace(".", "") + "/");
                            break;

                        default:
                            t_track.ExtractFile(entry.FullPath, Global.pcs_dirDump + entry.FullPath);
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message + Environment.NewLine, "ERROR");
                return false;
            }

            return true;
        }
    }
}
