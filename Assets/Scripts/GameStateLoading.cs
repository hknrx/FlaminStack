// Nicolas Robert [Nrx]

public class GameStateLoading : IGameState
{
	public void Enter (Game game)
	{
	}

	public void Execute (Game game)
	{
	}

	public void Exit (Game game)
	{
		// Load all player data
		game.LogicPlayerDataLoad ();

		// Initialize the display of the amount of coins
		game.SpriteUpdateDisplayedNumber (Game.Sprites.COINS, game.data.coinAmount);

		// Initialize the coin slot
		game.data.slotOpenedStateCurrent = 1.0f;

		// Initialize the game board (rows of blocks)
		game.data.blockRowState = new int [12];

		// Initialize the game board (block hue & saturation)
		game.data.blockHue = game.data.combo [game.data.comboCounter].blockHue;
		game.data.blockHueError = game.data.combo [game.data.comboCounter].blockHueError;
		game.data.blockSaturation = game.data.combo [game.data.comboCounter].blockSaturation;

		// Disable all the buttons but the PODIUM button
		game.SpriteUpdateStateEnabled (Game.Sprites.BUTTON_CONTINUE, false);
		game.SpriteUpdateStateEnabled (Game.Sprites.BUTTON_GET_PRIZE, false);
		game.SpriteUpdateStateEnabled (Game.Sprites.BUTTON_STACK, false);
		game.SpriteUpdateStateEnabled (Game.Sprites.BUTTON_TV, false);

		// Disable the bonus timer
		game.SpriteUpdateStateEnabled (Game.Sprites.BONUS, false);

		// Initialize the information banner
		game.data.informationBannerRenderer = game.informationBanner.GetComponent <UnityEngine.CanvasRenderer> ();
		game.data.informationBannerTextRenderer = game.informationBannerText.GetComponent <UnityEngine.CanvasRenderer> ();

		// Initialize the Game Center
		UnityEngine.SocialPlatforms.GameCenter.GameCenterPlatform.ShowDefaultAchievementCompletionBanner (true);
		UnityEngine.Social.localUser.Authenticate (success => {
			game.data.gameCenterReady = success;
		});
	}
}
