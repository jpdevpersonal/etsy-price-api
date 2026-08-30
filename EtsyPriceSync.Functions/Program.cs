using EtsyPriceSync.Functions.Options;
using EtsyPriceSync.Functions.Repositories;
using EtsyPriceSync.Functions.Services;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = FunctionsApplication.CreateBuilder(args);
builder.Services.AddOptions<EtsyOptions>().BindConfiguration(EtsyOptions.SectionName).Validate(x => OptionsValidation.IsConfiguredSecret(x.Keystring, EtsyOptions.PlaceholderKeystring), "Replace Etsy:Keystring.").Validate(x => OptionsValidation.IsConfiguredSecret(x.SharedSecret, EtsyOptions.PlaceholderSharedSecret), "Replace Etsy:SharedSecret.").Validate(x => OptionsValidation.IsConfiguredValue(x.AppName), "Replace Etsy:AppName.").Validate(x => OptionsValidation.IsValidHttpsUrl(x.CallbackUrl), "Etsy:CallbackUrl must use HTTPS.").Validate(x => OptionsValidation.IsValidEtsyBaseUrl(x.BaseUrl), "Etsy:BaseUrl must be the HTTPS api.etsy.com host.").Validate(x => OptionsValidation.IsValidCurrency(x.Currency), "Etsy:Currency must be a recognized ISO 4217 code.").ValidateOnStart();
builder.Services.AddOptions<DatabaseOptions>().BindConfiguration(DatabaseOptions.SectionName).Validate(x => OptionsValidation.IsConfiguredValue(x.ConnectionString), "Configure Database:ConnectionString.").ValidateOnStart();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<IEtsyListingIdParser, EtsyListingIdParser>();
builder.Services.AddSingleton<ICurrencyPriceFormatter, CurrencyPriceFormatter>();
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<IPriceSyncService, PriceSyncService>();
builder.Services.AddHttpClient<IEtsyClient, EtsyClient>((provider, client) => { var options = provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<EtsyOptions>>().Value; client.BaseAddress = new Uri(string.Concat(options.BaseUrl.TrimEnd('/'), "/")); client.Timeout = TimeSpan.FromSeconds(100); }).SetHandlerLifetime(TimeSpan.FromMinutes(15)).AddStandardResilienceHandler();
builder.Build().Run();

