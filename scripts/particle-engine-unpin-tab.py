"""Disable persistent Particle Engine tab placement (opt-out).

Run in Rhino with  _-RunPythonScript  while Grasshopper is open:
  _-RunPythonScript "D:\\CLAUDE\\Particle Engine\\scripts\\particle-engine-unpin-tab.py"

Clears the persistent Grasshopper setting so the plugin stops repositioning the tab on future
sessions. The current session's tab position is left as-is (it returns to its natural place on the
next Grasshopper restart). Other plugins' tabs are never touched.
"""
import clr
import json
import os
import traceback
clr.AddReference('Grasshopper')
import Grasshopper
import Rhino

ENABLED_KEY = 'ParticleEngine.PinTab.Enabled'

report = {}
report_dir = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', 'diagnostics'))
if not os.path.isdir(report_dir):
    os.makedirs(report_dir)
path = os.path.join(report_dir, 'particle-engine-unpin-tab.json')
try:
    settings = Grasshopper.Instances.Settings
    settings.SetValue(ENABLED_KEY, False)
    try:
        settings.WritePersistentSettings()
    except Exception:
        pass
    report['enabled'] = False
    Rhino.RhinoApp.WriteLine('Persistent Particle Engine tab placement is now DISABLED.')
    Rhino.RhinoApp.WriteLine('The tab returns to its natural position on the next Grasshopper restart.')
except Exception:
    report['error'] = traceback.format_exc()
    Rhino.RhinoApp.WriteLine(report['error'])
finally:
    with open(path, 'w') as output:
        json.dump(report, output, indent=2)
    Rhino.RhinoApp.WriteLine('Unpin report saved: ' + path)
