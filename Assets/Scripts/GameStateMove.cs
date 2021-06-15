// Nicolas Robert [Nrx]

public class GameStateMove : IGameState
{
	public void Enter (Game game)
	{
		// Display the appropriate prize
		game.SpriteUpdateStateEnabled (Game.Sprites.PRIZE_MAJOR, game.data.blockRowIndex < 3);
		game.SpriteUpdateStateEnabled (Game.Sprites.PRIZE_MINOR, game.data.blockRowIndex >= 3);

		// Enable the STACK button
		game.SpriteUpdateStateEnabled (Game.Sprites.BUTTON_STACK, true);

		// Handle the advertising
		if (game.data.blockRowIndex == 2 && UnityEngine.Random.value < 0.5f && UnityEngine.Advertisements.Advertisement.IsReady ()) {
			game.data.advertising = GameData.Advertising.PROPOSED;
			game.SpriteUpdateStateEnabled (Game.Sprites.BUTTON_TV, true);
		}

		// Handle the tutorial
		if ((game.data.informationTutorialDisplayedFlags & GameData.InformationTutorialDisplayedFlags.STACK_BUTTON) == 0 && game.data.blockRowIndex == 11) {
			game.data.informationTutorialDisplayedFlags |= GameData.InformationTutorialDisplayedFlags.STACK_BUTTON;
			game.data.informationBannerText = GameData.informationTutorialTextStackButton;
		} else if ((game.data.informationTutorialDisplayedFlags & GameData.InformationTutorialDisplayedFlags.TV_BUTTON) == 0 && game.data.advertising == GameData.Advertising.PROPOSED) {
			game.data.informationTutorialDisplayedFlags |= GameData.InformationTutorialDisplayedFlags.TV_BUTTON;
			game.data.informationBannerText = GameData.informationTutorialTextTvButton;
		}
	}

	public void Execute (Game game)
	{
		// Define the moving speed
		int moveTimer = 2 + ((3 * game.data.blockRowIndex) >> 2);
		if (game.data.advertising == GameData.Advertising.VIEWED) {
			moveTimer += 8;
		}

		// Limit the speed
		if (game.data.stateTimer > moveTimer) {

			// Move the blocks
			if (game.data.blockRowDirection) {
				game.data.blockRowState [game.data.blockRowIndex] >>= 1;
				game.data.blockRowDirection = (game.data.blockRowState [game.data.blockRowIndex] & 1) == 0;
			} else {
				game.data.blockRowState [game.data.blockRowIndex] <<= 1;
				game.data.blockRowDirection = (game.data.blockRowState [game.data.blockRowIndex] & 64) != 0;
			}

			// Rearm the timer
			game.data.stateTimer = 0;
		}

		// Handle the taps
		if (game.SpriteCheckTap (Game.Sprites.BUTTON_STACK)) {

			// The STACK button is tapped, play a sound effect
			game.audioSourceEffect.PlayOneShot (game.audioEffectDing);

			// Check whether the blocks are well aligned
			if (game.data.blockRowIndex < 11) {
				game.data.blockRowError = game.data.blockRowState [game.data.blockRowIndex] & ~game.data.blockRowState [game.data.blockRowIndex + 1];
			} else {
				game.data.blockRowError = 0;
			}

			// Define the next state
			if (game.data.blockRowError != 0) {
				game.data.stateNext = new GameStateFailure ();
			} else if (game.data.blockRowIndex == 0) {
				game.data.stateNext = new GameStateMajorPrize ();
			} else if (game.data.blockRowIndex == 3) {
				game.data.stateNext = new GameStateMinorPrize ();
			} else {
				game.data.stateNext = new GameStateStack ();
			}
		} else if (game.data.advertising == GameData.Advertising.PROPOSED && game.SpriteCheckTap (Game.Sprites.BUTTON_TV)) {

			// Hide the tutorial
			game.data.informationBannerText = null;

			// Show an ad
			UnityEngine.Advertisements.ShowOptions showOptions = new UnityEngine.Advertisements.ShowOptions ();
			showOptions.resultCallback = (result) => {
				if (result == UnityEngine.Advertisements.ShowResult.Finished) {
					game.data.advertising = GameData.Advertising.VIEWED;
					game.SpriteUpdateStateEnabled (Game.Sprites.BUTTON_TV, false);
				}
			};
			UnityEngine.Advertisements.Advertisement.Show (null, showOptions);
		} else if (game.data.informationBannerText != null && game.data.playCounter > GameData.informationTutorialDismissPlayCount && game.SpriteCheckTapExclusive (Game.Sprites.BACKGROUND, Game.Sprites.GAME_BOARD)) {

			// Hide the tutorial
			game.data.informationBannerText = null;
		}
	}

	public void Exit (Game game)
	{
		// Check what the next state is
		if (!(game.data.stateNext is GameStateStack)) {

			// Disable the STACK button
			game.SpriteUpdateStateEnabled (Game.Sprites.BUTTON_STACK, false);
		}

		// Handle the advertising
		if (game.data.advertising == GameData.Advertising.PROPOSED) {
			game.data.advertising = GameData.Advertising.DISABLED;
			game.SpriteUpdateStateEnabled (Game.Sprites.BUTTON_TV, false);
		}

		// Hide the tutorial
		game.data.informationBannerText = null;
	}
}
