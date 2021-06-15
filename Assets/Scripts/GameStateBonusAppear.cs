// Nicolas Robert [Nrx]

public class GameStateBonusAppear : IGameState
{
	public void Enter (Game game)
	{
		// Launch the alarm sound effect
		game.audioSourceEffect.clip = game.audioEffectAlarm;
		game.audioSourceEffect.Play ();

		// Display the information banner
		System.Text.StringBuilder text = new System.Text.StringBuilder ();
		if ((game.data.informationTutorialDisplayedFlags & GameData.InformationTutorialDisplayedFlags.BONUS) == 0) {
			game.data.informationTutorialDisplayedFlags |= GameData.InformationTutorialDisplayedFlags.BONUS;
			text.Append (GameData.informationTutorialTextBonus);
		} else {
			text.Append (GameData.informationBonusTextBegin);
		}
		text.Append (GameData.informationBonusTextEnd);
		game.data.informationBannerText = text.ToString ();

		// Prepare the display of the bonus title
		game.data.bonusTimer = 0;

		// Enable the bonus timer
		game.SpriteUpdateStateEnabled (Game.Sprites.BONUS, true);
	}

	public void Execute (Game game)
	{
		// Update the bonus title (make sure to update it as soon as this state is entered!)
		if ((game.data.stateTimer & 31) == 0) {
			game.SpriteUpdateDisplayedNumber (Game.Sprites.BONUS, game.data.bonusTitle [game.data.bonusTimer % game.data.bonusTitle.Length]);
			++game.data.bonusTimer;
		}

		// Show the bonus box
		game.renderBonusStateEnabled = game.data.stateTimer / 30.0f;
		if (game.renderBonusStateEnabled >= 1.0f) {

			// Enable the STACK button
			game.SpriteUpdateStateEnabled (Game.Sprites.BUTTON_STACK, true);

			// Check whether the STACK button is tapped
			if (game.SpriteCheckTap (Game.Sprites.BUTTON_STACK)) {
				game.data.stateNext = new GameStateBonusRun ();
			}
		}
	}

	public void Exit (Game game)
	{
		// Stop the alarm sound effect
		game.audioSourceEffect.Stop ();
	}
}
