import clr
import System
import json
import os
import traceback
clr.AddReference('Grasshopper')
import Grasshopper
import Rhino

report = {'runtime': str(System.Environment.Version), 'assemblies': [], 'loading_errors': [], 'components': [], 'diagnostic_errors': [], 'discovered_files': []}
for assembly in System.AppDomain.CurrentDomain.GetAssemblies():
    if 'particleengine' in str(assembly.FullName).lower():
        item = {'identity': str(assembly.FullName)}
        try: item['location'] = str(assembly.Location)
        except Exception as error: item['location_error'] = str(error)
        report['assemblies'].append(item)
server = Grasshopper.Instances.ComponentServer
try:
    for error in server.LoadingExceptions:
        entry = {'text': str(error)}
        for field in error.GetType().GetFields():
            try: entry[str(field.Name)] = str(field.GetValue(error))
            except: pass
        for prop in error.GetType().GetProperties():
            if prop.GetIndexParameters().Length == 0:
                try: entry[str(prop.Name)] = str(prop.GetValue(error, None))
                except: pass
        report['loading_errors'].append(entry)
except Exception:
    report['diagnostic_errors'].append(traceback.format_exc())
try:
    for proxy in server.ObjectProxies:
        if str(proxy.Desc.Category) == 'Particle Engine':
            report['components'].append({'name': str(proxy.Desc.Name), 'guid': str(proxy.Guid), 'exposure': str(proxy.Exposure), 'location': str(proxy.Location), 'library': str(proxy.LibraryGuid)})
except Exception:
    report['diagnostic_errors'].append(traceback.format_exc())
try:
    # Discovery only: do not load assemblies, change settings or alter the document.
    for entry in Grasshopper.Kernel.GH_ComponentServer.ExternalFiles(False, False):
        if 'particle' in str(entry.FilePath).lower():
            report['discovered_files'].append(str(entry.FilePath))
except Exception:
    report['diagnostic_errors'].append(traceback.format_exc())
try:
    report['category_short_name'] = str(server.GetCategoryShortName('Particle Engine'))
    report['category_symbol'] = str(server.GetCategorySymbolName('Particle Engine'))
    icon = server.GetCategoryIcon('Particle Engine')
    report['category_icon'] = None if icon is None else [icon.Width, icon.Height]
except Exception:
    report['diagnostic_errors'].append(traceback.format_exc())
report_dir = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', 'diagnostics'))
if not os.path.isdir(report_dir):
    os.makedirs(report_dir)
path = os.path.join(report_dir, 'particle-engine-load-report.json')
with open(path, 'w') as output:
    json.dump(report, output, indent=2)
Rhino.RhinoApp.WriteLine('Particle Engine diagnostic saved: ' + path)
Rhino.RhinoApp.WriteLine('Loaded ParticleEngine assemblies: ' + str(len(report['assemblies'])))
Rhino.RhinoApp.WriteLine('Registered Particle Engine components: ' + str(len(report['components'])))
Rhino.RhinoApp.WriteLine('Discovered Particle plugin files: ' + str(len(report['discovered_files'])))
for entry in report['loading_errors']:
    if 'particle' in json.dumps(entry).lower():
        Rhino.RhinoApp.WriteLine(json.dumps(entry, indent=2))
