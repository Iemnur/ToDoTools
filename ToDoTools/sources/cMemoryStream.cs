using System.IO;

namespace ToDoTools.sources
{
    public class cMemoryStream : MemoryStream
    {
        public cMemoryStream()
            : base()
        { }

        public void CopyTo(Stream destination, long size, long position = -1)
        {
            byte[] bytes = new byte[size];

            if (position != -1)
                this.Position = position;

            this.Read(bytes, 0, (int)size);
            destination.Write(bytes, 0, (int)size);
        }

        public void CopyFrom(Stream source, long size, long position = -1)
        {
            byte[] bytes = new byte[size];

            if (position != -1)
                source.Position = position;

            source.Read(bytes, 0, (int)size);
            this.Write(bytes, 0, (int)size);
        }
    }
}
