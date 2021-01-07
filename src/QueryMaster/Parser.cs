using System;
using System.Linq;
using System.Text;

namespace QueryMaster
{
    class Parser
    {
        private byte[] Data = null;
        private int CurrentPosition = -1;
        private int LastPosition;

        internal bool HasUnParsedBytes
        {
            get { return CurrentPosition < LastPosition; }
        }

        internal Parser(byte[] data)
        {
            Data = data;
            CurrentPosition = -1;
            LastPosition = Data.Length - 1;
        }

        internal byte ReadByte()
        {
            CurrentPosition++;
            if (CurrentPosition > LastPosition)
                throw new ParseException("Index was outside the bounds of the byte array.");
            return Data[CurrentPosition];

        }

        internal short ReadShort()
        {
            CurrentPosition++;
            if (CurrentPosition + 3 > LastPosition)
                throw new ParseException("Unable to parse bytes to short.");
            short num;
            if (!BitConverter.IsLittleEndian)
                Array.Reverse(Data, CurrentPosition, 2);
            num = BitConverter.ToInt16(Data, CurrentPosition);
            CurrentPosition++;
            return num;
        }

        internal int ReadInt()
        {
            CurrentPosition++;
            if (CurrentPosition + 3 > LastPosition)
                throw new ParseException("Unable to parse bytes to int.");
            if (!BitConverter.IsLittleEndian)
                Array.Reverse(Data, CurrentPosition, 4);
            int num = BitConverter.ToInt32(Data, CurrentPosition);
            CurrentPosition += 3;
            return num;
        }

        internal float ReadFloat()
        {
            CurrentPosition++;
            if (CurrentPosition + 3 > LastPosition)
                throw new ParseException("Unable to parse bytes to float.");
            if (!BitConverter.IsLittleEndian)
                Array.Reverse(Data, CurrentPosition, 4);
            float Num = BitConverter.ToSingle(Data, CurrentPosition);
            CurrentPosition += 3;
            return Num;

        }

        internal string ReadString()
        {
            CurrentPosition++;
            int temp = CurrentPosition;
            while (Data[CurrentPosition] != 0x00)
            {
                CurrentPosition++;
                if (CurrentPosition > LastPosition)
                    throw new ParseException("Unable to parse bytes to string.");
            }
            return Encoding.UTF8.GetString(Data, temp, CurrentPosition - temp);
        }

        internal void Skip(byte count)
        {
            CurrentPosition += count;
            if (CurrentPosition > LastPosition)
                throw new ParseException("skip count was outside the bounds of the byte array.");
        }

        internal byte[] GetUnParsedData()
        {
            return Data.Skip(CurrentPosition + 1).ToArray();
        }

    }
}