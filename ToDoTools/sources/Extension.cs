using System.IO;

namespace ToDoTools.sources
{
    public static class Extension
    {
        /// <summary>
        /// Read bytes from stream and write it in another stream
        /// </summary>
        /// <param name="source">Stream where read bytes</param>
        /// <param name="destination">Stream where write bytes</param>
        /// <param name="size">Size of bytes to copy</param>
        /// <param name="position">Position in the source stream where begin to read</param>
        public static void CopyTo(this Stream source, Stream destination, long size, long position = -1)
        {
            byte[] bytes = new byte[size];
            
            if (position != -1)
                source.Position = position;

            source.Read(bytes, 0, (int)size);
            destination.Write(bytes, 0, (int)size);
        }

        /// <summary>
        /// Read bytes from another stream and write it in the stream
        /// </summary>
        /// <param name="destination">Stream where write bytes</param>
        /// <param name="source">Stream where read bytes</param>
        /// <param name="size">Size of bytes to copy</param>
        /// <param name="position">Position in the source stream where begin to read</param>
        public static void CopyFrom(this Stream destination, Stream source, long size, long position = -1)
        {
            byte[] bytes = new byte[size];

            if (position != -1)
                source.Position = position;

            source.Read(bytes, 0, (int)size);
            destination.Write(bytes, 0, (int)size);
        }
    }
}
