﻿using System;
using System.IO;
using System.Text;
using System.Collections;
using System.Collections.Generic;

#if UNITY_EDITOR
	
public interface IDependBinary
{
	string BundleFileName
	{
		get;
	}

	int CompressType
	{
		get;
	}

	bool IsMainAsset
	{
		get;
	}

	int SubFileCount
	{
		get;
	}

	int DependFileCount
	{
		get;
	}

	string GetSubFiles(int index);
	string GetDependFiles(int index);
}

#endif

// 依赖二进制文件格式
public class DependBinaryFile
{

	private static bool WriteInt(Stream stream, int value)
	{
		if (stream == null)
			return false;

		byte b = (byte)(value & 0xFF);
		stream.WriteByte(b);
		b = (byte)((value & 0xFF00) >> 8);
		stream.WriteByte(b);
		b = (byte)((value & 0xFF0000) >> 16);
		stream.WriteByte(b);
		b = (byte)((value & 0xFF000000) >> 24);
		stream.WriteByte(b);
		return true;
	}

	private static bool WriteBytes(Stream stream, byte[] bytes)
	{
		if (stream == null)
			return false;

		if (bytes == null || bytes.Length <= 0)
			return WriteInt(stream, 0);
		else if(!WriteInt(stream, bytes.Length))
			return false;
		stream.Write(bytes, 0, bytes.Length);
		return true;
	}

	private static bool WriteString(Stream stream, string str)
	{
		if (stream == null)
			return false;

		if (string.IsNullOrEmpty(str))
			return WriteInt(stream, 0);

		byte[] bytes = System.Text.Encoding.UTF8.GetBytes(str);
		return WriteBytes(stream, bytes);
	}

	private static int ReadInt(Stream stream)
	{
		int ret = stream.ReadByte();
		int b = stream.ReadByte();
		ret = (ret << 8) | b;
		b = stream.ReadByte();
		ret = (ret << 16) | b;
		b = stream.ReadByte();
		ret = (ret << 24) | b;
		return ret;
	}

	private static string ReadString(Stream stream)
	{
		int cnt = ReadInt(stream);
		if (cnt <= 0)
			return string.Empty;

		byte[] bytes = new byte[cnt];
		cnt = stream.Read(bytes, 0, cnt);
		return System.Text.Encoding.UTF8.GetString(bytes, 0, cnt);
	}

	private static bool WriteBool(Stream stream, bool value)
	{
		if (stream == null)
			return false;
		
		int b = value ? 1 : 0;
		stream.WriteByte((byte)b);
		return true;
	}

	private static bool ReadBool(Stream stream)
	{
		int b = stream.ReadByte();
		return b != 0;
	}

	// 文件头
	private struct FileHeader
	{
		// 版本号
		public string version;
		// AB文件数量
		public int abFileCount;
		// 标记
		public int Flag;

		public void SaveToStream(Stream stream)
		{
			WriteString(stream, version);
			WriteInt(stream, abFileCount);
			WriteInt(stream, Flag);
		}
	}

	private struct ABFileHeader
	{
		public int subFileCount;
		public int dependFileCount;
		public bool isScene;
		public bool isMainAsset;
		public int compressType;

		public void SaveToStream(Stream stream)
		{
			WriteInt(stream, subFileCount);
			WriteInt(stream, dependFileCount);
			WriteBool(stream, isScene);
			WriteBool(stream, isMainAsset);
			WriteInt(stream, compressType);
		}

		public void LoadFromStream(Stream stream)
		{
			subFileCount = ReadInt(stream);
			dependFileCount = ReadInt(stream);
			isScene = ReadBool(stream);
			isMainAsset = ReadBool(stream);
			compressType = ReadInt(stream);
		}
	}

	private struct SubFileInfo
	{
		public string fileName;

		public void SaveToStream(Stream stream)
		{
			WriteString(stream, fileName);
		}

		public void LoadFromStream(Stream stream)
		{
			fileName = ReadString(stream);
		}
	}

	private struct DependInfo
	{
		public string abFileName;
		public int refCount;

		public void SaveToStream(Stream stream)
		{
			WriteString(stream, abFileName);
			WriteInt(stream, refCount);
		}

		public void LoadFromStream(Stream stream)
		{
			abFileName = ReadString(stream);
			refCount = ReadInt(stream);
		}
	}

#if UNITY_EDITOR

	public static void ExportFileHeader(Stream Stream, int abFileCount, int flag)
	{
		FileHeader header = new FileHeader();
		header.version = _CurrVersion;
		header.abFileCount = abFileCount;
		header.Flag = flag;
		header.SaveToStream(Stream);
	}

	public static void ExportToABFileHeader(Stream stream, IDependBinary file, string bundleName)
	{
	}

	public static void ExportToSubFile(string subFileName)
	{
	}

	public static void ExportToDependFile(string abFileName, int refCount)
	{
	}

#endif

	private static readonly string _CurrVersion = "_D01";
	public static readonly int FLAG_UNCOMPRESS = 0x0;
}