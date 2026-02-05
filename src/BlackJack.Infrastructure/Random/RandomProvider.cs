using BlackJack.Domain;

namespace BlackJack.Infrastructure;

public sealed class RandomProvider : IRandomProvider
{
  private readonly Random _random;

  public RandomProvider() : this(new Random())
  {
  }

  public RandomProvider(int seed) : this(new Random(seed))
  {
  }

  private RandomProvider(Random random)
  {
    _random = random;
  }

  public int Next(int minInclusive, int maxExclusive) => _random.Next(minInclusive, maxExclusive);
}
