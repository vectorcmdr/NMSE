using NMSE.Data;
using NMSE.Models;

namespace NMSE.Core;

internal static partial class CatalogueCompletionLogic
{
	/// <summary>
	/// Returns the number of catalogue completion entries that are still missing.
	/// Technology follows the save-known list; products and specials count when either
	/// the save-known array or the account Seen array contains them. Raw materials use
	/// the account SeenSubstances list.
	/// </summary>
	/// <param name="playerState">The save PlayerStateData object.</param>
	/// <param name="userSettings">The account UserSettingsData object, or null.</param>
	/// <param name="items">The game item database.</param>
	/// <param name="catalogue">The catalogue completion data.</param>
	internal static int CountMissingCatalogue(
		JsonObject playerState,
		JsonObject? userSettings,
		GameItemDatabase items,
		CatalogueDatabase catalogue)
	{
		var tech = catalogue.KnownTechForCompletion(items);
		var products = catalogue.KnownProductsForCompletion(items);
		var specials = catalogue.KnownSpecialsForCompletion(items);

		var (haveTech, totalTech) = GetCompletion(playerState, "KnownTech", tech);
		var (haveProducts, totalProducts) = GetKnownOrSeenCompletion(
			playerState, "KnownProducts", userSettings, "SeenProducts", products);
		var (haveSpecials, totalSpecials) = GetKnownOrSeenCompletion(
			playerState, "KnownProducts", userSettings, "SeenProducts", specials);

		int missing = (totalTech - haveTech) + (totalProducts - haveProducts) + (totalSpecials - haveSpecials);
		if (userSettings != null)
		{
			var (haveSubstances, totalSubstances) =
				GetSeenCompletion(userSettings, "SeenSubstances", catalogue.SeenSubstances);
			missing += totalSubstances - haveSubstances;
		}
		return missing;
	}

	/// <summary>
	/// Completes the catalogue across technology, products, specials and raw materials.
	/// Save-known writes follow the game's item paths: KnownTech for technology,
	/// KnownProducts for products, KnownSpecials for specials, and both known arrays for
	/// SpecialShop buildings. Account Seen lists complete the in-game wiki visibility and
	/// include SeenSubstances for the Materials page.
	/// </summary>
	/// <param name="playerState">The save PlayerStateData object.</param>
	/// <param name="userSettings">The account UserSettingsData object, or null.</param>
	/// <param name="items">The game item database.</param>
	/// <param name="catalogue">The catalogue completion data.</param>
	/// <returns>The number of IDs added to save and account arrays.</returns>
	internal static int CompleteCatalogue(
		JsonObject playerState,
		JsonObject? userSettings,
		GameItemDatabase items,
		CatalogueDatabase catalogue)
	{
		var tech = catalogue.KnownTechForCompletion(items);
		var products = catalogue.KnownProductsForCompletion(items);
		var specials = catalogue.KnownSpecialsForCompletion(items);

		int added = AddMissingIds(playerState, "KnownTech", tech);
		added += AddMissingIds(playerState, "KnownProducts", products);
		added += AddMissingIds(playerState, "KnownSpecials", specials);

		// A SpecialShop building is tracked in both arrays by the game. The special
		// completion set can contain buildings that are not in the product catalogue
		// list, so add those product entries explicitly.
		var buildingSpecials = specials
			.Where(id => items.GetItem(id)?.IsBuilding == true)
			.ToList();
		added += AddMissingIds(playerState, "KnownProducts", buildingSpecials);

		if (userSettings != null)
		{
			added += AddMissingSeen(userSettings, "SeenTechnologies", catalogue.KnownTech);
			added += AddMissingSeen(userSettings, "SeenProducts", products);
			added += AddMissingSeen(userSettings, "SeenProducts", specials);
			added += AddMissingSeen(userSettings, "SeenSubstances", catalogue.SeenSubstances);
		}

		return added;
	}

	/// <summary>
	/// Counts pack entries present in either the save array or the account Seen array.
	/// This matches the wiki's product check: a product counts when either its recipe is
	/// known or its account entry has been seen.
	/// </summary>
	private static (int Have, int Total) GetKnownOrSeenCompletion(
		JsonObject playerState,
		string arrayName,
		JsonObject? userSettings,
		string accountArrayName,
		IReadOnlyList<string> packIds)
	{
		var known = GetCurrentIdSet(playerState, arrayName);
		HashSet<string>? seen = userSettings == null
			? null
			: GetSeenIdSet(userSettings, accountArrayName);

		int have = 0, total = 0;
		var unique = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		foreach (string raw in packIds)
		{
			string id = NormalizeId(raw);
			if (id.Length == 0 || !unique.Add(id)) continue;
			total++;
			if (known.Contains(id) || (seen != null && seen.Contains(id))) have++;
		}
		return (have, total);
	}
}
