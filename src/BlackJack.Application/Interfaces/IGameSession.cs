using BlackJack.Domain;

namespace BlackJack.Application;

public interface IGameSession
{
  decimal Bankroll { get; }

  decimal MinBet { get; }

  decimal MaxBet { get; }

  string Status { get; }

  RoundState? RoundState { get; }

  RoundResult? LastResult { get; }

  bool CanSplit { get; }

  bool CanDoubleDown { get; }

  bool IsDealerHoleCardHidden { get; }

  int GetDealerVisibleValue();

  IReadOnlyList<Card> GetDealerVisibleCards();

  void UpdateSettings(GameSettings settings);

  bool TryStartRound(string playerName, decimal bet, out string error);

  void Hit();

  void Stand();

  bool TryDoubleDown(out string error);

  bool TrySplit(out string error);
}
