// Nicolas Robert [Nrx]

public class GameStateBonusFailure : IGameState
{
	public void Enter (Game game)
	{
		// Stop the music
		game.data.music = null;

		// Play a sound effect
		game.audioSourceEffect.PlayOneShot (game.audioEffectLose);
	}

	public void Execute (Game game)
	{
		// The bonus timer is blinking
		game.SpriteUpdateStateEnabled (Game.Sprites.BONUS, (game.data.stateTimer & 16) == 0);

		// Check the state timer (make sure to change state right before the next blink)
		if (game.data.stateTimer >= 16 * 9 - 1) {
			game.data.stateNext = new GameStateBonusDisappear ();
		}
	}

	public void Exit (Game game)
	{
		// Handle achievements
		if (game.data.bonusTimer <= 100) {
			UnityEngine.Social.ReportProgress (GameData.achievementIdSpeedy, 100.0, success => {});
		}

		// Reset the combo counter
		game.data.comboCounter = 0;
	}
}
