using System;
using System.Collections.Generic;
using System.Formats.Asn1;
using System.Linq;
using System.Threading;

public class Weapon
{
    public string Name { get; set; }
    public int AttackBonus { get; set; }
    public string Description { get; set; }

    public Weapon(string name, int attackBonus, string description = "")
    {
        Name = name;
        AttackBonus = attackBonus;
        Description = description;
    }
}
public class Spell
{
    public string Name { get; set; }
    public int ManaBonus { get; set; }
    public string Description { get; set; }

    public Spell(string name, int manaBonus, string description = "")
    {
        Name = name;
        ManaBonus = manaBonus;
        Description = description;
    }
}

public class Consumable
{
    public string Name { get; set; }
    public int Hp { get; set; }
    public string Description { get; set; }

    public Consumable(string name, int hpRestore, string description = "")
    {
        Name = name;
        Hp = hpRestore;
        Description = description;
    }
}

public class PlayerData
{
    public string UserName { get; set; }
    public int FloorCount { get; set; }
    public int Level { get; set; }
    public int BaseAttack { get; set; }
    public int BaseMana { get; set; }
    public int Max_Base_HP { get; set; }
    public int Base_HP { get; set; }
    public int CurrentHP { get; set; }
    public int Experience { get; set; }
    public int ExperienceToNextLevel { get; set; }
    public List<Weapon> Inventory { get; set; } = new();
    public List<Spell> Spells { get; set; } = new();
    public List<Consumable> Potions { get; set; } = new();
    public bool OverlordDefeated { get; set; } = false;
    public int pX { get; set; }
    public int pY { get; set; }
    public int pPlaceX { get; set; }
    public int pPlaceY { get; set; }
    public int EncounterLimitMax { get; set; }
    public int EncounterLimitMin { get; set; }


    public int GetTotalAttack()
    {
        return BaseAttack + Inventory.Sum(w => w.AttackBonus);
    }
    public int GetTotalMana()
    {
        return BaseMana + Spells.Sum(s => s.ManaBonus);
    }
        public int GetTotalHp()
    {
        return Base_HP + Potions.Sum(p => p.Hp);
    }
        public int GetTotalMaxHp()
    {
        return Max_Base_HP + Potions.Sum(p => p.Hp);
    }
    public string GetWeaponSummary()
    {
        if (Inventory.Count == 0)
            return "None";

        return string.Join(", ", Inventory.Select(w => $"{w.Name}(+{w.AttackBonus})"));
    }

    public string GetPotionSummary()
    {
        if (Potions.Count == 0)
            return "None";

        return string.Join(", ", Potions.GroupBy(p => p.Name)
            .Select(g => $"{g.Key} x{g.Count()}"));
    }
}

public class Enemy
{
    public string Name { get; set; }
    public int HP { get; set; }
    public int Attack { get; set; }

    public Enemy(string name, int hp, int attack)
    {
        Name = name;
        HP = hp;
        Attack = attack;
    }
}

class Program
{
    static void Main(string[] args)
    {
        PlayerData player = new PlayerData {
            UserName = "Hero",
            Level = 1,
            FloorCount = 0,
            BaseAttack = 3,
            BaseMana = 1,
            Max_Base_HP = 20,
            Base_HP = 20,
            CurrentHP = 20,
            Experience = 0,
            ExperienceToNextLevel = 10,
            Inventory = new List<Weapon>
            {
                new Weapon("Disgraced Sword", 2, "A rusty blade with a tarnished reputation."),
            },
            pX = 9,
            pY = 9,
            pPlaceX = 4,
            pPlaceY = 4,
            EncounterLimitMax = 13,
            EncounterLimitMin = 7,
        };
        RunMap(player);
    }
    static char[,] GenerateMap(int width, int height)
{
    var rand = new Random();
    char[,] map = new char[height, width];

    for (int y = 0; y < height; y++)
    {
        for (int x = 0; x < width; x++)
        {
            if (y == 0 || y == height - 1 || x == 0 || x == width - 1)
            {
                map[y, x] = '#'; // border wall
            }
            else
            {
                int roll = rand.Next(100);
                if (roll < 70)
                    map[y, x] = '.';   // floor
                else if (roll < 85)
                    map[y, x] = 'E';   // encounter
                else if (roll < 95)
                    map[y, x] = 'T';   // treasure
                else
                    map[y, x] = '.';   // floor (extra chance for open space)
            }
        }
    }

    return map;
}
    static char[,] GenerateMapWithTracking(int width, int height, out int encounterCount, PlayerData player)
    {
        var rand = new Random();
        char[,] map = new char[height, width];
        encounterCount = 0;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if (y == 0 || y == height - 1 || x == 0 || x == width - 1)
                {
                    map[y, x] = '#'; // border wall
                }
                else
                {
                    int roll = rand.Next(20);
                    if (roll < 14)
                    {
                        map[y, x] = '.';   // floor
                    }
                    else if (roll > 14 && roll < 18)
                    {
                        map[y, x] = 'E';   // encounter
                        encounterCount++;
                        if (encounterCount > player.EncounterLimitMax) // Cap encounters to prevent overcrowding
                        {
                            map[y, x] = '.'; // Convert excess encounters to floor
                            encounterCount--;
                        }
                        if (map[y, x] == map[player.pPlaceY, player.pPlaceX] && map[y, x] == 'E') // Ensure starting position isn't an encounter
                        {
                            map[y, x] = '.'; // Convert to floor
                            encounterCount--;
                        }
                    }
                    else if (roll > 18)
                    {
                        map[y, x] = 'T';   // treasure
                        if (map[y, x] == map[player.pPlaceY, player.pPlaceX] && map[y, x] == 'T') // Ensure starting position isn't a treasure
                        {
                            map[y, x] = '.'; // Convert to floor
                        }
                    }
                    else
                    {
                        map[y, x] = '.';   // floor (extra chance for open space)
                        if (encounterCount < player.EncounterLimitMin) // Ensure we don't exceed encounter cap
                        {
                            map[y, x] = 'E'; // Convert excess encounters to floor
                            encounterCount++;
                        }
                    }
                }
            }
        }

        return map;
    }

    static Weapon GenerateRandomWeapon()
    {
        var rand = new Random();
        var aspects = new[]
        {
            new { Name = "Ignited ", Bonus = 2 },
            new { Name = "Frozen ", Bonus = 1 },
            new { Name = "Disgraced ", Bonus = 0 },
            new { Name = "Electric ", Bonus = 4 },
            new { Name = "Legendary ", Bonus = 5 },
            new { Name = "Shadow ", Bonus = 3 }
        };

        var weapons = new[]
        {
            new { Name = "Spear", Base = 3 },
            new { Name = "Sword", Base = 2 },
            new { Name = "Battle Axe", Base = 4 },
            new { Name = "Claymore", Base = 5 },
            new { Name = "Dagger", Base = 1 }
        };

        var aspect = aspects[rand.Next(aspects.Length)];
        var weapon = weapons[rand.Next(weapons.Length)];

        string name = aspect.Name + weapon.Name;
        int attack = weapon.Base + aspect.Bonus;

        Weapon result = new Weapon(name, attack, $"A {weapon.Name.ToLower()} imbued with {aspect.Name.ToLower().Trim()}power, granting +{attack} attack.");
        return result;
    }
    static Spell GenerateRandomSpell()
    {
        var rand = new Random();
        var aspects = new[]
        {
            new { Name = "Ignited ", Bonus = 2 },
            new { Name = "Frozen ", Bonus = 1 },
            new { Name = "Disgraced ", Bonus = 0 },
            new { Name = "Electric ", Bonus = 4 },
            new { Name = "Legendary ", Bonus = 5 },
            new { Name = "Shadow ", Bonus = 3 }
        };

        var magic = new[]
        {
            new { Name = "Wand", Base = 3 },
            new { Name = "Tome", Base = 2 },
            new { Name = "Scepter", Base = 4 },
            new { Name = "Arcane Staff", Base = 5 },
            new { Name = "Stick", Base = 1 }
        };

        var aspect = aspects[rand.Next(aspects.Length)];
        var magics = magic[rand.Next(magic.Length)];

        string name = aspect.Name + magics.Name;
        int mana = magics.Base + aspect.Bonus;

        Spell result = new Spell(name, mana, $"A {magics.Name.ToLower()} imbued with {aspect.Name.ToLower().Trim()}power, granting +{mana} mana.");
        return result;
    }

    static Consumable GenerateRandomPotion()
    {
        var rand = new Random();
        var potionTemplates = new[]
        {
            new { Name = "Minor Health Talisman", Hp = 15, Description = "Adds a small amount of HP." },
            new { Name = "Health Talisman", Hp = 30, Description = "Adds a moderate amount of HP." },
            new { Name = "Greater Health Talisman", Hp = 50, Description = "Adds a large amount of HP." },
            new { Name = "Charm of Vitality", Hp = 100, Description = "A magical charm that adds a very large chunk of HP." }
        };

        var template = potionTemplates[rand.Next(potionTemplates.Length)];
        return new Consumable(template.Name, template.Hp, template.Description);
    }
    static void RunMap(PlayerData player)
    {
        int floorNumber = 1;
        int encountersCleared = 0;
        int totalEncounters = 0;

        char[,] map = GenerateMapWithTracking(player.pX, player.pY, out totalEncounters, player);
        int playerX = player.pPlaceX;
        int playerY = player.pPlaceY;

        while (true)
        {
            Console.Clear();
            DrawMap(map, playerX, playerY);
            Console.WriteLine($"Floor {floorNumber} | Encounters: {encountersCleared}/{totalEncounters} | Total ATK Bonus: {player.GetTotalAttack() - player.BaseAttack} | Total ATK: {player.GetTotalAttack()}");
            Console.WriteLine("Move: W A S D / I inventory / Q to quit");

            string input = Console.ReadLine()?.ToUpper() ?? "";
            if (input == "Q") break;
            if (input == "I")
            {
                ShowInventory(player);
                continue;
            }

            int dx = 0;
            int dy = 0;

            if (input == "W") dy = -1;
            else if (input == "S") dy = 1;
            else if (input == "A") dx = -1;
            else if (input == "D") dx = 1;
            else if (input == "F") // cheat code for testing
            {
                encountersCleared = totalEncounters;
                continue;
            }
            else continue;

            int newX = playerX + dx;
            int newY = playerY + dy;

            if (CanMove(map, newX, newY))
            {
                playerX = newX;
                playerY = newY;

                char tile = map[playerY, playerX];

                if (tile == 'E')
                {
                    map[playerY, playerX] = '.'; // Clear the encounter
                    encountersCleared++;
                    Encounter(player, floorNumber);

                    // Check if floor is cleared
                    if (encountersCleared >= totalEncounters)
                    {
                        Console.Clear();
                        Console.WriteLine($"🎉 Floor {floorNumber} cleared!");
                        Console.WriteLine("A boss is approaching...");
                        Thread.Sleep(1500);

                        BossEncounter(player, floorNumber);

                        Console.Clear();
                        Console.WriteLine($"Boss defeated! Generating next floor...");
                        Thread.Sleep(2000);

                        floorNumber++;
                        int floorIndex = 0;
                        encountersCleared = 0;
                        if (floorNumber > 0)
                        {
                            floorIndex = floorNumber - 1;
                            if(floorNumber > 5)
                            {
                                floorIndex = 5; // Cap scaling after 5 floors to prevent it from getting out of hand
                            }
                            player.pX = 9 + (2 * floorIndex);
                            player.pY = 9 + (2 * floorIndex);
                            player.pPlaceX = 4 + (1 * floorIndex);
                            player.pPlaceY = 4 + (1 * floorIndex);
                            player.EncounterLimitMax = 13 + (1 * floorIndex);
                            player.EncounterLimitMin = 7 + (1 * floorIndex);
                        }
                        map = GenerateMapWithTracking(player.pX, player.pY, out totalEncounters, player);
                        playerX = player.pPlaceX;
                        playerY = player.pPlaceY;
                    }
                }
                else if (tile == 'T')
                {
                    map[playerY, playerX] = '.'; // Clear the treasure
                    var rand = new Random();
                    var newWeapon = GenerateRandomWeapon();
                    var newMagic = GenerateRandomSpell();
                    var newPotion = GenerateRandomPotion();
                    player.Inventory.Add(newWeapon);
                    player.Spells.Add(newMagic);
                    player.Potions.Add(newPotion);
                    Console.WriteLine($"You found a {newWeapon.Name}!");
                    Console.WriteLine($"You found a {newMagic.Name}!");
                    Console.WriteLine($"You found a {newPotion.Name}!");
                    Thread.Sleep(1500);
                }
            }
        }
    }

static void DrawMap(char[,] map, int playerX, int playerY)
{
    int rows = map.GetLength(0);
    int cols = map.GetLength(1);

    for (int y = 0; y < rows; y++)
    {
        for (int x = 0; x < cols; x++)
        {
            if (x == playerX && y == playerY)
                Console.Write('P');
            else
                Console.Write(map[y, x]);
        }
        Console.WriteLine();
    }
}

static bool CanMove(char[,] map, int x, int y)
{
    int rows = map.GetLength(0);
    int cols = map.GetLength(1);

    if (x < 0 || x >= cols || y < 0 || y >= rows)
        return false;

    return map[y, x] != '#'; // walls are '#'
}

    static void ShowInventory(PlayerData player)
    {
        bool viewingInventory = true;
        while (viewingInventory)
        {
            Console.Clear();
            Console.WriteLine("=== INVENTORY ===");
            Console.WriteLine("\nWEAPONS:");
            if (player.Inventory.Count == 0)
                Console.WriteLine("  (empty)");
            else
                for (int i = 0; i < player.Inventory.Count; i++)
                    Console.WriteLine($"  {i + 1}. {player.Inventory[i].Name} (+{player.Inventory[i].AttackBonus})");

            Console.WriteLine("\nMAGIC:");
            if (player.Spells.Count == 0)
                Console.WriteLine("  (empty)");
            else
                for (int i = 0; i < player.Spells.Count; i++)
                    Console.WriteLine($"  {i + 1}. {player.Spells[i].Name} (+{player.Spells[i].ManaBonus})");

            Console.WriteLine("\nTALISMANS:");
            if (player.Potions.Count == 0)
                Console.WriteLine("  (empty)");
            else
            {
                var potionGroups = player.Potions.GroupBy(p => p.Name).ToList();
                for (int i = 0; i < potionGroups.Count; i++)
                {
                    var group = potionGroups[i];
                    Console.WriteLine($"  {i + 1}. {group.Key} x{group.Count()} (Adds +{group.First().Hp} HP)");
                }
            }

            Console.WriteLine($"\nBase Attack: {player.BaseAttack}");
            Console.WriteLine($"Total Attack: {player.GetTotalAttack()}");
            Console.WriteLine($"HP: {player.GetTotalHp()}/{player.GetTotalMaxHp()}");
            Console.WriteLine("\nPress any key to return...");
            Console.ReadKey(true);
            viewingInventory = false;
        }
    }

    static void Encounter(PlayerData player, int floorNumber)
    {
        int totalMaxHp = player.GetTotalMaxHp();
        int totalAttack = player.GetTotalAttack();
        int totalMagic = player.GetTotalMana();
        int FireballDamage = totalMagic + (totalAttack / 2);
        int totalAttackBonus = player.GetTotalAttack() - player.BaseAttack;
        player.CurrentHP = totalMaxHp; // Heal player to full before encounter
        Random rand = new Random();
        int healsRemaining = 3;
        int turnCount = 0;

        var enemyTemplates = new[]
        {
            new { Name = "Goblin", BaseHP = 5, BaseAttack = 2 },
            new { Name = "Orc", BaseHP = 12, BaseAttack = 4 },
            new { Name = "Skeleton", BaseHP = 8, BaseAttack = 3 },
            new { Name = "Troll", BaseHP = 20, BaseAttack = 6 },
            new { Name = "Dragon", BaseHP = 35, BaseAttack = 10 }
        };

        var template = enemyTemplates[rand.Next(enemyTemplates.Length)];
        int floorScaling = floorNumber * 2;          // main difficulty driver
        int levelScaling = player.Level;             // player contribution
        int enemyHP = Math.Max(1, template.BaseHP + floorScaling + (levelScaling / 2) + rand.Next(-2, 3));
        int enemyAttack = Math.Max(1, template.BaseAttack + (floorScaling / 2) + (levelScaling / 3) + rand.Next(-1, 2));

        Enemy enemy = new Enemy(template.Name, enemyHP, enemyAttack);
        Console.WriteLine($"A wild {enemy.Name} appears!");

        while (enemy.HP > 0 && player.CurrentHP > 0)
        {
            turnCount++;
            Console.WriteLine($"\nTurn {turnCount} | Your HP: LV {player.Level} ATK {totalAttack} {player.CurrentHP}/{player.GetTotalMaxHp()} | EXP: {player.Experience}/{player.ExperienceToNextLevel} | {enemy.Name} HP: {enemy.HP}");
            Console.WriteLine($"Total Attack Bonus: {totalAttackBonus}");
            Console.WriteLine("1. Attack");
            Console.WriteLine($"2. Heal ({healsRemaining}/3 remaining)");
            Console.WriteLine("3. Spells");
            Console.WriteLine("4. Run");
            Console.Write("> ");

            string choice = Console.ReadLine();

            if (choice == "1")
            {
                enemy.HP -= totalAttack;
                Console.WriteLine($"You hit the {enemy.Name} for {totalAttack} damage!");

                if (enemy.HP > 0)
                {
                    player.CurrentHP -= enemy.Attack;
                    Console.WriteLine($"{enemy.Name} hits you for {enemy.Attack} damage!");
                }
            }
            else if (choice == "2")
            {
                if (healsRemaining > 0)
                {
                    int healAmount = 10 + (totalAttack * 2);
                    int oldHP = player.CurrentHP;
                    player.CurrentHP = Math.Min(player.CurrentHP + healAmount, player.GetTotalMaxHp());
                    int healed = player.CurrentHP - oldHP;
                    Console.WriteLine($"You healed for {healed} HP!");
                    healsRemaining--;
                    
                    if (enemy.HP > 0)
                    {
                        player.CurrentHP -= enemy.Attack;
                        Console.WriteLine($"{enemy.Name} hits you for {enemy.Attack} damage!");
                    }
                }
                else
                {
                    Console.WriteLine("You are out of heals for this encounter!");
                }
            }
            else if (choice == "3")
            {
                Console.WriteLine("Choose Your Spell to Cast!");
                Console.WriteLine("1. Fireball");
                Console.WriteLine("2. Flash Freeze");
                Console.WriteLine("3. Stormtrance");

                string spellchoice = Console.ReadLine();
                    if (spellchoice == "1")
                {
                    enemy.HP -= FireballDamage; // Magic damage is boosted by half of the player's attack bonus
                    Console.WriteLine($"You casted Fireball at the {enemy.Name} for {FireballDamage} damage!");

                    if (enemy.HP > 0 && rand.Next(100) < 50)
                    {
                        player.CurrentHP -= player.BaseMana; // Fireball has a chance to backfire and damage the player based on their mana
                        int backLash = totalAttack / 2;
                        Console.WriteLine($"The fireball backfires and burns you for {backLash} damage!");
                    }
                    if (enemy.HP > 0)
                    {
                        player.CurrentHP -= enemy.Attack;
                        Console.WriteLine($"{enemy.Name} hits you for {enemy.Attack} damage!");
                    }
                }
                    if (spellchoice == "2")
                {
                    enemy.HP -= totalMagic;
                    Console.WriteLine($"You casted Flash Freeze on the {enemy.Name} for {totalMagic} damage!");

                        if (enemy.HP > 0 && rand.Next(100) < 50) // 50% chance to freeze enemy and prevent their attack
                    {
                        Console.WriteLine($"{enemy.Name} is frozen and fails to attack!");
                    }
                        else
                    {
                        player.CurrentHP -= enemy.Attack;
                        Console.WriteLine($"{enemy.Name} hits you for {enemy.Attack} damage!");
                    }
                }
                    if (spellchoice == "3")
                {
                    int TranceDamage = enemy.Attack / 2;
                    enemy.HP -= totalMagic;
                    Console.WriteLine($"You casted Stormtrance on the {enemy.Name} for {totalMagic} damage!");

                        if (enemy.HP > 0)
                    {
                        player.CurrentHP -= TranceDamage;
                        Console.WriteLine($"{enemy.Name} hits you for {TranceDamage} damage!");
                    }
                }
            }

            else if (choice == "4")
            {
                Console.WriteLine("You ran away!");
                return;
            }
            else
            {
                Console.WriteLine("Invalid choice, try again.");
            }
        }

        if (player.GetTotalHp() <= 0)
        {
            Console.WriteLine("\nYou were defeated...");
            player.CurrentHP = player.GetTotalMaxHp();
        }
        else
        {
            if (enemy.HP <= 0){
                enemy.HP = 0; // Ensure HP doesn't go negative for display purposes
            }
            Console.WriteLine($"\nYou defeated the {enemy.Name}!");
            int expGained = CalculateExperience(enemy, player.Level);
            player.Experience += expGained;
            Console.WriteLine($"You gained {expGained} experience!");
            Random rng = new Random();
            var newWeapon = GenerateRandomWeapon();
            player.Inventory.Add(newWeapon);
            Console.WriteLine($"Enemy dropped a {newWeapon.Name}!");

            // Random chance to drop a potion
            if (rng.Next(100) < 40) // 40% chance
            {
                var newPotion = GenerateRandomPotion();
                player.Potions.Add(newPotion);
                Console.WriteLine($"Enemy dropped {newPotion.Name}!");
            }
            if (rng.Next(100) < 20) // 20% chance
            {
                var newSpell = GenerateRandomSpell();
                player.Spells.Add(newSpell);
                Console.WriteLine($"Enemy dropped {newSpell.Name}!");
            }

            CheckLevelUp(player);
        }
    }

    static int CalculateExperience(Enemy enemy, int playerLevel)
    {
        int baseExp = enemy.HP + (enemy.Attack * 2);
        int levelDiff = playerLevel - 1;
        double levelModifier = Math.Max(0.5, 1.0 + (levelDiff * 0.1));
        return (int)(baseExp * levelModifier);
    }

    static void CheckLevelUp(PlayerData player)
    {
        while (player.Experience >= player.ExperienceToNextLevel && player.Level < 50)
        {
            player.Experience -= player.ExperienceToNextLevel;
            PerformLevelUp(player);
        }
    }

    static void BossEncounter(PlayerData player, int floorNumber)
    {
        var bosses = new[]
        {
            new { Name = "Goblin King", BaseHP = 70, BaseAttack = 10, BonusExp = 80 },
            new { Name = "Troll Warlord", BaseHP = 95, BaseAttack = 14, BonusExp = 120 },
            new { Name = "Dragon Lord", BaseHP = 130, BaseAttack = 18, BonusExp = 180 },
            new { Name = "Necromancer", BaseHP = 155, BaseAttack = 22, BonusExp = 220 },
            new { Name = "Demon Overlord", BaseHP = 190, BaseAttack = 26, BonusExp = 300 }
        };

        int bossIndex = Math.Min(floorNumber - 1, bosses.Length - 1);
        var bossTemplate = bosses[bossIndex];
        int bossHP = (int)(player.GetTotalMaxHp() * 0.5) + (floorNumber * 15) + bossTemplate.BaseHP;
        int bossAttack = (int)(player.GetTotalAttack() * 0.7) + (floorNumber * 4) + bossTemplate.BaseAttack;
        Enemy boss = new Enemy(bossTemplate.Name, bossHP, bossAttack);

        Console.WriteLine($"A boss appears: {boss.Name}! HP: {bossHP}, ATK: {bossAttack}");
        Thread.Sleep(1500);

        int healsRemaining = 3;
        int turnCount = 0;
        int totalMaxHp = player.GetTotalMaxHp();
        player.CurrentHP = totalMaxHp;
        int totalAttack = player.GetTotalAttack();
        int totalMagic = player.GetTotalMana();
        int FireballDamage = totalMagic + (totalAttack / 2);
        Random rng = new Random();
        while (bossHP > 0 && player.CurrentHP > 0)
        {
            turnCount++;
            Console.WriteLine($"\nTurn {turnCount} | Boss {boss.Name}: {bossHP} HP | You: {player.CurrentHP}/{totalMaxHp} | ATK: {totalAttack}");
            Console.WriteLine("1. Attack");
            Console.WriteLine($"2. Heal ({healsRemaining}/3 remaining)");
            Console.WriteLine("3. Spells");
            Console.WriteLine("4. Run");
            Console.Write("> ");

            string choice = Console.ReadLine();
            if (choice == "1")
            {
                bossHP -= totalAttack;
                Console.WriteLine($"You hit {boss.Name} for {totalAttack} damage!");
            }
            else if (choice == "2")
            {
                if (healsRemaining > 0)
                {
                    int healAmount = 10 + (totalAttack * 2);
                    int oldHP = player.CurrentHP;
                    player.CurrentHP = Math.Min(player.CurrentHP + healAmount, totalMaxHp);
                    int healed = player.CurrentHP - oldHP;
                    Console.WriteLine($"You healed for {healed} HP!");
                    healsRemaining--;
                }
                else
                {
                    Console.WriteLine("You are out of heals for the boss fight!");
                }
            }
            else if (choice == "3")
            {
                Console.WriteLine("Choose Your Spell to Cast!");
                Console.WriteLine("1. Fireball");
                Console.WriteLine("2. Flash Freeze");
                Console.WriteLine("3. Stormtrance");

                string spellchoice = Console.ReadLine();
                    if (spellchoice == "1")
                {
                    boss.HP -= FireballDamage; // Magic damage is boosted by half of the player's attack bonus
                    Console.WriteLine($"You casted Fireball at the {boss.Name} for {FireballDamage} damage!");

                    if (bossHP > 0 && rng.Next(100) < 50)
                    {
                        player.CurrentHP -= player.BaseMana; // Fireball has a chance to backfire and damage the player based on their mana
                        int backLash = totalAttack / 2;
                        Console.WriteLine($"The fireball backfires and burns you for {backLash} damage!");
                    }
                    if (bossHP > 0)
                    {
                        player.CurrentHP -= boss.Attack;
                        Console.WriteLine($"{boss.Name} hits you for {boss.Attack} damage!");
                    }
                }
                    else if (spellchoice == "2")
                {
                    bossHP -= totalMagic;
                    Console.WriteLine($"You casted Flash Freeze on the {boss.Name} for {totalMagic} damage!");

                        if (bossHP > 0 && rng.Next(100) < 50) // 50% chance to freeze boss and prevent their attack
                    {
                        player.CurrentHP -= boss.Attack;
                        Console.WriteLine($"{boss.Name} hits you for {boss.Attack} damage!");
                    }
                        else
                    {
                        Console.WriteLine($"{boss.Name} is frozen and fails to attack!");
                    }
                }
                    else if (spellchoice == "3")
                {
                    int TranceDamage = 0;
                    if(boss.Attack % 2 == 1)
                    {
                        boss.Attack += 1; // Ensure boss attack is even for consistent damage reduction
                        TranceDamage = boss.Attack / 2;
                    }
                    else
                    {
                        TranceDamage = boss.Attack / 2;
                    }
                    bossHP -= totalMagic;
                    Console.WriteLine($"You casted Stormtrance on the {boss.Name} for {totalMagic} damage!");

                    if (bossHP > 0)
                    {
                        player.CurrentHP -= TranceDamage;
                        Console.WriteLine($"{boss.Name} hits you for {TranceDamage} damage!");
                    }
                }
                else
                {
                Console.WriteLine("Invalid choice, try again.");
                continue;
                }
            }
            else if (choice == "4")
            {
                Console.WriteLine("You can't run from a boss!");
                continue;
            }
            else
            {
                Console.WriteLine("Invalid choice, try again.");
                continue;
            }

            if (bossHP > 0 && choice != "2" && choice != "3") // Boss attacks after player's turn unless player chose to heal or use a spell (to give them a moment of respite)
            {
                player.CurrentHP -= bossAttack;
                Console.WriteLine($"{boss.Name} hits you for {bossAttack} damage!");
            }
        }

        if (player.CurrentHP <= 0)
        {
            Console.WriteLine("\nYou were defeated by the boss...");
            player.CurrentHP = player.GetTotalMaxHp();

        }
        else if (bossHP <= 0)
        {
            Console.WriteLine($"\nYou defeated {boss.Name}!");
            player.Experience += bossTemplate.BonusExp;
            Console.WriteLine($"You gained {bossTemplate.BonusExp} bonus experience!");

            var bossWeapon = GenerateRandomWeapon();
            player.Inventory.Add(bossWeapon);
            Console.WriteLine($"Boss dropped a {bossWeapon.Name}!");

            if (new Random().Next(100) < 60)
            {
                var bossPotion = GenerateRandomPotion();
                player.Potions.Add(bossPotion);
                Console.WriteLine($"You also found {bossPotion.Name}!");
            }
            if (floorNumber == 1)
            {
                Console.WriteLine("You have defeated the Goblin King and can now access the next floor!");
                Thread.Sleep(2000);
                Console.WriteLine("These monsters deserve no mercy!");
                Thread.Sleep(2000);
            }
            else if (floorNumber == 2)
            {
                Console.WriteLine("You have defeated the Troll Warlord and can now access the next floor!");
                Thread.Sleep(2000);
                Console.WriteLine("Their road is paved with sin. This is justice for those before!");
                Thread.Sleep(2000);
            }
            else if (floorNumber == 3)
            {
                Console.WriteLine("You have defeated the Dragon Lord and can now access the next floor!");
                Thread.Sleep(2000);
                Console.WriteLine("Their blood fuels your rage!");
                Thread.Sleep(2000);
            }
            else if (floorNumber == 4)
            {
                Console.WriteLine("You have defeated the Troll Warlord and can now access the next floor!");
                Thread.Sleep(2000);
                Console.WriteLine("They near their end!");
                Thread.Sleep(2000);
            }
            else if (floorNumber == 5)
            {
                player.OverlordDefeated = true;
                Console.WriteLine("You have defeated the Demon Overlord");
                Thread.Sleep(2000);
                Console.WriteLine("The source of all evil in the dungeon has been vanquished!");
                Thread.Sleep(2000);
                Console.WriteLine("Was it worth it?");
                Thread.Sleep(2000);
                Console.WriteLine("The blood your blades have spilled...");
                Thread.Sleep(2000);
                Console.WriteLine("The lives lost...");
                Thread.Sleep(2000);
                Console.WriteLine("Are you happy now?");
                Thread.Sleep(2000);
                Console.WriteLine("Are you happy with becoming the very thing you sought to destroy?");
                Thread.Sleep(2000);
            }
            CheckLevelUp(player);
            Thread.Sleep(2000);
        }
    }

    static void PerformLevelUp(PlayerData player)
    {
        player.Level++;
        
        int attackIncrease = 1 + (player.Level / 5);
        int hpIncrease = 3 + (player.Level / 3);
        
        player.BaseAttack += attackIncrease;
        player.Max_Base_HP += hpIncrease;
        player.Base_HP = player.Max_Base_HP;
        
        player.ExperienceToNextLevel = 10 + (player.Level * player.Level * 2);
        
        Console.WriteLine($"\n LEVEL UP! You are now level {player.Level}!");
        Console.WriteLine($"Attack increased by {attackIncrease} (now {player.GetTotalAttack()})");
        Console.WriteLine($"Max HP increased by {hpIncrease} (now {player.GetTotalMaxHp()})");
        Console.WriteLine($"Next level requires {player.ExperienceToNextLevel} experience");
    }
}

