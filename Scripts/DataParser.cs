using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;

public class DataParser
{
	public static bool bigEndian = false;
    private readonly static byte[] bitMasks = new byte[] { 0xff, 0xfe, 0xfc, 0xf8, 0xf0, 0xe0, 0xc0, 0x80 };

    #region Base Reader Functions
    public static byte[] ReadBits(byte[] data, uint bitIndex, uint bitsToRead)
    {
        byte[] outputBytes = new byte[(bitsToRead / 8) + (bitsToRead % 8 > 0 ? 1 : 0)];
        byte bitOffset = (byte)(bitIndex % 8);

        uint bitsRead = 0;
        for (uint outputByteIndex = 0; outputByteIndex < outputBytes.Length; outputByteIndex++)
        {
            uint dataByteIndex = (bitIndex / 8) + outputByteIndex;

            outputBytes[outputByteIndex] |= (byte)((data[dataByteIndex] & bitMasks[bitOffset]) >> bitOffset); bitsRead += (byte)(8 - bitOffset); //Start reading bits of current byte
            if (bitsRead < bitsToRead && bitOffset > 0) { outputBytes[outputByteIndex] |= (byte)((data[dataByteIndex + 1] & ~bitMasks[bitOffset]) << (8 - bitOffset)); bitsRead += bitOffset; } //If we did not get an entire byte, continue reading bits
            if (bitsRead > bitsToRead) outputBytes[outputByteIndex] &= (byte)~bitMasks[bitsToRead % 8]; //Trim off excess bits
        }

        if (bigEndian) outputBytes.Reverse();
        return outputBytes.Length > 0 ? outputBytes : new byte[] { 0 };
    }
    public static byte[] ReadBits(byte[] data, uint bitIndex, uint bitsToRead, uint resultByteArraySize)
    {
        byte[] result = ReadBits(data, bitIndex, bitsToRead);
        if (result.Length < resultByteArraySize)
        {
            byte[] fixedResult = new byte[resultByteArraySize];
            for (uint i = 0; i < result.Length; i++)
                fixedResult[i] = result[i];
            result = fixedResult;
        }

        return result;
    }
    public static byte[] ReadBytes(byte[] data, uint byteIndex, uint bytesToRead)
    {
        byte[] bytesRead = new byte[0];
        if (data != null && bytesToRead > 0 && byteIndex + bytesToRead - 1 < data.Length)
        {
            bytesRead = new byte[bytesToRead];
            Array.Copy(data, byteIndex, bytesRead, 0, bytesToRead);
            if (bigEndian) bytesRead.Reverse();
        }
        return bytesRead;
    }
    public static byte[] ReadBytes(Stream stream, uint amount)
    {
        byte[] buffer = new byte[amount];
        stream.Read(buffer, 0, (int)amount);
        if (bigEndian) buffer.Reverse();
        return buffer;
    }
    #endregion

    #region 1 byte structures
    public static bool ReadBool(Stream stream)
    {
        return BitConverter.ToBoolean(ReadBytes(stream, 1), 0);
    }
    public static bool ReadBool(byte[] data, uint bitIndex, byte bitsToRead = 8)
    {
        if (bitsToRead > 8) bitsToRead = 8;
        if (bitsToRead < 1) bitsToRead = 1;
        return BitConverter.ToBoolean(ReadBits(data, bitIndex, bitsToRead), 0);
    }
    public static bool ReadBit(byte[] data, uint bitIndex)
    {
        return ReadBits(data, bitIndex, 1)[0] != 0;
    }
    public static sbyte ReadSByte(Stream stream)
    {
        byte[] buffer = new byte[1];
        stream.Read(buffer, 0, 1);
        return Convert.ToSByte(buffer[0]);
    }
    public static sbyte ReadSByte(byte[] data, uint bitIndex, byte bitsToRead = 8)
    {
        if (bitsToRead > 8) bitsToRead = 8;
        if (bitsToRead < 1) bitsToRead = 1;
        return (sbyte)ReadBits(data, bitIndex, bitsToRead)[0];
    }
    public static byte ReadByte(Stream stream)
    {
        byte[] buffer = new byte[1];
        stream.Read(buffer, 0, 1);
        return buffer[0];
    }
    public static byte ReadByte(byte[] data, uint bitIndex, byte bitsToRead = 8)
    {
        if (bitsToRead > 8) bitsToRead = 8;
        if (bitsToRead < 1) bitsToRead = 1;
        return ReadBits(data, bitIndex, bitsToRead)[0];
    }
    public static char ReadChar(Stream stream)
    {
        byte[] buffer = new byte[2];
        stream.Read(buffer, 0, 1);
        return BitConverter.ToChar(buffer, 0);
    }
    #endregion

    #region 2 byte structures
    public static short ReadShort(Stream stream)
	{
		return BitConverter.ToInt16(ReadBytes(stream, 2), 0);
	}
    public static short ReadShort(byte[] data, uint bitIndex, byte bitsToRead = 16)
    {
        if (bitsToRead > 16) bitsToRead = 16;
        if (bitsToRead < 1) bitsToRead = 1;
        return BitConverter.ToInt16(ReadBits(data, bitIndex, bitsToRead, 2), 0);
    }
    public static ushort ReadUShort(Stream stream)
	{
		return BitConverter.ToUInt16(ReadBytes(stream, 2), 0);
	}
    public static ushort ReadUShort(byte[] data, uint bitIndex, byte bitsToRead = 16)
    {
        if (bitsToRead > 16) bitsToRead = 16;
        if (bitsToRead < 1) bitsToRead = 1;
        return BitConverter.ToUInt16(ReadBits(data, bitIndex, bitsToRead, 2), 0);
    }
    #endregion

    #region 4 byte structures
    public static float ReadFloat(Stream stream)
    {
        return BitConverter.ToSingle(ReadBytes(stream, 4), 0);
    }
    public static float ReadFloat(byte[] data, uint bitIndex, byte bitsToRead = 32)
    {
        if (bitsToRead > 32) bitsToRead = 32;
        if (bitsToRead < 1) bitsToRead = 1;
        return BitConverter.ToSingle(ReadBits(data, bitIndex, bitsToRead, 4), 0);
    }
    public static int ReadInt(Stream stream)
	{
        return BitConverter.ToInt32(ReadBytes(stream, 4), 0);
    }
    public static int ReadInt(byte[] data, uint bitIndex, byte bitsToRead = 32)
    {
        if (bitsToRead > 32) bitsToRead = 32;
        if (bitsToRead < 1) bitsToRead = 1;

        return BitConverter.ToInt32(ReadBits(data, bitIndex, bitsToRead, 4), 0);
    }
    public static uint ReadUInt(Stream stream)
	{
		return BitConverter.ToUInt32(ReadBytes(stream, 4), 0);
	}
    public static uint ReadUInt(byte[] data, uint bitIndex, byte bitsToRead = 32)
    {
        if (bitsToRead > 32) bitsToRead = 32;
        if (bitsToRead < 1) bitsToRead = 1;

        return BitConverter.ToUInt32(ReadBits(data, bitIndex, bitsToRead, 4), 0);
    }
    #endregion

    #region 8 byte structures
    public static double ReadDouble(Stream stream)
    {
        return BitConverter.ToDouble(ReadBytes(stream, 8), 0);
    }
    public static double ReadDouble(byte[] data, uint bitIndex, byte bitsToRead = 64)
    {
        if (bitsToRead > 64) bitsToRead = 64;
        if (bitsToRead < 1) bitsToRead = 1;
        return BitConverter.ToDouble(ReadBits(data, bitIndex, bitsToRead, 8), 0);
    }
    public static long ReadLong(Stream stream)
	{
		return BitConverter.ToInt64(ReadBytes(stream, 8), 0);
	}
    public static long ReadLong(byte[] data, uint bitIndex, byte bitsToRead = 64)
    {
        if (bitsToRead > 64) bitsToRead = 64;
        if (bitsToRead < 1) bitsToRead = 1;
        return BitConverter.ToInt64(ReadBits(data, bitIndex, bitsToRead, 8), 0);
    }
    public static ulong ReadULong(Stream stream)
    {
        return BitConverter.ToUInt64(ReadBytes(stream, 8), 0);
    }
    public static ulong ReadULong(byte[] data, uint bitIndex, byte bitsToRead = 64)
    {
        if (bitsToRead > 64) bitsToRead = 64;
        if (bitsToRead < 1) bitsToRead = 1;
        return BitConverter.ToUInt64(ReadBits(data, bitIndex, bitsToRead, 8), 0);
    }
    #endregion

    #region 16 byte structures
    public static decimal ReadDecimal(Stream stream)
    {
        return new decimal(new int[] { ReadInt(stream), ReadInt(stream), ReadInt(stream), ReadInt(stream) }); //Big endian probably doesn't work, each individual int is flipped but their ordering is probably wrong
    }
    public static decimal ReadDecimal(byte[] data, uint bitIndex, byte bitsToRead = 128)
    {
        if (bitsToRead > 128) bitsToRead = 128;
        if (bitsToRead < 1) bitsToRead = 1;

        int[] decimalParts = new int[4];
        for (byte i = 0; i < UnityEngine.Mathf.CeilToInt(bitsToRead / 32f); i++)
            decimalParts[i] = ReadInt(data, (uint)(bitIndex + i * 32), (byte)((bitsToRead - i * 32) < 32 ? (bitsToRead - i * 32) : 32));

        return new decimal(decimalParts);
    }
    #endregion

    #region Protobuf
    public static int ReadProtoInt(byte[] data, uint index, out uint bytesRead)
    {
        int protoInt = 0;
        bytesRead = 0;

        if (index < data.Length)
        {
            byte currentByte = 0;

            do
            {
                if (index + bytesRead < data.Length) currentByte = data[index + bytesRead];
                if (bytesRead < 4 || (bytesRead == 4 && ((currentByte & 0xf8) == 0 || (currentByte & 0xf8) == 0xf8)))
                    protoInt |= (currentByte & ~0x80) << (7 * (int)bytesRead);
                bytesRead++;
            }
            while (bytesRead < 10 && (currentByte & 0x80) != 0);
        }

        return protoInt;
    }
    public static int ReadProtoInt(byte[] data, uint index)
    {
        uint bytesRead;
        return ReadProtoInt(data, index, out bytesRead);
    }
    public static int ReadProtoInt(Stream stream, out uint bytesRead)
    {
        int protoInt = 0;
        byte currentByte;
        bytesRead = 0;

        do
        {
            currentByte = ReadByte(stream);
            if (bytesRead < 4 || (bytesRead == 4 && ((currentByte & 0xf8) == 0 || (currentByte & 0xf8) == 0xf8)))
                protoInt |= (currentByte & ~0x80) << (7 * (int)bytesRead);
            bytesRead++;
        }
        while (bytesRead < 10 && (currentByte & 0x80) != 0);

        return protoInt;
    }
    public static int ReadProtoInt(Stream stream)
    {
        uint bytesRead;
        return ReadProtoInt(stream, out bytesRead);
    }
    public static string ReadProtoString(byte[] data, uint index, out uint bytesRead)
    {
        uint sizeOfProtoInt;
        uint stringSize = (uint)ReadProtoInt(data, index, out sizeOfProtoInt);
        bytesRead = sizeOfProtoInt + stringSize;
        return System.Text.Encoding.UTF8.GetString(ReadBytes(data, index + sizeOfProtoInt, stringSize));
    }
    public static string ReadProtoString(byte[] data, uint index)
    {
        uint bytesRead;
        return ReadProtoString(data, index, out bytesRead);
    }
    public static string ReadProtoString(Stream stream, out uint bytesRead)
    {
        uint protoIntSize;
        uint stringSize = (uint)ReadProtoInt(stream, out protoIntSize);
        bytesRead = protoIntSize + stringSize;
        return System.Text.Encoding.UTF8.GetString(ReadBytes(stream, stringSize));
    }
    public static string ReadProtoString(Stream stream)
    {
        uint bytesRead;
        return ReadProtoString(stream, out bytesRead);
    }
    #endregion

    #region Strings
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
    public static string ReadNullTerminatedString(byte[] data, int byteIndex, out int bytesRead)
    {
        bytesRead = 0;
        string builtString = "";
        char nextChar = '\0';
        do
        {
            nextChar = BitConverter.ToChar(data, byteIndex + bytesRead);
            if (nextChar != '\0') builtString += nextChar;
            bytesRead++;
        }
        while (nextChar != '\0' && byteIndex + bytesRead < data.Length);

        return builtString;
    }
    public static string ReadNullTerminatedString(byte[] data, int byteIndex)
    {
        int bytesRead;
        return ReadNullTerminatedString(data, byteIndex, out bytesRead);
    }
    public static string ReadCString(byte[] data, uint byteIndex, uint bytesToRead, System.Text.Encoding encoding)
    {
        return encoding.GetString(ReadBytes(data, byteIndex, bytesToRead)).Split(new char[] { '\0' }, 2)[0];
    }
    public static string ReadCString(byte[] data, uint byteIndex, uint bytesToRead)
    {
        return ReadCString(data, byteIndex, bytesToRead, System.Text.Encoding.UTF8);
    }
    public static string ReadLimitedString(byte[] data, uint bitIndex, out uint bitsRead, uint byteLimit)
    {
        bitsRead = 0;
        List<byte> output = new List<byte>();
        for(uint i = 0; i < byteLimit; i++)
        {
            byte input = ReadByte(data, bitIndex + bitsRead);
            bitsRead += 8;
            if (input == 0 || input == 10)
                break;
            output.Add(input);
        }
        return System.Text.Encoding.ASCII.GetString(output.ToArray());
    }
    #endregion

    #region Other
    public static string ReadDataTableString(byte[] data, int byteIndex, out int bytesRead)
    {
        bytesRead = 0;
        System.Collections.Generic.List<byte> builtString = new System.Collections.Generic.List<byte>();
        byte nextChar = 0;
        do
        {
            nextChar = data[byteIndex + bytesRead];
            if (nextChar != 0) builtString.Add(nextChar);
            bytesRead++;
        }
        while (nextChar != 0 && byteIndex + bytesRead < data.Length);

        return System.Text.Encoding.Default.GetString(builtString.ToArray());
    }
    public static uint ReadUBitInt(byte[] data, uint bitIndex, out uint bitsRead)
    {
        uint uBitInt = ReadUInt(data, bitIndex, 6);
        bitsRead = 6;
        if ((uBitInt & (16 | 32)) == 16)
        {
            uBitInt = (uBitInt & 15) | (ReadUInt(data, bitIndex + bitsRead, 4) << 4);
            bitsRead += 4;
        }
        else if ((uBitInt & (16 | 32)) == 32)
        {
            uBitInt = (uBitInt & 15) | (ReadUInt(data, bitIndex + bitsRead, 8) << 4);
            bitsRead += 8;
        }
        else if ((uBitInt & (16 | 32)) == 48)
        {
            uBitInt = (uBitInt & 15) | (ReadUInt(data, bitIndex + bitsRead, 32 - 4) << 4);
            bitsRead += 28;
        }
        return uBitInt;
    }
    public static uint ReadVarInt32(byte[] data, uint bitIndex, out uint bitsRead)
    {
        bitsRead = 0;
        uint tmpByte = 0x80;
        uint result = 0;
        for (int count = 0; (tmpByte & 0x80) != 0; count++)
        {
            if (count > 5)
                throw new Exception("VarInt32 out of range");
            tmpByte = ReadByte(data, bitIndex);
            bitIndex += 8; bitsRead += 8;
            result |= (tmpByte & 0x7f) << (7 * count);
        }
        return result;
    }
    #endregion
}