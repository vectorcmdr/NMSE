using NMSE.Data;
using NMSE.Models;

namespace NMSE.Core;

/// <summary>The item tabs that use the unified catalogue item list.</summary>
internal enum CatalogueItemKind
{
	/// <summary>Known technology, backed by KnownTech; seen state uses SeenTechnologies.</summary>
	Technology,

	/// <summary>Known products, backed by KnownProducts; seen state uses SeenProducts.</summary>
	Product,

	/// <summary>Known specials, backed by KnownSpecials (and KnownProducts for buildings); seen state uses SeenProducts.</summary>
	Special,
}

/// <summary>One row in a unified catalogue item list.</summary>
internal sealed class CatalogueItemEntry
{
	/// <summary>Creates an item entry.</summary>
	/// <param name="id">The normalised item ID.</param>
	/// <param name="item">The resolved game item, or null for an unknown legacy ID.</param>
	/// <param name="isCatalogue">Whether the item belongs to the completion set.</param>
	/// <param name="expeditionNumber">The expedition/season number, or null.</param>
	/// <param name="canAddKnown">Whether the tab may add this item to its save-known arrays.</param>
	internal CatalogueItemEntry(
		string id,
		GameItem? item,
		bool isCatalogue,
		int? expeditionNumber,
		bool canAddKnown)
	{
		Id = id;
		Item = item;
		IsCatalogue = isCatalogue;
		ExpeditionNumber = expeditionNumber;
		CanAddKnown = canAddKnown;
		Name = item?.Name ?? id;
		Category = item?.ItemType ?? string.Empty;
		Description = item?.Description ?? string.Empty;
	}

	/// <summary>The normalised item ID.</summary>
	internal string Id { get; }

	/// <summary>The resolved game item, or null when the item database has no entry.</summary>
	internal GameItem? Item { get; }

	/// <summary>True when the item belongs to the completion set for this tab.</summary>
	internal bool IsCatalogue { get; }

	/// <summary>
	/// True when the tab's Known action may add this item. Legacy known-product entries
	/// for non-building SpecialShop items remain visible and removable but are not added.
	/// </summary>
	internal bool CanAddKnown { get; }

	/// <summary>Display name, falling back to the ID.</summary>
	internal string Name { get; }

	/// <summary>Display category, falling back to an empty string.</summary>
	internal string Category { get; }

	/// <summary>Full item description, falling back to an empty string.</summary>
	internal string Description { get; }

	/// <summary>Expedition/season number for expedition rewards, or null.</summary>
	internal int? ExpeditionNumber { get; }
}

/// <summary>
/// Builds the unified item rows for one catalogue tab. The normal Known action writes
/// the save-known arrays and marks the item seen; the advanced Seen action can clear
/// the account entry independently.
/// </summary>
internal sealed class CatalogueItemCatalog
{
	private readonly JsonObject _playerState;
	private readonly JsonObject? _userSettings;
	private readonly GameItemDatabase _items;
	private readonly CatalogueDatabase _catalogue;
	private readonly CatalogueItemKind _kind;

	private readonly HashSet<string> _knownTech = new(StringComparer.OrdinalIgnoreCase);
	private readonly HashSet<string> _knownProducts = new(StringComparer.OrdinalIgnoreCase);
	private readonly HashSet<string> _knownSpecials = new(StringComparer.OrdinalIgnoreCase);
	private readonly HashSet<string> _seenTechnologies = new(StringComparer.OrdinalIgnoreCase);
	private readonly HashSet<string> _seenProducts = new(StringComparer.OrdinalIgnoreCase);
	private readonly HashSet<string> _catalogueIds = new(StringComparer.OrdinalIgnoreCase);
	private readonly Dictionary<string, int> _expeditionNumbers = new(StringComparer.OrdinalIgnoreCase);
	private readonly List<CatalogueItemEntry> _entries = new();

	/// <summary>Creates a catalogue item catalog for one tab.</summary>
	/// <param name="kind">The item kind.</param>
	/// <param name="playerState">The save PlayerStateData object.</param>
	/// <param name="userSettings">The account UserSettingsData object, or null.</param>
	/// <param name="items">The game item database.</param>
	/// <param name="catalogue">The catalogue completion data.</param>
	internal CatalogueItemCatalog(
		CatalogueItemKind kind,
		JsonObject playerState,
		JsonObject? userSettings,
		GameItemDatabase items,
		CatalogueDatabase catalogue)
	{
		_kind = kind;
		_playerState = playerState;
		_userSettings = userSettings;
		_items = items;
		_catalogue = catalogue;

		BuildCompletionSet();
		BuildExpeditionMap();
		BuildEntries();
		RefreshKnownSets();
	}

	/// <summary>The rows for this catalogue tab, sorted by category and name.</summary>
	internal IReadOnlyList<CatalogueItemEntry> Entries => _entries;

	/// <summary>True when account data is available for Seen and Unlocked state.</summary>
	internal bool HasAccount => _userSettings != null;

	/// <summary>The save array path used by the tab's GOTO JSON action.</summary>
	internal string[] JsonPath => _kind switch
	{
		CatalogueItemKind.Technology => ["PlayerStateData", "KnownTech"],
		CatalogueItemKind.Product => ["PlayerStateData", "KnownProducts"],
		_ => ["PlayerStateData", "KnownSpecials"],
	};

	/// <summary>Recomputes the known and seen sets from the current JSON objects.</summary>
	internal void RefreshKnownSets()
	{
		LoadSet(_knownTech, "KnownTech");
		LoadSet(_knownProducts, "KnownProducts");
		LoadSet(_knownSpecials, "KnownSpecials");
		LoadSeenSet(_seenTechnologies, "SeenTechnologies");
		LoadSeenSet(_seenProducts, "SeenProducts");
	}

	/// <summary>Returns true when every required save-known array contains the item.</summary>
	/// <param name="entry">The item entry.</param>
	internal bool IsKnown(CatalogueItemEntry entry)
	{
		foreach (string arrayName in GetSaveArrayNames(entry))
		{
			if (!GetSaveSet(arrayName).Contains(entry.Id)) return false;
		}
		return true;
	}

	/// <summary>Returns true when at least one, but not every, required save array contains the item.</summary>
	/// <param name="entry">The item entry.</param>
	internal bool IsPartiallyKnown(CatalogueItemEntry entry)
	{
		if (IsKnown(entry)) return false;

		foreach (string arrayName in GetSaveArrayNames(entry))
		{
			if (GetSaveSet(arrayName).Contains(entry.Id)) return true;
		}
		return false;
	}

	/// <summary>Returns true when the account Seen array contains the item.</summary>
	/// <param name="entry">The item entry.</param>
	internal bool IsSeen(CatalogueItemEntry entry)
	{
		if (_userSettings == null) return false;
		return GetSeenSet(GetSeenArrayName()).Contains(entry.Id);
	}

	/// <summary>Returns true when the game's catalogue would count the item as visible.</summary>
	/// <param name="entry">The item entry.</param>
	internal bool IsCatalogueVisible(CatalogueItemEntry entry) =>
		ItemUnlockRules.IsCatalogueVisible(
			_kind,
			_knownTech.Contains(entry.Id),
			_knownProducts.Contains(entry.Id),
			_seenProducts.Contains(entry.Id));

	/// <summary>
	/// Sets or clears the save-known state for one entry. Adding also marks the account
	/// Seen entry; removing leaves Seen intact so the advanced Seen control can unsee.
	/// </summary>
	/// <param name="entry">The item entry.</param>
	/// <param name="known">True to add it to every required save array, false to remove it.</param>
	/// <returns>True when the save-known state changed.</returns>
	internal bool SetKnown(CatalogueItemEntry entry, bool known)
	{
		if (known && !entry.CanAddKnown) return false;

		bool before = IsKnown(entry);
		ApplySaveState(entry, known);
		if (known)
		{
			ApplySeenState(entry, true);
			AddUnlockedSpecial(entry);
		}
		return before != IsKnown(entry);
	}

	/// <summary>Sets or clears the account Seen state for one entry.</summary>
	/// <param name="entry">The item entry.</param>
	/// <param name="seen">True to add it to the account seen array, false to remove it.</param>
	/// <returns>True when the Seen state changed.</returns>
	internal bool SetSeen(CatalogueItemEntry entry, bool seen)
	{
		bool before = IsSeen(entry);
		UpdateSeenArray(
			GetSeenArrayName(),
			GetSeenSet(GetSeenArrayName()),
			new HashSet<string>(StringComparer.OrdinalIgnoreCase) { entry.Id },
			seen);
		return before != IsSeen(entry);
	}

	/// <summary>
	/// Sets or clears the save-known state for many entries in one pass. Adding also
	/// marks each entry seen; removing leaves Seen intact.
	/// </summary>
	/// <param name="entries">The entries to change.</param>
	/// <param name="known">True to add them to every required save array, false to remove them.</param>
	/// <returns>The number of entries whose save-known state changed.</returns>
	internal int SetKnown(IEnumerable<CatalogueItemEntry> entries, bool known)
	{
		var tech = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		var products = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		var specials = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		var unlocked = new List<string>();
		int changed = 0;

		foreach (var entry in entries)
		{
			if (known && !entry.CanAddKnown) continue;

			bool full = IsKnown(entry);
			bool any = full || IsPartiallyKnown(entry);
			if (known ? full : !any) continue;
			changed++;

			foreach (string arrayName in GetSaveArrayNames(entry))
				TargetSet(arrayName, tech, products, specials).Add(entry.Id);

			if (known)
			{
				seen.Add(entry.Id);
				if (IsSpecialRedeem(entry)) unlocked.Add(entry.Id);
			}
		}

		if (changed == 0) return 0;

		UpdateSaveArray("KnownTech", _knownTech, tech, known);
		UpdateSaveArray("KnownProducts", _knownProducts, products, known);
		UpdateSaveArray("KnownSpecials", _knownSpecials, specials, known);
		if (known)
		{
			UpdateSeenArray(GetSeenArrayName(), GetSeenSet(GetSeenArrayName()), seen, true);
			AddUnlockedSpecials(unlocked);
		}
		return changed;
	}

	/// <summary>Sets or clears the account Seen state for many entries in one pass.</summary>
	/// <param name="entries">The entries to change.</param>
	/// <param name="seen">True to add them to the account Seen array, false to remove them.</param>
	/// <returns>The number of entries whose Seen state changed.</returns>
	internal int SetSeen(IEnumerable<CatalogueItemEntry> entries, bool seen)
	{
		var targets = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		int changed = 0;
		foreach (var entry in entries)
		{
			bool current = IsSeen(entry);
			if (seen == current) continue;
			changed++;
			targets.Add(entry.Id);
		}
		if (changed == 0) return 0;

		UpdateSeenArray(GetSeenArrayName(), GetSeenSet(GetSeenArrayName()), targets, seen);
		return changed;
	}

	/// <summary>Returns the catalogue-counted progress for this tab.</summary>
	internal (int Have, int Total) GetCatalogueCompletion()
	{
		int have = 0, total = 0;
		foreach (var entry in _entries)
		{
			if (!entry.IsCatalogue) continue;
			total++;
			if (IsCatalogueVisible(entry)) have++;
		}
		return (have, total);
	}

	/// <summary>Returns the number of rows whose save-known state is complete.</summary>
	internal int GetKnownCount()
	{
		int count = 0;
		foreach (var entry in _entries)
			if (IsKnown(entry)) count++;
		return count;
	}

	/// <summary>Returns the number of rows whose account Seen state is set.</summary>
	internal int GetSeenCount()
	{
		int count = 0;
		foreach (var entry in _entries)
			if (IsSeen(entry)) count++;
		return count;
	}

	private void BuildCompletionSet()
	{
		IReadOnlyList<string> ids = _kind switch
		{
			CatalogueItemKind.Technology => _catalogue.KnownTechForCompletion(_items),
			CatalogueItemKind.Product => _catalogue.KnownProductsForCompletion(_items),
			_ => _catalogue.KnownSpecialsForCompletion(_items),
		};

		foreach (string raw in ids)
		{
			string id = CatalogueCompletionLogic.NormalizeId(raw);
			if (id.Length > 0) _catalogueIds.Add(id);
		}
	}

	private void BuildExpeditionMap()
	{
		if (!RewardDatabase.SeasonRewards.Any())
			RewardDatabase.LoadFromJsonDirectory(_catalogue.JsonDirectory);

		foreach (var reward in RewardDatabase.SeasonRewards)
		{
			if (reward.SeasonId <= 0) continue;

			string productId = CatalogueCompletionLogic.NormalizeId(reward.ProductId);
			if (productId.Length > 0) _expeditionNumbers[productId] = reward.SeasonId;

			string rewardId = CatalogueCompletionLogic.NormalizeId(reward.Id);
			if (rewardId.Length > 0) _expeditionNumbers[rewardId] = reward.SeasonId;
		}
	}

	private void BuildEntries()
	{
		var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		foreach (var item in _items.Items.Values)
		{
			if (IsValid(item)) ids.Add(item.Id);
		}

		foreach (string id in _catalogueIds) ids.Add(id);
		foreach (string id in LoadSaveIds(GetPrimarySaveArray())) ids.Add(id);

		foreach (string id in ids)
		{
			var item = _items.GetItem(id);
			int? expedition = _expeditionNumbers.TryGetValue(id, out int season) ? season : null;
			bool canAddKnown = _kind != CatalogueItemKind.Product
				|| ItemUnlockRules.CanLearnProduct(item);
			_entries.Add(new CatalogueItemEntry(
				id, item, _catalogueIds.Contains(id), expedition, canAddKnown));
		}

		_entries.Sort(static (left, right) =>
		{
			bool leftUncategorised = string.IsNullOrEmpty(left.Category);
			bool rightUncategorised = string.IsNullOrEmpty(right.Category);
			if (leftUncategorised != rightUncategorised)
				return leftUncategorised ? 1 : -1;

			int category = string.Compare(left.Category, right.Category, StringComparison.OrdinalIgnoreCase);
			if (category != 0) return category;
			int name = string.Compare(left.Name, right.Name, StringComparison.OrdinalIgnoreCase);
			return name != 0 ? name : string.Compare(left.Id, right.Id, StringComparison.OrdinalIgnoreCase);
		});
	}

	private bool IsValid(GameItem item)
	{
		if (_kind == CatalogueItemKind.Technology && GameItemDatabase.IsPickerExcluded(item.Id))
			return false;

		if (_kind == CatalogueItemKind.Product
			&& GameItemDatabase.IsPickerExcluded(item.Id)
			&& !ItemUnlockRules.IsSpecialShop(item))
		{
			return false;
		}

		return _kind switch
		{
			CatalogueItemKind.Technology =>
				CatalogueLogic.TechItemTypes.Contains(item.ItemType)
				&& CatalogueLogic.IsLearnableTechnology(item),
			CatalogueItemKind.Product =>
				CatalogueLogic.ProductItemTypes.Contains(item.ItemType)
				&& CatalogueLogic.IsLearnableProduct(item)
				&& ItemUnlockRules.CanLearnProduct(item),
			_ => ItemUnlockRules.IsSpecialShop(item),
		};
	}

	private string GetPrimarySaveArray() => _kind switch
	{
		CatalogueItemKind.Technology => "KnownTech",
		CatalogueItemKind.Product => "KnownProducts",
		_ => "KnownSpecials",
	};

	private IReadOnlyList<string> GetSaveArrayNames(CatalogueItemEntry entry) =>
		ItemUnlockRules.GetKnownSaveArrays(_kind, entry.Item);

	private string GetSeenArrayName() => ItemUnlockRules.GetSeenAccountArray(_kind);

	private bool IsSpecialRedeem(CatalogueItemEntry entry) =>
		_kind == CatalogueItemKind.Special && ItemUnlockRules.IsSpecialShop(entry.Item);

	private HashSet<string> GetSaveSet(string arrayName) => arrayName switch
	{
		"KnownTech" => _knownTech,
		"KnownProducts" => _knownProducts,
		_ => _knownSpecials,
	};

	private HashSet<string> GetSeenSet(string arrayName) =>
		arrayName == "SeenTechnologies" ? _seenTechnologies : _seenProducts;

	private static HashSet<string> TargetSet(
		string arrayName,
		HashSet<string> tech,
		HashSet<string> products,
		HashSet<string> specials) => arrayName switch
	{
		"KnownTech" => tech,
		"KnownProducts" => products,
		_ => specials,
	};

	private void ApplySaveState(CatalogueItemEntry entry, bool known)
	{
		foreach (string arrayName in GetSaveArrayNames(entry))
		{
			UpdateSaveArray(
				arrayName,
				GetSaveSet(arrayName),
				new HashSet<string>(StringComparer.OrdinalIgnoreCase) { entry.Id },
				known);
		}
	}

	private void ApplySeenState(CatalogueItemEntry entry, bool seen)
	{
		UpdateSeenArray(
			GetSeenArrayName(),
			GetSeenSet(GetSeenArrayName()),
			new HashSet<string>(StringComparer.OrdinalIgnoreCase) { entry.Id },
			seen);
	}

	private void AddUnlockedSpecial(CatalogueItemEntry entry)
	{
		if (IsSpecialRedeem(entry)) AddUnlockedSpecials([entry.Id]);
	}

	private void AddUnlockedSpecials(IEnumerable<string> ids)
	{
		if (_userSettings == null) return;

		var array = _userSettings.GetArray("UnlockedSpecials");
		if (array == null)
		{
			array = new JsonArray();
			_userSettings.Set("UnlockedSpecials", array);
		}

		var present = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		for (int i = 0; i < array.Length; i++)
		{
			if (array.GetString(i) is string value)
				present.Add(CatalogueCompletionLogic.NormalizeId(value));
		}

		foreach (string raw in ids)
		{
			string id = CatalogueCompletionLogic.NormalizeId(raw);
			if (id.Length == 0 || !present.Add(id)) continue;
			array.Add(id);
		}
	}

	private void UpdateSaveArray(string arrayName, HashSet<string> set, HashSet<string> targets, bool known)
	{
		if (targets.Count == 0) return;

		var array = _playerState.GetArray(arrayName);
		if (array == null)
		{
			array = new JsonArray();
			_playerState.Set(arrayName, array);
		}

		if (known)
		{
			foreach (string id in targets)
			{
				if (set.Add(id)) array.Add("^" + id);
			}
			return;
		}

		for (int i = array.Length - 1; i >= 0; i--)
		{
			string id = array.GetString(i) ?? string.Empty;
			if (!targets.Contains(CatalogueCompletionLogic.NormalizeId(id))) continue;
			array.RemoveAt(i);
			set.Remove(CatalogueCompletionLogic.NormalizeId(id));
		}
	}

	private void UpdateSeenArray(string arrayName, HashSet<string> set, HashSet<string> targets, bool known)
	{
		if (_userSettings == null || targets.Count == 0) return;

		var array = _userSettings.GetArray(arrayName);
		if (array == null)
		{
			array = new JsonArray();
			_userSettings.Set(arrayName, array);
		}

		if (known)
		{
			foreach (string id in targets)
			{
				if (set.Add(id)) array.Add("^" + id);
			}
			return;
		}

		for (int i = array.Length - 1; i >= 0; i--)
		{
			string id = array.GetString(i) ?? string.Empty;
			if (!targets.Contains(CatalogueCompletionLogic.NormalizeId(id))) continue;
			array.RemoveAt(i);
			set.Remove(CatalogueCompletionLogic.NormalizeId(id));
		}
	}

	private IReadOnlyList<string> LoadSaveIds(string arrayName) =>
		CatalogueLogic.LoadKnownItemIds(_playerState, arrayName);

	private void LoadSet(HashSet<string> destination, string arrayName)
	{
		destination.Clear();
		foreach (string id in CatalogueLogic.LoadKnownItemIds(_playerState, arrayName))
			destination.Add(id);
	}

	private void LoadSeenSet(HashSet<string> destination, string arrayName)
	{
		destination.Clear();
		if (_userSettings == null) return;
		foreach (string id in CatalogueCompletionLogic.GetSeenIdSet(_userSettings, arrayName))
			destination.Add(id);
	}
}
