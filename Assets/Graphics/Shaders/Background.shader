// Nicolas Robert [Nrx]

// Notes:
// - quadInfo.x = quad.width / quad.height
// - quadInfo.y = quad.height / screen.height
// - quadInfo.zw = position of the quad (center point) in the screen space = (2 * Qc - screen.size) / screen.height
// - fragScreen = coordinates of the fragment in the screen space (assuming that the screen height goes from -1 to 1)

Shader "Custom/Background" {
	Properties {
		[HideInInspector] quadInfo ("Quad information", Vector) = (1.0, 1.0, 0.0, 0.0)
		[HideInInspector] time ("Time", Float) = 0.0
		noiseTexture ("Noise texture", 2D) = "white" {}
	}
	SubShader {
		Tags {"Queue" = "Background"}
		Pass {
			Lighting Off
			Fog {Mode Off}
			Cull Off
			ZWrite Off
			ZTest LEqual
			Blend Off

			GLSLPROGRAM

			// Define multiple shader program variants (1 per scene)
			#pragma multi_compile __ SCENE_BIT0
			#pragma multi_compile __ SCENE_BIT1
			#pragma multi_compile __ SCENE_BIT2

			// Vertex shader: begin
			#ifdef VERTEX

				// Variables shared between the OpenGL ES environment and vertex shader
				uniform vec4 quadInfo;

				// Variables shared between the vertex shader and the fragment shader
				varying vec2 fragScreen;

				// Main function
				void main () {

					// Set the coordinates
					vec2 fragTexture = gl_MultiTexCoord0.st;
					fragScreen = (2.0 * fragTexture - 1.0) * vec2 (quadInfo.x, 1.0) * quadInfo.y + quadInfo.zw;

					// Set the vertex position
					gl_Position = gl_ModelViewProjectionMatrix * gl_Vertex;
				}

			// Vertex shader: end
			#endif

			// Fragment shader: begin
			#ifdef FRAGMENT

				// Constants
				#define PI					3.14159265359
				#define SQRT2				1.41421356237
				#define NOISE_TEXTURE_SIZE	64.0
//				#define NOISE_BUG			// If bilinear filtering does not work, then disable filtering and set this macro

				// Variables shared between the OpenGL ES environment and the fragment shader
				uniform float time;
				uniform sampler2D noiseTexture;

				// Variables shared between the vertex shader and the fragment shader
				varying vec2 fragScreen;

				// PRNG
				float rand (in vec2 seed) {
					return fract (sin (dot (seed, vec2 (11.9898, 78.233))) * 137.5453);
				}

				// Noise
				float noise (in vec3 p) {
					vec3 f = fract (p);
					f = f * f * (3.0 - 2.0 * f);
					p = floor (p);
					p.xy += p.z * vec2 (37.0, 17.0);
					#if !defined (NOISE_BUG)
						f.xy = texture2D (noiseTexture, (p.xy + f.xy + 0.5) / NOISE_TEXTURE_SIZE).xy;
					#else
						p.xy = (p.xy + 0.5) / NOISE_TEXTURE_SIZE;
						const vec2 n = vec2 (1.0 / NOISE_TEXTURE_SIZE, 0.0);
						f.xy = mix (
							mix (texture2D (noiseTexture, p.xy + n.yy).xy, texture2D (noiseTexture, p.xy + n.xy).xy, f.x),
							mix (texture2D (noiseTexture, p.xy + n.yx).xy, texture2D (noiseTexture, p.xy + n.xx).xy, f.x),
							f.y
						);
					#endif
					return mix (f.x, f.y, f.z);
				}

				// FBM
				float fbm (in vec3 p) {
					return (noise (p) + noise (p * 2.0) / 2.0 + noise (p * 4.0) / 4.0) / (1.0 + 1.0 / 2.0 + 1.0 / 4.0);
				}

				// HSV to RGB
				vec3 hsv2rgb (in vec3 hsv) {
					hsv.yz = clamp (hsv.yz, 0.0, 1.0);
					return hsv.z * (1.0 + hsv.y * clamp (abs (fract (hsv.x + vec3 (0.0, 2.0 / 3.0, 1.0 / 3.0)) * 6.0 - 3.0) - 2.0, -1.0, 0.0));
				}

/////////////////////////////
// SCENE 3 - "Star tunnel" //
/////////////////////////////
#if defined (SCENE_BIT0) && defined (SCENE_BIT1) && !defined (SCENE_BIT2)

				// Parameters
				#define CAMERA_FOCAL_LENGTH	2.5
				#define RAY_STEP_MAX		100
				#define RAY_LENGTH_MAX		150.0
				#define DELTA				0.01
				#define DIST_FIX_FACTOR		0.7
				#define CYLINDER_RADIUS		10.0
				#define LIGHT_DIRECTION		vec3 (0.0, 0.5, -1.0)
				#define AMBIENT				0.5
				#define SPECULAR_POWER		3.0
				#define SPECULAR_INTENSITY	0.5
				#define FADE_POWER			3.0

				// Distance to the scene
				float getDistance (in vec3 p, out vec2 q) {

					// Global deformation
					p.xy += vec2 (3.0 * sin (p.z * 0.1 + 2.0 * time), 2.0 * sin (p.z * 0.2 + time));

					// 2D mapping
					q = vec2 (atan (p.y, p.x) * CYLINDER_RADIUS * 2.0 / PI, p.z);

					// Deformed cylinder
					vec2 d = q * PI * 2.0 / CYLINDER_RADIUS;
					return CYLINDER_RADIUS - length (p.xy) + 1.2 * sin (d.x) * sin (d.y);
				}

				// Normal at a given point
				vec3 getNormal (in vec3 p) {
					const vec2 h = vec2 (DELTA, -DELTA);
					vec2 q;
					return normalize (
						h.xxx * getDistance (p + h.xxx, q) +
						h.xyy * getDistance (p + h.xyy, q) +
						h.yxy * getDistance (p + h.yxy, q) +
						h.yyx * getDistance (p + h.yyx, q)
					);
				}

				// Main function
				void main () {

					// Define the ray corresponding to this fragment
					vec3 rayDirection = normalize (vec3 (fragScreen, CAMERA_FOCAL_LENGTH));

					// Define the position and orientation of the camera
					vec3 rayOrigin = vec3 (CYLINDER_RADIUS * 0.4 * cos (time), CYLINDER_RADIUS * 0.4 * cos (time * 1.7), time * 20.0);
					vec3 cameraForward = vec3 (0.0, 0.5, 20.0 * cos (time * 0.1));
					float cameraUpAngle = PI * sin (time) * sin (time * 0.2);
					vec3 cameraUp = vec3 (cos (cameraUpAngle), sin (cameraUpAngle), 0.0);

					mat3 cameraOrientation;
					cameraOrientation [2] = normalize (cameraForward);
					cameraOrientation [0] = normalize (cross (cameraUp, cameraForward));
					cameraOrientation [1] = cross (cameraOrientation [2], cameraOrientation [0]);

					rayDirection = cameraOrientation * rayDirection;

					// Ray marching
					vec3 p = rayOrigin;
					vec2 q;
					float rayLength = 0.0;
					for (int rayStep = 0; rayStep < RAY_STEP_MAX; ++rayStep) {
						float dist = getDistance (p, q) * DIST_FIX_FACTOR;
						rayLength += dist;
						if (dist < DELTA || rayLength > RAY_LENGTH_MAX) {
							break;
						}
						p += dist * rayDirection;
					}

					// Compute the fragment color
					vec3 color;
					if (rayLength > RAY_LENGTH_MAX) {
						color = vec3 (0.0);
					} else {

						// Object color
						vec3 normal = getNormal (p);
						color = hsv2rgb (vec3 (p.z * 0.05, 0.8 + 0.1 * cos (q.x * 10.0) * cos (q.y * 10.0), 1.0));

						// Texturing (stars)
						q *= 0.6;
						vec2 pixelCoord = fract (q) - 0.5;
						float random = 2.0 * PI * rand (floor (q));
						float angle = atan (pixelCoord.y, pixelCoord.x) + 0.25 * PI * cos (random + time * 2.7);
						float radius = length (pixelCoord) * (1.3 + 0.3 * cos (angle * 5.0));
						radius *= 1.5 + 0.5 * cos (random + time);
						float stars = smoothstep (0.5, 0.4, radius);
						color *= 1.0 - stars;

						// Lighting
						vec3 lightDirection = normalize (LIGHT_DIRECTION);
						vec3 reflectDirection = reflect (rayDirection, normal);
						float diffuse = max (0.0, dot (normal, lightDirection));
						float specular = pow (max (0.0, dot (reflectDirection, lightDirection)), SPECULAR_POWER) * SPECULAR_INTENSITY;
						float fade = pow (1.0 - rayLength / RAY_LENGTH_MAX, FADE_POWER);
						color = ((AMBIENT + diffuse) * color + specular) * fade;

						// Special effect
						color *= 1.0 + 4.0 * smoothstep (0.98, 1.0, cos (p.z * 0.04 + time * 4.0));
					}

					// Set the fragment color
					gl_FragColor = vec4 (color, 1.0);
				}

////////////////////////////////////
// SCENE 2 - "Light dot cylinder" //
////////////////////////////////////
#elif !defined (SCENE_BIT0) && defined (SCENE_BIT1) && !defined (SCENE_BIT2)

				// Parameters
				#define CAMERA_FOCAL_LENGTH	1.2
				#define DOT_COUNT			100.0

				// Main function
				void main () {

					// Define the ray corresponding to this fragment
					vec3 ray = vec3 (fragScreen, CAMERA_FOCAL_LENGTH);

					// Simulate the music info
					float soundBass = 0.6 + 0.4 * cos (time * 0.2);
					float soundTreble = 0.5 + 0.5 * cos (time * 1.2);

					// Define the number of rows
					float dotRowCount = floor (20.0 + 60.0 * soundTreble * soundBass) * 2.0;

					// Compute the orientation of the camera
					float yawAngle = cos (time * 2.0);
					float pitchAngle = 2.0 * PI * cos (time * 0.2 + soundTreble * 0.4);

					float cosYaw = cos (yawAngle);
					float sinYaw = sin (yawAngle);
					float cosPitch = cos (pitchAngle);
					float sinPitch = sin (pitchAngle);

					mat3 cameraOrientation;
					cameraOrientation [0] = vec3 (cosYaw, 0.0, -sinYaw);
					cameraOrientation [1] = vec3 (sinYaw * sinPitch, cosPitch, cosYaw * sinPitch);
					cameraOrientation [2] = vec3 (sinYaw * cosPitch, -sinPitch, cosYaw * cosPitch);

					ray = cameraOrientation * ray;

					// Compute the position of the camera
					float cameraDist = -2.5 + 0.5 * (cos (time * 1.3) * cos (time * 1.7) + soundBass);
					vec3 cameraPosition = cameraOrientation [2] * cameraDist;

					// Compute the intersection point (ray / cylinder)
					float a = dot (ray.xz, ray.xz);
					float b = dot (cameraPosition.xz, ray.xz);
					float c = dot (cameraPosition.xz, cameraPosition.xz) - 1.0;
					float ok = 1.0 - step (0.0, b) * step (0.0, c);
					c = sqrt (b * b - a * c);
					vec3 hit;
					if (b < -c) {
						hit = cameraPosition - ray * (b + c) / a;
						if (abs (hit.y * DOT_COUNT / PI + 1.0) > dotRowCount) {
							hit = cameraPosition - ray * (b - c) / a;
						}
					} else {
						hit = cameraPosition - ray * (b - c) / a;
					}
					vec2 frag = vec2 ((atan (hit.z, hit.x) + PI) * DOT_COUNT, hit.y * DOT_COUNT + PI) / (2.0 * PI);

					// Compute the fragment color
					vec2 id = floor (frag);
					float random = rand (id);
					vec3 color = hsv2rgb (vec3 (time * 0.05 + id.y * 0.005, 1.0, 1.0));
					color += 0.5 * cos (random * vec3 (1.0, 2.0, 3.0));
					color *= smoothstep (0.5, 0.1, length (fract (frag) - 0.5));
					color *= 0.5 + 1.5 * step (0.9, cos (random * time * 5.0));
					color *= 0.5 + 0.5 * cos (random * time + PI * 0.5 * soundTreble);
					color *= smoothstep (dotRowCount, 0.0, (abs (id.y + 0.5) - 1.0) * 2.0);
					color *= ok;

					// Set the fragment color
					gl_FragColor = vec4 (color, 1.0);
				}

/////////////////////////
// SCENE 0 - "Dragoon" //
/////////////////////////
#elif !defined (SCENE_BIT0) && !defined (SCENE_BIT1) && !defined (SCENE_BIT2)

				// Parameters
				#define DELTA			0.01
				#define RAY_LENGTH_MAX	300.0
				#define RAY_STEP_MAX	100

				float fixDistance (in float d, in float correction, in float k) {
					return min (d, max ((d - DELTA) * k + DELTA, d - correction));
				}

				float getDistance (in vec3 p) {
					p += vec3 (3.0 * sin (p.z * 0.2 + time * 2.0), sin (p.z * 0.3 + time), 0.0);
					return fixDistance (length (p.xy) - 4.0 + 0.8 * sin (abs (p.x * p.y) + p.z * 4.0) * sin (p.z), 2.5, 0.2);
				}

				// Main function
				void main () {

					// Define the ray corresponding to this fragment
					vec3 direction = normalize (vec3 (fragScreen, 2.0));

					// Set the camera
					vec3 origin = vec3 ((17.0 + 5.0 * sin (time)) * cos (time * 0.2), 12.0 * sin (time * 0.2), 0.0);
					vec3 forward = vec3 (-origin.x, -origin.y, 22.0 + 6.0 * cos (time * 0.2));
					vec3 up = vec3 (0.0, 1.0, 0.0);
					mat3 rotation;
					rotation [2] = normalize (forward);
					rotation [0] = normalize (cross (up, forward));
					rotation [1] = cross (rotation [2], rotation [0]);
					direction = rotation * direction;

					// Ray marching
					vec3 p = origin;
					float dist = RAY_LENGTH_MAX;
					float rayLength = 0.0;
					float stepCount = 0.0;
					for (int rayStep = 0; rayStep < RAY_STEP_MAX; ++rayStep) {
						dist = getDistance (p);
						rayLength += dist;
						if (dist < DELTA || rayLength > RAY_LENGTH_MAX) {
							break;
						}
						p = origin + direction * rayLength;
						++stepCount;
					}

					// Compute the fragment color
					vec3 color = vec3 (1.0, 0.5, 0.0) * 2.0 * stepCount / float (RAY_STEP_MAX);
					vec3 LIGHT = normalize (vec3 (1.0, -3.0, -1.0));
					if (dist < DELTA) {
						vec2 h = vec2 (DELTA, -DELTA);
						vec3 normal = normalize (
							h.xxx * getDistance (p + h.xxx) +
							h.xyy * getDistance (p + h.xyy) +
							h.yxy * getDistance (p + h.yxy) +
							h.yyx * getDistance (p + h.yyx));
						color.rg += 0.5 * max (0.0, dot (normal, LIGHT));
					}
					else {
						color.b += 0.1 + 0.5 * max (0.0, dot (-direction, LIGHT));
					}

					// Set the fragment color
					gl_FragColor = vec4 (color, 1.0);
				}

//////////////////////////////
// SCENE 1 - "Voxel land 2" //
//////////////////////////////
#elif defined (SCENE_BIT0) && !defined (SCENE_BIT1) && !defined (SCENE_BIT2)

				// Parameters
				#define CAMERA_FOCAL_LENGTH	1.5
				#define VOXEL_STEP			50

				// Main function
				void main () {

					// Define the ray corresponding to this fragment
					vec3 ray = normalize (vec3 (fragScreen, CAMERA_FOCAL_LENGTH));

					// Simulate the music info
					float soundBass = 0.6 + 0.4 * cos (time * 0.2);
					float soundTreble = 0.5 + 0.5 * cos (time * 1.2);

					// Set the camera
					vec3 origin = vec3 (0.0, 6.0 - 3.0 * cos (time * 0.3), time * 2.0 + 200.0 * (0.5 + 0.5 * sin (time * 0.1)));
					float cameraAngle = time * 0.1;
					vec3 cameraForward = vec3 (cos (cameraAngle), cos (time * 0.3) - 1.5, sin (cameraAngle));
					vec3 cameraUp = vec3 (0.2 * cos (time * 0.7), 1.0, 0.0);
					mat3 cameraRotation;
					cameraRotation [2] = normalize (cameraForward);
					cameraRotation [0] = normalize (cross (cameraUp, cameraForward));
					cameraRotation [1] = cross (cameraRotation [2], cameraRotation [0]);
					ray = cameraRotation * ray;

					// Voxel
					vec3 color = vec3 (0.0);

					vec2 voxelSign = sign (ray.xz);
					vec2 voxelIncrement = voxelSign / ray.xz;
					float voxelTimeCurrent = 0.0;
					vec2 voxelTimeNext = (0.5 + voxelSign * (0.5 - fract (origin.xz + 0.5))) * voxelIncrement;
					vec2 voxelPosition = floor (origin.xz + 0.5);
					float voxelHeight = 0.0;
					bool voxelDone = false;
					vec3 voxelNormal = vec3 (0.0);
					for (int voxelStep = 0; voxelStep < VOXEL_STEP; ++voxelStep) {

						// Compute the height of this column
						voxelHeight = 2.0 * rand (voxelPosition) * smoothstep (0.2, 0.5, soundBass) * sin (soundBass * 8.0 + voxelPosition.x * voxelPosition.y) - 5.0 * (0.5 + 0.5 * cos (voxelPosition.y * 0.15));

						// Check whether we hit the side of the column
						if (voxelDone = voxelHeight > origin.y + voxelTimeCurrent * ray.y) {
							break;
						}

						// Check whether we hit the top of the column
						float timeNext = min (voxelTimeNext.x, voxelTimeNext.y);
						float timeIntersect = (voxelHeight - origin.y) / ray.y;
						if (voxelDone = timeIntersect > voxelTimeCurrent && timeIntersect < timeNext) {
							voxelTimeCurrent = timeIntersect;
							voxelNormal = vec3 (0.0, 1.0, 0.0);
							break;
						}

						// Next voxel...
						voxelTimeCurrent = timeNext;
						voxelNormal.xz = step (voxelTimeNext.xy, voxelTimeNext.yx);
						voxelTimeNext += voxelNormal.xz * voxelIncrement;
						voxelPosition += voxelNormal.xz * voxelSign;
					}
					if (voxelDone) {
						origin += voxelTimeCurrent * ray;

						// Compute the local color
						vec3 mapping = origin;
						mapping.y -= voxelHeight + 0.5;
						mapping *= 1.0 - voxelNormal;
						mapping += 0.5;
						float id = rand (voxelPosition);
						color = hsv2rgb (vec3 ((time + floor (mapping.y)) * 0.05 + voxelPosition.x * 0.01, smoothstep (0.2, 0.4, soundBass), 0.7 + 0.3 * cos (id * time + PI * soundTreble)));
						color *= smoothstep (0.8 - 0.6 * cos (soundBass * PI), 0.1, length (fract (mapping) - 0.5));
						color *= 0.5 + smoothstep (0.90, 0.95, cos (id * 100.0 + soundTreble * PI * 0.5 + time * 0.5));
						color *= 1.0 - voxelTimeCurrent / float (VOXEL_STEP) * SQRT2;
					}

					// Set the fragment color
					gl_FragColor = vec4 (color, 1.0);
				}

////////////////////////////
// SCENE 4 - "Cloudy sky" //
////////////////////////////
#elif !defined (SCENE_BIT0) && !defined (SCENE_BIT1) && defined (SCENE_BIT2)

				// Parameters
				#define CAMERA_FOCAL_LENGTH	2.0
				#define CAMERA_SPEED		4.0
				#define RAY_STEP_MAX		50
				#define RAY_STEP_DIST		0.3
				#define RAY_LENGTH_MAX		20.0
				#define NOISE_FACTOR		6.0
				#define NOISE_FREQUENCY		0.6
				#define CLOUDS_SPACING		1.6
				#define DENSITY_STEP_MAX	1.0
				#define DENSITY_FACTOR		0.1
				#define COLOR_CLOUDS_A		vec3 (0.1, 0.1, 0.2)
				#define COLOR_CLOUDS_B		vec3 (0.95, 0.9, 1.0)
				#define COLOR_SKY_A			vec3 (0.95, 0.9, 1.0)
				#define COLOR_SKY_B			vec3 (0.2, 0.2, 0.3)
//				#define COLORIZE

				// Distance to the scene
				float distScene (in vec3 p, in float noise) {
					return CLOUDS_SPACING - abs (p.y + noise);
				}

				// Main function
				void main () {

					// Define the ray corresponding to this fragment
					vec3 rayDirection = normalize (vec3 (fragScreen, CAMERA_FOCAL_LENGTH));

					// Define the position and orientation of the camera
					vec3 rayOrigin = vec3 (0.0, 1.5 * cos (time * 0.5), CAMERA_SPEED * time);
					float cameraAngleY = PI * 0.5 * cos(time * 0.3);
					vec3 cameraForward = vec3 (sin (cameraAngleY), 0.0, cos (cameraAngleY));
					vec3 cameraUp = vec3 (sin (time * 0.2), 1.5, 0.0);

					mat3 cameraOrientation;
					cameraOrientation [2] = normalize (cameraForward);
					cameraOrientation [0] = normalize (cross (cameraUp, cameraForward));
					cameraOrientation [1] = cross (cameraOrientation [2], cameraOrientation [0]);

					rayDirection = cameraOrientation * rayDirection;

					// Define the colors
					float colorMix = smoothstep (-0.4, 0.4, cos (time * 0.2));
					vec3 colorSky = mix (COLOR_SKY_A, COLOR_SKY_B, colorMix);
					vec3 colorClouds = mix (COLOR_CLOUDS_A, COLOR_CLOUDS_B, colorMix);
					#ifdef COLORIZE
						vec3 color = vec3 (0.0);
						float colorCloudsValue = max (max (colorClouds.x, colorClouds.y), colorClouds.z);
						float colorCloudsSaturation = colorCloudsValue > 0.0 ? 1.0 - min (min (colorClouds.x, colorClouds.y), colorClouds.z) / colorCloudsValue : 0.0;
					#endif

					// Ray marching
					float densityTotal = 0.0;
					float rayLength = 0.0;
					for (int rayStep = 0; rayStep < RAY_STEP_MAX; ++rayStep) {

						// Compute the maximum density
						float densityMax = 1.0 - rayLength / max (RAY_LENGTH_MAX, float (RAY_STEP_MAX) * RAY_STEP_DIST);
						if (densityTotal >= densityMax) {
							break;
						}

						// Get the distance to the scene
						vec3 p = rayOrigin + rayDirection * rayLength;
						float f = NOISE_FACTOR * (fbm (p * NOISE_FREQUENCY) - 0.5);
						float dist = distScene (p, f);
						if (dist < 0.0) {

							// Compute the local density
							float densityLocal = min (-dist, DENSITY_STEP_MAX) * DENSITY_FACTOR;
							densityLocal *= densityMax - densityTotal;

							// Update the color
							#ifdef COLORIZE
								color += hsv2rgb (vec3 ((p.x + p.z) * 0.2, colorCloudsSaturation, densityLocal * colorCloudsValue));
							#endif

							// Update the total density
							densityTotal += densityLocal;
						}

						// Go ahead
						rayLength += max (RAY_STEP_DIST, dist - NOISE_FACTOR);
					}
					#ifdef COLORIZE
						color += colorSky * (1.0 - densityTotal);
					#else
						vec3 color = mix (colorSky, colorClouds, densityTotal);
					#endif

					// Set the fragment color
					gl_FragColor = vec4 (color, 1.0);
				}

/////////////////////////////
// SCENE 5 - "Psycho dots" //
/////////////////////////////
#elif defined (SCENE_BIT0) && !defined (SCENE_BIT1) && defined (SCENE_BIT2)

				// Parameters
				#define PIXEL_COUNT 40.0

				// Main function
				void main () {

					// Prepare the pixelisation
					vec2 fragPixel = fragScreen * PIXEL_COUNT;
					vec2 pixelCoord = (floor (fragPixel) + 0.5) / PIXEL_COUNT;
					fragPixel = fract (fragPixel) - 0.5;

					// Animated background
					float angle = 2.0 * PI * cos (0.5 * PI * cos (time * 0.03));
					float c = cos (angle);
					float s = sin (angle);
					pixelCoord = (2.0 + 0.5 * cos (time * 0.5)) * mat2 (c, s, -s, c) * pixelCoord;
					vec3 color = hsv2rgb (vec3 (length (pixelCoord) * 0.2 - time * 0.2, 0.6, 0.4));

					// Black shapes
					float random = 2.0 * PI * rand (floor (pixelCoord));
					pixelCoord = fract (pixelCoord) - 0.5;
					angle = atan (pixelCoord.y, pixelCoord.x) + 0.25 * PI * cos (random + time * 1.3);
					float radius = length (pixelCoord) * (1.3 + 0.3 * cos (angle * 5.0));
					float shape = smoothstep (0.5, 0.4, radius);
					shape *= smoothstep (0.0, 0.5, cos (random + time * 0.7));
					color *= 1.0 - shape;

					// Pixelisation
					color *= 1.0 - dot (fragPixel, fragPixel);

					// Set the fragment color
					gl_FragColor = vec4 (color, 1.0);
				}

////////////////
// SCENES 6~7 //
////////////////
#else

				// Main function
				void main () {

					// Set the fragment color
					gl_FragColor = vec4 (0.0);
				}

/////////
// END //
/////////
#endif

			// Fragment shader: end
			#endif

			ENDGLSL
		}
	}
}
