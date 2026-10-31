"""Enable persistent Particle Engine tab placement (opt-in) and apply it now.

Run in Rhino with  _-RunPythonScript  while Grasshopper is open:
  _-RunPythonScript "D:\\CLAUDE\\Particle Engine\\scripts\\particle-engine-pin-tab.py"

Creates a flag file at %APPDATA%\\ParticleEngine\\pin-tab.flag. The plugin reads that flag on every
future session and, while it exists, moves ONLY the Particle Engine tab just after the Rhino tab
(restoring order if anything fails). This script also moves it immediately. Windows only.
Disable again with particle-engine-unpin-tab.py.
"""
import clr
import json
import os
import traceback
clr.AddReference('Grasshopper')
import Grasshopper
import Rhino

FLAG = os.path.join(os.environ['APPDATA'], 'ParticleEngine', 'pin-tab.flag')


def find_ribbon(control):
    if control.GetType().FullName == 'Grasshopper.GUI.Ribbon.GH_Ribbon':
        return control
    for child in control.Controls:
        found = find_ribbon(child)
        if found is not None:
            return found
    return None


def move_after_rhino(ribbon, tab):
    count_before = ribbon.Tabs.Count
    old_index = ribbon.Tabs.IndexOf(tab)
    ribbon.SuspendLayout()
    try:
        ribbon.Tabs.Remove(tab)
        native = [t for t in ribbon.Tabs if str(t.NameFull) == 'Rhino']
        dest = ribbon.Tabs.IndexOf(native[0]) + 1 if native else 1
        dest = min(dest, ribbon.Tabs.Count)
        ribbon.Tabs.Insert(dest, tab)
        if ribbon.Tabs.Count != count_before:
            raise Exception('Unexpected ribbon tab count after reordering.')
        ribbon.LayoutRibbon()
        ribbon.ActiveTabIndex = dest
        return dest
    except Exception:
        if tab not in list(ribbon.Tabs):
            ribbon.Tabs.Insert(min(old_index, ribbon.Tabs.Count), tab)
        ribbon.LayoutRibbon()
        raise
    finally:
        ribbon.ResumeLayout(True)


report = {}
report_dir = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', 'diagnostics'))
if not os.path.isdir(report_dir):
    os.makedirs(report_dir)
path = os.path.join(report_dir, 'particle-engine-pin-tab.json')
try:
    # Write the persistent opt-in flag first, so placement survives restart even if the
    # immediate move below fails for any reason.
    flag_dir = os.path.dirname(FLAG)
    if not os.path.isdir(flag_dir):
        os.makedirs(flag_dir)
    with open(FLAG, 'w') as f:
        f.write('enabled')
    report['flag'] = FLAG
    report['flag_exists'] = os.path.isfile(FLAG)

    editor = Grasshopper.Instances.DocumentEditor
    if editor is None:
        raise Exception('Open Grasshopper first.')
    ribbon = find_ribbon(editor)
    if ribbon is None:
        raise Exception('Grasshopper ribbon control not found.')
    matching = [t for t in ribbon.Tabs if str(t.NameFull) == 'Particle Engine']
    if len(matching) != 1:
        raise Exception('Expected one Particle Engine tab; found ' + str(len(matching)))

    dest = move_after_rhino(ribbon, matching[0])
    ribbon.Refresh()
    editor.Show()
    editor.Activate()
    report['moved_to_index'] = dest
    report['tab_count'] = ribbon.Tabs.Count
    Rhino.RhinoApp.WriteLine('Particle Engine tab pinned after Rhino (tab ' + str(dest + 1) + ').')
    Rhino.RhinoApp.WriteLine('Persistent placement ENABLED (flag: ' + FLAG + '). It reapplies every session until you unpin.')
except Exception:
    report['error'] = traceback.format_exc()
    Rhino.RhinoApp.WriteLine(report['error'])
finally:
    with open(path, 'w') as output:
        json.dump(report, output, indent=2)
    Rhino.RhinoApp.WriteLine('Pin report saved: ' + path)
