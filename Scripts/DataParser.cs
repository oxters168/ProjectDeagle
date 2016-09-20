using System;
using System.Linq;
using System.IO;

public class DataParser
{
	public static bool bigEndian = false;
    private readonly static byte[] bitMasks = new byte[] { 0x80, 0xc0, 0xe0, 0xf0, 0xf8, 0xfc, 0xfe, 0xff };

    public static bool ReadBit(byte[] data, int bitIndex)
    {
        return ReadBits(data, 0, 1)[0] != 0;
    }
    public static byte[] ReadBits(byte[] data, int bitIndex, int bitLength)
    {
        byte[] collectedBits = new byte[(bitLength / 8) + (bitLength % 8 > 0 ? 1 : 0)];
        int dataIndex = bitIndex / 8;
        byte dataBitOffset = (byte)(bitIndex % 8);

        int currentBit = 0;
        for(int i = 0; i < collectedBits.Length; i++)
        {
            if (dataIndex + i >= data.Length) throw new Exception("Not enough data available to read");

            bool endEarly = false;
            byte bitsToRead = (byte)(8 - dataBitOffset);
            if (bitLength - currentBit + dataBitOffset <= 8) { bitsToRead = (byte)(bitLength - currentBit); endEarly = true; }
            byte bitMask = bitMasks[bitsToRead - 1];

            collectedBits[i] |= (byte)((data[dataIndex + i] & bitMask) >> (8 - bitsToRead));
            currentBit += bitsToRead;

            if(!endEarly && bitsToRead < 8)
            {
                if (dataIndex + i + 1 >= data.Length) throw new Exception("Not enough data available to read");

                bitsToRead = (byte)(8 - bitsToRead);
                bitMask = (byte)(Math.Pow(2, bitsToRead) - 1);

                collectedBits[i] |= (byte)((data[dataIndex + i + 1] & bitMask) << (8 - bitsToRead));
                currentBit += bitsToRead;
            }
        }

        if (bigEndian) collectedBits.Reverse();

        return collectedBits;
    }
    public static byte[] ReadBytes(byte[] original, int index, int length)
    {
        byte[] bytesRead = new byte[0];
        if (original != null && index > -1 && length > 0 && index + length - 1 < original.Length)
        {
            bytesRead = new byte[length];
            Array.Copy(original, index, bytesRead, 0, length);
            if (bigEndian) bytesRead.Reverse();
        }
        return bytesRead;
    }
    public static byte[] ReadBytes(Stream stream, int amount)
    {
        byte[] buffer = new byte[amount];
        stream.Read(buffer, 0, amount);
        if (bigEndian) buffer.Reverse();
        return buffer;
    }

    public static sbyte ReadSByte(Stream stream)
    {
        byte[] buffer = new byte[1];
        stream.Read(buffer, 0, 1);
        return Convert.ToSByte(buffer[0]);
    }
    public static byte ReadByte(Stream stream)
    {
        byte[] buffer = new byte[1];
        stream.Read(buffer, 0, 1);
        return buffer[0];
    }
    public static char ReadChar(Stream stream)
    {
        byte[] buffer = new byte[2];
        stream.Read(buffer, 0, 1);
        return BitConverter.ToChar(buffer, 0);
    }
    public static bool ReadBool(Stream stream)
    {
        return BitConverter.ToBoolean(ReadBytes(stream, 1), 0);
    }

    public static short ReadShort(Stream stream)
	{
		return BitConverter.ToInt16(ReadBytes(stream, 2), 0);
	}
    public static ushort ReadUShort(Stream stream)
	{
		return BitConverter.ToUInt16(ReadBytes(stream, 2), 0);
	}

    public static float ReadFloat(Stream stream)
    {
        return BitConverter.ToSingle(ReadBytes(stream, 4), 0);
    }
    public static int ReadInt(Stream stream)
	{
        return BitConverter.ToInt32(ReadBytes(stream, 4), 0);
        //return BitConverter.ToInt32(ReadBits(ReadBytes(stream, 4), 0, 4 * 8), 0);
    }
    public static uint ReadUInt(Stream stream)
	{
		return BitConverter.ToUInt32(ReadBytes(stream, 4), 0);
	}
    public static uint ReadUBitInt(byte[] data, int index, out int bitsRead)
    {
        uint uBitInt = BitConverter.ToUInt32(ReadBits(data, index, 6), 0);
        bitsRead = 6;
        if((uBitInt & 48) == 16)
        {
            uBitInt = (uBitInt & 15) | (BitConverter.ToUInt32(ReadBits(data, index + bitsRead, 4), 0) << 4);
            bitsRead += 4;
        }
        else if((uBitInt & 48) == 32)
        {
            uBitInt = (uBitInt & 15) | (BitConverter.ToUInt32(ReadBits(data, index + bitsRead, 8), 0) << 4);
            bitsRead += 8;
        }
        else if((uBitInt & 48) == 48)
        {
            uBitInt = (uBitInt & 15) | (BitConverter.ToUInt32(ReadBits(data, index + bitsRead, 32 - 4), 0) << 4);
            bitsRead += 32 - 4;
        }
        return uBitInt;
    }

    public static double ReadDouble(Stream stream)
    {
        return BitConverter.ToDouble(ReadBytes(stream, 8), 0);
    }
    public static long ReadLong(Stream stream)
	{
		return BitConverter.ToInt64(ReadBytes(stream, 8), 0);
	}
    public static ulong ReadULong(Stream stream)
    {
        return BitConverter.ToUInt64(ReadBytes(stream, 8), 0);
    }

    public static decimal ReadDecimal(Stream stream)
    {
        return new decimal(new int[] { ReadInt(stream), ReadInt(stream), ReadInt(stream), ReadInt(stream) }); //Big endian probably doesn't work, each individual int is flipped but their ordering is probably wrong
    }

    public static int ReadProtoInt(byte[] data, int index, out int bytesRead)
    {
        int protoInt = 0;
        bytesRead = 0;

        if (index > -1 && index < data.Length)
        {
            byte currentByte = 0;

            do
            {
                if (index + bytesRead < data.Length) currentByte = data[index + bytesRead];
                if (bytesRead < 4 || (bytesRead == 4 && ((currentByte & 0xf8) == 0 || (currentByte & 0xf8) == 0xf8)))
                    protoInt |= (currentByte & ~0x80) << (7 * bytesRead);
                bytesRead++;
            }
            while (bytesRead < 10 && (currentByte & 0x80) != 0);
        }

        return protoInt;
    }
    public static int ReadProtoInt(byte[] data, int index)
    {
        int bytesRead;
        return ReadProtoInt(data, index, out bytesRead);
    }
    public static int ReadProtoInt(Stream stream, out int bytesRead)
    {
        int protoInt = 0;
        byte currentByte;
        bytesRead = 0;

        do
        {
            currentByte = ReadByte(stream);
            if (bytesRead < 4 || (bytesRead == 4 && ((currentByte & 0xf8) == 0 || (currentByte & 0xf8) == 0xf8)))
                protoInt |= (currentByte & ~0x80) << (7 * bytesRead);
            bytesRead++;
        }
        while (bytesRead < 10 && (currentByte & 0x80) != 0);

        return protoInt;
    }
    public static int ReadProtoInt(Stream stream)
    {
        int bytesRead;
        return ReadProtoInt(stream, out bytesRead);
    }
    public static string ReadProtoString(byte[] data, int index, out int bytesRead)
    {
        int sizeOfProtoInt;
        int stringSize = ReadProtoInt(data, index, out sizeOfProtoInt);
        bytesRead = sizeOfProtoInt + stringSize;
        return System.Text.Encoding.UTF8.GetString(ReadBytes(data, index + sizeOfProtoInt, stringSize));
    }
    public static string ReadProtoString(byte[] data, int index)
    {
        int bytesRead;
        return ReadProtoString(data, index, out bytesRead);
    }
    public static string ReadProtoString(Stream stream, out int bytesRead)
    {
        int protoIntSize;
        int stringSize = ReadProtoInt(stream, out protoIntSize);
        bytesRead = protoIntSize + stringSize;
        return System.Text.Encoding.UTF8.GetString(ReadBytes(stream, stringSize));
    }
    public static string ReadProtoString(Stream stream)
    {
        int bytesRead;
        return ReadProtoString(stream, out bytesRead);
    }

    public static string ReadNullTerminatedString(Stream stream)
    {
        string builtString = "";
        char nextChar = '\0';
        do
        {
            if(stream.CanRead) nextChar = ReadChar(stream);
            if (nextChar != '\0') builtString += nextChar;
        }
        while (nextChar != '\0' && stream.CanRead);

        return builtString;
    }
    public static string ReadNullTerminatedString(byte[] data, int index, out int bytesRead)
    {
        bytesRead = 0;
        string builtString = "";
        char nextChar = '\0';
        do
        {
            nextChar = BitConverter.ToChar(data, index + bytesRead);
            if (nextChar != '\0') builtString += nextChar;
            bytesRead++;
        }
        while (nextChar != '\0' && index + bytesRead < data.Length);

        return builtString;
    }
    public static string ReadNullTerminatedString(byte[] data, int index)
    {
        int bytesRead;
        return ReadNullTerminatedString(data, index, out bytesRead);
    }
    public static string ReadDataTableString(byte[] data, int index, out int bytesRead)
    {
        bytesRead = 0;
        System.Collections.Generic.List<byte> builtString = new System.Collections.Generic.List<byte>();
        byte nextChar = 0;
        do
        {
            nextChar = data[index + bytesRead];
            if (nextChar != 0) builtString.Add(nextChar);
            bytesRead++;
        }
        while (nextChar != 0 && index + bytesRead < data.Length);

        return System.Text.Encoding.Default.GetString(builtString.ToArray());
    }
}