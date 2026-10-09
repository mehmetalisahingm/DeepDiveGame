using System;
using System.Collections.Generic;

namespace DeepDive.Core.Contracts
{
    // P4.5-C (#132): daily orders/sponsors, the visible home/town development and the light team roles. Core only holds ids,
    // catalog data, the read shapes and the seams; the rules live in DeepDive.Living (host authority) and the money in
    // EconomyManager. Nothing here decides weather, current, night light (Utku, #131) or what a role DOES to a player (Mehmet,
    // #130): those are consumed through the seams below.
    //
    // Working values everywhere (prices, rewards, thresholds) are first numbers for the narrow balance pass of this issue, marked as such in
    // docs/reports/P4-5-LIVING-WORLD.md; the structure (ids, rules, one-active, replay safety) is the contract.

    // ---- orders and sponsors ---------------------------------------------------------------------------------------

    public enum ContractKind : byte
    {
        FishOrder = 1,
        VideoSponsor = 2
    }

    public enum ContractStatus : byte
    {
        None = 0,
        Active = 1,
        Completed = 2,
        Expired = 3
    }

    // What a contract counts. Always a HOST-verified event: a fish hand-in the economy settled, or a publication the channel
    // authority accepted. No UI click, no rename and no client report ever advances one.
    public enum ContractMeasure : byte
    {
        ItemsSold = 1,          // caught items handed in at the fish buyer (any species)
        SpeciesItemsSold = 2,   // caught items of one species
        GramsSold = 3,          // total weight handed in
        PublishNewSpecies = 4,  // a published clip that is the first recording of its species
        PublishEvent = 5,       // a published clip of a special event
        PublishQuality = 6,     // a published clip of at least a quality
        PublishNight = 7        // a published night clip (needs the world's night data, never generated until it exists)
    }

    public static class ContractIds
    {
        public const string OrderQuick = "order-fish-quick";
        public const string OrderBass = "order-fish-bass";
        public const string OrderHeavy = "order-fish-heavy";
        public const string SponsorNewSpecies = "sponsor-new-species";
        public const string SponsorEvent = "sponsor-event";
        public const string SponsorQuality = "sponsor-quality";
        public const string SponsorNight = "sponsor-night";

        public static string RewardId(int day, string templateId) => "reward:day" + day + ":" + templateId;
    }

    public readonly struct ContractTemplate
    {
        public readonly string Id;
        public readonly ContractKind Kind;
        public readonly ContractMeasure Measure;
        public readonly string Title;
        public readonly string Hint;
        public readonly string SpeciesId;     // for SpeciesItemsSold
        public readonly int Target;           // count, grams or minimum quality (see Measure)
        public readonly int Reward;           // credits, paid once
        public readonly bool RequiresNight;   // eligible only when the world says night capture exists

        public ContractTemplate(string id, ContractKind kind, ContractMeasure measure, string title, string hint, string speciesId,
            int target, int reward, bool requiresNight = false)
        {
            Id = id; Kind = kind; Measure = measure; Title = title; Hint = hint; SpeciesId = speciesId ?? string.Empty;
            Target = target; Reward = reward; RequiresNight = requiresNight;
        }
    }

    public static class ContractCatalog
    {
        // WORKING VALUES (balance pass). Three fish orders and three video sponsors are the plan's content budget; the night
        // sponsor is a fourth, RESERVED entry that stays dormant until Utku's night data exists (WORLD_SYSTEMS: the system never
        // asks for something the crew cannot reach yet).
        public static readonly IReadOnlyList<ContractTemplate> All = new[]
        {
            new ContractTemplate(ContractIds.OrderQuick, ContractKind.FishOrder, ContractMeasure.ItemsSold,
                "Hizli siparis", "Balikci tezgahina 2 av teslim et", "", 2, 80),
            new ContractTemplate(ContractIds.OrderBass, ContractKind.FishOrder, ContractMeasure.SpeciesItemsSold,
                "Levrek siparisi", "4 levrek teslim et", "sea_bass", 4, 200),
            new ContractTemplate(ContractIds.OrderHeavy, ContractKind.FishOrder, ContractMeasure.GramsSold,
                "Agir sepet", "Toplam 3 kg av teslim et", "", 3000, 120),
            new ContractTemplate(ContractIds.SponsorNewSpecies, ContractKind.VideoSponsor, ContractMeasure.PublishNewSpecies,
                "Yeni tur sponsoru", "Daha once yayinlanmamis bir turun ilk kaydini yayinla", "", 1, 150),
            new ContractTemplate(ContractIds.SponsorEvent, ContractKind.VideoSponsor, ContractMeasure.PublishEvent,
                "Olay sponsoru", "Ozel bir olay kaydini yayinla", "", 1, 180),
            new ContractTemplate(ContractIds.SponsorQuality, ContractKind.VideoSponsor, ContractMeasure.PublishQuality,
                "Kalite sponsoru", "Kalitesi 3 veya ustu bir klip yayinla", "", 3, 120),
            new ContractTemplate(ContractIds.SponsorNight, ContractKind.VideoSponsor, ContractMeasure.PublishNight,
                "Gece sponsoru", "Gece cekimi yayinla", "", 1, 220, requiresNight: true)
        };

        public static bool TryGet(string id, out ContractTemplate template)
        {
            for (var i = 0; i < All.Count; i++)
                if (string.Equals(All[i].Id, id, StringComparison.Ordinal)) { template = All[i]; return true; }
            template = default;
            return false;
        }
    }

    // One active slot (one fish order, one sponsor), as every reader sees it.
    public readonly struct ContractState
    {
        public readonly string TemplateId;
        public readonly ContractStatus Status;
        public readonly int Progress;
        public readonly int Target;
        public readonly int Reward;

        public ContractState(string templateId, ContractStatus status, int progress, int target, int reward)
        {
            TemplateId = templateId ?? string.Empty; Status = status; Progress = progress; Target = target; Reward = reward;
        }

        public bool HasContract => !string.IsNullOrEmpty(TemplateId);
    }

    public readonly struct DailyBoardState
    {
        public readonly int Day;
        public readonly ContractState Order;
        public readonly ContractState Sponsor;
        public readonly int Revision;

        public DailyBoardState(int day, ContractState order, ContractState sponsor, int revision)
        {
            Day = day; Order = order; Sponsor = sponsor; Revision = revision;
        }
    }

    // A fish hand-in the economy settled (host). The deal id is unique per hand-in, so the same hand-in can never count twice.
    public readonly struct SoldCatch
    {
        public readonly string SpeciesId;
        public readonly int WeightGrams;

        public SoldCatch(string speciesId, int weightGrams)
        {
            SpeciesId = speciesId ?? string.Empty; WeightGrams = weightGrams;
        }
    }

    // World seam (Utku, #131): what the crew can reach/see TODAY. Unbound = the conservative default: no night capture, and an
    // unrecorded species is assumed to exist. Orders/sponsors the world cannot serve are simply not generated.
    public interface IOrderWorld
    {
        bool NightCaptureAvailable { get; }
        bool UnrecordedSpeciesRemain { get; }
    }

    public static class OrderWorld
    {
        public static IOrderWorld Current { get; private set; }

        public static void Bind(IOrderWorld world) => Current = world;

        public static void Unbind(IOrderWorld world)
        {
            if (ReferenceEquals(Current, world)) Current = null;
        }
    }

    // ---- home and town development -----------------------------------------------------------------------------------

    public static class DevelopmentIds
    {
        public const string Home2 = "home-2";
        public const string TownFisher = "town-fisher";
        public const string TownShop = "town-shop";
        public const string TownDock = "town-dock";

        public static readonly IReadOnlyList<string> All = new[] { Home2, TownFisher, TownShop, TownDock };
    }

    public readonly struct DevelopmentDefinition
    {
        public readonly string Id;
        public readonly string Title;
        public readonly string Effect;
        public readonly int Price;

        public DevelopmentDefinition(string id, string title, string effect, int price)
        {
            Id = id; Title = title; Effect = effect; Price = price;
        }
    }

    public static class DevelopmentCatalog
    {
        // The effect numbers are what the OTHER systems read (DevelopmentEffects): the shared home storage grows, the fish buyer pays a bit
        // more, the equipment shop stocks one more tube tier and the harbor vendor gives a discount. One-off prices, no upkeep.
        public const int HomeStorageBonusSlots = 20;
        public const int FisherPricePercent = 10;
        public const int DockVehicleDiscountPercent = 15;
        public const string ShopUnlockedEquipmentId = "tube-3";

        // WORKING VALUES (balance pass).
        public static readonly IReadOnlyList<DevelopmentDefinition> All = new[]
        {
            new DevelopmentDefinition(DevelopmentIds.Home2, "Ev 2. seviye", "Ortak depo +20 yuva, sergi rafi", 700),
            new DevelopmentDefinition(DevelopmentIds.TownFisher, "Balikci tezgahi", "Av satisi +%10", 450),
            new DevelopmentDefinition(DevelopmentIds.TownShop, "Ekipman dukkani", "Yeni tup kademesi (Tup III)", 550),
            new DevelopmentDefinition(DevelopmentIds.TownDock, "Iskele", "Arac fiyatlari -%15", 450)
        };

        public static bool TryGet(string id, out DevelopmentDefinition definition)
        {
            for (var i = 0; i < All.Count; i++)
                if (string.Equals(All[i].Id, id, StringComparison.Ordinal)) { definition = All[i]; return true; }
            definition = default;
            return false;
        }
    }

    public readonly struct DevelopmentState
    {
        public readonly IReadOnlyList<string> OwnedIds;
        public readonly int Revision;

        public DevelopmentState(IReadOnlyList<string> ownedIds, int revision)
        {
            OwnedIds = ownedIds ?? Array.Empty<string>(); Revision = revision;
        }

        public bool Owns(string id)
        {
            for (var i = 0; i < OwnedIds.Count; i++)
                if (string.Equals(OwnedIds[i], id, StringComparison.Ordinal)) return true;
            return false;
        }

        public bool HomeLevel2 => Owns(DevelopmentIds.Home2);
    }

    // What the other systems read. Unbound (no campaign) = no development: every number is the base value.
    public static class DevelopmentEffects
    {
        private static Func<DevelopmentState> provider;

        public static void Bind(Func<DevelopmentState> state) => provider = state;

        public static void Unbind(Func<DevelopmentState> state)
        {
            if (provider == state) provider = null;
        }

        public static DevelopmentState State => provider != null ? provider() : new DevelopmentState(null, 0);

        public static int StorageBonusSlots => State.HomeLevel2 ? DevelopmentCatalog.HomeStorageBonusSlots : 0;
        public static int FishPricePercentBonus => State.Owns(DevelopmentIds.TownFisher) ? DevelopmentCatalog.FisherPricePercent : 0;
        public static int VehiclePriceDiscountPercent => State.Owns(DevelopmentIds.TownDock) ? DevelopmentCatalog.DockVehicleDiscountPercent : 0;
        public static bool ShopStockUnlocked => State.Owns(DevelopmentIds.TownShop);
    }

    // ---- roles ----------------------------------------------------------------------------------------------------
    // The role TYPE and what a role does belong to Mehmet (CrewRole / CrewRoleEffectRules, #130): small bonuses recomputed from the base
    // value, so a repeated change, a reconnect or a restore never stacks. This issue owns the free town CHOICE, its UI and its save; the
    // choice is applied to the player through CrewRoleEffectBinding.TryApplyRoleServer and nothing here touches a player stat.
    public static class CrewRoleLabels
    {
        public static string Label(CrewRole role) => role switch
        {
            CrewRole.CameraOperator => "Kameraci",
            CrewRole.Hunter => "Avci",
            CrewRole.Explorer => "Kasif",
            CrewRole.Carrier => "Tasiyici",
            _ => "Rol yok"
        };
    }

    // ---- the one campaign record ---------------------------------------------------------------------------------------

    [Serializable]
    public sealed class LivingWorldSaveData
    {
        public int Version = 1;
        public int BoardDay;
        public string OrderTemplateId = "";
        public byte OrderStatus;
        public int OrderProgress;
        public string SponsorTemplateId = "";
        public byte SponsorStatus;
        public int SponsorProgress;
        // Yesterday's templates, so the roll varies from day to day.
        public string LastOrderTemplateId = "";
        public string LastSponsorTemplateId = "";
        // Evidence ids (a hand-in deal, a publication) already counted: a replay or a reload cannot count them again.
        public List<string> UsedEvidenceIds = new List<string>();
        public List<string> DevelopmentIds = new List<string>();
        // D06: only the host's role is saved (a guest's client id is not a persistent identity).
        public byte HostRole;
    }

    public interface ILivingPersistence
    {
        LivingWorldSaveData ExportLiving();
        bool RestoreLiving(LivingWorldSaveData data);
    }
}
