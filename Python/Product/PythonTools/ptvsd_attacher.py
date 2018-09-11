# Python Tools for Visual Studio
# Copyright(c) Microsoft Corporation
# All rights reserved.
# 
# Licensed under the Apache License, Version 2.0 (the License); you may not use
# this file except in compliance with the License. You may obtain a copy of the
# License at http://www.apache.org/licenses/LICENSE-2.0
# 
# THIS CODE IS PROVIDED ON AN  *AS IS* BASIS, WITHOUT WARRANTIES OR CONDITIONS
# OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION ANY
# IMPLIED WARRANTIES OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR PURPOSE,
# MERCHANTABLITY OR NON-INFRINGEMENT.
# 
# See the Apache Version 2.0 License for specific language governing
# permissions and limitations under the License.

"""
Attach debugger to a local process with a given ID.
"""

import os
import os.path
import sys
import traceback

# Arguments are:
# 1. PID of the process to attach to.
# 2. VS debugger port to connect to.
# 3. GUID for the debug session.
# 4. Debug options (as list of names - see enum PythonDebugOptions).
# 5. '-g' to use the installed ptvsd package, rather than bundled one.

pid = int(sys.argv[1])
port_num = int(sys.argv[2])
debug_id = sys.argv[3]
debug_options = set([opt.strip() for opt in sys.argv[4].split(',')])
bundled_ptvsd = not (len(sys.argv) > 5 and sys.argv[5] == '-g')

# Load the debugger package
try:
    ptvsd_loaded = False
    ptvs_lib_path = None
    if bundled_ptvsd:
        ptvs_lib_path = os.path.dirname(__file__)
        sys.path.insert(0, ptvs_lib_path)
    else:
        #ptvs_lib_path = os.path.join(os.path.dirname(__file__), 'Packages')
        #sys.path.append(ptvs_lib_path)
        ptvs_lib_path = 'c:/git/ptvsd'
        sys.path.insert(0, ptvs_lib_path)

    print(sys.path)
    import ptvsd
    print(ptvsd.__file__)
    ptvsd_loaded = True

    del sys.argv[1:]
    sys.argv += ['--host', '127.0.0.1', '--port', str(port_num), '--pid', str(pid)]
    from ptvsd.__main__ import main
    main()

except:
    traceback.print_exc()
    if not bundled_ptvsd and not ptvsd_loaded:
        # This is experimental debugger import error. Exit immediately.
        # This process will be killed by VS since it does not see a debugger
        # connect to it. The exit code we will get there will be wrong.
        # 126 : ERROR_MOD_NOT_FOUND
        sys.exit(126)
    print('''
Internal error detected. Please copy the above traceback and report at
https://go.microsoft.com/fwlink/?LinkId=293415

Press Enter to close. . .''')
    try:
        raw_input()
    except NameError:
        input()
    sys.exit(1)
