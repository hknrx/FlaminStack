// Nicolas Robert [Nrx]

using UnityEngine;

public partial class Game
{
	// Tap flags
	private int spriteTapFlagsPrevious;
	private int spriteTapFlagsCurrent;

	// Check for taps on all the sprites
	private bool SpriteCheckTap ()
	{
		// Reset all the tap flags
		spriteTapFlagsPrevious = spriteTapFlagsCurrent;
		spriteTapFlagsCurrent = 0;

		// Check whether there is a tap
		if (!Input.GetMouseButton (0)) {
			return false;
		}

		// Check whether some sprites are tapped
		Vector2 tapPosition = (Vector2) Input.mousePosition - 0.5f * new Vector2 (Screen.width, Screen.height);
		for (int spriteIndex = (int) Sprites.BUTTON_PODIUM; spriteIndex >= 0; --spriteIndex) {
			Transform spriteTransform = spriteObjects [spriteIndex].transform;
			Vector2 tapDistance = 2.0f * (tapPosition - (Vector2) spriteTransform.localPosition);
			if (Mathf.Abs (tapDistance.x) < spriteTransform.localScale.x && Mathf.Abs (tapDistance.y) < spriteTransform.localScale.y) {
				spriteTapFlagsCurrent |= 1 << spriteIndex;
			}
		}
		return true;
	}

	// Check for a tap on a sprite
	public bool SpriteCheckTap (Sprites sprite)
	{
		int tapFlag = 1 << (int) sprite;
		return (spriteTapFlagsCurrent & tapFlag) != 0 && (spriteTapFlagsPrevious & tapFlag) == 0;
	}

	// Check for a tap on several sprites at once, without other sprites being tapped
	public bool SpriteCheckTapExclusive (params Sprites [] sprites)
	{
		int tapFlags = 0;
		foreach (Sprites sprite in sprites) {
			tapFlags |= 1 << (int) sprite;
		}
		tapFlags &= spriteTapFlagsCurrent ^ spriteTapFlagsPrevious;
		return tapFlags != 0 && (spriteTapFlagsCurrent ^ tapFlags) == 0;
	}

	// Update the pushed state of a sprite
	public void SpriteUpdateStatePushed (Sprites sprite)
	{
		int index = (int) sprite;
		float statePushed = (spriteTapFlagsCurrent & (1 << index)) != 0 ? 1.0f : 0.0f;
		spriteMaterials [index].SetFloat ("statePushed", statePushed);
	}

	// Update the enabled state of a sprite
	public void SpriteUpdateStateEnabled (Sprites sprite, bool enabled)
	{
		spriteMaterials [(int) sprite].SetFloat ("stateEnabled", enabled ? 1.0f : 0.0f);
	}

	// Update the opened state of a sprite
	public void SpriteUpdateStateOpened (Sprites sprite, float stateOpened)
	{
		spriteMaterials [(int) sprite].SetFloat ("stateOpened", stateOpened);
	}

	// Update a LED number
	public void SpriteUpdateDisplayedNumber (Sprites sprite, int number)
	{
		spriteMaterials [(int) sprite].SetInt ("displayedNumber", number);
	}

	// Update the game board
	private void SpriteUpdateGameBoard ()
	{
		// Update the data
		Vector4 gameData = new Vector4 (
			data.blockRowState [11] + (data.blockRowState [10] << 7) + (data.blockRowState [9] << 14),
			data.blockRowState [8] + (data.blockRowState [7] << 7) + (data.blockRowState [6] << 14),
			data.blockRowState [5] + (data.blockRowState [4] << 7) + (data.blockRowState [3] << 14),
			data.blockRowState [2] + (data.blockRowState [1] << 7) + (data.blockRowState [0] << 14)
		);

		// Update the block hue
		float blockHueTarget = data.combo [data.comboCounter].blockHue;
		blockHueTarget -= Mathf.Floor (blockHueTarget);

		data.blockHue -= Mathf.Floor (data.blockHue);

		float blockHueError = blockHueTarget - data.blockHue;
		if (blockHueError > 0.5f) {
			blockHueError -= 1.0f;
		} else if (blockHueError < -0.5f) {
			blockHueError += 1.0f;
		}
		data.blockHue += Mathf.Clamp (blockHueError, -0.01f, 0.01f);

		// Update the block hue error
		data.blockHueError = Mathf.MoveTowards (data.blockHueError, data.combo [data.comboCounter].blockHueError, 0.01f);

		// Update the block saturation
		data.blockSaturation = Mathf.MoveTowards (data.blockSaturation, data.combo [data.comboCounter].blockSaturation, 0.01f);

		// Update the shader
		Material spriteMaterial = spriteMaterials [(int) Sprites.GAME_BOARD];
		spriteMaterial.SetVector ("gameData", gameData);
		spriteMaterial.SetFloat ("hue", data.blockHue);
		spriteMaterial.SetFloat ("hueError", data.blockHueError);
		spriteMaterial.SetFloat ("saturation", data.blockSaturation);
	}

	// Update the background
	public void SpriteUpdateBackground ()
	{
		int backgroundIndex = data.backgroundOriginal ? 5 : data.comboCounter;
		Material spriteMaterial = spriteMaterials [(int) Sprites.BACKGROUND];
		if ((backgroundIndex & 1) != 0) {
			spriteMaterial.EnableKeyword ("SCENE_BIT0");
		} else {
			spriteMaterial.DisableKeyword ("SCENE_BIT0");
		}
		if ((backgroundIndex & 2) != 0) {
			spriteMaterial.EnableKeyword ("SCENE_BIT1");
		} else {
			spriteMaterial.DisableKeyword ("SCENE_BIT1");
		}
		if ((backgroundIndex & 4) != 0) {
			spriteMaterial.EnableKeyword ("SCENE_BIT2");
		} else {
			spriteMaterial.DisableKeyword ("SCENE_BIT2");
		}
	}

	// Update the time of all the sprites
	private void SpriteUpdateTime (float time)
	{
		foreach (Material spriteMaterial in spriteMaterials) {
			spriteMaterial.SetFloat ("time", time);
		}
	}

	// Update all the sprites
	private void SpriteUpdate (float time)
	{
		// Update the display of the amount of coins
		if (data.coinTimer >= 0) {
			--data.coinTimer;
			SpriteUpdateStateEnabled (Sprites.COINS, (data.coinTimer & 8) == 8);
		}

		// Update the coin slot
		SpriteUpdateStateOpened (Sprites.SLOT, data.slotOpenedStateCurrent);

		// Update the game board
		SpriteUpdateGameBoard ();

		// Update the pushed state of all the buttons
		SpriteUpdateStatePushed (Sprites.BUTTON_STACK);
		SpriteUpdateStatePushed (Sprites.BUTTON_CONTINUE);
		SpriteUpdateStatePushed (Sprites.BUTTON_GET_PRIZE);
		SpriteUpdateStatePushed (Sprites.BUTTON_TV);
		SpriteUpdateStatePushed (Sprites.BUTTON_PODIUM);

		// Update the time of all the sprites
		SpriteUpdateTime (time);
	}
}
