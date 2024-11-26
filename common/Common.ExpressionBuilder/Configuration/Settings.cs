using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using Common.ExpressionBuilder.Common;

namespace Common.ExpressionBuilder.Configuration
{
    public class Settings
    {
        public List<SupportedType> SupportedTypes { get; private set; }

        public static void LoadSettings(Settings settings)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json",
                    optional: true,
                    reloadOnChange: true);

            var _config = builder.Build();

            settings.SupportedTypes = new List<SupportedType>();
            foreach (var supportedType in _config.GetSection("supportedTypes").GetChildren())
            {
                var typeGroup = supportedType.GetValue<TypeGroup>("typeGroup");
                var type = Type.GetType(supportedType.GetValue<string>("Type"), false, true);
                if (type != null)
                {
                    settings.SupportedTypes.Add(new SupportedType { TypeGroup = typeGroup, Type = type });
                }
            }
        }
    }
}