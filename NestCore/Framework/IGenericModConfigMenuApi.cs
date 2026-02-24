using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;

namespace Mikew.NestCore.Framework
{
    public interface IGenericModConfigMenuAPI
    {
    	void Register(IManifest mod, Action reset, Action save, bool titleScreenOnly = false);
        void AddImage(IManifest mod, Func<Texture2D> texture, Rectangle? texturePixelArea = null, int scale = Game1.pixelZoom);    	void AddBoolOption(IManifest mod, Func<bool> getValue, Action<bool> setValue, Func<string> name, Func<string>? tooltip = null, string? fieldId = null);
        void RegisterImage(IManifest mod, string texPath, Rectangle? texRect = null, int scale = 4);

    	void AddNumberOption(IManifest mod, Func<int> getValue, Action<int> setValue, Func<string> name, Func<string>? tooltip = null, int? min = null, int? max = null, int? interval = null, Func<int, string>? formatValue = null, string? fieldId = null);
        void AddNumberOption(IManifest mod, Func<float> getValue, Action<float> setValue, Func<string> name, Func<string>? tooltip = null, float? min = null, float? max = null, float? interval = null, Func<float, string>? formatValue = null, string? fieldId = null);
    	void AddTextOption(IManifest mod, Func<string> getValue, Action<string> setValue, Func<string> name, Func<string>? tooltip = null, string[]? allowedValues = null, Func<string, string>? formatAllowedValue = null, string? fieldId = null);

    	void AddSectionTitle(IManifest mod, Func<string> text, Func<string>? tooltip = null);

    	void AddPage(IManifest mod, string pageId, Func<string>? pageTitle = null);

    	void AddPageLink(IManifest mod, string pageId, Func<string> text, Func<string>? tooltip = null);
		        void AddParagraph(IManifest mod, Func<string> text);
		void Unregister(IManifest mod);
    }
}
