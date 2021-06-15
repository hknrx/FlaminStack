// Nicolas Robert [Nrx]

// Notes:
// - quadInfo.x = quad.width / quad.height
// - quadInfo.y = quad.height / screen.height
// - quadInfo.zw = position of the quad (center point) in the screen space = (2 * Qc - screen.size) / screen.height
// - fragLocal = coordinates of the fragment in the quad space (from -1 to 1 on the Y axis)
// - fragScreen = coordinates of the fragment in the screen space (assuming that the screen height goes from -1 to 1)

Shader "Custom/Bonus" {
	Properties {
		[HideInInspector] quadInfo ("Quad information", Vector) = (1.0, 1.0, 0.0, 0.0)
		[HideInInspector] time ("Time", Float) = 0.0
		[HideInInspector] borderThickness ("Border thickness", Float) = 1.0
		[HideInInspector] ledRatio ("LED ratio", Float) = 0.0
		[HideInInspector] stateEnabled ("State enabled", Float) = 1.0
		[HideInInspector] displayedNumber ("Displayed number", Int) = 0
		hue ("Hue", Float) = 0.0
	}
	SubShader {
		Tags {"Queue" = "Background+1"}
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

				// Constants & parameters (global)
				#define REFLECTION_SCALE 12.0
				#define SMOOTH_DISTANCE 0.005

				// Constants & parameters (specific)
				#define BORDER_VALUE vec3 (0.8, 0.6, 0.2)
				#define LED_GAP 0.0
				#define LED_SEGMENT_HALF_THICKNESS 0.17

				// Variables shared between the OpenGL ES environment and the fragment shader
				uniform vec4 quadInfo;
				uniform float time;
				uniform float borderThickness;
				uniform float ledRatio;
				uniform float stateEnabled;
				uniform float displayedNumber;
				uniform float hue;

				// Variables shared between the vertex shader and the fragment shader
				varying vec2 fragLocal;
				varying vec2 fragScreen;

				// HSV to RGB
				vec3 hsv2rgb (in vec3 hsv) {
					hsv.yz = clamp (hsv.yz, 0.0, 1.0);
					return hsv.z * (1.0 + hsv.y * clamp (abs (fract (hsv.x + vec3 (0.0, 2.0 / 3.0, 1.0 / 3.0)) * 6.0 - 3.0) - 2.0, -1.0, 0.0));
				}

				// Distance to a horizontal lozenge shape (LED segment)
				void ledSegDist (in vec2 frag, in vec3 segment, inout float distMin, in float digit, in float mask, inout float state) {
					frag -= vec2 (clamp (frag.x, segment.x + LED_SEGMENT_HALF_THICKNESS, segment.y - LED_SEGMENT_HALF_THICKNESS), segment.z);
					float dist = abs (frag.x) + abs (frag.y);
					if (dist < distMin) {
						distMin = dist;
						state = fract (mask / digit);
					}
				}

				// Distance to a rounded box
				float boxDist (in vec2 frag, in vec3 box) {
					return length (max (abs (frag) - box.xy + box.z, 0.0)) - box.z;
				}

				// Main function
				void main () {

					// Define the geometry of the bonus box
					vec3 boxSize = vec3 (4.0 * ledRatio, 1.0, 0.4);
					float boxBorder = 2.0 * borderThickness / quadInfo.y;
					float boxScale = boxSize.y / (1.0 - boxBorder);
					vec2 fragLED = fragLocal * boxScale;
					boxBorder *= boxScale;
					float boxDist = boxDist (fragLED, boxSize + boxBorder);
					float alpha = smoothstep (0.0, -SMOOTH_DISTANCE, boxDist);

					// Extract the digit
					float ledWidth = 2.0 * ledRatio;
					float ledIndex = fragLED.x - quadInfo.x * boxScale + boxBorder;
					fragLED = vec2 (mod (ledIndex, ledWidth) - ledRatio, fragLED.y);

					ledIndex = -ceil (ledIndex / ledWidth);
					float ledValue = floor (displayedNumber / pow (10.0, ledIndex));
					ledValue = exp2 (mod (ledValue, 10.0) + 9.0 * step (displayedNumber, -0.5) + 1.0);

					// Compute the distance to the nearest LED segment
					float top = 1.0 - LED_SEGMENT_HALF_THICKNESS;
					float bottom = -top;
					float right = ledRatio - LED_GAP * 0.5 - LED_SEGMENT_HALF_THICKNESS;
					float left = -right;

					float ledDist = 1.0;
					float ledState = 0.0;
					ledSegDist (fragLED, vec3 (left, right, top), ledDist, ledValue, 10221.0, ledState);
					ledSegDist (fragLED, vec3 (left, right, 0.0), ledDist, ledValue, 32636.0, ledState);
					ledSegDist (fragLED, vec3 (left, right, bottom), ledDist, ledValue, 8045.0, ledState);
					ledSegDist (fragLED.yx, vec3 (0.0, top, left), ledDist, ledValue, 12145.0, ledState);
					ledSegDist (fragLED.yx, vec3 (bottom, 0.0, left), ledDist, ledValue, 31045.0, ledState);
					ledSegDist (fragLED.yx, vec3 (0.0, top, right), ledDist, ledValue, 9119.0, ledState);
					ledSegDist (fragLED.yx, vec3 (bottom, 0.0, right), ledDist, ledValue, 22523.0, ledState);
					ledState = step (0.5, ledState) * stateEnabled;

					// Define the LED segment color
					vec3 color = hsv2rgb (vec3 (hue, 1.0, 0.1 + 0.9 * ledState));
					color *= smoothstep (LED_SEGMENT_HALF_THICKNESS * 0.75, LED_SEGMENT_HALF_THICKNESS * 0.75 - SMOOTH_DISTANCE, ledDist);

					// Add the border
					color *= smoothstep (-boxBorder, -boxBorder - SMOOTH_DISTANCE, boxDist);
					color += BORDER_VALUE * smoothstep (boxBorder * 0.5, boxBorder * 0.1, abs (boxDist + boxBorder * 0.5));

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
