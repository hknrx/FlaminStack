// Nicolas Robert [Nrx]

public class GameStateBonusRun : IGameState
{
	private float timerStart;
	private const float timerDurationAcceleration = 3.0f;
	private const float timerDurationStable = 12.0f;
	private const float timerTriggerDeceleration = timerDurationAcceleration + timerDurationStable;
	private const float timerTriggerStop = timerTriggerDeceleration + timerDurationAcceleration;

	public void Enter (Game game)
	{
		// Launch the game music
		game.LogicMusicUpdate ();

		// Take note of the current time
		timerStart = UnityEngine.Time.realtimeSinceStartup;
	}

	public void Execute (Game game)
	{
		// Update the bonus timer
		float timerValue = UnityEngine.Time.realtimeSinceStartup - timerStart;
		if (timerValue < timerDurationAcceleration) {
			timerValue = timerValue * timerValue / (2.0f * timerDurationAcceleration);
		} else if (timerValue < timerTriggerDeceleration) {
			timerValue -= timerDurationAcceleration * 0.5f;
		} else if (timerValue < timerTriggerStop) {
			timerValue = timerTriggerStop - timerValue;
			timerValue = timerValue * timerValue / (2.0f * timerDurationAcceleration);
			timerValue = timerTriggerDeceleration - timerValue;
		} else {
			timerValue = timerTriggerDeceleration;

			// Handle achievements
			UnityEngine.Social.ReportProgress (GameData.achievementIdSleepy, 100.0, success => {});

			// Define the next state
			game.data.stateNext = new GameStateBonusFailure ();
		}
		game.data.bonusTimer = (int) (timerValue * 100.0f);
		game.SpriteUpdateDisplayedNumber (Game.Sprites.BONUS, game.data.bonusTimer);

		// Make sure the bonus timer can reach precisely 1000 with the given settings!
		#if UNITY_EDITOR
		if (game.data.bonusTimer >= 995 && game.data.bonusTimer <= 1005) {
			UnityEngine.Debug.Log (System.String.Format ("Timer = {0} at {1:F2}", game.data.bonusTimer, UnityEngine.Time.realtimeSinceStartup - timerStart));
		}
		#endif

		// Check whether the STACK button is tapped
		if (game.SpriteCheckTap (Game.Sprites.BUTTON_STACK)) {

			// Play a sound effect
			game.audioSourceEffect.PlayOneShot (game.audioEffectDing);

			// Define the next state
			if (game.data.bonusTimer >= 995 && game.data.bonusTimer <= 1005) {
				game.data.stateNext = new GameStateBonusPrize ();
			} else {
				game.data.stateNext = new GameStateBonusFailure ();
			}
		}
	}

	public void Exit (Game game)
	{
		// Disable the STACK button
		game.SpriteUpdateStateEnabled (Game.Sprites.BUTTON_STACK, false);
	}
}
