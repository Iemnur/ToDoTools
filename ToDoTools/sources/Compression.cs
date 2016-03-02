using System;
using System.IO;
using System.Diagnostics;
using System.Text;
using TalesOfLib;

namespace ToDoTools.sources
{
    static class Compression
    {
        static private cGlobal global;

        //----------------------------------------------------------------------------------------

        static public bool isCompressed(Stream as_file, ref int mode, int ai_size = -1)
        {
            mode = 0;
            long pos = as_file.Position;

            if (ai_size == -1)
                ai_size = (int)as_file.Length;

            try
            {
                if (ai_size < 9)
                    return false;

                using (BinaryReader br = new BinaryReader(as_file, Encoding.ASCII, true))
                {
                    return isCompressed(br, ref mode, ai_size);
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

        static public bool isCompressed(BinaryReader abr_file, ref int mode, int ai_size = -1)
        {
            global = cGlobal.INSTANCE;

            mode = 0;
            UInt32 size_in = 0;
            UInt32 size_out = 0;
            long pos = abr_file.BaseStream.Position;
            abr_file.BaseStream.Position = 0;

            if (ai_size == -1)
                ai_size = (int)abr_file.BaseStream.Length;

            Trace.WriteLineIf(global.ts_TypeTrace.TraceVerbose && global.VERBOSE >= global.verboseFull, "isCompressed - ENTER");
            Trace.Indent();

            try
            {
                Trace.WriteLineIf(global.ts_TypeTrace.TraceVerbose && global.VERBOSE >= global.verboseFull, string.Format("Size     : {0}", ai_size));

                if (ai_size < 9)
                    return false;

                mode = (int)abr_file.ReadByte();
                Trace.WriteLineIf(global.ts_TypeTrace.TraceVerbose && global.VERBOSE >= global.verboseFull, string.Format("Mode     : {0}", mode));

                if (mode != 0 && mode != 1 && mode != 3)
                    return false;

                size_in = abr_file.ReadUInt32() + 9;
                size_out = abr_file.ReadUInt32();

                Trace.WriteLineIf(global.ts_TypeTrace.TraceVerbose && global.VERBOSE >= global.verboseFull, string.Format("Size IN  : {0}", size_in));
                Trace.WriteLineIf(global.ts_TypeTrace.TraceVerbose && global.VERBOSE >= global.verboseFull, string.Format("Size OUT : {0}", size_out));

                while (size_in % 4 != 0 && size_in < ai_size)
                    size_in++;

                if (mode == 0 && size_in != size_out)
                    return false;

                if ((mode == 0x01 || mode == 0x03) && (size_out <= size_in || ai_size < size_in))
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
                Trace.Unindent();
                Trace.WriteLineIf(global.ts_TypeTrace.TraceVerbose && global.VERBOSE >= global.verboseFull, "isCompressed - EXIT");
            }
        }

        //----------------------------------------------------------------------------------------

        static public void decompFile()
        {
            global = cGlobal.INSTANCE;

            Trace.WriteLine("Decompressing");
            Trace.Indent();

            try
            {
                using (FileStream fs_in = new FileStream(global.SOURCE, FileMode.Open))
                using (MemoryStream ms_out = new MemoryStream())
                {
                    bool b_result = TOLib.decomp(fs_in, ms_out);
                    if (!b_result)
                        throw new Exception();

                    global.writeFileToDisk(ms_out, global.DESTINATION);
                }
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

        static public void decompFile(Stream as_in, Stream as_out)
        {
            global = cGlobal.INSTANCE;

            Trace.WriteLine("Decompressing");
            Trace.Indent();

            try
            {
                bool b_result = TOLib.decomp(as_in, as_out);
                if (!b_result)
                    throw new Exception();

                as_in.Position = 0;
                as_out.Position = 0;
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

        static public void compFile()
        {
            global = cGlobal.INSTANCE;

            Trace.WriteLine(string.Format("Compression of file {0}", global.SOURCE));
            Trace.Indent();

            try
            {
                using (FileStream fs_in = new FileStream(global.SOURCE, FileMode.Open))
                using (FileStream fs_out = new FileStream(global.DESTINATION, FileMode.Create))
                {
                    bool b_result = TOLib.comp(fs_in, fs_out, global.MODE);
                    if (!b_result)
                        throw new Exception();
                }
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

        static public void compFile(Stream as_in, Stream as_out)
        {
            global = cGlobal.INSTANCE;

            Trace.WriteLine(string.Format("Compression of file {0}", global.SOURCE));
            Trace.Indent();

            try
            {
                bool b_result = TOLib.comp(as_in, as_out, global.MODE);
                if (!b_result)
                    throw new Exception();

                as_out.Position = 0;
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
    }
}
