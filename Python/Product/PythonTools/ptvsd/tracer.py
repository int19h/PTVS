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

__author__ = "Microsoft Corporation <ptvshelp@microsoft.com>"
__version__ = "3.2.0.0"

# Globals are slow, because they require a string lookup on every access. To minimize this
# overhead on hot paths like trace_func, we define the entirety of this module inside a
# function, so that all "globals" are actually locals. Because Python 2 doesn't support
# mutating locals captured from the outer scope inside the closure, any variable that needs
# to be mutable is represented as a list of one element. All variables and functions that
# actually need to be exported from the module have to be explicitly declared as "global".
def main():
    import sys
    from os import path
    try:
        import thread
    except ImportError:
        import _thread as thread

    try:
        xrange
    except:
        xrange = range

    global Breakpoint
    class Breakpoint(tuple):
        __slots__ = [
            'id',
            'filename',
            'line_number',
            'is_bound',
            'hit_count',
            'condition_kind',
            'condition',
            'last_condition_value',
            'pass_count_kind',
            'pass_count', 
        ]

        _dummy_last_condition_value = object()

        _next_id = 1
        #_next_id_lock = thread.allocate_lock()

        def __init__(cls, filename, line_number):
            self.id = Breakpoint._next_id
            Breakpoint._next_id += 1

            self.filename = filename
            self.line_number = line_number
            self.is_bound = False
            self.hit_count = 0
            self.condition_kind = None
            self.pass_count_kind = None

            # Extend list if needed to have an entry for this line_number.
            extra = self.line_number - len(breakpoints) + 1
            if extra > 0:
                breakpoints.extend([None] * extra)

            bps_on_line = breakpoints[self.line_number]
            bps_in_file = bps_on_line.setdefault(self.filename, set())
            bps_in_file.add(self)

        def remove(self):
            bps_on_line = breakpoints[self.line_number]
            assert bps_on_line
            bps_in_file = bps_on_line[self.filename]
            assert bps_in_file
            bps_in_file.remove(self)

    # Breakpoints are stored as a list of dicts of sets. Index in the outer list corresponds
    # to line_number of the breakpoint, with lines that have no breakpoints either don't have
    # an entry in the list at all (i.e. its length is shorter than the index), or have one that
    # is None. For lines that do have one or more breakpoints, the list entry is a dict mapping
    # filenames to set of Breakpoint objects on that line - note that there can be more than
    # one Breakpoint on the same line, with different ids and other properties.
    #
    # This arrangement optimizes access for breakpoint checks in line trace callback, where
    # the most common case is that the current line does not have a matching breakpoint:
    # indexing the list using a line number and truth-checking the result is very fast, and
    # if there's no breakpoint, that's the only operation that needs to be done before returning.
    # Only if line number matches, a slower dict lookup on filenames needs to be done.
    breakpoints = []

    class Thread(object):
        __slots__ = [
            'id',
            'stepping',
            'trace_func',
            'prev_trace_func',
        ]

        def __init__(self, id = None):
            if id is not None:
                self.id = id 
            else:
                self.id = thread.get_ident()
            self.trace_func = self.trace_func

        def trace_func(self, frame, event, arg):
            # If we're so far into process shutdown that modules are being unloaded, stop tracing
            # since we can't rely on any modules we've imported to still be working.
            if not sys.modules:
                return None

    global start_tracing
    def start_tracing():
        pass

    global stop_tracing
    def stop_tracing():
        pass

        

