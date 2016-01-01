using UnityEngine;
using System.Collections;
using System.IO;
using System.Collections.Generic;

public class VPKParser
{
    Stream stream;

    public VPKParser(Stream strm)
    {
        stream = strm;
    }

    public string Parse()
    {
        string parsed = "";
        //List<byte> parsedBytes = new List<byte>();
        byte[] parsedBytes = new byte[stream.Length];
        int streamLength = 0;
        try { streamLength = System.Convert.ToInt32(stream.Length); } catch(System.Exception) {}
        stream.Read(parsedBytes, 0, streamLength);
        parsed = System.Text.Encoding.UTF8.GetString(parsedBytes);
        return parsed;
    }
}
