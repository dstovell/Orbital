/// <summary>
/// Hexasphere Grid System
/// Created by Ramiro Oliva (Kronnect)
/// </summary>

using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;


namespace HexasphereGrid {
				
				public enum STYLE {
								Wireframe = 0,
								Shaded = 1,
								ShadedWireframe = 2
				}

				public partial class Hexasphere: MonoBehaviour {

								#region Public properties and configuration

								[SerializeField]
								int _numDivisions = 8;

								public int numDivisions {
												get { return _numDivisions; }
												set {
																if (_numDivisions != value) {
																				_numDivisions = Mathf.Max (2, value);
																				if (Application.isPlaying) {
																								UpdateMaterialProperties ();
																				}
																}
												}
								}

								[SerializeField]
								STYLE _style = STYLE.ShadedWireframe;

								public STYLE style {
												get { return _style; }
												set {
																if (_style != value) {
																				_style = value;
																				UpdateMaterialProperties ();
																}
												}
								}


								[SerializeField]
								bool _invertedMode = false;

								public bool invertedMode {
												get { return _invertedMode; }
												set {
																if (_invertedMode != value) {
																				_invertedMode = value;
																				if (_invertedMode)
																								_extruded = false;
																				UpdateMaterialProperties ();
																}
												}
								}

								[SerializeField]
								bool _extruded = false;

								public bool extruded {
												get { return _extruded; }
												set {
																if (_extruded != value) {
																				_extruded = value && !_invertedMode;
																				UpdateMaterialProperties ();
																}
												}
								}


								[SerializeField]
								int _tileTextureSize = 256;

								public int tileTextureSize {
												get { return _tileTextureSize; }
												set {
																if (_tileTextureSize != value) {
																				_tileTextureSize = value;
																				pendingTextureArrayUpdate = true;
																				UpdateMaterialProperties ();
																}
												}
								}

								[SerializeField]
								[Range (0, 1f)]
								float _extrudeMultiplier = 0.05f;

								public float extrudeMultiplier {
												get { return _extrudeMultiplier; }
												set {
																if (_extrudeMultiplier != value) {
																				_extrudeMultiplier = value;
																				DestroyCachedTiles (true);
																				UpdateMaterialProperties ();
																}
												}
								}

								[SerializeField]
								bool _VREnabled = false;

								public bool VREnabled {
												get { return _VREnabled; }
												set {
																if (_VREnabled != value) {
																				_VREnabled = value;
																				UpdateMaterialProperties ();
																}
												}
								}


								[SerializeField]
								[Range (0, 1f)]
								float _gradientIntensity = 0.25f;

								public float gradientIntensity {
												get { return _gradientIntensity; }
												set {
																if (_gradientIntensity != value) {
																				_gradientIntensity = value;
																				UpdateMaterialProperties ();
																}
												}
								}

								[SerializeField]
								Color _wireframeColor = Color.white;

								public Color wireframeColor {
												get { return _wireframeColor; }
												set {
																if (_wireframeColor != value) {
																				_wireframeColor = value;
																				UpdateMaterialProperties ();
																}
												}
								}


								[SerializeField]
								bool _wireframeColorFromTile = false;

								public bool wireframeColorFromTile {
												get { return _wireframeColorFromTile; }
												set {
																if (_wireframeColorFromTile != value) {
																				_wireframeColorFromTile = value;
																				UpdateMaterialProperties ();
																}
												}
								}


								[SerializeField]
								[Range (0, 2f)]
								float _wireframeIntensity = 1f;

								public float wireframeIntensity {
												get { return _wireframeIntensity; }
												set {
																if (_wireframeIntensity != value) {
																				_wireframeIntensity = value;
																				UpdateMaterialProperties ();
																}
												}
								}


								[SerializeField]
								Color _defaultShadedColor = new Color (0.56f, 0.71f, 0.54f);

								public Color defaultShadedColor {
												get { return _defaultShadedColor; }
												set {
																if (_defaultShadedColor != value) {
																				_defaultShadedColor = value;
																				UpdateMaterialProperties ();
																}
												}
								}


								[SerializeField]
								Vector3 _rotationShift;

								public Vector3 rotationShift {
												get { return _rotationShift; }
												set {
																if (_rotationShift != value) {
																				_rotationShift = value;
																				UpdateMaterialProperties ();
																}
												}
								}

								[SerializeField]
								bool _enableGridEditor = true;

								/// <summary>
								/// Enabled grid editing options in Scene View
								/// </summary>
								public bool enableGridEditor { 
												get {
																return _enableGridEditor; 
												}
												set {
																if (value != _enableGridEditor) {
																				_enableGridEditor = value;
																				UpdateMaterialProperties ();
																}
												}
								}

								/// <summary>
								/// Array of user-defined textures that can be used in the Grid Editor to customize aspect
								/// </summary>
								public Texture2D[] textures;

								#endregion

								#region Public API

								public static Hexasphere GetInstance (string gameObjectName) {
												Hexasphere[] hh = Resources.FindObjectsOfTypeAll<Hexasphere> ();
												for (int k = 0; k < hh.Length; k++) {
																if (hh [k] != null && hh [k].gameObject.name.Equals (gameObjectName)) {
																				return hh [k];
																}
												}
												return null;
								}

								#endregion

		
				}

}