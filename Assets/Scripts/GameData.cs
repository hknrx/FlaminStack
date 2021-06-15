// Nicolas Robert [Nrx]

public class GameData
{
	// States
	public IGameState stateCurrent;
	public IGameState stateNext;
	public int stateTimer;

	// Background
	public bool backgroundOriginal;

	// Coins
	public const int coinAmountMinimum = 2000;
	public int coinAmount;
	public int coinTimer;

	// Coin slot
	public float slotOpenedStateCurrent;
	public float slotOpenedStateTarget;

	// Blocks
	public int [] blockRowState;
	public int blockRowIndex;
	public int blockRowError;
	public bool blockRowDirection;
	public byte [] blockTitle;
	public float blockHue;
	public float blockHueError;
	public float blockSaturation;

	// Combo system
	public int comboCounter;
	public struct Combo
	{
		public int prizeMajor;
		public int prizeMinor;
		public float blockHue;
		public float blockHueError;
		public float blockSaturation;
	}
	public readonly Combo [] combo = {
		new Combo {prizeMajor = 100, prizeMinor = 5, blockHue = 0.93f, blockHueError = 0.0f, blockSaturation = 1.0f},
		new Combo {prizeMajor = 300, prizeMinor = 3, blockHue = 0.55f, blockHueError = 0.0f, blockSaturation = 1.0f},
		new Combo {prizeMajor = 1000, prizeMinor = 2, blockHue = 0.13f, blockHueError = 0.0f, blockSaturation = 1.0f},
		new Combo {prizeMajor = 5000, prizeMinor = 1, blockHue = 0.0f, blockHueError = 1.0f, blockSaturation = 1.0f},
		new Combo {prizeMajor = 9999, prizeMinor = 0, blockHue = 0.65f, blockHueError = 0.2f, blockSaturation = 0.2f},
	};

	// Play counter
	public int playCounter;

	// Game Center
	public bool gameCenterReady;

	// Leaderboards
	public const string leaderboardIdCoinBucket = "CoinBucket";
	public const string leaderboardIdPlayedGames = "PlayedGames";

	// Achievements
	public int achievementGameOverInARowCounter;
	public const string achievementIdFirstStep = "FirstStep"; // Got a minor prize for the first time (10 points)
	public const string achievementIdWinner = "Winner"; // Got a major prize for the first time (20 points)
	public const string achievementIdStackMaster = "StackMaster"; // Got a major prize at the Frozen Stack (50 points)
	public const string achievementIdGodOfStack = "GodOfStack"; // Got a major prize at the Enlightened Stack (100 points)
	public const string achievementIdDevilOfStack = "DevilOfStack"; // Got a major prize at the Rainbow Stack (20 points)
	public const string achievementIdPocketMoney = "PocketMoney"; // Got a bucket worth 500 coins (20 points)
	public const string achievementIdOpulence = "Opulence"; // Got a bucket worth 1,000 coins (50 points)
	public const string achievementIdParadise = "Paradise"; // Got a bucket worth 5,000 coins (100 points)
	public const string achievementIdAether = "Aether"; // Got a bucket worth 10,000 coins (100 points)
	public const string achievementIdRealmOfHades = "RealmOfHades"; // Got a bucket worth 50,000 coins (100 points)
	public const string achievementIdLittleGambler = "LittleGambler"; // Played 50 games (10 points)
	public const string achievementIdSeriousGambler = "SeriousGambler"; // Played 100 games (20 points)
	public const string achievementIdAddictedGambler = "AddictedGambler"; // Played 500 games (50 points)
	public const string achievementIdCrazyGambler = "CrazyGambler"; // Played 1,000 games (100 points)
	public const string achievementIdNoLuck = "NoLuck"; // Failed 10 times in a row (20 points)
	public const string achievementIdLoser = "Loser"; // Failed at the 2nd row (20 points)
	public const string achievementIdTimeMaster = "TimeMaster"; // Stopped the bonus timer between 995 and 1005 (50 points)
	public const string achievementIdGodOfTime = "GodOfTime"; // Stopped the bonus timer on 1000 (100 points)
	public const string achievementIdSpeedy = "Speedy"; // Stopped the bonus timer before 100 (20 points)
	public const string achievementIdSleepy = "Sleepy"; // Forgot to stop the bonus timer (20 points)

	// Information banner (general)
	public float informationBannerAlpha;
	public string informationBannerText;
	public UnityEngine.CanvasRenderer informationBannerRenderer;
	public UnityEngine.CanvasRenderer informationBannerTextRenderer;

	// Information banner (tutorial)
	public enum InformationTutorialDisplayedFlags {
		COIN_SLOT = 1 << 0,
		STACK_BUTTON = 1 << 1,
		TV_BUTTON = 1 << 2,
		BONUS = 1 << 3
	}
	public InformationTutorialDisplayedFlags informationTutorialDisplayedFlags;
	public const int informationTutorialDismissPlayCount = 5;
	public const string informationTutorialTextCoinSlot = "Welcome to Flamin Stack!\n\nTap the coin slot to start the game!";
	public const string informationTutorialTextStackButton = "Tap the STACK button to pile the blocks!\n\nIf your stack reaches the 9th row, then you may claim a small reward, or choose to attempt reaching the 12th row to be awarded a big prize! Good luck!";
	public const string informationTutorialTextTvButton = "Hint! You can tap the TV button to watch a video ad and slow down the block!";
	public const string informationTutorialTextBonus = "Whenever you fail, it is BONUS TIME! Tap the STACK button to start/stop the timer!";

	// Information banner (rating)
	public const int informationRatingDisplayPlayCount = 20;
	public int informationRatingDisplayedCounter;
	public const string informationRatingText = "Well done! You've got quite a lot of coins!\n\nIf you like the game, please rate it 5 stars!\n(You can do so from the podium menu!)\n\nYour kind support is greatly appreciated!";

	// Information banner (bonus)
	public const string informationBonusTextBegin = "BONUS TIME!";
	public const string informationBonusTextEnd = "\n\nStop on 1000 = 1000 coins!\nStop between 995 and 1005 = 100 coins!";

	// Information banner (ranking)
	public int informationRankCurrent = int.MaxValue;
	public int informationRankDisplayed = int.MaxValue;
	public const string informationRankText = "You are now ranked #{0} worldwide!";

	// Advertising
	public enum Advertising {DISABLED, PROPOSED, VIEWED}
	public Advertising advertising;

	// Bonus
	public int bonusTimer;
	public readonly int [] bonusTitle = {-8766, -3644, 1000, -3334};

	// Music
	public UnityEngine.AudioClip music;
	public const float musicFadeSpeed = 0.1f;
}
