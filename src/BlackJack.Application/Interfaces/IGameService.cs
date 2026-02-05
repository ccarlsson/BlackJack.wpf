using BlackJack.Domain;

namespace BlackJack.Application;

public interface IGameService
{
  RoundState StartNewRound(GameSettings settings, IRandomProvider randomProvider, string playerName, decimal baseBet);

  RoundState PlayerHit(RoundState state);

  RoundState PlayerStand(RoundState state);

  RoundState PlayerDoubleDown(RoundState state);

  RoundState PlayerSplit(RoundState state);

  RoundResult FinishRound(RoundState state);
}
