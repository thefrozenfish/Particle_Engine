# Dump every Culebra component's PORT DEFINITIONS to JSON, for the Particle Engine
# port-conformance test (spec 12.1).
#
# CLEAN-ROOM NOTE: this reads only PUBLIC Grasshopper metadata from the installed
# plugin (component names, nicknames, categories, GUIDs, port names/types/defaults)
# via the object server. It copies NO Culebra source, binaries, icons or text.
#
# HOW TO RUN (Rhino 8, where you already have Culebra installed):
#   1. Start Rhino 8 and open Grasshopper once, so Culebra is loaded.
#   2. Run the Rhino command  ScriptEditor  (or the older  EditPythonScript ).
#   3. Open THIS file in it and Run it as a STANDALONE script.
#      Do NOT paste it into a GhPython / Python component on the canvas: Culebra's
#      bundled IKVM libraries can clash with in-canvas scripting (per Culebra's own
#      warning). A standalone editor script is fine.
#   4. It writes culebra_ports.json next to this file and prints the path + a count.
#      Tell me the count (and paste any error lines); then I fold it into M1.
#
# Best-effort draft: if an attribute name differs in your Culebra build, send me the
# error and I'll adjust it.

import json
import Grasshopper as gh

OUT = r"D:\CLAUDE\Particle Engine\tests\ParticleEngine.Parity\culebra_ports.json"


def port(p):
    d = {
        "name": p.Name,
        "nick": p.NickName,
        "desc": p.Description,
        "access": str(p.Access),
        "optional": bool(p.Optional),
        "type": p.TypeName,
    }
    try:
        d["default"] = [str(g) for g in p.PersistentData.AllData(True)]
    except Exception:
        pass
    return d


rows = []
for proxy in gh.Instances.ComponentServer.ObjectProxies:
    desc = proxy.Desc
    if "culebra" not in (desc.Category or "").lower():
        continue
    row = {"name": desc.Name, "nick": desc.NickName}
    # Capture each metadata field independently: a missing attribute (e.g. Exposure
    # lives on the proxy and varies by build) must never drop the whole component's ports.
    for key, getter in (
        ("category", lambda: desc.Category),
        ("sub",      lambda: desc.SubCategory),
        ("exposure", lambda: str(proxy.Exposure)),
        ("guid",     lambda: str(proxy.Guid)),
    ):
        try:
            row[key] = getter()
        except Exception:
            pass
    try:
        obj = proxy.CreateInstance()
        row["inputs"]  = [port(p) for p in obj.Params.Input]
        row["outputs"] = [port(p) for p in obj.Params.Output]
    except Exception as e:
        row["error"] = str(e)
    rows.append(row)

with open(OUT, "w") as f:
    f.write(json.dumps(rows, indent=1))

print("Wrote %d Culebra component(s) to %s" % (len(rows), OUT))

# If nothing matched, the tab name differs; this lists the tabs present so we can fix the filter.
if not rows:
    cats = sorted({(p.Desc.Category or "") for p in gh.Instances.ComponentServer.ObjectProxies})
    print("No 'culebra' category matched. Tabs present:")
    for c in cats:
        print("  -", c)
