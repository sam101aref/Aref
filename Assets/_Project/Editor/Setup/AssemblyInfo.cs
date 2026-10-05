using System.Runtime.CompilerServices;

// These assemblies only compile once their packages are installed, and hook into ProjectSetup.
[assembly: InternalsVisibleTo("Arash.Editor.Setup.URP")]
[assembly: InternalsVisibleTo("Arash.Editor.Content")]
[assembly: InternalsVisibleTo("Arash.Editor.CI")]
