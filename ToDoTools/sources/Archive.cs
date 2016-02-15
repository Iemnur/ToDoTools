using System;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;
using System.Text;

namespace ToDoTools.sources
{
    static class Archive
    {
        static private cGlobal global;

        static private List<cGlobal.st_index> readIndex(BinaryReader abr_file, int mode)
        {
            List<cGlobal.st_index> index = new List<cGlobal.st_index>();
            cGlobal.st_index elem;
            int nb = 0;

            if (mode == 1)
            {
                nb = (int)abr_file.ReadUInt32() / 4;
                abr_file.BaseStream.Position = 0;
            }
            else
                nb = (int)abr_file.ReadUInt32();

            for (int i = 0; i < nb; i++)
            {
                elem.id = i;
                elem.pos = abr_file.ReadUInt32();
                elem.size = 0;

                index.Add(elem);
            }

            return index;
        }

        static private void writeIndex(MemoryStream ms, List<cGlobal.st_index> index)
        {
            using (BinaryWriter bw = new BinaryWriter(ms, Encoding.ASCII, true))
            {
                foreach (cGlobal.st_index elem in index)
                {
                    bw.Write(elem.pos);
                }
            }

        }

        //----------------------------------------------------------------------------------------

        static private bool isArchive(BinaryReader abr_file, int ai_size)
        {
            int i_value_prec = 0;
            int i_value = 0;
            int i_nb = 0;
            long pos = abr_file.BaseStream.Position;

            try
            {
                i_value = (int)abr_file.ReadUInt32();
                i_nb = i_value / 4;
                
                if (i_nb <= 1 || i_value % 4 != 0 || ai_size <= 4)
                    return false;

                for (int i = 0; i < i_nb; i++)
                {
                    if (i_value < i_value_prec || i_value > ai_size)
                        return false;

                    i_value_prec = i_value;
                    i_value = (int)abr_file.ReadUInt32();
                }

                return true;
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message + Environment.NewLine, "ERROR");
                return false;
            }
            finally
            {
                abr_file.BaseStream.Position = pos;
            }
        }

        static private bool isArchiveNb(BinaryReader abr_file, int ai_size)
        {
            int i_value = 0;
            int i_value_prec = 0;
            int i_nb = 0;
            long pos = abr_file.BaseStream.Position;

            try
            {
                i_nb = (int)abr_file.ReadUInt32();

                if (i_nb == 0 || ai_size <= 4 || i_nb * 4 > ai_size)
                    return false;

                i_value = (int)abr_file.ReadUInt32();

                for (int i = 0; i < i_nb; i++)
                {
                    if (i_value < i_value_prec || i_value > ai_size)
                        return false;

                    i_value_prec = i_value;
                    i_value = (int)abr_file.ReadUInt32();
                }

                return true;
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message + Environment.NewLine, "ERROR");
                return false;
            }
            finally
            {
                abr_file.BaseStream.Position = pos;
            }
        }

        static private bool isCompressed(BinaryReader abr_file, int ai_size, ref int mode)
        {
            mode = 0;
            int size_in = 0;
            int size_out = 0;
            long pos = abr_file.BaseStream.Position;

            try
            {
                if (ai_size < 9)
                    return false;

                mode = (int)abr_file.ReadByte();
                size_in = (int)abr_file.ReadUInt32() + 9;
                size_out = (int)abr_file.ReadUInt32();

                while (size_in % 4 != 0 && size_in < ai_size)
                    size_in++;

                if (mode == 0 && size_in != size_out)
                    return false;

                if ((mode == 0x01 || mode == 0x03) && size_out <= size_in && ai_size < size_in)
                    return false;

                return true;
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message + Environment.NewLine, "ERROR");
                return false;
            }
            finally
            {
                abr_file.BaseStream.Position = pos;
            }
        }

        static private bool isTim(BinaryReader abr_file, int ai_size)
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
                Trace.WriteLine(ex.Message + Environment.NewLine, "ERROR");
                return false;
            }
            finally
            {
                abr_file.BaseStream.Position = pos;
            }
        }

        //----------------------------------------------------------------------------------------

        static private string getTypeOfFile(MemoryStream ams_file, int ai_pos, int ai_size)
        {
            using (BinaryReader br_file = new BinaryReader(ams_file, Encoding.ASCII, true))
            {
                return getTypeOfFile(br_file, ai_pos, ai_size);
            }
        }

        static private string getTypeOfFile(BinaryReader abr_file, int ai_pos, int ai_size)
        {
            string ext = ".bin";
            int mode = 0;

            abr_file.BaseStream.Position = ai_pos;

            if (isArchive(abr_file, ai_size))
                return ".arc1";
            else if (isArchiveNb(abr_file, ai_size))
                return ".arc2";
            else if (isCompressed(abr_file, ai_size, ref mode))
                return string.Format(".{0:00}", mode);
            else if (isTim(abr_file, ai_size))
                return ".tim";

            return ext;
        }

        //----------------------------------------------------------------------------------------

        static public void unpackFile(BinaryReader abr_file = null)
        {
            global = cGlobal.INSTANCE;
            List<cGlobal.st_index> index = new List<cGlobal.st_index>();
            byte[] b_file;
            string s_ext;
            string s_dir;
            bool b_fileDisk = false;
            int size;

            if (abr_file == null)
            { 
                abr_file = new BinaryReader(File.Open(global.SOURCE, FileMode.Open, FileAccess.Read));
                b_fileDisk = true;
            }

            Trace.WriteLine(string.Format("Extracting file {0}", global.SOURCE));
            Trace.Indent();

            try
            {
                if (isArchive(abr_file, (int)abr_file.BaseStream.Length))
                    index = readIndex(abr_file, 1);
                else if (isArchiveNb(abr_file, (int)abr_file.BaseStream.Length))
                    index = readIndex(abr_file, 2);
                else
                    throw new Exception("This file is not an archive");

                for (int i = 0; i < index.Count; i++)
                {
                    if (i == index.Count-1)
                        size = (int)(abr_file.BaseStream.Length - index[i].pos);
                    else
                        size = (int)(index[i + 1].pos - index[i].pos);

                    b_file = new byte[size];

                    string s_name = string.Format("{0:0000}", index[i].id);

                    s_ext = getTypeOfFile(abr_file, (int)index[i].pos, size);

                    Trace.WriteLineIf(global.ts_TypeTrace.TraceVerbose, string.Format("Extracting file {0}{1}", s_name, s_ext));

                    abr_file.BaseStream.Seek(index[i].pos, SeekOrigin.Begin);

                    using (MemoryStream ms_file = new MemoryStream())
                    {
                        abr_file.Read(b_file, 0, size);
                        ms_file.Write(b_file, 0, size);

                        s_dir = b_fileDisk ? Path.GetDirectoryName(global.SOURCE) + "/" : global.DIR_DUMP;
                        s_dir += Path.GetFileNameWithoutExtension(global.SOURCE) + "/" + s_name + s_ext;

                        global.writeFileToDisk(ms_file, s_dir);
                    }

                    b_file = null;
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message + Environment.NewLine, "ERROR");
            }
            finally
            {
                abr_file.Close();
                Trace.Unindent();
            }
        }

        static public void unpackBDat(MemoryStream ams_file, List<cGlobal.st_index> al_index, string as_path)
        {
            global = cGlobal.INSTANCE;
            byte[] b_file;
            string s_ext;

            Trace.Indent();

            try
            {
                foreach (cGlobal.st_index file in al_index)
                {
                    b_file = new byte[file.size];

                    string s_name = string.Format("{0:0000}", file.id);

                    Trace.WriteLineIf(global.ts_TypeTrace.TraceVerbose, string.Format("Extracting file {0}", s_name));

                    s_ext = getTypeOfFile(ams_file, (int)file.pos, (int)file.size);

                    ams_file.Seek(file.pos, SeekOrigin.Begin);

                    using (MemoryStream ms_file = new MemoryStream())
                    {
                        ams_file.Read(b_file, 0, (int)file.size);

                        ms_file.Write(b_file, 0, (int)file.size);

                        global.writeFileToDisk(ms_file, global.DIR_DUMP + as_path + s_name + s_ext);
                    }

                    b_file = null;
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message + Environment.NewLine, "ERROR");
            }
            finally
            {
                Trace.Unindent();
            }
        }

        //----------------------------------------------------------------------------------------

        static public void packFile()
        {
            global = cGlobal.INSTANCE;
            List<cGlobal.st_index> index_orig = new List<cGlobal.st_index>();
            List<cGlobal.st_index> index_dest = new List<cGlobal.st_index>();
            MemoryStream ms_dest = new MemoryStream();
            cGlobal.st_index elem;
            byte[] b_file;
            string s_pathname;
            string s_name;
            int size;

            Trace.WriteLine(string.Format("Inserting file {0}", global.SOURCE));
            Trace.Indent();

            try
            {
                using (BinaryReader br_file = new BinaryReader(File.Open(global.SOURCE, FileMode.Open, FileAccess.Read)))
                {
                    if (isArchive(br_file, (int)br_file.BaseStream.Length))
                        index_orig = readIndex(br_file, 1);
                    else if (isArchiveNb(br_file, (int)br_file.BaseStream.Length))
                        index_orig = readIndex(br_file, 2);
                    else
                        throw new Exception("This file is not an archive");

                    ms_dest.Position = index_orig.Count * 4;

                    for (int i = 0; i < index_orig.Count; i++)
                    {
                        if (i == index_orig.Count - 1)
                            size = (int)(br_file.BaseStream.Length - index_orig[i].pos);
                        else
                            size = (int)(index_orig[i + 1].pos - index_orig[i].pos);

                        s_name = string.Format("{0:0000}{1}", index_orig[i].id, getTypeOfFile(br_file, (int)index_orig[i].pos, size));
                        s_pathname = Path.GetDirectoryName(global.SOURCE) + "/" + Path.GetFileNameWithoutExtension(global.SOURCE) + "/" + s_name;

                        elem.id = i;
                        elem.pos = (UInt32)ms_dest.Position;

                        if (File.Exists(s_pathname))
                        {
                            Trace.WriteLineIf(global.ts_TypeTrace.TraceVerbose, string.Format("Inserting file {0}", s_name));

                            using (BinaryReader br = new BinaryReader(File.Open(s_pathname, FileMode.Open, FileAccess.Read)))
                            {
                                b_file = br.ReadBytes((int)br.BaseStream.Length);

                                elem.size = (UInt32)br.BaseStream.Length;
                            }
                        }
                        else
                        {
                            Trace.WriteLineIf(global.ts_TypeTrace.TraceVerbose, string.Format("Copying file {0}{1}", s_name));

                            br_file.BaseStream.Seek(index_orig[i].pos, SeekOrigin.Begin);

                            b_file = new byte[size];
                            br_file.Read(b_file, 0, size);

                            elem.size = (UInt32)size;
                        }

                        index_dest.Add(elem);
                        ms_dest.Write(b_file, 0, size);
                        global.padding(ms_dest, 4);

                        b_file = null;
                    }
                }

                ms_dest.Position = 0;
                writeIndex(ms_dest, index_dest);

                global.writeFileToDisk(ms_dest, global.DESTINATION);
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message + Environment.NewLine, "ERROR");
            }
            finally
            {
                Trace.Unindent();
            }
        }

        static public MemoryStream packBDat(MemoryStream ams_orig, List<cGlobal.st_index> al_orig, string as_path)
        {
            global = cGlobal.INSTANCE;
            MemoryStream ms_new = new MemoryStream();
            List<cGlobal.st_index> l_newIndex = new List<cGlobal.st_index>();
            cGlobal.st_index elem;
            byte[] b_file;
            string s_ext;
            int i_num = 0;

            Trace.Indent();

            try
            {
                foreach (cGlobal.st_index file in al_orig)
                {
                    string s_name = string.Format("{0:0000}", file.id);
                    string s_fullPath = global.DIR_DUMP + as_path + s_name;

                    Trace.WriteLineIf(global.ts_TypeTrace.TraceVerbose, string.Format("Inserting file {0}", s_name));

                    s_ext = getTypeOfFile(ams_orig, (int)file.pos, (int)file.size);
                    s_fullPath += s_ext;

                    if (File.Exists(s_fullPath))
                    {
                        using (BinaryReader br = new BinaryReader(File.Open(s_fullPath, FileMode.Open)))
                        {
                            b_file = br.ReadBytes((int)br.BaseStream.Length);

                            elem.id = i_num++;
                            elem.pos = (UInt32)ms_new.Position;
                            elem.size = (UInt32)br.BaseStream.Length;
                            l_newIndex.Add(elem);

                            ms_new.Write(b_file, 0, (int)br.BaseStream.Length);
                        }
                    }
                    else
                    {
                        b_file = new byte[file.size];

                        ams_orig.Seek(file.pos, SeekOrigin.Begin);

                        ams_orig.Read(b_file, 0, (int)file.size);

                        elem.id = i_num++;
                        elem.pos = (UInt32)ms_new.Position;
                        elem.size = file.size;
                        l_newIndex.Add(elem);

                        ms_new.Write(b_file, 0, (int)file.size);
                    }

                    b_file = null;
                }

                return ms_new;
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message + Environment.NewLine, "ERROR");
                return null;
            }
            finally
            {
                Trace.Unindent();
            }
        }

    }
}
