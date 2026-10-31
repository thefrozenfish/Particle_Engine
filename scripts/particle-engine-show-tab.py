import clr
import json
import os
import traceback
import scriptcontext as sc
clr.AddReference('Grasshopper')
import Grasshopper
import Rhino

# Session-only placement adjustment for a crowded ribbon. The same tab object
# and panels are retained. No plugin, document or saved layout is replaced.
def find_ribbon(control):
    if control.GetType().FullName == 'Grasshopper.GUI.Ribbon.GH_Ribbon':
        return control
    for child in control.Controls:
        found = find_ribbon(child)
        if found is not None:
            return found
    return None

def state(ribbon, tab):
    grip = tab.Grip
    return {'index': ribbon.Tabs.IndexOf(tab), 'active_tab': str(ribbon.ActiveTabName),
            'ribbon_width': ribbon.ClientSize.Width,
            'grip': {'left': grip.Left, 'right': grip.Right, 'top': grip.Top, 'bottom': grip.Bottom},
            'display_style': str(tab.DisplayStyle), 'panel_count': len(list(tab.Panels))}

report = {}
report_dir = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', 'diagnostics'))
if not os.path.isdir(report_dir):
    os.makedirs(report_dir)
path = os.path.join(report_dir, 'particle-engine-tab-placement.json')
try:
    editor = Grasshopper.Instances.DocumentEditor
    if editor is None:
        raise Exception('Open Grasshopper first.')
    ribbon = find_ribbon(editor)
    if ribbon is None:
        raise Exception('Grasshopper ribbon control not found.')
    matching = [tab for tab in ribbon.Tabs if str(tab.NameFull) == 'Particle Engine']
    if len(matching) != 1:
        raise Exception('Expected one Particle Engine tab; found ' + str(len(matching)))
    tab = matching[0]
    ribbon.LayoutRibbon()
    report['before'] = state(ribbon, tab)
    old_index = ribbon.Tabs.IndexOf(tab)
    old_active = ribbon.ActiveTab
    count_before = ribbon.Tabs.Count
    key = 'ParticleEngine.RibbonPlacement'
    if key not in sc.sticky:
        sc.sticky[key] = {'tab': tab, 'ribbon': ribbon, 'index': old_index, 'active': old_active}
    # The public SDK currently exposes the tab order as a List, without public
    # move methods. Reorder this existing object only, then request a layout.
    ribbon.SuspendLayout()
    try:
        ribbon.Tabs.Remove(tab)
        native = [item for item in ribbon.Tabs if str(item.NameFull) == 'Rhino']
        destination = ribbon.Tabs.IndexOf(native[0]) + 1 if native else 1
        destination = min(destination, ribbon.Tabs.Count)
        ribbon.Tabs.Insert(destination, tab)
        if ribbon.Tabs.Count != count_before:
            raise Exception('Unexpected ribbon tab count after reordering.')
        ribbon.LayoutRibbon()
        ribbon.ActiveTabIndex = destination
    except Exception:
        ribbon.Tabs.Remove(tab)
        ribbon.Tabs.Insert(min(old_index, ribbon.Tabs.Count), tab)
        ribbon.ActiveTab = old_active
        ribbon.LayoutRibbon()
        raise
    finally:
        ribbon.ResumeLayout(True)
    ribbon.Refresh()
    editor.Show()
    editor.Activate()
    report['after'] = state(ribbon, tab)
    report['tab_count'] = ribbon.Tabs.Count
    if str(ribbon.ActiveTabName) != 'Particle Engine':
        raise Exception('Tab moved, but Grasshopper did not retain it as the active tab.')
    grip = tab.Grip
    if grip.Width <= 0 or grip.Right > ribbon.ClientSize.Width or grip.Left < 0:
        raise Exception('Tab moved, but its clickable area is still outside the ribbon.')
    Rhino.RhinoApp.WriteLine('Particle Engine is now selected just after the Rhino tab (tab ' + str(destination + 1) + ').')
    Rhino.RhinoApp.WriteLine('All ' + str(count_before) + ' tabs retained. Placement lasts for this Grasshopper session.')
except Exception:
    report['error'] = traceback.format_exc()
    Rhino.RhinoApp.WriteLine(report['error'])
finally:
    with open(path, 'w') as output:
        json.dump(report, output, indent=2)
    Rhino.RhinoApp.WriteLine('Placement report saved: ' + path)
