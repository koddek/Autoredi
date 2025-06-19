namespace Autoredi.Generators;

public static class Names
{
    internal const string AttributesNamespace = "Attributes";
    internal const string AutorediAttName = "Autoredi";
    internal const string AttributesFullNamespace = $"{AutorediAttName}.{AttributesNamespace}";
    internal const string AutorediAttNameWithPostfix = $"{AutorediAttName}Attribute";
    internal const string AutorediAttFullName = $"{AttributesFullNamespace}.{AutorediAttNameWithPostfix}";
}
