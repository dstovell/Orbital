/// <summary>
/// Hexasphere Grid System
/// Created by Ramiro Oliva (Kronnect)
/// </summary>

using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;


namespace HexasphereGrid {

				public partial class Hexasphere: MonoBehaviour {

								#region Public API

								/// <summary>
								/// Array of generated tiles.
								/// </summary>
								public Tile[] tiles;

								/// <summary>
								/// Returns the index of the tile in the tiles list
								/// </summary>
								public int GetTileIndex (Tile tile) {
												if (tiles == null)
																return -1;
												return tile.index;
								}


								/// <summary>
								/// Sets the tile material.
								/// </summary>
								/// <returns><c>true</c>, if tile material was set, <c>false</c> otherwise.</returns>
								/// <param name="tileIndex">Tile index.</param>
								/// <param name="mat">Material to be used.</param>
								/// <param name="temporary">If set to <c>true</c> the material is not saved anywhere and will be restored to default tile material when tile gets unselected.</param>
								public bool SetTileMaterial (int tileIndex, Material mat, bool temporary = false) {
												if (tileIndex < 0 || tileIndex >= tiles.Length)
																return false;
												Tile tile = tiles [tileIndex];
												if (temporary) {
																if (tile.renderer == null) {
																				GenerateTileMesh (tileIndex, mat);
																} else {
																				tile.renderer.sharedMaterial = mat;
																				tile.renderer.enabled = true;
																}
												} else {
																Color32 matColor = mat.color;
																pendingColorsUpdate = true;
																if (mat.mainTexture) {
																				pendingTextureArrayUpdate = true;
																} else {
																				List<Color32> colorChunk = colorShaded [tile.uvShadedChunkIndex];
																				for (int k = 0; k < tile.uvShadedChunkLength; k++) {
																								colorChunk [tile.uvShadedChunkStart + k] = matColor;
																				}
																				colorShadedDirty [tile.uvShadedChunkIndex] = true;
																}
																// Only if wire color is set to use the tile color
																List<Color32> colorWireChunk = colorWire [tile.uvWireChunkIndex];
																if (!_wireframeColorFromTile) {
																				matColor = Misc.Color32White;
																}
																				
																for (int k = 0; k < tile.uvWireChunkLength; k++) {
																				colorWireChunk [tile.uvWireChunkStart + k] = matColor;
																}
																colorWireDirty [tile.uvWireChunkIndex] = true;
												}

												if (mat != highlightMaterial) {
																if (temporary) {
																				tile.tempMat = mat;
																} else {
																				tile.customMat = mat;
																}
												}

												if (highlightMaterial != null && tile == lastHighlightedTile) {
																if (tile.renderer != null)
																				tile.renderer.sharedMaterial = highlightMaterial;
																if (tile.tempMat != null) {
																				highlightMaterial.SetColor ("_Color2", tile.tempMat.color);
																				highlightMaterial.mainTexture = tile.tempMat.mainTexture;
																} else if (tile.customMat != null) {
																				highlightMaterial.SetColor ("_Color2", tile.customMat.color);
																				highlightMaterial.mainTexture = tile.customMat.mainTexture;
																}
												}

												return true;
								}

								/// <summary>
								/// Sets the color of the tile.
								/// </summary>
								/// <returns><c>true</c>, if tile color was set, <c>false</c> otherwise.</returns>
								/// <param name="tileIndex">Tile index.</param>
								/// <param name="color">Color.</param>
								/// <param name="temporary">If set to <c>true</c> the tile is colored temporarily and returns to default color when it gets unselected.</param>
								public bool SetTileColor (int tileIndex, Color color, bool temporary = false) {
												Material mat = GetCachedMaterial (color);
												return SetTileMaterial (tileIndex, mat, temporary);
								}

		
								/// <summary>
								/// Sets the color of a list of tiles.
								/// </summary>
								/// <returns><c>true</c>, if tile color was set, <c>false</c> otherwise.</returns>
								/// <param name="tileIndex">Tile index.</param>
								/// <param name="color">Color.</param>
								/// <param name="temporary">If set to <c>true</c> the tile is colored temporarily and returns to default color when it gets unselected.</param>
								public void SetTileColor (List<int> tileIndices, Color color, bool temporary = false) {
												if (tileIndices == null)
																return;
												Material mat = GetCachedMaterial (color);
												int tc = tileIndices.Count;
												for (int k = 0; k < tc; k++) {
																int tileIndex = tileIndices [k];
																SetTileMaterial (tileIndex, mat, temporary);
																if (!temporary) {
																				Tile tile = tiles [tileIndex];
																				colorShadedDirty [tile.uvShadedChunkIndex] = true;
																}
												}
												if (!temporary) {
																pendingColorsUpdate = true;
												}
								}

								/// <summary>
								/// Sets the texture of the tile.
								/// </summary>
								/// <returns><c>true</c>, if tile color was set, <c>false</c> otherwise.</returns>
								/// <param name="tileIndex">Tile index.</param>
								/// <param name="texture">Color.</param>
								/// <param name="temporary">If set to <c>true</c> the tile is colored temporarily and returns to default color when it gets unselected.</param>
								public bool SetTileTexture (int tileIndex, Texture2D texture, bool temporary = false) {
												if (!temporary)
																pendingTextureArrayUpdate = true;
												return SetTileTexture (tileIndex, texture, Color.white, temporary);
								}

								/// <summary>
								/// Sets the texture and tint color of the tile.
								/// </summary>
								/// <returns><c>true</c>, if tile color was set, <c>false</c> otherwise.</returns>
								/// <param name="tileIndex">Tile index.</param>
								/// <param name="texture">Color.</param>
								/// <param name="tint">Optional tint color.</param>
								/// <param name="temporary">If set to <c>true</c> the tile is colored temporarily and returns to default color when it gets unselected.</param>
								public bool SetTileTexture (int tileIndex, Texture2D texture, Color tint, bool temporary = false) {
												Material mat = GetCachedMaterial (tint, texture);
												return SetTileMaterial (tileIndex, mat, temporary);
								}

								/// <summary>
								/// Sets the texture (by texture index in the global texture array) and tint color of the tile
								/// </summary>
								/// <returns><c>true</c>, if tile texture was set, <c>false</c> otherwise.</returns>
								/// <param name="tileIndex">Tile index.</param>
								/// <param name="textureIndex">Texture index.</param>
								/// <param name="tint">Tint.</param>
								/// <param name="temporary">If set to <c>true</c> temporary.</param>
								public bool SetTileTexture (int tileIndex, int textureIndex, Color tint, bool temporary = false) {
												Texture2D texture = null;
												if (textureIndex >= 0 && textureIndex < textures.Length) {
																texture = textures [textureIndex];
												}
												return SetTileTexture (tileIndex, texture, tint, temporary);
								}


								/// <summary>
								/// Sets texture rotation of tile
								/// </summary>
								public bool SetTileTextureRotation (int tileIndex, float rotation) {
												if (tileIndex < 0 || tileIndex >= tiles.Length)
																return false;
												if (rotation != tiles [tileIndex].rotation) {
																tiles [tileIndex].rotation = rotation;
																pendingTextureArrayUpdate = true;
												}
												return true;
								}

								/// <summary>
								/// Returns tile texture rotation
								/// </summary>
								/// <param name="tileIndex">Tile index.</param>
								public float GetTileTextureRotation (int tileIndex) {
												if (tileIndex < 0 || tileIndex >= tiles.Length)
																return 0;
												return tiles [tileIndex].rotation;
								}

								/// <summary>
								/// Returns current tile's fill texture index (if texture exists in textures list).
								/// Texture index is from 1..32. It will return 0 if texture does not exist or it does not match any texture in the list of textures.
								/// </summary>
								public int GetTileTextureIndex (int tileIndex) {
												if (tileIndex < 0 || tileIndex >= tiles.Length || tiles [tileIndex].customMat == null)
																return 0;
												Texture2D tex = (Texture2D)tiles [tileIndex].customMat.mainTexture;
												for (int k = 1; k < textures.Length; k++) {
																if (tex == textures [k])
																				return k;
												}
												return 0;
								}


								/// <summary>
								/// Sets if path finding can cross this tile.
								/// </summary>
								public bool SetTileCanCross (int tileIndex, bool canCross) {
												if (tileIndex < 0 || tileIndex >= tiles.Length)
																return false;
												tiles [tileIndex].canCross = canCross;
												return true;
								}


								/// <summary>
								/// Specifies the tile group (by default 1) used by FindPath tileGroupMask optional argument
								/// </summary>
								public bool SetTileGroup (int tileIndex, int group) {
												if (tileIndex < 0 || tileIndex >= tiles.Length)
																return false;
												tiles [tileIndex].group = group;
												needRefreshRouteMatrix = true;
												return true;
								}

								/// <summary>
								/// Returns cell group (default 1)
								/// </summary>
								public int GetTileGroup (int tileIndex) {
												if (tileIndex < 0 || tileIndex >= tiles.Length)
																return -1;
												return tiles [tileIndex].group;
								}


								/// <summary>
								/// Sets the tile extrude amount. Returns true if tile has been set.
								/// </summary>
								/// <param name="tileIndex">Tile index.</param>
								/// <param name="extrudeAmount">Extrude amount (0-1).</param>
								public bool SetTileExtrudeAmount (int tileIndex, float extrudeAmount) {
												if (tileIndex < 0 || tiles == null || tileIndex >= tiles.Length)
																return false;
												Tile tile = tiles [tileIndex];
												if (extrudeAmount == tile.extrudeAmount)
																return true;
												if (extrudeAmount < 0)
																extrudeAmount = 0;
												else if (extrudeAmount > 1f)
																extrudeAmount = 1f;
												tile.extrudeAmount = extrudeAmount;
												if (tile.renderer != null) {
																DestroyImmediate (tile.renderer.gameObject);
																tile.renderer = null;
												}
												if (_highlightEnabled && tileIndex == lastHighlightedTileIndex) {
																RefreshHighlightedTile ();
												}
												// Fast update uv info
												if (_style != STYLE.Wireframe) {
																List<Vector4> uvShadedChunk = uvShaded [tile.uvShadedChunkIndex];
																for (int k = 0; k < tile.uvShadedChunkLength; k++) {
																				Vector4 uv4 = uvShadedChunk [tile.uvShadedChunkStart + k];
																				uv4.w = tile.extrudeAmount;
																				uvShadedChunk [tile.uvShadedChunkStart + k] = uv4;
																}
																uvShadedDirty [tile.uvShadedChunkIndex] = true;
												}
												if (_style != STYLE.Shaded) {
																List<Vector2> uvWireChunk = uvWire [tile.uvWireChunkIndex];
																for (int k = 0; k < tile.uvWireChunkLength; k++) {
																				Vector2 uv2 = uvWireChunk [tile.uvWireChunkStart + k];
																				uv2.y = tile.extrudeAmount;
																				uvWireChunk [tile.uvWireChunkStart + k] = uv2;
																}
																uvWireDirty [tile.uvWireChunkIndex] = true;
												}
												pendingUVUpdateFast = true;
												return true;
								}


								/// <summary>
								/// Sets the tile extrude amount for a group of tiles.
								/// </summary>
								/// <param name="tiles">Array of tiles.</param>
								/// <param name="extrudeAmount">Extrude amount (0-1).</param>
								public void SetTileExtrudeAmount (Tile[] tiles, float extrudeAmount) {
												if (tiles == null)
																return;
												extrudeAmount = Mathf.Clamp01 (extrudeAmount);
												for (int k = 0; k < tiles.Length; k++) {
																Tile tile = tiles [k];
																if (extrudeAmount != tile.extrudeAmount) {
																				tile.extrudeAmount = extrudeAmount;
																				if (tile.renderer != null) {
																								DestroyImmediate (tile.renderer.gameObject);
																								tile.renderer = null;
																								if (_highlightEnabled && tile.index == lastHighlightedTileIndex) {
																												RefreshHighlightedTile ();
																								}
																				}
																}
																// Fast update uv info
																if (_style != STYLE.Wireframe) {
																				List<Vector4> uvShadedChunk = uvShaded [tile.uvShadedChunkIndex];
																				for (int j = 0; j < tile.uvShadedChunkLength; j++) {
																								Vector4 uv4 = uvShadedChunk [tile.uvShadedChunkStart + j];
																								uv4.w = tile.extrudeAmount;
																								uvShadedChunk [tile.uvShadedChunkStart + j] = uv4;
																				}
																				uvShadedDirty [tile.uvShadedChunkIndex] = true;
																}
																if (_style != STYLE.Shaded) {
																				List<Vector2> uvWireChunk = uvWire [tile.uvWireChunkIndex];
																				for (int j = 0; j < tile.uvWireChunkLength; j++) {
																								Vector4 uv2 = uvWireChunk [tile.uvWireChunkStart + j];
																								uv2.y = tile.extrudeAmount;
																								uvWireChunk [tile.uvWireChunkStart + j] = uv2;
																				}
																				uvWireDirty [tile.uvWireChunkIndex] = true;
																}
												}
												pendingUVUpdateFast = true;
								}

								/// <summary>
								/// Sets the tile extrude amount for a group of tiles.
								/// </summary>
								/// <param name="tiles">Array of tiles.</param>
								/// <param name="extrudeAmount">Extrude amount (0-1).</param>
								public void SetTileExtrudeAmount (List<int> tileIndices, float extrudeAmount) {
												if (tiles == null)
																return;
												extrudeAmount = Mathf.Clamp01 (extrudeAmount);
												int indicesCount = tileIndices.Count;
												for (int k = 0; k < indicesCount; k++) {
																int tileIndex = tileIndices [k];
																Tile tile = tiles [tileIndex];
																if (extrudeAmount != tile.extrudeAmount) {
																				tile.extrudeAmount = extrudeAmount;
																				if (tile.renderer != null) {
																								DestroyImmediate (tile.renderer.gameObject);
																								tile.renderer = null;
																								if (_highlightEnabled && tile.index == lastHighlightedTileIndex) {
																												RefreshHighlightedTile ();
																								}
																				}
																}
																// Fast update uv info
																if (_style != STYLE.Wireframe) {
																				List<Vector4> uvShadedChunk = uvShaded [tile.uvShadedChunkIndex];
																				for (int j = 0; j < tile.uvShadedChunkLength; j++) {
																								Vector4 uv4 = uvShadedChunk [tile.uvShadedChunkStart + j];
																								uv4.w = tile.extrudeAmount;
																								uvShadedChunk [tile.uvShadedChunkStart + j] = uv4;
																				}
																				uvShadedDirty [tile.uvShadedChunkIndex] = true;
																}
																if (_style != STYLE.Shaded) {
																				List<Vector2> uvWireChunk = uvWire [tile.uvWireChunkIndex];
																				for (int j = 0; j < tile.uvWireChunkLength; j++) {
																								Vector4 uv2 = uvWireChunk [tile.uvWireChunkStart + j];
																								uv2.y = tile.extrudeAmount;
																								uvWireChunk [tile.uvWireChunkStart + j] = uv2;
																				}
																				uvWireDirty [tile.uvWireChunkIndex] = true;
																}
												}
												pendingUVUpdateFast = true;
								}

								/// <summary>
								/// Sets the tile extrude amount for a group of tiles.
								/// </summary>
								/// <param name="tiles">List of tiles.</param>
								/// <param name="extrudeAmount">Extrude amount (0-1).</param>
								public void SetTileExtrudeAmount (List<Tile> tiles, float extrudeAmount) {
												Tile[] tempArray = tiles.ToArray ();
												SetTileExtrudeAmount (tempArray, extrudeAmount);
								}

								/// <summary>
								/// Sets the user-defined string tag of a given tile
								/// </summary>
								/// <param name="tileIndex">Tile index.</param>
								/// <param name="tag">String data.</param>
								public bool SetTileTag (int tileIndex, string tag) {
												if (tileIndex < 0 || tileIndex >= tiles.Length)
																return false;
												tiles [tileIndex].tag = tag;
												return true;
								}

								/// <summary>
								/// Sets the user-defined integer tag of a given tile
								/// </summary>
								/// <param name="tileIndex">Tile index.</param>
								/// <param name="tag">Integer data.</param>
								public bool SetTileTag (int tileIndex, int tag) {
												if (tileIndex < 0 || tileIndex >= tiles.Length)
																return false;
												tiles [tileIndex].tagInt = tag;
												return true;
								}

								/// <summary>
								/// Gets the tile string tag.
								/// </summary>
								/// <returns>The tile string tag.</returns>
								/// <param name="tileIndex">Tile index.</param>
								public string GetTileTag (int tileIndex) {
												if (tileIndex < 0 || tileIndex >= tiles.Length)
																return null;
												return tiles [tileIndex].tag;
								}

								/// <summary>
								/// Gets the tile int tag.
								/// </summary>
								/// <returns>The tile string tag.</returns>
								/// <param name="tileIndex">Tile index.</param>
								public int GetTileTagInt (int tileIndex) {
												if (tileIndex < 0 || tileIndex >= tiles.Length)
																return 0;
												return tiles [tileIndex].tagInt;
								}


								/// <summary>
								/// Removes any extrusion amount from all tiles
								/// </summary>
								public void ClearTilesExtrusion () {
												SetTileExtrudeAmount (tiles, 0);
								}

								/// <summary>
								/// Returns whether path finding can cross this tile.
								/// </summary>
								public bool GetTileCanCross (int tileIndex) {
												if (tileIndex < 0 || tileIndex >= tiles.Length)
																return false;
												return tiles [tileIndex].canCross;
								}

								/// <summary>
								/// Returns current tile color.
								/// </summary>
								public Color GetTileColor (int tileIndex, bool ignoreTemporaryColor = false) {
												if (tileIndex < 0 || tileIndex >= tiles.Length)
																return _defaultShadedColor;
												Tile tile = tiles [tileIndex];
												if (tile.tempMat != null && !ignoreTemporaryColor)
																return tile.tempMat.color;
												if (tile.customMat != null)
																return tile.customMat.color;
												return _defaultShadedColor;
								}


								/// <summary>
								/// Returns current tile height or extrude amount.
								/// </summary>
								public float GetTileExtrudeAmount (int tileIndex) {
												if (tileIndex < 0 || tileIndex >= tiles.Length)
																return 0;
												return tiles [tileIndex].extrudeAmount;
								}

								/// <summary>
								/// Gets the neighbours indices of a given tile
								/// </summary>
								public int[] GetTileNeighbours (int tileIndex) {
												if (tileIndex < 0 || tileIndex >= tiles.Length)
																return null;
												return tiles [tileIndex].neighboursIndices;
								}

								/// <summary>
								/// Gets the neighbours objects of a given tile
								/// </summary>
								public Tile[] GetTileNeighboursTiles (int tileIndex) {
												if (tileIndex < 0 || tileIndex >= tiles.Length)
																return null;
												return tiles [tileIndex].neighbours;
								}

								/// <summary>
								/// Gets the index of the tile on the exact opposite pole
								/// </summary>
								public int GetTileAtPolarOpposite (int tileIndex) {
												if (tileIndex < 0 || tileIndex >= tiles.Length)
																return -1;
												return GetTileAtLocalPosition (-tiles [tileIndex].center, true);
								}

								/// <summary>
								/// Gets an array of tile indices found within a distance to a given tile
								/// </summary>
								/// <returns>The tiles within distance.</returns>
								/// <param name="tileIndex">Tile index.</param>
								/// <param name="worldSpace">By default, distance is used in local space in the range of 0..1 (which is faster). Using world space = true will compute distances in world space which will apply the current transform to the examined tile centers.</param>
								public List<int> GetTilesWithinDistance (int tileIndex, float distance, bool worldSpace = false) {
												if (tileIndex < 0 || tileIndex >= tiles.Length)
																return null;

												Vector3 refPos;
												if (worldSpace) {
																refPos = GetTileCenter (tileIndex);
												} else {
																refPos = tiles [tileIndex].center;
												}
												float d2 = distance * distance;
												List <int> candidates = new List<int> (GetTileNeighbours (tileIndex));
												Dictionary<int,bool> processed = new Dictionary<int,bool> (); // dictionary is faster for value types than HashSet
												processed [tileIndex] = true;
												List<int> results = new List<int> ();
												int candidateLast = candidates.Count - 1;
												while (candidateLast >= 0) {
																// Pop candidate
																int t = candidates [candidateLast];
																candidates.RemoveAt (candidateLast);
																candidateLast--;
																float dist;
																if (worldSpace) {
																				dist = Misc.Vector3SqrDistance (GetTileCenter (t), refPos);
																} else {
																				dist = Misc.Vector3SqrDistance (tiles [t].center, refPos);
																}
																if (dist < d2) {
																				results.Add (t);
																				processed [t] = true;
																				int[] nn = GetTileNeighbours (t);
																				for (int k = 0; k < nn.Length; k++) {
																								if (!processed.ContainsKey (nn [k])) {
																												candidates.Add (nn [k]);
																												candidateLast++;
																								}
																				}
																}
												}
												return results;
								}



								/// <summary>
								/// Gets an array of tile indices found within a number of tile steps
								/// </summary>
								/// <returns>The tiles within distance.</returns>
								/// <param name="maxSteps">Max number of steps.</param>
								public List<int> GetTilesWithinSteps (int tileIndex, int maxSteps) {
												if (tileIndex < 0 || tileIndex >= tiles.Length)
																return null;

												List <int> candidates = new List<int> (GetTileNeighbours (tileIndex));
												Dictionary<int,bool> processed = new Dictionary<int,bool> (tileIndex); // dictionary is faster for value types than HashSet
												processed [tileIndex] = true;
												List<int> results = new List<int> ();
												int candidateLast = candidates.Count - 1;
												while (candidateLast >= 0) {
																// Pop candidate
																int t = candidates [candidateLast];
																candidates.RemoveAt (candidateLast);
																candidateLast--;
																List<int> tt = FindPath (tileIndex, t, maxSteps);
																if (tt != null) {
																				results.Add (t);
																				processed [t] = true;
																				int[] nn = GetTileNeighbours (t);
																				for (int k = 0; k < nn.Length; k++) {
																								if (!processed.ContainsKey (nn [k])) {
																												candidates.Add (nn [k]);
																												candidateLast++;
																								}
																				}
																}
												}
												return results;
								}

								/// <summary>
								/// Hide a given tile
								/// </summary>
								public void ClearTile (int tileIndex, bool clearTemporaryColor = false, bool clearAllColors = true, bool clearObstacles = true) {
												if (tileIndex < 0 || tileIndex >= tiles.Length)
																return;
												Tile tile = tiles [tileIndex];
												Renderer tileRenderer = tile.renderer;
												tile.tempMat = null;
												if (tileRenderer != null) {
																tileRenderer.enabled = false;
												}
												if (clearAllColors) {
																if (tile.customMat != null) {
																				if (tile.customMat.mainTexture) {
																								pendingTextureArrayUpdate = true;
																				}
																				tile.customMat = null;
																}
																pendingColorsUpdate = true;
																Color32 matColor = _defaultShadedColor;
																List<Color32> colorChunk = colorShaded [tile.uvShadedChunkIndex];
																for (int k = 0; k < tile.uvShadedChunkLength; k++) {
																				colorChunk [tile.uvShadedChunkStart + k] = matColor;
																}
																colorShadedDirty [tile.uvShadedChunkIndex] = true;
												}
												if (clearObstacles) {
																tile.canCross = true;
												}
								}


								/// <summary>
								/// Hide all tiles
								/// </summary>
								public void ClearTiles (bool clearTemporaryColors = false, bool clearAllColors = true, bool clearObstacles = true) {
												for (int k = 0; k < tiles.Length; k++) {
																ClearTile (k, clearTemporaryColors, clearAllColors, clearObstacles);
												}
												ResetHighlightMaterial ();
								}

								/// <summary>
								/// Destroys a colored tile
								/// </summary>
								public void DestroyTile (int tileIndex) {
												if (tileIndex < 0 || tileIndex >= tiles.Length)
																return;
												if (tiles [tileIndex].customMat != null) {
																tiles [tileIndex].customMat = null;
																pendingColorsUpdate = true;
																pendingTextureArrayUpdate = true;
																colorShadedDirty [tiles [tileIndex].uvShadedChunkIndex] = true;
												}
												if (tiles [tileIndex].renderer != null) {
																DestroyImmediate (tiles [tileIndex].renderer.gameObject);
																tiles [tileIndex].renderer = null;
												}
								}

								/// <summary>
								/// Toggles tile visibility
								/// </summary>
								public bool ToggleTile (int tileIndex, bool visible) {
												if (tileIndex < 0 || tileIndex >= tiles.Length)
																return false;
												if (tiles [tileIndex].renderer != null) {
																tiles [tileIndex].renderer.enabled = visible;
																return true;
												}
												return false;
								}

								/// <summary>
								/// Hides a colored tile
								/// </summary>
								public bool HideTile (int tileIndex) {
												if (tileIndex < 0 || tileIndex >= tiles.Length)
																return false;
												if (tiles [tileIndex].renderer != null) {
																tiles [tileIndex].renderer.enabled = false;
																return true;
												}
												return false;
								}

								/// <summary>
								/// Shows a colored tile
								/// </summary>
								public bool ShowTile (int tileIndex) {
												if (tileIndex < 0 || tileIndex >= tiles.Length)
																return false;
												if (tiles [tileIndex].renderer != null) {
																tiles [tileIndex].renderer.enabled = true;
																return true;
												}
												return false;
								}

								/// <summary>
								/// Returns the center of the tile in world space coordinates.
								/// </summary>
								[Obsolete ("Use GetTileCenter(tileIndex)")]
								public Vector3 GetWorldSpaceTileCenter (int tileIndex) {
												return GetTileCenter (tileIndex, false);
								}

								/// <summary>
								/// Returns the position of the tile vertex.
								/// </summary>
								[Obsolete ("Use GetTileVertexPosition")]
								public Vector3 GetWorldSpaceTileVertex (int tileIndex, int vertexIndex, bool worldSpace = true) {
												return GetTileVertexPosition (tileIndex, vertexIndex, worldSpace);
								}

								/// <summary>
								/// Gets the tile center in local or world space coordinates.
								/// </summary>
								/// <returns>The tile center.</returns>
								/// <param name="tileIndex">Tile index.</param>
								/// <param name="worldSpace">If set to <c>true</c> it returns the world space coordinates.</param>
								public Vector3 GetTileCenter (int tileIndex, bool worldSpace = true) {
												if (tileIndex < 0 || tileIndex >= tiles.Length)
																return Misc.Vector3zero;
												Tile tile = tiles [tileIndex];
												if (worldSpace) {
																Vector3 tileTop = transform.TransformPoint (tile.center * (1.0f + tile.extrudeAmount * _extrudeMultiplier));
																return tileTop;
												} else {
																return tile.center;
												}
								}

								/// <summary>
								/// Returns the center of the tile in world space coordinates.
								/// </summary>
								public Vector3 GetTileVertexPosition (int tileIndex, int vertexIndex, bool worldSpace = true) {
												if (tileIndex < 0 || tileIndex >= tiles.Length)
																return Misc.Vector3zero;
												Tile tile = tiles [tileIndex];
												if (vertexIndex < 0 || vertexIndex >= tile.vertices.Length)
																return Misc.Vector3zero;
												Vector3 v = tile.vertices [vertexIndex] * (1.0f + tile.extrudeAmount * _extrudeMultiplier);
												if (worldSpace) {
																v = transform.TransformPoint (v);
												}
												return v;
								}



								/// <summary>
								/// Gets the tile under a given position in world space coordinates.
								/// </summary>
								/// <returns>The tile at position.</returns>
								/// <param name="worldPosition">World position.</param>
								public int GetTileAtPos (Vector3 worldPosition) {
												if (tiles == null)
																return -1;

												Vector3 localPosition = transform.InverseTransformPoint (worldPosition);
												return GetTileAtLocalPosition (localPosition, false);
								}

								/// <summary>
								/// Gets the tile under a given position in local space coordinates.
								/// </summary>
								/// <returns>The tile at local position.</returns>
								/// <param name="localPosition">Local position.</param>
								public int GetTileAtLocalPos (Vector3 localPosition) {
												return GetTileAtLocalPosition (localPosition, false);
								}


								/// <summary>
								/// Returns a jSON formatted representation of current cells settings.
								/// </summary>
								public string GetTilesConfigurationData () {
												List<TileSaveData> tsd = new List<TileSaveData> ();
												for (int k = 0; k < tiles.Length; k++) {
																Tile tile = tiles [k];
																if (tile.tagInt != 0 || tile.customMat != null || !string.IsNullOrEmpty (tile.tag)) {
																				TileSaveData sd = new TileSaveData ();
																				sd.tileIndex = k;
																				sd.color = tile.customMat.color;
																				sd.textureIndex = GetTileTextureIndex (k);
																				sd.tag = tile.tag;
																				sd.tagInt = tile.tagInt;
																				tsd.Add (sd);
																}
												}
												HexasphereSaveData hsd = new HexasphereSaveData ();
												hsd.tiles = tsd.ToArray ();
												return JsonUtility.ToJson (hsd);
								}

								public void SetTilesConfigurationData (string json) {
												if (tiles == null)
																return;
												
												HexasphereSaveData hsd = JsonUtility.FromJson<HexasphereSaveData> (json);
												for (int k = 0; k < hsd.tiles.Length; k++) {
																int tileIndex = hsd.tiles [k].tileIndex;
																if (tileIndex < 0 || tileIndex >= tiles.Length)
																				continue;
																tiles [tileIndex].tag = hsd.tiles [k].tag;
																tiles [tileIndex].tagInt = hsd.tiles [k].tagInt;
																SetTileTexture (tileIndex, hsd.tiles [k].textureIndex, hsd.tiles [k].color);
												}
								}

								#endregion

		
				}

}