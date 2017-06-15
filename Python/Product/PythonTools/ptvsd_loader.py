import sys
import os.path

ptvs_lib_path = os.path.dirname(__file__)
sys.path.insert(0, ptvs_lib_path)
try:
    from ptvsd.visualstudio_py_debugger import attach_process, new_thread, new_external_thread, set_debugger_dll_handle
finally:
    sys.path.remove(ptvs_lib_path)
