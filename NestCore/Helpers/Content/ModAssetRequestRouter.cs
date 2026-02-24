using System;
using StardewModdingAPI.Events;

namespace Mikew.NestCore.Helpers.Content
{
    /// <summary>
    /// Routes SMAPI asset requests to a <see cref="ModAssetHandler"/> based on the handler's known game paths.
    /// </summary>
    public sealed class ModAssetRequestRouter
    {
        private readonly ModAssetHandler _handler;

        public ModAssetRequestRouter(ModAssetHandler handler)
        {
            _handler = handler ?? throw new ArgumentNullException(nameof(handler));
        }

        public bool TryHandle(AssetRequestedEventArgs e)
        {
            if (e == null)
                throw new ArgumentNullException(nameof(e));

            var key = e.NameWithoutLocale.BaseName.ToLowerInvariant();

            if (_handler.HasGamePath(key))
            {
                _handler.LoadModAssets(e);
                return true;
            }

            return false;
        }
    }
}
