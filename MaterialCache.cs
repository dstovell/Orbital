using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Orbital
{
	public static class MaterialCache
	{
		public static Dictionary<Color, Material> ColoredMaterials = new Dictionary<Color, Material>();

		public static Material GetByColor(Color color, Material fallback)
		{
			if (ColoredMaterials.ContainsKey(color))
			{
				return ColoredMaterials[color];
			}

			Material cachedMaterial = new Material(fallback);
			cachedMaterial.color = color;

			ColoredMaterials[color] = cachedMaterial;

			return cachedMaterial;
		}
	}
}
