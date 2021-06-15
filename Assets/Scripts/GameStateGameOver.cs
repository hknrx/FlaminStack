// Nicolas Robert [Nrx]

public class GameStateGameOver : IGameState
{
	public void Enter (Game game)
	{
		// Stop the music
		game.data.music = null;

		// Hide both prizes
		game.SpriteUpdateStateEnabled (Game.Sprites.PRIZE_MAJOR, false);
		game.SpriteUpdateStateEnabled (Game.Sprites.PRIZE_MINOR, false);
	}

	public void Execute (Game game)
	{
		// Limit the speed
		if ((game.data.stateTimer & 1) == 1) {

			// Make all the blocks fall down (row per row)
			for (int rowIndex = 11; rowIndex >= 0; --rowIndex) {
				if (game.data.blockRowState [rowIndex] != 0) {

					// Move this row down
					if (rowIndex < 11) {
						game.data.blockRowState [rowIndex + 1] = game.data.blockRowState [rowIndex];
					}

					// Clear that row
					game.data.blockRowState [rowIndex] = 0;
					return;
				}
			}

			// Once all the blocks disappeared...
			if (game.data.comboCounter < 4) {

				// Launch the bonus game
				game.data.stateNext = new GameStateBonusAppear ();
			} else {

				// Update the combo counter
				--game.data.comboCounter;

				// Go back to the title
				game.data.stateNext = new GameStateTitle ();
			}
		}
	}

	public void Exit (Game game)
	{
		// Handle achievements
		if (game.data.blockRowIndex == 10) {
			UnityEngine.Social.ReportProgress (GameData.achievementIdLoser, 100.0, success => {});
		}
		if (++game.data.achievementGameOverInARowCounter >= 10) {
			UnityEngine.Social.ReportProgress (GameData.achievementIdNoLuck, 100.0, success => {});
		}
	}
}
