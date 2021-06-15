// Nicolas Robert [Nrx]

using UnityEngine;

public partial class Game
{
	// Game data
	[System.NonSerialized]
	public GameData data = new GameData ();

	// Game title
	public Texture2D [] titleTextures;

	// Sounds
	public AudioSource audioSourceEffect;
	public AudioSource audioSourceMusic;
	public AudioClip audioEffectAlarm;
	public AudioClip audioEffectDing;
	public AudioClip audioEffectLose;
	public AudioClip audioMusicMenu;
	public AudioClip [] audioMusicPlay;

	// Information banner
	public GameObject informationBanner;
	public UnityEngine.UI.Text informationBannerText;

	// Update the title
	public void LogicTitleUpdate ()
	{
		// Make sure we have the title texture we need
		if (data.comboCounter < titleTextures.Length) {

			// Read the appropriate title texture
			Color32 [] titlePixels = titleTextures [data.comboCounter].GetPixels32 ();
			int titlePixelIndex = titlePixels.Length;
			data.blockTitle = new byte [titleTextures [data.comboCounter].height];
			for (int titleRowIndex = 0; titleRowIndex < data.blockTitle.Length; ++titleRowIndex) {
				byte titleRowData = 0;
				for (byte bit = 1 << 6; bit != 0; bit >>= 1) {
					if (titlePixels [--titlePixelIndex].r > 0.5f) {
						titleRowData |= bit;
					}
				}
				data.blockTitle [titleRowIndex] = titleRowData;
			}
		} else if (data.blockTitle == null) {

			// Define a simple default title
			data.blockTitle = new byte [] {85, 0, 42, 0};
		}
	}

	// Get a hash corresponding to all player data
	private int LogicPlayerDataHash ()
	{
		return Animator.StringToHash (string.Format ("F{2}l{0}a{4}M{1}i{3}N",
			data.coinAmount,
			data.comboCounter,
			data.playCounter,
			data.achievementGameOverInARowCounter,
			data.informationRatingDisplayedCounter));
	}

	// Load all player data
	public void LogicPlayerDataLoad ()
	{
		data.coinAmount = PlayerPrefs.GetInt ("K1");
		data.comboCounter = PlayerPrefs.GetInt ("K2");
		data.playCounter = PlayerPrefs.GetInt ("K3");
		data.achievementGameOverInARowCounter = PlayerPrefs.GetInt ("K4");
		data.informationRatingDisplayedCounter = PlayerPrefs.GetInt ("K5");
		if (PlayerPrefs.GetInt ("K6") != LogicPlayerDataHash ()) {

			// Delete all existing keys and values
			PlayerPrefs.DeleteAll ();

			// Reset the values
			data.coinAmount = 100;
			data.comboCounter = 0;
			data.playCounter = 0;
			data.achievementGameOverInARowCounter = 0;
			data.informationRatingDisplayedCounter = 0;
		}
	}

	// Save all player data
	public void LogicPlayerDataSave ()
	{
		PlayerPrefs.SetInt ("K1", data.coinAmount);
		PlayerPrefs.SetInt ("K2", data.comboCounter);
		PlayerPrefs.SetInt ("K3", data.playCounter);
		PlayerPrefs.SetInt ("K4", data.achievementGameOverInARowCounter);
		PlayerPrefs.SetInt ("K5", data.informationRatingDisplayedCounter);
		PlayerPrefs.SetInt ("K6", LogicPlayerDataHash ());
	}

	// Update the music
	public void LogicMusicUpdate ()
	{
		data.music = audioMusicPlay [data.playCounter % audioMusicPlay.Length];
	}

	// Increase the amount of coins
	public void LogicCoinAmountIncrease (int value)
	{
		// Increase the amount of coins
		data.coinAmount += value;
		if (data.coinAmount < 0) {
			data.coinAmount = 0;
		} else if (data.coinAmount > 9999999) {
			data.coinAmount = 9999999;
		}
		SpriteUpdateDisplayedNumber (Sprites.COINS, data.coinAmount);

		// Arm the timer
		data.coinTimer = 8 * 9;

		// Make sure the amount of coins was increased
		if (value > 0) {

			// Update the coin bucket leaderboard
			UnityEngine.Social.ReportScore (data.coinAmount, GameData.leaderboardIdCoinBucket, firstSuccess => {

				// Get the rank of the local user in the coin bucket leaderboard
				UnityEngine.SocialPlatforms.ILeaderboard leaderboard = UnityEngine.Social.CreateLeaderboard ();
				leaderboard.id = GameData.leaderboardIdCoinBucket;
				leaderboard.LoadScores (secondSuccess => {
					if (secondSuccess) {
						if (leaderboard.localUserScore.rank > 0) {
							data.informationRankCurrent = leaderboard.localUserScore.rank;
						}
						if (leaderboard.localUserScore.value - GameData.coinAmountMinimum > data.coinAmount) {
							data.coinAmount = (int) leaderboard.localUserScore.value - (GameData.coinAmountMinimum >> 1);
							LogicPlayerDataSave ();
						}
					}

					// Get the actual number of played games from the played games leaderboard
					leaderboard = UnityEngine.Social.CreateLeaderboard ();
					leaderboard.id = GameData.leaderboardIdPlayedGames;
					leaderboard.LoadScores (thirdSuccess => {
						if (thirdSuccess && leaderboard.localUserScore.value > data.playCounter) {
							data.playCounter = (int) leaderboard.localUserScore.value;
							LogicPlayerDataSave ();
						}
					});
				});
			});

			// Handle achievements
			data.achievementGameOverInARowCounter = 0;
			UnityEngine.Social.ReportProgress (GameData.achievementIdPocketMoney, 100.0 * data.coinAmount / 500, success => {});
			UnityEngine.Social.ReportProgress (GameData.achievementIdOpulence, 100.0 * data.coinAmount / 1000, success => {});
			UnityEngine.Social.ReportProgress (GameData.achievementIdParadise, 100.0 * data.coinAmount / 5000, success => {});
			UnityEngine.Social.ReportProgress (GameData.achievementIdAether, 100.0 * data.coinAmount / 10000, success => {});
			UnityEngine.Social.ReportProgress (GameData.achievementIdRealmOfHades, 100.0 * data.coinAmount / 50000, success => {});
		}
	}

	// Initialize the game logic
	private void LogicInitialize ()
	{
		// Initialize the state machine
		data.stateCurrent = new GameStateLoading ();
		data.stateNext = new GameStateTitle ();
	}

	// Update the game logic
	private void LogicUpdate ()
	{
		// Check for taps on all the sprites
		SpriteCheckTap ();

		// Check whether there is a change of state
		if (data.stateCurrent != data.stateNext) {

			// Exit the current state
			data.stateCurrent.Exit (this);

			// Reset the state timer
			data.stateTimer = 0;

			// Enter the new state
			data.stateCurrent = data.stateNext;
			data.stateCurrent.Enter (this);
		}

		// Execute the current state
		data.stateCurrent.Execute (this);

		// Update the state timer
		++data.stateTimer;

		// Open/close the coin slot
		data.slotOpenedStateCurrent = Mathf.MoveTowards (data.slotOpenedStateCurrent, data.slotOpenedStateTarget, 0.1f);

		// Show/hide the information banner
		data.informationBannerAlpha = Mathf.MoveTowards (data.informationBannerAlpha, data.informationBannerText == informationBannerText.text && data.informationBannerText !=null ? 1.0f : 0.0f, 0.05f);
		data.informationBannerRenderer.SetAlpha (data.informationBannerAlpha);
		data.informationBannerTextRenderer.SetAlpha (data.informationBannerAlpha);
		if (data.informationBannerAlpha == 0.0f) {
			informationBannerText.text = data.informationBannerText;
		}

		// Handle the music
		if (data.music == null) {
			if (audioSourceMusic.isPlaying) {
				audioSourceMusic.Stop ();
			}
		} else if (audioSourceMusic.clip == data.music) {
			if (!audioSourceMusic.isPlaying) {
				audioSourceMusic.Play ();
			} else if (audioSourceMusic.volume < 1.0f) {
				audioSourceMusic.volume += GameData.musicFadeSpeed;
			}
		} else if (audioSourceMusic.isPlaying && audioSourceMusic.volume > 0.0f) {
			audioSourceMusic.volume -= GameData.musicFadeSpeed;
		} else {
			audioSourceMusic.clip = data.music;
		}

		// Handle manual changes of the background
		if (SpriteCheckTap (Game.Sprites.COINS)) {
			data.backgroundOriginal = !data.backgroundOriginal;
			SpriteUpdateBackground ();
		}
	}

	// Method to call when the game is paused or resumed (to be called from "OnApplicationPause")
	private void LogicPause (bool pause)
	{
		// Check whether the game is being resumed
		if (!pause) {

			// Check whether an ad was just displayed
			if (data.advertising != GameData.Advertising.VIEWED) {

				// Let's display the ranking again
				data.informationRankCurrent = data.informationRankDisplayed = int.MaxValue;

				// Adapt the rendering scale once again
				frameTimer = 0.0f;
				frameCount = 0;
			}
		}
	}
}
