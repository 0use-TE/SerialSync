import { dotnet } from './_framework/dotnet.js'

const dotnetRuntime = await dotnet
    .withDiagnosticTracing(false)
    .create();

const config = dotnetRuntime.getConfig();

await dotnetRuntime.runMain(config.mainAssemblyName, [globalThis.location.href]);
