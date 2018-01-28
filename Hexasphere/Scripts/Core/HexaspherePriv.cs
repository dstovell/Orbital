/// <summary>
/// Hexasphere Grid System
/// Created by Ramiro Oliva (Kronnect)
/// </summary>


//#define VR_GOOGLE				  	 							    // Uncomment this line to support Google VR SDK (pointer and controller touch)
//#define VR_SAMSUNG_GEAR_CONTROLLER  // Uncomment this line to support Samsung Gear VR SDK (laser pointer)

//#define TRACE_PERFORMANCE    	      // Used to track performance metrics. Internal use.
//#define RAYCAST3D_DEBUG             // Used to debug raycasting in extrusion mode. Internal use.

using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif
#if VR_GOOGLE
using GVR;
#endif
using System;
using System.Collections;
using System.Collections.Generic;


namespace HexasphereGrid {
				public delegate Point GetCachedPointDelegate (Point point);

				[ExecuteInEditMode]
				public partial class Hexasphere : MonoBehaviour {

								const float MIN_FIELD_OF_VIEW = 10.0f;
								const float MAX_FIELD_OF_VIEW = 85.0f;
								const int MAX_TEXTURES = 255;

								//public Material _tileShadedFrameMatExtrusion, _tileShadedFrameMatNoExtrusion;
								//public Material _gridMatExtrusion, _gridMatNoExtrusion;
								//public Material _tileColoredMat, _tileTexturedMat;
								public Material highlightMaterial;
								int currentDivisions, currentTextureSize;
								bool currentExtruded, currentInvertedMode, currentWireframeColorFromTile;
								Color currentDefautlShadedColor;
								bool pendingUVUpdateFast, pendingTextureArrayUpdate, pendingColorsUpdate;
								STYLE currentStyle;
								Vector3 lastLocalPositionClicked;
								bool mouseIsOver, mouseStartedDragging, hasDragged;
								Vector3 mouseDragStartScreenPos, mouseDragStartLocalPosition;
								float wheelAccel;
								Quaternion flyingStartRotation, flyingEndRotation;
								bool flying;
								float flyingStartTime, flyingDuration;
								public Texture2D defaultRampTexture;
								SphereCollider sphereCollider;
								int lastHitTileIndex;
								Texture2D whiteTex;
								int uvChunkCount, wireChunkCount;
								Vector3 currentRotationShift;
								bool leftMouseButtonClick, leftMouseButtonPressed, leftMouseButtonRelease;
								bool rightMouseButtonPressed;
								bool allowedTextureArray;
								bool useEditorRay;
								Ray editorRay;

								#region Gameloop events

								void OnEnable () {
												Init ();
								}

								void Start () {
												RegisterVRPointers ();
								}

								void OnDestroy () {
												if (_gridMatExtrusion != null)
																DestroyImmediate (_gridMatExtrusion);
												if (_gridMatNoExtrusion != null)
																DestroyImmediate (_gridMatNoExtrusion);
												if (_tileShadedFrameMatExtrusion != null)
																DestroyImmediate (_tileShadedFrameMatExtrusion);
												if (_tileShadedFrameMatNoExtrusion != null)
																DestroyImmediate (_tileShadedFrameMatNoExtrusion);
												if (_tileColoredMat != null)
																DestroyImmediate (_tileColoredMat);
												if (_tileColoredMat != null)
																DestroyImmediate (_tileColoredMat);
												if (_tileTexturedMat != null)
																DestroyImmediate (_tileTexturedMat);
								}

								void LateUpdate () {

#if RAYCAST3D_DEBUG
												if (Input.GetKeyDown (KeyCode.D))
																rayDebug = true;
#endif

												if (pendingTextureArrayUpdate) {
																UpdateShadedMaterials ();
																pendingUVUpdateFast = false;
												} else if (pendingUVUpdateFast || pendingColorsUpdate) {
																UpdateShadedMaterialsFast ();
																UpdateWireMaterialsFast ();
																pendingUVUpdateFast = false;
												}
												if (pendingUVUpdateFast) {
																UpdateWireMaterialsFast ();
																pendingUVUpdateFast = false;
												}

												if (highlightMaterial != null && lastHighlightedTileIndex >= 0) {
																highlightMaterial.SetFloat ("_ColorShift", Mathf.PingPong (Time.time * _highlightSpeed, 1f));
												}

												// Check mouse buttons state
												leftMouseButtonClick = Input.GetMouseButtonDown (0) || Input.GetButtonDown ("Fire1");
												#if VR_GOOGLE
												if (GvrController.TouchDown) {
												GVR_TouchStarted = true;
												leftMouseButtonClick = true;
												}
												#endif

												leftMouseButtonPressed = leftMouseButtonClick || Input.GetMouseButton (0);
												#if VR_GOOGLE
												if (GVR_TouchStarted)
												leftMouseButtonPressed = true;
												#endif

												leftMouseButtonRelease = Input.GetMouseButtonUp (0) || Input.GetButtonUp ("Fire1");
												#if VR_GOOGLE
												if (GvrController.TouchUp) {
												GVR_TouchStarted = false;
												leftMouseButtonRelease = true;
												}
												#endif

												rightMouseButtonPressed = Input.GetMouseButton (1);

												if (_invertedMode) {
																CheckUserInteractionInvertedMode ();
												} else if (mouseIsOver || _VREnabled) {
																CheckUserInteractionNormalMode ();
												}

												if (flying) {
																float t = (Time.time - flyingStartTime) / flyingDuration;
																t = Mathf.Clamp01 (t);
																transform.rotation = Quaternion.Slerp (flyingStartRotation, flyingEndRotation, t);
																if (t >= 1)
																				flying = false;
												}
								}

								void OnMouseEnter () {
												mouseIsOver = true;
								}

								void OnMouseExit () {

												if (_VREnabled)
																return;

												// Check if it's really outside of hexasphere
												Vector3 dummy;
												Ray dummyRay;
												if (!GetHitPoint (out dummy, out dummyRay)) {
																mouseIsOver = false;
												}
												if (!mouseIsOver) {
																HideHighlightedTile ();
												}
								}

								void FixedUpdate () {
												if (_style != STYLE.Shaded) {
																if (_gridMatExtrusion != null) {
																				_gridMatExtrusion.SetVector ("_Center", transform.position);
																}
												}
								}

								#endregion

								#region Initialization

								public void Init () {
												sphereCollider = GetComponent<SphereCollider> ();
												if (sphereCollider == null) {
																sphereCollider = gameObject.AddComponent<SphereCollider> ();
												}
												if (highlightMaterial == null) {
																highlightMaterial = Resources.Load<Material> ("Materials/HexaTileHighlightMat");
												}

												allowedTextureArray = SystemInfo.supports2DArrayTextures;
												if (!allowedTextureArray) {
																Debug.LogWarning ("Current platform does not support array textures. Hexasphere shading won't work.");
												}

												if (!_invertedMode && Camera.main != null)
																oldCameraPosition = Camera.main.transform.position;

												if (textures == null || textures.Length < MAX_TEXTURES)
																textures = new Texture2D[MAX_TEXTURES];

												UpdateMaterialProperties ();
								}


								#endregion



								#region Interaction

								/// <summary>
								/// Issues a selection check based on a given ray. Used by editor to manipulate tiles from Scene window.
								/// Returns true if ray hits the grid.
								/// </summary>
								public bool CheckRay (Ray ray) {
												useEditorRay = true;
												editorRay = ray;
												Vector3 dummyPos;
												Ray dummyRay;
												if (_invertedMode) {
																CheckMousePosInvertedMode (out dummyPos, out dummyRay);
												} else {
																CheckMousePosNormalMode (out dummyPos, out dummyRay);
												}
												if (!mouseIsOver) {
																HideHighlightedTile();
												}
												return mouseIsOver;
								}

								void CheckUserInteractionNormalMode () {
												Vector3 position;
												Ray ray;

												CheckMousePosNormalMode (out position, out ray);

												if (_rotationEnabled) {
																if (leftMouseButtonClick) {
																				#if VR_GOOGLE
																				mouseDragStartScreenPos = GvrController.TouchPos;
																				#endif
																				mouseDragStartLocalPosition = transform.InverseTransformPoint (position);
																				mouseStartedDragging = true;
																				hasDragged = false;
																} else if (mouseStartedDragging && (leftMouseButtonPressed || (Input.touchSupported && Input.touchCount == 1))) {
																				#if VR_GOOGLE
																				float distFactor = Mathf.Min (Vector3.Distance (Camera.main.transform.position, transform.position) / transform.localScale.y, 1f);
																				Vector3 dragDirection = (mouseDragStartScreenPos - (Vector3)GvrController.TouchPos) * distFactor * _mouseDragSensitivity;
																					dragDirection.y *= -1.0f;
																					if (dragDirection.x != 0 || dragDirection.y != 0) {
																					hasDragged = true;
																				gameObject.transform.Rotate (Vector3.up, dragDirection.x, Space.World);
																				Vector3 axisY = Vector3.Cross (transform.position - Camera.main.transform.position, Vector3.up);
																				transform.Rotate (axisY, dragDirection.y, Space.World);																				}
																				#else
																				Vector3 localPos = transform.InverseTransformPoint (position);
																				if (localPos != mouseDragStartLocalPosition) {
																								float angle = Vector3.Angle (mouseDragStartLocalPosition, localPos);
																								Quaternion rot = Quaternion.AngleAxis (angle, Vector3.Cross (mouseDragStartLocalPosition, localPos));  
																								if (_rotationSpeed < 1f) {
																												Quaternion newRot = transform.rotation * rot;
																												transform.rotation = Quaternion.Slerp (transform.rotation, newRot, _rotationSpeed);
																								} else {
																												transform.rotation *= rot;
																								}
																								hasDragged = true;
																				}
																				#endif
																} else {
																				mouseStartedDragging = false;
																}
												}

												if (_rightClickRotates && rightMouseButtonPressed) {
																Vector3 axis = (transform.position - Camera.main.transform.position).normalized;
																float rotAngle = _rightClickRotatingClockwise ? -2f : 2f;
																transform.Rotate (axis, rotAngle, Space.World);
												}

												if (OnTileClick != null && leftMouseButtonRelease && (Application.isMobilePlatform || (!hasDragged && !mouseStartedDragging))) {
																OnTileClick (lastHighlightedTileIndex);
												}

												if (_zoomEnabled) {
																// Use mouse wheel to zoom in and out
																float wheel = Input.GetAxis ("Mouse ScrollWheel");
																wheelAccel += wheel;

																// Support for pinch on mobile
																if (Input.touchSupported && Input.touchCount == 2) {
																				// Store both touches.
																				Touch touchZero = Input.GetTouch (0);
																				Touch touchOne = Input.GetTouch (1);

																				// Find the position in the previous frame of each touch.
																				Vector2 touchZeroPrevPos = touchZero.position - touchZero.deltaPosition;
																				Vector2 touchOnePrevPos = touchOne.position - touchOne.deltaPosition;

																				// Find the magnitude of the vector (the distance) between the touches in each frame.
																				float prevTouchDeltaMag = (touchZeroPrevPos - touchOnePrevPos).magnitude;
																				float touchDeltaMag = (touchZero.position - touchOne.position).magnitude;

																				// Find the difference in the distances between each frame.
																				float deltaMagnitudeDiff = prevTouchDeltaMag - touchDeltaMag;

																				// Pass the delta to the wheel accel
																				wheelAccel += deltaMagnitudeDiff;
																}

																if (wheelAccel != 0) {
																				wheelAccel = Mathf.Clamp (wheelAccel, -0.1f, 0.1f);
																				if (wheelAccel >= 0.01f || wheelAccel <= -0.01f) {
																								Vector3 camPos = Camera.main.transform.position - (transform.position - Camera.main.transform.position) * wheelAccel * _zoomSpeed;
																								Camera.main.transform.position = camPos;
																								float radiusSqr = (1.0f + _zoomMinDistance) * transform.localScale.z * 0.5f + (Camera.main.nearClipPlane + 0.01f);
																								radiusSqr *= radiusSqr;
																								float camDistSqr = (Camera.main.transform.position - transform.position).sqrMagnitude;
																								if (camDistSqr < radiusSqr) {
																												Camera.main.transform.position = transform.position + (Camera.main.transform.position - transform.position).normalized * Mathf.Sqrt (radiusSqr); // + 0.01f);
																												wheelAccel = 0;
																								} else {
																												radiusSqr = _zoomMaxDistance + transform.localScale.z * 0.5f + Camera.main.nearClipPlane;
																												radiusSqr *= radiusSqr;
																												if (camDistSqr > radiusSqr) {
																																Camera.main.transform.position = transform.position + (Camera.main.transform.position - transform.position).normalized * Mathf.Sqrt (radiusSqr - 0.01f);
																																wheelAccel = 0;
																												}
																								}
																								wheelAccel *= _zoomDamping; // smooth dampening
																				}
																} else {
																				wheelAccel = 0;
																}
												}
								}

								void CheckUserInteractionInvertedMode () {
												Vector3 position;
												Ray ray;

												CheckMousePosInvertedMode (out position, out ray);
												if (!mouseIsOver)
																return;

												if (_rotationEnabled) {
																if (leftMouseButtonClick) {
																				#if VR_GOOGLE
																				mouseDragStartScreenPos = GvrController.TouchPos;
																				#endif
																				mouseDragStartLocalPosition = transform.InverseTransformPoint (position);
																				mouseStartedDragging = true;
																				hasDragged = false;
																} else if (mouseStartedDragging && (leftMouseButtonPressed || (Input.touchSupported && Input.touchCount == 1))) {
																				#if VR_GOOGLE
																				float distFactor = Mathf.Min (Vector3.Distance (Camera.main.transform.position, transform.position) / transform.localScale.y, 1f);
																				Vector3 dragDirection = (mouseDragStartScreenPos - (Vector3)GvrController.TouchPos) * distFactor * _mouseDragSensitivity;
																				dragDirection.y *= -1.0f;
																				if (dragDirection.x != 0 || dragDirection.y != 0) {
																				hasDragged = true;
																				gameObject.transform.Rotate (Vector3.up, dragDirection.x, Space.World);
																				Vector3 axisY = Vector3.Cross (transform.position - Camera.main.transform.position, Vector3.up);
																				transform.Rotate (axisY, dragDirection.y, Space.World);																				}
																				#else
																				Vector3 localPos = transform.InverseTransformPoint (position);
																				if (localPos != mouseDragStartLocalPosition) {
																								float angle = Vector3.Angle (mouseDragStartLocalPosition, localPos);
																								Quaternion rot = Quaternion.AngleAxis (angle, Vector3.Cross (mouseDragStartLocalPosition, localPos));  
																								if (_rotationSpeed < 1f) {
																												Quaternion newRot = transform.rotation * rot;
																												transform.rotation = Quaternion.Slerp (transform.rotation, newRot, _rotationSpeed);
																								} else {
																												transform.rotation *= rot;
																								}
																								hasDragged = true;
																				}
																				#endif
																} else {
																				mouseStartedDragging = false;
																}
												}

												if (_rightClickRotates && rightMouseButtonPressed) {
																Vector3 axis = Camera.main.transform.forward;
																float rotAngle = _rightClickRotatingClockwise ? -2f : 2f;
																transform.Rotate (axis, rotAngle, Space.World);
												}

												if (!hasDragged && !mouseStartedDragging && OnTileClick != null && leftMouseButtonRelease) {
																OnTileClick (lastHighlightedTileIndex);
												}

												if (_zoomEnabled) {
																// Use mouse wheel to zoom in and out
																float wheel = Input.GetAxis ("Mouse ScrollWheel");
																wheelAccel += wheel;

																// Support for pinch on mobile
																if (Input.touchSupported && Input.touchCount == 2) {
																				// Store both touches.
																				Touch touchZero = Input.GetTouch (0);
																				Touch touchOne = Input.GetTouch (1);

																				// Find the position in the previous frame of each touch.
																				Vector2 touchZeroPrevPos = touchZero.position - touchZero.deltaPosition;
																				Vector2 touchOnePrevPos = touchOne.position - touchOne.deltaPosition;

																				// Find the magnitude of the vector (the distance) between the touches in each frame.
																				float prevTouchDeltaMag = (touchZeroPrevPos - touchOnePrevPos).magnitude;
																				float touchDeltaMag = (touchZero.position - touchOne.position).magnitude;

																				// Find the difference in the distances between each frame.
																				float deltaMagnitudeDiff = prevTouchDeltaMag - touchDeltaMag;

																				// Pass the delta to the wheel accel
																				wheelAccel += deltaMagnitudeDiff;
																}

																if (wheelAccel != 0) {
																				wheelAccel = Mathf.Clamp (wheelAccel, -0.1f, 0.1f);
																				if (wheelAccel >= 0.01f || wheelAccel <= -0.01f) {
																								Camera.main.fieldOfView = Mathf.Clamp (Camera.main.fieldOfView + (90.0f * Camera.main.fieldOfView / MAX_FIELD_OF_VIEW) * wheelAccel * _zoomSpeed, MIN_FIELD_OF_VIEW, MAX_FIELD_OF_VIEW);
																								wheelAccel *= _zoomDamping; // smooth dampening
																				}
																} else {
																				wheelAccel = 0;
																}
												}
								}


								void CheckMousePosNormalMode (out Vector3 position, out Ray ray) {
												mouseIsOver = GetHitPoint (out position, out ray);
												if (!mouseIsOver)
																return;

												if (_highlightEnabled || OnTileMouseOver != null || (!Application.isPlaying && useEditorRay)) {
																int tileIndex;
																if (_extruded && _raycast3D) {
																				tileIndex = GetTileInRayDirection (ray, position);
																} else {
																				Vector3 localPosition = transform.InverseTransformPoint (position);
																				tileIndex = GetTileAtLocalPosition (localPosition, true);
																}
																if (tileIndex >= 0 && tileIndex != lastHighlightedTileIndex) {
																				if (OnTileMouseOver != null)
																								OnTileMouseOver (tileIndex);
																				if (_highlightEnabled) {
																								if (lastHighlightedTile != null)
																												HideHighlightedTile ();
																								lastHighlightedTile = tiles [tileIndex];
																								lastHighlightedTileIndex = tileIndex;
																								SetTileMaterial (lastHighlightedTileIndex, highlightMaterial, true);
																				}
																} else if (tileIndex < 0 && lastHighlightedTileIndex >= 0) {
																				HideHighlightedTile ();
																}
												}
								}

								void CheckMousePosInvertedMode (out Vector3 position, out Ray ray) {
												mouseIsOver = GetHitPoint (out position, out ray);
												if (!mouseIsOver)
																return;

												if (_highlightEnabled || OnTileMouseOver != null) {
																int tileIndex;
																Vector3 localPosition = transform.InverseTransformPoint (position);
																tileIndex = GetTileAtLocalPosition (localPosition, true);
																if (tileIndex >= 0 && tileIndex != lastHighlightedTileIndex) {
																				if (OnTileMouseOver != null)
																								OnTileMouseOver (tileIndex);
																				if (_highlightEnabled) {
																								if (lastHighlightedTile != null)
																												HideHighlightedTile ();
																								lastHighlightedTile = tiles [tileIndex];
																								lastHighlightedTileIndex = tileIndex;
																								SetTileMaterial (lastHighlightedTileIndex, highlightMaterial, true);
																				}
																} else if (tileIndex < 0 && lastHighlightedTileIndex >= 0) {
																				HideHighlightedTile ();
																}
												}
								}


								#endregion

								#region Hexasphere builder

								// internal fields
								const string HEXASPHERE_WIREFRAME = "WireFrame";
								const string HEXASPHERE_SHADEDFRAME = "ShadedFrame";
								const string HEXASPHERE_TILESROOT = "TilesRoot";
								const int HEXASPHERE_MAX_PARTS = 100;
								const int MAX_VERTEX_COUNT_PER_CHUNK = 65500;
								const int VERTEX_ARRAY_SIZE = 65530;

								Dictionary<Point, Point> points = new Dictionary<Point, Point> ();
								Dictionary<Point, int> verticesIdx = new Dictionary<Point, int> ();
								List<Vector3>[] verticesWire = new List<Vector3>[HEXASPHERE_MAX_PARTS];
								List<int>[] indicesWire = new List<int>[HEXASPHERE_MAX_PARTS];
								List<Vector2>[] uvWire = new List<Vector2>[HEXASPHERE_MAX_PARTS];
								List<Color32>[] colorWire = new List<Color32>[HEXASPHERE_MAX_PARTS];
								List<Vector3>[] verticesShaded = new List<Vector3>[HEXASPHERE_MAX_PARTS];
								List<int>[] indicesShaded = new List<int>[HEXASPHERE_MAX_PARTS];
								List<Vector4>[] uvShaded = new List<Vector4>[HEXASPHERE_MAX_PARTS];
								List<Color32>[] colorShaded = new List<Color32>[HEXASPHERE_MAX_PARTS];
								const float PHI = 1.61803399f;
								List<Texture2D> texArray = new List<Texture2D> (255);
								Dictionary<Color, Texture2D> solidTexCache = new Dictionary<Color, Texture2D> ();
								Mesh[] shadedMeshes = new Mesh[HEXASPHERE_MAX_PARTS];
								MeshFilter[] shadedMFs = new MeshFilter[HEXASPHERE_MAX_PARTS];
								MeshRenderer[] shadedMRs = new MeshRenderer[HEXASPHERE_MAX_PARTS];
								Mesh[] wiredMeshes = new Mesh[HEXASPHERE_MAX_PARTS];
								MeshFilter[] wiredMFs = new MeshFilter[HEXASPHERE_MAX_PARTS];
								MeshRenderer[] wiredMRs = new MeshRenderer[HEXASPHERE_MAX_PARTS];
								bool[] colorShadedDirty = new bool[HEXASPHERE_MAX_PARTS];
								bool[] uvShadedDirty = new bool[HEXASPHERE_MAX_PARTS];
								bool[] uvWireDirty = new bool[HEXASPHERE_MAX_PARTS];
								bool[] colorWireDirty = new bool[HEXASPHERE_MAX_PARTS];
								[SerializeField] Vector3 oldCameraPosition;

//								Material gridMatExtrusion {
//												get {
//																if (_gridMatExtrusion == null) {
//																				_gridMatExtrusion = new Material (Shader.Find ("Hexasphere/HexaGridExtrusion"));
//																				_gridMatExtrusion.hideFlags = HideFlags.DontSave;
//																}
//																return _gridMatExtrusion;
//												}
//								}
//
//								Material gridMatNoExtrusion {
//												get {
//																if (_gridMatNoExtrusion == null) {
//																				_gridMatNoExtrusion = Instantiate (Resources.Load<Material> ("Materials/HexaGridMatNoExtrusion")) as Material;
//																				_gridMatNoExtrusion.hideFlags = HideFlags.DontSave;
//																}
//																return _gridMatNoExtrusion;
//												}
//								}
//
//								Material tileShadedFrameMatExtrusion {
//												get {
//																if (_tileShadedFrameMatExtrusion == null) {
//																				_tileShadedFrameMatExtrusion = new Material (Shader.Find ("Hexasphere/HexaTileBackgroundExtrusion"));
//																				_tileShadedFrameMatExtrusion.hideFlags = HideFlags.DontSave;
//																}
//																return _tileShadedFrameMatExtrusion;
//												}
//								}
//
//								Material tileShadedFrameMatNoExtrusion {
//												get {
//																if (_tileShadedFrameMatNoExtrusion == null) {
//																				_tileShadedFrameMatNoExtrusion = Instantiate (Resources.Load<Material> ("Materials/HexaTilesBackgroundMatNoExtrusion")) as Material;
//																				_tileShadedFrameMatNoExtrusion.hideFlags = HideFlags.DontSave;
//																}
//																return _tileShadedFrameMatNoExtrusion;
//												}
//								}
//
//								Material tileColoredMat {
//												get {
//																if (_tileColoredMat == null) {
//																				_tileColoredMat = Instantiate (Resources.Load<Material> ("Materials/HexaTilesMat")) as Material;
//																				_tileColoredMat.hideFlags = HideFlags.DontSave;
//																}
//																return _tileColoredMat;
//												}
//								}
//
//								Material tileTexturedMat {
//												get {
//																if (_tileTexturedMat == null) {
//																				_tileTexturedMat = Instantiate (Resources.Load<Material> ("Materials/HexaTilesTexturedMat")) as Material;
//																				_tileTexturedMat.hideFlags = HideFlags.DontSave;
//																}
//																return _tileTexturedMat;
//												}
//								}



								Point GetCachedPoint (Point point) {
												Point thePoint;
												if (points.TryGetValue (point, out thePoint)) {
																return thePoint;
												} else {
																points [point] = point;
																return point;
												}
								}

								/// <summary>
								/// Updates shader properties and generate hexasphere geometry if divisions or style has changed
								/// </summary>
								public void UpdateMaterialProperties () {
												_numDivisions = Mathf.Max (2, _numDivisions);
												_tileTextureSize = Mathf.Max (32, (int)Mathf.Pow (2, (int)Mathf.Log (_tileTextureSize, 2)));
												if (highlightMaterial != null) {
																highlightMaterial.color = _highlightColor;
												}

												// In inverted mode, moves the camera into the center of the sphere
												if (Camera.main != null && currentInvertedMode != _invertedMode) {
																if (_invertedMode) {
																				oldCameraPosition = Camera.main.transform.position;
																				Camera.main.transform.position = transform.position;
																} else {
																				Camera.main.transform.position = oldCameraPosition;
																}
												}

												if (tiles == null || /*currentDivisions != _numDivisions ||*/ currentStyle != _style || currentTextureSize != _tileTextureSize || currentDefautlShadedColor != _defaultShadedColor || currentExtruded != _extruded || _rotationShift != currentRotationShift || _invertedMode != currentInvertedMode) {
																Generate ();
												} else {
																if (currentWireframeColorFromTile != _wireframeColorFromTile) {
																				RebuildWireframe();
																}
																UpdateShadedMaterials ();
												}
												if (_tileShadedFrameMatExtrusion != null) {
																_tileShadedFrameMatExtrusion.SetFloat ("_GradientIntensity", 1f - _gradientIntensity);
																_tileShadedFrameMatExtrusion.SetFloat ("_ExtrusionMultiplier", _extrudeMultiplier);
												}
												if (_gridMatExtrusion != null) {
																_gridMatExtrusion.SetFloat ("_GradientIntensity", 1f - _gradientIntensity);
																_gridMatExtrusion.SetFloat ("_ExtrusionMultiplier", _extrudeMultiplier);
																_gridMatExtrusion.SetColor ("_Color", _wireframeColor * _wireframeIntensity);
												}
												if (_gridMatNoExtrusion != null) {
																_gridMatNoExtrusion.SetColor ("_Color", _wireframeColor);
												}
												sphereCollider.radius = _extruded ? 0.5f * (1.0f + _extrudeMultiplier) : 0.5f;
								}

								/// <summary>
								/// Generate the hexasphere geometry.
								/// </summary>
								public void Generate () {
#if TRACE_PERFORMANCE
			DateTime dt = DateTime.Now;
#endif

												Point[] corners = new Point[] {
																new Point (1, PHI, 0),
																new Point (-1, PHI, 0),
																new Point (1, -PHI, 0),
																new Point (-1, -PHI, 0),
																new Point (0, 1, PHI),
																new Point (0, -1, PHI),
																new Point (0, 1, -PHI),
																new Point (0, -1, -PHI),
																new Point (PHI, 0, 1),
																new Point (-PHI, 0, 1),
																new Point (PHI, 0, -1),
																new Point (-PHI, 0, -1)
												};

												if (_rotationShift != Misc.Vector3zero) {
																Quaternion q = Quaternion.Euler (_rotationShift);
																for (int k = 0; k < corners.Length; k++) {
																				Point c = corners [k];
																				Vector3 v = (Vector3)c;
																				v = q * v;
																				c.x = v.x;
																				c.y = v.y;
																				c.z = v.z;
																}
												}


												Triangle[] triangles = new Triangle[] {
																new Triangle (corners [0], corners [1], corners [4], false),
																new Triangle (corners [1], corners [9], corners [4], false),
																new Triangle (corners [4], corners [9], corners [5], false),
																new Triangle (corners [5], corners [9], corners [3], false),
																new Triangle (corners [2], corners [3], corners [7], false),
																new Triangle (corners [3], corners [2], corners [5], false),
																new Triangle (corners [7], corners [10], corners [2], false),
																new Triangle (corners [0], corners [8], corners [10], false),
																new Triangle (corners [0], corners [4], corners [8], false),
																new Triangle (corners [8], corners [2], corners [10], false),
																new Triangle (corners [8], corners [4], corners [5], false),
																new Triangle (corners [8], corners [5], corners [2], false),
																new Triangle (corners [1], corners [0], corners [6], false),
																new Triangle (corners [11], corners [1], corners [6], false),
																new Triangle (corners [3], corners [9], corners [11], false),
																new Triangle (corners [6], corners [10], corners [7], false),
																new Triangle (corners [3], corners [11], corners [7], false),
																new Triangle (corners [11], corners [6], corners [7], false),
																new Triangle (corners [6], corners [0], corners [10], false),
																new Triangle (corners [9], corners [1], corners [11], false)
												};


												DestroyCachedTiles (false);

												currentDivisions = _numDivisions;
												currentStyle = _style;
												currentExtruded = _extruded;
												currentDefautlShadedColor = _defaultShadedColor;
												currentRotationShift = _rotationShift;
												currentInvertedMode = _invertedMode;

												points.Clear ();

												for (int i = 0; i < corners.Length; i++) {
																points [corners [i]] = corners [i];
												}

#if TRACE_PERFORMANCE
			Debug.Log ("Stage 1 " + DateTime.Now);
#endif

												List<Point> bottom = new List<Point> ();
												int triCount = triangles.Length;
												for (int f = 0; f < triCount; f++) {
																List<Point> prev = null;
																Point point0 = triangles [f].points [0];
																bottom.Clear ();
																bottom.Add (point0);
																List<Point> left = point0.Subdivide (triangles [f].points [1], numDivisions, GetCachedPoint);
																List<Point> right = point0.Subdivide (triangles [f].points [2], numDivisions, GetCachedPoint);
																for (int i = 1; i <= numDivisions; i++) {
																				prev = bottom;
																				bottom = left [i].Subdivide (right [i], i, GetCachedPoint);
																				new Triangle (prev [0], bottom [0], bottom [1]);
																				for (int j = 1; j < i; j++) {
																								new Triangle (prev [j], bottom [j], bottom [j + 1]);
																								new Triangle (prev [j - 1], prev [j], bottom [j]);
																				}
																}
												}

#if TRACE_PERFORMANCE
		Debug.Log ("Stage 2 " + DateTime.Now);
#endif
												int meshPointsCount = points.Values.Count;

#if TRACE_PERFORMANCE
			Debug.Log ("Stage 2.1 " + DateTime.Now);
#endif

#if TRACE_PERFORMANCE
			Debug.Log ("Stage 2.2 " + DateTime.Now);
#endif
												int p = 0;
												Point.flag = 0;
												tiles = new Tile[meshPointsCount];
												foreach (Point point in points.Values) {
																tiles [p] = new Tile (point, p);
																p++;
												}
#if TRACE_PERFORMANCE
			Debug.Log ("Stage 3 " + DateTime.Now);
#endif

												// Destroy placeholders
												Transform t = gameObject.transform.Find (HEXASPHERE_WIREFRAME);
												if (t != null)
																DestroyImmediate (t.gameObject);
												t = gameObject.transform.Find (HEXASPHERE_SHADEDFRAME);
												if (t != null)
																DestroyImmediate (t.gameObject);
												t = gameObject.transform.Find (HEXASPHERE_TILESROOT);
												if (t != null)
																DestroyImmediate (t.gameObject);

												// Create meshes
												if (_style != STYLE.Shaded) {
																BuildWireframe ();
												}
												if (_style != STYLE.Wireframe) {
																BuildTiles ();
												}


#if TRACE_PERFORMANCE
			Debug.Log ("Stage 3.1 " + DateTime.Now);
#endif

												needRefreshRouteMatrix = true;

#if TRACE_PERFORMANCE
			Debug.Log ("Stage 4 " + DateTime.Now);
			Debug.Log ("Time = " + (DateTime.Now - dt).TotalSeconds + " s.");
#endif
								}

								List<T> CheckList<T> (ref List<T> l) {
												if (l == null) {
																l = new List<T> (VERTEX_ARRAY_SIZE);
												} else {
																l.Clear ();
												}
												return l;
								}


								public void RebuildWireframe() {
												Transform t = gameObject.transform.Find (HEXASPHERE_WIREFRAME);
												if (t != null)
																DestroyImmediate (t.gameObject);
												BuildWireframe();
								}

								void BuildWireframe () {

												currentWireframeColorFromTile = _wireframeColorFromTile;

												if (_extruded && !_invertedMode) {
																BuildWireframeExtruded ();
																return;
												}

												int chunkIndex = 0;
												List<Vector3> vertexChunk = CheckList<Vector3> (ref verticesWire [chunkIndex]);
												List<int> indicesChunk = CheckList<int> (ref indicesWire [chunkIndex]);

												int pos;
												int verticesCount = -1;
												verticesIdx.Clear ();
												int tileCount = tiles.Length;
												for (int k = 0; k < tileCount; k++) {
																if (verticesCount > MAX_VERTEX_COUNT_PER_CHUNK) {
																				chunkIndex++;
																				vertexChunk = CheckList<Vector3> (ref verticesWire [chunkIndex]);
																				indicesChunk = CheckList<int> (ref indicesWire [chunkIndex]);
																				verticesIdx.Clear ();
																				verticesCount = -1;
																}
																Tile tile = tiles [k];
																int pos0 = 0;
																Point[] tileVertices = tile.vertexPoints;
																int tileVerticesCount = tileVertices.Length;
																for (int b = 0; b < tileVerticesCount; b++) {
																				Point point = tileVertices [b];
																				if (!verticesIdx.TryGetValue (point, out pos)) {
																								vertexChunk.Add (point.projectedVector3);
																								verticesCount++;
																								pos = verticesCount;
																								verticesIdx [point] = pos;
																				}
																				indicesChunk.Add (pos);
																				if (b == 0) {
																								pos0 = pos;
																				} else {
																								indicesChunk.Add (pos);
																				}
																}
																indicesChunk.Add (pos0);
												}

												GameObject partsRoot = CreateGOandParent (gameObject.transform, HEXASPHERE_WIREFRAME);
												for (int k = 0; k <= chunkIndex; k++) {
																GameObject go = CreateGOandParent (partsRoot.transform, "Wire");
																MeshFilter mf = go.AddComponent<MeshFilter> ();
																wiredMeshes [k] = new Mesh ();
																wiredMeshes [k].hideFlags = HideFlags.DontSave;
																wiredMeshes [k].SetVertices (verticesWire [k]);
																wiredMeshes [k].SetIndices (indicesWire [k].ToArray (), MeshTopology.Lines, 0, false);
																mf.sharedMesh = wiredMeshes [k];
																wiredMFs [k] = mf;
																MeshRenderer mr = go.AddComponent<MeshRenderer> ();
																wiredMRs [k] = mr;
																mr.sharedMaterial = gridMatNoExtrusion;
												}

								}

								void BuildWireframeExtruded () {

												int chunkIndex = 0;
												List<Vector3> vertexChunk = CheckList<Vector3> (ref verticesWire [chunkIndex]);
												List<Vector2> uvChunk = CheckList<Vector2> (ref uvWire [chunkIndex]);
												List<Color32> colorChunk = CheckList<Color32>(ref colorWire[chunkIndex]);
												List<int> indicesChunk = CheckList<int> (ref indicesWire [chunkIndex]);

												int verticesCount = 0;
												int tileCount = tiles.Length;
												Vector2 uvExtrudeData;
												for (int k = 0; k < tileCount; k++) {
																if (verticesCount > MAX_VERTEX_COUNT_PER_CHUNK) {
																				chunkIndex++;
																				vertexChunk = CheckList<Vector3> (ref verticesWire [chunkIndex]);
																				uvChunk = CheckList<Vector2> (ref uvWire [chunkIndex]);
																				colorChunk = CheckList<Color32> (ref colorWire [chunkIndex]);
																				indicesChunk = CheckList<int> (ref indicesWire [chunkIndex]);
																				verticesCount = 0;
																}
																Tile tile = tiles [k];
																int pos0 = verticesCount;
																Point[] tileVertices = tile.vertexPoints;
																int tileVerticesCount = tileVertices.Length;
																uvExtrudeData.x = k;
																uvExtrudeData.y = tile.extrudeAmount;
																tile.uvWireChunkIndex = chunkIndex;
																tile.uvWireChunkStart = verticesCount;
																tile.uvWireChunkLength = tileVerticesCount;
																Color32 tileColor;
																if (_wireframeColorFromTile && tile.customMat!=null) {
																				tileColor = tile.customMat.color;
																} else {
																				tileColor = Misc.Color32White;
																}
																for (int b = 0; b < tileVerticesCount; b++) {
																				Point point = tileVertices [b];
																				vertexChunk.Add (point.projectedVector3);
																				uvChunk.Add (uvExtrudeData);
																				colorChunk.Add(tileColor);
																				indicesChunk.Add (verticesCount);
																				if (b > 0) {
																								indicesChunk.Add (verticesCount);
																				}
																				verticesCount++;
																}
																indicesChunk.Add (pos0);
												}

												GameObject partsRoot = CreateGOandParent (gameObject.transform, HEXASPHERE_WIREFRAME);
												for (int k = 0; k <= chunkIndex; k++) {
																uvWireDirty [k] = false;
																colorWireDirty[k] = false;
																GameObject go = CreateGOandParent (partsRoot.transform, "Wire");
																MeshFilter mf = go.AddComponent<MeshFilter> ();
																wiredMeshes [k] = new Mesh ();
																wiredMeshes [k].hideFlags = HideFlags.DontSave;
																wiredMeshes [k].SetVertices (verticesWire [k]);
																wiredMeshes [k].SetUVs (0, uvWire [k]);
																wiredMeshes [k].SetColors(colorWire[k]);
																wiredMeshes [k].SetIndices (indicesWire [k].ToArray (), MeshTopology.Lines, 0, false);
																mf.sharedMesh = wiredMeshes [k];
																wiredMFs [k] = mf;
																MeshRenderer mr = go.AddComponent<MeshRenderer> ();
																mr.sharedMaterial = gridMatExtrusion;
																wiredMRs [k] = mr;
												}
												wireChunkCount = chunkIndex + 1;
								}

								void UpdateWireMaterialsFast () {
												if (_style == STYLE.Shaded || !_extruded || _invertedMode)
																return;

												for (int k = 0; k < wireChunkCount; k++) {
																if (uvWireDirty [k]) {
																				uvWireDirty [k] = false;
																				wiredMeshes [k].SetUVs (0, uvWire [k]);
																}
																if (colorWireDirty [k]) {
																				colorWireDirty [k] = false;
																				wiredMeshes [k].SetColors (colorWire [k]);
																}
												}
								}

								void BuildTiles () {

												int chunkIndex = 0;
												List<Vector3> vertexChunk = CheckList<Vector3> (ref verticesShaded [chunkIndex]);
												List<int> indexChunk = CheckList<int> (ref indicesShaded [chunkIndex]);

												int verticesCount = 0;
												int tileCount = tiles.Length;
												int[] hexIndices, pentIndices;
												if (_invertedMode) {
																hexIndices = hexagonIndicesInverted;
																pentIndices = pentagonIndicesInverted;
												} else {
																if (_extruded) {
																				hexIndices = hexagonIndicesExtruded;
																				pentIndices = pentagonIndicesExtruded;
																} else {
																				hexIndices = hexagonIndices;
																				pentIndices = pentagonIndices;
																}
												}
												for (int k = 0; k < tileCount; k++) {
																if (verticesCount > MAX_VERTEX_COUNT_PER_CHUNK) {
																				chunkIndex++;
																				vertexChunk = CheckList<Vector3> (ref verticesShaded [chunkIndex]);
																				indexChunk = CheckList<int> (ref indicesShaded [chunkIndex]);
																				verticesCount = 0;
																}
																Tile tile = tiles [k];
																Point[] tileVertices = tile.vertexPoints;
																int tileVerticesCount = tileVertices.Length;
																for (int b = 0; b < tileVerticesCount; b++) {
																				Point point = tileVertices [b];
																				vertexChunk.Add (point.projectedVector3);
																}
																int[] indicesList;
																if (tileVerticesCount == 6) {
																				if (_extruded) {
																								vertexChunk.Add ((tileVertices [1].projectedVector3 + tileVertices [5].projectedVector3) * 0.5f);
																								vertexChunk.Add ((tileVertices [2].projectedVector3 + tileVertices [4].projectedVector3) * 0.5f);
																								tileVerticesCount += 2;
																				}
																				indicesList = hexIndices;
																} else {
																				if (_extruded) {
																								vertexChunk.Add ((tileVertices [1].projectedVector3 + tileVertices [4].projectedVector3) * 0.5f);
																								vertexChunk.Add ((tileVertices [2].projectedVector3 + tileVertices [4].projectedVector3) * 0.5f);
																								tileVerticesCount += 2;
																				}
																				indicesList = pentIndices;
																}
																for (int b = 0; b < indicesList.Length; b++) {
																				indexChunk.Add (verticesCount + indicesList [b]);
																}
																verticesCount += tileVerticesCount;
												}

												Material tileShadedFrameMat = _extruded ? tileShadedFrameMatExtrusion : tileShadedFrameMatNoExtrusion;
												GameObject partsRoot = CreateGOandParent (gameObject.transform, HEXASPHERE_SHADEDFRAME);
												for (int k = 0; k <= chunkIndex; k++) {
																GameObject go = CreateGOandParent (partsRoot.transform, "Shade");
																MeshFilter mf = go.AddComponent<MeshFilter> ();
																shadedMFs [k] = mf;
																if (shadedMeshes [k] == null) {
																				shadedMeshes [k] = new Mesh ();
																				shadedMeshes [k].hideFlags = HideFlags.DontSave;
																}
																shadedMeshes [k].Clear ();
																shadedMeshes [k].SetVertices (verticesShaded [k]);
																shadedMeshes [k].SetIndices (indicesShaded [k].ToArray (), MeshTopology.Triangles, 0, false);
																mf.sharedMesh = shadedMeshes [k];
																MeshRenderer mr = go.AddComponent<MeshRenderer> ();
																shadedMRs [k] = mr;
																mr.sharedMaterial = tileShadedFrameMat;
												}

												BuildShadedMaterials ();
								}

								void BuildShadedMaterials () {

												if (tiles == null || _style == STYLE.Wireframe)
																return;

												int chunkIndex = 0;
												List<Vector4> uvChunk = CheckList<Vector4> (ref uvShaded [chunkIndex]);
												List<Color32> colorChunk = CheckList<Color32> (ref colorShaded [chunkIndex]);
												Material tileShadedFrameMat = _extruded ? tileShadedFrameMatExtrusion : tileShadedFrameMatNoExtrusion;
												if (whiteTex == null)
																whiteTex = GetCachedSolidTex (Color.white);
												texArray.Clear ();
												texArray.Add (whiteTex);

												float cosTheta = 0;
												float sinTheta = 0;

												int verticesCount = 0;
												int tileCount = tiles.Length;
												for (int k = 0; k < tileCount; k++) {
																if (verticesCount > MAX_VERTEX_COUNT_PER_CHUNK) {
																				chunkIndex++;
																				uvChunk = CheckList<Vector4> (ref uvShaded [chunkIndex]);
																				colorChunk = CheckList<Color32> (ref colorShaded [chunkIndex]);
																				verticesCount = 0;
																}
																Tile tile = tiles [k];
																Point[] tileVertices = tile.vertexPoints;
																int tileVerticesCount = tileVertices.Length;
																Vector2[] uvList;
																if (tileVerticesCount == 6) {
																				if (currentExtruded) {
																								uvList = hexagonUVsExtruded;
																				} else {
																								uvList = _invertedMode ? hexagonUVsInverted : hexagonUVs;
																				}
																} else {
																				if (currentExtruded) {
																								uvList = pentagonUVsExtruded;
																				} else {
																								uvList = _invertedMode ? pentagonUVsInverted : pentagonUVs;
																				}
																}
																// Put tile color or texture into tex array
																Texture2D tileTexture;
																int textureIndex = 0;
																if (tile.customMat && tile.customMat.mainTexture != null) {
																				tileTexture = (Texture2D)tile.customMat.mainTexture;
																				textureIndex = texArray.IndexOf (tileTexture);
																} else {
																				tileTexture = whiteTex;
																}
																if (textureIndex < 0) {
																				texArray.Add (tileTexture);
																				textureIndex = texArray.Count - 1;
																}
																Color color = tile.customMat != null ? tile.customMat.color : _defaultShadedColor;
																Vector4 uv4;
																tile.uvShadedChunkStart = verticesCount;
																tile.uvShadedChunkIndex = chunkIndex;
																tile.uvShadedChunkLength = uvList.Length;

																if (tile.rotation != 0) {
																				cosTheta = Mathf.Cos (tile.rotation);
																				sinTheta = Mathf.Sin (tile.rotation);
																}
																for (int b = 0; b < uvList.Length; b++) {
																				Vector2 uv = uvList [b];
																				float x = uv.x;
																				float y = uv.y;
																				if (tile.rotation != 0) {
																								RotateUV (ref x, ref y, cosTheta, sinTheta);
																				}
																				uv4.x = x;
																				uv4.y = y;
																				uv4.z = textureIndex;
																				uv4.w = tile.extrudeAmount;
																				uvChunk.Add (uv4);
																				colorChunk.Add (color);
																}
																verticesCount += uvList.Length;
												}

												for (int k = 0; k <= chunkIndex; k++) {
																uvShadedDirty [k] = false;
																colorShadedDirty [k] = false;
																shadedMeshes [k].SetUVs (0, uvShaded [k]);
																shadedMeshes [k].SetColors (colorShaded [k]);
																shadedMFs [k].sharedMesh = shadedMeshes [k];
																shadedMRs [k].sharedMaterial = tileShadedFrameMat;
												}

												// Build texture array
												if (allowedTextureArray) {
																int texArrayCount = texArray.Count;
																currentTextureSize = _tileTextureSize;
																Texture2DArray finalTexArray = new Texture2DArray (_tileTextureSize, _tileTextureSize, texArrayCount, TextureFormat.ARGB32, true);
																for (int k = 0; k < texArrayCount; k++) {
																				if (texArray [k].width != _tileTextureSize || texArray [k].height != _tileTextureSize) {
																								texArray [k] = Instantiate (texArray [k]) as Texture2D;
																								texArray [k].hideFlags = HideFlags.DontSave;
																								TextureScaler.Scale (texArray [k], _tileTextureSize, _tileTextureSize, FilterMode.Trilinear);
																				}
																				finalTexArray.SetPixels32 (texArray [k].GetPixels32 (), k);
																}
																finalTexArray.Apply (true, true);
																tileShadedFrameMat.SetTexture ("_MainTex", finalTexArray);
												}

												pendingTextureArrayUpdate = false;
												pendingColorsUpdate = false;
												pendingUVUpdateFast = false;
												uvChunkCount = chunkIndex + 1;
								}

								void UpdateShadedMaterials () {

												if (tiles == null || _style == STYLE.Wireframe)
																return;

												int chunkIndex = 0;
												List<Vector4> uvChunk = uvShaded [chunkIndex];
												List<Color32> colorChunk = colorShaded [chunkIndex];
												Material tileShadedFrameMat = _extruded ? tileShadedFrameMatExtrusion : tileShadedFrameMatNoExtrusion;
												if (whiteTex == null)
																whiteTex = GetCachedSolidTex (Color.white);
												texArray.Clear ();
												texArray.Add (whiteTex);

												float cosTheta = 0;
												float sinTheta = 0;

												int verticesCount = 0;
												int tileCount = tiles.Length;
												Color color = _defaultShadedColor;
												for (int k = 0; k < tileCount; k++) {
																if (verticesCount > MAX_VERTEX_COUNT_PER_CHUNK) {
																				chunkIndex++;
																				uvChunk = uvShaded [chunkIndex];
																				colorChunk = colorShaded [chunkIndex];
																				verticesCount = 0;
																}
																Tile tile = tiles [k];
																Point[] tileVertices = tile.vertexPoints;
																int tileVerticesCount = tileVertices.Length;
																Vector2[] uvList;
																if (tileVerticesCount == 6) {
																				if (currentExtruded) {
																								uvList = hexagonUVsExtruded;
																				} else {
																								uvList = _invertedMode ? hexagonUVsInverted : hexagonUVs;
																				}
																} else {
																				if (currentExtruded) {
																								uvList = pentagonUVsExtruded;
																				} else {
																								uvList = _invertedMode ? pentagonUVsInverted : pentagonUVs;
																				}
																}
																// Put tile color or texture into tex array
																Texture2D tileTexture;
																int textureIndex = 0;
																if (tile.customMat && tile.customMat.mainTexture) {
																				tileTexture = (Texture2D)tile.customMat.mainTexture;
																				textureIndex = texArray.IndexOf (tileTexture);
																} else {
																				tileTexture = whiteTex;
																}
																if (textureIndex < 0) {
																				texArray.Add (tileTexture);
																				textureIndex = texArray.Count - 1;
																}
																if (pendingColorsUpdate) {
																				color = tile.customMat ? tile.customMat.color : _defaultShadedColor;
																}
																if (tile.rotation != 0) {
																				cosTheta = Mathf.Cos (tile.rotation);
																				sinTheta = Mathf.Sin (tile.rotation);
																}
																for (int b = 0; b < uvList.Length; b++) {
																				Vector2 uv = uvList [b];
																				float x = uv.x;
																				float y = uv.y;
																				if (tile.rotation != 0) {
																								RotateUV (ref x, ref y, cosTheta, sinTheta);
																				}
																				Vector4 uv4;
																				uv4.x = x;
																				uv4.y = y;
																				uv4.z = textureIndex;
																				uv4.w = tile.extrudeAmount;
																				uvChunk [verticesCount] = uv4;
																				if (pendingColorsUpdate) {
																								colorChunk [verticesCount] = color;
																				}
																				verticesCount++;
																}
												}

												for (int k = 0; k <= chunkIndex; k++) {
																uvShadedDirty [k] = false;
																shadedMeshes [k].SetUVs (0, uvShaded [k]);
																if (pendingColorsUpdate) {
																				colorShadedDirty [k] = false;
																				shadedMeshes [k].SetColors (colorShaded [k]);
																}
																shadedMFs [k].sharedMesh = shadedMeshes [k];
																shadedMRs [k].sharedMaterial = tileShadedFrameMat;
												}

												if (pendingTextureArrayUpdate && allowedTextureArray) {
																// Build texture array
																int texArrayCount = texArray.Count;
																currentTextureSize = _tileTextureSize;
																Texture2DArray finalTexArray = new Texture2DArray (_tileTextureSize, _tileTextureSize, texArrayCount, TextureFormat.ARGB32, true);
																for (int k = 0; k < texArrayCount; k++) {
																				if (texArray [k].width != _tileTextureSize || texArray [k].height != _tileTextureSize) {
																								texArray [k] = Instantiate (texArray [k]) as Texture2D;
																								texArray [k].hideFlags = HideFlags.DontSave;
																								TextureScaler.Scale (texArray [k], _tileTextureSize, _tileTextureSize, FilterMode.Trilinear);
																				}
																				finalTexArray.SetPixels32 (texArray [k].GetPixels32 (), k);
																}
																finalTexArray.Apply (true, true);
																tileShadedFrameMat.SetTexture ("_MainTex", finalTexArray);
																pendingTextureArrayUpdate = false;
												}
												pendingColorsUpdate = false;
								}

								void RotateUV (ref float x, ref float y, float cosTheta, float sinTheta) {
												x -= 0.5f;
												y -= 0.5f;
												float x1 = cosTheta * x - sinTheta * y;
												float y1 = sinTheta * x + cosTheta * y;
												x = x1 + 0.5f;
												y = y1 + 0.5f;
								}


								void UpdateShadedMaterialsFast () {

												if (_style == STYLE.Wireframe)
																return;

												if (pendingColorsUpdate) {
																for (int k = 0; k < uvChunkCount; k++) {
																				if (colorShadedDirty [k]) {
																								colorShadedDirty [k] = false;
																								shadedMeshes [k].SetColors (colorShaded [k]);
																				}
																}
																pendingColorsUpdate = false;
												}

												if (pendingUVUpdateFast) {
																for (int k = 0; k < uvChunkCount; k++) {
																				if (uvShadedDirty [k]) {
																								uvShadedDirty [k] = false;
																								shadedMeshes [k].SetUVs (0, uvShaded [k]);
																				}
																}
												}
								}

								Texture2D GetCachedSolidTex (Color color) {
												Texture2D tex;
												if (solidTexCache.TryGetValue (color, out tex)) {
																return tex;
												} else {
																tex = new Texture2D (_tileTextureSize, _tileTextureSize, TextureFormat.ARGB32, true);
																tex.hideFlags = HideFlags.DontSave;
																int l = tex.width * tex.height;
																Color32[] colors32 = new Color32[l];
																Color32 color32 = color;
																for (int k = 0; k < l; k++) {
																				colors32 [k] = color32;
																}
																tex.SetPixels32 (colors32);
																tex.Apply ();
																solidTexCache [color] = tex;
																return tex;
												}
								}



								GameObject CreateGOandParent (Transform parent, string name) {
												GameObject go = new GameObject (name);
												go.transform.SetParent (parent, false);
												go.transform.localPosition = Misc.Vector3zero;
												go.transform.localScale = Vector3.one;
												go.transform.localRotation = Quaternion.Euler (0, 0, 0);
												return go;
								}

								#endregion

								#region Tile functions

								Transform tilesRoot;
								int[] hexagonIndices = new int[] {
												0, 1, 5,
												1, 2, 5,
												4, 5, 2,
												3, 4, 2
								};
								Vector2[] hexagonUVs = new Vector2[] {
												new Vector2 (0, 0.5f),
												new Vector2 (0.25f, 1f),
												new Vector2 (0.75f, 1f),
												new Vector2 (1f, 0.5f),
												new Vector2 (0.75f, 0f),
												new Vector2 (0.25f, 0f)
								};
								Vector2[] hexagonUVsInverted = new Vector2[] {	// same but y' = 1 - y
												new Vector2 (0, 0.5f),
												new Vector2 (0.25f, 0f),
												new Vector2 (0.75f, 0f),
												new Vector2 (1f, 0.5f),
												new Vector2 (0.75f, 1f),
												new Vector2 (0.25f, 1f)
								};
								int[] hexagonIndicesExtruded = new int[] {
												0, 1, 6,
												5, 0, 6,
												1, 2, 5,
												4, 5, 2,
												2, 3, 7,
												3, 4, 7
								};
								Vector2[] hexagonUVsExtruded = new Vector2[] {
												new Vector2 (0, 0.5f),
												new Vector2 (0.25f, 1f),
												new Vector2 (0.75f, 1f),
												new Vector2 (1f, 0.5f),
												new Vector2 (0.75f, 0f),
												new Vector2 (0.25f, 0f),
												new Vector2 (0.25f, 0.5f),
												new Vector2 (0.75f, 0.5f)
								};
								int[] hexagonIndicesInverted = new int[] {
												0, 5, 1,
												1, 5, 2,
												4, 2, 5,
												3, 2, 4
								};

								int[] pentagonIndices = new int[] {
												0, 1, 4,
												1, 2, 4,
												3, 4, 2
								};
								Vector2[] pentagonUVs = new Vector2[] {
												new Vector2 (0, 0.33f),
												new Vector2 (0.25f, 1f),
												new Vector2 (0.75f, 1f),
												new Vector2 (1f, 0.33f),
												new Vector2 (0.5f, 0f),
								};
								Vector2[] pentagonUVsInverted = new Vector2[] { // same but y' = 1 - y
												new Vector2 (0f, 0.66f),
												new Vector2 (0.25f, 0f),
												new Vector2 (0.75f, 0f),
												new Vector2 (1f, 0.66f),
												new Vector2 (0.5f, 1f),
								};
								int[] pentagonIndicesExtruded = new int[] {
												0, 1, 5,
												4, 0, 5,
												1, 2, 4,
												2, 3, 6,
												3, 4, 6
								};
								Vector2[] pentagonUVsExtruded = new Vector2[] {
												new Vector2 (0, 0.33f),
												new Vector2 (0.25f, 1f),
												new Vector2 (0.75f, 1f),
												new Vector2 (1f, 0.33f),
												new Vector2 (0.5f, 0f),
												new Vector2 (0.375f, 0.5f),
												new Vector2 (0.625f, 0.5f)

								};
								int[] pentagonIndicesInverted = new int[] {
												0, 4, 1,
												1, 4, 2,
												3, 2, 4
								};
								Dictionary<Color, Material> colorCache = new Dictionary<Color, Material> ();
								Dictionary<Texture2D, Material> textureCache = new Dictionary<Texture2D, Material> ();

								void GenerateTileMesh (int tileIndex, Material mat) {
												if (tilesRoot == null) {
																tilesRoot = CreateGOandParent (gameObject.transform, HEXASPHERE_TILESROOT).transform;
												}
												GameObject go = CreateGOandParent (tilesRoot, "Tile");
												MeshFilter mf = go.AddComponent<MeshFilter> ();
												Mesh mesh = new Mesh ();
												mesh.hideFlags = HideFlags.DontSave;
												Tile tile = tiles [tileIndex];
												if (_extruded) {
																Vector3[] tileVertices = tile.vertices;
																Vector3[] extrudedVertices = new Vector3[tileVertices.Length];
																for (int k = 0; k < tileVertices.Length; k++) {
																				extrudedVertices [k] = tileVertices [k] * (1f + tile.extrudeAmount * _extrudeMultiplier);
																}
																mesh.vertices = extrudedVertices;
												} else {
																mesh.vertices = tile.vertices;
												}
												int tileVerticesCount = tile.vertices.Length;
												if (tileVerticesCount == 6) {
																mesh.SetIndices (_invertedMode ? hexagonIndicesInverted : hexagonIndices, MeshTopology.Triangles, 0, false);
																mesh.uv = _invertedMode ? hexagonUVsInverted : hexagonUVs;
												} else {
																mesh.SetIndices (_invertedMode ? pentagonIndicesInverted : pentagonIndices, MeshTopology.Triangles, 0, false);
																mesh.uv = _invertedMode ? pentagonUVsInverted : pentagonUVs;
												}
												mf.sharedMesh = mesh;
												MeshRenderer mr = go.AddComponent<MeshRenderer> ();
												mr.sharedMaterial = mat;
												tile.renderer = mr;
								}

								Material GetCachedMaterial (Color color, Texture2D texture = null) {
												Material mat;
												if (texture == null) {
																if (colorCache.TryGetValue (color, out mat)) {
																				return mat;
																}
																mat = Instantiate (tileColoredMat) as Material;
																colorCache [color] = mat;
												} else {
																if (textureCache.TryGetValue (texture, out mat)) {
																				return mat;
																}
																mat = Instantiate (tileTexturedMat) as Material;
																mat.mainTexture = texture;
																textureCache [texture] = mat;
												}
												mat.hideFlags = HideFlags.DontSave;
												this.SetMaterialColor(mat, color);
												return mat;
								}

								void SetMaterialColor(Material mat, Color color) {
									mat.SetColor("_Color", color);
									mat.SetColor("_Tint", color);
									//mat.color = color;
								}

								Color GetMaterialColor(Material mat) {
									return mat.color;
								}

								void HideHighlightedTile () {
												if (lastHighlightedTileIndex >= 0 && lastHighlightedTile != null && lastHighlightedTile.renderer != null && lastHighlightedTile.renderer.sharedMaterial == highlightMaterial) {
																if (lastHighlightedTile.tempMat != null) {
																				TileRestoreTemporaryMaterial (lastHighlightedTileIndex);
																} else {
																				HideTile (lastHighlightedTileIndex);
																}
												}
												ResetHighlightMaterial ();
												lastHighlightedTile = null;
												lastHighlightedTileIndex = -1;
								}


								void TileRestoreTemporaryMaterial (int tileIndex) {
												if (tileIndex < 0 || tileIndex >= tiles.Length)
																return;
												Tile tile = tiles [tileIndex];
												if (tile.tempMat != null) {
																tile.renderer.sharedMaterial = tile.tempMat;
												}
								}

								void ResetHighlightMaterial () {
												if (highlightMaterial != null) {
																Color co = highlightMaterial.color;
																co.a = 0.2f;
																highlightMaterial.SetColor ("_Color2", co);
																highlightMaterial.mainTexture = null;
												}
								}

								void RefreshHighlightedTile () {
												if (lastHighlightedTileIndex < 0 || lastHighlightedTileIndex >= tiles.Length)
																return;
												SetTileMaterial (lastHighlightedTileIndex, highlightMaterial, true);
								}


								void DestroyCachedTiles (bool preserveMaterials) {
												if (tiles == null)
																return;

												HideHighlightedTile ();
												for (int k = 0; k < tiles.Length; k++) {
																Tile tile = tiles [k];
																if (tile.renderer != null) {
																				DestroyImmediate (tile.renderer.gameObject);
																				tile.renderer = null;
																				if (!preserveMaterials) {
																								tile.customMat = null;
																								tile.tempMat = null;
																				}
																}
												}

								}

								Tile GetNearestTileToPosition (Tile[] tiles, Vector3 localPosition, out float distance) {
												distance = float.MaxValue;
												Tile nearest = null;
												for (int k = 0; k < tiles.Length; k++) {
																Tile tile = tiles [k];
																Vector3 center = tile.center;
																// unwrapped SqrMagnitude for performance considerations
																float dist = (center.x - localPosition.x) * (center.x - localPosition.x) + (center.y - localPosition.y) * (center.y - localPosition.y) + (center.z - localPosition.z) * (center.z - localPosition.z);
																if (dist < distance) {
																				nearest = tile;
																				distance = dist;
																}
												}
												return nearest;
								}

								Tile GetNeighbourIfNearestToPosition (Tile tile, Vector3 localPosition) {
												float minDist =
																(tile.center.x - localPosition.x) * (tile.center.x - localPosition.x) +
																(tile.center.y - localPosition.y) * (tile.center.y - localPosition.y) +
																(tile.center.z - localPosition.z) * (tile.center.z - localPosition.z);
												Tile nearest = tile;
												Tile[] tiles = tile.neighbours;
												if (tiles == null)
																return null;
												for (int k = 0; k < tiles.Length; k++) {
																tile = tiles [k];
																Vector3 center = tile.center;
																// unwrapped SqrMagnitude for performance considerations
																float dist =
																				(center.x - localPosition.x) * (center.x - localPosition.x) +
																				(center.y - localPosition.y) * (center.y - localPosition.y) +
																				(center.z - localPosition.z) * (center.z - localPosition.z);
																if (dist < minDist) {
																				nearest = tile;
																				minDist = dist;
																}
												}
												return nearest;
								}

								int GetTileAtLocalPosition (Vector3 localPosition, bool reuseLastHit) {

												if (tiles == null)
																return -1;

												// If this the same tile? Heuristic: any neighour will be farther
												if (lastHitTileIndex >= 0 && lastHitTileIndex < tiles.Length) {
																Tile lastHitTile = tiles [lastHitTileIndex];
																if (lastHitTile != null) {
																				float dist = Vector3.SqrMagnitude (lastHitTile.center - localPosition);
																				bool valid = true;
																				for (int k = 0; k < lastHitTile.neighbours.Length; k++) {
																								float otherDist = Vector3.SqrMagnitude (lastHitTile.neighbours [k].center - localPosition);
																								if (otherDist < dist) {
																												valid = false;
																												break;
																								}
																				}
																				if (valid) {
																								return lastHitTileIndex;
																				}
																}
												} else {
																lastHitTileIndex = 0;
												}

												// follow the shortest path to the minimum distance
												Tile nearest = tiles [lastHitTileIndex];
												float tileDist =
																(nearest.center.x - localPosition.x) * (nearest.center.x - localPosition.x) +
																(nearest.center.y - localPosition.y) * (nearest.center.y - localPosition.y) +
																(nearest.center.z - localPosition.z) * (nearest.center.z - localPosition.z);
												float minDist = 1e6f;
												for (int k = 0; k < tiles.Length; k++) {
																Tile newNearest = GetNearestTileToPosition (nearest.neighbours, localPosition, out tileDist);
																if (tileDist < minDist) {
																				minDist = tileDist;
																				nearest = newNearest;
																} else {
																				break;
																}
												}
												lastHitTileIndex = nearest.index;
												return lastHitTileIndex;
								}

								#if RAYCAST3D_DEBUG
								bool rayDebug;
								void PutBall (Vector3 pos, Color color) {
												GameObject obj = GameObject.CreatePrimitive (PrimitiveType.Sphere);
												obj.transform.position = pos;
												obj.transform.localScale = Vector3.one * 0.1f;
												obj.GetComponent<Renderer> ().material.color = color;
								}
								#endif

								int GetTileInRayDirection (Ray ray, Vector3 worldPosition) {
												if (tiles == null)
																return -1;

												// Compute final point
												Vector3 minPoint = worldPosition;
												Vector3 maxPoint = worldPosition + ray.direction * transform.localScale.x * 0.5f;
												float rangeMin = transform.localScale.x * 0.5f;
												rangeMin *= rangeMin;
												float rangeMax = (worldPosition - transform.position).sqrMagnitude;
												float dist;
												Vector3 bestPoint = maxPoint;
												for (int k = 0; k < 10; k++) {
																Vector3 midPoint = (minPoint + maxPoint) * 0.5f;
																dist = (midPoint - transform.position).sqrMagnitude;
																if (dist < rangeMin) {
																				maxPoint = midPoint;
																				bestPoint = midPoint;
																} else if (dist > rangeMax) {
																				maxPoint = midPoint;
																} else {
																				minPoint = midPoint;
																}
												}

												// Get tile at first hit
												int nearest = GetTileAtLocalPosition (transform.InverseTransformPoint (worldPosition), true);
												if (nearest < 0)
																return -1;

												Vector3 currPoint = worldPosition;
												Tile tile = tiles [nearest];
												Vector3 tileTop = transform.TransformPoint (tile.center * (1.0f + tile.extrudeAmount * _extrudeMultiplier));
												float tileHeight = (tileTop - transform.position).sqrMagnitude;
												float rayHeight = (currPoint - transform.position).sqrMagnitude;
												float minDist = 1e6f;
												dist = minDist;
												const int NUM_STEPS = 10;
												int candidate = -1;
												for (int k = 1; k <= NUM_STEPS; k++) {
																dist = Mathf.Abs (rayHeight - tileHeight);
																if (dist < minDist) {
																				minDist = dist;
																				candidate = nearest;

																}
																if (rayHeight < tileHeight) {
#if RAYCAST3D_DEBUG
																				rayDebug = false;
#endif
																				return candidate;
																}
																float t = k / (float)NUM_STEPS;
																currPoint = worldPosition * (1f - t) + bestPoint * t;
#if RAYCAST3D_DEBUG
																if (rayDebug)
																				PutBall (currPoint, Color.red);
#endif

																nearest = GetTileAtLocalPosition (transform.InverseTransformPoint (currPoint), true); //( GetNearestTileToPosition (tile, transform.InverseTransformPoint (currPoint));
																if (nearest < 0)
																				break;
																tile = tiles [nearest];
																tileTop = transform.TransformPoint (tile.center * (1.0f + tile.extrudeAmount * _extrudeMultiplier));
#if RAYCAST3D_DEBUG
																if (rayDebug)
																				PutBall (tileTop, Color.blue);
#endif
																tileHeight = (tileTop - transform.position).sqrMagnitude;
																rayHeight = (currPoint - transform.position).sqrMagnitude;
												}

#if RAYCAST3D_DEBUG
												rayDebug = false;
#endif
												if (dist < minDist) {
																minDist = dist;
																candidate = nearest;

												}
												if (rayHeight < tileHeight) {
																return candidate;
												} else {
																return -1;
												}
								}


								#endregion

								#region Editor integration

								#if UNITY_EDITOR

								[MenuItem ("GameObject/3D Object/Hexasphere", false)]
								static void CreateHexasphereMenuOption (MenuCommand menuCommand) {
												// Create a custom game object
												GameObject go = new GameObject ("Hexasphere");
												go.name = "Hexasphere";
												Undo.RegisterCreatedObjectUndo (go, "Create " + go.name);
												if (Selection.activeTransform != null) {
																go.transform.SetParent (Selection.activeTransform, false);
																go.transform.localPosition = Misc.Vector3zero;
												}
												go.transform.localRotation = Quaternion.Euler (0, 0, 0);
												go.transform.localScale = new Vector3 (1f, 1f, 1f);
												Selection.activeObject = go;
												go.AddComponent<Hexasphere> ();
								}


								#endif

								#region Raycasting functions - separated here so they can be modified to fit other purposes

								Transform GVR_Reticle;
								bool GVR_TouchStarted;
								LineRenderer SVR_Laser;
								float lastTimeCheckVRPointers;

								void RegisterVRPointers () {
												if (Time.time - lastTimeCheckVRPointers < 1f)
																return;
												lastTimeCheckVRPointers = Time.time;

												#if VR_GOOGLE
												GameObject obj = GameObject.Find ("GvrControllerPointer");
												if (obj != null) {
												Transform t = obj.transform.FindChild ("Laser");
												if (t != null) {
												GVR_Reticle = t.FindChild ("Reticle");
												}
												}
												#elif VR_SAMSUNG_GEAR_CONTROLLER
												GameObject obj = GameObject.Find ("GearVrController");
												if (obj != null) {
												Transform t = obj.transform.FindChild ("Model/Laser");
												if (t != null) {
												SVR_Laser = t.gameObject.GetComponent<LineRenderer>();
												}
												}
												#endif
								}

								bool GetHitPoint (out Vector3 position, out Ray ray) {
												RaycastHit hit;
												ray = GetRay ();
												if (_invertedMode) {
																if (Physics.Raycast (ray.origin + ray.direction * transform.localScale.z, -ray.direction, out hit, transform.localScale.z)) {
																				if (hit.collider.gameObject == gameObject) {
																								position = hit.point;
																								return true;
																				}
																}
												} else {
																if (Physics.Raycast (ray, out hit)) {
																				if (hit.collider.gameObject == gameObject) {
																								position = hit.point;
																								return true;
																				}
																}
												}

												position = Misc.Vector3zero;
												return false;
								}

								Ray GetRay () {
												Ray ray;

												if (useEditorRay && !Application.isPlaying) {
																return editorRay;
												}

												if (_VREnabled) {
																#if VR_GOOGLE
																if (GVR_Reticle != null && GVR_Reticle.gameObject.activeInHierarchy) {
																Vector3 screenPoint = Camera.main.WorldToScreenPoint (GVR_Reticle.position);
																ray = Camera.main.ScreenPointToRay (screenPoint);
																} else {
																RegisterVRPointers();
																ray = new Ray (Camera.main.transform.position, GvrController.Orientation * Vector3.forward);
																}
																#elif VR_SAMSUNG_GEAR_CONTROLLER && UNITY_5_5_OR_NEWER
																if (SVR_Laser != null && SVR_Laser.gameObject.activeInHierarchy) {
																Vector3 endPos = SVR_Laser.GetPosition(1);
																if (!SVR_Laser.useWorldSpace) endPos = SVR_Laser.transform.TransformPoint(endPos);
																Vector3 screenPoint = Camera.main.WorldToScreenPoint (endPos);
																ray = Camera.main.ScreenPointToRay (screenPoint);
																} else {
																RegisterVRPointers();
																ray = new Ray (Camera.main.transform.position, Camera.main.transform.rotation * Vector3.forward);
																}
																#else
																ray = new Ray (Camera.main.transform.position, Camera.main.transform.forward);
																#endif
												} else {
																Vector3 mousePos = Input.mousePosition;
																ray = Camera.main.ScreenPointToRay (mousePos);
												}
												return ray;
								}

								#endregion


								#endregion

				}

}