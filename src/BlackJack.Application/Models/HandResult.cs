namespace BlackJack.Application;

public sealed record HandResult(
  int HandIndex,
  int PlayerValue,
  int DealerValue,
  OutcomeType Outcome,
  bool PlayerBlackjack,
  bool DealerBlackjack,
  bool PlayerBust,
  bool DealerBust,
  decimal PayoutMultiplier);
