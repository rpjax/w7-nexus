using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Server.Kestrel.Core;

namespace Nexus.Composition;

public static class KestrelExtensions
{
    public static WebApplicationBuilder ConfigureNexusKestrel(this WebApplicationBuilder builder)
    {
        builder.WebHost.ConfigureKestrel(options =>
        {
            options.ConfigureEndpointDefaults(listenOptions =>
            {
                listenOptions.Protocols = HttpProtocols.Http1AndHttp2;
            });
        });

        return builder;
    }

    public static WebApplicationBuilder ConfigureNexusDevelopmentHttpsCertificate(this WebApplicationBuilder builder)
    {
        if (!builder.Environment.IsDevelopment())
            return builder;

        var certPath = builder.Configuration["Kestrel:Certificates:Default:Path"];
        var keyPath = builder.Configuration["Kestrel:Certificates:Default:KeyPath"];

        if (string.IsNullOrWhiteSpace(certPath) || string.IsNullOrWhiteSpace(keyPath))
            throw new InvalidOperationException(
                "Kestrel certificate paths are required in appsettings.Development.json for HTTPS local certificate setup.");

        var absoluteCertPath = Path.GetFullPath(Path.Combine(builder.Environment.ContentRootPath, certPath));
        var absoluteKeyPath = Path.GetFullPath(Path.Combine(builder.Environment.ContentRootPath, keyPath));

        if (!File.Exists(absoluteCertPath) || !File.Exists(absoluteKeyPath))
            throw new InvalidOperationException(
                $"Kestrel certificate files were not found. Cert: '{absoluteCertPath}', Key: '{absoluteKeyPath}'.");

        var certificate = LoadCertificateFromPem(absoluteCertPath, absoluteKeyPath);

        builder.WebHost.ConfigureKestrel(options =>
        {
            options.ConfigureHttpsDefaults(httpsOptions =>
            {
                httpsOptions.ServerCertificate = certificate;
            });
        });

        return builder;
    }

    private static X509Certificate2 LoadCertificateFromPem(string certificatePath, string privateKeyPath)
    {
        var certWithKey = X509Certificate2.CreateFromPemFile(certificatePath, privateKeyPath);
        // Re-import as PFX so Kestrel on Windows gets a cert object with an associated private key handle.
        var pfxBytes = certWithKey.Export(X509ContentType.Pkcs12);
        return X509CertificateLoader.LoadPkcs12(pfxBytes, password: null);
    }
}
