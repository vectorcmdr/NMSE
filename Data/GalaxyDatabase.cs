using System.Drawing;

namespace NMSE.Data;

/// <summary>All 256 NMS galaxies with their type classification.</summary>
public static class GalaxyDatabase
{
    /// <summary>
    /// Array of galaxy metadata used for UI display and game lookup.
    /// </summary>
    /// <remarks>
    /// The first 256 entries are the actual in-game galaxies, indexed from 0 .. 255.
    /// A final sentinel entry exists at index 256 for UI/listing purposes only.
    /// This fake entry should never be treated as a real galaxy for coordinate or
    /// portal hex generation.
    /// </remarks>
    public static readonly (int Number, string Hex, string Name, string Type, string Description, string Core)[] Galaxies =
    [
		(1,"00","Euclid","Normal","","White"),
		(2,"01","Hilbert Dimension","Normal","Imperfect","Deep Pink"),
		(3,"02","Calypso","Harsh","Raging","Medium Orchid"),
		(4,"03","Hesperius Dimension","Normal","","Violet"),
		(5,"04","Hyades","Normal","","Light Blue"),
		(6,"05","Ickjamatew","Normal","","Turquoise"),
		(7,"06","Budullangr","Empty","Ancestral","Green"),
		(8,"07","Kikolgallr","Normal","","Lime Green"),
		(9,"08","Eltiensleen","Normal","","Yellow"),
		(10,"09","Eissentam","Lush","Halcyon","Orange"),
		(11,"0A","Elkupalos","Normal","Imperfect","Indigo"),
		(12,"0B","Aptarkaba","Empty","Ancestral","Sky Blue"),
		(13,"0C","Ontiniangp","Normal","","Cyan"),
		(14,"0D","Odiwagiri","Normal","","Dark Green"),
		(15,"0E","Ogtialabi","Harsh","","Pale Green"),
		(16,"0F","Muhacksonto","Normal","","Pale Yellow"),
		(17,"10","Hitonskyer","Normal","","Pale Orange"),
		(18,"11","Rerasmutul","Normal","","Pale Red"),
		(19,"12","Isdoraijung","Lush","","Fuchsia"),
		(20,"13","Doctinawyra","Normal","","Purple"),
		(21,"14","Loychazinq","Normal","","Dark Green"),
		(22,"15","Zukasizawa","Normal","","Light Green"),
		(23,"16","Ekwathore","Harsh","","Pale Yellow"),
		(24,"17","Yeberhahne","Normal","","Pale Orange"),
		(25,"18","Twerbetek","Normal","","Red"),
		(26,"19","Sivarates","Normal","","Magenta"),
		(27,"1A","Eajerandal","Empty","","Purple"),
		(28,"1B","Aldukesci","Normal","","Violet"),
		(29,"1C","Wotyarogii","Normal","","Royal Blue"),
		(30,"1D","Sudzerbal","Lush","","Turquoise"),
		(31,"1E","Maupenzhay","Normal","","Light Orange"),
		(32,"1F","Sugueziume","Empty","","Pale Red"),
		(33,"20","Brogoweldian","Normal","","Light Fuchsia"),
		(34,"21","Ehbogdenbu","Normal","","Light Burgundy"),
		(35,"22","Ijsenufryos","Harsh","","Indigo"),
		(36,"23","Nipikulha","Normal","","Light Blue"),
		(37,"24","Autsurabin","Normal","","Turquoise"),
		(38,"25","Lusontrygiamh","Normal","","Sea Green"),
		(39,"26","Rewmanawa","Lush","","Lime Green"),
		(40,"27","Ethiophodhe","Normal","","Yellow"),
		(41,"28","Urastrykle","Normal","","Burgundy"),
		(42,"29","Xobeurindj","Normal","","Indigo"),
		(43,"2A","Oniijialdu","Harsh","","Light Blue"),
		(44,"2B","Wucetosucc","Normal","","Turquoise"),
		(45,"2C","Ebyeloof","Normal","","Light Green"),
		(46,"2D","Odyavanta","Normal","","Lime Green"),
		(47,"2E","Milekistri","Empty","","Yellow"),
		(48,"2F","Waferganh","Normal","","Light Orange"),
		(49,"30","Agnusopwit","Normal","","Red"),
		(50,"31","Teyaypilny","Lush","","Magenta"),
		(51,"32","Zalienkosm","Normal","","Electric Blue"),
		(52,"33","Ladgudiraf","Empty","","Light Green"),
		(53,"34","Mushonponte","Normal","","Light Green"),
		(54,"35","Amsentisz","Normal","","Canary Yellow"),
		(55,"36","Fladiselm","Harsh","","Manhattan Orange"),
		(56,"37","Laanawemb","Normal","Improved","Pink"),
		(57,"38","Ilkerloor","Normal","","Magenta"),
		(58,"39","Davanossi","Normal","","Medium Orchid"),
		(59,"3A","Ploehrliou","Lush","Tranquil","Medium Slate Blue"),
		(60,"3B","Corpinyaya","Normal","","Maya Blue"),
		(61,"3C","Leckandmeram","Normal","","Canary Yellow"),
		(62,"3D","Quulngais","Normal","","Mandys Pink"),
		(63,"3E","Nokokipsechl","Harsh","Burning","Red"),
		(64,"3F","Rinblodesa","Normal","","Magenta"),
		(65,"40","Loydporpen","Normal","","Medium Orchid"),
		(66,"41","Ibtrevskip","Normal","","Medium Slate Blue"),
		(67,"42","Elkowaldb","Empty","Ancestral","Maya Blue"),
		(68,"43","Heholhofsko","Normal","","Turquoise Blue"),
		(69,"44","Yebrilowisod","Normal","","Aquamarine"),
		(70,"45","Husalvangewi","Lush","","Light Green"),
		(71,"46","Ovna'uesed","Normal","","Orchid"),
		(72,"47","Bahibusey","Empty","","Medium Orchid"),
		(73,"48","Nuybeliaure","Normal","","Medium Slate Blue"),
		(74,"49","Doshawchuc","Normal","","Cornflower Blue"),
		(75,"4A","Ruckinarkh","Harsh","","Turquoise Blue"),
		(76,"4B","Thorettac","Normal","","Aquamarine"),
		(77,"4C","Nuponoparau","Normal","","Light Green"),
		(78,"4D","Moglaschil","Normal","","Canary Yellow"),
		(79,"4E","Uiweupose","Lush","","Manhattan Orange"),
		(80,"4F","Nasmilete","Normal","","Pink"),
		(81,"50","Ekdaluskin","Normal","","Cornflower Blue"),
		(82,"51","Hakapanasy","Normal","","Electric Blue"),
		(83,"52","Dimonimba","Harsh","","Aquamarine"),
		(84,"53","Cajaccari","Normal","","Mint Green"),
		(85,"54","Olonerovo","Normal","","Yellow"),
		(86,"55","Umlanswick","Normal","","Orange"),
		(87,"56","Henayliszm","Empty","","Pink"),
		(88,"57","Utzenmate","Normal","","Magenta"),
		(89,"58","Umirpaiya","Normal","","Medium Orchid"),
		(90,"59","Paholiang","Lush","","Medium Slate Blue"),
		(91,"5A","Iaereznika","Normal","","Mint Green"),
		(92,"5B","Yudukagath","Empty","","Yellow"),
		(93,"5C","Boealalosnj","Normal","","Manhattan Orange"),
		(94,"5D","Yaevarcko","Normal","","Salmon"),
		(95,"5E","Coellosipp","Harsh","","Neon Pink"),
		(96,"5F","Wayndohalou","Normal","","Medium Orchid"),
		(97,"60","Smoduraykl","Normal","","Medium Slate Blue"),
		(98,"61","Apmaneessu","Normal","","Maya Blue"),
		(99,"62","Hicanpaav","Lush","","Electric Blue"),
		(100,"63","Akvasanta","Normal","","Aquamarine"),
		(101,"64","Tuychelisaor","Normal","","Light Coral"),
		(102,"65","Rivskimbe","Normal","","Magenta"),
		(103,"66","Daksanquix","Harsh","","Medium Orchid"),
		(104,"67","Kissonlin","Normal","","Light Slate Blue"),
		(105,"68","Aediabiel","Normal","","Maya Blue"),
		(106,"69","Ulosaginyik","Normal","","Electric Blue"),
		(107,"6A","Roclaytonycar","Empty","","Aquamarine"),
		(108,"6B","Kichiaroa","Normal","","Mint Green"),
		(109,"6C","Irceauffey","Normal","","Canary Yellow"),
		(110,"6D","Nudquathsenfe","Lush","","Manhattan Orange"),
		(111,"6E","Getaizakaal","Normal","","Medium Slate Blue"),
		(112,"6F","Hansolmien","Empty","","Cornflower Blue"),
		(113,"70","Bloytisagra","Normal","","Turquoise Blue"),
		(114,"71","Ladsenlay","Normal","","Light Green"),
		(115,"72","Luyugoslasr","Harsh","","Mint Green"),
		(116,"73","Ubredhatk","Normal","","Canary Yellow"),
		(117,"74","Cidoniana","Normal","","Manhattan Orange"),
		(118,"75","Jasinessa","Normal","","Light Coral"),
		(119,"76","Torweierf","Lush","","Magenta"),
		(120,"77","Saffneckm","Normal","","Dark Orchid"),
		(121,"78","Thnistner","Normal","","Aquamarine"),
		(122,"79","Dotusingg","Normal","","Pale Green"),
		(123,"7A","Luleukous","Harsh","","Canary Yellow"),
		(124,"7B","Jelmandan","Normal","","Peach Orange"),
		(125,"7C","Otimanaso","Normal","","Pink"),
		(126,"7D","Enjaxusanto","Normal","","Fuchsia Pink"),
		(127,"7E","Sezviktorew","Empty","","Blue Violet"),
		(128,"7F","Zikehpm","Normal","","Medium Slate Blue"),
		(129,"80","Bephembah","Normal","","Maya Blue"),
		(130,"81","Broomerrai","Lush","","Electric Blue"),
		(131,"82","Meximicka","Normal","","Manhattan Orange"),
		(132,"83","Venessika","Empty","","Light Red"),
		(133,"84","Gaiteseling","Normal","","Magenta"),
		(134,"85","Zosakasiro","Normal","","Medium Orchid"),
		(135,"86","Drajayanes","Harsh","","Medium Slate Blue"),
		(136,"87","Ooibekuar","Normal","","Maya Blue"),
		(137,"88","Urckiansi","Normal","","Electric Blue"),
		(138,"89","Dozivadido","Normal","","Aquamarine"),
		(139,"8A","Emiekereks","Lush","","Pale Green"),
		(140,"8B","Meykinunukur","Normal","","Canary Yellow"),
		(141,"8C","Kimycuristh","Normal","","Medium Orchid"),
		(142,"8D","Roansfien","Normal","","Medium Slate Blue"),
		(143,"8E","Isgarmeso","Harsh","","Maya Blue"),
		(144,"8F","Daitibeli","Normal","","Electric Blue"),
		(145,"90","Gucuttarik","Normal","","Aquamarine"),
		(146,"91","Enlaythie","Normal","","Pale Green"),
		(147,"92","Drewweste","Empty","","Canary Yellow"),
		(148,"93","Akbulkabi","Normal","","Manhattan Orange"),
		(149,"94","Homskiw","Normal","","Pink"),
		(150,"95","Zavainlani","Lush","","Magenta"),
		(151,"96","Jewijkmas","Normal","","Electric Blue"),
		(152,"97","Itlhotagra","Empty","","Aquamarine"),
		(153,"98","Podalicess","Normal","Improved","Pale Green"),
		(154,"99","Hiviusauer","Normal","","Canary Yellow"),
		(155,"9A","Halsebenk","Harsh","Raging","Manhattan Orange"),
		(156,"9B","Puikitoac","Normal","","Pink"),
		(157,"9C","Gaybakuaria","Normal","","Magenta"),
		(158,"9D","Grbodubhe","Normal","","Medium Orchid"),
		(159,"9E","Rycempler","Lush","Halcyon","Medium Slate Blue"),
		(160,"9F","Indjalala","Normal","","Purple"),
		(161,"A0","Fontenikk","Normal","","Canary Yellow"),
		(162,"A1","Pasycihelwhee","Normal","","Manhattan Orange"),
		(163,"A2","Ikbaksmit","Harsh","","Pink"),
		(164,"A3","Telicianses","Normal","","Neon Pink"),
		(165,"A4","Oyleyzhan","Normal","","Magenta"),
		(166,"A5","Uagerosat","Normal","","Medium Slate Blue"),
		(167,"A6","Impoxectin","Empty","Frozen","Cornflower Blue"),
		(168,"A7","Twoodmand","Normal","","Electric Blue"),
		(169,"A8","Hilfsesorbs","Normal","","Aquamarine"),
		(170,"A9","Ezdaranit","Lush","","Pale Green"),
		(171,"AA","Wiensanshe","Normal","","Pink"),
		(172,"AB","Ewheelonc","Empty","","Medium Orchid"),
		(173,"AC","Litzmantufa","Normal","","Medium Slate Blue"),
		(174,"AD","Emarmatosi","Normal","","Maya Blue"),
		(175,"AE","Mufimbomacvi","Harsh","","Electric Blue"),
		(176,"AF","Wongquarum","Normal","","Aquamarine"),
		(177,"B0","Hapirajua","Normal","","Pale Green"),
		(178,"B1","Igbinduina","Normal","","Canary Yellow"),
		(179,"B2","Wepaitvas","Lush","","Manhattan Orange"),
		(180,"B3","Sthatigudi","Normal","","Pink"),
		(181,"B4","Yekathsebehn","Normal","","Maya Blue"),
		(182,"B5","Ebedeagurst","Normal","","Electric Blue"),
		(183,"B6","Nolisonia","Harsh","","Pale Green"),
		(184,"B7","Ulexovitab","Normal","","Mint Green"),
		(185,"B8","Iodhinxois","Normal","","Canary Yellow"),
		(186,"B9","Irroswitzs","Normal","","Manhattan Orange"),
		(187,"BA","Bifredait","Empty","","Pink"),
		(188,"BB","Beiraghedwe","Normal","","Magenta"),
		(189,"BC","Yeonatlak","Normal","","Medium Orchid"),
		(190,"BD","Cugnatachh","Lush","","Medium Slate Blue"),
		(191,"BE","Nozoryenki","Normal","","Mint Green"),
		(192,"BF","Ebralduri","Empty","","Canary Yellow"),
		(193,"C0","Evcickcandj","Normal","","Manhattan Orange"),
		(194,"C1","Ziybosswin","Normal","","Pink"),
		(195,"C2","Heperclait","Harsh","","Magenta"),
		(196,"C3","Sugiuniam","Normal","","Medium Orchid"),
		(197,"C4","Aaseertush","Normal","","Medium Slate Blue"),
		(198,"C5","Uglyestemaa","Normal","","Maya Blue"),
		(199,"C6","Horeroedsh","Lush","","Electric Blue"),
		(200,"C7","Drundemiso","Normal","","Pale Green"),
		(201,"C8","Ityanianat","Normal","","Pink"),
		(202,"C9","Purneyrine","Normal","","Magenta"),
		(203,"CA","Dokiessmat","Harsh","","Medium Orchid"),
		(204,"CB","Nupiacheh","Normal","","Medium Slate Blue"),
		(205,"CC","Dihewsonj","Normal","","Maya Blue"),
		(206,"CD","Rudrailhik","Normal","","Electric Blue"),
		(207,"CE","Tweretnort","Empty","","Pale Green"),
		(208,"CF","Snatreetze","Normal","","Mint Green"),
		(209,"D0","Iwundaracos","Normal","","Canary Yellow"),
		(210,"D1","Digarlewena","Lush","","Manhattan Orange"),
		(211,"D2","Erquagsta","Normal","","Medium Slate Blue"),
		(212,"D3","Logovoloin","Empty","","Maya Blue"),
		(213,"D4","Boyaghosganh","Normal","","Electric Blue"),
		(214,"D5","Kuolungau","Normal","","Pale Green"),
		(215,"D6","Pehneldept","Harsh","","Mint Green"),
		(216,"D7","Yevettiiqidcon","Normal","","Canary Yellow"),
		(217,"D8","Sahliacabru","Normal","","Manhattan Orange"),
		(218,"D9","Noggalterpor","Normal","","Pink"),
		(219,"DA","Chmageaki","Lush","","Magenta"),
		(220,"DB","Veticueca","Normal","","Medium Orchid"),
		(221,"DC","Vittesbursul","Normal","","Aquamarine"),
		(222,"DD","Nootanore","Normal","","Mint Green"),
		(223,"DE","Innebdjerah","Harsh","","Canary Yellow"),
		(224,"DF","Kisvarcini","Normal","","Manhattan Orange"),
		(225,"E0","Cuzcogipper","Normal","","Pink"),
		(226,"E1","Pamanhermonsu","Normal","","Magenta"),
		(227,"E2","Brotoghek","Empty","","Medium Orchid"),
		(228,"E3","Mibittara","Normal","","Medium Slate Blue"),
		(229,"E4","Huruahili","Normal","","Maya Blue"),
		(230,"E5","Raldwicarn","Lush","","Electric Blue"),
		(231,"E6","Ezdartlic","Normal","","Manhattan Orange"),
		(232,"E7","Badesclema","Empty","","Pink"),
		(233,"E8","Isenkeyan","Normal","","Magenta"),
		(234,"E9","Iadoitesu","Normal","","Medium Orchid"),
		(235,"EA","Yagrovoisi","Harsh","","Medium Slate Blue"),
		(236,"EB","Ewcomechio","Normal","","Maya Blue"),
		(237,"EC","Inunnunnoda","Normal","","Electric Blue"),
		(238,"ED","Dischiutun","Normal","","Aquamarine"),
		(239,"EE","Yuwarugha","Lush","","Mint Green"),
		(240,"EF","Ialmendra","Normal","","Canary Yellow"),
		(241,"F0","Reponudrle","Normal","","Medium Orchid"),
		(242,"F1","Rinjanagrbo","Normal","","Medium Slate Blue"),
		(243,"F2","Zeziceloh","Harsh","","Maya Blue"),
		(244,"F3","Oeileutasc","Normal","","Electric Blue"),
		(245,"F4","Zicniijinis","Normal","","Aquamarine"),
		(246,"F5","Dugnowarilda","Normal","","Mint Green"),
		(247,"F6","Neuxoisan","Empty","","Canary Yellow"),
		(248,"F7","Ilmenhorn","Normal","","Manhattan Orange"),
		(249,"F8","Rukwatsuku","Normal","","Pink"),
		(250,"F9","Nepitzaspru","Lush","","Magenta"),
		(251,"FA","Chcehoemig","Normal","","Electric Blue"),
		(252,"FB","Haffneyrin","Empty","","Aquamarine"),
		(253,"FC","Uliciawai","Normal","","Mint Green"),
		(254,"FD","Tuhgrespod","Normal","","Canary Yellow"),
		(255,"FE","Iousongola","Harsh","","Manhattan Orange"),
		(256,"FF","Odyalutai","Normal","","Magenta"),
		(257,"??","Yilsrussimil","Normal","","Magenta"), // Used only as a sentinel (the special value kind, not the in game kind). The hex field is deliberately invalid so accidental direct hex lookups fail.
    ];

    /// <summary>
    /// Mapping of named galaxy core colours to their UI hex values.
    /// </summary>
    /// <remarks>
    /// This list is used to resolve the galaxy core <c>Core</c> field in the
    /// <see cref="Galaxies"/> table into a concrete display colour for UI dots
    /// and other galaxy-related colour rendering.
    /// </remarks>
    private const string DefaultCoreColor = "#000000";

    public static readonly (string ColorName, string HexColor)[] CoreColors =
    [
		("White","#ffffff"),
		("Deep Pink","#ff1493"),
		("Medium Orchid","#ba55d3"),
		("Violet","#7f00ff"),
		("Light Blue","#add8e6"),
		("Turquoise","#30d5c8"),
		("Green","#15b01a"),
		("Lime Green","#89fe05"),
		("Yellow","#ffff14"),
		("Orange","#f97306"),
		("Indigo","#380282"),
		("Sky Blue","#75bbfd"),
		("Cyan","#00ffff"),
		("Dark Green","#006400"),
		("Pale Green","#c7fdb5"),
		("Pale Yellow","#ffff84"),
		("Pale Orange","#ffa756"),
		("Pale Red","#d9544d"),
		("Fuchsia","#ed0dd9"),
		("Purple","#7e1e9c"),
		("Light Green","#76ff7b"),
		("Red","#e50000"),
		("Magenta","#c20078"),
		("Royal Blue","#0504aa"),
		("Light Orange","#fdaa48"),
		("Light Fuchsia","#f700c6"),
		("Light Burgundy","#a8415b"),
		("Sea Green","#53fca1"),
		("Burgundy","#610023"),
		("Canary Yellow","#ffef00"),
		("Manhattan Orange","#e8a95c"),
		("Pink","#ff81c0"),
		("Medium Slate Blue","#7b68ee"),
		("Maya Blue","#73c2fb"),
		("Electric Blue","#0652ff"),
		("Turquoise Blue","#06c2ac"),
		("Aquamarine","#04d8b2"),
		("Orchid","#c875c4"),
		("Cornflower Blue","#6495ed"),
		("Mint Green","#8fff9f"),
		("Salmon","#ff796c"),
		("Neon Pink","#fe019a"),
		("Dark Orchid","#9932cc"),
		("Peach Orange","#ffb28a"),
		("Fuchsia Pink","#ff5bf8"),
		("Blue Violet","#5d06e9"),
		("Light Red","#ff474c"),
		("Light Coral","#f08080"),
		("Light Slate Blue","#8470ff"),
	];

    private static readonly Dictionary<string, string> CoreColorMap = CoreColors.ToDictionary(c => c.ColorName, c => c.HexColor, StringComparer.Ordinal);

    /// <summary>
    /// The number of real in-game galaxies in <see cref="Galaxies" />.
    /// </summary>
    /// <remarks>
    /// Entries 0..255 are real galaxies. The entry at index 256 is a special
    /// non-game sentinel used only for UI/listing. It is not a valid target for
    /// coordinate generation or portal hex calculations and is only valid for
    /// setting RealityIndex in the players current location from MainStats.
    /// It should never be treated as a real galaxy for any game logic or coordinate/portal hex generation.
    /// </remarks>
    public const int RealGalaxyCount = 256;

    /// <summary>
    /// Returns true when the given reality index refers to a real game galaxy.
    /// </summary>
    /// <remarks>
    /// Use this guard before accessing game-only properties such as hex coordinates.
    /// </remarks>
    private static bool IsRealGalaxyIndex(int realityIndex) => realityIndex >= 0 && realityIndex < RealGalaxyCount;

    /// <summary>
    /// Returns true when the given reality index refers to the special UI-only galaxy.
    /// </summary>
    /// <remarks>
    /// This sentinel entry is included for display and selection purposes, but it
    /// does not represent a playable galaxy and must never be used for normal
    /// coordinate/portal hex generation.
    /// </remarks>
    public static bool IsSpecialGalaxyIndex(int realityIndex) => realityIndex == RealGalaxyCount;

    private static bool IsKnownGalaxyIndex(int realityIndex) => realityIndex >= 0 && realityIndex < Galaxies.Length;

    /// <summary>Get raw galaxy name by 0-based reality index.</summary>
    public static string GetGalaxyName(int realityIndex) => IsKnownGalaxyIndex(realityIndex)
        ? Galaxies[realityIndex].Name
        : UiStrings.Get("common.galaxy_unknown");

    /// <summary>Get galaxy type by 0-based reality index.</summary>
    public static string GetGalaxyType(int realityIndex) => IsKnownGalaxyIndex(realityIndex)
        ? Galaxies[realityIndex].Type
        : UiStrings.Get("common.galaxy_type_default");

    /// <summary>Get galaxy core colour by 0-based reality index.</summary>
    public static string GetGalaxyCore(int realityIndex) => IsKnownGalaxyIndex(realityIndex)
        ? Galaxies[realityIndex].Core
        : "Unknown";

    /// <summary>Get full display name by 0-based reality index, e.g. "Euclid (1)".</summary>
    public static string GetGalaxyDisplayName(int realityIndex) => IsKnownGalaxyIndex(realityIndex)
        ? $"{Galaxies[realityIndex].Name} ({Galaxies[realityIndex].Number})"
        : UiStrings.Get("common.galaxy_unknown");

    /// Parses a hex colour string like <c>#RRGGBB</c> or <c>#AARRGGBB</c> into a <see cref="Color"/>.
    /// Invalid values fall back to <see cref="Color.Black"/>.
    public static Color ParseHexColor(string hexColor)
    {
        if (string.IsNullOrWhiteSpace(hexColor) || !hexColor.StartsWith("#", StringComparison.Ordinal)
            || (hexColor.Length != 7 && hexColor.Length != 9))
        {
            return Color.Black;
        }

        try
        {
            int argb = Convert.ToInt32(hexColor[1..], 16);
            return hexColor.Length == 7 ? Color.FromArgb(unchecked((int)0xFF000000 | argb)) : Color.FromArgb(argb);
        }
        catch
        {
            return Color.Black;
        }
    }

    /// <summary>
    /// Gets the resolved <see cref="Color"/> for the given galaxy core index.
    /// </summary>
    /// <param name="realityIndex">The 0-based galaxy reality index.</param>
    /// <returns>The parsed core colour, or black for unknown values.</returns>
    public static Color GetGalaxyCoreColorValue(int realityIndex) =>
        ParseHexColor(GetGalaxyCoreColor(realityIndex));

    /// <summary>
    /// Resolves the galaxy core colour hex string for a given galaxy index.
    /// </summary>
    /// <param name="realityIndex">The 0-based galaxy reality index.</param>
    /// <returns>The hex colour for the galaxy core, or the default colour for unknown indices.</returns>
    public static string GetGalaxyCoreColor(int realityIndex)
    {
        if (!IsKnownGalaxyIndex(realityIndex))
            return DefaultCoreColor;

        string colorName = GetGalaxyCore(realityIndex);
        return CoreColorMap.TryGetValue(colorName, out var hexColor)
            ? hexColor
            : DefaultCoreColor;
    }

    /// <summary>
    /// Returns the raw galaxy hex code for valid in-game galaxy indices.
    /// </summary>
    /// <param name="realityIndex">The 0-based galaxy reality index.</param>
    /// <returns>The hex code string, or <c>null</c> for non-game or unknown indices.</returns>
    public static string? GetGalaxyHex(int realityIndex) => IsRealGalaxyIndex(realityIndex)
        ? Galaxies[realityIndex].Hex
        : null;

    /// <summary>
    /// Tries to get the raw galaxy hex code for a real galaxy index.
    /// </summary>
    /// <param name="realityIndex">The 0-based galaxy reality index.</param>
    /// <param name="hex">The returned hex string if successful.</param>
    /// <returns><c>true</c> when the index is a real galaxy; otherwise <c>false</c>.</returns>
    public static bool TryGetGalaxyHex(int realityIndex, out string hex)
    {
        if (IsRealGalaxyIndex(realityIndex))
        {
            hex = Galaxies[realityIndex].Hex;
            return true;
        }

        hex = string.Empty;
        return false;
    }
}