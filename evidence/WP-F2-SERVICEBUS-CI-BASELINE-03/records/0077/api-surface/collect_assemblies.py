"""One assembly per name, chosen by a fixed framework preference, applied identically to both trees.

Record 0076 F-1: the earlier collection globbed net9.0 only, so two assemblies that build no net9.0
output disappeared from the measurement without anything saying so -- among them a shipped package.
The preference order below is fixed and documented, so baseline and current pick the same build of
each assembly and the comparison stays like for like.
"""
import pathlib, shutil, sys

PREFERENCE = ["net9.0", "net10.0", "net8.0", "netstandard2.1", "netstandard2.0", "net472"]
src, dest = pathlib.Path(sys.argv[1]), pathlib.Path(sys.argv[2])
dest.mkdir(parents=True, exist_ok=True)

found = {}
for dll in src.glob("**/bin/Release/*/ViciOne.ServiceBus*.dll"):
    if "/ref/" in str(dll):
        continue
    tfm = dll.parent.name
    if tfm not in PREFERENCE:
        continue
    rank = PREFERENCE.index(tfm)
    if dll.name not in found or rank < found[dll.name][0]:
        found[dll.name] = (rank, dll)

for name, (_, dll) in sorted(found.items()):
    shutil.copy2(dll, dest/name)
print(f"{len(found)} Assemblies gesammelt")
