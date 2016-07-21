using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

public class FileReader
{
	static bool bigEndian = false;

    public static byte ReadByte(Stream stream)
	{
		byte[] buffer = new byte[1];
		stream.Read(buffer, 0, 1);
		return buffer[0];
	}

    public static byte[] ReadBytes(Stream stream, int amount)
    {
        byte[] buffer = new byte[amount];
        stream.Read(buffer, 0, amount);
        return buffer;
    }

    public static char ReadChar(Stream stream)
    {
        byte[] buffer = new byte[2];
        stream.Read(buffer, 0, 1);
        return BitConverter.ToChar(buffer, 0);
    }

    public static short ReadShort(Stream stream)
	{
		byte[] buffer = new byte[2];
		stream.Read(buffer, 0, 2);
		if (bigEndian) buffer.Reverse();
		return BitConverter.ToInt16(buffer, 0);
	}

    public static ushort ReadUShort(Stream stream)
	{
		byte[] buffer = new byte[2];
		stream.Read(buffer, 0, 2);
		if (bigEndian) buffer.Reverse();
		return BitConverter.ToUInt16(buffer, 0);
	}

    public static int ReadInt(Stream stream)
	{
		byte[] buffer = new byte[4];
		stream.Read(buffer, 0, 4);
		if (bigEndian) buffer.Reverse();
		return BitConverter.ToInt32(buffer, 0);
	}

    public static uint ReadUInt(Stream stream)
	{
		byte[] buffer = new byte[4];
		stream.Read(buffer, 0, 4);
		if (bigEndian) buffer.Reverse();
		return BitConverter.ToUInt32(buffer, 0);
	}

    public static long ReadLong(Stream stream)
	{
		byte[] buffer = new byte[8];
		stream.Read(buffer, 0, 8);
		if (bigEndian) buffer.Reverse();
		return BitConverter.ToInt64(buffer, 0);
	}

    public static float ReadFloat(Stream stream)
	{
		byte[] buffer = new byte[4];
		stream.Read(buffer, 0, 4);
		if (bigEndian) buffer.Reverse();
		return BitConverter.ToSingle(buffer, 0);
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
}