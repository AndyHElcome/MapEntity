using System.Globalization;
using System.Reflection;
using System.Text.Json;
using MongoDB.Bson;
using Refit;

namespace MMIv8_Ktype.Models.Util
{
    public static class RefitSettingsOptions
    {
        public static RefitSettings GetApplyRefitSettings(this RefitSettings refitSettings)
        {
            ApplyRefitSettings(refitSettings);
            return refitSettings;
        }

        public static void ApplyRefitSettings(RefitSettings refitSettings)
        {
            refitSettings.ContentSerializer = new SystemTextJsonContentSerializer(new JsonSerializerOptions().GetJsonSerializerOptions());
        }
    }
}
