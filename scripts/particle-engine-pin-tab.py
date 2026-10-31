"""Re-enable automatic Particle Engine tab placement (undo an opt-out) and apply it now.

Placement is ON BY DEFAULT, so you only need this if you previously opted out, or want to move the
tab immediately without restarting. Run in Rhino with  _-RunPythonScript  while Grasshopper is open:
  _-RunPythonScript "D:\\CLAUDE\\Particle Engine\\scripts\\particle-engine-pin-tab.py"

Removes the opt-out flag %APPDATA%\\ParticleEngine\\pin-tab.disabled (if present) and moves ONLY the
Particle Engine tab to just after the Rhino tab now. Windows only.
"""
import clr
import json
import os
import traceback
clr.AddReference('Grasshopper')
import Grasshopper
import Rhino

DISABLE = os.path.join(os.environ['APPDATA'], 'ParticleEngine', 'pin-tab.disabled')


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
    # Remove any opt-out flag so automatic placement resumes on future sessions.
    if os.path.isfile(DISABLE):
        os.remove(DISABLE)
    report['disable_flag_removed'] = not os.path.isfile(DISABLE)

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
    Rhino.RhinoApp.WriteLine('Automatic placement is ENABLED (default); it reapplies every session.')
except Exception:
    report['error'] = traceback.format_exc()
    Rhino.RhinoApp.WriteLine(report['error'])
finally:
    with open(path, 'w') as output:
        json.dump(report, output, indent=2)
    Rhino.RhinoApp.WriteLine('Pin report saved: ' + path)
