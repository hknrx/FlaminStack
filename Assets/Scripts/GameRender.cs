// Nicolas Robert [Nrx]

using UnityEngine;

public partial class Game
{
	// Camera
	private Camera cameraComponent;

	// Sprites
	public enum Sprites {BACKGROUND, PRIZE_MAJOR, PRIZE_MINOR, COINS, BONUS, GAME_BOARD, SLOT, BUTTON_STACK, BUTTON_CONTINUE, BUTTON_GET_PRIZE, BUTTON_TV, BUTTON_PODIUM}
	public GameObject spriteQuad;
	private Material [] spriteMaterials;
	private GameObject [] spriteObjects;

	// Rendering
	public float renderScaleMin = 1.414f;
	public float renderScaleMax = 4.0f;
	private float renderScaleTarget;
	private float renderScale = 0.0f;
	public bool renderBgLastTarget = true;
	private bool renderBgLast = false;
	public bool renderTAATarget = true;
	private bool renderTAA = false;
	private Vector2 renderSizeTarget;
	private Vector2 renderSize;
	public Shader renderMixShader;
	private Material renderMixMaterial;
	private RenderTexture renderTextureMain;
	private RenderTexture renderTextureOffsetted;
	private Vector2 renderBonusSize;
	private float renderBonusYDisabled;
	private float renderBonusYEnabled;
	public float renderBonusStateEnabled = 0.0f;

	// Layout
	public float LAYOUT_SPACING = 0.01f;
	public float LAYOUT_BORDER_THICKNESS = 0.01f;
	public float LAYOUT_LED_RATIO = 0.6f;
	public float LAYOUT_COINS_LED_COUNT = 6.0f;
	public float LAYOUT_COINS_HEIGHT = 0.08f;
	public float LAYOUT_PRIZE_RATIO = 0.8f;
	public float LAYOUT_PRIZE_MAJOR_LED_COUNT = 4.0f;
	public float LAYOUT_PRIZE_MINOR_LED_COUNT = 1.0f;
	public float LAYOUT_BUTTON_BOTTOM_HEIGHT = 0.1f;

	// Frame rate measurement
	public UnityEngine.UI.Text frameRateLabel;
	public float frameRateMeasureDuration = 1.5f;
	public float frameRateAccuracy = 0.95f;
	private float frameTimer = 0.0f;
	private int frameCount = 0;

	// Initialize the rendering
	private void RenderInitialize ()
	{
		// Set the target frame rate
		Application.targetFrameRate = 60;

		// Set the screen dimming timeout
		Screen.sleepTimeout = SleepTimeout.SystemSetting;

		// Get the camera
		cameraComponent = GetComponent <Camera> ();

		// Create the mix material
		renderMixMaterial = new Material (renderMixShader);

		// Create all the sprites
		Sprites [] sprites = (Sprites []) System.Enum.GetValues (typeof (Sprites));
		spriteMaterials = new Material [sprites.Length];
		spriteObjects = new GameObject [sprites.Length];
		foreach (Sprites sprite in sprites) {

			// Load and copy the material
			// Note: we do not check whether the material exists (we assume that it does)
			Material spriteMaterial = (Material) Resources.Load (sprite.ToString (), typeof (Material));
			spriteMaterial = Object.Instantiate <Material> (spriteMaterial);
			spriteMaterials [(int) sprite] = spriteMaterial;

			// Create the game object
			GameObject spriteObject = Object.Instantiate <GameObject> (spriteQuad);
			spriteObject.name = spriteMaterial.name;
			spriteObject.GetComponent <Renderer> ().material = spriteMaterial;
			spriteObjects [(int) sprite] = spriteObject;
		}
	}

	// Reset a sprite
	private void RenderResetSprite (Sprites sprite, Vector3 position, Vector2 size)
	{
		GameObject spriteObject = spriteObjects [(int) sprite];
		spriteObject.transform.localPosition = position;
		spriteObject.transform.localScale = new Vector3 (size.x, size.y, 1.0f);
		spriteMaterials [(int) sprite].SetVector ("quadInfo", new Vector4 (size.x / size.y, size.y / renderSize.y, 2.0f * position.x / renderSize.y, 2.0f * position.y / renderSize.y));
	}

	// Reset the rendering
	private void RenderReset ()
	{
		// Define the rendering resolution
		int width = (int) (renderSize.x / renderScale);
		int height = (int) (renderSize.y / renderScale);

		// Create new render textures (with bilinear filtering)
		renderTextureMain = renderTAA || renderScale != 1.0f ? new RenderTexture (width, height, 16, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear) : null;
		renderTextureOffsetted = renderTAA ? new RenderTexture (width + 1, height + 1, 16, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear) : null;

		// Update the mix shader
		renderMixMaterial.SetVector ("_MainTexResolution", new Vector2 (width, height));
		renderMixMaterial.SetTexture ("_OffsettedTex", renderTextureOffsetted);

		// Change the rendering order of the background
		if (renderBgLast) {

			// Display the background last
			spriteMaterials [(int) Sprites.BACKGROUND].renderQueue = 2001;
			foreach (Material spriteMaterial in spriteMaterials) {
				spriteMaterial.EnableKeyword ("TRANSPARENT_CUTOUT");
			}
		} else {

			// Display the background first
			spriteMaterials [(int) Sprites.BACKGROUND].renderQueue = 1000;
			foreach (Material spriteMaterial in spriteMaterials) {
				spriteMaterial.DisableKeyword ("TRANSPARENT_CUTOUT");
			}

			// The bonus is displayed second in this case (it is displayed first otherwise)
			spriteMaterials [(int) Sprites.BONUS].EnableKeyword ("TRANSPARENT_CUTOUT");
		}

		// Define basic layout values
		float spacingDefault = renderSize.y * LAYOUT_SPACING;
		float borderThickness = renderSize.y * LAYOUT_BORDER_THICKNESS;
		float prizeRatioMax = LAYOUT_PRIZE_RATIO * LAYOUT_LED_RATIO * Mathf.Max (LAYOUT_PRIZE_MAJOR_LED_COUNT, LAYOUT_PRIZE_MINOR_LED_COUNT);

		// Compute the size of each sprite
		float coinsWidth = Mathf.Min (renderSize.y * LAYOUT_COINS_HEIGHT * LAYOUT_LED_RATIO * LAYOUT_COINS_LED_COUNT, renderSize.x - 2.0f * spacingDefault);
		float coinsHeight = coinsWidth / (LAYOUT_LED_RATIO * LAYOUT_COINS_LED_COUNT);

		float buttonBottomHeight = Mathf.Min (renderSize.y * LAYOUT_BUTTON_BOTTOM_HEIGHT, (renderSize.x - 4.0f * spacingDefault) / 3.0f);

		float gameBoardHeight = renderSize.y - 4.0f * spacingDefault - coinsHeight - buttonBottomHeight;
		float blockSize = (gameBoardHeight - 2.0f * borderThickness) / 12.0f;
		float gameWidth = 3.0f * spacingDefault + 2.0f * borderThickness + blockSize * (7.0f + prizeRatioMax);
		if (gameWidth > renderSize.x) {
			gameWidth = renderSize.x;
			blockSize = (gameWidth - 3.0f * spacingDefault - 2.0f * borderThickness) / (7.0f + prizeRatioMax);
			gameBoardHeight = blockSize * 12.0f + 2.0f * borderThickness;
		}
		float gameBoardWidth = blockSize * 7.0f + 2.0f * borderThickness;

		float buttonBottomWidth = Mathf.Max ((gameWidth - 4.0f * spacingDefault) / 3.0f, buttonBottomHeight);
		float buttonStackHeight = Mathf.Min (renderSize.y - 4.0f * spacingDefault - coinsHeight - gameBoardHeight, buttonBottomWidth);

		float spacingVertical = (renderSize.y - coinsHeight - gameBoardHeight - buttonStackHeight) / 4.0f;

		float prizeHeight = blockSize * LAYOUT_PRIZE_RATIO;
		float prizeMajorWidth = prizeHeight * LAYOUT_LED_RATIO * LAYOUT_PRIZE_MAJOR_LED_COUNT;
		float prizeMinorWidth = prizeHeight * LAYOUT_LED_RATIO * LAYOUT_PRIZE_MINOR_LED_COUNT;

		float buttonTvSize = blockSize * Mathf.Min (prizeRatioMax, 1.5f);
		float buttonPodiumSize = buttonTvSize;

		float slotSize = blockSize * prizeRatioMax;

		renderBonusSize.x = gameWidth;
		renderBonusSize.y = (renderBonusSize.x - 2.0f * borderThickness) / (LAYOUT_LED_RATIO * 4.0f) + 2.0f * borderThickness;

		// Compute the position of each sprite
		float coinsX = 0.0f;
		float coinsY = 0.5f * (renderSize.y - coinsHeight) - spacingVertical;

		float gameBoardX = -0.5f * (spacingDefault + Mathf.Max (prizeMajorWidth, prizeMinorWidth));
		float gameBoardY = 0.5f * (renderSize.y - gameBoardHeight) - 2.0f * spacingVertical - coinsHeight;

		float prizeMajorX = gameBoardX + spacingDefault + 0.5f * (gameBoardWidth + prizeMajorWidth);
		float prizeMajorY = gameBoardY + blockSize * 5.5f;

		float prizeMinorX = gameBoardX + spacingDefault + 0.5f * (gameBoardWidth + prizeMinorWidth);
		float prizeMinorY = gameBoardY + blockSize * 2.5f;

		float buttonBottomY = spacingVertical - 0.5f * (renderSize.y - buttonStackHeight);
		float buttonStackX = 0.0f;
		float buttonGetPrizeX = spacingDefault + buttonBottomWidth;
		float buttonContinueX = -buttonGetPrizeX;

		float buttonTvX = gameBoardX + spacingDefault + 0.5f * (gameBoardWidth + buttonTvSize);
		float buttonTvY = gameBoardY + blockSize * 4.0f;

		float buttonPodiumX = gameBoardX + spacingDefault + 0.5f * (gameBoardWidth + buttonPodiumSize);
		float buttonPodiumY = gameBoardY;

		float slotX = gameBoardX + spacingDefault + 0.5f * (gameBoardWidth + slotSize);
		float slotY = gameBoardY - blockSize * 4.0f;

		renderBonusYDisabled = 0.5f * (renderSize.y + renderBonusSize.y) + 1.0f;
		renderBonusYEnabled = gameBoardY + blockSize * 4.0f;

		// Reset all the sprites except the bonus
		// Notes:
		// - Make sure the background is behind everything else
		// - Make sure the background covers the whole screen (and even more, because of the TAA!)
		// - Make sure the bonus is above everything else, except the amount of coins
		// - Make sure the amount of coins is above everything else
		RenderResetSprite (Sprites.BACKGROUND, new Vector3 (0.0f, 0.0f, 0.1f), new Vector2 (renderSize.x + 2.0f, renderSize.y + 2.0f));
		RenderResetSprite (Sprites.GAME_BOARD, new Vector2 (gameBoardX, gameBoardY), new Vector2 (gameBoardWidth, gameBoardHeight));
		RenderResetSprite (Sprites.PRIZE_MAJOR, new Vector2 (prizeMajorX, prizeMajorY), new Vector2 (prizeMajorWidth, prizeHeight));
		RenderResetSprite (Sprites.PRIZE_MINOR, new Vector2 (prizeMinorX, prizeMinorY), new Vector2 (prizeMinorWidth, prizeHeight));
		RenderResetSprite (Sprites.COINS, new Vector3 (coinsX, coinsY, -0.2f), new Vector2 (coinsWidth, coinsHeight));
		RenderResetSprite (Sprites.SLOT, new Vector2 (slotX, slotY), new Vector2 (slotSize, slotSize));
		RenderResetSprite (Sprites.BUTTON_STACK, new Vector2 (buttonStackX, buttonBottomY), new Vector2 (buttonBottomWidth, buttonStackHeight));
		RenderResetSprite (Sprites.BUTTON_CONTINUE, new Vector2 (buttonContinueX, buttonBottomY), new Vector2 (buttonBottomWidth, buttonBottomHeight));
		RenderResetSprite (Sprites.BUTTON_GET_PRIZE, new Vector2 (buttonGetPrizeX, buttonBottomY), new Vector2 (buttonBottomWidth, buttonBottomHeight));
		RenderResetSprite (Sprites.BUTTON_TV, new Vector2 (buttonTvX, buttonTvY), new Vector2 (buttonTvSize, buttonTvSize));
		RenderResetSprite (Sprites.BUTTON_PODIUM, new Vector2 (buttonPodiumX, buttonPodiumY), new Vector2 (buttonPodiumSize, buttonPodiumSize));

		// Additional settings for specific sprites
		for (int spriteIndex = (int) Sprites.BONUS; spriteIndex <= (int) Sprites.BUTTON_PODIUM; ++spriteIndex) {
			spriteMaterials [spriteIndex].SetFloat ("borderThickness", LAYOUT_BORDER_THICKNESS);
		}
		for (int spriteIndex = (int) Sprites.PRIZE_MAJOR; spriteIndex <= (int) Sprites.BONUS; ++spriteIndex) {
			spriteMaterials [spriteIndex].SetFloat ("ledRatio", LAYOUT_LED_RATIO);
		}
	}

	// Update the rendering
	private void RenderUpdate ()
	{
		// Measure the frame rate
		if (frameTimer <= 0.0f) {
			float frameRate = frameCount / (frameRateMeasureDuration - frameTimer);

			// Display the frame rate and current rendering scale
			frameRateLabel.text = System.String.Format ("{0:F1} fps\n(x{1:F1})", frameRate, renderScale);

			// Adapt the rendering scale once the game is stable
			if (frameRate <= 0.0f) {
				renderScaleTarget = renderScaleMin;
			} else if (frameRate < Application.targetFrameRate * frameRateAccuracy || frameRate > Application.targetFrameRate * (2.0f - frameRateAccuracy)) {
				renderScaleTarget = Mathf.Clamp (renderScale * Mathf.Sqrt (Application.targetFrameRate / frameRate), renderScaleMin, renderScaleMax);
			}

			// Reset
			frameTimer = frameRateMeasureDuration;
			frameCount = 1;
		} else {
			frameTimer -= Time.unscaledDeltaTime;
			++frameCount;
		}

		// Monitor changes that would require to reset the rendering
		bool renderReset = renderScale != renderScaleTarget ||
			renderBgLast != renderBgLastTarget ||
			renderTAA != renderTAATarget ||
			renderSize != renderSizeTarget;
		if (renderReset) {

			// Update the rendering parameters
			renderScale = renderScaleTarget;
			renderBgLast = renderBgLastTarget;
			renderTAA = renderTAATarget;
			renderSize = renderSizeTarget;

			// Reset the rendering
			RenderReset ();
		}

		// Reset the bonus sprite to update its position
		RenderResetSprite (Sprites.BONUS, new Vector3 (0.0f, Mathf.Lerp (renderBonusYDisabled, renderBonusYEnabled, renderBonusStateEnabled), -0.1f), renderBonusSize);

		// Select the appropriate render texture
		if (renderTAA && (Time.frameCount & 1) != 0) {
			cameraComponent.targetTexture = renderTextureOffsetted;
			cameraComponent.orthographicSize = renderSize.y * 0.5f * renderTextureOffsetted.height / renderTextureMain.height;
			cameraComponent.aspect = (float) renderTextureOffsetted.width / renderTextureOffsetted.height;
		} else {
			cameraComponent.targetTexture = renderTextureMain;
			cameraComponent.orthographicSize = renderSize.y * 0.5f;
			if (cameraComponent.targetTexture) {
				cameraComponent.aspect = (float) renderTextureMain.width / renderTextureMain.height;
			} else {
				cameraComponent.aspect = renderSize.x / renderSize.y;
			}
		}

		// Set the mix ratio
		float mixRatio;
		if (!renderReset) {
			mixRatio = 0.5f;
		} else if (cameraComponent.targetTexture == renderTextureMain) {
			mixRatio = 0.0f;
		} else {
			mixRatio = 1.0f;
		}
		renderMixMaterial.SetFloat ("mixRatio", mixRatio);
	}

	// Method to render the scene full screen (to be called from the "OnPostRender" of another camera)
	public void RenderFullScreen (RenderTexture renderTextureDest = null)
	{
		if (renderTAA) {
			Graphics.Blit (renderTextureMain, renderTextureDest, renderMixMaterial);
		} else if (cameraComponent.targetTexture) {
			Graphics.Blit (cameraComponent.targetTexture, renderTextureDest);
		}
	}
}
