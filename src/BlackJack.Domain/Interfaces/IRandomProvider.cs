namespace BlackJack.Domain;

public interface IRandomProvider
{
  int Next(int minInclusive, int maxExclusive);
}
