import clr
import System
import json
import os
import traceback
clr.AddReference('Grasshopper')
import Grasshopper
import Rhino

# Refresh the existing editor only. Do not load plugins, edit documents, reset
# saved preferences or replace a custom ribbon layout.
report = {'components': [], 'errors': []}
report_dir = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', 'diagnostics'))
if not os.path.isdir(report_dir):
    os.makedirs(report_dir)
path = os.path.join(report_dir, 'particle-engine-ribbon-report.json')

def find_ribbon(control):
    if control.GetType().FullName == 'Grasshopper.GUI.Ribbon.GH_Ribbon':
        return control
    for child in control.Controls:
        found = find_ribbon(child)
        if found is not None:
            return found
    return None

def tabs(ribbon):
    if ribbon is None:
        return None
    return [{'name': str(tab.NameFull), 'visible': bool(tab.Visible),
             'panel_count': len(list(tab.Panels))} for tab in ribbon.Tabs]

try:
    server = Grasshopper.Instances.ComponentServer
    for proxy in server.ObjectProxies:
        if str(proxy.Desc.Category) == 'Particle Engine':
            report['components'].append({
                'name': str(proxy.Desc.Name),
                'subcategory': str(proxy.Desc.SubCategory),
                'sdk_compliant': bool(proxy.SDKCompliant),
                'obsolete': bool(proxy.Obsolete),
                'exposure': str(proxy.Exposure)})
    report['in_default_layout'] = server.CompleteRibbonLayout.FindTab('Particle Engine') is not None
    editor = Grasshopper.Instances.DocumentEditor
    if editor is None:
        raise Exception('Open the Grasshopper window before running this check.')
    ribbon = find_ribbon(editor)
    report['tabs_before'] = tabs(ribbon)
    server.UpdateRibbonUI()
    ribbon = find_ribbon(editor)
    report['tabs_after'] = tabs(ribbon)
    found = [] if ribbon is None else [tab for tab in ribbon.Tabs if str(tab.NameFull) == 'Particle Engine']
    report['particle_tab_found'] = len(found) > 0
    if found:
        ribbon.ActiveTabName = 'Particle Engine'
        ribbon.Invalidate()
        editor.Show()
        editor.Activate()
        Rhino.RhinoApp.WriteLine('Particle Engine tab found and selected after ribbon refresh.')
    else:
        Rhino.RhinoApp.WriteLine('Particle Engine tab still absent; diagnostic details saved for inspection.')
    match = server.FindObjectByName('Box Spawn', True, True)
    report['box_spawn_search'] = None if match is None else {
        'name': str(match.Desc.Name), 'category': str(match.Desc.Category), 'guid': str(match.Guid)}
except Exception:
    report['errors'].append(traceback.format_exc())
    Rhino.RhinoApp.WriteLine(report['errors'][-1])
finally:
    with open(path, 'w') as output:
        json.dump(report, output, indent=2)
    Rhino.RhinoApp.WriteLine('Ribbon report saved: ' + path)
