using UnityEngine;
using System.Collections;

namespace HexasphereGrid {

				partial class Hexasphere : MonoBehaviour {

								const float EARTH_WATER_MASK_OCEAN_LEVEL_MAX_ALPHA = 16f/255f;
												
								#region public API

								/// <summary>
								/// Applies the height map provided by heightMap texture to sphere grid. It uses red channel as height (0..1), multiplied by the maxExtrusionAmount value.
								/// </summary>
								/// <param name="heightMap">Height map.</param>
								/// <param name="maxExtrusionAmount">Max extrusion amount.</param>
								/// <param name="rampColors">Gradient of colors that will be mapped to the height map.</param>
								public void ApplyHeightMap (Texture2D heightMap, Texture2D waterMask, Texture2D rampColors = null) {

												heights = heightMap.GetPixels ();
												if (waterMask!=null) {
																waters = waterMask.GetPixels ();
												} else {
																waters = null;
												}
												heightMapWidth = heightMap.width;
												heightMapHeight = heightMap.height;								
												LoadRampColors(rampColors);
												UpdateHeightMap ();
								}

								/// <summary>
								/// Applies the height map provided by heightMap texture to sphere grid. It uses red channel as height (0..1), multiplied by the maxExtrusionAmount value.
								/// </summary>
								/// <param name="heightMap">Height map.</param>
								/// <param name="maxExtrusionAmount">Max extrusion amount.</param>
								/// <param name="rampColors">Gradient of colors that will be mapped to the height map.</param>
								public void ApplyHeightMap (Texture2D heightMap, float seaLevel = 0.1f, Texture2D rampColors = null) {
												
												heights = heightMap.GetPixels ();
												waters = null;
												heightMapWidth = heightMap.width;
												heightMapHeight = heightMap.height;								
												LoadRampColors(rampColors);
												UpdateHeightMap (seaLevel);
								}

								void LoadRampColors(Texture2D rampColors) {
												if (rampColors == null) {
																if (defaultRampTexture == null) {
																//change it here
																				defaultRampTexture = Resources.Load<Texture2D> ("Textures/HexasphereDefaultRampTex");
																}
																gradientColors = defaultRampTexture.GetPixels ();
																rampWidth = defaultRampTexture.width;
												} else {
																gradientColors = rampColors.GetPixels ();
																rampWidth = rampColors.width;
												}
								}

								/// <summary>
								/// Reuses previous heightmap texture which results faster update if you need to change rampColors or seaLevel dynamically.
								/// </summary>
								/// <param name="maxExtrusionAmount">Max extrusion amount.</param>
								/// <param name="rampColors">Gradient of colors that will be mapped to the height map.</param>
								void UpdateHeightMap (float seaLevel = 0.1f, Texture2D rampColors = null) {
												if (tiles == null || heights==null)
																return;
												extruded = true;
												if (rampColors) {
																gradientColors = rampColors.GetPixels ();
																rampWidth = rampColors.width;
												}
												for (int k = 0; k < tiles.Length; k++) {
																Vector3 p = tiles [k].center;
																float latDec = Mathf.Asin (p.y * 2.0f);
																float lonDec = -Mathf.Atan2 (p.x, p.z);
																if (_invertedMode) lonDec *= -1f;
																int px = (int)((lonDec + Mathf.PI) * heightMapWidth / (2f * Mathf.PI));
																int py = (int)(latDec * heightMapHeight / Mathf.PI + heightMapHeight / 2.0f);
																if (py >= heightMapHeight)
																				py = heightMapHeight - 1;
																float h = heights [py * heightMapWidth + px].r;
																// Water mask supplied?
																if (waters!=null) {
																				bool isWater = waters [py * heightMapWidth + px].a < EARTH_WATER_MASK_OCEAN_LEVEL_MAX_ALPHA;
																				if (isWater) {
																								h = 0;
																								tiles [k].canCross = false;
																				}
																} else {
																				if (h <= seaLevel) {
																								h = 0;
																								tiles [k].canCross = false;
																				}
																}
																SetTileExtrudeAmount (k, h);
																int gc = (int)((rampWidth - 1) * h);
																tiles [k].heightMapValue = h;
																SetTileColor (k, gradientColors [gc]);
												}
								}

								/// <summary>
								/// Takes colors from a texture and maps them to the hexasphere
								/// </summary>
								/// <param name="textureWithColors">Texture with colors.</param>
								public void ApplyColors(Texture2D textureWithColors) {
												// Load texture colors and dimensions
												Color32[] colors = textureWithColors.GetPixels32();
												int textureWidth = textureWithColors.width;
												int textureHeight = textureWithColors.height;

												// For each tile, determine its color
												for (int k=0;k<tiles.Length;k++) {
																Vector3 p = tiles[k].center;

																// Convert center to texture coordinates
																float latDec = Mathf.Asin (p.y * 2.0f);
																float lonDec = -Mathf.Atan2 (p.x, p.z);
																int px = (int)(lonDec * textureWidth / (2f * Mathf.PI));
																int py = (int)(latDec * textureHeight / Mathf.PI + textureHeight / 2.0f);
																if (py >= textureHeight)
																				py = textureHeight - 1;
																Color32 tileColor = colors [py * textureWidth + px];

																SetTileColor(k, tileColor);
												}
								}

								#endregion

				}

}