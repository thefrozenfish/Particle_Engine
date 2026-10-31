"""Disable persistent Particle Engine tab placement (opt-out).

Run in Rhino with  _-RunPythonScript  while Grasshopper is open:
  _-RunPythonScript "D:\\CLAUDE\\Particle Engine\\scripts\\particle-engine-unpin-tab.py"

Removes the flag file at %APPDATA%\\ParticleEngine\\pin-tab.flag so the plugin stops repositioning
the tab on future sessions. The current session's tab position is left as-is (it returns to its
natural place on the next Grasshopper restart). Other plugins' tabs are never touched.
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

FLAG = os.path.join(os.environ['APPDATA'], 'ParticleEngine', 'pin-tab.flag')

report = {}
report_dir = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', 'diagnostics'))
if not os.path.isdir(report_dir):
    os.makedirs(report_dir)
path = os.path.join(report_dir, 'particle-engine-unpin-tab.json')
try:
    if os.path.isfile(FLAG):
        os.remove(FLAG)
    report['flag'] = FLAG
    report['flag_exists'] = os.path.isfile(FLAG)
    _log('Persistent Particle Engine tab placement DISABLED (flag removed).')
    _log('The tab returns to its natural position on the next Grasshopper restart.')
except Exception:
    report['error'] = traceback.format_exc()
    _log(report['error'])
finally:
    with open(path, 'w') as output:
        json.dump(report, output, indent=2)
    _log('Unpin report saved: ' + path)
