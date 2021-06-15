// Nicolas Robert [Nrx]

public class GameStateMajorPrize : IGameState
{
	public void Enter (Game game)
	{
		// Launch the alarm sound effect
		game.audioSourceEffect.clip = game.audioEffectAlarm;
		game.audioSourceEffect.Play ();

		// Enable the GET_PRIZE button
		game.SpriteUpdateStateEnabled (Game.Sprites.BUTTON_GET_PRIZE, true);

		// Invert the state of the current row
		game.data.blockRowState [game.data.blockRowIndex] ^= 127;
	}

	public void Execute (Game game)
	{
		// The major prize is blinking
		game.SpriteUpdateStateEnabled (Game.Sprites.PRIZE_MAJOR, (game.data.stateTimer & 16) != 0);

		// Limit the speed
		if ((game.data.stateTimer & 1) == 1) {

			// Invert the state of each row, one by one
			game.data.blockRowState [game.data.blockRowIndex] ^= 127;
			if (game.data.blockRowIndex < 11) {
				++game.data.blockRowIndex;
			} else {
				game.data.blockRowIndex = 0;
			}
			game.data.blockRowState [game.data.blockRowIndex] ^= 127;
		}

		// Check whether the GET_PRIZE button is tapped
		if (game.SpriteCheckTap (Game.Sprites.BUTTON_GET_PRIZE)) {
			game.data.stateNext = new GameStateTitle ();
		}
	}

	public void Exit (Game game)
	{
		// Stop the alarm sound effect
		game.audioSourceEffect.Stop ();

		// Disable the GET_PRIZE button
		game.SpriteUpdateStateEnabled (Game.Sprites.BUTTON_GET_PRIZE, false);

		// Get the prize
		game.LogicCoinAmountIncrease (game.data.combo [game.data.comboCounter].prizeMajor);

		// Handle achievements
		UnityEngine.Social.ReportProgress (GameData.achievementIdWinner, 100.0, success => {});
		if (game.data.comboCounter == 1) {
			UnityEngine.Social.ReportProgress (GameData.achievementIdStackMaster, 100.0, success => {});
		} else if (game.data.comboCounter == 2) {
			UnityEngine.Social.ReportProgress (GameData.achievementIdGodOfStack, 100.0, success => {});
		} else if (game.data.comboCounter == 3) {
			UnityEngine.Social.ReportProgress (GameData.achievementIdDevilOfStack, 100.0, success => {});
		}

		// Update the combo counter
		if (game.data.comboCounter < 4) {
			++game.data.comboCounter;
		}

		// Display the rating text when it is appropriate
		int cointAmountRating = game.data.informationRatingDisplayedCounter == 0 ? 1000 : 15000 * game.data.informationRatingDisplayedCounter;
		if (game.data.coinAmount >= cointAmountRating && game.data.playCounter >= GameData.informationRatingDisplayPlayCount) {
			game.data.informationBannerText = GameData.informationRatingText;
			game.data.informationRatingDisplayedCounter = (game.data.coinAmount / 15000) + 1;
		}
	}
}
