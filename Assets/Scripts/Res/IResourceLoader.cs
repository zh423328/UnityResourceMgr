/*----------------------------------------------------------------
// 模块名：资源加载抽象类
// 创建者：zengyi
// 修改者列表：
// 创建日期：2015年6月1日
// 模块描述：
//----------------------------------------------------------------*/

using System;
using UnityEngine;

public enum ResourceCacheType
{
    //使用后立即删除，即在读取AB数据后，下一帧AssetBundle.Unload(true), 常用于读取配置，文本用（ResourceMgr.Instance.LoadText, ResourceMgr.Instance.LoadBytes
    rctNone = 0, // xiao xin shi yong
     //读取的对象会放入AssetCacheMgr，进行缓存，但并不会对资源引用计数+1，一般不建议外部使用这个参数，内部库中在LoadPrefab后，立即GameObject.instance时使用
    rctTemp,
   // 读取的对象会放入AssetCacheMgr进行缓存，并且会对资源进行引用计数+1，外部调用常用类型，切记使用这个参数加载的对象，要使用ResourceMgr.Instance.DestroyObject删除对象（把这个类型读取出来的对象，看做是C++的指针，需要手动调用）
    rctRefAdd
}

public abstract class IResourceLoader
{
	#region public function
	public abstract bool OnSceneLoad(string sceneName);
	public abstract bool OnSceneLoadAsync(string sceneName, Action onEnd);
	public abstract bool OnSceneClose(string sceneName);
	public abstract Font LoadFont (string fileName, ResourceCacheType cacheType);
	public abstract bool LoadFontAsync (string fileName, ResourceCacheType cacheType, Action<float, bool, Font> onProcess);
	public abstract GameObject LoadPrefab(string fileName, ResourceCacheType cacheType);
	public abstract bool LoadPrefabAsync(string fileName, ResourceCacheType cacheType, Action<float, bool, GameObject> onProcess);
	public abstract Material LoadMaterial(string fileName, ResourceCacheType cacheType);
	public abstract bool LoadMaterialAsync(string fileName, ResourceCacheType cacheType, Action<float, bool, Material> onProcess);
	public abstract Texture LoadTexture(string fileName, ResourceCacheType cacheType);
	public abstract bool LoadTextureAsync(string fileName, ResourceCacheType cacheType, Action<float, bool, Texture> onProcess);
	public abstract AudioClip LoadAudioClip(string fileName, ResourceCacheType cacheType);
	public abstract bool LoadAudioClipAsync(string fileName, ResourceCacheType cacheType, Action<float, bool, AudioClip> onProcess);
	public abstract string LoadText(string fileName, ResourceCacheType cacheType);
	public abstract byte[] LoadBytes(string fileName, ResourceCacheType cacheType);
	public abstract bool LoadTextAsync(string fileName, ResourceCacheType cacheType, Action<float, bool, TextAsset> onProcess);
	public abstract RuntimeAnimatorController LoadAniController(string fileName, ResourceCacheType cacheType);
	public abstract bool LoadAniControllerAsync(string fileName, ResourceCacheType cacheType, Action<float, bool, RuntimeAnimatorController> onProcess);
	public abstract AnimationClip LoadAnimationClip(string fileName, ResourceCacheType cacheType);
	public abstract bool LoadAnimationClipAsync(string fileName, ResourceCacheType cacheType, Action<float, bool, AnimationClip> onProcess);
	public abstract Shader LoadShader(string fileName, ResourceCacheType cacheType);
	public abstract bool LoadShaderAsync(string fileName, ResourceCacheType cacheType, Action<float, bool, Shader> onProcess);
	public abstract Sprite[] LoadSprites(string fileName, ResourceCacheType cacheType);
	public abstract bool LoadSpritesAsync(string fileName, ResourceCacheType cacheType, Action<float, bool, UnityEngine.Object[]> onProcess);
	public abstract ScriptableObject LoadScriptableObject (string fileName, ResourceCacheType cacheType);
	public abstract bool LoadScriptableObjectAsync (string fileName, ResourceCacheType cacheType, Action<float, bool, UnityEngine.ScriptableObject> onProcess);
#if UNITY_5
	public abstract ShaderVariantCollection LoadShaderVarCollection(string fileName, ResourceCacheType cacheType);
	public abstract bool LoadShaderVarCollectionAsync(string fileName, ResourceCacheType ResourceCacheType, Action<float, bool, ShaderVariantCollection> onProcess);
#endif
	public abstract AssetCache CreateCache(UnityEngine.Object orgObj, string fileName);
	#endregion public function 
}
