using System.Runtime.CompilerServices;
using VerifyTests;

namespace SourceGeneratorTemplate.Tests;

public static class ModuleInitializer
{
    [ModuleInitializer]
    public static void Init()
    {
        VerifySourceGenerators.Enable();
    }
}