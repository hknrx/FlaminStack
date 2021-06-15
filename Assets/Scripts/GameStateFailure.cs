// Nicolas Robert [Nrx]

public class GameStateFailure : IGameState
{
	public void Enter (Game game)
	{
		// Check whether the game is over
		if (game.data.blockRowState [game.data.blockRowIndex] == game.data.blockRowError) {

			// Play a sound effect
			game.audioSourceEffect.PlayOneShot (game.audioEffectLose);
		}
	}

	public void Execute (Game game)
	{
		// Incorrect blocks are blinking
		if ((game.data.stateTimer & 15) == 15) {
			game.data.blockRowState [game.data.blockRowIndex] ^= game.data.blockRowError;
		}

		// Check the timer (make sure to change state right before the next blink)
		if (game.data.stateTimer >= 16 * 7 - 2) {
			game.data.stateNext = new GameStateStack ();
		}
	}

	public void Exit (Game game)
	{
		// Remove the incorrect blocks
		game.data.blockRowState [game.data.blockRowIndex] &= ~game.data.blockRowError;
	}
}
