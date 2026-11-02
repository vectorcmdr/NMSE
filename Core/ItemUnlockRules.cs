using NMSE.Data;

namespace NMSE.Core;

/// <summary>
/// Canonical item-to-array rules derived from the game's known and seen item paths.
/// These rules are shared by the catalogue UI, completion actions and reward syncing
/// so the editor does not invent game state.
/// </summary>
internal static class ItemUnlockRules
{
	/// <summary>Returns true when the item is sold through the SpecialShop trade category.</summary>
	/// <param name="item">The resolved game item, or null.</param>
	internal static bool IsSpecialShop(GameItem? item) =>
		string.Equals(item?.TradeCategory, "SpecialShop", StringComparison.OrdinalIgnoreCase);

	/// <summary>
	/// Returns true for SpecialShop items that are not base-building objects. These are
	/// tracked in <c>KnownSpecials</c> only; the game never adds them to
	/// <c>KnownProducts</c> through the special redemption path.
	/// </summary>
	/// <param name="item">The resolved game item, or null.</param>
	internal static bool IsSpecialOnlyProduct(GameItem? item) =>
		IsSpecialShop(item) && item?.IsBuilding != true;

	/// <summary>
	/// Returns true when the item can be added through the Product tab's
	/// <c>KnownProducts</c> action. SpecialShop cosmetics and trails are not products.
	/// </summary>
	/// <param name="item">The resolved game item, or null.</param>
	internal static bool CanLearnProduct(GameItem? item) =>
		item != null && !IsSpecialOnlyProduct(item);

	/// <summary>
	/// Returns the save arrays written by the item action for a catalogue tab.
	/// Technology uses <c>KnownTech</c>; Product uses <c>KnownProducts</c>; Special
	/// uses <c>KnownSpecials</c> and, for buildings, <c>KnownProducts</c> as well.
	/// </summary>
	/// <param name="kind">The catalogue tab/item kind.</param>
	/// <param name="item">The resolved game item, or null for a legacy ID.</param>
	internal static IReadOnlyList<string> GetKnownSaveArrays(CatalogueItemKind kind, GameItem? item)
	{
		return kind switch
		{
			CatalogueItemKind.Technology => ["KnownTech"],
			CatalogueItemKind.Product => ["KnownProducts"],
			_ => item?.IsBuilding == true
				? ["KnownSpecials", "KnownProducts"]
				: ["KnownSpecials"],
		};
	}

	/// <summary>Returns the account Seen array used by the catalogue tab.</summary>
	/// <param name="kind">The catalogue tab/item kind.</param>
	internal static string GetSeenAccountArray(CatalogueItemKind kind) =>
		kind == CatalogueItemKind.Technology ? "SeenTechnologies" : "SeenProducts";

	/// <summary>
	/// Returns true when the game's catalogue checks consider the item visible.
	/// Technologies use the save-known list; products and specials use the seen list
	/// or the product recipe list, matching the wiki checks.
	/// </summary>
	/// <param name="kind">The catalogue tab/item kind.</param>
	/// <param name="knownTech">Whether the item is in <c>KnownTech</c>.</param>
	/// <param name="knownProduct">Whether the item is in <c>KnownProducts</c>.</param>
	/// <param name="seenProduct">Whether the item is in <c>SeenProducts</c>.</param>
	internal static bool IsCatalogueVisible(
		CatalogueItemKind kind,
		bool knownTech,
		bool knownProduct,
		bool seenProduct) =>
		kind == CatalogueItemKind.Technology
			? knownTech
			: knownProduct || seenProduct;
}
