using Newtonsoft.Json;

public class NobelPrize
{
    [JsonProperty("category")]
    public required Category Category { get; set; }

    [JsonProperty("laureates")]
    public required List<Laureate> Laureates { get; set; }

    [JsonProperty("awardYear")]
    public required string AwardYear { get; set; }

    [JsonProperty("prizeAmountAdjusted")]
    public int PrizeAmountAdjusted { get; set; }
}

public class Category
{
    [JsonProperty("en")]
    public required string Name { get; set; }
}

public class Laureate
{
    [JsonProperty("knownName")]
    public required KnownName KnownName { get; set; }

    [JsonProperty("orgName")]
    public required OrgName OrgName { get; set; }
}

public class KnownName
{
    [JsonProperty("en")]
    public required string Name { get; set; }
}

public class OrgName
{
    [JsonProperty("en")]
    public required string Name { get; set; }
}

public class NobelPrizeResponse
{
    [JsonProperty("nobelPrizes")]
    public required List<NobelPrize> NobelPrizes { get; set; }
}