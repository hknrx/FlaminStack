// Nicolas Robert [Nrx]

public class GameStateTitle : IGameState
{
	public void Enter (Game game)
	{
		// Launch the menu music
		game.data.music = game.audioMusicMenu;

		// Update the background
		game.SpriteUpdateBackground ();

		// Update the title
		game.LogicTitleUpdate ();

		// Update and display both prizes
		game.SpriteUpdateDisplayedNumber (Game.Sprites.PRIZE_MAJOR, game.data.combo [game.data.comboCounter].prizeMajor);
		game.SpriteUpdateDisplayedNumber (Game.Sprites.PRIZE_MINOR, game.data.combo [game.data.comboCounter].prizeMinor);
		game.SpriteUpdateStateEnabled (Game.Sprites.PRIZE_MAJOR, true);
		game.SpriteUpdateStateEnabled (Game.Sprites.PRIZE_MINOR, true);

		// Open the coin slot
		game.data.slotOpenedStateTarget = 1.0f;

		// Prepare the display of the title
		game.data.blockRowIndex = 0;

		// Save all player data
		game.LogicPlayerDataSave ();
	}

	public void Execute (Game game)
	{
		// Enable the PODIUM button if the Game Center is ready
		game.SpriteUpdateStateEnabled (Game.Sprites.BUTTON_PODIUM, game.data.gameCenterReady);

		// The coin slot is blinking
		game.SpriteUpdateStateEnabled (Game.Sprites.SLOT, (game.data.stateTimer & 16) != 0);

		// Display the title
		if (game.data.stateTimer % 3 == 0) {

			// Scroll all the blocks up
			for (int rowIndex = 0; rowIndex < 11; ++rowIndex) {
				game.data.blockRowState [rowIndex] = game.data.blockRowState [rowIndex + 1];
			}

			// Display a row of the title on the bottom row
			game.data.blockRowState [11] = game.data.blockTitle [game.data.blockRowIndex % game.data.blockTitle.Length];
			++game.data.blockRowIndex;
		}

		// Handle the information banner
		if (game.data.informationBannerText == null) {
			if ((game.data.informationTutorialDisplayedFlags & GameData.InformationTutorialDisplayedFlags.COIN_SLOT) == 0) {

				// Display the tutorial
				game.data.informationTutorialDisplayedFlags |= GameData.InformationTutorialDisplayedFlags.COIN_SLOT;
				game.data.informationBannerText = GameData.informationTutorialTextCoinSlot;
			} else if (game.data.informationRankCurrent != game.data.informationRankDisplayed) {

				// Display the rank of the local user in the coin bucket leaderboard
				game.data.informationRankDisplayed = game.data.informationRankCurrent;
				game.data.informationBannerText = string.Format (GameData.informationRankText, game.data.informationRankCurrent);
			}
		}

		// Handle the taps
		if (game.SpriteCheckTap (Game.Sprites.BUTTON_PODIUM)) {

			// The PODIUM button is tapped, show the leaderboard if the Game Center is ready
			if (game.data.gameCenterReady) {
				UnityEngine.Social.ShowLeaderboardUI ();
			}
		} else if (game.data.slotOpenedStateCurrent > 0.9f && game.SpriteCheckTap (Game.Sprites.SLOT)) {

			// The coin slot is opened and tapped, start the game
			game.data.stateNext = new GameStateStack ();
		} else if ((game.data.informationBannerText != GameData.informationTutorialTextCoinSlot || game.data.playCounter >= GameData.informationTutorialDismissPlayCount) && game.SpriteCheckTapExclusive (Game.Sprites.BACKGROUND, Game.Sprites.GAME_BOARD)) {

			// The background or game board is tapped, hide the information banner
			game.data.informationBannerText = null;
		}
	}

	public void Exit (Game game)
	{
		// Insert a coin
		game.LogicCoinAmountIncrease (-1);

		// Update the play counter
		++game.data.playCounter;

		// Handle achievements
		UnityEngine.Social.ReportProgress (GameData.achievementIdLittleGambler, 100.0 * game.data.playCounter / 50, success => {});
		UnityEngine.Social.ReportProgress (GameData.achievementIdSeriousGambler, 100.0 * game.data.playCounter / 100, success => {});
		UnityEngine.Social.ReportProgress (GameData.achievementIdAddictedGambler, 100.0 * game.data.playCounter / 500, success => {});
		UnityEngine.Social.ReportProgress (GameData.achievementIdCrazyGambler, 100.0 * game.data.playCounter / 1000, success => {});

		// Update the leaderboard
		UnityEngine.Social.ReportScore (game.data.playCounter, GameData.leaderboardIdPlayedGames, success => {});

		// Disable the PODIUM button
		game.SpriteUpdateStateEnabled (Game.Sprites.BUTTON_PODIUM, false);

		// Close and disable the coin slot
		game.SpriteUpdateStateEnabled (Game.Sprites.SLOT, false);
		game.data.slotOpenedStateTarget = 0.0f;

		// Clear the game board
		for (int rowIndex = 0; rowIndex < 12; ++rowIndex) {
			game.data.blockRowState [rowIndex] = 0;
		}

		// Initialize the row index
		game.data.blockRowIndex = 12;

		// Launch the game music
		game.LogicMusicUpdate ();

		// Hide the information banner (and take note that the tutorial does not need to be displayed again)
		game.data.informationTutorialDisplayedFlags |= GameData.InformationTutorialDisplayedFlags.COIN_SLOT;
		game.data.informationBannerText = null;

		// Initialize the advertising
		game.data.advertising = GameData.Advertising.DISABLED;

		// Save all player data
		game.LogicPlayerDataSave ();
	}
}
