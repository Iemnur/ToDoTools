using System;
using System.IO;
using System.Diagnostics;
using System.Text;

namespace ToDoTools.sources
{
    class cGlobal
    {
        private static cGlobal instance;
        public TraceSwitch ts_TypeTrace = new TraceSwitch("ToDToolsSwitch", "Switch");

        public const string cs_extArc1 = ".arc1";
        public const string cs_extArc2 = ".arc2";
        public const string cs_extBin = ".bin";
        public const string cs_extComp0 = ".00";
        public const string cs_extComp1 = ".01";
        public const string cs_extComp3 = ".03";
        public const string cs_extTim = ".tim";

        public int verboseLight { get; set; } = 1;
        public int verboseFull { get; set; } = 2;

        private string s_dirOut;
        private bool b_recursive;
        private bool b_log;
        private string s_fileLog;
        private string s_fileIn;
        private string s_fileOut;
        private string s_action;
        private int i_mode;
        private int i_verbose;

        public struct st_index
        {
            public int id;
            public UInt32 pos;
            public UInt32 size;
        }

        public struct st_filename
        {
            public string directory;
            public string filename;
            public string extension;
        }

        // CONSTRUCTOR
        private cGlobal()
        {
            b_recursive = false;
            b_log = false;
            s_fileLog = "ToDoTools.log";
            s_fileIn = "";
            s_fileOut = "";
            s_dirOut = "/DUMP";
            i_mode = -1;
            i_verbose = 0;

            s_action = "";
        }

        // SINGLETON
        public static cGlobal INSTANCE
        {
            get
            {
                if (instance == null)
                    instance = new cGlobal();

                return instance;
            }
        }


        // ACCESSEURS
        public string DIR_OUT     { get { return s_dirOut; } }
        public string FILELOG     { get { return s_fileLog; } }
        public string SOURCE      { get { return s_fileIn; } set { s_fileIn = value; } }
        public string DESTINATION { get { return s_fileOut; } }
        public string ACTION      { get { return s_action; } }
        public int MODE           { get { return i_mode; } }
        public bool LOG           { get { return b_log; } }
        public int VERBOSE        { get { return i_verbose; } }
        public bool RECURSIVE     { get { return b_recursive; } }


        // METHODS

        /// <summary>
        /// Read command line arguments and init globals variables
        /// </summary>
        /// <param name="args">List of arguments from the command line</param>
        /// <returns></returns>
        public void readArguments(string[] args)
        {
            bool result;
            int value;

            try
            {
                for (int i = 0; i < args.Length;)
                {
                    switch (args[i])
                    {
                        case "extract":
                            s_action = "EXTRACT";
                            i++;
                            break;

                        case "insert":
                            s_action = "INSERT";
                            i++;
                            break;

                        case "unpack":
                            s_action = "UNPACK";
                            i++;
                            break;

                        case "pack":
                            s_action = "PACK";
                            i++;
                            break;

                        case "decomp":
                            s_action = "DECOMP";
                            i++;
                            break;

                        case "comp":
                            s_action = "COMP";
                            i++;
                            break;

                        case "-d":
                            s_dirOut = args[i + 1];
                            i += 2;
                            break;

                        case "-i":
                            if (!File.Exists(args[i + 1]))
                                throw new ToDException(string.Format("The file doesn't exist : {0}", args[i + 1]));
                            s_fileIn = args[i + 1];
                            i += 2;
                            break;

                        case "-l":
                            b_log = true;
                            s_fileLog = args[i + 1];
                            StreamWriter sw = new StreamWriter(s_fileLog);
                            TextWriterTraceListener twtl_trace = new TextWriterTraceListener(sw);
                            Trace.Listeners.Add(twtl_trace);
                            i += 2;
                            break;

                        case "-m":
                            value = 0;
                            result = int.TryParse(args[i + 1], out value);
                            if (!result)
                                throw new ToDException(string.Format("Incorrect mode : {0}", args[i + 1]));
                            i_mode = value;
                            i += 2;
                            break;

                        case "-o":
                            s_fileOut = args[i + 1];
                            i += 2;
                            break;

                        case "-r":
                            b_recursive = true;
                            i++;
                            break;

                        case "-v":
                            value = 0;
                            result = int.TryParse(args[i + 1], out value);
                            if (!result)
                                throw new ToDException(string.Format("Incorrect verbose level : {0}", args[i + 1]));
                            ts_TypeTrace.Level = TraceLevel.Verbose;
                            i_verbose = value;
                            i += 2;
                            break;

                        default:
                            throw new ToDException("Incorrect parameters !");
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public cGlobal.st_filename splitPathName(string as_pathname)
        {
            cGlobal.st_filename infos;

            infos.directory = Path.GetDirectoryName(as_pathname);
            infos.filename = Path.GetFileNameWithoutExtension(as_pathname);
            infos.extension = Path.GetExtension(as_pathname);

            return infos;
        }

        public bool processFile(Stream as_file, string as_pathname, string as_name, string as_filename)
        {
            int mode = -1;

            Trace.WriteLineIf(ts_TypeTrace.TraceVerbose, string.Format("Processing file {0}", as_filename));
            Trace.Indent();

            try
            {
                if (Compression.isCompressed(as_file, ref mode))
                {
                    string s_ext = string.Format(".{0:00}", mode);

                    writeFileToDisk(as_file, as_pathname + as_name + s_ext);

                    using (MemoryStream ms_out = new MemoryStream())
                    {
                        Compression.decompFile(as_file, ms_out);

                        if (b_recursive)
                            processFile(ms_out, as_pathname, as_name, as_pathname + as_name + s_ext);
                        else
                            writeFileToDisk(ms_out, as_pathname + as_name + s_ext + cs_extBin);
                    }
                }
                else if (Archive.isArchive(as_file))
                {
                    writeFileToDisk(as_file, as_pathname + as_name + cs_extArc1);

                    Archive.unpackFile(as_file, as_pathname + as_name + "/", 1);
                }
                else if (Archive.isArchiveNb(as_file))
                {
                    writeFileToDisk(as_file, as_pathname + as_name + cs_extArc2);

                    Archive.unpackFile(as_file, as_pathname + as_name + "/", 2);
                }
                else if (isTim(as_file))
                    writeFileToDisk(as_file, as_pathname + as_name + cs_extTim);
                else
                    writeFileToDisk(as_file, as_pathname + as_name + cs_extBin);

                return true;
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                Trace.Unindent();
            }
        }

        static public bool isTim(Stream as_file, int ai_size = -1)
        {
            long pos = as_file.Position;

            if (ai_size == -1)
                ai_size = (int)as_file.Length;

            try
            {
                if (ai_size < 5)
                    return false;

                using (BinaryReader br = new BinaryReader(as_file, Encoding.ASCII, true))
                {
                    return isTim(br, ai_size);
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                as_file.Position = pos;
            }
        }

        static public bool isTim(BinaryReader abr_file, int ai_size)
        {
            int i_value = 0;
            long pos = abr_file.BaseStream.Position;

            try
            {
                if (ai_size < 5)
                    return false;

                i_value = (int)abr_file.ReadByte();

                if (i_value != 0x10)
                    return false;

                abr_file.BaseStream.Position = 4;

                i_value = abr_file.ReadByte();

                if (i_value != 0x08 && i_value != 0x09)
                    return false;

                return true;
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                abr_file.BaseStream.Position = pos;
            }
        }

        public string getTypeOfFile(Stream ams_file, int ai_pos, int ai_size)
        {
            using (BinaryReader br = new BinaryReader(ams_file, Encoding.ASCII, true))
            {
                return getTypeOfFile(br, ai_pos, ai_size);
            }
        }

        public string getTypeOfFile(BinaryReader abr_file, int ai_pos, int ai_size)
        {
            string ext = ".bin";
            int mode = 0;

            abr_file.BaseStream.Position = ai_pos;

            if (Archive.isArchive(abr_file, ai_size))
                return ".arc1";
            else if (Archive.isArchiveNb(abr_file, ai_size))
                return ".arc2";
            else if (Compression.isCompressed(abr_file, ref mode, ai_size))
                return string.Format(".{0:00}", mode);
            else if (isTim(abr_file, ai_size))
                return ".tim";

            return ext;
        }

        //----------------------------------------------------------------------------------------

        /// <summary>
        /// Write a stream on the disk and create directories
        /// </summary>
        /// <param name="ams_file">Stream to write</param>
        /// <param name="as_outPath">Path of the file on the disk</param>
        /// <returns></returns>
        public bool writeFileToDisk(Stream ams_file, string as_outPath)
        {
            try
            {
                Trace.WriteLineIf(ts_TypeTrace.TraceVerbose, string.Format("Creating file {0}", as_outPath));

                Directory.CreateDirectory(Path.GetDirectoryName(as_outPath));

                using (FileStream fs = new FileStream(as_outPath, FileMode.Create, FileAccess.Write))
                {
                    ams_file.Position = 0;
                    ams_file.CopyTo(fs);
                }
                ams_file.Position = 0;

                return true;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        /// <summary>
        /// Pad a file
        /// </summary>
        /// <param name="ms">MemoryStream to pad</param>
        /// <param name="modulo">Value of the modulo</param>
        public void padding(Stream ms, int modulo)
        {
            while (ms.Position % modulo != 0)
                ms.WriteByte(0);
        }
    }
}
