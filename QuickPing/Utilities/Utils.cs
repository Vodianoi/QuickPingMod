using System.IO;

namespace QuickPing.Utilities
{
    internal static class DataUtils
    {

        public static byte[] ReadAllBytes(this BinaryReader reader)
        {
            const int bufferSize = 4096;
            using (var ms = new MemoryStream())
            {
                byte[] buffer = new byte[bufferSize];
                int count;
                while ((count = reader.Read(buffer, 0, buffer.Length)) != 0)
                    ms.Write(buffer, 0, count);
                return ms.ToArray();
            }
        }
        public static bool Compare(this Minimap.PinData x, Minimap.PinData y)
        {
            return x.m_pos == y.m_pos
                && x.m_name == y.m_name;
        }


    }
}
