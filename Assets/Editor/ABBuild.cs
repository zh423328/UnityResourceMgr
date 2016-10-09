﻿/*----------------------------------------------------------------
// 模块名：AssetBundle 打包功能
// 创建者：zengyi
// 修改者列表：
// 创建日期：2015年9月29日
// 模块描述：
 *          5.x的打包方式，BuildPipeline.BuildAssetBundles打包
 *          5.x打包需要把脚本给出，而4.6.x不需要
//----------------------------------------------------------------*/

#define ASSETBUNDLE_ONLYRESOURCES
#define USE_UNITY5_X_BUILD
#define USE_HAS_EXT
#define USE_DEP_BINARY
#define USE_DEP_BINARY_AB

using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;

public enum eBuildPlatform
{
	eBuildWindow = 0,
	eBuildMac,
	eBuildIOS,
	eBuildAndroid
}

// AssetBundle 文件打包类型
enum AssetBundleFileType
{
	abError = 0,// 错误
	abMainFile, // 一个文件一个打包(单文件模式)
	abDirFiles  // 一个目录所有文件一个打包(目录文件模式---多文件模式)
}

// AB信息
class AssetBunbleInfo: IDependBinary
{
    public struct DependFileInfo
    {
        public string fileName;
    }

	// 获得根据Assets目录的局部目录
	public static string GetLocalPath(string path)
	{
        return AssetBundleMgr.GetAssetRelativePath(path);
	}

	public bool IsBuilded {
		get;
		set;
	}

	public int CompressType {
		get;
		set;
	}

	public bool IsMainAsset {
	
		get {
			return (FileType == AssetBundleFileType.abMainFile) || 
				((FileType == AssetBundleFileType.abDirFiles) && (SubFileCount == 1));
		}
	}


	public string BundleFileName {
		get {
			if (FileType == AssetBundleFileType.abError)
				return string.Empty;
			string ret = GetBundleFileName(Path, false, true);
			return ret;
		}
	}

	public string Md5BundleFileName(string outPath, bool isOnlyFileName = true)
    {
//		get
		{
			string fileName = BundleFileName;
			if (string.IsNullOrEmpty(fileName))
				return string.Empty;
			fileName = outPath + '/' + fileName;
			string ret = Md5(fileName, isOnlyFileName);
			return ret;
		}
    }

#if USE_UNITY5_X_BUILD
	public void RebuildDependFiles(AssetBundleManifest manifest)
	{
		if (manifest == null)
			return;
		string[] directDepnendFileNames = manifest.GetDirectDependencies(this.BundleFileName);
		List<DependFileInfo> list = DependABFileNameList;
		list.Clear();
		for (int i = 0; i < directDepnendFileNames.Length; ++i)
		{
			string fileName = System.IO.Path.GetFileNameWithoutExtension(directDepnendFileNames[i]);
			if (!string.IsNullOrEmpty(fileName))
			{
				DependFileInfo info = new DependFileInfo();
				info.fileName = fileName;
				list.Add(info);
			}
		}
	}
#endif

	private static string GetBundleFileName(string path, bool removeAssets, bool doReplace)
	{
		if (string.IsNullOrEmpty(path))
			return string.Empty;

		path = path.ToLower();
		// delete "Assets/"
		string localPath;

		if (removeAssets) {
			string startStr = "assets/";
			if (path.StartsWith (startStr))
				localPath = path.Substring (startStr.Length);
			else
				localPath = path;
		} else
			localPath = path;
		
		if (doReplace)
			localPath = localPath.Replace ('/', '$') + ".assets";
		//localPath = localPath.ToLower();
		return localPath;
	}

    private static MD5 m_Md5 = new MD5CryptoServiceProvider();
	// filePath Md5
	private static Dictionary<string, string> m_Md5FileMap = new Dictionary<string, string>();
	private static Dictionary<string, string> m_Md5FileMap2 = new Dictionary<string, string>();
	public static void ClearMd5FileMap()
	{
		m_Md5FileMap.Clear();
		m_Md5FileMap2.Clear();
	}
    // 返回文件名的MD5
    internal static string Md5(string filePath, bool isOnlyUseFileName = true)
    {
        if (string.IsNullOrEmpty(filePath))
            return string.Empty;

		if (!File.Exists(filePath))
			return string.Empty;

		string ret;
		if(isOnlyUseFileName)
		{
			if (m_Md5FileMap.TryGetValue(filePath, out ret))
				return ret;
		} else
		{
			if (m_Md5FileMap2.TryGetValue(filePath, out ret))
				return ret;
		}

		ret = string.Empty;

		if (isOnlyUseFileName)
		{
			string fileName = System.IO.Path.GetFileNameWithoutExtension(filePath);
			byte[] src = System.Text.Encoding.ASCII.GetBytes(fileName);
			byte[] hash = m_Md5.ComputeHash(src);
			for (int i = 0; i < hash.Length; i++)
			{
				ret += hash[i].ToString("X").PadLeft(2, '0');  
			}

			ret = ret.ToLower();
			
			m_Md5FileMap.Add(filePath, ret);
		} else
		{
			FileStream stream = new FileStream(filePath, FileMode.Open); 
			try
			{
				if (stream.Length <= 0)
					return string.Empty;
				byte[] src = new byte[stream.Length];
				stream.Seek(0, SeekOrigin.Begin);
				stream.Read(src, 0, src.Length);
        		byte[] hash = m_Md5.ComputeHash(src);
      			//  m_Md5.Clear();

        		for (int i = 0; i < hash.Length; i++)
        		{
            		ret += hash[i].ToString("X").PadLeft(2, '0');  
        		}

        		ret = ret.ToLower();

				m_Md5FileMap2.Add(filePath, ret);
			}
			finally
			{
				stream.Close();
				stream.Dispose();
			}
		}

        return ret;
    }

	public AssetBunbleInfo(string fullPath, string[] fileNames)
	{
		Path = GetLocalPath(fullPath);
		if (fileNames == null || fileNames.Length <= 0 || string.IsNullOrEmpty (Path))
		{
			FileType = AssetBundleFileType.abError;
			FullPath = string.Empty;
			return;
		}

		FileType = AssetBundleFileType.abDirFiles;
		Path = Path.ToLower();
		FullPath = fullPath.ToLower();

		List<string> fileList = this.FileList;
		for (int i = 0; i < fileNames.Length; ++i)
		{
			string fileName = fileNames[i];
			if (!AssetBundleBuild.FileIsResource(fileName))
				continue;
			fileName = GetLocalPath(fileName);
			if (string.IsNullOrEmpty(fileName))
				continue;
			fileList.Add(fileName.ToLower());
		}

		BuildDepends();
		CheckIsScene();

		Set_5_x_AssetBundleNames();

		IsBuilded = false;
	}

	public AssetBunbleInfo(string fullPath)
	{
		Path = GetLocalPath(fullPath);

		if (string.IsNullOrEmpty (Path)) {
			FileType = AssetBundleFileType.abError;
			FullPath = string.Empty;
		}
		else {
			if (System.IO.Directory.Exists(fullPath))
				FileType = AssetBundleFileType.abDirFiles;
			else
			if (System.IO.File.Exists(fullPath))
				FileType = AssetBundleFileType.abMainFile;
			Path = Path.ToLower();
			FullPath = fullPath.ToLower();
		}

		BuildDirFiles ();
		BuildDepends ();

		CheckIsScene ();

        Set_5_x_AssetBundleNames();

		IsBuilded = false;


	}

	private void CheckIsScene()
	{
		if (FileType == AssetBundleFileType.abError) {
			IsScene = false;
			return;
		}

		/*
		if (FileType == AssetBundleFileType.abMainFile) {
			// 判断是否是场景
			string ext = System.IO.Path.GetExtension (FullPath);
			IsScene = (string.Compare (ext, ".unity", true) == 0);
			return;
		}*/

		bool isSceneFiles = false;
		bool isRemoveScene = false;
		for (int i = 0; i < SubFileCount; ++i)
		{
			string fileName = GetSubFiles(i);
			string ext = System.IO.Path.GetExtension(fileName);
			bool b = (string.Compare (ext, ".unity", true) == 0);
			if ((i > 0) && (isSceneFiles != b))
			{
				// string errStr = string.Format("AssetBundle [{0}] don't has Scene and other type files", Path);
				// Debug.LogError(errStr);
				// FileType = AssetBundleFileType.abError;
				// return;
				isRemoveScene = true;
			}

			isSceneFiles = b;
		}

		if (isRemoveScene) {
			string errStr = string.Format("AssetBundle [{0}] don't has Scene and other type files(so remove Scene)", Path);
			Debug.LogWarning(errStr);

			isSceneFiles = false;
			if (mFileList != null)
			{
				List<string> fileList = new List<string>();
				for (int i = 0; i < mFileList.Count; ++i)
				{
					string fileName = mFileList[i];
					string ext = System.IO.Path.GetExtension(fileName);
					bool b = (string.Compare (ext, ".unity", true) == 0);
					if (!b)
						fileList.Add(fileName);
				}

				mFileList.Clear();
				mFileList.AddRange(fileList);
			}
		}

		IsScene = isSceneFiles;
	}

	public void ExportBinary(Stream stream, bool isMd5, string outPath)
	{
		if ((stream == null) || (FileType == AssetBundleFileType.abError))
			return;

		string bundleFileName;
		if (isMd5)
			bundleFileName = this.Md5BundleFileName(outPath);
		else
			bundleFileName = this.BundleFileName;
		if (string.IsNullOrEmpty (bundleFileName))
			return;

		DependBinaryFile.ExportToABFileHeader(stream, this, bundleFileName);
		if (SubFileCount > 0)
		{
			for (int i = 0; i < SubFileCount; ++i)
			{
				string fileName = GetBundleFileName(GetSubFiles(i), true, false);
				if (string.IsNullOrEmpty(fileName))
					continue;
				string resFileName = AssetBundleBuild.GetXmlFileName(fileName);
				if (string.IsNullOrEmpty(resFileName))
					continue;
				DependBinaryFile.ExportToSubFile(stream, resFileName);
			}
		}

		if (DependFileCount > 0)
		{
			for (int i = 0; i < DependFileCount; ++i)
			{
				string fileName = GetBundleFileName(GetDependFiles(i), false, true);
				if (string.IsNullOrEmpty(fileName))
					continue;
				int depCnt = AssetBundleRefHelper.GetAssetBundleRefCount(this, fileName);
				if (isMd5)
				{
					string filePath = outPath + '/' + fileName;
					fileName = AssetBunbleInfo.Md5(filePath);
				}

				if (depCnt <= 0)
					depCnt = 1;

				DependBinaryFile.ExportToDependFile(stream, fileName, depCnt);
			}	
		}

	}

//	private static readonly bool _cIsOnlyFileNameMd5 = true;
	public void ExportXml(StringBuilder builder, bool isMd5, string outPath)
	{
		if ((builder == null) || (FileType == AssetBundleFileType.abError))
			return;

		string bundleFileName;
        if (isMd5)
			bundleFileName = this.Md5BundleFileName(outPath);
        else
            bundleFileName = this.BundleFileName;
		if (string.IsNullOrEmpty (bundleFileName))
			return;

		builder.AppendFormat("\t<AssetBundle fileName=\"{0}\" isScene=\"{1}\" isMainAsset=\"{2}\" compressType=\"{3}\">", 
		                     bundleFileName, System.Convert.ToString(IsScene), 
		                     System.Convert.ToString(IsMainAsset), System.Convert.ToString(CompressType));
		builder.AppendLine();

		if (SubFileCount > 0)
		{
			builder.Append("\t\t<SubFiles>");
			builder.AppendLine();
			for (int i = 0; i < SubFileCount; ++i) {
				string fileName = GetBundleFileName(GetSubFiles(i), true, false);
				if (string.IsNullOrEmpty(fileName))
					continue;
				//string name = System.IO.Path.ChangeExtension(fileName, "");
				//string ext = System.IO.Path.GetExtension(fileName);
				string resFileName = AssetBundleBuild.GetXmlFileName(fileName);
				if (string.IsNullOrEmpty(resFileName))
					continue;
				// builder.AppendFormat("\t\t\t<SubFile fileName=\"{0}\" hashCode=\"{1}\"/>", resFileName, System.Convert.ToString(Animator.StringToHash(resFileName)));
				builder.AppendFormat("\t\t\t<SubFile fileName=\"{0}\"/>", resFileName);
				builder.AppendLine();
			}

			builder.Append("\t\t</SubFiles>");
			builder.AppendLine();
		}

		if (DependFileCount > 0) {
			builder.Append("\t\t<DependFiles>");
			builder.AppendLine();

			for (int i = 0; i < DependFileCount; ++i)
			{
				string fileName = GetBundleFileName(GetDependFiles(i), false, true);
				if (string.IsNullOrEmpty(fileName))
					continue;
				int depCnt = AssetBundleRefHelper.GetAssetBundleRefCount(this, fileName);
                if (isMd5)
				{
					string filePath = outPath + '/' + fileName;
					fileName = AssetBunbleInfo.Md5(filePath);
				}
				if (depCnt > 1)
					builder.AppendFormat("\t\t\t<DependFile fileName=\"{0}\" refCount=\"{1}\"/>", fileName, depCnt);
				else
					builder.AppendFormat("\t\t\t<DependFile fileName=\"{0}\" />", fileName);
				builder.AppendLine();
			}

			builder.Append("\t\t</DependFiles>");
			builder.AppendLine();
		}

		builder.Append ("\t</AssetBundle>");
		builder.AppendLine ();
	}

	private void _AddDependHashSet(HashSet<string> hashSet)
	{
		for (int i = 0; i < DependFileCount; ++i) {
			string dependFileName = GetDependFiles(i);
			if (string.IsNullOrEmpty(dependFileName))
				continue;
			AssetBunbleInfo dependInfo = AssetBundleBuild.FindAssetBundle(dependFileName);
			if (dependInfo != null)
			{
				if (!hashSet.Contains(dependFileName))
				{
					hashSet.Add(dependFileName);
					dependInfo._AddDependHashSet(hashSet);
				}
			}
		}
	}

	private int GetAllDependCount()
	{
		if (FileType == AssetBundleFileType.abError)
			return 0;

		HashSet<string> dependSet = new HashSet<string> ();
		for (int i = 0; i < DependFileCount; ++i) {
			string dependFileName = GetDependFiles(i);
			if (string.IsNullOrEmpty(dependFileName))
				continue;
			AssetBunbleInfo dependInfo = AssetBundleBuild.FindAssetBundle(dependFileName);
			if (dependInfo == null)
				continue;
			dependSet.Add(dependFileName);
			dependInfo._AddDependHashSet(dependSet);
		}

		return dependSet.Count;
	}

	public void RefreshAllDependCount()
	{
		AllDependCount = GetAllDependCount ();
	}

	// 文件类型
	public AssetBundleFileType FileType {
		get;
		protected set;
	}

	public string Path {
		get;
		protected set;
	}

	public string FullPath
	{
		get; protected set;
	}

	// 是否是场景
	public bool IsScene
	{
		get;
		protected set;
	}

	public int DependFileCount
	{
		get {
			if (mDependABFileNameList == null)
				return 0;
			return mDependABFileNameList.Count;
		}
	}

	public int AllDependCount {
		get 
		{
			if (mAllDependCount <= 0)
				return DependFileCount;
			return mAllDependCount;
		}
		protected set
		{
			mAllDependCount = value;
		}
	}

	public int SubFileCount
	{
		get {
			if (FileType == AssetBundleFileType.abMainFile)
				return 1;

			if (mFileList == null)
				return 0;
			return mFileList.Count;
		}
	}

	public string GetDependFiles(int index)
	{
		if ((mDependABFileNameList == null) || (index < 0) || (index >= mDependABFileNameList.Count))
			return string.Empty;
		return mDependABFileNameList [index].fileName;
	}

	public string GetSubFiles(int index)
	{
		if (FileType == AssetBundleFileType.abMainFile) {
			if (index == 0)
				return this.Path;
			else
				return string.Empty;
		}

		if ((mFileList == null) || (index < 0) || (index >= mFileList.Count))
			return string.Empty;
		return mFileList [index];
	}

	public string[] GetSubFiles()
	{
		if (FileType == AssetBundleFileType.abMainFile) {
			string[] ret = new string[1];
			ret[0] = this.Path;
			return ret;
		}

		if ((mFileList == null) || (mFileList.Count <= 0))
			return null;
		return mFileList.ToArray ();
	}

	public void Print()
	{
		Debug.Log ("===================================================");
		Debug.Log ("{");
		Debug.Log ("\t[相对目录] " + Path);
		Debug.Log ("\t[绝对目录] " + FullPath);

		Debug.Log ("\t[文件数量] " + System.Convert.ToString (SubFileCount));
		for (int i = 0; i < SubFileCount; ++i) {
			string fileName = GetSubFiles(i);
			Debug.Log("\t\t[子文件名] " + fileName);
		}

		Debug.Log (string.Format("\t[依赖数量: {0}] [总依赖数量: {1}] ", System.Convert.ToString (DependFileCount),
		                         									   System.Convert.ToString (AllDependCount)));
		for (int i = 0; i < DependFileCount; ++i) {
			string depend = GetDependFiles(i);
			Debug.Log("\t\t[依赖名] " + depend);
		}
		Debug.Log ("}");
		Debug.Log ("===================================================");
	}

    public int ScriptFileCount
    {
        get
        {
            if (mScriptFileNameList == null)
                return 0;
            return mScriptFileNameList.Count;
        }
    }

    public string GetScriptFileName(int index)
    {
        if (mScriptFileNameList == null)
            return string.Empty;
        if ((index < 0) || (index >= mScriptFileNameList.Count))
            return string.Empty;
        return mScriptFileNameList[index];
    }

    public string[] GetScriptFileNames()
    {
        if (mScriptFileNameList == null)
            return null;
        return mScriptFileNameList.ToArray();
    }

	// 排序函数
	public static int OnSort(AssetBunbleInfo info1, AssetBunbleInfo info2)
	{
		int dependCnt1 = info1.AllDependCount;
		int dependCnt2 = info2.AllDependCount;

		if ((dependCnt1 == 0) && (dependCnt2 > 0))
			return -1;

		if ((dependCnt2 == 0) && (dependCnt1 > 0))
			return 1;

		if (dependCnt1 < dependCnt2)
			return -1;
		if (dependCnt1 > dependCnt2)
			return 1;
		else
			return 0;
	}

	// 包含的文件
	protected void BuildDirFiles()
	{
		if (FileType != AssetBundleFileType.abDirFiles)
			return;

		string path = FullPath;
		if (string.IsNullOrEmpty (path))
			return;

		string[] files = System.IO.Directory.GetFiles (path);
		if ((files != null) && (files.Length > 0)) {
			for (int i = 0; i < files.Length; ++i)
			{
				string fileName = files[i];
				if (!AssetBundleBuild.FileIsResource(fileName))
					continue;
				string localPath = GetLocalPath(fileName);
				if (!string.IsNullOrEmpty(localPath))
				{
					localPath = localPath.ToLower();
					FileList.Add(localPath);
				}
			}
		}

		// is contain _Dir??
		string[] dirs = System.IO.Directory.GetDirectories (path);

		if (dirs != null)
		{
			for (int i = 0; i < dirs.Length; ++i)
			{
				string dir = dirs[i];
				string local = System.IO.Path.GetFileName(dir);
				if (local.StartsWith(AssetBundleBuild._NotUsed))
				{
					List<string> resFiles = AssetBundleBuild.GetAllSubResFiles(dir);
					if ((resFiles != null) && (resFiles.Count > 0))
					{
						for (int j = 0; j < resFiles.Count; ++j)
						{
							string localPath = GetLocalPath(resFiles[j]);
							localPath = localPath.ToLower();
							FileList.Add(localPath);
						}
					}
				}
			}
		}
	}


	protected bool ExistsFile(string localPath)
	{
		for (int i = 0; i < SubFileCount; ++i) {
			if (string.Compare(GetSubFiles(i), localPath, true) == 0)
				return true;
		}

		return false;
	}

    protected void Set_5_x_AssetBundleNames()
    {
#if USE_UNITY5_X_BUILD
        for (int i = 0; i < SubFileCount; ++i)
        {
            string subFileName = GetSubFiles(i);
            if (!string.IsNullOrEmpty(subFileName))
            {
                AssetImporter importer = AssetImporter.GetAtPath(subFileName);
                if (importer != null)
                {
					//string name = System.IO.Path.GetFileNameWithoutExtension(this.BundleFileName);
					string name = this.BundleFileName;
					if (string.Compare(importer.assetBundleName, name) != 0)
					{
						importer.assetBundleName = name;
						EditorUtility.UnloadUnusedAssetsImmediate();
					//	importer.assetBundleVariant = "assets";
					//	importer.SaveAndReimport();
					}

					AssetBundleBuild.AddShowTagProcess(name);
                }
            }
        }

#endif
    }

	protected void BuildDepends()
	{
		if (FileType == AssetBundleFileType.abError)
			return;

		string[] fileNames = null;
		if (FileType == AssetBundleFileType.abMainFile) {
			// 一个文件模式
			fileNames = new string[1];
			fileNames[0] = Path;
		} else
		if (FileType == AssetBundleFileType.abDirFiles) {
			// 整个目录文件模式
			fileNames = FileList.ToArray();
		}

		if ((fileNames == null) || (fileNames.Length <= 0))
			return;

		string[] dependFiles = AssetDatabase.GetDependencies (fileNames);
		if ((dependFiles != null) && (dependFiles.Length > 0)) {
			for (int i = 0; i < dependFiles.Length; ++i)
			{
				string fileName = dependFiles[i];

                if (AssetBundleBuild.FileIsScript(fileName))
                {
                    // 如果是脚本
                    if (!ScriptFileNameList.Contains(fileName))
                        ScriptFileNameList.Add(fileName);
                    continue;
                }

				if (!AssetBundleBuild.FileIsResource(fileName))
					continue;
#if ASSETBUNDLE_ONLYRESOURCES
                if (AssetBundleBuild.IsOtherResourcesDir(fileName))
                    continue;
#endif
                if (ExistsFile(fileName))
				    continue;

				fileName = GetDependFileName(fileName);
				if (string.IsNullOrEmpty(fileName))
					continue;
				if (!ExistsDepend(fileName))
				{
					DependFileInfo info = new DependFileInfo();
					info.fileName = fileName;
					DependABFileNameList.Add(info);	
				}

			}
		}
	}

	protected string GetDependFileName(string localFileName)
	{
		if (string.IsNullOrEmpty (localFileName))
			return string.Empty;
		string dirName = System.IO.Path.GetDirectoryName(localFileName);
		string searchStr = "/" + AssetBundleBuild._NotUsed;
		int idx = dirName.IndexOf (searchStr);
		bool isNotUsed = (idx >= 0);
		if (isNotUsed) {
			// 判断前面是否有一个@文件夹

			string ret = dirName.Substring(0, idx);
			string preDirName = System.IO.Path.GetFileName(ret);
			bool isOnly = !preDirName.StartsWith(AssetBundleBuild._MainFileSplit);
			if (isOnly)
				return localFileName.ToLower();
			return ret.ToLower();
		}

		string localDirName = System.IO.Path.GetFileName(dirName);
		bool isGroupOnly = !localDirName.StartsWith (AssetBundleBuild._MainFileSplit);
		if (!isGroupOnly)
			return dirName.ToLower(); 
		return localFileName.ToLower();
	}

	/*
	 * 老的规则：@ 表示每个为单独AB
	protected string GetDependFileName(string localFileName)
	{
		if (string.IsNullOrEmpty (localFileName))
			return string.Empty;
		string dirName = System.IO.Path.GetDirectoryName(localFileName);
		string localDirName = System.IO.Path.GetFileName(dirName);
		bool isOnly = localDirName.StartsWith (AssetBundleBuild._MainFileSplit);
		if (isOnly)
			return localFileName;

		// has _ Dir??
		string searchStr = "/" + AssetBundleBuild._NotUsed;
		int idx = dirName.IndexOf(searchStr);
		if (idx >= 0) {
			string ret = dirName.Substring(0, idx);
			// 判断前一个文件夹是否是@文件夹，如果不是，则保持原样
			string preDirName = System.IO.Path.GetFileName(ret);
			bool isPreOnly = preDirName.StartsWith(AssetBundleBuild._MainFileSplit);
			if (isPreOnly)
				return localFileName;
			return ret;
		}

		return dirName;
	}*/


	protected bool ExistsDepend(string localPath)
	{
		if (mDependABFileNameList == null)
			return false;

		for (int i = 0; i < mDependABFileNameList.Count; ++i) {
			if (string.Compare(mDependABFileNameList[i].fileName, localPath, true) == 0)
				return true;
		}

		return false;
	}

    protected List<AssetBunbleInfo.DependFileInfo> DependABFileNameList
	{
		get {
			if (mDependABFileNameList == null)
                mDependABFileNameList = new List<AssetBunbleInfo.DependFileInfo>();
			return mDependABFileNameList;
		}
	}

    // 直接依赖的脚本文件名列表
    protected List<string> ScriptFileNameList
    {
        get
        {
            if (mScriptFileNameList == null)
                mScriptFileNameList = new List<string>();
            return mScriptFileNameList;
        }
    }

	protected List<string> FileList
	{
		get {
			if (mFileList == null)
				mFileList = new List<string>();
			return mFileList;
		}
	}


	// 依赖的文件名列表(Local Path)
	private List<DependFileInfo> mDependABFileNameList = null;
	// 包含的文件，如果是独立的包，则为NULL
	private List<string> mFileList = null;
	private int mAllDependCount = 0;
    private List<string> mScriptFileNameList = null;
}

// AB Ref Helper
static class AssetBundleRefHelper
{
	public static void ClearFileMetaMap()
	{
		mFileMetaMap.Clear();
	}

	public static int GetAssetBundleRefCount(string srcABFileName, string dependABFileName)
	{
		if (string.IsNullOrEmpty(srcABFileName))
			return 0;
		srcABFileName = srcABFileName.Replace('$', '/');
		string bundleExt = ".assets";
		if (srcABFileName.EndsWith(bundleExt))
			srcABFileName = srcABFileName.Substring(0, srcABFileName.Length - bundleExt.Length);
		AssetBunbleInfo srcInfo = AssetBundleBuild.FindAssetBundle(srcABFileName);
		if (srcInfo == null)
			return 0;
		return GetAssetBundleRefCount(srcInfo, dependABFileName);
	} 

	public static int GetAssetBundleRefCount(AssetBunbleInfo srcInfo, string dependABFileName)
	{
		if (srcInfo == null || string.IsNullOrEmpty(dependABFileName))
			return 0;
		dependABFileName = dependABFileName.Replace('$', '/');
		string bundleExt = ".assets";
		if (dependABFileName.EndsWith(bundleExt))
			dependABFileName = dependABFileName.Substring(0, dependABFileName.Length - bundleExt.Length);
		AssetBunbleInfo depInfo = AssetBundleBuild.FindAssetBundle(dependABFileName);
		if (depInfo == null)
			return 0;
		int ret = 0;
		if (srcInfo.SubFileCount > 0 && depInfo.SubFileCount > 0)
		{
			for (int i = 0; i < srcInfo.SubFileCount; ++i)
			{
				string srcSubFile = srcInfo.GetSubFiles(i);
				if (string.IsNullOrEmpty(srcSubFile))
					continue;
				string srcExt = Path.GetExtension(srcSubFile);
				bool isSceneFile = string.Compare(srcExt, ".unity") == 0;
				string yaml = GetYamlStr(srcSubFile);
				if (string.IsNullOrEmpty(yaml))
					continue;

				for (int j = 0; j < depInfo.SubFileCount; ++j)
				{
					string depSubFile = depInfo.GetSubFiles(j);
					if (string.IsNullOrEmpty(depSubFile))
						continue;
					string guid = GetMetaFileGuid(depSubFile);
					if (string.IsNullOrEmpty(guid))
						continue;
					int refCnt = GetDependRefCount(yaml, guid);
					if (refCnt > 0)
					{
						if (isSceneFile)
							ret += 1;
						else
							ret += refCnt;
					}
				}
			}
		}

		return ret;
	}

	private static int GetDependRefCount(string srcYaml, string dependGuid)
	{
		if (string.IsNullOrEmpty(srcYaml) || string.IsNullOrEmpty(dependGuid))
			return 0;
		int searchIdx = 0;
		int ret = 0;
		while (searchIdx >= 0)
		{
			searchIdx = srcYaml.IndexOf(dependGuid, searchIdx);
			if (searchIdx >= 0)
			{
				++ret;
				searchIdx += dependGuid.Length;
			}
		}

		return ret;
	}

	private static string[] _cYamlFileExts = {".unity", ".prefab", ".mat", ".controller", ".mask",
		".flare", ".renderTexture", ".mixer", ".giparams", ".anim", ".overrideController",
		".physicMaterial", ".physicsMaterial2D", ".guiskin", ".fontsettings", ".shadervariants",
		".cubemap"};

	private static bool IsYamlFile(string fileName)
	{
		if (EditorSettings.serializationMode != SerializationMode.ForceText)
			return false;
		
		string ext = Path.GetExtension(fileName);
		for (int i = 0; i < _cYamlFileExts.Length; ++i)
		{
			if (string.Compare(ext, _cYamlFileExts[i]) == 0)
				return true;
		}

		return false;
	}

	// fileName is not AssetBundle FileName
	private static string GetYamlStr(string fileName)
	{
		if (string.IsNullOrEmpty(fileName))
			return string.Empty;
		fileName = Path.GetFullPath(fileName);
		if (!File.Exists(fileName))
			return string.Empty;
	
		if (!IsYamlFile(fileName))
			return string.Empty;

		try
		{
			FileStream stream = new FileStream(fileName, FileMode.Open, FileAccess.Read);

			byte[] buf = new byte[stream.Length];
			stream.Read(buf, 0, buf.Length);

			stream.Close();
			stream.Dispose();

			string str = System.Text.Encoding.ASCII.GetString(buf);
			return str;
		} catch
		{
			return string.Empty;
		}
	}

	// fileName is not AssetBundle FileName
	private static string GetMetaFileGuid(string fileName)
	{
		string ret = string.Empty;
		if (mFileMetaMap.TryGetValue(fileName, out ret))
			return ret;
		string metaExt = ".meta";
		string metaFileName = fileName;
		if (!metaFileName.EndsWith(metaExt, true, System.Globalization.CultureInfo.CurrentCulture))
			metaFileName += metaExt;
		metaFileName = System.IO.Path.GetFullPath(metaFileName);
		if (!File.Exists(metaFileName))
			return ret;
		
		try
		{
			FileStream stream = new FileStream(metaFileName, FileMode.Open, FileAccess.Read);
			byte[] buf = new byte[stream.Length];
			stream.Read(buf, 0, buf.Length);
			
			stream.Close();
			stream.Dispose();
			
			string str = System.Text.Encoding.ASCII.GetString(buf);
			string guidPre = "guid:";
			int startIdx = str.IndexOf(guidPre);
			if (startIdx >= 0)
			{
				startIdx += guidPre.Length;
				int endIdx = str.IndexOf('\n', startIdx);
				if (endIdx >= 0)
				{
					ret = str.Substring(startIdx, endIdx - startIdx);
					ret = ret.Trim();
					
					mFileMetaMap.Add(fileName, ret);
				}
			}
		} catch
		{
			// nothing
		}
		
		return ret;
	}
	// fileName, Guid
	private static Dictionary<string, string> mFileMetaMap = new Dictionary<string, string>(); 
}

// AB Tree
class AssetBundleMgr
{

	public AssetBunbleInfo FindAssetBundle(string fileName)
	{
		if (string.IsNullOrEmpty (fileName))
			return null;
		AssetBunbleInfo ret;
		if (!mAssetBundleMap.TryGetValue (fileName, out ret))
			ret = null;
		return ret;
	}

	public int MaxTagFileCount
	{
		get;
		private set;
	}

	public int CurTagIdx
	{
		get;
		set;
	}

	private bool GetSplitABCnt(string dir, out int cnt)
	{
		cnt = 0;
		if (string.IsNullOrEmpty(dir))
			return false;
		string dirName = Path.GetFileName(dir);
		if (dirName.StartsWith("["))
		{
			int idx = dirName.IndexOf(']');
			if (idx <= 1)
				return false;
			string numStr = dirName.Substring(1, idx - 1);
			int num;
			if (int.TryParse(numStr, out num) && num > 0)
			{
				cnt = num;
				return true;
			}
		}

		return false;
	}

	private bool IsSplitABDir(string dir)
	{
		int num;
		return GetSplitABCnt(dir, out num);
	}

	private void BuildSplitABDir(string splitDir, ABLinkFileCfg cfg)
	{
		if (cfg == null || string.IsNullOrEmpty(splitDir))
			return;
		string[] files = Directory.GetFiles(splitDir, "*.*", SearchOption.TopDirectoryOnly);
		if (files == null || files.Length <= 0)
			return;

		int maxCnt;
		if (!GetSplitABCnt(splitDir, out maxCnt) || maxCnt <= 0)
			return;

		List<string> resFiles = new List<string>();
		for (int i = 0; i < files.Length; ++i)
		{
			string fileName = files[i];
			if (AssetBundleBuild.FileIsResource(fileName))
				resFiles.Add(fileName);
		}

		if (resFiles.Count <= 0)
			return;

		// 查找最合适的
		int idx = splitDir.IndexOf(']');
		string subDir = splitDir.Substring(idx + 1).Trim();
		if (string.IsNullOrEmpty(subDir))
			return;

		splitDir = AssetBunbleInfo.GetLocalPath(splitDir);

		int curIdx = 0;
		while (true)
		{
			string dstDir = string.Format("{0}/@{1}{2:D}", splitDir, subDir, curIdx);
			int curCnt;
			if (cfg.GetDstDirCnt(dstDir, out curCnt))
			{
				if (curCnt < maxCnt)
					break;
			} else
				break;

			++curIdx;
		}

		for (int i = 0; i < resFiles.Count; ++i)
		{
			string srcFileName = AssetBunbleInfo.GetLocalPath(resFiles[i]);
			if (cfg.ContainsLink(srcFileName))
				continue;

			string dstDir = string.Format("{0}/@{1}{2:D}", splitDir, subDir, curIdx);
			int curCnt;

			while (true)
			{
				if (!cfg.GetDstDirCnt(dstDir, out curCnt))
				{
					curCnt = 0;
					break;
				}
				if (curCnt + 1 > maxCnt)
				{
					++curIdx;
					dstDir = string.Format("{0}/@{1}{2:D}", splitDir, subDir, curIdx);
				} else
					break;
			}


			string dstFileName = string.Format("{0}/{1}", dstDir, Path.GetFileName(srcFileName));
			cfg.AddLink(srcFileName, dstFileName);
			++curCnt;
		}

		Dictionary<string, List<string>> dirFileMap = new Dictionary<string, List<string>>();
		var iter = cfg.GetIter();
		while (iter.MoveNext())
		{
			string dstFileName = iter.Current.Value;
			string dstDir = Path.GetDirectoryName(dstFileName);
			if (dirFileMap.ContainsKey(dstDir))
				dirFileMap[dstDir].Add(iter.Current.Key);
			else
			{
				List<string> list = new List<string>();
				list.Add(iter.Current.Key);
				dirFileMap.Add(dstDir, list);
			}
		}
		iter.Dispose();

		var dirIter = dirFileMap.GetEnumerator();
		while (dirIter.MoveNext())
		{
            string path = AssetBunbleInfo.GetLocalPath(dirIter.Current.Key).ToLower();
            if (mAssetBundleMap.ContainsKey(path))
                continue;
            var list = dirIter.Current.Value;
			// 排个序
			list.Sort();
			string[] fileNames = list.ToArray();
			string fullPath = Path.GetFullPath(path);
			AssetBunbleInfo ab = new AssetBunbleInfo(fullPath, fileNames);
			mAssetBundleMap.Add(path, ab);
			mAssetBundleList.Add(ab);
		}
		dirIter.Dispose();
	}

	private void BuildSplitABDirs(HashSet<string> splitABDirs)
	{
		if (splitABDirs == null || splitABDirs.Count <= 0)
			return;

		string abSplitCfgFileName = Path.GetFullPath("buildABSplit.cfg");
		ABLinkFileCfg abLinkCfg = new ABLinkFileCfg();
		if (File.Exists(abSplitCfgFileName))
		{
			abLinkCfg.LoadFromFile(abSplitCfgFileName);
		}
		var abSplitIter = splitABDirs.GetEnumerator();
		while (abSplitIter.MoveNext())
		{
			BuildSplitABDir(abSplitIter.Current, abLinkCfg);
			EditorUtility.UnloadUnusedAssetsImmediate();
		}
		abSplitIter.Dispose();

		abLinkCfg.SaveToFile(abSplitCfgFileName);
	}

#if USE_UNITY5_X_BUILD

	private void CheckRongYuRes(AssetBunbleInfo info)
	{
		if (info == null)
			return;
		for (int i = 0; i < info.SubFileCount; ++i)
		{
			string subFileName = info.GetSubFiles(i);
			string[] depFileList = AssetDatabase.GetDependencies(subFileName, false);
			if (depFileList == null || depFileList.Length <= 0)
				continue;
			for (int j = 0; j < depFileList.Length; ++j)
			{
				string depFileName = depFileList[j];

				if (!AssetBundleBuild.FileIsResource(depFileName))
					continue;

				bool isFound = false;
				for (int k = 0; k < info.SubFileCount; ++k)
				{
					if (string.Compare(depFileName, info.GetSubFiles(k), true) == 0)
					{
						isFound = true;
						break;
					}
				}

				if (isFound)
					continue;

				AssetImporter importer = AssetImporter.GetAtPath(depFileName);
				if (importer == null)
					continue;

				isFound = !string.IsNullOrEmpty(importer.assetBundleName);

				if (!isFound)
				{
					// 打印出来
					Debug.LogFormat("<color=yellow>[{0}]</color><color=white>依赖被额外包含</color><color=red>{1}</color>", 
						info.BundleFileName, depFileName);
				}
			}
		}
	}

	// 打印未被打包但引用的资源
	private void CheckRongYuRes()
	{
		if (mAssetBundleList == null || mAssetBundleList.Count <= 0)
			return;
		for (int i = 0; i < mAssetBundleList.Count; ++i)
		{
			AssetBunbleInfo info = mAssetBundleList[i];
			if (info == null)
				continue;
			CheckRongYuRes(info);
			EditorUtility.UnloadUnusedAssetsImmediate();
		}
	}

#endif

	// 生成
	public void BuildDirs(List<string> dirList)
	{
		Clear ();
		if ((dirList == null) || (dirList.Count <= 0))
			return;

		MaxTagFileCount = dirList.Count;
		List<string> abFiles = new List<string> ();
		HashSet<string> NotUsedDirHash = new HashSet<string> ();
        string notUsedSplit = "/" + AssetBundleBuild._NotUsed;
		// 需要分割的目錄
		HashSet<string> splitABDirs = new HashSet<string>();
		for (int i = 0; i < dirList.Count; ++i) {
			string dir = dirList[i];

			if (NotUsedDirHash.Contains(dir))
				continue;

			if (IsSplitABDir(dir))
			{
				// 分割的對象
				if (!splitABDirs.Contains(dir))
				{
					string[] files = System.IO.Directory.GetFiles(dir);
					if (files != null && files.Length > 0)
					{
						for (int j = 0; j < files.Length; ++j)
						{
							string fileName = files[j];
							if (AssetBundleBuild.FileIsResource(fileName))
							{
								splitABDirs.Add(dir);
								break;
							}
						}
					}
				}
				continue;
			}

            string abDir = dir;
            int notUsedIdx = abDir.IndexOf(notUsedSplit);
            if (notUsedIdx > 0)
            {
                abDir = abDir.Substring(0, notUsedIdx);
            }

			bool isMainFileMode = false;
		//	string localFileName = Path.GetFileName(dir);
			string localFileName = Path.GetFileName(abDir);
			bool isOnly = !localFileName.StartsWith(AssetBundleBuild._MainFileSplit);
			if (isOnly)
			{
				// 说明是单独文件模式
				isMainFileMode = true;
				string[] files = System.IO.Directory.GetFiles(dir);
				if (files != null)
				{
					for (int j = 0; j < files.Length; ++j)
					{
						string fileName = files[j];
						if (AssetBundleBuild.FileIsResource(fileName))
						{
						//	fileName = AssetBundleBuild.GetXmlFileName(fileName);
							abFiles.Add(fileName);
						}
					}
				}
			} else
			{
                abFiles.Add(abDir);
			}
			// 判断目录
			CheckDirsNotUsed(dir, NotUsedDirHash, abFiles, isMainFileMode);
		}
#if USE_UNITY5_X_BUILD
		AssetDatabase.RemoveUnusedAssetBundleNames();
		EditorUtility.UnloadUnusedAssetsImmediate();
#endif
		// 创建AssetBundleInfo
		for (int i = 0; i < abFiles.Count; ++i)
		{
			AssetBunbleInfo info = new AssetBunbleInfo(abFiles[i]);
			if (info.FileType != AssetBundleFileType.abError)
			{
                if (mAssetBundleMap.ContainsKey(info.Path))
                    continue;
				mAssetBundleList.Add(info);
				mAssetBundleMap.Add(info.Path, info);
			}
		}

		// 加入Split的AB LINK
		BuildSplitABDirs(splitABDirs);

        EditorUtility.ClearProgressBar();

        RefreshAllDependCount ();

		mAssetBundleList.Sort (AssetBunbleInfo.OnSort);
#if USE_UNITY5_X_BUILD
		EditorUtility.ClearProgressBar();
		AssetDatabase.Refresh();
		//AssetDatabase.RemoveUnusedAssetBundleNames();
	    //AssetDatabase.Refresh();
#endif
	}

	private void CheckDirsNotUsed(string dir, HashSet<string> NotUsedDirHash, List<string> abFiles, bool isMainFileMode)
	{
		string[] dirs = System.IO.Directory.GetDirectories(dir);
		if (dirs != null)
		{
			for (int j = 0; j < dirs.Length; ++j)
			{
				string path = dirs[j];
				string local = System.IO.Path.GetFileName(path);
				if (local.StartsWith(AssetBundleBuild._NotUsed))
				{
					//if (!AssetBundleBuild.DirExistResource(path))
					//	continue;
					NotUsedDirHash.Add(path);
					
					if (isMainFileMode)
					{
						// add Files
						List<string> subFiles = AssetBundleBuild.GetAllSubResFiles(path);
						if ((subFiles != null) && (subFiles.Count > 0))
							abFiles.AddRange(subFiles);
					}
				}
			}
		}
	}

	private void RefreshAllDependCount()
	{
		for (int i = 0; i < mAssetBundleList.Count; ++i) {
			AssetBunbleInfo info = mAssetBundleList[i];
			if (info == null)
				continue;
			info.RefreshAllDependCount();
		}
	}

	public void Clear()
	{
		CurTagIdx = 0;
		MaxTagFileCount = 0;
		mAssetBundleMap.Clear ();
		mAssetBundleList.Clear ();
	}

	public void Print()
	{
		for (int i = 0; i < mAssetBundleList.Count; ++i) {
			AssetBunbleInfo info = mAssetBundleList[i];
			if (info != null)
			{
				info.Print();
			}
		}
	}

	private bool GetBuildTarget(eBuildPlatform platform, ref BuildTarget target)
	{
		switch(platform) {
		case eBuildPlatform.eBuildAndroid:
		{
			target = BuildTarget.Android;
			break;
		}
			
		case eBuildPlatform.eBuildWindow:
		{
			target = BuildTarget.StandaloneWindows;
			break;
		}
		case eBuildPlatform.eBuildMac:
		{
			target = BuildTarget.StandaloneOSXIntel;
			break;
		}
		case eBuildPlatform.eBuildIOS:
		{
			target = BuildTarget.iOS;
			break;
		}
		default:
			return false;
		}

		return true;
	}

	private string CreateAssetBundleDir(eBuildPlatform platform, string exportDir)
	{

		string outPath;
		bool isExternal = false;
		if (!string.IsNullOrEmpty(exportDir))
		{
			isExternal = true;
			outPath = exportDir;
		}
		else
			outPath = "Assets/StreamingAssets";

		if (!Directory.Exists(outPath))
		{
			if (isExternal)
				Directory.CreateDirectory(outPath);
			else
				AssetDatabase.CreateFolder("Assets", "StreamingAssets");
		}

		switch(platform) {
		case eBuildPlatform.eBuildAndroid:
		{
			outPath += "/Android";
			if (!Directory.Exists(outPath)) 
			{
				if (isExternal)
					Directory.CreateDirectory(outPath);
				else
					AssetDatabase.CreateFolder("Assets/StreamingAssets", "Android");
			}
			break;
		}
		
		case eBuildPlatform.eBuildWindow:
		{
			outPath += "/Windows";
			if (!Directory.Exists(outPath))
			{
				if (isExternal)
					Directory.CreateDirectory(outPath);
				else
					AssetDatabase.CreateFolder("Assets/StreamingAssets", "Windows");
			}
			break;
		}
		case eBuildPlatform.eBuildMac:
		{
			outPath += "/Mac";
			if (!Directory.Exists(outPath))
			{
				if (isExternal)
					Directory.CreateDirectory(outPath);
				else
					AssetDatabase.CreateFolder("Assets/StreamingAssets", "Mac");
			}
			break;
		}
		case eBuildPlatform.eBuildIOS:
		{
			outPath += "/IOS";
			if (!Directory.Exists(outPath))
			{
				if (isExternal)
					Directory.CreateDirectory(outPath);
				else
					AssetDatabase.CreateFolder("Assets/StreamingAssets", "IOS");
			}
			break;
		}
		default:
			return string.Empty;
		}

		return outPath;
	}
	
#if USE_UNITY5_X_BUILD
	private string m_TempExportDir;
	private int m_TempCompressType;
	private BuildTarget m_TempBuildTarget;
	
	private void OnBuildTargetChanged()
	{
		EditorUserBuildSettings.activeBuildTargetChanged -= OnBuildTargetChanged;
		ProcessBuild_5_x(m_TempExportDir, m_TempCompressType, m_TempBuildTarget);
	}

	private AssetBundleManifest CallBuild_5_x_API(string exportDir, int compressType, BuildTarget target, bool isReBuild = true)
	{
		BuildAssetBundleOptions buildOpts = BuildAssetBundleOptions.DisableWriteTypeTree |
			BuildAssetBundleOptions.DeterministicAssetBundle;
		
		if (isReBuild)
			buildOpts |= BuildAssetBundleOptions.ForceRebuildAssetBundle;
		if (compressType == 0)
			buildOpts |= BuildAssetBundleOptions.UncompressedAssetBundle;
	#if UNITY_5_3 || UNITY_5_4
		else if (compressType == 2)
			buildOpts |= BuildAssetBundleOptions.ChunkBasedCompression;
	#endif

		AssetBundleManifest manifest = BuildPipeline.BuildAssetBundles(exportDir, buildOpts, target);
		EditorUtility.UnloadUnusedAssetsImmediate();

		return manifest;
	}
	
	void ProcessBuild_5_x(string exportDir, int compressType, BuildTarget target)
	{
		AssetBundleManifest manifest = CallBuild_5_x_API(exportDir, compressType, target);
		
		for (int i = 0; i < mAssetBundleList.Count; ++i)
		{
			AssetBunbleInfo info = mAssetBundleList[i];
			if ((info != null) && (!info.IsBuilded) && (info.SubFileCount > 0) && (info.FileType != AssetBundleFileType.abError))
			{
				info.IsBuilded = true;
				info.CompressType = compressType;
				info.RebuildDependFiles(manifest);
			}
		}
	}
	
#endif

	private void BuildAssetBundlesInfo_5_x(eBuildPlatform platform, string exportDir, int compressType)
	{	
#if USE_UNITY5_X_BUILD
		if (string.IsNullOrEmpty(exportDir))
			return;
		BuildTarget target = BuildTarget.Android;
		if (!GetBuildTarget(platform, ref target))
			return;
		
		if (EditorUserBuildSettings.activeBuildTarget != target)
		{
			EditorUserBuildSettings.activeBuildTargetChanged += OnBuildTargetChanged;
			m_TempExportDir = exportDir;
			m_TempCompressType = compressType;
			m_TempBuildTarget = target;
			EditorUserBuildSettings.SwitchActiveBuildTarget(target);
			return;
		}

		ProcessBuild_5_x(exportDir, compressType, target);
		
#endif
	}

    public static string GetAssetRelativePath(string fullPath)
    {
        if (string.IsNullOrEmpty(fullPath))
            return string.Empty;
        fullPath = fullPath.Replace("\\", "/");
        int index = fullPath.IndexOf("Assets/", StringComparison.CurrentCultureIgnoreCase);
        if (index < 0)
            return fullPath;
        string ret = fullPath.Substring(index);
        return ret;
    }

	private void BuildAssetBundleInfo(AssetBunbleInfo info, eBuildPlatform platform, string exportDir, int compressType)
	{
#if USE_UNITY5_X_BUILD
#else
		if ((info == null) || (info.IsBuilded) || string.IsNullOrEmpty(exportDir) || (info.FileType == AssetBundleFileType.abError) || (info.SubFileCount <= 0))
			return;
		BuildTarget target = BuildTarget.Android;
		if (!GetBuildTarget (platform, ref target))
			return;

		if (info.DependFileCount > 0) {
			// check DepndFile
			for (int i = 0; i < info.DependFileCount; ++i)
			{
				string fileName = info.GetDependFiles(i);
				if (!string.IsNullOrEmpty(fileName))
				{
					AssetBunbleInfo depInfo;
					if ((!mAssetBundleMap.TryGetValue(fileName, out depInfo)) || (depInfo == null))
					{
						string errStr = string.Format("AssetBundle [{0}] depend file: {1} is not exists", info.Path, fileName);
						Debug.LogError(errStr);
						return;
					}
				    
					if ((!depInfo.IsBuilded) && (depInfo.AllDependCount != info.AllDependCount))
					{
						string errStr = string.Format("AssetBundle [{0}] depend file: {1} is not build first", info.Path, fileName);
						Debug.LogError(errStr);
						return;
					}

				}
			}
		}

		// Create AssetBundle
		string localOutFileName = info.BundleFileName;
		string outFileName = string.Format("{0}/{1}", exportDir, localOutFileName);
		if (info.IsScene) {
			string[] fileArr = info.GetSubFiles ();
			if (fileArr == null) {
				string errStr = string.Format ("AssetBundle [{0}] Subfiles is empty", info.Path);
				Debug.LogError (errStr);
				return;
			}

			BuildOptions buildOpts = BuildOptions.BuildAdditionalStreamedScenes;
			if (compressType != 1)
				buildOpts |= BuildOptions.UncompressedAssetBundle;
			
			//BuildPipeline.BuildPlayer(fileArr, outFileName, target, buildOpts);
			BuildPipeline.BuildStreamedSceneAssetBundle (fileArr, outFileName, target, buildOpts); 
			info.IsBuilded = true;
			info.CompressType = compressType;
			return;
		} else {
			// not BuildAssetBundleOptions.CollectDependencies
			BuildAssetBundleOptions buildOpts = BuildAssetBundleOptions.CompleteAssets |
												BuildAssetBundleOptions.DisableWriteTypeTree |
												BuildAssetBundleOptions.DeterministicAssetBundle;
			if (compressType != 1)
				buildOpts |= BuildAssetBundleOptions.UncompressedAssetBundle;

			if (info.IsMainAsset)
			{
				string mainFileName = info.GetSubFiles(0);

				UnityEngine.Object mainAsset = AssetDatabase.LoadMainAssetAtPath(mainFileName);
				if (mainAsset == null)
				{
					string errStr = string.Format ("AssetBundle [{0}] Subfiles has null UnityObject", info.Path);
					Debug.LogError (errStr);
					return;
				}

				bool ret = BuildPipeline.BuildAssetBundle(mainAsset, null, outFileName, buildOpts, target);
				if (!ret)
				{
					string errStr = string.Format ("AssetBundle [{0}] build not ok", info.Path);
					Debug.LogError (errStr);
					return;
				}

				info.IsBuilded = true;
				info.CompressType = compressType;
			} else
			if (info.FileType == AssetBundleFileType.abDirFiles)
			{
				List<UnityEngine.Object> assetObjs = new List<UnityEngine.Object>();

				for (int i = 0; i < info.SubFileCount; ++i)
				{
					string subFileName = info.GetSubFiles(i);
					if (string.IsNullOrEmpty(subFileName))
						continue;
					Type t = AssetBundleBuild.GetResourceExtType(subFileName);

					if (t == null)
					{
						string errStr = string.Format ("AssetBundle [{0}] Subfile [{1}] has null Type", info.Path, subFileName);
						Debug.LogError (errStr);
						return;
					}

					UnityEngine.Object asset = AssetDatabase.LoadAssetAtPath(subFileName, t);
					if (asset == null)
					{
						string errStr = string.Format ("AssetBundle [{0}] Subfile [{1}] has null UnityObject", info.Path, subFileName);
						Debug.LogError (errStr);
						return;
					}

					assetObjs.Add(asset);
				}

				bool ret = BuildPipeline.BuildAssetBundle(null, assetObjs.ToArray(), outFileName, buildOpts, target);
				if (!ret)
				{
					string errStr = string.Format ("AssetBundle [{0}] build not ok", info.Path);
					Debug.LogError (errStr);
					return;
				}

				info.IsBuilded = true;
				info.CompressType = compressType;
			}
		}
#endif
	}

	private void ResetAssetBundleInfo ()
	{
		for (int i = 0; i < mAssetBundleList.Count; ++i) {
			AssetBunbleInfo info = mAssetBundleList[i];
			if (info != null)
			{
				info.IsBuilded = false;
				info.CompressType = 0;
			}
		}
	}

	private void ExportXmlStr(StringBuilder builder, bool isMd5, string outPath)
	{
		if (builder == null)
			return;

		for (int i = 0; i < mAssetBundleList.Count; ++i) {
			AssetBunbleInfo info = mAssetBundleList[i];
			if ((info != null) && info.IsBuilded)
                info.ExportXml(builder, isMd5, outPath);
		}
	}

	// 导出二进制
	private void ExportBinarys(string exportPath, bool isMd5)
	{
		if (string.IsNullOrEmpty (exportPath))
			return;
		string fullPath = Path.GetFullPath (exportPath);
		if (string.IsNullOrEmpty (fullPath))
			return;
		#if USE_DEP_BINARY_AB
		string fileName = "Assets/AssetBundles.xml";
		#else
		string fileName = string.Format ("{0}/AssetBundles.xml", fullPath);
		#endif
		if (System.IO.File.Exists (fileName)) {
			System.IO.File.Delete(fileName);
		}

		FileStream stream = new FileStream (fileName, FileMode.Create);

		int abFileCount = mAssetBundleList == null ? 0: mAssetBundleList.Count;
		DependBinaryFile.ExportFileHeader(stream, abFileCount, DependBinaryFile.FLAG_UNCOMPRESS);

		for (int i = 0; i < mAssetBundleList.Count; ++i) {
			AssetBunbleInfo info = mAssetBundleList[i];
			if ((info != null) && info.IsBuilded)
				info.ExportBinary(stream, isMd5, fullPath);
		}

		stream.Close ();
		stream.Dispose ();
	}

	// export xml
	private void ExportXml(string exportPath, bool isMd5 = false)
	{
		if (string.IsNullOrEmpty (exportPath))
			return;
		string fullPath = Path.GetFullPath (exportPath);
		if (string.IsNullOrEmpty (fullPath))
			return;
		string fileName = string.Format ("{0}/AssetBundles.xml", fullPath);
		if (System.IO.File.Exists (fileName)) {
			System.IO.File.Delete(fileName);
		}

		FileStream stream = new FileStream (fileName, FileMode.Create);

		StringBuilder builder = new StringBuilder ();
		builder.Append("<?xml version=\"1.0\" encoding=\"utf-8\" ?>");
		builder.AppendLine ();
		builder.Append ("<AssetBundles>");
		builder.AppendLine ();
		ExportXmlStr (builder, isMd5, fullPath);
		builder.Append("</AssetBundles>");
		string str = builder.ToString ();
		byte[] bytes = System.Text.Encoding.UTF8.GetBytes (str);
		stream.Write (bytes, 0, bytes.Length);
		stream.Dispose ();
	}

	private void RemoveBundleManifestFiles_5_x(string outPath)
	{
#if USE_UNITY5_X_BUILD
		string[] files = Directory.GetFiles(outPath, "*.manifest", SearchOption.TopDirectoryOnly);
		for (int i = 0; i < files.Length; ++i)
		{
			File.Delete(files[i]);
		}
#endif
	}

	public string GetUnityEditorPath()
	{
#if UNITY_EDITOR_WIN
			string pathList = System.Environment.GetEnvironmentVariable("Path", EnvironmentVariableTarget.Machine);
			if (string.IsNullOrEmpty(pathList))
				return string.Empty;

			char[] split = new char[1];
			split[0] = ';';
			string[] paths = pathList.Split(split, StringSplitOptions.RemoveEmptyEntries);
			if (paths == null || paths.Length <= 0)
				return string.Empty;
			for (int i = 0; i < paths.Length; ++i) {
				string p = paths[i];
				if (string.IsNullOrEmpty(p))
					continue;
				int unityIdx = p.IndexOf("Unity", StringComparison.CurrentCultureIgnoreCase);
				if (unityIdx < 0)
					continue;
				p = p.Replace('\\', '/');
				int editorIdx = p.IndexOf("/Editor", StringComparison.CurrentCultureIgnoreCase);
				if (editorIdx < 0 || editorIdx <= unityIdx)
					continue;
				return p;
			}
#endif
			return string.Empty;
	}

	public bool BuildCSharpProject(string ProjFileName, string buildExe)
	{
#if UNITY_EDITOR_WIN
		if (string.IsNullOrEmpty(ProjFileName) || string.IsNullOrEmpty(buildExe))
			return false;
		if (!File.Exists(ProjFileName))
		{
			Debug.LogErrorFormat("【编译】不存在文件：", ProjFileName);
			return false;
		}

		string unityEditorPath = GetUnityEditorPath();
		if (string.IsNullOrEmpty(unityEditorPath))
		{
			Debug.LogError("请增加UnityEditor环境变量Path");
			return false;
		}

		unityEditorPath = unityEditorPath.Replace('/', '\\');

		string preCmd = string.Format("start /D \"{0}\\Data\\MonoBleedingEdge\\bin\" /B", unityEditorPath);
		//string preCmd = "start /B";

		 ProjFileName = ProjFileName.Replace('/' , '\\');
		 buildExe = buildExe.Replace('/', '\\');
		string cmd = string.Format("{0} {1} {2} /p:Configuration=Release", preCmd, buildExe, ProjFileName);
		AssetBundleBuild.RunCmd(cmd);
		return true;
#else
		return false;
#endif
	}

	public void BuildCSharpProjectUpdateFile(string streamAssetsPath, string outPath, string version)
	{
#if UNITY_EDITOR_WIN
		/*
		string unityEditorPath = GetUnityEditorPath();
		if (string.IsNullOrEmpty(unityEditorPath))
			return;

		string buildExe = unityEditorPath + "/Data/MonoBleedingEdge/lib/mono/unity/xbuild.exe";
		*/
	//	string buildExe = "xbuild.bat";

		string rootPath = System.IO.Directory.GetCurrentDirectory();
		rootPath = rootPath.Replace('\\', '/');
		string projFileName = rootPath + "/Assembly-CSharp.csproj";
	//	if (!BuildCSharpProject(projFileName, buildExe))
	//		return;

		string resDir = outPath + '/' + version;
		resDir = System.IO.Path.GetFullPath(resDir);

		if (!System.IO.Directory.Exists(resDir))
			System.IO.Directory.CreateDirectory(resDir);

		resDir = resDir.Replace('\\', '/');

		string[] csharpFiles = new string[1];
		csharpFiles[0] = projFileName;

		string fileListFileName1 = streamAssetsPath + "/fileList.txt";
		string fileListFileName2 = resDir + "/fileList.txt";

		List<string> externFiles = new List<string>();
		List<bool> firstDowns = new List<bool>();
		for (int i = 0; i < csharpFiles.Length; ++i)
		{
			string s = csharpFiles[i];

			string dllFileName = System.IO.Path.GetFileNameWithoutExtension(s) + ".dll";
			string f = string.Format("{0}/Temp/bin/Release/{1}", rootPath, dllFileName);

			if (File.Exists(f))
			{
				externFiles.Add(f);
				firstDowns.Add(true);

				string dllMd5 = AssetBunbleInfo.Md5(f, false);
				string dstDllFileName = resDir + "/" + dllMd5 + ".dll";
				File.Copy(f, dstDllFileName);
			}
		}

		string srcFileName = streamAssetsPath + "/AssetBundles.xml";
		if (File.Exists(srcFileName))
		{
			externFiles.Add(srcFileName);
			firstDowns.Add(true);
			string md5Str = AssetBunbleInfo.Md5(srcFileName, false);
			string dstFileName = resDir + "/" + md5Str + ".xml";
			File.Copy(srcFileName, dstFileName);
		}

		string[] fileArr = externFiles.ToArray();
		bool[] firstDownArr = firstDowns.ToArray();
		ExternMd5WriteToFileList(fileArr, fileListFileName1, firstDownArr);
		ExternMd5WriteToFileList(fileArr, fileListFileName2, firstDownArr);
#endif
	}

	private void CSharpDllCopyTo(string srcDll, string dstDll)
	{
		if (string.IsNullOrEmpty(srcDll) || string.IsNullOrEmpty(dstDll))
			return;

	}

	private void ExternMd5WriteToFileList(string[] files, string fileListFileName, bool[] isFirstDowns)
	{
		if (files == null || files.Length <= 0 || string.IsNullOrEmpty(fileListFileName))
			return;

		if (System.IO.File.Exists(fileListFileName))
		{
			FileStream fileStream = new FileStream(fileListFileName, FileMode.Open, FileAccess.Read);
			if (fileStream.Length > 0)
			{
				byte[] src = new byte[fileStream.Length];
				
				fileStream.Read(src, 0, src.Length);
				fileStream.Close();
				fileStream.Dispose();
				
				string s = System.Text.Encoding.ASCII.GetString(src);
				s = s.Trim();
				if (!string.IsNullOrEmpty(s))
				{
					ResListFile resFile = new ResListFile();
					resFile.Load(s);
					bool isNews = false;
					for (int i = 0; i < files.Length; ++i)
					{
						string f = Path.GetFileName(files[i]);
						f = f.Trim();
						if (string.IsNullOrEmpty(f))
							continue;
						// string key = AssetBunbleInfo.Md5(csharpFiles[i], true);
						string ext = Path.GetExtension(files[i]);
						string value = AssetBunbleInfo.Md5(files[i], false) + ext;

						bool isFirstDown = false;
						if (isFirstDowns != null && i < isFirstDowns.Length)
							isFirstDown = isFirstDowns[i]; 

						if (!resFile.AddFile(f, value, isFirstDown))
							Debug.LogErrorFormat("【BuildCSharpProjectUpdateFile】 file {0} error!", f);
						else
							isNews = true;
					}
					
					if (isNews)
						resFile.SaveToFile(fileListFileName);
				}
			} else
			{
				fileStream.Close();
				fileStream.Dispose();
			}
		}

	}

	private void CreateBundleResUpdateFiles(string streamAssetsPath, string outPath, string version, bool isRemoveVersionDir)
	{
		string resDir = outPath + '/' + version;
		resDir = System.IO.Path.GetFullPath(resDir);

		if (isRemoveVersionDir)
		{
			if (System.IO.Directory.Exists(resDir))
			{
				string[] fileNames = System.IO.Directory.GetFiles(resDir, "*.*", SearchOption.TopDirectoryOnly);
				if (fileNames != null)
				{
					for(int i = 0; i < fileNames.Length; ++i)
					{
						System.IO.File.Delete(fileNames[i]);
					}
				}
			}
		}

		if (!System.IO.Directory.Exists(resDir))
			System.IO.Directory.CreateDirectory(resDir);

		List<string> fileList = new List<string>();
		for (int i = 0; i < mAssetBundleList.Count; ++i)
		{
			AssetBunbleInfo info = mAssetBundleList[i];
			if ((info != null) && info.IsBuilded && (info.SubFileCount > 0) && (info.FileType != AssetBundleFileType.abError))
			{
				string md5FileName = info.Md5BundleFileName(streamAssetsPath);
				string newMd5FileName = info.Md5BundleFileName(streamAssetsPath, false);
				string bundleFileName = info.BundleFileName;
				if (string.IsNullOrEmpty(bundleFileName) || string.IsNullOrEmpty(md5FileName) || string.IsNullOrEmpty(newMd5FileName))
					continue;
				string fileCompareStr = string.Format("{0}={1}", md5FileName, newMd5FileName);

				bundleFileName = streamAssetsPath + '/' + bundleFileName;
				newMd5FileName = resDir + '/' + newMd5FileName;
				if (File.Exists(bundleFileName))
				{
					fileList.Add(fileCompareStr);
					if (!File.Exists(newMd5FileName))
					{
						File.Copy(bundleFileName, newMd5FileName);
					} else
					{
						Debug.LogErrorFormat("Bundle To Md5: [srcFile: {0}] => [dstFile: {1}] is exists", 
						                       info.BundleFileName, newMd5FileName);
					}
				}
			}
		}

		// write fileList file
		string fileListFileName = streamAssetsPath + "/fileList.txt";
		string fileListFileName1 = resDir + "/fileList.txt";
		FileStream fileStream = new FileStream(fileListFileName, FileMode.Create, FileAccess.Write);
		try
		{
			for (int i = 0; i < fileList.Count; ++i)
			{
				string flieListStr = fileList[i];
				if (!string.IsNullOrEmpty(flieListStr))
				{
					flieListStr += "\r\n";
					byte[] fileListBytes = System.Text.Encoding.ASCII.GetBytes(flieListStr);
					if (fileListBytes != null && fileListBytes.Length > 0)
						fileStream.Write(fileListBytes, 0, fileListBytes.Length);
				}
			}
		} 
		finally
		{
			fileStream.Close();
			fileStream.Dispose();
		}

		File.Copy(fileListFileName, fileListFileName1);

		// write version file
		string versionFileName = streamAssetsPath + "/version.txt";
		string versionFileName1 = resDir + "/version.txt";
		fileStream = new FileStream(versionFileName, FileMode.Create, FileAccess.Write);
		try
		{
			string fileListMd5 = AssetBunbleInfo.Md5(fileListFileName, false);
			string versionStr = string.Format("res={0}\r\nfileList={1}", version, fileListMd5);
			byte[] versionBytes = System.Text.Encoding.ASCII.GetBytes(versionStr);
			fileStream.Write(versionBytes, 0, versionBytes.Length);
		}
		finally
		{
			fileStream.Close();
			fileStream.Dispose();
		}

		File.Copy(versionFileName, versionFileName1);
	}

//	private static readonly bool _cIsOnlyFileNameMd5 = true;
	private void ChangeBundleFileNameToMd5(string outPath)
	{
		// Temp script
		for (int i = 0; i < mAssetBundleList.Count; ++i)
		{
			AssetBunbleInfo info = mAssetBundleList[i];
			if ((info != null) && info.IsBuilded && (info.SubFileCount > 0) && (info.FileType != AssetBundleFileType.abError))
			{
				string oldFileName = outPath + '/' + info.BundleFileName;
				string md5FileName = info.Md5BundleFileName(outPath);
				if (string.IsNullOrEmpty(md5FileName))
					continue;
				string newFileName = outPath + '/' + md5FileName;
				if (File.Exists(oldFileName))
				{
					if (File.Exists(newFileName))
						File.Delete(newFileName);

					FileInfo fileInfo = new FileInfo(oldFileName);
					fileInfo.MoveTo(newFileName);
				
					/*
					if (!File.Exists(newFileName))
					{
						FileInfo fileInfo = new FileInfo(oldFileName);
						fileInfo.MoveTo(newFileName);
					} else
					{
						File.Delete(oldFileName);

						Debug.LogWarningFormat("Bundle To Md5: [srcFile: {0}] => [dstFile: {1}] is exists", 
						                       info.BundleFileName, md5FileName);
					}*/
				}
			}
		}
	}

    // 5.x打包方法
    private void BuildAssetBundles_5_x(eBuildPlatform platform, int compressType, string outPath, bool isMd5)
    {
#if USE_UNITY5_X_BUILD
        // 5.x不再需要收集依赖PUSH和POP
        Caching.CleanCache();
        string exportDir = CreateAssetBundleDir(platform, outPath);
        if (mAssetBundleList.Count > 0)
        {

		#if USE_DEP_BINARY && USE_DEP_BINARY_AB
			AssetImporter xmlImport = AssetImporter.GetAtPath("Assets/AssetBundles.xml");
			if (xmlImport != null)
			{
				if (!string.IsNullOrEmpty(xmlImport.assetBundleName))
				{
					xmlImport.assetBundleName = string.Empty;
					xmlImport.SaveAndReimport();
				}
			}
		#endif
			/*
            for (int i = 0; i < mAssetBundleList.Count; ++i)
            {
                AssetBunbleInfo info = mAssetBundleList[i];
                if ((info != null) && (!info.IsBuilded) && (info.SubFileCount > 0) && (info.FileType != AssetBundleFileType.abError))
                    BuildAssetBundleInfo_5_x(info, platform, exportDir, compressType);
            }*/
			BuildAssetBundlesInfo_5_x(platform, exportDir, compressType);

			// 是否存在冗余资源，如果有打印出来
			CheckRongYuRes();

		#if USE_DEP_BINARY
			// 二进制格式
			ExportBinarys(exportDir, isMd5);
		#else
            // export xml
            ExportXml(exportDir, isMd5);
		#endif
			
            AssetDatabase.Refresh();

		#if USE_DEP_BINARY && USE_DEP_BINARY_AB
			BuildTarget target = BuildTarget.Android;
			if (GetBuildTarget(platform, ref target))
			{
				if (xmlImport == null)
					xmlImport = AssetImporter.GetAtPath("Assets/AssetBundles.xml");
				if (xmlImport != null)
				{
					xmlImport.assetBundleName = "AssetBundles.xml";
					xmlImport.SaveAndReimport();
		#if UNITY_5_3 || UNITY_5_4
					CallBuild_5_x_API(exportDir, compressType, target, false);
		#else
					CallBuild_5_x_API(exportDir, 0, target,  false);
		#endif

					AssetDatabase.Refresh();

					string xmlSrcFile = string.Format("{0}/assetbundles.xml", exportDir);
					if (File.Exists(xmlSrcFile))
					{
						string xmlDstFile = string.Format("{0}/AssetBundles.xml", exportDir);
						File.Move(xmlSrcFile, xmlDstFile);
						AssetDatabase.Refresh();
					}
				}
			}

		#endif
			
			RemoveBundleManifestFiles_5_x(exportDir);

			if (isMd5)
			{
				ProcessVersionRes(exportDir, platform);
				ChangeBundleFileNameToMd5(exportDir);
			}

			AssetDatabase.Refresh();

            ResetAssetBundleInfo();
        }
#endif
    }

	private void ProcessVersionRes(string streamAssetsPath, eBuildPlatform platform)
	{
	//	if (platform == eBuildPlatform.eBuildAndroid || platform == eBuildPlatform.eBuildIOS ||
	//	    platform == eBuildPlatform.eBuildWindow)
		{
			string versionDir = AssetBundleBuild.GetCurrentPackageVersion(platform);
			CreateBundleResUpdateFiles(streamAssetsPath, "outPath", versionDir, true);
			BuildCSharpProjectUpdateFile(streamAssetsPath, "outPath", versionDir);
		}
	}

	// isCompress
	public void BuildAssetBundles(eBuildPlatform platform, int compressType, bool isMd5 = false, string outPath = null)
	{
		AssetBundleRefHelper.ClearFileMetaMap();
		AssetBunbleInfo.ClearMd5FileMap();
#if USE_UNITY5_X_BUILD
        // 5.x版本采用新打包
        string appVersion = Application.unityVersion;
        if (appVersion.StartsWith("5."))
        {
            BuildAssetBundles_5_x(platform, compressType, outPath, isMd5);
            return;
        }
#else

		Caching.CleanCache ();

		string exportDir = CreateAssetBundleDir (platform);
		int dependLevel = -1;
		int pushCnt = 0;
		if (mAssetBundleList.Count > 0) {
			for (int i = 0; i < mAssetBundleList.Count; ++i) {
				AssetBunbleInfo info = mAssetBundleList [i];
				if ((info != null) && (!info.IsBuilded) && (info.SubFileCount > 0) && (info.FileType != AssetBundleFileType.abError)) {
					bool isPush = (dependLevel < info.DependFileCount);

					if (isPush)
					{
						BuildPipeline.PushAssetDependencies();
						++pushCnt;
					}

					dependLevel = info.DependFileCount;

					BuildAssetBundleInfo (info, platform, exportDir, compressType);
				}
			}

			for (int i = 0; i < pushCnt; ++i)
			{
				BuildPipeline.PopAssetDependencies();
			}

			#if USE_DEP_BINARY
			// 二进制格式
			ExportBinarys(exportDir, isMd5);
			#else
			// export xml
			ExportXml(exportDir, isMd5);
			#endif

			if (isMd5)
			{
				ProcessVersionRes(exportDir, platform);
				ChangeBundleFileNameToMd5(exportDir);
			}

			AssetDatabase.Refresh ();

			ResetAssetBundleInfo ();
		}
#endif
    }

	private void ProcessPackage(BuildTarget platform, string outputFileName, bool isNew, bool canProfilter, bool isDebug, bool isDevelop)
	{
		var scenes = EditorBuildSettings.scenes;
		List<string> sceneNameList = new List<string> ();
		for (int i = 0; i < scenes.Length; ++i) {
			var scene = scenes[i];
			if (scene != null)
			{
				if (scene.enabled)
				{
					//string sceneName = Path.GetFileNameWithoutExtension(scene.path);
					if (System.IO.File.Exists(scene.path))
					{
						//Debug.Log("Apk scenePath: " + scene.path);
						if (!string.IsNullOrEmpty(scene.path))
							sceneNameList.Add(scene.path);
					}
				}
			}
		}
		
		if (sceneNameList.Count <= 0)
			return;
		
		string[] levelNames = sceneNameList.ToArray ();
		
		BuildOptions opts;
		if (isNew)
			opts = BuildOptions.None;
		else
			opts = BuildOptions.AcceptExternalModificationsToPlayer;
		
		if (canProfilter)
			opts |= BuildOptions.ConnectWithProfiler;
		if (isDebug)
			opts |= BuildOptions.AllowDebugging;
		if (isDevelop)
			opts |= BuildOptions.Development;
		
		BuildPipeline.BuildPlayer (levelNames, outputFileName, platform, opts); 
	}

	private bool m_TempIsNew;
	private bool m_TempCanProfilter;
	private bool m_TempIsDebug;
	private bool m_TempIsDevep;
	private string m_TempOutput;
	private BuildTarget m_TempTarget;

	private void OnBuildPackagePlatformChanged()
	{
		EditorUserBuildSettings.activeBuildTargetChanged -= OnBuildPackagePlatformChanged;
		ProcessPackage(m_TempTarget, m_TempOutput, m_TempIsNew, m_TempCanProfilter, m_TempIsDebug, m_TempIsDevep);
	}

	public bool BuildPackage(eBuildPlatform buildPlatform, string outputFileName, bool isNew, bool canProfilter = false, bool isDebug = false, bool isDevelop = false)
	{
		if (string.IsNullOrEmpty (outputFileName))
			return false;

		BuildTarget platform = BuildTarget.Android;
		if (!GetBuildTarget (buildPlatform, ref platform))
			return false;

		EditorUserBuildSettings.allowDebugging = isDebug;
		EditorUserBuildSettings.development = isDevelop;
		EditorUserBuildSettings.connectProfiler = canProfilter;
		if (EditorUserBuildSettings.activeBuildTarget != platform)
		{
			m_TempIsNew = isNew;
			m_TempCanProfilter = canProfilter;
			m_TempIsDebug = isDebug;
			m_TempIsDevep = isDevelop;
			m_TempOutput = outputFileName;
			m_TempTarget = platform;
			EditorUserBuildSettings.activeBuildTargetChanged += OnBuildPackagePlatformChanged;
			EditorUserBuildSettings.SwitchActiveBuildTarget (platform);
			return true;
		}

		ProcessPackage(platform, outputFileName, isNew, canProfilter, isDebug, isDevelop);
		return true;
	}

	private Dictionary<string, AssetBunbleInfo> mAssetBundleMap = new Dictionary<string, AssetBunbleInfo>();
	// 排序，按照有木有依赖来排序
	private List<AssetBunbleInfo> mAssetBundleList = new List<AssetBunbleInfo>();
}

[ExecuteInEditMode]
public static class AssetBundleBuild
{
    private static string cAssetsResourcesPath = "Assets/Resources/";
	// 支持的资源文件格式
	private static readonly string[] ResourceExts = {".prefab", ".fbx",
													 ".png", ".jpg", ".dds", ".gif", ".psd", ".tga", ".bmp",
													 ".txt", ".bytes", ".xml", ".csv", ".json",
													 ".controller", ".shader", ".anim", ".unity", ".mat",
													 ".wav", ".mp3", ".ogg",
													 ".ttf",
													 ".shadervariants", ".asset"};
	
	private static readonly string[] ResourceXmlExts = {".prefab", ".fbx",
														".tex", ".tex",  ".tex", ".tex", ".tex", ".tex", ".tex",
														".bytes", ".bytes", ".bytes", ".bytes", ".bytes",
														".controller", ".shader", ".anim", ".unity", ".mat",
														".audio", ".audio", ".audio",
													    ".ttf",
														".shaderVar", ".asset"};

	private static readonly Type[] ResourceExtTypes = {
														typeof(UnityEngine.GameObject), typeof(UnityEngine.GameObject),
														typeof(UnityEngine.Texture), typeof(UnityEngine.Texture), typeof(UnityEngine.Texture), typeof(UnityEngine.Texture), typeof(UnityEngine.Texture), typeof(UnityEngine.Texture), typeof(UnityEngine.Texture),
														typeof(UnityEngine.TextAsset), typeof(UnityEngine.TextAsset), typeof(UnityEngine.TextAsset), typeof(UnityEngine.TextAsset), typeof(UnityEngine.TextAsset),
														typeof(UnityEngine.Object), typeof(UnityEngine.Shader), typeof(UnityEngine.AnimationClip), null, typeof(UnityEngine.Material),
														typeof(UnityEngine.AudioClip), typeof(UnityEngine.AudioClip), typeof(UnityEngine.AudioClip),
														typeof(UnityEngine.Font),
														typeof(UnityEngine.ShaderVariantCollection), typeof(UnityEngine.ScriptableObject)
	};

	private static readonly string[] _DirSplit = {"\\"};

	// 目录中所有子文件为一个AB
	public static readonly string _MainFileSplit = "@";
	// _ 表示这个文件夹被忽略
	public static readonly string _NotUsed = "_";
	// Resources目录应该被忽略(未实现)

	public static string GetXmlExt(string ext)
	{
		if (string.IsNullOrEmpty (ext))
			return string.Empty;

		for (int i = 0; i < ResourceExts.Length; ++i) {
			if (string.Compare(ext, ResourceExts[i], true) == 0)
			{
#if USE_HAS_EXT
				return ResourceExts[i];
#else
				return ResourceXmlExts[i];
#endif
			}
		}

		return string.Empty;
	}

	public static string GetXmlFileName(string fileName)
	{
		if (string.IsNullOrEmpty (fileName))
			return string.Empty;
		string ext = Path.GetExtension (fileName);
		string newExt = GetXmlExt (ext);
		if (string.IsNullOrEmpty (newExt))
			return string.Empty;
		if (string.Compare (newExt, ".unity", true) == 0)
			fileName = Path.GetFileName (fileName);
		return Path.ChangeExtension (fileName, newExt);
	}

	public static Type GetResourceExtType(string fileName)
	{
		if (string.IsNullOrEmpty (fileName))
			return null;
		string ext = Path.GetExtension (fileName);
		if (string.IsNullOrEmpty (ext))
			return null;

		for (int i = 0; i < ResourceExts.Length; ++i) {
			if (string.Compare(ext, ResourceExts[i], true) == 0)
			{
				return ResourceExtTypes[i];
			}
		}

		return null;
	}


	private static void GetAllSubResFiles(string fullPath, List<string> fileList)
	{
		if ((fileList == null) || (string.IsNullOrEmpty(fullPath)))
			return;
		
		string[] files = System.IO.Directory.GetFiles (fullPath);
		if ((files != null) && (files.Length > 0)) {
			for (int i = 0; i < files.Length; ++i)
			{
				string fileName = files[i];
				if (FileIsResource(fileName))
					fileList.Add(fileName);
			}
		}
		
		string[] dirs = System.IO.Directory.GetDirectories (fullPath);
		if (dirs != null) {
			for (int i = 0; i < dirs.Length; ++i)
			{
				GetAllSubResFiles(dirs[i], fileList);
			}
		}
	}
	
	public static List<string> GetAllSubResFiles(string fullPath)
	{
		List<string> fileList = new List<string> ();
		GetAllSubResFiles (fullPath, fileList);
		return fileList;
	}

	public static List<string> GetAllLocalSubDirs(string rootPath)
	{
		if (string.IsNullOrEmpty (rootPath))
			return null;
		string fullRootPath = System.IO.Path.GetFullPath (rootPath);
		if (string.IsNullOrEmpty (fullRootPath))
			return null;

		string[] dirs = System.IO.Directory.GetDirectories (fullRootPath);
		if ((dirs == null) || (dirs.Length <= 0))
			return null;
		List<string> ret = new List<string> ();

		for (int i = 0; i < dirs.Length; ++i) {
			string dir = AssetBunbleInfo.GetLocalPath(dirs[i]);
			ret.Add (dir);
		}
		for (int i = 0; i < dirs.Length; ++i) {
			string dir = dirs[i];
			List<string> list = GetAllLocalSubDirs(dir);
			if (list != null)
				ret.AddRange(list);
		}

		return ret;
	}

	private static bool IsVaildSceneResource(string fileName)
	{
		bool ret = false;

		if (string.IsNullOrEmpty (fileName))
			return ret;

		string localFileName = AssetBunbleInfo.GetLocalPath (fileName);
		if (string.IsNullOrEmpty (localFileName))
			return ret;

		var scenes = EditorBuildSettings.scenes;
		if (scenes == null)
			return ret;

		var iter = scenes.GetEnumerator ();
		while (iter.MoveNext()) {
			EditorBuildSettingsScene scene = iter.Current as EditorBuildSettingsScene;
			if ((scene != null) && scene.enabled)
			{
				if (string.Compare(scene.path, localFileName, true) == 0)
				{
					ret = true;
					break;
				}
			}
		}

		return ret;
	}

    // 是否在Assets/Resources内
    public static bool IsInAssetsResourcesDir(string fileName)
    {
        if (string.IsNullOrEmpty(fileName))
            return false;
        int idx = fileName.IndexOf(cAssetsResourcesPath);
        return (idx >= 0);
    }

    // 是否在其他的Resources目录
    public static bool IsOtherResourcesDir(string fileName)
    {
        if (string.IsNullOrEmpty(fileName))
            return false;
        if (IsInAssetsResourcesDir(fileName))
            return false;
        int idx = fileName.IndexOf("/Resources/");
        return (idx > 0);
    }

    // 文件是否是脚本
    public static bool FileIsScript(string fileName)
    {
        if (string.IsNullOrEmpty(fileName))
            return false;
        string ext = Path.GetExtension(fileName);
        if (string.IsNullOrEmpty(ext))
            return false;
        if (string.Compare(ext, ".cs", true) == 0)
            return true;
        return false;
    }

	public static bool FileIsResource(string fileName)
	{
		if (string.IsNullOrEmpty (fileName))
			return false;
		string ext = Path.GetExtension (fileName);
		if (string.IsNullOrEmpty (ext))
			return false;
		for (int i = 0; i < ResourceExts.Length; ++i) {
			if (string.Compare(ext, ResourceExts[i], true) == 0)
			{
				if ((ResourceExts[i] == ".fbx") || (ResourceExts[i] == ".controller"))
				{
					// ingore xxx@idle.fbx
					string name = Path.GetFileNameWithoutExtension(fileName);
					if (name.IndexOf('@') >= 0)
						return false;
				} else
				if (ResourceExts[i] == ".unity")
				{
					if (!IsVaildSceneResource(fileName))
						return false;
				}
				return true;
			}
		}

		return false;
	}

	// 根据目录判断是否有资源文件
	public static bool DirExistResource(string path)
	{
		if (string.IsNullOrEmpty (path))
			return false;
		string fullPath = Path.GetFullPath (path);
		if (string.IsNullOrEmpty (fullPath))
			return false;

		string[] files = System.IO.Directory.GetFiles (fullPath);
		if ((files == null) || (files.Length <= 0))
			return false;
		for (int i = 0; i < files.Length; ++i) {
			string ext = System.IO.Path.GetExtension(files[i]);
			if (string.IsNullOrEmpty(ext))
				continue;
			for (int j = 0; j < ResourceExts.Length; ++j)
			{
				if (string.Compare(ext, ResourceExts[j], true) == 0)
				{
					if ((ResourceExts[j] == ".fbx") || (ResourceExts[j] == ".controller"))
					{
						// ingore xxx@idle.fbx
						string name = Path.GetFileNameWithoutExtension(files[i]);
						if (name.IndexOf('@') >= 0)
							return false;
					} else
					if (ResourceExts[j] == ".unity")
					{
						if (!IsVaildSceneResource(files[i]))
							return false;
					}
					return true;
				}
			}
		}

		return false;
	}


	// 获得Assets下面有资源的文件夹
	private static void BuildResAllDirPath(string rootPath, HashSet<string> dirHash)
	{
		if (string.IsNullOrEmpty (rootPath) || (dirHash == null))
			return;

		string dirName = System.IO.Path.GetFileName (rootPath);
		if (dirName.StartsWith (_NotUsed)) {
			// add parentlist
			string parentPath = System.IO.Path.GetDirectoryName(rootPath);
			if (!AssetBundleBuild.DirExistResource(parentPath))
			{
				if (!dirHash.Contains(parentPath))
					dirHash.Add(parentPath);
			}
			return;
		}

		string fullPath = Path.GetFullPath (rootPath);
		if (string.IsNullOrEmpty (fullPath))
			return;
		if (DirExistResource(fullPath))
			dirHash.Add (fullPath);
		// 获得fullPath目录下的所有子文件夹
		string[] dirs = System.IO.Directory.GetDirectories (fullPath);
		if ((dirs != null) && (dirs.Length > 0)) {
			for (int i = 0; i < dirs.Length; ++i)
			{
				BuildResAllDirPath(dirs[i], dirHash);
			}
		}
	}

	// 目录排序
	private static int OnDirSort(string dir1, string dir2)
	{
		// 获得 \\ 次数
		string[] str1 = dir1.Split (_DirSplit, StringSplitOptions.None);
		string[] str2 = dir2.Split (_DirSplit, StringSplitOptions.None);
		if (((str1 == null) && (str2 == null)) || ((str1.Length <= 0) && (str2.Length <= 0)) || (str1.Length == str2.Length))
			return 0;

		if ((str1 == null) || (str1.Length <= 0))
			return 1;

		if ((str2 == null) || (str2.Length <= 0))
			return -1;

		if (str1.Length < str2.Length)
			return 1;

		return -1;
	}

	/*
	private static List<string> GetResAllDirPath(string rootPath)
	{
		HashSet<string> dirSet = new HashSet<string> ();
		BuildResAllDirPath (rootPath, dirSet);
		// 排序
		List<string> ret = new List<string> ();
		var iter = dirSet.GetEnumerator ();
		while (iter.MoveNext()) {
			ret.Add(iter.Current);
		}

		ret.Sort (OnDirSort);
		return ret;
	}*/

	private static List<string> GetResAllDirPath(List<string> rootDir)
		{
			if (rootDir == null || rootDir.Count <= 0)
				return null;
			List<string> ret = null;
			for (int i = 0; i < rootDir.Count; ++i) {
				List<string> list = AssetBundleBuild.GetAllLocalSubDirs (rootDir [i]);
				if (list != null && list.Count > 0)
				{
					if (ret == null)
						ret = new List<string>();
					ret.AddRange (list);
				}

				if (DirExistResource(rootDir[i]))
				{
					if (ret == null)
						ret = new List<string>();
					ret.Add(rootDir[i]);
				}
			}

			if (ret != null)
				ret.Sort(OnDirSort);

			return ret;
		}

	private static List<string> GetResAllDirPath()
	{
#if ASSETBUNDLE_ONLYRESOURCES
        List<string> ret = AssetBundleBuild.GetAllLocalSubDirs(cAssetsResourcesPath);
        if (DirExistResource(cAssetsResourcesPath))
        {
            if (ret == null)
                ret = new List<string>();
            ret.Add(cAssetsResourcesPath);
        }

        if (ret != null)
            ret.Sort(OnDirSort);
        return ret;
#else
		List<string> searchs = AssetBundleBuild.GetAllLocalSubDirs ("Assets/"); 
		//string[] searchs = AssetDatabase.GetAllAssetPaths ();//AssetDatabase.FindAssets ("Assets/Resources");
		if (searchs == null)
			return null;
		string searchStr = "/Resources/";
		HashSet<string> rootPathHash = new HashSet<string> ();
		for (int i = 0; i < searchs.Count; ++i) {
			string path = searchs[i] + "/";//AssetDatabase.GUIDToAssetPath(searchs[i]);
			//if (path.StartsWith("Assets/"))
			{
				int idx = path.LastIndexOf(searchStr);
				if (idx >= 0)
				{
					path = path.Substring(0, idx + searchStr.Length - 1);
					// Assets/Resources/
					if (!rootPathHash.Contains(path))
						rootPathHash.Add(path);
				}

			}
		}


		List<string> ret = new List<string> ();
		var iter = rootPathHash.GetEnumerator ();
		HashSet<string> dirSet = new HashSet<string> ();
		while (iter.MoveNext()) {
			string path = iter.Current;
			BuildResAllDirPath (path, dirSet);
		}

		iter = dirSet.GetEnumerator ();
		while (iter.MoveNext())
			ret.Add (iter.Current);
		ret.Sort (OnDirSort);

		return ret;
#endif
    }

	[MenuItem("Assets/打印依赖关系")]
	static void OnPrintDir()
	{
		List<string> dirList = GetResAllDirPath ();//GetResAllDirPath ("Assets/Resources");
        if (dirList == null)
        {
            Debug.Log("Resources res is None!");
            return;
        }
		// dirList.Add("Assets/Scene");
		mMgr.BuildDirs (dirList);
		mMgr.Print ();
	}

	private static string GetAndCreateDefaultOutputPackagePath(eBuildPlatform platform)
	{
		string ret = Path.GetDirectoryName(Application.dataPath) + "/output";
		
		if (!Directory.Exists (ret)) {
			DirectoryInfo info = Directory.CreateDirectory (ret);
			if (info == null)
				return null;
		}

		switch(platform)
		{
		case eBuildPlatform.eBuildAndroid:
			ret += "/Android";
			break;
		case eBuildPlatform.eBuildIOS:
			ret += "/IOS";
			break;
		case eBuildPlatform.eBuildMac:
			ret += "/Mac";
			break;
		case eBuildPlatform.eBuildWindow:
			ret += "/Windows";
			break;
		default:
			return null;
		}
		
		if (!Directory.Exists (ret)) {
			if (Directory.CreateDirectory (ret) == null)
				return null;
		}
		
		return ret;
	}

	private static string m_PackageVersion = string.Empty;
	// 当前版本号
	public static string GetCurrentPackageVersion(eBuildPlatform platform)
	{
		if (string.IsNullOrEmpty(m_PackageVersion))
		{
			string versionFile = "buildVersion.cfg";
			if (!System.IO.File.Exists(versionFile))
				m_PackageVersion = "1.000";
			else
			{
				FileStream stream = new FileStream(versionFile, FileMode.Open, FileAccess.Read);
				try
				{
					if (stream.Length <= 0)
						m_PackageVersion = "1.000";
					else
					{
						byte[] src = new byte[stream.Length];
						stream.Read(src, 0, src.Length);
						string ver = System.Text.Encoding.ASCII.GetString(src);
						ver = ver.Trim();
						if (string.IsNullOrEmpty(ver))
							m_PackageVersion = "1.000";
						else
							m_PackageVersion = ver;
					}
				}
				finally
				{
					stream.Close();
					stream.Dispose();
				}
			}
		}
		return m_PackageVersion;
	}

	public static string GetPackageExt(eBuildPlatform platform)
	{
		switch (platform) {
		case eBuildPlatform.eBuildAndroid:
			return ".apk";
		case eBuildPlatform.eBuildIOS:
			return ".ipa";
		default:
			return "";
		}
	}

	static private bool IsBuildNewPackageMode(eBuildPlatform platform, string outpath)
	{
		if (string.IsNullOrEmpty (outpath))
			return false;
		switch (platform) {
		case eBuildPlatform.eBuildIOS:
			string[] fileNames = Directory.GetFiles(outpath, "*.xcodeproj");
			return fileNames.Length <= 0;
		case eBuildPlatform.eBuildAndroid:
			return false;
		default:
			return true;
		}
	}

	static public void BuildPlatform(eBuildPlatform platform, int compressType = 0, bool isMd5 = false, string outPath = null)
	{
		// GetResAllDirPath ();
		// 编译平台`
		m_PackageVersion = string.Empty;
		List<string> resList = GetResAllDirPath();
		// resList.Add("Assets/Scene");
		mMgr.BuildDirs(resList);
        mMgr.BuildAssetBundles(platform, compressType, isMd5, outPath);
		/*
		string outpath = GetAndCreateDefaultOutputPackagePath (platform);
		string outFileName = outpath + "/" + GetCurrentPackageVersion (platform);
		if (!mMgr.BuildPackage (platform, outFileName, IsBuildNewPackageMode(platform, outpath)))
			LogMgr.Instance.LogError ("BuildPlatform: BuildPackage error!");*/
	}

	internal static AssetBunbleInfo FindAssetBundle(string fileName)
	{
		return mMgr.FindAssetBundle (fileName);
	}

	[MenuItem("Assets/平台打包/Windows(非压缩)")]
	static public void OnBuildPlatformWindowsNoCompress()
	{
		BuildPlatform (eBuildPlatform.eBuildWindow);
	}

	[MenuItem("Assets/平台打包/Windows MD5(非压缩)")]
	static public void OnBuildPlatformWindowsNoCompressMd5()
	{
		BuildPlatform (eBuildPlatform.eBuildWindow, 0, true);
	}

	[MenuItem("Assets/平台打包/OSX(非压缩)")]
	static public void OnBuildPlatformOSXNoCompress()
	{
		BuildPlatform (eBuildPlatform.eBuildMac);
	}

	[MenuItem("Assets/平台打包/OSX MD5(非压缩)")]
	static public void OnBuildPlatformOSXNoCompressMd5()
	{
		BuildPlatform (eBuildPlatform.eBuildMac, 0, true);
	}

	[MenuItem("Assets/平台打包/Android(非压缩)")]
	static public void OnBuildPlatformAndroidNoCompress()
	{
		BuildPlatform (eBuildPlatform.eBuildAndroid);
	}

	[MenuItem("Assets/平台打包/Android MD5(非压缩)")]
	static public void OnBuildPlatformAndroidNoCompressMd5()
	{
		BuildPlatform (eBuildPlatform.eBuildAndroid, 0, true);
	}

	[MenuItem("Assets/平台打包/IOS(非压缩)")]
	static public void OnBuildPlatformIOSNoCompress()
	{
		BuildPlatform (eBuildPlatform.eBuildIOS);
	}

	[MenuItem("Assets/平台打包/IOS MD5(非压缩)")]
	static public void OnBuildPlatformIOSNoCompressMd5()
	{
		BuildPlatform (eBuildPlatform.eBuildIOS, 0, true);
	}

	[MenuItem("Assets/平台打包/----------")]
	static public void OnBuildPlatformNone()
	{
	}

	[MenuItem("Assets/平台打包/----------", true)]
	static bool CanBuildPlatformNone()
	{
		return false;
	}

	[MenuItem("Assets/平台打包/Windows(压缩)")]
	static public void OnBuildPlatformWindowsCompress()
	{
		BuildPlatform (eBuildPlatform.eBuildWindow, 1);
	}

	[MenuItem("Assets/平台打包/Windows MD5(压缩)")]
	static public void OnBuildPlatformWindowsCompressMd5()
	{
		BuildPlatform (eBuildPlatform.eBuildWindow, 1, true);
	}
	
	[MenuItem("Assets/平台打包/OSX(压缩)")]
	static public void OnBuildPlatformOSXCompress()
	{
		BuildPlatform (eBuildPlatform.eBuildMac, 1);
	}

	[MenuItem("Assets/平台打包/OSX MD5(压缩)")]
	static public void OnBuildPlatformOSXCompressMd5()
	{
		BuildPlatform (eBuildPlatform.eBuildMac, 1, true);
	}
	
	[MenuItem("Assets/平台打包/Android(压缩)")]
	static public void OnBuildPlatformAndroidCompress()
	{
		BuildPlatform (eBuildPlatform.eBuildAndroid, 1);
	}

	[MenuItem("Assets/平台打包/Android MD5(压缩)")]
	static public void OnBuildPlatformAndroidCompressMd5()
	{
		BuildPlatform (eBuildPlatform.eBuildAndroid, 1, true);
	}
	
	[MenuItem("Assets/平台打包/IOS(压缩)")]
	static public void OnBuildPlatformIOSCompress()
	{
		BuildPlatform (eBuildPlatform.eBuildIOS, 1);
		//UnityEditor.EditorUserBuildSettings.SetBuildLocation
	}

	[MenuItem("Assets/平台打包/IOS MD5(压缩)")]
	static public void OnBuildPlatformIOSCompressMd5()
	{
		BuildPlatform (eBuildPlatform.eBuildIOS, 1, true);
	}

#if UNITY_5_3 || UNITY_5_4

	[MenuItem("Assets/平台打包/-----------")]
	static public void OnBuildPlatformNone1() {
	}

	[MenuItem("Assets/平台打包/-----------", true)]
	static bool CanBuildPlatformNone1() {
		return false;
	}

	[MenuItem("Assets/平台打包/Windows(Lz4)")]
	static public void OnBuildPlatformWinLz4() {
		BuildPlatform(eBuildPlatform.eBuildWindow, 2);
	}

	[MenuItem("Assets/平台打包/Windows Md5(Lz4)")]
	static public void OnBuildPlatformWinLz4Md5() {
		BuildPlatform(eBuildPlatform.eBuildWindow, 2, true);
	}


	[MenuItem("Assets/平台打包/OSX(Lz4)")]
	static public void OnBuildPlatformOSXLz4() {
		BuildPlatform(eBuildPlatform.eBuildMac, 2);
	}

	[MenuItem("Assets/平台打包/OSX MD5(Lz4)")]
	static public void OnBuildPlatformOSXLz4Md5() {
		BuildPlatform(eBuildPlatform.eBuildMac, 2, true);
	}

	[MenuItem("Assets/平台打包/Android(Lz4)")]
	static public void OnBuildPlatformAndroidLz4() {
		BuildPlatform(eBuildPlatform.eBuildAndroid, 2);
	}

	[MenuItem("Assets/平台打包/Android MD5(Lz4)")]
	static public void OnBuildPlatformAndroidLz4Md5() {
		BuildPlatform(eBuildPlatform.eBuildAndroid, 2, true);
	}

	[MenuItem("Assets/平台打包/IOS(Lz4)")]
	static public void OnBuildPlatformIOSLz4() {
		BuildPlatform(eBuildPlatform.eBuildIOS, 2);
		//UnityEditor.EditorUserBuildSettings.SetBuildLocation
	}

	[MenuItem("Assets/平台打包/IOS MD5(Lz4)")]
	static public void OnBuildPlatformIOSLz4Md5() {
		BuildPlatform(eBuildPlatform.eBuildIOS, 2, true);
	}

#endif

    /* 真正打包步骤 */
    /*
     * 1.判断目录是否为空，否则生成新工程
     * 2.拷贝非资源的文件
     * 3.打包原工程的资源到AB
     * 4.新工程生成APK
     */

    static void _CopyAllDirs(string dir, string outPath, List<string> resPaths)
    {
		if (resPaths != null)
		{
        	for (int j = 0; j < resPaths.Count; ++j)
        	{
           	 	string resPath = resPaths[j];
            	if (string.IsNullOrEmpty(resPath))
                	continue;
				int idx = dir.IndexOf(resPath, StringComparison.CurrentCultureIgnoreCase);
				if (idx >= 0)
                	return;
        	}
		}

        string subDir = System.IO.Path.GetFileName(dir);
        string newDir = outPath + '/' + subDir;
        if (!System.IO.Directory.Exists(newDir))
        {
            System.IO.DirectoryInfo dstDirInfo = System.IO.Directory.CreateDirectory(newDir);
            if (dstDirInfo == null)
                return;
        }

        string[] fileNames = System.IO.Directory.GetFiles(dir, "*.*", SearchOption.TopDirectoryOnly);
        if (fileNames != null)
        {
            for (int i = 0; i < fileNames.Length; ++i)
            {
                string fileName = fileNames[i];
				fileName = fileName.Replace('\\', '/');
			//	string ext = System.IO.Path.GetExtension(fileName);
			//	if (string.Compare(ext, ".meta", StringComparison.CurrentCultureIgnoreCase) == 0)
			//		continue;
                string dstFileName = newDir + '/' + System.IO.Path.GetFileName(fileName);
                if (File.Exists(dstFileName))
                    File.Delete(dstFileName);
                System.IO.File.Copy(fileName, dstFileName);
            }
        }

        string[] subDirs = System.IO.Directory.GetDirectories(dir);
        if (subDirs != null)
        {
            for (int i = 0; i < subDirs.Length; ++i)
            {
				string sub = subDirs[i];
				sub = sub.Replace('\\', '/');
				_CopyAllDirs(sub, newDir, resPaths);
            }
        }
    }

	// 后面可以考虑加版本好
	//[MenuItem("Assets/測試/APK")]
	static public void Cmd_Apk()
	{
		// Import Package
	//	Cmd_OtherImportAssetRootFiles("..");
	//	Cmd_RemoveAssetRootFiles("..");
		string apkName = "../" + DateTime.Now.ToString("yyyy_MM_dd[HH_mm_ss]") + ".apk";
		apkName = System.IO.Path.GetFullPath(apkName);
		Debug.Log("Build APK: " + apkName);
		mMgr.BuildPackage(eBuildPlatform.eBuildAndroid, apkName, true); 
	}

	static public void Cmd_Win()
	{
		string winApp = "../client.exe";
		winApp = System.IO.Path.GetFullPath(winApp);
		Debug.Log("Build WIN: " + winApp);
		mMgr.BuildPackage(eBuildPlatform.eBuildWindow, winApp, true); 
	}

	static public void Cmd_Win_Debug()
	{
		string winApp = "../client.exe";
		winApp = System.IO.Path.GetFullPath(winApp);
		Debug.Log("Build WIN: " + winApp);
		mMgr.BuildPackage(eBuildPlatform.eBuildWindow, winApp, true, true, true, true); 
	}

	[MenuItem("Assets/发布/Win32(非压缩)")]
	static public void Cmd_BuildWin32_NoCompress()
	{
		Cmd_Build(0, true, eBuildPlatform.eBuildWindow);
	}

	[MenuItem("Assets/发布/Win32_Debug(非压缩)")]
	static public void Cmd_BuidWin32_Debug_NoCompress()
	{
		Cmd_Build(0, true, eBuildPlatform.eBuildWindow, true);
	}

#if UNITY_5_3 || UNITY_5_4
	[MenuItem("Assets/发布/Win32_Debug(Lz4)")]
	static public void Cmd_BuidWin32_Debug_Lz4()
	{
		Cmd_Build(2, true, eBuildPlatform.eBuildWindow, true);
	}
#endif

    [MenuItem("Assets/发布/Win32(压缩)")]
    static public void Cmd_BuildWin32_Compress()
    {
        Cmd_Build(1, true, eBuildPlatform.eBuildWindow);
    }

#if UNITY_5_3 || UNITY_5_4
	[MenuItem("Assets/发布/Win32(Lz4)")]
	static public void Cmd_BuildWin32_Lz4() {
		Cmd_Build(2, true, eBuildPlatform.eBuildWindow);
	}
#endif

	[MenuItem("Assets/编译CSharp")]
	static public void Cmd_BuildCSharpProj()
	{
		/*
		string unityEditorPath = mMgr.GetUnityEditorPath();
		if (string.IsNullOrEmpty(unityEditorPath))
			return;

		string buildExe = unityEditorPath + "/Data/MonoBleedingEdge/lib/mono/unity/xbuild.exe";
		*/
		string buildExe = "xbuild.bat";

		string rootPath = System.IO.Directory.GetCurrentDirectory();
		rootPath = rootPath.Replace('\\', '/');
		string[] csPrjs = new string[1];
		csPrjs[0] = "Assembly-CSharp.csproj";

		for (int i = 0; i < csPrjs.Length; ++i)
		{
			string projFileName = rootPath + '/' + csPrjs[i];
			mMgr.BuildCSharpProject(projFileName, buildExe);		
		}
	}

	 static private void Cmd_Build(int compressType, bool isMd5, eBuildPlatform platform, bool isDebug = false)
	{
		string outPath = "outPath/Proj";
		
		string searchProjPath = System.IO.Path.GetFullPath(outPath);
		if (!System.IO.Directory.Exists(searchProjPath))
		{
			// Create Unity Project
#if UNITY_EDITOR_WIN
			RunCmd("Unity.exe -quit -batchmode -nographics -createProject " + searchProjPath);
#endif
		}

		// 如果后面COPY慢，可以从SVN Download(不会每次都有更新)
		List<string> resPaths = new List<string>();
		resPaths.Add("Assets/Resources");
		resPaths.Add("Assets/StreamingAssets/Android");
        resPaths.Add("Assets/StreamingAssets/IOS");
        resPaths.Add("Assets/StreamingAssets/Windows");
	//	resPaths.Add("Library/metadata");
		//resPaths.Add("Assets/Plugs");
		Cmd_CopyOther(outPath, resPaths);
		
		// Delete outPath StreaingAssets subDirs
		string targetStreamingAssetsPath = outPath + '/' + "Assets/StreamingAssets";
		if (System.IO.Directory.Exists(targetStreamingAssetsPath))
		{
			string[] subDirs = System.IO.Directory.GetDirectories(targetStreamingAssetsPath);
			if (subDirs != null)
			{
				for (int i = 0; i < subDirs.Length; ++i)
				{
					System.IO.Directory.Delete(subDirs[i], true);
				}
			}
		} else
		{
			System.IO.Directory.CreateDirectory(targetStreamingAssetsPath);
		}
		
		// build AssetsBundle to Target
		BuildPlatform(platform, compressType, isMd5, targetStreamingAssetsPath); 

		string logFileName = string.Empty;
		string funcName = string.Empty;
		if (platform == eBuildPlatform.eBuildAndroid)
		{
			// Copy 渠道包
		
			// 新工程生成APK Path=outPath/XXX.apk
			funcName = "AssetBundleBuild.Cmd_Apk";
            logFileName = System.IO.Path.GetDirectoryName(searchProjPath) + '/' + "apkLog.txt";
        }
        else if (platform == eBuildPlatform.eBuildWindow)
        {
            logFileName = System.IO.Path.GetDirectoryName(searchProjPath) + '/' + "winLog.txt";
            if (isDebug)
				funcName = "AssetBundleBuild.Cmd_Win_Debug";
			else
				funcName = "AssetBundleBuild.Cmd_Win";
        }

		if (!string.IsNullOrEmpty(funcName))
		{
#if UNITY_EDITOR_WIN
			string cmdApk = string.Format("Unity.exe -quit -batchmode -nographics -executeMethod {0} -logFile {1} -projectPath {2}", 
			                              funcName, logFileName, searchProjPath);
			RunCmd(cmdApk);
#endif
		}
	}

    [MenuItem("Assets/发布/APK_整包(非压缩AB)")]
    static public void Cmd_BuildAPK_NoCompress()
    {
		Cmd_Build(0, true, eBuildPlatform.eBuildAndroid);
    }

    [MenuItem("Assets/发布/APK_整包(压缩AB)")]
    static public void Cmd_BuildAPK_Compress()
    {
        Cmd_Build(1, true, eBuildPlatform.eBuildAndroid);
    }

#if UNITY_5_3 || UNITY_5_4
	[MenuItem("Assets/发布/APK_整包(Lz4)")]
	static public void Cmd_BuildAPK_Lz4() {
		Cmd_Build(2, true, eBuildPlatform.eBuildAndroid);
	}

	[MenuItem("Assets/发布/APK_Debug(Lz4)")]
	static public void Cmd_BuildAPK_Debug_Lz4()
	{
		Cmd_Build(2, true, eBuildPlatform.eBuildAndroid, true);
	}
#endif

	[MenuItem("Assets/发布/APK_Debug(非压缩)")]
	static public void Cmd_BuildAPK_DEBUG_UNCOMPRESS()
	{
		Cmd_Build(0, true, eBuildPlatform.eBuildAndroid, true);
	}

	public static void RunCmd(string command)
	{

		if (string.IsNullOrEmpty(command))
			return;
#if UNITY_EDITOR_WIN
		command = " /c " + command;
		processCommand("cmd.exe", command);
#elif UNITY_EDITOR_MAC
            command = " -al " + command;
            processCommand("ls", command);
#endif
	}

	private static void processCommand(string command, string argument){
		System.Diagnostics.ProcessStartInfo start = new System.Diagnostics.ProcessStartInfo(command);
		start.Arguments = argument;
		start.CreateNoWindow = false;
		start.ErrorDialog = true;
	    start.UseShellExecute = true;
	//	start.UseShellExecute = false;
		
		if(start.UseShellExecute){
			start.RedirectStandardOutput = false;
			start.RedirectStandardError = false;
			start.RedirectStandardInput = false;
		} else{
			start.RedirectStandardOutput = true;
			start.RedirectStandardError = true;
			start.RedirectStandardInput = true;
		//	start.StandardOutputEncoding = System.Text.UTF8Encoding.UTF8;
		//	start.StandardErrorEncoding = System.Text.UTF8Encoding.UTF8;
			start.StandardOutputEncoding = System.Text.Encoding.Default;
			start.StandardErrorEncoding = System.Text.Encoding.Default;
		}
		
		System.Diagnostics.Process p = System.Diagnostics.Process.Start(start);
		
		if(!start.UseShellExecute){
			Exec_Print(p.StandardOutput, false);
			Exec_Print(p.StandardError, true);
		}

		p.WaitForExit();
		p.Close();
	}

	private static void Exec_Print(StreamReader reader, bool isError)
	{
		if (reader == null)
			return;

		string str = reader.ReadToEnd();

		if (!string.IsNullOrEmpty(str))
		{
			if (isError)
				Debug.LogError(str);
			else
				Debug.Log(str);
		}

		reader.Close();
	}

	static private void _CopyAllFiles(string srcPath, string dstPath, List<string> resPaths)
	{
		if (string.IsNullOrEmpty(srcPath) || string.IsNullOrEmpty(dstPath))
			return;
		string[] dirs = System.IO.Directory.GetDirectories(srcPath);
		if (dirs != null)
		{
			for (int i = 0; i < dirs.Length; ++i)
			{
				string dir = dirs[i];
				dir = dir.Replace('\\', '/');
				_CopyAllDirs(dir, dstPath, resPaths);
			}
		}

		string[] srcRootFiles = System.IO.Directory.GetFiles(srcPath, "*.*", SearchOption.TopDirectoryOnly);
		if (srcRootFiles != null)
		{
			for (int i = 0; i < srcRootFiles.Length; ++i)
			{
				string srcFilePath = srcRootFiles[i];
				string srcFileName = System.IO.Path.GetFileName(srcFilePath);
				string dstFilePath = dstPath + '/' + srcFileName;
				System.IO.File.Copy(srcFilePath, dstFilePath, true);
			}
		}
	} 

	static private void Cmd_Svn(string outPath, List<string> resPaths)
	{
		if (string.IsNullOrEmpty (outPath) || resPaths == null || resPaths.Count <= 1)
				return;
			string url = resPaths [0].Trim ();
			if (string.IsNullOrEmpty (url))
				return;
			
			// SVN更新
			for (int i = 1; i < resPaths.Count; ++i) {
				string path = string.Format ("{0}/{1}", outPath, resPaths [i]);
				path = Path.GetFullPath (path);
				if (Directory.Exists (path)) {
					// svn update
					#if UNITY_EDITOR_WIN
					string cmd = string.Format("TortoiseProc.exe /command:update /path:\"{0}\" /closeonend:3", path);
					RunCmd(cmd);
					#endif
				} else {
					// svn checkout
					#if UNITY_EDITOR_WIN
					string cmd = string.Format("TortoiseProc.exe /command:checkout /path:\"{0}\" /url:\"{1}/{2}\"", path, url, resPaths[i]);
					RunCmd(cmd);
					#endif
				}
			}
	}

	static private void Cmd_CopyList(string outPath, List<string> copyList)
		{
			if (string.IsNullOrEmpty(outPath) || copyList == null || copyList.Count <= 0)
				return;

			string dstAssets = outPath + '/' + "Assets";
			if (!System.IO.Directory.Exists(dstAssets)) {
				if (System.IO.Directory.CreateDirectory(dstAssets) == null)
					return;
			}

			for (int i = 0; i < copyList.Count; ++i) {
				string dir = copyList [i];
				string dstDir = Path.GetFullPath(outPath + '/' + dir);
				if (Directory.Exists (dstDir)) {
					var subDirs = System.IO.Directory.GetDirectories (dstDir);
					if (subDirs != null) {
						for (int j = 0; j < subDirs.Length; ++j) {
							System.IO.Directory.Delete (subDirs [j], true);
						}
					}

					var subFiles = System.IO.Directory.GetFiles (dstDir);
					if (subFiles != null) {
						for (int j = 0; j < subFiles.Length; ++j) {
							System.IO.File.Delete (subFiles [j]);
						}
					}
				}

				dir = dir.Replace('\\', '/');
				_CopyAllDirs(dir, dstAssets, null);
			}

			dstAssets = outPath + '/' + "ProjectSettings";
			_CopyAllFiles("ProjectSettings", dstAssets, null);
		}

    // 拷贝非资源文件夹
    // resPaths: 资源目录列表
    // outPath: 目录
    static private void Cmd_CopyOther(string outPath, List<string> resPaths)
    {
        if (string.IsNullOrEmpty(outPath))
            return;
        var dirs = System.IO.Directory.GetDirectories("Assets");
        if (dirs == null || dirs.Length <= 0)
            return;

        string dstAssets = outPath + '/' + "Assets";
        if (!System.IO.Directory.Exists(dstAssets))
        {
            if (System.IO.Directory.CreateDirectory(dstAssets) == null)
                return;
        }

		var delDirs = System.IO.Directory.GetDirectories(dstAssets);
		if (delDirs != null)
		{
			for (int i = 0; i < delDirs.Length; ++i)
			{
				System.IO.Directory.Delete(delDirs[i], true);
			}
		}

		var delFiles = System.IO.Directory.GetFiles(dstAssets);
		if (delFiles != null)
		{
			for (int i = 0; i < delFiles.Length; ++i)
			{
				System.IO.File.Delete(delFiles[i]);
			}
		}

		var srcRootFiles = System.IO.Directory.GetFiles("Assets", "*.*", SearchOption.TopDirectoryOnly);
		if (srcRootFiles != null)
		{
			for (int i = 0; i < srcRootFiles.Length; ++i)
			{
				string srcFilePath = srcRootFiles[i];
				string srcFileName = System.IO.Path.GetFileName(srcFilePath);
				string dstFilePath = dstAssets + '/' + srcFileName;
				System.IO.File.Copy(srcFilePath, dstFilePath, true);
			}
		}

		// Copy DstAssets

        for (int i = 0; i < dirs.Length; ++i)
        {
            string dir = dirs[i];
			dir = dir.Replace('\\', '/');
			_CopyAllDirs(dir, dstAssets, resPaths);
        }

	//	dstAssets = outPath + '/' + "Library";
	//	_CopyAllFiles("Library", dstAssets, null);

		dstAssets = outPath + '/' + "ProjectSettings";
		_CopyAllFiles("ProjectSettings", dstAssets, null);
    }

	private static void Cmd_RemoveAssetRootFiles(string outPath)
	{
		string tempPacketPath = outPath + "/Temp.unitypackage";
		if (System.IO.File.Exists(tempPacketPath))
			System.IO.File.Delete(tempPacketPath);
	}

	private static void Cmd_OtherImportAssetRootFiles(string outPath)
	{
		// Import unitypackage to New Project
		string tempPacketPath = outPath + "/Temp.unitypackage";
		if (!System.IO.File.Exists(tempPacketPath))
			return;
		AssetDatabase.ImportPackage(tempPacketPath, false);
		AssetDatabase.Refresh();
	}

	private static void Cmd_ExportAssetRootFiles(string outPath)
	{
		var topFiles = System.IO.Directory.GetFiles("Assets", "*.*", SearchOption.TopDirectoryOnly);
		List<string> packetFileList = new List<string>();
		if (topFiles != null)
		{
			string dstTopFilePath = outPath + "/Assets";
			for (int i = 0; i < topFiles.Length; ++i)
			{
				string filePath = topFiles[i];
			//	string fileName = System.IO.Path.GetFileName(filePath);
			//	string ext = System.IO.Path.GetExtension(fileName);
			//	if (string.Compare(ext, ".meta", StringComparison.CurrentCultureIgnoreCase) == 0)
			//		continue;

				string packetSubFileName = AssetBundleMgr.GetAssetRelativePath(filePath);
				packetFileList.Add(packetSubFileName);
			//	string dstFilePath = dstTopFilePath + '/' + fileName;
			//	System.IO.File.Copy(filePath, dstFilePath, true);
			}
		}

		if (packetFileList.Count > 0)
		{
			string tempPacketFile = "outPath/Temp.unitypackage";
			if (System.IO.File.Exists(tempPacketFile))
				System.IO.File.Delete(tempPacketFile);
			string[] allFiles = AssetDatabase.GetDependencies(packetFileList.ToArray());
			AssetDatabase.ExportPackage(allFiles, tempPacketFile);
		}
	}

	public static void AddShowTagProcess(string tagName)
	{
		if (mMgr.MaxTagFileCount <= 0)
			return;
		mMgr.CurTagIdx += 1;
		float maxCnt = mMgr.MaxTagFileCount;
		float curIdx = mMgr.CurTagIdx;
		float process = curIdx/maxCnt;
		EditorUtility.DisplayProgressBar("设置Tag中...", tagName, process);
	}

	#if UNITY_5

	[MenuItem("Assets/清理所有AssetBundle的Tag")]
	public static void ClearAllAssetNames()
	{
		string[] assetBundleNames = AssetDatabase.GetAllAssetBundleNames();
		if (assetBundleNames == null || assetBundleNames.Length <= 0)
			return;
		for (int i = 0; i <assetBundleNames.Length; ++i)
		{
			float process = ((float)i)/((float)assetBundleNames.Length);
			EditorUtility.DisplayProgressBar("清理Tag中...", assetBundleNames[i], process);
			AssetDatabase.RemoveAssetBundleName(assetBundleNames[i], true);
			EditorUtility.UnloadUnusedAssetsImmediate();
		}
		EditorUtility.ClearProgressBar();
	}

	#endif

	private static AssetBundleMgr mMgr = new AssetBundleMgr();
}
