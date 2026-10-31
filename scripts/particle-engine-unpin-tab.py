"""Opt OUT of automatic Particle Engine tab placement.

Placement is ON BY DEFAULT. Run this in Rhino with  _-RunPythonScript  to turn it off:
  _-RunPythonScript "D:\\CLAUDE\\Particle Engine\\scripts\\particle-engine-unpin-tab.py"

Creates a flag file at %APPDATA%\\ParticleEngine\\pin-tab.disabled. While it exists the plugin stops
repositioning the tab on future sessions. The current session's tab position is left as-is (the tab
returns to its natural place on the next Grasshopper restart). Other plugins' tabs are never touched.
Re-enable with particle-engine-pin-tab.py.
"""
import json
import os
import traceback

try:
    import Rhino

    def _log(msg):
        Rhino.RhinoApp.WriteLine(msg)
except Exception:
    def _log(msg):
        print(msg)

DISABLE = os.path.join(os.environ['APPDATA'], 'ParticleEngine', 'pin-tab.disabled')

report = {}
report_dir = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', 'diagnostics'))
if not os.path.isdir(report_dir):
    os.makedirs(report_dir)
path = os.path.join(report_dir, 'particle-engine-unpin-tab.json')
try:
    flag_dir = os.path.dirname(DISABLE)
    if not os.path.isdir(flag_dir):
        os.makedirs(flag_dir)
    with open(DISABLE, 'w') as f:
        f.write('disabled')
    report['disabled_flag'] = DISABLE
    report['exists'] = os.path.isfile(DISABLE)
    _log('Automatic Particle Engine tab placement is now DISABLED.')
    _log('The tab returns to its natural position on the next Grasshopper restart.')
except Exception:
    report['error'] = traceback.format_exc()
    _log(report['error'])
finally:
    with open(path, 'w') as output:
        json.dump(report, output, indent=2)
    _log('Unpin report saved: ' + path)
