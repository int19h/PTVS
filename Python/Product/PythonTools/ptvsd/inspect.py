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

import sys
from ptvsd.util import unicode, xrange

NONEXPANDABLE_TYPES = {type(None), int, str, bool, float, object, unicode]}
try:
    NONEXPANDABLE_TYPES.append(long)
except NameError: pass

# key is type, value is function producing the raw repr
TYPES_WITH_RAW_REPR = {
    unicode: (lambda s: s),

    # getfilesystemencoding is used here because it effectively corresponds to the notion of "locale encoding":
    # current ANSI codepage on Windows, LC_CTYPE on Linux, UTF-8 on OS X - which is exactly what we want.
    bytearray: lambda b: b.decode(sys.getfilesystemencoding(), 'ignore') ,
}

if sys.version_info >= (3,):
    TYPES_WITH_RAW_REPR[bytes] = TYPES_WITH_RAW_REPR[bytearray]
else:
    TYPES_WITH_RAW_REPR[str] = TYPES_WITH_RAW_REPR[unicode]


class EvaluationResult(object):
    __slots__ = [
        # Value wrapped by this evaluation result
        'value',
        # Short name of this result relative to its parent. None if it's not a child.
        'name',
        # Full expression that can be evaluated to recompute value, or None if there isn't one.
        'expr',
        # Cached value of len(value). This is needed to compute EXPANDABLE, and it can be O(N),
        # e.g., for iterators, so caching it is desirable.
        'len',
        # Bit flags - see below.
        'flags',
    ]

    # Flags
    ## This value has children that can be queried.
    EXPANDABLE = 1
    ## This value is a result of a method call.
    METHOD_CALL = 2
    ## Recomputing this value will cause observable side effects.
    SIDE_EFFECTS = 4
    ## This value has a raw representation.
    HAS_RAW_REPR = 16

    def __init__(self, value, name=None, expr=None, flags=0):
        self.value = value
        self.name = name
        self.expr = expr
        self.len = len(value)
        self.flags = flags

        obj_type = type(value)
        if obj_type not in NONEXPANDABLE_TYPES and self.len != 0:
            self.flags |= EXPANDABLE

        try:
            for cls in TYPES_WITH_RAW_REPR:
                if issubclass(obj_type, cls):
                    flags |= PYTHON_EVALUATION_RESULT_HAS_RAW_REPR
                    break
        except: # guard against broken issubclass for types which aren't actually types, like vtkclass
            pass




            
        
        


class SafeRepr(object):
    # String types are truncated to maxstring_outer when at the outer-
    # most level, and truncated to maxstring_inner characters inside
    # collections.
    maxstring_outer = 2 ** 16
    maxstring_inner = 30

    if sys.version_info >= (3, 0):
        string_types = (str, bytes)
        set_info = (set, '{', '}', False)
        frozenset_info = (frozenset, 'frozenset({', '})', False)
    else:
        string_types = (str, unicode)
        set_info = (set, 'set([', '])', False)
        frozenset_info = (frozenset, 'frozenset([', '])', False)

    # Collection types are recursively iterated for each limit in
    # maxcollection.
    maxcollection = (15, 10)

    # Specifies type, prefix string, suffix string, and whether to include a
    # comma if there is only one element. (Using a sequence rather than a
    # mapping because we use isinstance() to determine the matching type.)
    collection_types = [
        (tuple, '(', ')', True),
        (list, '[', ']', False),
        frozenset_info,
        set_info,
    ]
    try:
        from collections import deque
        collection_types.append((deque, 'deque([', '])', False))
    except:
        pass

    # type, prefix string, suffix string, item prefix string, item key/value separator, item suffix string
    dict_types = [(dict, '{', '}', '', ': ', '')]
    try:
        from collections import OrderedDict
        dict_types.append((OrderedDict, 'OrderedDict([', '])', '(', ', ', ')'))
    except:
        pass

    # All other types are treated identically to strings, but using
    # different limits.
    maxother_outer = 2 ** 16
    maxother_inner = 30
    
    def __call__(self, obj):
        try:
            return ''.join(self._repr(obj, 0))
        except:
            try:
                return 'An exception was raised: %r' % sys.exc_info()[1]
            except:
                return 'An exception was raised'

    def _repr(self, obj, level):
        '''Returns an iterable of the parts in the final repr string.'''

        try:
            obj_repr = type(obj).__repr__
        except:
            obj_repr = None

        def has_obj_repr(t):
            r = t.__repr__
            try:
                return obj_repr == r
            except:
                return obj_repr is r

        for t, prefix, suffix, comma in self.collection_types:
            if isinstance(obj, t) and has_obj_repr(t):
                return self._repr_iter(obj, level, prefix, suffix, comma)
        
        for t, prefix, suffix, item_prefix, item_sep, item_suffix in self.dict_types:
            if isinstance(obj, t) and has_obj_repr(t):
                return self._repr_dict(obj, level, prefix, suffix, item_prefix, item_sep, item_suffix)

        for t in self.string_types:
            if isinstance(obj, t) and has_obj_repr(t):
                return self._repr_str(obj, level)

        if self._is_long_iter(obj):
            return self._repr_long_iter(obj)
        
        return self._repr_other(obj, level)

    # Determines whether an iterable exceeds the limits set in maxlimits, and is therefore unsafe to repr().
    def _is_long_iter(self, obj, level = 0):
        try:
            # Strings have their own limits (and do not nest). Because they don't have __iter__ in 2.x, this
            # check goes before the next one.
            if isinstance(obj, self.string_types):
                return len(obj) > self.maxstring_inner

            # If it's not an iterable (and not a string), it's fine.
            if not hasattr(obj, '__iter__'):
                return False

            # Iterable is its own iterator - this is a one-off iterable like generator or enumerate(). We can't
            # really count that, but repr() for these should not include any elements anyway, so we can treat it
            # the same as non-iterables.
            if obj is iter(obj):
                return False

            # xrange reprs fine regardless of length.
            if isinstance(obj, xrange):
                return False

            # numpy and scipy collections (ndarray etc) have self-truncating repr, so they're always safe.
            try:
                module = type(obj).__module__.partition('.')[0]
                if module in ('numpy', 'scipy'):
                    return False
            except:
                pass

            # Iterables that nest too deep are considered long.
            if level >= len(self.maxcollection):
                return True

            # It is too long if the length exceeds the limit, or any of its elements are long iterables.
            if hasattr(obj, '__len__'):
                try:
                    l = len(obj)
                except:
                    l = None
                if l is not None and l > self.maxcollection[level]:
                    return True
                return any((self._is_long_iter(item, level + 1) for item in obj))
            return any(i > self.maxcollection[level] or self._is_long_iter(item, level + 1) for i, item in enumerate(obj))

        except:
            # If anything breaks, assume the worst case.
            return True
    
    def _repr_iter(self, obj, level, prefix, suffix, comma_after_single_element = False):
        yield prefix
        
        if level >= len(self.maxcollection):
            yield '...'
        else:
            count = self.maxcollection[level]
            yield_comma = False
            for item in obj:
                if yield_comma:
                    yield ', '
                yield_comma = True
                
                count -= 1
                if count <= 0:
                    yield '...'
                    break

                for p in self._repr(item, 100 if item is obj else level + 1):
                    yield p
            else:
                if comma_after_single_element and count == self.maxcollection[level] - 1:
                    yield ','
        yield suffix

    def _repr_long_iter(self, obj):
        try:
            obj_repr = '<%s, len() = %s>' % (type(obj).__name__, len(obj))
        except:
            try:
                obj_repr = '<' + type(obj).__name__ + '>'
            except:
                obj_repr = '<no repr available for object>'
        yield obj_repr
        
    def _repr_dict(self, obj, level, prefix, suffix, item_prefix, item_sep, item_suffix):
        if not obj:
            yield prefix + suffix
            return
        if level >= len(self.maxcollection):
            yield prefix + '...' + suffix
            return
        
        yield prefix
        
        count = self.maxcollection[level]
        yield_comma = False
        
        try:
            sorted_keys = sorted(obj)
        except Exception:
            sorted_keys = list(obj)
        
        for key in sorted_keys:
            if yield_comma:
                yield ', '
            yield_comma = True
            
            count -= 1
            if count <= 0:
                yield '...'
                break
            
            yield item_prefix
            for p in self._repr(key, level + 1):
                yield p

            yield item_sep

            try:
                item = obj[key]
            except Exception:
                yield '<?>'
            else:
                for p in self._repr(item, 100 if item is obj else level + 1):
                    yield p
            yield item_suffix
        
        yield suffix

    def _repr_str(self, obj, level):
        return self._repr_obj(obj, level, self.maxstring_inner, self.maxstring_outer)

    def _repr_other(self, obj, level):
        return self._repr_obj(obj, level, self.maxother_inner, self.maxother_outer)
    
    def _repr_obj(self, obj, level, limit_inner, limit_outer):
        try:
            obj_repr = repr(obj)
        except:
            try:
                obj_repr = object.__repr__(obj)
            except:
                try:
                    obj_repr = '<no repr available for ' + type(obj).__name__ + '>'
                except:
                    obj_repr = '<no repr available for object>'
        
        limit = limit_inner if level > 0 else limit_outer
        
        if limit >= len(obj_repr):
            yield obj_repr
            return
        
        # Slightly imprecise calculations - we may end up with a string that is
        # up to 3 characters longer than limit. If you need precise formatting,
        # you are using the wrong class.
        left_count, right_count = max(1, int(2 * limit / 3)), max(1, int(limit / 3))
        
        yield obj_repr[:left_count]
        yield '...'
        yield obj_repr[-right_count:]

safe_repr = SafeRepr()
