// Nicolas Robert [Nrx]

public class GameStateBonusDisappear : IGameState
{
	public void Enter (Game game)
	{
		// Hide the information banner
		game.data.informationBannerText = null;

		// Disable the bonus timer
		game.SpriteUpdateStateEnabled (Game.Sprites.BONUS, false);
	}

	public void Execute (Game game)
	{
		// Hide the bonus box
		game.renderBonusStateEnabled = 1.0f - game.data.stateTimer / 30.0f;
		if (game.renderBonusStateEnabled <= 0.0f) {
			game.data.stateNext = new GameStateTitle ();
		}
	}

	public void Exit (Game game)
	{
		// Stop the alarm sound effect
		game.audioSourceEffect.Stop ();
	}
}
