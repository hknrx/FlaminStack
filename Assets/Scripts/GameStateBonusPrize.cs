// Nicolas Robert [Nrx]

public class GameStateBonusPrize : IGameState
{
	public void Enter (Game game)
	{
		// Launch the alarm sound effect
		game.audioSourceEffect.clip = game.audioEffectAlarm;
		game.audioSourceEffect.Play ();

		// Enable the GET_PRIZE button
		game.SpriteUpdateStateEnabled (Game.Sprites.BUTTON_GET_PRIZE, true);
	}

	public void Execute (Game game)
	{
		// The bonus timer is blinking
		game.SpriteUpdateStateEnabled (Game.Sprites.BONUS, (game.data.stateTimer & 16) != 0);

		// Check whether the GET_PRIZE button is tapped
		if (game.SpriteCheckTap (Game.Sprites.BUTTON_GET_PRIZE)) {
			game.data.stateNext = new GameStateBonusDisappear ();
		}
	}

	public void Exit (Game game)
	{
		// Disable the GET_PRIZE button
		game.SpriteUpdateStateEnabled (Game.Sprites.BUTTON_GET_PRIZE, false);

		// Get the prize
		game.LogicCoinAmountIncrease (game.data.bonusTimer == 1000 ? 1000 : 100);

		// Handle achievements
		UnityEngine.Social.ReportProgress (GameData.achievementIdTimeMaster, 100.0, success => {});
		if (game.data.bonusTimer == 1000) {
			UnityEngine.Social.ReportProgress (GameData.achievementIdGodOfTime, 100.0, success => {});
		}

		// Update the combo counter
		if (game.data.bonusTimer == 1000) {
			game.data.comboCounter = 3;
		}
	}
}
