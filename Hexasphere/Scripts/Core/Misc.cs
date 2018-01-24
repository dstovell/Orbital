using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

namespace HexasphereGrid {
				
				public static class Misc {

								public static Vector3 Vector3zero = Vector3.zero;
								public static Color32 Color32White = Color.white;
												
								public static float Vector3SqrDistance(Vector3 a, Vector3 b) {
												float dx = a.x - b.x;
												float dy = a.y - b.y;
												float dz = a.z - b.z;
												return dx * dx + dy * dy + dz * dz;
								}

				}

}