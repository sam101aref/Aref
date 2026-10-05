using System.Runtime.CompilerServices;

// The CI test runner only compiles once the Test Framework package is installed, and hooks into CIBuild.
[assembly: InternalsVisibleTo("Siavosh.Editor.CI")]
