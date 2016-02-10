using System;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;
using System.Text;

namespace ToDoTools.sources
{
    static class Archive
    {
        static private List<Global.st_index> readIndex(BinaryReader abr_file, int mode)
        {
            List<Global.st_index> index = new List<Global.st_index>();
            Global.st_index elem;
            int nb = 0;

            if (mode == 1)
            {
                nb = (int)abr_file.ReadUInt32() / 4;
            }
            else
            {
                nb = (int)abr_file.ReadUInt32();
                abr_file.BaseStream.Position = 0;
            }

            for (int i = 0; i < nb; i++)
            {
                elem.id = i;
                elem.pos = abr_file.ReadUInt32();
                elem.size = 0;

                index.Add(elem);
            }

            return index;
        }

        static private bool isArchive(BinaryReader abr_file, int ai_size)
        {
            int i_value_prec = 0;
            int i_value = 0;
            int i_nb = 0;

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
                abr_file.BaseStream.Position = 0;
            }
        }

        static private bool isArchiveNb(BinaryReader abr_file, int ai_size)
        {
            int i_value = 0;
            int i_value_prec = 0;
            int i_nb = 0;

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
                abr_file.BaseStream.Position = 0;
            }
        }

        static private bool isCompressed(BinaryReader abr_file, int ai_size, ref int mode)
        {
            mode = 0;
            int size_in = 0;
            int size_out = 0;

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
                abr_file.BaseStream.Position = 0;
            }
        }

        static private bool isTim(BinaryReader abr_file, int ai_size)
        {
            int i_value = 0;

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
                abr_file.BaseStream.Position = 0;
            }
        }

        static private string getTypeOfFile(MemoryStream ams_file, int ai_pos, int ai_size)
        {
            string ext = ".bin";
            int mode = 0;

            using (BinaryReader br_file = new BinaryReader(ams_file, Encoding.ASCII, true))
            {
                br_file.BaseStream.Position = ai_pos;

                if (isArchive(br_file, ai_size))
                    return ".arc1";
                else if (isArchiveNb(br_file, ai_size))
                    return ".arc2";
                else if (isCompressed(br_file, ai_size, ref mode))
                    return string.Format("{0:00}", mode);
                else if (isTim(br_file, ai_size))
                    return ".tim";
            }

            return ext;
        }

        static public void unpackFile(string as_pathname)
        {
            List<Global.st_index> index = new List<Global.st_index>();

            using (BinaryReader br_file = new BinaryReader(File.Open(as_pathname, FileMode.Open, FileAccess.Read)))
            {
                index = readIndex(br_file, 1);
            }
        }

        static public void unpackBDat(MemoryStream ams_file, List<Global.st_index> al_index, string as_path)
        {
            byte[] b_file;
            string s_ext;

            Trace.Indent();

            try
            {
                foreach (Global.st_index file in al_index)
                {
                    b_file = new byte[file.size];

                    string s_name = string.Format("{0:0000}", file.id);

                    Trace.WriteLine(string.Format("Extracting file {0}", s_name));

                    s_ext = getTypeOfFile(ams_file, (int)file.pos, (int)file.size);

                    ams_file.Seek(file.pos, SeekOrigin.Begin);

                    using (MemoryStream ms_file = new MemoryStream())
                    {
                        ams_file.Read(b_file, 0, (int)file.size);

                        ms_file.Write(b_file, 0, (int)file.size);

                        Global.writeFileToDisk(ms_file, Global.pcs_dirDump + as_path + s_name + s_ext);
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
    }
}
