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

        static private List<cGlobal.st_index> readIndex(Stream as_file, int mode)
        {
            List<cGlobal.st_index> index = new List<cGlobal.st_index>();
            cGlobal.st_index elem;
            int nb = 0;

            using (BinaryReader br = new BinaryReader(as_file, Encoding.ASCII, true))
            {
                if (mode == 1)
                {
                    nb = (int)br.ReadUInt32() / 4;
                    br.BaseStream.Position = 0;
                }
                else
                    nb = (int)br.ReadUInt32();

                for (int i = 0; i < nb; i++)
                {
                    elem.id = i;
                    elem.pos = br.ReadUInt32();
                    elem.size = 0;

                    index.Add(elem);
                }
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

        static public bool isArchive(Stream as_file, int ai_size = -1)
        {
            long pos = as_file.Position;

            if (ai_size == -1)
                ai_size = (int)as_file.Length;

            try
            {
                using (BinaryReader br = new BinaryReader(as_file, Encoding.ASCII, true))
                {
                    return isArchive(br, ai_size);
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

        static public bool isArchive(BinaryReader abr_file, int ai_size = -1)
        {
            int i_value_prec = 0;
            int i_value = 0;
            int i_nb = 0;
            long pos = abr_file.BaseStream.Position;

            if (ai_size == -1)
                ai_size = (int)abr_file.BaseStream.Length;

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
                throw ex;
            }
            finally
            {
                abr_file.BaseStream.Position = pos;
            }
        }

        static public bool isArchiveNb(Stream as_file, int ai_size = -1)
        {
            long pos = as_file.Position;

            if (ai_size == -1)
                ai_size = (int)as_file.Length;

            try
            {
                using (BinaryReader br = new BinaryReader(as_file, Encoding.ASCII, true))
                {
                    return isArchiveNb(br, ai_size);
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

        static public bool isArchiveNb(BinaryReader abr_file, int ai_size = -1)
        {
            int i_value = 0;
            int i_value_prec = 0;
            int i_nb = 0;
            long pos = abr_file.BaseStream.Position;

            if (ai_size == -1)
                ai_size = (int)abr_file.BaseStream.Length;

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
                throw ex;
            }
            finally
            {
                abr_file.BaseStream.Position = pos;
            }
        }

        //----------------------------------------------------------------------------------------

        static public void unpackFile(Stream as_in, string as_pathname, int mode)
        {
            global = cGlobal.INSTANCE;
            List<cGlobal.st_index> index = new List<cGlobal.st_index>();
            byte[] b_file;
            string s_ext;
            int size;

            try
            {
                index = readIndex(as_in, mode);

                for (int i = 0; i < index.Count; i++)
                {
                    if (i == index.Count - 1)
                        size = (int)(as_in.Length - index[i].pos);
                    else
                        size = (int)(index[i + 1].pos - index[i].pos);

                    b_file = new byte[size];

                    string s_name = string.Format("{0:0000}", index[i].id);

                    s_ext = global.getTypeOfFile(as_in, (int)index[i].pos, size);

                    as_in.Seek(index[i].pos, SeekOrigin.Begin);

                    using (MemoryStream ms_file = new MemoryStream())
                    {
                        ms_file.CopyFrom(as_in, size);
                        ms_file.Position = 0;

                        if (global.RECURSIVE)
                            global.processFile(ms_file, as_pathname, s_name, as_pathname + s_name + s_ext);
                        else
                            global.writeFileToDisk(ms_file, as_pathname + s_name + s_ext);
                    }

                    b_file = null;
                }
            }
            catch (Exception ex)
            {
                throw ex;
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

                    s_ext = global.getTypeOfFile(ams_file, (int)file.pos, (int)file.size);

                    using (MemoryStream ms_file = new MemoryStream())
                    {
                        ams_file.CopyTo(ms_file, file.size, file.pos);
                        ms_file.Position = 0;

                        if (global.RECURSIVE)
                            global.processFile(ms_file, as_path, s_name, as_path + s_name + s_ext);
                        else
                            global.writeFileToDisk(ms_file, as_path + s_name + s_ext);
                    }

                    b_file = null;
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
                    index_orig = readIndex(br_file, global.MODE);

                    ms_dest.Position = global.MODE == 1 ? index_orig.Count * 4 : index_orig.Count * 4 + 4;

                    for (int i = 0; i < index_orig.Count; i++)
                    {
                        if (i == index_orig.Count - 1)
                            size = (int)(br_file.BaseStream.Length - index_orig[i].pos);
                        else
                            size = (int)(index_orig[i + 1].pos - index_orig[i].pos);

                        s_name = string.Format("{0:0000}{1}", index_orig[i].id, global.getTypeOfFile(br_file, (int)index_orig[i].pos, size));
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
                if (global.MODE == 2)
                {
                    byte[] b = new byte[4];
                    b = BitConverter.GetBytes(index_dest.Count);
                    Array.Reverse(BitConverter.GetBytes(index_dest.Count));
                    ms_dest.Write(b, 0, 4);
                }
                writeIndex(ms_dest, index_dest);

                global.writeFileToDisk(ms_dest, global.DESTINATION);
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
                    string s_fullPath = global.DIR_OUT + as_path + s_name;

                    Trace.WriteLineIf(global.ts_TypeTrace.TraceVerbose, string.Format("Inserting file {0}", s_name));

                    s_ext = global.getTypeOfFile(ams_orig, (int)file.pos, (int)file.size);
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
                throw ex;
            }
            finally
            {
                Trace.Unindent();
            }
        }

        //----------------------------------------------------------------------------------------

    }
}
