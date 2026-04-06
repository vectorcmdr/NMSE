using System;
using Avalonia.Data.Core;
using Avalonia.Markup.Xaml;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Markup.Xaml.MarkupExtensions.CompiledBindings;

namespace NMSE.UI.Localization;

/// <summary>
/// XAML markup extension that resolves a localised string from
/// <see cref="LocaleManager"/> using compiled bindings.
/// <para>Usage: <c>{loc:Locale menu.file}</c></para>
/// </summary>
public class LocaleExtension : MarkupExtension
{
    public LocaleExtension(string key)
    {
        Key = key;
    }

    public string Key { get; set; }

    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        var keyToUse = Key;

        var builder = new CompiledBindingPathBuilder();
        builder.Property(
            new ClrPropertyInfo(
                "Item",
                _ => LocaleManager.Instance[keyToUse],
                null,
                typeof(string)),
            PropertyInfoAccessorFactory.CreateInpcPropertyAccessor);

        var path = builder.Build();
        var binding = new CompiledBindingExtension(path)
        {
            Source = LocaleManager.Instance
        };

        return binding.ProvideValue(serviceProvider);
    }
}
