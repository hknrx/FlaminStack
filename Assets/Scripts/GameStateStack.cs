// Nicolas Robert [Nrx]

public class GameStateStack : IGameState
{
	public void Enter (Game game)
	{
		// Count the number of blocks in the current row
		int blockCount;
		if (game.data.blockRowIndex < 12) {
			int blockRowStateCurrent = game.data.blockRowState [game.data.blockRowIndex];
			for (blockCount = 0; blockRowStateCurrent != 0; ++blockCount) {
				blockRowStateCurrent &= blockRowStateCurrent - 1;
			}
		} else {
			blockCount = 7;
		}

		// Check whether the game can continue or not
		if (blockCount == 0) {

			// Game over!
			game.data.stateNext = new GameStateGameOver ();
		} else {

			// Move up 1 row
			--game.data.blockRowIndex;

			// Compute the maximum number of blocks for the new row
			int blockCountMax = (game.data.blockRowIndex >> 2) + 1;
			if (blockCount > blockCountMax) {
				blockCount = blockCountMax;
			}

			// Initialize the state of the new row
			game.data.blockRowDirection = UnityEngine.Random.value < 0.5f;
			game.data.blockRowState [game.data.blockRowIndex] = (1 << blockCount) - 1;
			if (game.data.blockRowDirection) {
				game.data.blockRowState [game.data.blockRowIndex] <<= 7 - blockCount;
			}

			// Let's move
			game.data.stateNext = new GameStateMove ();
		}
	}

	public void Execute (Game game)
	{
	}

	public void Exit (Game game)
	{
	}
}
