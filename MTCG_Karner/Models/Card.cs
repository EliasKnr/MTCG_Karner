namespace MTCG_Karner.Models;

public enum ElementType
{
    Fire,
    Water,
    Normal
}

public enum MonsterType
{
    Goblin,
    Dragon,
    Wizard,
    Ork,
    Knight,
    Kraken,
    FireElf
}

public class Card
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public double Damage { get; set; }
    public ElementType ElementType { get; set; }
    public MonsterType? MonsterType { get; set; } // Nullable for spell cards
    public bool Destroyed { get; set; }
}

public class MonsterCard : Card
{
    // Additional properties and methods for monster cards can be added here
}

public class SpellCard : Card
{
    // Additional properties and methods for spell cards can be added here
}