namespace Autoredi.Generators;

public static class Names
{
    internal const string AttributesFullNamespace = "Autoredi.Attributes";

    internal const string AutorediAttName = "Autoredi";
    internal const string AutorediAttNameWithPostfix = $"{AutorediAttName}Attribute";
    internal const string AutorediAttGeneratedFilename = $"{AutorediAttNameWithPostfix}.g.cs";
    internal const string AutorediAttFullName = $"{AttributesFullNamespace}.{AutorediAttNameWithPostfix}";
    internal const string AutorediAttFullNameT = $"{AutorediAttFullName}`1";

    internal const string DefinedInstanceAttName = "Autoredi";
    internal const string DefinedInstanceAttNameWithPostfix = $"{DefinedInstanceAttName}Attribute";
    internal const string DefinedInstanceAttFullName = $"{AttributesFullNamespace}.{DefinedInstanceAttNameWithPostfix}";
    internal const string AttributesNamespace = "Attributes";
}
