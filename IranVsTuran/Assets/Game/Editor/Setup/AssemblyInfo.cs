using System.Runtime.CompilerServices;

// The CI test assembly only compiles once the Test Framework is installed, and hooks into CIBuild.
[assembly: InternalsVisibleTo("IVT.Editor.CI")]
