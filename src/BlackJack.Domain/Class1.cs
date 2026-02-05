namespace BlackJack.Domain;

public enum Farg
{
	Hjarte,
	Ruter,
	Spader,
	Klover
}

public enum Valoer
{
	Tvaa = 2,
	Tre = 3,
	Fyra = 4,
	Fem = 5,
	Sex = 6,
	Sju = 7,
	Aatta = 8,
	Nio = 9,
	Tio = 10,
	Knekt = 11,
	Dam = 12,
	Kung = 13,
	Ess = 14
}

public sealed record Kort(Farg Farg, Valoer Valoer);

public interface ISlumpProvider
{
	int Next(int minInclusive, int maxExclusive);
}
