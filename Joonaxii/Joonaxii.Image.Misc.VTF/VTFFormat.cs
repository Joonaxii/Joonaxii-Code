﻿namespace Joonaxii.Image.Misc.VTF
{   
	//From https://developer.valvesoftware.com/wiki/Valve_Texture_Format
	public enum VTFFormat
    {
		NONE = -1,
		RGBA8888 = 0,
		ABGR8888,
		RGB888,
		BGR888,
		RGB565,
		I8,
		IA88,
		P8,
		A8,
		RGB888_BLUESCREEN,
		BGR888_BLUESCREEN,
		ARGB8888,
		BGRA8888,
		DXT1,
		DXT3,
		DXT5,
		BGRX8888,
		BGR565,
		BGRX5551,
		BGRA4444,
		DXT1_ONEBITALPHA,
		BGRA5551,
		UV88,
		UVWQ8888,
		RGBA16161616F,
		RGBA16161616,
		UVLX8888
	}
}