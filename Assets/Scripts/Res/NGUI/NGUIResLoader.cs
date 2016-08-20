﻿#define _USE_NGUI

#if _USE_NGUI

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Utils;

public class NGUIResLoader: BaseResLoader  {

	public bool LoadMainTexture(UITexture uiTexture, string fileName)
	{
		if (uiTexture == null || string.IsNullOrEmpty(fileName))
			return false;

		Texture tex = ResourceMgr.Instance.LoadTexture(fileName, ResourceCacheType.rctRefAdd);
		SetResource(uiTexture, tex, typeof(Texture), _cMainTex);
		uiTexture.mainTexture = tex;

		return tex != null;
	}

	public void ClearMainTexture(UITexture uiTexture)
	{
		if (uiTexture == null)
			return;
		ClearTexture(uiTexture, _cMainTex);
		uiTexture.mainTexture = null;
	}

	public void ClearTexture(UITexture uiTexture, string matName)
	{
		if (uiTexture == null)
			return;
		
		ClearResource<Texture>(uiTexture, matName);
		Material mat = uiTexture.material;
		if (mat != null)
			mat.SetTexture(matName, null);
	}

	public bool LoadShader(UITexture uiTexture, string fileName)
	{
		if (uiTexture == null || string.IsNullOrEmpty(fileName))
			return false;

		Shader shader = ResourceMgr.Instance.LoadShader(fileName, ResourceCacheType.rctRefAdd);
		SetResource(uiTexture, shader, typeof(Shader));
		uiTexture.shader = shader;

		return shader != null;
	}

	public void ClearShader(UITexture uiTexture)
	{
		if (uiTexture == null)
			return;
		ClearResource<Shader>(uiTexture);
		uiTexture.shader = null;
	}

	public bool LoadTexture(UITexture uiTexture, string fileName, string matName)
	{
		if (uiTexture == null || string.IsNullOrEmpty(fileName) || string.IsNullOrEmpty(matName))
			return false;

		Material mat = uiTexture.material;
		if (mat == null)
			return false;

		Texture tex = ResourceMgr.Instance.LoadTexture(fileName, ResourceCacheType.rctRefAdd);
		SetResource(uiTexture, tex, typeof(Texture), matName);
		mat.SetTexture(matName, tex);

		return tex != null;
	}

	public void ClearMaterial(UITexture uiTexture)
	{
		if (uiTexture == null)
			return;
		ClearResource<Material>(uiTexture);
		uiTexture.material = null;
	}

	public bool LoadMaterial(UITexture uiTexture, string fileName)
	{
		if (uiTexture == null || string.IsNullOrEmpty(fileName))
			return false;

		Material mat = ResourceMgr.Instance.LoadMaterial(fileName, ResourceCacheType.rctRefAdd);
		SetResource(uiTexture, mat, typeof(Material));
		if (mat != null)
			//uiTexture.material = mat;
			uiTexture.material = GameObject.Instantiate(mat);
		else
			uiTexture.material = null;

		return mat != null;
	}

	public bool LoadMaterial(UISprite uiSprite, string fileName)
	{
		if (uiSprite == null || string.IsNullOrEmpty(fileName))
			return false;
		Material mat = ResourceMgr.Instance.LoadMaterial(fileName, ResourceCacheType.rctRefAdd);
		SetResource(uiSprite, mat, typeof(Material));

		if (mat != null)
			//uiSprite.material = mat;
			uiSprite.material = GameObject.Instantiate(mat);
		else
			uiSprite.material = null;

		return mat != null;
	}

	public void ClearMaterial(UISprite uiSprite)
	{
		if (uiSprite == null)
			return;
		ClearResource<Material>(uiSprite);
		uiSprite.material = null;
	}

	public bool LoadTexture(UISprite uiSprite, string fileName, string matName)
	{
		if (uiSprite == null || string.IsNullOrEmpty(fileName) || string.IsNullOrEmpty(matName))
			return false;

		Material mat = uiSprite.material;
		if (mat == null)
			return false;

		Texture tex = ResourceMgr.Instance.LoadTexture(fileName, ResourceCacheType.rctRefAdd);
		SetResource(uiSprite, tex, typeof(Texture), matName);
		mat.SetTexture(matName, tex);

		return tex != null;
	}

	public void ClearMainTexture(UISprite uiSprite)
	{
		if (uiSprite == null)
			return;
		ClearTexture(uiSprite, _cMainTex);
		uiSprite.mainTexture = null;
	}

	public void ClearTexture(UISprite uiSprite, string matName)
	{
		if (uiSprite == null)
			return;
		
		ClearResource<Texture>(uiSprite, matName);

		Material mat = uiSprite.material;
		if (mat != null)
			mat.SetTexture(matName, null);
	}

	public bool LoadMainTexture(UISprite uiSprite, string fileName)
	{
		if (uiSprite == null || string.IsNullOrEmpty(fileName))
			return false;
		
		Texture tex = ResourceMgr.Instance.LoadTexture(fileName, ResourceCacheType.rctRefAdd);
		SetResource(uiSprite, tex, typeof(Texture), _cMainTex);
		uiSprite.mainTexture = tex;

		return tex != null;
	}

	public bool LoadMainTexture(UI2DSprite uiSprite, string fileName)
	{
		if (uiSprite == null || string.IsNullOrEmpty(fileName))
			return false;

		Texture tex = ResourceMgr.Instance.LoadTexture(fileName, ResourceCacheType.rctRefAdd);
		SetResource(uiSprite, tex, typeof(Texture), _cMainTex);
		uiSprite.mainTexture = tex;

		return tex != null;
	}

	public bool LoadShader(UI2DSprite uiSprite, string fileName)
	{
		if (uiSprite == null || string.IsNullOrEmpty(fileName))
			return false;

		Shader shader = ResourceMgr.Instance.LoadShader(fileName, ResourceCacheType.rctRefAdd);
		SetResource(uiSprite, shader, typeof(Shader));
		uiSprite.shader = shader;

		return shader != null;
	}

	public void ClearShader(UI2DSprite uiSprite)
	{
		if (uiSprite == null)
			return;
		ClearResource<Shader>(uiSprite);
		uiSprite.shader = null;
	}

	public void ClearTexture(UI2DSprite uiSprite, string matName)
	{
		if (uiSprite == null)
			return;
		
		ClearResource<Texture>(uiSprite, matName);

		Material mat = uiSprite.material;
		if (mat != null)
			mat.SetTexture(matName, null);
	}

	public void ClearMainTexture(UI2DSprite uiSprite)
	{
		if (uiSprite == null)
			return;
		
		ClearTexture(uiSprite, _cMainTex);
		uiSprite.mainTexture = null;
	}

	public bool LoadTexture(UI2DSprite uiSprite, string fileName, string matName)
	{
		if (uiSprite == null || string.IsNullOrEmpty(fileName) || string.IsNullOrEmpty(matName))
			return false;

		Material mat = uiSprite.material;
		if (mat == null)
			return false;

		Texture tex = ResourceMgr.Instance.LoadTexture(fileName, ResourceCacheType.rctRefAdd);
		SetResource(uiSprite, tex, typeof(Texture), matName);
		mat.SetTexture(matName, tex);

		return tex != null;
	}

	public bool LoadSprite(UI2DSprite uiSprite, string fileName, string spriteName)
	{
		if (uiSprite == null || string.IsNullOrEmpty(fileName) || string.IsNullOrEmpty(spriteName))
			return false;
		Sprite[] sps = ResourceMgr.Instance.LoadSprites(fileName, ResourceCacheType.rctRefAdd);
		bool isFound = false;
		for (int i = 0; i < sps.Length; ++i)
		{
			Sprite sp = sps[i];
			if (sp == null)
				continue;
			if (!isFound && string.Compare(sp.name, spriteName) == 0)
			{
				uiSprite.sprite2D = sp;
				isFound = true;
				SetResources(uiSprite, sps, typeof(Sprite[]));
				break;
			}
		}

		if (!isFound)
		{
			for (int i = 0; i < sps.Length; ++i)
			{
				Sprite sp = sps[i];
				DestroySprite(sp);
			}
			SetResources(uiSprite, null, typeof(Sprite[]));
		}

		return isFound;
	}

	public void ClearMaterial(UI2DSprite uiSprite)
	{
		if (uiSprite == null)
			return;
		uiSprite.material = null;
		ClearResource<Material>(uiSprite);
	}

	public bool LoadMaterial(UI2DSprite uiSprite, string fileName)
	{
		if (uiSprite == null || string.IsNullOrEmpty(fileName))
			return false;
		Material mat = ResourceMgr.Instance.LoadMaterial(fileName, ResourceCacheType.rctRefAdd);
		SetResource(uiSprite, mat, typeof(Material));

		if (mat != null)
			//uiSprite.material = mat;
			uiSprite.material = GameObject.Instantiate(mat);
		else
			uiSprite.material = null;

		return mat != null;
	}
}

#endif
