using System;
using System.IO;
using System.Diagnostics;

namespace ToDoTools.sources
{
    class cGlobal
    {
        private static cGlobal instance;
        public TraceSwitch ts_TypeTrace = new TraceSwitch("ToDToolsSwitch", "Switch");
        
        private string s_dirDump;
        private bool b_verbose;
        private bool b_recursive;
        private bool b_log;
        private string s_fileLog;
        private string s_fileIn;
        private string s_fileOut;
        private int i_mode;

        private bool b_extract;
        private bool b_insert;
        private bool b_unpack;
        private bool b_pack;
        private bool b_decomp;
        private bool b_comp;

        public struct st_index
        {
            public int id;
            public UInt32 pos;
            public UInt32 size;
        }



        // CONSTRUCTOR
        private cGlobal()
        {
            s_dirDump = "/DUMP/";
            b_verbose = false;
            b_recursive = false;
            b_log = false;
            s_fileLog = "ToDoTools.log";
            s_fileIn = "";
            s_fileOut = "";
            i_mode = 1;
            
            b_extract = false;
            b_insert = false;
            b_unpack = false;
            b_pack = false;
            b_decomp = false;
            b_comp = false;
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
        public string DIR_DUMP    { get { return s_dirDump; } }
        public string FILELOG     { get { return s_fileLog; } }
        public string SOURCE      { get { return s_fileIn; } }
        public string DESTINATION { get { return s_fileOut; } }
        public int MODE           { get { return i_mode; } }
        public bool LOG           { get { return b_log; } }
        public bool VERBOSE       { get { return b_verbose; } }
        public bool RECURSIVE     { get { return b_recursive; } }
        public bool EXTRACT       { get { return b_extract; } }
        public bool INSERT        { get { return b_insert; } }
        public bool UNPACK        { get { return b_unpack; } }
        public bool PACK          { get { return b_pack; } }
        public bool DECOMP        { get { return b_decomp; } }
        public bool COMP          { get { return b_comp; } }


        // FUNCTIONS
        public bool readArguments(string[] args)
        {
            bool result;

            for (int i = 0; i < args.Length;)
            {
                switch (args[i])
                {
                    case "extract":
                        b_extract = true;
                        i++;
                        break;

                    case "insert":
                        b_insert = true;
                        i++;
                        break;

                    case "unpack":
                        b_unpack = true;
                        i++;
                        break;

                    case "pack":
                        b_pack = true;
                        i++;
                        break;

                    case "decomp":
                        b_decomp = true;
                        i++;
                        break;

                    case "comp":
                        b_comp = true;
                        i++;
                        break;

                    case "-v":
                        b_verbose = true;
                        ts_TypeTrace.Level = TraceLevel.Verbose;
                        i++;
                        break;

                    case "-r":
                        b_recursive = true;
                        i++;
                        break;

                    case "-l":
                        b_log = true;
                        s_fileLog = args[i + 1];
                        StreamWriter sw = new StreamWriter(s_fileLog);
                        TextWriterTraceListener twtl_trace = new TextWriterTraceListener(sw);
                        Trace.Listeners.Add(twtl_trace);
                        i += 2;
                        break;

                    case "-i":
                        if (!File.Exists(args[i + 1]))
                            return false;
                        s_fileIn = args[i + 1];
                        s_dirDump = Path.GetDirectoryName(s_fileIn);
                        i += 2;
                        break;

                    case "-o":
                        s_fileOut = args[i + 1];
                        i += 2;
                        break;

                    case "-m":
                        int value;
                        result = int.TryParse(args[i + 1], out value);
                        if (!result)
                            return false;
                        i_mode = value;
                        i += 2;
                        break;

                    default:
                        Trace.WriteLine("Incorrect parameters");
                        return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Write a stream on the disk and create directories
        /// </summary>
        /// <param name="ams_file">Stream to write</param>
        /// <param name="as_outPath">Path of the file on the disk</param>
        /// <returns></returns>
        public bool writeFileToDisk(MemoryStream ams_file, string as_outPath)
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

        /// <summary>
        /// Pad a file
        /// </summary>
        /// <param name="ms">MemoryStream to pad</param>
        /// <param name="modulo">Value of the modulo</param>
        public void padding(MemoryStream ms, int modulo)
        {
            while (ms.Position % modulo != 0)
                ms.WriteByte(0);
        }
    }
}
