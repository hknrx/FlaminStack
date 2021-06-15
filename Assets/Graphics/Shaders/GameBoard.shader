// Nicolas Robert [Nrx]

// Notes:
// - quadInfo.x = quad.width / quad.height
// - quadInfo.y = quad.height / screen.height
// - quadInfo.zw = position of the quad (center point) in the screen space = (2 * Qc - screen.size) / screen.height
// - fragLocal = coordinates of the fragment in the quad space (from -1 to 1 on the Y axis)
// - fragScreen = coordinates of the fragment in the screen space (assuming that the screen height goes from -1 to 1)

// Notes:
// labelRatio = labelTexture.width / labelTexture.height
// Font used: Sans, 64px
// - MAJOR_PRIZE 451x53
// - MINOR_PRIZE 436x53

Shader "Custom/GameBoard" {
	Properties {
		[HideInInspector] quadInfo ("Quad information", Vector) = (1.0, 1.0, 0.0, 0.0)
		[HideInInspector] time ("Time", Float) = 0.0
		[HideInInspector] borderThickness ("Border thickness", Float) = 1.0
		[HideInInspector] gameData ("Game data", Vector) = (0.0, 0.0, 0.0, 0.0)
		labelMajorTexture ("Label major texture", 2D) = "white" {}
		labelMajorRatio ("Label major ratio", Float) = 1.0
		labelMinorTexture ("Label minor texture", 2D) = "white" {}
		labelMinorRatio ("Label minor ratio", Float) = 1.0
		hue ("Hue", Float) = 0.0
		hueError ("Hue error", Float) = 0.0
		saturation ("Saturation", Float) = 0.0
	}
	SubShader {
		Tags {"Queue" = "Geometry"}
		Pass {
			Lighting Off
			Fog {Mode Off}
			Cull Off
			ZWrite On
			ZTest LEqual
			Blend SrcAlpha OneMinusSrcAlpha

			GLSLPROGRAM

			// Define multiple shader program variants:
			// - (default): process all fragments
			// - TRANSPARENT_CUTOUT: discard fragments which the alpha is lower than 0.5
			#pragma multi_compile __ TRANSPARENT_CUTOUT

			// Vertex shader: begin
			#ifdef VERTEX

				// Variables shared between the OpenGL ES environment and vertex shader
				uniform vec4 quadInfo;

				// Variables shared between the vertex shader and the fragment shader
				varying vec2 fragLocal;
				varying vec2 fragScreen;

				// Main function
				void main () {

					// Set the coordinates
					vec2 fragTexture = gl_MultiTexCoord0.st;
					fragLocal = (2.0 * fragTexture - 1.0) * vec2 (quadInfo.x, 1.0);
					fragScreen = fragLocal * quadInfo.y + quadInfo.zw;

					// Set the vertex position
					gl_Position = gl_ModelViewProjectionMatrix * gl_Vertex;
				}

			// Vertex shader: end
			#endif

			// Fragment shader: begin
			#ifdef FRAGMENT

				// Constants (global)
				#define PI 3.14159265359
				#define BORDER_VALUE 0.4
				#define REFLECTION_SCALE 12.0
				#define SMOOTH_DISTANCE 0.005

				// Parameters (specific)
				#define PRIZE_BOX_HALF_THICKNESS 0.03
				#define PRIZE_BOX_MARGIN 0.06

				// Variables shared between the OpenGL ES environment and the fragment shader
				uniform vec4 quadInfo;
				uniform float time;
				uniform float borderThickness;
				uniform vec4 gameData;
				uniform sampler2D labelMajorTexture;
				uniform float labelMajorRatio;
				uniform sampler2D labelMinorTexture;
				uniform float labelMinorRatio;
				uniform float hue;
				uniform float hueError;
				uniform float saturation;

				// Variables shared between the vertex shader and the fragment shader
				varying vec2 fragLocal;
				varying vec2 fragScreen;

				// HSV to RGB
				vec3 hsv2rgb (in vec3 hsv) {
					hsv.yz = clamp (hsv.yz, 0.0, 1.0);
					return hsv.z * (1.0 + hsv.y * clamp (abs (fract (hsv.x + vec3 (0.0, 2.0 / 3.0, 1.0 / 3.0)) * 6.0 - 3.0) - 2.0, -1.0, 0.0));
				}

				// PRNG
				float rand (in vec2 seed) {
					return fract (sin (dot (seed, vec2 (12.9898, 78.233))) * 137.5453);
				}

				// Distance to a rounded box
				float boxDist (in vec2 frag, in vec3 box) {
					return length (max (abs (frag) - box.xy + box.z, 0.0)) - box.z;
				}

				// Main function
				void main () {

					// Define the geometry of the game board
					// Note the change of space: blocks go from [0,0] to [7,12]
					vec3 gameBoardSize = vec3 (3.5, 6.0, 0.5);
					float gameBoardBorder = 2.0 * borderThickness / quadInfo.y;
					float gameBoardScale = gameBoardSize.y / (1.0 - gameBoardBorder);
					vec2 fragBlock = fragLocal * gameBoardScale;
					gameBoardBorder *= gameBoardScale;
					float gameBoardDist = boxDist (fragBlock, gameBoardSize + gameBoardBorder);
					fragBlock += gameBoardSize.xy;
					float alpha = smoothstep (0.0, -SMOOTH_DISTANCE, gameBoardDist);

					// Blocks
					vec2 blockCoord = floor (fragBlock);
					float blockArea = floor (blockCoord.y / 3.0);
					float blockData = dot (gameData, floor (mod (vec4 (1, 2, 4, 8) / exp2 (blockArea), 2.0)));
					float blockId = blockCoord.x + 7.0 * mod (blockCoord.y, 3.0);
					float blockState = floor (mod (blockData / exp2 (blockId), 2.0));
					float blockHue = hue + hueError * (blockCoord.x + blockCoord.y) / (7.0 + 12.0) + blockState * cos (time + PI * rand (blockCoord)) * 0.02;
					float blockSaturation = saturation;
					float blockValue = 0.1 + 0.9 * blockState;
					blockValue *= 0.1 + 0.9 * smoothstep (1.0, 0.0, length (fract (fragBlock) - 0.5));

					// Prize boxes
					float prizeValue = smoothstep (PRIZE_BOX_HALF_THICKNESS, PRIZE_BOX_HALF_THICKNESS - SMOOTH_DISTANCE, abs (abs (fragBlock.y - 9.5 - PRIZE_BOX_MARGIN - PRIZE_BOX_HALF_THICKNESS) - 1.5))
						* smoothstep (-0.5, 0.5, cos (4.0 * PI * (fragBlock.x + cos (time))));

					prizeValue += smoothstep (PRIZE_BOX_HALF_THICKNESS, PRIZE_BOX_HALF_THICKNESS - SMOOTH_DISTANCE, abs (abs (fragBlock.y - 10.5 + PRIZE_BOX_MARGIN + PRIZE_BOX_HALF_THICKNESS) - 1.5))
						* smoothstep (-0.5, 0.5, cos (4.0 * PI * (fragBlock.x - cos (time))));

					// Prize labels
					vec2 labelCoord = 2.0 * (fragBlock - vec2 (3.5, 11.5)) / vec2 (labelMajorRatio, 1.0) + 0.5;
					prizeValue += texture2D (labelMajorTexture, labelCoord).r;

					labelCoord = 2.0 * (fragBlock - vec2 (3.5, 8.5)) / vec2 (labelMinorRatio, 1.0) + 0.5;
					prizeValue += texture2D (labelMinorTexture, labelCoord).r;

					// Define the game board
					vec3 color = hsv2rgb (vec3 (blockHue, blockSaturation, blockValue));
					color += 0.5 * prizeValue * (1.0 - color);

					// Add the border
					color *= smoothstep (-gameBoardBorder, -gameBoardBorder - SMOOTH_DISTANCE, gameBoardDist);
					color += BORDER_VALUE * smoothstep (gameBoardBorder * 0.5, gameBoardBorder * 0.1, abs (gameBoardDist + gameBoardBorder * 0.5));

					// Screen reflection
					color *= 1.1 + 0.3 * cos (REFLECTION_SCALE * (fragScreen.x + fragScreen.y) + time);

					// Transparency Cutout
					#ifdef TRANSPARENT_CUTOUT
					if (alpha < 0.5) {
						discard;
					}
					#endif

					// Set the fragment color
					gl_FragColor = vec4 (color, alpha);
				}

			// Fragment shader: end
			#endif

			ENDGLSL
		}
	}
}
