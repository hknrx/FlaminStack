// Nicolas Robert [Nrx]

public class GameStateMinorPrize : IGameState
{
	public void Enter (Game game)
	{
		// Launch the alarm sound effect
		game.audioSourceEffect.clip = game.audioEffectAlarm;
		game.audioSourceEffect.Play ();

		// Display the major prize
		game.SpriteUpdateStateEnabled (Game.Sprites.PRIZE_MAJOR, true);

		// Enable both the CONTINUE and GET_PRIZE buttons
		game.SpriteUpdateStateEnabled (Game.Sprites.BUTTON_CONTINUE, true);
		game.SpriteUpdateStateEnabled (Game.Sprites.BUTTON_GET_PRIZE, true);

		// Define a mask to change the state of all the other blocks of the row
		game.data.blockRowError = 127 - game.data.blockRowState [game.data.blockRowIndex];
	}

	public void Execute (Game game)
	{
		// The minor prize is blinking
		game.SpriteUpdateStateEnabled (Game.Sprites.PRIZE_MINOR, (game.data.stateTimer & 16) != 0);

		// The other blocks of the row are blinking
		if ((game.data.stateTimer & 7) == 7) {
			game.data.blockRowState [game.data.blockRowIndex] ^= game.data.blockRowError;
		}

		// Check whether the CONTINUE or GET_PRIZE buttons are tapped
		if (game.SpriteCheckTap (Game.Sprites.BUTTON_CONTINUE)) {
			game.data.stateNext = new GameStateStack ();
		} else if (game.SpriteCheckTap (Game.Sprites.BUTTON_GET_PRIZE)) {
			game.data.stateNext = new GameStateTitle ();
		}
	}

	public void Exit (Game game)
	{
		// Stop the alarm sound effect
		game.audioSourceEffect.Stop ();

		// Disable both the CONTINUE and GET_PRIZE buttons
		game.SpriteUpdateStateEnabled (Game.Sprites.BUTTON_CONTINUE, false);
		game.SpriteUpdateStateEnabled (Game.Sprites.BUTTON_GET_PRIZE, false);

		// Remove the blinking blocks
		game.data.blockRowState [game.data.blockRowIndex] &= ~game.data.blockRowError;

		// Check what the next state is
		if (game.data.stateNext is GameStateTitle) {

			// Get the prize
			game.LogicCoinAmountIncrease (game.data.combo [game.data.comboCounter].prizeMinor);

			// Handle achievements
			UnityEngine.Social.ReportProgress (GameData.achievementIdFirstStep, 100.0, success => {});
		}
	}
}
