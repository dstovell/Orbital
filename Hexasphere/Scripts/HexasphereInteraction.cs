using UnityEngine;
using System.Collections;

namespace HexasphereGrid {

				public delegate void TileEvent (int tileIndex);


				partial class Hexasphere : MonoBehaviour {


								/// <summary>
								/// Fired when path finding algorithmn evaluates a cell. Return the increased cost for cell.
								/// </summary>
								public event TileEvent OnTileClick;

								/// <summary>
								/// Fired when cursor is on a tile
								/// </summary>
								public event TileEvent OnTileMouseOver;


								[SerializeField]
								bool _rotationEnabled = true;

								public bool rotationEnabled {
												get { return _rotationEnabled; }
												set {
																if (_rotationEnabled != value) {
																				_rotationEnabled = value;
																}
												}
								}

								[SerializeField]
								[Range (0.01f, 1f)]
								float _rotationSpeed = 1f;

								public float rotationSpeed {
												get { return _rotationSpeed; }
												set {
																if (_rotationSpeed != value) {
																				_rotationSpeed = value;
																}
												}
								}


								[SerializeField]
								bool _zoomEnabled = true;

								public bool zoomEnabled {
												get { return _zoomEnabled; }
												set {
																if (_zoomEnabled != value) {
																				_zoomEnabled = value;
																}
												}
								}

								[SerializeField]
								[Range (0.1f, 5f)]
								float _zoomSpeed = 1f;

								public float zoomSpeed {
												get { return _zoomSpeed; }
												set {
																if (_zoomSpeed != value) {
																				_zoomSpeed = value;
																}
												}
								}

								[SerializeField]
								[Range (0f, 1f)]
								float _zoomDamping = 0.6f;

								public float zoomDamping {
												get { return _zoomDamping; }
												set {
																if (_zoomDamping != value) {
																				_zoomDamping = value;
																}
												}
								}

								[SerializeField]
								float _zoomMinDistance = 0.1f;

								public float zoomMinDistance {
												get { return _zoomMinDistance; }
												set {
																if (_zoomMinDistance != value) {
																				_zoomMinDistance = value;
																}
												}
								}

								[SerializeField]
								float _zoomMaxDistance = 2f;

								public float zoomMaxDistance {
												get { return _zoomMaxDistance; }
												set {
																if (_zoomMaxDistance != value) {
																				_zoomMaxDistance = value;
																}
												}
								}


								[SerializeField]
								Color _highlightColor = new Color (0, 0.25f, 1f, 0.8f);

								public Color highlightColor {
												get { return _highlightColor; }
												set {
																if (_highlightColor != value) {
																				_highlightColor = value;
																				UpdateMaterialProperties ();
																}
												}
								}

								[SerializeField]
								[Range (0.1f, 5f)]
								float _highlightSpeed = 1f;

								public float highlightSpeed {
												get { return _highlightSpeed; }
												set {
																if (_highlightSpeed != value) {
																				_highlightSpeed = value;
																}
												}
								}

								[SerializeField]
								bool _highlightEnabled = true;

								public bool highlightEnabled {
												get { return _highlightEnabled; }
												set {
																if (_highlightEnabled != value) {
																				_highlightEnabled = value;
																				if (!_highlightEnabled)
																								HideHighlightedTile ();
																}
												}
								}

								[SerializeField]
								bool _raycast3D = true;

								public bool raycast3D {
												get { return _raycast3D; }
												set {
																if (_raycast3D != value) {
																				_raycast3D = value;
																				UpdateMaterialProperties ();
																}
												}
								}

								[SerializeField]
								bool
												_rightClickRotates = true;

								public bool	rightClickRotates {
												get { return _rightClickRotates; }
												set {
																if (value != _rightClickRotates) {
																				_rightClickRotates = value;
																}
												}
								}

								[SerializeField]
								bool
								_rightClickRotatingClockwise = false;

								public bool	rightClickRotatingClockwise {
												get { return _rightClickRotatingClockwise; }
												set {
																if (value != _rightClickRotatingClockwise) {
																				_rightClickRotatingClockwise = value;
																}
												}
								}


								public int lastHighlightedTileIndex = -1;
								public Tile lastHighlightedTile;



								#region Public API

								/// <summary>
								/// Centers view on target tile.
								/// </summary>
								/// <param name="destinationTileIndex">Destination tile index.</param>
								public void FlyTo (int destinationTileIndex) {
												FlyTo (destinationTileIndex, 0);
								}

								/// <summary>
								/// Navigates to target tile.
								/// </summary>
								/// <param name="destinationTileIndex">Destination tile index.</param>
								/// <param name="duration">Duration.</param>
								public void FlyTo (int destinationTileIndex, float duration) {
												if (destinationTileIndex < 0 || destinationTileIndex >= tiles.Length)
																return;
												flyingStartRotation = transform.rotation;
												Vector3 v1;
												Vector3 v2 = tiles [destinationTileIndex].center.normalized;
												if (_invertedMode) {
																v1 = Camera.main.transform.forward;
												} else {
																v1 = (Camera.main.transform.position - transform.position).normalized;
												}
												flyingEndRotation = Quaternion.FromToRotation (v2, v1);
												if (duration <= 0) {
																transform.rotation = flyingEndRotation;
																return;
												}
												flyingStartTime = Time.time;
												flyingDuration = duration;
												flying = true;
								}


								public bool isMouseOver {
												get { return mouseIsOver || _VREnabled; }
								}

								#endregion
				}

}